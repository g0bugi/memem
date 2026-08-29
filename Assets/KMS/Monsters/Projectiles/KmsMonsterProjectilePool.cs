using System.Collections.Generic;
using UnityEngine;

namespace KMS
{
    [DisallowMultipleComponent]
    public sealed class KmsMonsterProjectilePool : MonoBehaviour
    {
        [Header("Known Projectile Prefabs")]
        [SerializeField] private KmsMonsterProjectile[] projectilePrefabs;

        [Header("Capacity Per Prefab")]
        [SerializeField, Min(0)] private int prewarmCount = 12;
        [Tooltip("프리팝(종류)별 동시에 활성화될 수 있는 최대 개수. 초과 시 가장 오래된 것부터 강제로 회수해 재사용한다.")]
        [SerializeField, Min(1)] private int hardCapacity = 500;

        private readonly Dictionary<KmsMonsterProjectile, PoolState> pools =
            new Dictionary<KmsMonsterProjectile, PoolState>();
        private readonly HashSet<KmsMonsterProjectile> activeProjectiles =
            new HashSet<KmsMonsterProjectile>();

        // 프리팝(종류)별 활성 투사체를 생성 순서대로 적재해둔다(맨 앞이 가장 오래된 것). hardCapacity를 넘어서 더
        // 만들 수 없을 때 이 순서를 보고 같은 프리팝 중 가장 오래된 것부터 강제로 회수한다.
        private readonly Dictionary<KmsMonsterProjectile, LinkedList<KmsMonsterProjectile>> activeOrderByPrefab =
            new Dictionary<KmsMonsterProjectile, LinkedList<KmsMonsterProjectile>>();
        private readonly Dictionary<KmsMonsterProjectile, LinkedListNode<KmsMonsterProjectile>> activeNodes =
            new Dictionary<KmsMonsterProjectile, LinkedListNode<KmsMonsterProjectile>>();

        public int ActiveCount => activeProjectiles.Count;
        public int TotalLaunchCount { get; private set; }

        public int TotalInstanceCount
        {
            get
            {
                int count = 0;
                foreach (PoolState state in pools.Values)
                {
                    count += state.CreatedCount;
                }

                return count;
            }
        }

        public int InactiveCount => Mathf.Max(0, TotalInstanceCount - ActiveCount);

        private void Awake()
        {
            EnsureConfiguredPools();
        }

        private void OnDestroy()
        {
            activeProjectiles.Clear();
            pools.Clear();
            activeOrderByPrefab.Clear();
            activeNodes.Clear();
        }

        public void Configure(KmsMonsterProjectile[] prefabs, int initialCount, int capacity)
        {
            projectilePrefabs = prefabs ?? System.Array.Empty<KmsMonsterProjectile>();
            prewarmCount = Mathf.Max(0, initialCount);
            hardCapacity = Mathf.Max(1, capacity);
        }

        public bool TryLaunch(
            KmsMonsterProjectile prefab,
            Vector2 position,
            Vector2 direction,
            float speed,
            float damage,
            float lifetime)
        {
            if (prefab == null)
            {
                return false;
            }

            PoolState state = GetOrCreatePool(prefab);
            KmsMonsterProjectile projectile = state.Acquire();
            if (projectile == null)
            {
                // 이 프리팝 타입의 풀이 hardCapacity에 도달해 더 만들 수 없는 상태다.
                // 같은 프리팝 타입 중 가장 오래된 활성 투사체를 강제로 회수하고 재시도한다.
                if (!TryForceEvictOldest(prefab))
                {
                    return false;
                }

                projectile = state.Acquire();
                if (projectile == null)
                {
                    return false;
                }
            }

            projectile.gameObject.SetActive(true);
            projectile.PrepareForLaunch(this, prefab, position, direction, speed, damage, lifetime);
            activeProjectiles.Add(projectile);
            TrackActive(prefab, projectile);
            TotalLaunchCount++;
            return true;
        }

