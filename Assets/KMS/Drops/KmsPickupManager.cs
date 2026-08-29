using System.Collections.Generic;
using HDY;
using UnityEngine;

namespace KMS
{
    [DisallowMultipleComponent]
    public sealed class KmsPickupManager : MonoBehaviour
    {
        private const float ReferenceRetryInterval = 1f;

        [Header("Pickup Prefabs")]
        [SerializeField] private KmsGoldPickup goldPickupPrefab;
        [SerializeField] private KmsWeaponPickup weaponPickupPrefab;
        [SerializeField] private KmsHealthPickup healthPickupPrefab;

        [Header("Pool Prewarm")]
        [SerializeField, Min(0)] private int initialGoldPoolSize = 64;
        [SerializeField, Min(0)] private int initialWeaponPoolSize = 4;
        [SerializeField, Min(0)] private int initialHealthPoolSize = 4;

        [Header("Pool Safety Cap (안전장치)")]
        [Tooltip("각 픽업 타입(골드/무기/체력)별로 동시에 활성화될 수 있는 최대 개수. 이 값을 넘으면 가장 오래 활성화되어 있던 픽업을 강제로 회수(풀로 반환)한다.")]
        [SerializeField, Min(1)] private int maxActivePickupsPerType = 500;

        [Header("Spawn Throttle (안전장치)")]
        [Tooltip("한 프레임에 실제로 생성(Instantiate)할 수 있는 픽업(골드/무기/체력 합산) 최대 개수. 몬스터가 대량으로 동시에 죽어 드랍 요청이 한꿄에 몰려도, 이 값을 넘어서는 요청은 큐에 쌀았다가 이후 프레임에 걸쳐 분산 생성된다(순간 렉 완화).")]
        [SerializeField, Min(1)] private int maxPickupSpawnsPerFrame = 5;

        private readonly Queue<KmsGoldPickup> inactiveGoldPickups = new Queue<KmsGoldPickup>();
        private readonly Queue<KmsWeaponPickup> inactiveWeaponPickups = new Queue<KmsWeaponPickup>();
        private readonly Queue<KmsHealthPickup> inactiveHealthPickups = new Queue<KmsHealthPickup>();
        private readonly List<KmsGoldPickup> activeGoldPickups = new List<KmsGoldPickup>();
        private readonly List<KmsWeaponPickup> activeWeaponPickups = new List<KmsWeaponPickup>();
        private readonly List<KmsHealthPickup> activeHealthPickups = new List<KmsHealthPickup>();

        // 각 활성 픽업을 생성(스폰) 순서대로 추적한다(맨 앞이 가장 오래된 것). maxActivePickupsPerType를 넘어서면
        // 가장 오래된 픽업을 강제로 풀에 반환하여 무한정 늘어나는 것을 방지한다(방어코드).
        private readonly LinkedList<KmsGoldPickup> activeGoldOrder = new LinkedList<KmsGoldPickup>();
        private readonly Dictionary<KmsGoldPickup, LinkedListNode<KmsGoldPickup>> activeGoldOrderNodes = new Dictionary<KmsGoldPickup, LinkedListNode<KmsGoldPickup>>();
        private readonly LinkedList<KmsWeaponPickup> activeWeaponOrder = new LinkedList<KmsWeaponPickup>();
        private readonly Dictionary<KmsWeaponPickup, LinkedListNode<KmsWeaponPickup>> activeWeaponOrderNodes = new Dictionary<KmsWeaponPickup, LinkedListNode<KmsWeaponPickup>>();
        private readonly LinkedList<KmsHealthPickup> activeHealthOrder = new LinkedList<KmsHealthPickup>();
        private readonly Dictionary<KmsHealthPickup, LinkedListNode<KmsHealthPickup>> activeHealthOrderNodes = new Dictionary<KmsHealthPickup, LinkedListNode<KmsHealthPickup>>();

