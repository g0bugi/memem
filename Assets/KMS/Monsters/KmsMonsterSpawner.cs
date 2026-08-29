using System;
using System.Collections.Generic;
using UnityEngine;

namespace KMS
{
    [DisallowMultipleComponent]
    public sealed class KmsMonsterSpawner : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private KmsMonsterData[] knownMonsterData;
        [SerializeField] private Transform playerTarget;
        [SerializeField] private Collider2D spawnArea;
        [SerializeField] private KmsMonsterProjectilePool projectilePool;

        [Header("Spawn Position")]
        [SerializeField, Min(0f)] private float innerSpawnRadius = 4f;
        [SerializeField, Min(0f)] private float outerSpawnRadius = 8f;
        [SerializeField, Min(1)] private int positionAttemptCount = 12;
        [SerializeField, Min(0f)] private float spawnClearanceRadius = 0.55f;

        [Header("Pool Capacity Per Prefab")]
        [SerializeField, Min(0)] private int prewarmCount = 12;
        [SerializeField, Min(1)] private int hardCapacityPerPrefab = 360;
        [SerializeField, Min(1)] private int absoluteMaxActive = 360;

        [Header("Optional Initial Test Spawn")]
        [SerializeField, Min(1)] private int spawnCount = 1;
        [SerializeField] private bool spawnOnStart;

        private readonly Dictionary<KmsMonster, KmsMonsterPool> pools =
            new Dictionary<KmsMonster, KmsMonsterPool>();
        private readonly Dictionary<KmsMonster, KmsMonsterPool> poolByInstance =
            new Dictionary<KmsMonster, KmsMonsterPool>();
        private readonly HashSet<KmsMonster> activeMonsters = new HashSet<KmsMonster>();
        private bool hasInitialSpawned;
        private bool warnedMissingTarget;

        public event Action<KmsMonster> MonsterDied;

        public int ConfiguredSpawnCount => spawnCount;
        public int SpawnedCount { get; private set; }
        public int ActiveCount => activeMonsters.Count;
        public int AbsoluteMaxActive => Mathf.Max(1, absoluteMaxActive);

        public int TotalPooledInstanceCount
        {
            get
            {
                int count = 0;
                foreach (KmsMonsterPool pool in pools.Values)
                {
                    count += pool.CreatedCount;
                }

                return count;
            }
        }

        public int InactivePooledCount
        {
            get
            {
                int count = 0;
                foreach (KmsMonsterPool pool in pools.Values)
                {
                    count += pool.InactiveCount;
                }

                return count;
            }
        }

        private void Awake()
        {
            ResolveTarget();
            EnsureKnownPools();
        }

        private void Start()
        {
            if (spawnOnStart)
            {
                SpawnConfiguredCount();
            }
        }

        private void OnDestroy()
        {
            if (activeMonsters.Count > 0)
            {
                KmsMonster[] snapshot = new KmsMonster[activeMonsters.Count];
                activeMonsters.CopyTo(snapshot);
                foreach (KmsMonster monster in snapshot)
                {
                    if (monster != null)
                    {
                        monster.DeathCompleted -= HandleMonsterDied;
                        monster.UnexpectedlyDisabled -= HandleMonsterUnexpectedlyDisabled;
                    }
                }
            }

            activeMonsters.Clear();
            poolByInstance.Clear();
            pools.Clear();
        }

        public void Configure(
            KmsMonsterData[] monsters,
            Transform target,
            Collider2D validSpawnArea,
            KmsMonsterProjectilePool monsterProjectilePool,
            bool spawnInitialTestMonster = false)
        {
            knownMonsterData = monsters ?? Array.Empty<KmsMonsterData>();
            playerTarget = target;
            spawnArea = validSpawnArea;
            projectilePool = monsterProjectilePool;
            spawnOnStart = spawnInitialTestMonster;
        }