        public void Return(KmsMonsterProjectile projectile)
        {
            if (projectile == null || !activeProjectiles.Remove(projectile))
            {
                return;
            }

            UntrackActive(projectile);

            KmsMonsterProjectile prefab = projectile.PrefabKey;
            projectile.PrepareForPool();
            projectile.gameObject.SetActive(false);

            if (prefab != null && pools.TryGetValue(prefab, out PoolState state))
            {
                state.Release(projectile);
                return;
            }

            Destroy(projectile.gameObject);
        }

private void TrackActive(KmsMonsterProjectile prefab, KmsMonsterProjectile instance)
        {
            if (!activeOrderByPrefab.TryGetValue(prefab, out LinkedList<KmsMonsterProjectile> order))
            {
                order = new LinkedList<KmsMonsterProjectile>();
                activeOrderByPrefab[prefab] = order;
            }

            LinkedListNode<KmsMonsterProjectile> node = order.AddLast(instance);
            activeNodes[instance] = node;
        }

        private void UntrackActive(KmsMonsterProjectile instance)
        {
            if (activeNodes.TryGetValue(instance, out LinkedListNode<KmsMonsterProjectile> node))
            {
                node.List.Remove(node);
                activeNodes.Remove(instance);
            }
        }

        /// <summary>같은 프리팝 타입의 활성 투사체 중 가장 오래된 것을 강제로 풀에 반환한다.</summary>
        private bool TryForceEvictOldest(KmsMonsterProjectile prefab)
        {
            if (!activeOrderByPrefab.TryGetValue(prefab, out LinkedList<KmsMonsterProjectile> order) || order.Count == 0)
            {
                return false;
            }

            KmsMonsterProjectile oldest = order.First.Value;
            Return(oldest);
            return true;
        }


        public void DespawnAll()
        {
            if (activeProjectiles.Count == 0)
            {
                return;
            }

            KmsMonsterProjectile[] snapshot = new KmsMonsterProjectile[activeProjectiles.Count];
            activeProjectiles.CopyTo(snapshot);
            foreach (KmsMonsterProjectile projectile in snapshot)
            {
                Return(projectile);
            }
        }

        private void EnsureConfiguredPools()
        {
            if (projectilePrefabs == null)
            {
                return;
            }

            foreach (KmsMonsterProjectile prefab in projectilePrefabs)
            {
                if (prefab != null)
                {
                    GetOrCreatePool(prefab);
                }
            }
        }

        private PoolState GetOrCreatePool(KmsMonsterProjectile prefab)
        {
            if (pools.TryGetValue(prefab, out PoolState existing))
            {
                return existing;
            }

            PoolState created = new PoolState(
                prefab,
                transform,
                Mathf.Min(prewarmCount, hardCapacity),
                hardCapacity);
            pools.Add(prefab, created);
            return created;
        }

        private sealed class PoolState
        {
            private readonly KmsMonsterProjectile prefab;
            private readonly Transform parent;
            private readonly int capacity;
            private readonly Stack<KmsMonsterProjectile> inactive = new Stack<KmsMonsterProjectile>();

            public PoolState(KmsMonsterProjectile source, Transform poolParent, int initialCount, int hardLimit)
            {
                prefab = source;
                parent = poolParent;
                capacity = Mathf.Max(1, hardLimit);

                for (int index = 0; index < initialCount; index++)
                {
                    KmsMonsterProjectile projectile = CreateInstance();
                    if (projectile != null)
                    {
                        inactive.Push(projectile);
                    }
                }
            }

            public int CreatedCount { get; private set; }

            public KmsMonsterProjectile Acquire()
            {
                while (inactive.Count > 0)
                {
                    KmsMonsterProjectile instance = inactive.Pop();
                    if (instance != null)
                    {
                        return instance;
                    }

                    CreatedCount = Mathf.Max(0, CreatedCount - 1);
                }

                return CreatedCount < capacity ? CreateInstance() : null;
            }

            public void Release(KmsMonsterProjectile projectile)
            {
                if (projectile != null)
                {
                    inactive.Push(projectile);
                }
            }

            private KmsMonsterProjectile CreateInstance()
            {
                KmsMonsterProjectile instance = Object.Instantiate(prefab, parent);
                instance.name = $"{prefab.name}_Pooled_{CreatedCount + 1:000}";
                instance.PrepareForPool();
                instance.gameObject.SetActive(false);
                CreatedCount++;
                return instance;
            }
        }
    }
}