        // 대량 사망 등으로 한 프레임에 드랍 요청이 몰릴 때, maxPickupSpawnsPerFrame을 초과하는 요청은 즉시 생성하지 않고
        // 아래 큐에 넘겨서 다음 프레임(들)에 걸쳐 나눠 생성한다(순간 스파이크 완화 방어코드).
        private readonly Queue<GoldSpawnRequest> pendingGoldSpawns = new Queue<GoldSpawnRequest>();
        private readonly Queue<WeaponSpawnRequest> pendingWeaponSpawns = new Queue<WeaponSpawnRequest>();
        private readonly Queue<HealthSpawnRequest> pendingHealthSpawns = new Queue<HealthSpawnRequest>();
        private int pickupSpawnsRemainingThisFrame;

        private readonly struct GoldSpawnRequest
        {
            public GoldSpawnRequest(Vector3 origin, Vector2 scatterOffset)
            {
                Origin = origin;
                ScatterOffset = scatterOffset;
            }

            public Vector3 Origin { get; }
            public Vector2 ScatterOffset { get; }
        }

        private readonly struct WeaponSpawnRequest
        {
            public WeaponSpawnRequest(string weaponId, ItemGrade grade, Vector3 origin, Vector2 scatterOffset)
            {
                WeaponId = weaponId;
                Grade = grade;
                Origin = origin;
                ScatterOffset = scatterOffset;
            }

            public string WeaponId { get; }
            public ItemGrade Grade { get; }
            public Vector3 Origin { get; }
            public Vector2 ScatterOffset { get; }
        }

        private readonly struct HealthSpawnRequest
        {
            public HealthSpawnRequest(Vector3 origin, Vector2 scatterOffset)
            {
                Origin = origin;
                ScatterOffset = scatterOffset;
            }

            public Vector3 Origin { get; }
            public Vector2 ScatterOffset { get; }
        }

        public int PendingSpawnCount => pendingGoldSpawns.Count + pendingWeaponSpawns.Count + pendingHealthSpawns.Count;

        private KmsGoldDropController goldDropController;
        private KmsWeaponDropController weaponDropController;
        private KmsHealthDropController healthDropController;
        private PlayerStats playerStats;
        private WeaponInventory weaponInventory;
        private PlayerPickupMagnet pickupMagnet;
        private float nextReferenceRetryTime;
        private int nextGoldSequence;
        private int nextWeaponSequence;
        private int nextHealthSequence;

        public PlayerStats PlayerStats => playerStats;
        public WeaponInventory WeaponInventory => weaponInventory;
        public int ActiveGoldCount => activeGoldPickups.Count;
        public int ActiveWeaponCount => activeWeaponPickups.Count;
        public int ActiveHealthCount => activeHealthPickups.Count;
        public int InactiveGoldCount => inactiveGoldPickups.Count;
        public int InactiveWeaponCount => inactiveWeaponPickups.Count;
        public int InactiveHealthCount => inactiveHealthPickups.Count;

        private void Awake()
        {
            goldDropController = GetComponent<KmsGoldDropController>();
            weaponDropController = GetComponent<KmsWeaponDropController>();
            healthDropController = GetComponent<KmsHealthDropController>();
            ResolveSceneReferences();
            PrewarmPools();
        }

        private void Start()
        {
            ResolveSceneReferences();
        }

        private void Update()
        {
            // 매 프레임 시작 시 실제 생성 예산을 리셋하고, 지난 프레임에 몰려 큐에 쌌인 요청부터 먼저 소화한다.
            pickupSpawnsRemainingThisFrame = Mathf.Max(1, maxPickupSpawnsPerFrame);
            DrainPendingSpawnQueues();

            RetryMissingSceneReferences();

            float deltaTime = Time.deltaTime;
            UpdateGoldPickups(deltaTime);
            UpdateWeaponPickups(deltaTime);
            UpdateHealthPickups(deltaTime);
        }

