using UnityEngine;

namespace KMS
{
    [CreateAssetMenu(fileName = "KmsMonsterData", menuName = "KMS/Monsters/Monster Data")]
    public sealed class KmsMonsterData : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string monsterId = "monster";
        [SerializeField] private string displayName = "Monster";

        [Header("Runtime Structure")]
        [SerializeField] private KmsMonsterBehaviorType behaviorType = KmsMonsterBehaviorType.ChaseContact;
        [SerializeField] private KmsMonster prefab;

        [Header("Common Stats")]
        [SerializeField, Min(0.01f)] private float maxHealth = 30f;
        [SerializeField, Min(0f)] private float moveSpeed = 2f;

        [Header("Attack")]
        [SerializeField, Min(0f)] private float attackDamage = 5f;
        [SerializeField, Min(0.05f)] private float attackCooldown = 1f;
        [SerializeField, Min(0f)] private float attackRange = 0.02f;

        [Header("Ranged Movement")]
        [SerializeField, Min(0f)] private float preferredDistance = 5f;
        [SerializeField, Min(0f)] private float distanceTolerance = 0.5f;

        [Header("Projectile")]
        [SerializeField] private KmsMonsterProjectile projectilePrefab;
        [SerializeField, Min(0f)] private float projectileSpeed = 6f;
        [SerializeField, Min(0.05f)] private float projectileLifetime = 4f;

        [Header("Presentation")]
        [SerializeField] private Sprite sprite;
        [SerializeField] private Color color = Color.white;
        [SerializeField, Min(0.05f)] private float visualScale = 1f;
        [SerializeField, Min(0f)] private float hitFlashDuration = 0.08f;
        [SerializeField] private Color hitFlashColor = Color.white;
        [SerializeField, Min(0.1f)] private float healthBarVisibleDuration = 1.25f;

        public string MonsterId => monsterId;
        public string DisplayName => displayName;
        public KmsMonsterBehaviorType BehaviorType => behaviorType;
        public KmsMonster Prefab => prefab;
        public float MaxHealth => maxHealth;
        public float MoveSpeed => moveSpeed;
        public float AttackDamage => attackDamage;
        public float AttackCooldown => attackCooldown;
        public float AttackRange => attackRange;
        public float PreferredDistance => preferredDistance;
        public float DistanceTolerance => distanceTolerance;
        public KmsMonsterProjectile ProjectilePrefab => projectilePrefab;
        public float ProjectileSpeed => projectileSpeed;
        public float ProjectileLifetime => projectileLifetime;
        public Sprite Sprite => sprite;
        public Color Color => color;
        public float VisualScale => visualScale;
        public float HitFlashDuration => hitFlashDuration;
        public Color HitFlashColor => hitFlashColor;
        public float HealthBarVisibleDuration => healthBarVisibleDuration;

        public bool TryValidate(out string error)
        {
            if (string.IsNullOrWhiteSpace(monsterId))
            {
                error = $"{name}: monsterId가 비어 있습니다.";
                return false;
            }

            if (prefab == null)
            {
                error = $"{name}: 몬스터 프리팹 참조가 없습니다.";
                return false;
            }

            if (behaviorType == KmsMonsterBehaviorType.KeepDistanceProjectile && projectilePrefab == null)
            {
                error = $"{name}: 원거리 행동에 필요한 투사체 프리팹 참조가 없습니다.";
                return false;
            }

            error = string.Empty;
            return true;
        }

        private void OnValidate()
        {
            maxHealth = Mathf.Max(0.01f, maxHealth);
            moveSpeed = Mathf.Max(0f, moveSpeed);
            attackDamage = Mathf.Max(0f, attackDamage);
            attackCooldown = Mathf.Max(0.05f, attackCooldown);
            attackRange = Mathf.Max(0f, attackRange);
            preferredDistance = Mathf.Max(0f, preferredDistance);
            distanceTolerance = Mathf.Max(0f, distanceTolerance);
            projectileSpeed = Mathf.Max(0f, projectileSpeed);
            projectileLifetime = Mathf.Max(0.05f, projectileLifetime);
            visualScale = Mathf.Max(0.05f, visualScale);
            hitFlashDuration = Mathf.Max(0f, hitFlashDuration);
            healthBarVisibleDuration = Mathf.Max(0.1f, healthBarVisibleDuration);
        }
    }
}