        [ContextMenu("Spawn Configured Count")]
        public void SpawnConfiguredCount()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("[KMS] 몬스터 생성은 Play Mode에서 실행해 주세요.", this);
                return;
            }

            if (hasInitialSpawned)
            {
                Debug.LogWarning("[KMS] 초기 테스트 생성은 한 번만 실행합니다.", this);
                return;
            }

            if (knownMonsterData == null || knownMonsterData.Length == 0 || knownMonsterData[0] == null)
            {
                Debug.LogError("[KMS] 초기 테스트 생성에 사용할 MonsterData가 없습니다.", this);
                return;
            }

            hasInitialSpawned = true;
            int count = Mathf.Max(1, spawnCount);
            for (int index = 0; index < count; index++)
            {
                TrySpawn(knownMonsterData[0]);
            }
        }

        public bool TrySpawn(KmsMonsterData data)
        {
            return TrySpawn(data, out _);
        }

        internal bool TrySpawn(KmsMonsterData data, out KmsMonster spawnedMonster)
        {
            spawnedMonster = null;
            if (!TryGetSpawnPosition(out Vector3 position))
            {
                return false;
            }

            return TrySpawnAt(data, position, out spawnedMonster);
        }

        public bool TrySpawnAt(KmsMonsterData data, Vector3 position)
        {
            return TrySpawnAt(data, position, out _);
        }

        private bool TrySpawnAt(
            KmsMonsterData data,
            Vector3 position,
            out KmsMonster spawnedMonster)
        {
            spawnedMonster = null;
            if (data == null)
            {
                Debug.LogError("[KMS] MonsterData가 없는 생성 요청을 받았습니다.", this);
                return false;
            }

            if (!data.TryValidate(out string validationError))
            {
                Debug.LogError($"[KMS] 유효하지 않은 MonsterData 생성 요청: {validationError}", this);
                return false;
            }

            if (!ResolveTarget())
            {
                return false;
            }

            if (ActiveCount >= AbsoluteMaxActive)
            {
                return false;
            }

            KmsMonsterPool pool = GetOrCreatePool(data.Prefab);
            KmsMonster monster = pool.Acquire();
            if (monster == null)
            {
                return false;
            }

            monster.PrepareForSpawn(data, playerTarget, position, projectilePool);
            monster.DeathCompleted += HandleMonsterDied;
            monster.UnexpectedlyDisabled += HandleMonsterUnexpectedlyDisabled;
            poolByInstance[monster] = pool;
            activeMonsters.Add(monster);
            SpawnedCount++;
            monster.name = $"{data.MonsterId}_{SpawnedCount:000}";
            monster.gameObject.SetActive(true);
            spawnedMonster = monster;
            return true;
        }

        public void DespawnAll()
        {
            if (activeMonsters.Count > 0)
            {
                KmsMonster[] snapshot = new KmsMonster[activeMonsters.Count];
                activeMonsters.CopyTo(snapshot);
                foreach (KmsMonster monster in snapshot)
                {
                    ReturnToPool(monster);
                }
            }

            projectilePool?.DespawnAll();
        }

        public int GetActiveCount(KmsMonsterBehaviorType behaviorType)
        {
            int count = 0;
            foreach (KmsMonster monster in activeMonsters)
            {
                if (monster != null && monster.Data != null && monster.Data.BehaviorType == behaviorType)
                {
                    count++;
                }
            }

            return count;
        }

        private void EnsureKnownPools()
        {
            if (knownMonsterData == null)
            {
                return;
            }

            foreach (KmsMonsterData data in knownMonsterData)
            {
                if (data != null && data.Prefab != null)
                {
                    GetOrCreatePool(data.Prefab);
                }
            }
        }

        private KmsMonsterPool GetOrCreatePool(KmsMonster prefab)
        {
            if (pools.TryGetValue(prefab, out KmsMonsterPool existing))
            {
                return existing;
            }

            KmsMonsterPool created = new KmsMonsterPool(
                prefab,
                transform,
                Mathf.Min(prewarmCount, hardCapacityPerPrefab),
                hardCapacityPerPrefab);
            pools.Add(prefab, created);
            return created;
        }

        private bool ResolveTarget()
        {
            if (playerTarget == null)
            {
                GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
                if (playerObject != null)
                {
                    playerTarget = playerObject.transform;
                }
            }

            if (playerTarget != null)
            {
                warnedMissingTarget = false;
                return true;
            }

            if (!warnedMissingTarget)
            {
                warnedMissingTarget = true;
                Debug.LogError("[KMS] Player 태그를 가진 생성 대상을 찾을 수 없습니다.", this);
            }

            return false;
        }

        private bool TryGetSpawnPosition(out Vector3 position)
        {
            if (!ResolveTarget())
            {
                position = transform.position;
                return false;
            }

            float minimumRadius = Mathf.Min(innerSpawnRadius, outerSpawnRadius);
            float maximumRadius = Mathf.Max(innerSpawnRadius, outerSpawnRadius);
            int attempts = Mathf.Max(1, positionAttemptCount);

            for (int attempt = 0; attempt < attempts; attempt++)
            {
                float angle = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
                float radius = Mathf.Sqrt(UnityEngine.Random.value) *
                    (maximumRadius - minimumRadius) + minimumRadius;
                Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                Vector3 candidate = playerTarget.position + (Vector3)(direction * radius);

                if (IsSpawnPositionValid(candidate))
                {
                    position = candidate;
                    return true;
                }
            }

            position = transform.position;
            return false;
        }

        private bool IsSpawnPositionValid(Vector3 candidate)
        {
            float clearance = Mathf.Max(0f, spawnClearanceRadius);
            if (spawnArea != null)
            {
                Bounds bounds = spawnArea.bounds;
                if (candidate.x - clearance < bounds.min.x ||
                    candidate.x + clearance > bounds.max.x ||
                    candidate.y - clearance < bounds.min.y ||
                    candidate.y + clearance > bounds.max.y ||
                    !spawnArea.OverlapPoint(candidate))
                {
                    return false;
                }
            }

            Collider2D[] overlaps = Physics2D.OverlapCircleAll(candidate, clearance);
            foreach (Collider2D overlap in overlaps)
            {
                if (overlap == null || overlap == spawnArea || overlap.isTrigger ||
                    !overlap.enabled || !overlap.gameObject.activeInHierarchy)
                {
                    continue;
                }

                return false;
            }

            return true;
        }

        private void HandleMonsterDied(KmsMonster monster)
        {
            if (monster == null || !activeMonsters.Remove(monster))
            {
                return;
            }

            monster.DeathCompleted -= HandleMonsterDied;
            monster.UnexpectedlyDisabled -= HandleMonsterUnexpectedlyDisabled;
            try
            {
                InvokeMonsterDiedHandlersSafely(monster);
            }
            finally
            {
                ReleasePreparedMonster(monster);
            }
        }

        private void ReturnToPool(KmsMonster monster)
        {
            if (monster == null || !activeMonsters.Remove(monster))
            {
                return;
            }

            monster.DeathCompleted -= HandleMonsterDied;
            monster.UnexpectedlyDisabled -= HandleMonsterUnexpectedlyDisabled;
            ReleasePreparedMonster(monster);
        }

        private void HandleMonsterUnexpectedlyDisabled(KmsMonster monster)
        {
            if (monster == null || !activeMonsters.Remove(monster))
            {
                return;
            }

            monster.DeathCompleted -= HandleMonsterDied;
            monster.UnexpectedlyDisabled -= HandleMonsterUnexpectedlyDisabled;
            ReleasePreparedMonster(monster);
        }

        private void InvokeMonsterDiedHandlersSafely(KmsMonster monster)
        {
            Action<KmsMonster> handlers = MonsterDied;
            if (handlers == null)
            {
                return;
            }

            foreach (Delegate handler in handlers.GetInvocationList())
            {
                try
                {
                    ((Action<KmsMonster>)handler).Invoke(monster);
                }
                catch (Exception exception)
                {
                    Debug.LogException(exception, this);
                }
            }
        }

        private void ReleasePreparedMonster(KmsMonster monster)
        {
            if (!poolByInstance.TryGetValue(monster, out KmsMonsterPool pool))
            {
                monster.PrepareForPool();
                monster.gameObject.SetActive(false);
                Destroy(monster.gameObject);
                return;
            }

            poolByInstance.Remove(monster);
            monster.PrepareForPool();
            monster.gameObject.SetActive(false);
            pool.Release(monster);
        }

        private void OnDrawGizmosSelected()
        {
            Vector3 center = playerTarget != null ? playerTarget.position : transform.position;
            Gizmos.color = new Color(1f, 0.4f, 0.2f, 0.7f);
            Gizmos.DrawWireSphere(center, Mathf.Max(innerSpawnRadius, outerSpawnRadius));
            Gizmos.color = new Color(1f, 0.8f, 0.2f, 0.7f);
            Gizmos.DrawWireSphere(center, Mathf.Min(innerSpawnRadius, outerSpawnRadius));
        }
    }
}