        public void ConfigureAssets(
            KmsGoldPickup goldPrefab,
            KmsWeaponPickup weaponPrefab,
            KmsHealthPickup healthPrefab)
        {
            goldPickupPrefab = goldPrefab;
            weaponPickupPrefab = weaponPrefab;
            healthPickupPrefab = healthPrefab;
        }

        public bool SpawnGold(Vector3 origin, Vector2 scatterOffset)
        {
            if (pickupSpawnsRemainingThisFrame <= 0)
            {
                pendingGoldSpawns.Enqueue(new GoldSpawnRequest(origin, scatterOffset));
                return true;
            }

            pickupSpawnsRemainingThisFrame--;
            return SpawnGoldImmediate(origin, scatterOffset);
        }

        private bool SpawnGoldImmediate(Vector3 origin, Vector2 scatterOffset)
        {
            KmsGoldPickup pickup = RentGoldPickup();
            if (pickup == null)
            {
                return false;
            }

            pickup.name = $"KmsGoldPickup_{++nextGoldSequence:000}";
            pickup.Launch(origin, scatterOffset);
            activeGoldPickups.Add(pickup);
            TrackActiveGold(pickup);
            EnforceGoldActiveLimit();
            return true;
        }

        public bool SpawnWeapon(string weaponId, ItemGrade grade, Vector3 origin, Vector2 scatterOffset)
        {
            if (pickupSpawnsRemainingThisFrame <= 0)
            {
                pendingWeaponSpawns.Enqueue(new WeaponSpawnRequest(weaponId, grade, origin, scatterOffset));
                return true;
            }

            pickupSpawnsRemainingThisFrame--;
            return SpawnWeaponImmediate(weaponId, grade, origin, scatterOffset);
        }

        private bool SpawnWeaponImmediate(string weaponId, ItemGrade grade, Vector3 origin, Vector2 scatterOffset)
        {
            if (string.IsNullOrWhiteSpace(weaponId))
            {
                Debug.LogError("[KMS] 빈 무기 ID로 픽업을 생성할 수 없습니다.", this);
                return false;
            }

            KmsWeaponPickup pickup = RentWeaponPickup();
            if (pickup == null)
            {
                return false;
            }

            pickup.name = $"KmsWeaponPickup_{++nextWeaponSequence:000}_{weaponId}";
            pickup.Initialize(weaponId, grade, origin, scatterOffset);
            activeWeaponPickups.Add(pickup);
            TrackActiveWeapon(pickup);
            EnforceWeaponActiveLimit();
            return true;
        }

        public bool SpawnHealth(Vector3 origin, Vector2 scatterOffset)
        {
            if (pickupSpawnsRemainingThisFrame <= 0)
            {
                pendingHealthSpawns.Enqueue(new HealthSpawnRequest(origin, scatterOffset));
                return true;
            }

            pickupSpawnsRemainingThisFrame--;
            return SpawnHealthImmediate(origin, scatterOffset);
        }

        private bool SpawnHealthImmediate(Vector3 origin, Vector2 scatterOffset)
        {
            KmsHealthPickup pickup = RentHealthPickup();
            if (pickup == null)
            {
                return false;
            }

            pickup.name = $"KmsHealthPickup_{++nextHealthSequence:000}";
            pickup.Launch(origin, scatterOffset);
            activeHealthPickups.Add(pickup);
            TrackActiveHealth(pickup);
            EnforceHealthActiveLimit();
            return true;
        }

        private void ResolveSceneReferences()
        {
            if (playerStats == null)
            {
                playerStats = FindFirstObjectByType<PlayerStats>();
            }

            if (weaponInventory == null)
            {
                weaponInventory = FindFirstObjectByType<WeaponInventory>();
            }

            if (pickupMagnet == null)
            {
                pickupMagnet = FindFirstObjectByType<PlayerPickupMagnet>();
            }
        }

