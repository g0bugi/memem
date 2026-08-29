using HDY;
using UnityEngine;

namespace KMS
{
    [DisallowMultipleComponent]
    public sealed class KmsGoldDropController : MonoBehaviour
    {
        public const int MinimumDropCount = 1;
        public const int MaximumDropCount = 5;
        /// <summary>몬스터 한 마리가 한 번에 드랍하는 골드 개수의 절대 상한(안전장치). 시련 등 어떤 보너스 계산 결과가
        /// 나오든 이 값을 넘어서 실제로 스폰되는 일은 없다(중후반 드랍 개체가 과도하게 늘어 렉이 생기는 것을 방지).</summary>
        public const int HardMaximumDropCount = 10;

        [Header("Scatter")]
        [SerializeField, Min(0f)] private float minimumScatterDistance = 0.65f;
        [SerializeField, Min(0f)] private float maximumScatterDistance = 1.35f;
        [SerializeField, Range(0f, 45f)] private float angleJitter = 10f;

        private KmsMonsterSpawner monsterSpawner;
        private KmsPickupManager pickupManager;
        private bool isSubscribed;

        public int LastDropCount { get; private set; }
        public int TotalSpawnedPickupCount { get; private set; }
        internal bool HasSpawnerSubscription => isSubscribed && monsterSpawner != null;

        private void Awake()
        {
            ResolveReferences();
        }

        private void OnEnable()
        {
            SubscribeToSpawner();
        }

        private void Start()
        {
            SubscribeToSpawner();
        }

        private void OnDisable()
        {
            UnsubscribeFromSpawner();
        }

        public static int SelectDropCount(float unitRoll)
        {
            return SelectDropCount(unitRoll, MinimumDropCount, MaximumDropCount);
        }

        public static int SelectDropCount(float unitRoll, int minimumCount, int maximumCount)
        {
            int minCount = Mathf.Max(0, Mathf.Min(minimumCount, maximumCount));
            int maxCount = Mathf.Max(minCount, maximumCount);
            int range = (maxCount - minCount) + 1;
            float clampedRoll = Mathf.Clamp(unitRoll, 0f, 0.99999994f);
            int zeroBasedIndex = Mathf.FloorToInt(clampedRoll * range);
            return Mathf.Clamp(zeroBasedIndex + minCount, minCount, maxCount);
        }

        internal void EnsureSpawnerSubscription()
        {
            if (!isActiveAndEnabled || HasSpawnerSubscription)
            {
                return;
            }

            isSubscribed = false;
            monsterSpawner = null;
            SubscribeToSpawner();
        }

        private void SubscribeToSpawner()
        {
            ResolveReferences();
            if (isSubscribed || monsterSpawner == null)
            {
                return;
            }

            monsterSpawner.MonsterDied += HandleMonsterDied;
            isSubscribed = true;
        }

        private void UnsubscribeFromSpawner()
        {
            if (!isSubscribed)
            {
                return;
            }

            if (monsterSpawner != null)
            {
                monsterSpawner.MonsterDied -= HandleMonsterDied;
            }

            isSubscribed = false;
        }

        private void ResolveReferences()
        {
            if (pickupManager == null)
            {
                pickupManager = GetComponent<KmsPickupManager>();
            }

            if (monsterSpawner == null)
            {
                monsterSpawner = FindFirstObjectByType<KmsMonsterSpawner>();
            }
        }

        private void HandleMonsterDied(KmsMonster monster)
        {
            ResolveReferences();
            if (pickupManager == null)
            {
                Debug.LogError("[KMS] 골드 픽업을 관리할 KmsPickupManager가 없습니다.", this);
                return;
            }

            int trialMinimum = TrialManager.Instance != null ? TrialManager.Instance.MinGoldDropCount : MinimumDropCount;
            int trialMaximum = TrialManager.Instance != null ? TrialManager.Instance.MaxGoldDropCount : MaximumDropCount;

            // 방어코드: TrialManager가 이미 HardMaximumDropCount로 클램프하지만, 향후 TrialManager 계산이 바뀌거나
            // TrialManager.Instance가 없어 기본값(MaximumDropCount)만 쓰이는 경우에도 항상 안전하도록 스폰 직전에 한 번 더 클램프한다.
            trialMinimum = Mathf.Min(trialMinimum, HardMaximumDropCount);
            trialMaximum = Mathf.Min(trialMaximum, HardMaximumDropCount);

            int dropCount = SelectDropCount(Random.value, trialMinimum, trialMaximum);
            LastDropCount = dropCount;

            Vector3 origin = monster.transform.position;
            float baseAngle = Random.Range(0f, 360f);
            float minimumDistance = Mathf.Min(minimumScatterDistance, maximumScatterDistance);
            float maximumDistance = Mathf.Max(minimumScatterDistance, maximumScatterDistance);

            for (int index = 0; index < dropCount; index++)
            {
                float evenAngle = baseAngle + (360f * index / dropCount);
                float angle = evenAngle + Random.Range(-angleJitter, angleJitter);
                float distance = Random.Range(minimumDistance, maximumDistance);
                Vector2 direction = new Vector2(
                    Mathf.Cos(angle * Mathf.Deg2Rad),
                    Mathf.Sin(angle * Mathf.Deg2Rad));

                if (pickupManager.SpawnGold(origin, direction * distance))
                {
                    TotalSpawnedPickupCount++;
                }
            }
        }
    }
}
