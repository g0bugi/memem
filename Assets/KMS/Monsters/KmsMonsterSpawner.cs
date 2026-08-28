using System.Collections.Generic;
using UnityEngine;

namespace KMS
{
    [DisallowMultipleComponent]
    public sealed class KmsMonsterSpawner : MonoBehaviour
    {
        private const float GoldenAngleDegrees = 137.50776f;

        [Header("References")]
        [SerializeField] private KmsMonster monsterPrefab;
        [SerializeField] private Transform playerTarget;

        [Header("Initial Spawn Test")]
        [SerializeField, Min(1)] private int spawnCount = 1;
        [SerializeField, Min(0f)] private float innerSpawnRadius = 4f;
        [SerializeField, Min(0f)] private float outerSpawnRadius = 8f;
        [SerializeField] private bool spawnOnStart = true;

        private readonly List<KmsMonster> spawnedMonsters = new List<KmsMonster>();
        private bool hasSpawned;

        public int ConfiguredSpawnCount => spawnCount;
        public int SpawnedCount => spawnedMonsters.Count;
        public int ActiveCount { get; private set; }

        private void Start()
        {
            if (spawnOnStart)
            {
                SpawnConfiguredCount();
            }
        }

        private void OnDestroy()
        {
            foreach (KmsMonster monster in spawnedMonsters)
            {
                if (monster != null)
                {
                    monster.Died -= HandleMonsterDied;
                }
            }
        }

        [ContextMenu("Spawn Configured Count")]
        public void SpawnConfiguredCount()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("[KMS] 몬스터 생성은 Play Mode에서 실행해 주세요.", this);
                return;
            }

            if (hasSpawned)
            {
                Debug.LogWarning("[KMS] 이 테스트 스포너는 한 번만 생성합니다.", this);
                return;
            }

            if (!ResolveReferences())
            {
                return;
            }

            hasSpawned = true;
            int count = Mathf.Max(1, spawnCount);

            for (int index = 0; index < count; index++)
            {
                Vector3 spawnPosition = GetSpawnPosition(index, count);
                KmsMonster monster = Instantiate(monsterPrefab, spawnPosition, Quaternion.identity, transform);
                monster.name = $"{monsterPrefab.name}_{index + 1:000}";
                monster.Initialize(playerTarget);
                monster.Died += HandleMonsterDied;
                spawnedMonsters.Add(monster);
                ActiveCount++;
            }

            Debug.Log($"[KMS] Enemy {count}마리 생성 완료 (기본 테스트 값: {spawnCount}).", this);
        }

        public void Configure(KmsMonster prefab, Transform target, int initialSpawnCount)
        {
            monsterPrefab = prefab;
            playerTarget = target;
            spawnCount = Mathf.Max(1, initialSpawnCount);
        }

        private bool ResolveReferences()
        {
            if (monsterPrefab == null)
            {
                Debug.LogError("[KMS] Monster Prefab 참조가 없습니다.", this);
                return false;
            }

            if (playerTarget == null)
            {
                GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
                if (playerObject != null)
                {
                    playerTarget = playerObject.transform;
                }
            }

            if (playerTarget == null)
            {
                Debug.LogError("[KMS] Player 태그를 가진 대상을 찾을 수 없습니다.", this);
                return false;
            }

            return true;
        }

        private Vector3 GetSpawnPosition(int index, int count)
        {
            if (count == 1)
            {
                return transform.position;
            }

            float minimumRadius = Mathf.Min(innerSpawnRadius, outerSpawnRadius);
            float maximumRadius = Mathf.Max(innerSpawnRadius, outerSpawnRadius);
            float normalizedIndex = (index + 0.5f) / count;
            float radius = Mathf.Lerp(minimumRadius, maximumRadius, Mathf.Sqrt(normalizedIndex));
            float angleRadians = index * GoldenAngleDegrees * Mathf.Deg2Rad;
            Vector2 direction = new Vector2(Mathf.Cos(angleRadians), Mathf.Sin(angleRadians));
            return playerTarget.position + (Vector3)(direction * radius);
        }

        private void HandleMonsterDied(KmsMonster monster)
        {
            monster.Died -= HandleMonsterDied;
            ActiveCount = Mathf.Max(0, ActiveCount - 1);
        }

        private void OnDrawGizmosSelected()
        {
            Vector3 center = playerTarget != null ? playerTarget.position : Vector3.zero;
            Gizmos.color = new Color(1f, 0.4f, 0.2f, 0.7f);
            Gizmos.DrawWireSphere(center, Mathf.Max(innerSpawnRadius, outerSpawnRadius));
            Gizmos.color = new Color(1f, 0.8f, 0.2f, 0.7f);
            Gizmos.DrawWireSphere(center, Mathf.Min(innerSpawnRadius, outerSpawnRadius));
        }
    }
}