        private void RetryMissingSceneReferences()
        {
            bool needsRetry = playerStats == null
                || weaponInventory == null
                || pickupMagnet == null
                || (goldDropController != null
                    && goldDropController.isActiveAndEnabled
                    && !goldDropController.HasSpawnerSubscription)
                || (weaponDropController != null
                    && weaponDropController.isActiveAndEnabled
                    && !weaponDropController.HasSpawnerSubscription)
                || (healthDropController != null
                    && healthDropController.isActiveAndEnabled
                    && !healthDropController.HasSpawnerSubscription);
            if (!needsRetry || Time.unscaledTime < nextReferenceRetryTime)
            {
                return;
            }

            nextReferenceRetryTime = Time.unscaledTime + ReferenceRetryInterval;
            ResolveSceneReferences();
            goldDropController?.EnsureSpawnerSubscription();
            weaponDropController?.EnsureSpawnerSubscription();
            healthDropController?.EnsureSpawnerSubscription();
        }

        private void PrewarmPools()
        {
            for (int index = 0; index < Mathf.Max(0, initialGoldPoolSize); index++)
            {
                KmsGoldPickup pickup = CreateGoldPickup();
                if (pickup == null)
                {
                    break;
                }

                inactiveGoldPickups.Enqueue(pickup);
            }

            for (int index = 0; index < Mathf.Max(0, initialWeaponPoolSize); index++)
            {
                KmsWeaponPickup pickup = CreateWeaponPickup();
                if (pickup == null)
                {
                    break;
                }

                inactiveWeaponPickups.Enqueue(pickup);
            }

            for (int index = 0; index < Mathf.Max(0, initialHealthPoolSize); index++)
            {
                KmsHealthPickup pickup = CreateHealthPickup();
                if (pickup == null)
                {
                    break;
                }

                inactiveHealthPickups.Enqueue(pickup);
            }
        }

private void UpdateGoldPickups(float deltaTime)
        {
            float magnetRadius = pickupMagnet != null ? pickupMagnet.Radius : 0f;
            for (int index = activeGoldPickups.Count - 1; index >= 0; index--)
            {
                KmsGoldPickup pickup = activeGoldPickups[index];
                if (pickup == null)
                {
                    RemoveAtSwapBack(activeGoldPickups, index);
                    continue;
                }

                if (!pickup.Tick(deltaTime, playerStats, magnetRadius))
                {
                    continue;
                }

                RemoveAtSwapBack(activeGoldPickups, index);
                ReturnGoldPickup(pickup);
            }
        }

private void UpdateWeaponPickups(float deltaTime)
        {
            float magnetRadius = pickupMagnet != null ? pickupMagnet.Radius : 0f;
            for (int index = activeWeaponPickups.Count - 1; index >= 0; index--)
            {
                KmsWeaponPickup pickup = activeWeaponPickups[index];
                if (pickup == null)
                {
                    RemoveAtSwapBack(activeWeaponPickups, index);
                    continue;
                }

                if (!pickup.Tick(deltaTime, weaponInventory, magnetRadius))
                {
                    continue;
                }

                RemoveAtSwapBack(activeWeaponPickups, index);
                ReturnWeaponPickup(pickup);
            }
        }

        private void UpdateHealthPickups(float deltaTime)
        {
            for (int index = activeHealthPickups.Count - 1; index >= 0; index--)
            {
                KmsHealthPickup pickup = activeHealthPickups[index];
                if (pickup == null)
                {
                    RemoveAtSwapBack(activeHealthPickups, index);
                    continue;
                }

                if (!pickup.Tick(deltaTime, playerStats))
                {
                    continue;
                }

                RemoveAtSwapBack(activeHealthPickups, index);
                ReturnHealthPickup(pickup);
            }
        }

        private KmsGoldPickup RentGoldPickup()
        {
            KmsGoldPickup pickup = null;
            while (pickup == null && inactiveGoldPickups.Count > 0)
            {
                pickup = inactiveGoldPickups.Dequeue();
            }

            pickup ??= CreateGoldPickup();
            if (pickup != null)
            {
                pickup.gameObject.SetActive(true);
            }

            return pickup;
        }

