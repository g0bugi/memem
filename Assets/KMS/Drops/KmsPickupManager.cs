using System.Collections.Generic;
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

        [Header("Pool Prewarm")]
        [SerializeField, Min(0)] private int initialGoldPoolSize = 64;
        [SerializeField, Min(0)] private int initialWeaponPoolSize = 4;

        private readonly Queue<KmsGoldPickup> inactiveGoldPickups = new Queue<KmsGoldPickup>();
        private readonly Queue<KmsWeaponPickup> inactiveWeaponPickups = new Queue<KmsWeaponPickup>();
        private readonly List<KmsGoldPickup> activeGoldPickups = new List<KmsGoldPickup>();
        private readonly List<KmsWeaponPickup> activeWeaponPickups = new List<KmsWeaponPickup>();

        private KmsGoldDropController goldDropController;
        private KmsWeaponDropController weaponDropController;
        private PlayerStats playerStats;
        private WeaponInventory weaponInventory;
        private float nextReferenceRetryTime;
        private int nextGoldSequence;
        private int nextWeaponSequence;

        public PlayerStats PlayerStats => playerStats;
        public WeaponInventory WeaponInventory => weaponInventory;
        public int ActiveGoldCount => activeGoldPickups.Count;
        public int ActiveWeaponCount => activeWeaponPickups.Count;
        public int InactiveGoldCount => inactiveGoldPickups.Count;
        public int InactiveWeaponCount => inactiveWeaponPickups.Count;

        private void Awake()
        {
            goldDropController = GetComponent<KmsGoldDropController>();
            weaponDropController = GetComponent<KmsWeaponDropController>();
            ResolveSceneReferences();
            PrewarmPools();
        }

        private void Start()
        {
            ResolveSceneReferences();
        }

        private void Update()
        {
            RetryMissingSceneReferences();

            float deltaTime = Time.deltaTime;
            UpdateGoldPickups(deltaTime);
            UpdateWeaponPickups(deltaTime);
        }

        public void ConfigureAssets(KmsGoldPickup goldPrefab, KmsWeaponPickup weaponPrefab)
        {
            goldPickupPrefab = goldPrefab;
            weaponPickupPrefab = weaponPrefab;
        }

        public bool SpawnGold(Vector3 origin, Vector2 scatterOffset)
        {
            KmsGoldPickup pickup = RentGoldPickup();
            if (pickup == null)
            {
                return false;
            }

            pickup.name = $"KmsGoldPickup_{++nextGoldSequence:000}";
            pickup.Launch(origin, scatterOffset);
            activeGoldPickups.Add(pickup);
            return true;
        }

        public bool SpawnWeapon(string weaponId, ItemGrade grade, Vector3 origin, Vector2 scatterOffset)
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
        }

        private void RetryMissingSceneReferences()
        {
            bool needsRetry = playerStats == null
                || weaponInventory == null
                || (goldDropController != null
                    && goldDropController.isActiveAndEnabled
                    && !goldDropController.HasSpawnerSubscription)
                || (weaponDropController != null
                    && weaponDropController.isActiveAndEnabled
                    && !weaponDropController.HasSpawnerSubscription);
            if (!needsRetry || Time.unscaledTime < nextReferenceRetryTime)
            {
                return;
            }

            nextReferenceRetryTime = Time.unscaledTime + ReferenceRetryInterval;
            ResolveSceneReferences();
            goldDropController?.EnsureSpawnerSubscription();
            weaponDropController?.EnsureSpawnerSubscription();
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
        }

        private void UpdateGoldPickups(float deltaTime)
        {
            for (int index = activeGoldPickups.Count - 1; index >= 0; index--)
            {
                KmsGoldPickup pickup = activeGoldPickups[index];
                if (pickup == null)
                {
                    RemoveAtSwapBack(activeGoldPickups, index);
                    continue;
                }

                if (!pickup.Tick(deltaTime, playerStats))
                {
                    continue;
                }

                RemoveAtSwapBack(activeGoldPickups, index);
                ReturnGoldPickup(pickup);
            }
        }

        private void UpdateWeaponPickups(float deltaTime)
        {
            for (int index = activeWeaponPickups.Count - 1; index >= 0; index--)
            {
                KmsWeaponPickup pickup = activeWeaponPickups[index];
                if (pickup == null)
                {
                    RemoveAtSwapBack(activeWeaponPickups, index);
                    continue;
                }

                if (!pickup.Tick(deltaTime, weaponInventory))
                {
                    continue;
                }

                RemoveAtSwapBack(activeWeaponPickups, index);
                ReturnWeaponPickup(pickup);
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

        private void ReturnGoldPickup(KmsGoldPickup pickup)
        {
            pickup.ResetForPool();
            pickup.transform.SetParent(transform, false);
            pickup.gameObject.SetActive(false);
            inactiveGoldPickups.Enqueue(pickup);
        }

        private void ReturnWeaponPickup(KmsWeaponPickup pickup)
        {
            pickup.ResetForPool();
            pickup.transform.SetParent(transform, false);
            pickup.gameObject.SetActive(false);
            inactiveWeaponPickups.Enqueue(pickup);
        }

        private static void RemoveAtSwapBack<T>(List<T> list, int index)
        {
            int lastIndex = list.Count - 1;
            list[index] = list[lastIndex];
            list.RemoveAt(lastIndex);
        }
    }
}
