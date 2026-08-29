using UnityEngine;

namespace KMS
{
    [DisallowMultipleComponent]
    public sealed class KmsHealthDropController : MonoBehaviour
    {
        public const float DefaultHealthDropChance = 0.01f;

        [Header("Drop Rule")]
        [SerializeField, Range(0f, 1f)]
        private float healthDropChance = DefaultHealthDropChance;

        [Header("Scatter")]
        [SerializeField, Min(0f)] private float minimumScatterDistance = 0.55f;
        [SerializeField, Min(0f)] private float maximumScatterDistance = 1f;

        private KmsMonsterSpawner monsterSpawner;
        private KmsPickupManager pickupManager;
        private bool isSubscribed;

        public float HealthDropChance => healthDropChance;
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

        public static bool ShouldDrop(float unitRoll, float configuredChance)
        {
            float chance = Mathf.Clamp01(configuredChance);
            if (chance <= 0f)
            {
                return false;
            }

            return chance >= 1f || Mathf.Clamp01(unitRoll) < chance;
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
            if (!ShouldDrop(Random.value, healthDropChance))
            {
                return;
            }

            ResolveReferences();
            if (pickupManager == null)
            {
                Debug.LogError("[KMS] 체력 회복 픽업을 관리할 KmsPickupManager가 없습니다.", this);
                return;
            }

            float minimumDistance = Mathf.Min(minimumScatterDistance, maximumScatterDistance);
            float maximumDistance = Mathf.Max(minimumScatterDistance, maximumScatterDistance);
            float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            float distance = Random.Range(minimumDistance, maximumDistance);
            Vector2 scatterOffset = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * distance;

            if (pickupManager.SpawnHealth(monster.transform.position, scatterOffset))
            {
                TotalSpawnedPickupCount++;
            }
        }
    }
}