        private KmsWeaponPickup RentWeaponPickup()
        {
            KmsWeaponPickup pickup = null;
            while (pickup == null && inactiveWeaponPickups.Count > 0)
            {
                pickup = inactiveWeaponPickups.Dequeue();
            }

            pickup ??= CreateWeaponPickup();
            if (pickup != null)
            {
                pickup.gameObject.SetActive(true);
            }

            return pickup;
        }

        private KmsHealthPickup RentHealthPickup()
        {
            KmsHealthPickup pickup = null;
            while (pickup == null && inactiveHealthPickups.Count > 0)
            {
                pickup = inactiveHealthPickups.Dequeue();
            }

            pickup ??= CreateHealthPickup();
            if (pickup != null)
            {
                pickup.gameObject.SetActive(true);
            }

            return pickup;
        }

        private KmsGoldPickup CreateGoldPickup()
        {
            if (goldPickupPrefab == null)
            {
                Debug.LogError("[KMS] 풀에 사용할 1골드 픽업 프리팹 참조가 없습니다.", this);
                return null;
            }

            KmsGoldPickup pickup = Instantiate(goldPickupPrefab, transform);
            pickup.name = "KmsGoldPickup_Pooled";
            pickup.ResetForPool();
            pickup.gameObject.SetActive(false);
            return pickup;
        }

        private KmsWeaponPickup CreateWeaponPickup()
        {
            if (weaponPickupPrefab == null)
            {
                Debug.LogError("[KMS] 풀에 사용할 무기 픽업 프리팹 참조가 없습니다.", this);
                return null;
            }

            KmsWeaponPickup pickup = Instantiate(weaponPickupPrefab, transform);
            pickup.name = "KmsWeaponPickup_Pooled";
            pickup.ResetForPool();
            pickup.gameObject.SetActive(false);
            return pickup;
        }

        private KmsHealthPickup CreateHealthPickup()
        {
            if (healthPickupPrefab == null)
            {
                Debug.LogError("[KMS] 풀에 사용할 체력 회복 픽업 프리팹 참조가 없습니다.", this);
                return null;
            }

            KmsHealthPickup pickup = Instantiate(healthPickupPrefab, transform);
            pickup.name = "KmsHealthPickup_Pooled";
            pickup.ResetForPool();
            pickup.gameObject.SetActive(false);
            return pickup;
        }

        private void ReturnGoldPickup(KmsGoldPickup pickup)
        {
            UntrackActiveGold(pickup);
            pickup.ResetForPool();
            pickup.transform.SetParent(transform, false);
            pickup.gameObject.SetActive(false);
            inactiveGoldPickups.Enqueue(pickup);
        }

        private void ReturnWeaponPickup(KmsWeaponPickup pickup)
        {
            UntrackActiveWeapon(pickup);
            pickup.ResetForPool();
            pickup.transform.SetParent(transform, false);
            pickup.gameObject.SetActive(false);
            inactiveWeaponPickups.Enqueue(pickup);
        }

        private void ReturnHealthPickup(KmsHealthPickup pickup)
        {
            UntrackActiveHealth(pickup);
            pickup.ResetForPool();
            pickup.transform.SetParent(transform, false);
            pickup.gameObject.SetActive(false);
            inactiveHealthPickups.Enqueue(pickup);
        }

        private void TrackActiveGold(KmsGoldPickup pickup)
        {
            UntrackActiveGold(pickup);
            activeGoldOrderNodes[pickup] = activeGoldOrder.AddLast(pickup);
        }

        private bool UntrackActiveGold(KmsGoldPickup pickup)
        {
            if (pickup == null || !activeGoldOrderNodes.TryGetValue(pickup, out LinkedListNode<KmsGoldPickup> node))
            {
                return false;
            }

            activeGoldOrder.Remove(node);
            activeGoldOrderNodes.Remove(pickup);
            return true;
        }

