using UnityEngine;

namespace KMS
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody2D), typeof(Collider2D), typeof(SpriteRenderer))]
    public sealed class KmsMonsterProjectile : MonoBehaviour
    {
        private Rigidbody2D body;
        private Collider2D bodyCollider;
        private SpriteRenderer spriteRenderer;
        private KmsMonsterProjectilePool ownerPool;
        private KmsMonsterProjectile prefabKey;
        private float damage;
        private float lifetimeRemaining;
        private bool isActiveProjectile;

        public KmsMonsterProjectile PrefabKey => prefabKey;
        public bool IsActiveProjectile => isActiveProjectile;

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            bodyCollider = GetComponent<Collider2D>();
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        private void Update()
        {
            if (!isActiveProjectile)
            {
                return;
            }

            lifetimeRemaining -= Time.deltaTime;
            if (lifetimeRemaining <= 0f)
            {
                Release();
            }
        }

        private void OnDisable()
        {
            if (!isActiveProjectile)
            {
                return;
            }

            KmsMonsterProjectilePool pool = ownerPool;
            if (pool != null)
            {
                pool.Return(this);
            }
            else
            {
                PrepareForPool();
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!isActiveProjectile || other == null)
            {
                return;
            }

            if (other.GetComponentInParent<KmsMonster>() != null)
            {
                return;
            }

            // 플레이어 투사체와는 서로 무시한다 (충돌 시 소멸하지 않도록).
            if (other.GetComponentInParent<global::Projectile>() != null)
            {
                return;
            }

            PlayerStats playerStats = other.GetComponentInParent<PlayerStats>();
            if (playerStats != null)
            {
                playerStats.TakeDamage(damage);
                Release();
                return;
            }

            if (!other.isTrigger)
            {
                Release();
            }
        }

        internal void PrepareForLaunch(
            KmsMonsterProjectilePool pool,
            KmsMonsterProjectile sourcePrefab,
            Vector2 position,
            Vector2 direction,
            float speed,
            float launchDamage,
            float lifetime)
        {
            ownerPool = pool;
            prefabKey = sourcePrefab;
            damage = Mathf.Max(0f, launchDamage);
            lifetimeRemaining = Mathf.Max(0.05f, lifetime);
            isActiveProjectile = true;

            transform.position = position;
            transform.rotation = direction.sqrMagnitude > 0f
                ? Quaternion.FromToRotation(Vector3.right, direction.normalized)
                : Quaternion.identity;

            if (body == null)
            {
                body = GetComponent<Rigidbody2D>();
            }

            if (bodyCollider == null)
            {
                bodyCollider = GetComponent<Collider2D>();
            }

            bodyCollider.enabled = true;
            body.WakeUp();
            body.linearVelocity = direction.sqrMagnitude > 0f
                ? direction.normalized * Mathf.Max(0f, speed)
                : Vector2.zero;
        }

        internal void PrepareForPool()
        {
            isActiveProjectile = false;
            damage = 0f;
            lifetimeRemaining = 0f;

            if (body != null)
            {
                body.linearVelocity = Vector2.zero;
                body.angularVelocity = 0f;
            }

            if (bodyCollider != null)
            {
                bodyCollider.enabled = false;
            }

            ownerPool = null;
        }

        private void Release()
        {
            if (!isActiveProjectile)
            {
                return;
            }

            KmsMonsterProjectilePool pool = ownerPool;
            if (pool != null)
            {
                pool.Return(this);
                return;
            }

            PrepareForPool();
            gameObject.SetActive(false);
        }
    }
}
