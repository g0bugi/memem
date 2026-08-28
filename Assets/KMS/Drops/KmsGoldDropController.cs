using UnityEngine;

namespace KMS
{
    [DisallowMultipleComponent]
    public sealed class KmsGoldDropController : MonoBehaviour
    {
        public const int MinimumDropCount = 1;
        public const int MaximumDropCount = 5;

        [Header("References")]
        [SerializeField] private KmsMonsterSpawner monsterSpawner;
        [SerializeField] private KmsGoldPickup goldPickupPrefab;

        [Header("Scatter")]
        [SerializeField, Min(0f)] private float minimumScatterDistance = 0.65f;
        [SerializeField, Min(0f)] private float maximumScatterDistance = 1.35f;
        [SerializeField, Range(0f, 45f)] private float angleJitter = 10f;

        private bool isSubscribed;

        public int LastDropCount { get; private set; }
        public int TotalSpawnedPickupCount { get; private set; }

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

        public void Configure(KmsMonsterSpawner spawner, KmsGoldPickup pickupPrefab)
        {
            monsterSpawner = spawner;
            goldPickupPrefab = pickupPrefab;
        }

        public static int SelectDropCount(float unitRoll)
        {
            float clampedRoll = Mathf.Clamp(unitRoll, 0f, 0.99999994f);
            int zeroBasedIndex = Mathf.FloorToInt(clampedRoll * MaximumDropCount);
            return Mathf.Clamp(zeroBasedIndex + MinimumDropCount, MinimumDropCount, MaximumDropCount);
        }

        private void SubscribeToSpawner()
        {
            if (isSubscribed || monsterSpawner == null)
            {
                return;
            }

            monsterSpawner.MonsterDied += HandleMonsterDied;
            isSubscribed = true;
        }

        private void UnsubscribeFromSpawner()
        {
            if (!isSubscribed || monsterSpawner == null)
            {
                return;
            }

            monsterSpawner.MonsterDied -= HandleMonsterDied;
            isSubscribed = false;
        }

        private void HandleMonsterDied(KmsMonster monster)
        {
            if (goldPickupPrefab == null)
            {
                Debug.LogError("[KMS] 1골드 픽업 프리팹 참조가 없습니다.", this);
                return;
            }

            int dropCount = SelectDropCount(Random.value);
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

                KmsGoldPickup pickup = Instantiate(
                    goldPickupPrefab,
                    origin,
                    Quaternion.identity,
                    transform);
                pickup.name = $"KmsGoldPickup_{TotalSpawnedPickupCount + 1:000}";
                pickup.Launch(origin, direction * distance);
                TotalSpawnedPickupCount++;
            }
        }
    }
}