        private void EnforceGoldActiveLimit()
        {
            while (activeGoldOrder.Count > maxActivePickupsPerType)
            {
                KmsGoldPickup oldest = activeGoldOrder.First.Value;
                int index = activeGoldPickups.IndexOf(oldest);
                if (index >= 0)
                {
                    RemoveAtSwapBack(activeGoldPickups, index);
                }

                ReturnGoldPickup(oldest);
            }
        }

        private void TrackActiveWeapon(KmsWeaponPickup pickup)
        {
            UntrackActiveWeapon(pickup);
            activeWeaponOrderNodes[pickup] = activeWeaponOrder.AddLast(pickup);
        }

        private bool UntrackActiveWeapon(KmsWeaponPickup pickup)
        {
            if (pickup == null || !activeWeaponOrderNodes.TryGetValue(pickup, out LinkedListNode<KmsWeaponPickup> node))
            {
                return false;
            }

            activeWeaponOrder.Remove(node);
            activeWeaponOrderNodes.Remove(pickup);
            return true;
        }

        private void EnforceWeaponActiveLimit()
        {
            while (activeWeaponOrder.Count > maxActivePickupsPerType)
            {
                KmsWeaponPickup oldest = activeWeaponOrder.First.Value;
                int index = activeWeaponPickups.IndexOf(oldest);
                if (index >= 0)
                {
                    RemoveAtSwapBack(activeWeaponPickups, index);
                }

                ReturnWeaponPickup(oldest);
            }
        }

        private void TrackActiveHealth(KmsHealthPickup pickup)
        {
            UntrackActiveHealth(pickup);
            activeHealthOrderNodes[pickup] = activeHealthOrder.AddLast(pickup);
        }

        private bool UntrackActiveHealth(KmsHealthPickup pickup)
        {
            if (pickup == null || !activeHealthOrderNodes.TryGetValue(pickup, out LinkedListNode<KmsHealthPickup> node))
            {
                return false;
            }

            activeHealthOrder.Remove(node);
            activeHealthOrderNodes.Remove(pickup);
            return true;
        }

        private void EnforceHealthActiveLimit()
        {
            while (activeHealthOrder.Count > maxActivePickupsPerType)
            {
                KmsHealthPickup oldest = activeHealthOrder.First.Value;
                int index = activeHealthPickups.IndexOf(oldest);
                if (index >= 0)
                {
                    RemoveAtSwapBack(activeHealthPickups, index);
                }

                ReturnHealthPickup(oldest);
            }
        }

        private void DrainPendingSpawnQueues()
        {
            while (pickupSpawnsRemainingThisFrame > 0 &&
                (pendingGoldSpawns.Count > 0 || pendingWeaponSpawns.Count > 0 || pendingHealthSpawns.Count > 0))
            {
                if (pendingGoldSpawns.Count > 0)
                {
                    pickupSpawnsRemainingThisFrame--;
                    GoldSpawnRequest request = pendingGoldSpawns.Dequeue();
                    SpawnGoldImmediate(request.Origin, request.ScatterOffset);
                    continue;
                }

                if (pendingWeaponSpawns.Count > 0)
                {
                    pickupSpawnsRemainingThisFrame--;
                    WeaponSpawnRequest request = pendingWeaponSpawns.Dequeue();
                    SpawnWeaponImmediate(request.WeaponId, request.Grade, request.Origin, request.ScatterOffset);
                    continue;
                }

                pickupSpawnsRemainingThisFrame--;
                HealthSpawnRequest healthRequest = pendingHealthSpawns.Dequeue();
                SpawnHealthImmediate(healthRequest.Origin, healthRequest.ScatterOffset);
            }
        }

        private static void RemoveAtSwapBack<T>(List<T> list, int index)
        {
            int lastIndex = list.Count - 1;
            list[index] = list[lastIndex];
            list.RemoveAt(lastIndex);
        }
    }
}
