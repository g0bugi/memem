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
        [SerializeField, Min(1)] private int hardCapacity = 96;

        private readonly Dictionary<KmsMonsterProjectile, PoolState> pools =
            new Dictionary<KmsMonsterProjectile, PoolState>();
        private readonly HashSet<KmsMonsterProjectile> activeProjectiles =
            new HashSet<KmsMonsterProjectile>();

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
                return false;
            }

            projectile.gameObject.SetActive(true);
            projectile.PrepareForLaunch(this, prefab, position, direction, speed, damage, lifetime);
            activeProjectiles.Add(projectile);
            TotalLaunchCount++;
            return true;
        }

        public void Return(KmsMonsterProjectile projectile)
        {
            if (projectile == null || !activeProjectiles.Remove(projectile))
            {
                return;
            }

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
