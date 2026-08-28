using System;
using UnityEngine;

namespace KMS
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody2D), typeof(Collider2D), typeof(SpriteRenderer))]
    public sealed class KmsMonster : MonoBehaviour, IDamageable
    {
        [Header("Stats")]
        [SerializeField, Min(0.01f)] private float maxHealth = 30f;
        [SerializeField, Min(0f)] private float moveSpeed = 2f;
        [SerializeField, Min(0f)] private float contactDamage = 5f;
        [SerializeField, Min(0.05f)] private float attackCooldown = 1f;
        [SerializeField, Min(0f)] private float contactTolerance = 0.02f;

        [Header("Hit Feedback")]
        [SerializeField, Min(0f)] private float hitFlashDuration = 0.08f;
        [SerializeField] private Color hitFlashColor = Color.white;

        [Header("Health Bar")]
        [SerializeField] private SpriteRenderer healthBarBackground;
        [SerializeField] private SpriteRenderer healthBarFill;
        [SerializeField, Min(0.1f)] private float healthBarVisibleDuration = 1.25f;
        [SerializeField, Min(0.01f)] private float healthBarFullWidth = 0.8f;

        private Rigidbody2D body;
        private Collider2D bodyCollider;
        private SpriteRenderer spriteRenderer;
        private Color baseColor;

        private Transform playerTarget;
        private Collider2D playerCollider;
        private PlayerStats playerStats;

        private float currentHealth;
        private float attackCooldownRemaining;
        private float hitFlashRemaining;
        private float healthBarVisibleRemaining;
        private bool isDead;

        public event Action<KmsMonster> Died;

        public float CurrentHealth => currentHealth;
        public float MaxHealth => maxHealth;
        public bool IsDead => isDead;

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            bodyCollider = GetComponent<Collider2D>();
            spriteRenderer = GetComponent<SpriteRenderer>();
            baseColor = spriteRenderer.color;
        }

        private void OnEnable()
        {
            ResetRuntimeState();
        }

        private void Start()
        {
            if (playerTarget != null)
            {
                return;
            }

            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject != null)
            {
                Initialize(playerObject.transform);
            }
        }

        private void OnDisable()
        {
            if (body != null)
            {
                body.linearVelocity = Vector2.zero;
            }

            HideHealthBar();
        }

        private void FixedUpdate()
        {
            float deltaTime = Time.fixedDeltaTime;
            attackCooldownRemaining = Mathf.Max(0f, attackCooldownRemaining - deltaTime);
            UpdateHitFlash(deltaTime);
            UpdateHealthBarVisibility(deltaTime);

            if (isDead)
            {
                body.linearVelocity = Vector2.zero;
                return;
            }

            ChaseAndAttack();
        }

        public void Initialize(Transform target)
        {
            playerTarget = target;
            playerCollider = target != null ? target.GetComponent<Collider2D>() : null;
            playerStats = target != null ? target.GetComponent<PlayerStats>() : null;
        }

        public void TakeDamage(float amount)
        {
            if (isDead || amount <= 0f)
            {
                return;
            }

            currentHealth = Mathf.Max(0f, currentHealth - amount);
            ApplyHitFeedback();
            ShowHealthBarAfterDamage();

            if (currentHealth <= 0f)
            {
                Die();
            }
        }

        private void ChaseAndAttack()
        {
            if (playerTarget == null || playerCollider == null || !playerCollider.enabled)
            {
                body.linearVelocity = Vector2.zero;
                return;
            }

            ColliderDistance2D distance = bodyCollider.Distance(playerCollider);
            bool isTouching = distance.isOverlapped || distance.distance <= contactTolerance;

            if (!isTouching)
            {
                Vector2 offset = (Vector2)playerTarget.position - body.position;
                body.linearVelocity = offset.sqrMagnitude > 0f
                    ? offset.normalized * moveSpeed
                    : Vector2.zero;
                return;
            }

            body.linearVelocity = Vector2.zero;

            if (attackCooldownRemaining <= 0f && playerStats != null)
            {
                playerStats.TakeDamage(contactDamage);
                attackCooldownRemaining = attackCooldown;
            }
        }

        private void ApplyHitFeedback()
        {
            hitFlashRemaining = hitFlashDuration;
            spriteRenderer.color = hitFlashColor;
        }

        private void UpdateHitFlash(float deltaTime)
        {
            if (hitFlashRemaining <= 0f)
            {
                return;
            }

            hitFlashRemaining = Mathf.Max(0f, hitFlashRemaining - deltaTime);
            if (hitFlashRemaining <= 0f)
            {
                spriteRenderer.color = baseColor;
            }
        }

        private void Die()
        {
            if (isDead)
            {
                return;
            }

            isDead = true;
            body.linearVelocity = Vector2.zero;
            bodyCollider.enabled = false;
            HideHealthBar();

            Action<KmsMonster> diedHandlers = Died;
            Died = null;
            diedHandlers?.Invoke(this);
            gameObject.SetActive(false);
        }

        private void ResetRuntimeState()
        {
            currentHealth = maxHealth;
            attackCooldownRemaining = 0f;
            hitFlashRemaining = 0f;
            healthBarVisibleRemaining = 0f;
            isDead = false;

            if (bodyCollider != null)
            {
                bodyCollider.enabled = true;
            }

            if (spriteRenderer != null)
            {
                spriteRenderer.color = baseColor;
            }

            UpdateHealthBarFill();
            HideHealthBar();
        }

        private void ShowHealthBarAfterDamage()
        {
            UpdateHealthBarFill();

            if (currentHealth <= 0f || currentHealth >= maxHealth)
            {
                HideHealthBar();
                return;
            }

            healthBarVisibleRemaining = healthBarVisibleDuration;
            SetHealthBarVisible(true);
        }

        private void UpdateHealthBarVisibility(float deltaTime)
        {
            if (healthBarVisibleRemaining <= 0f)
            {
                return;
            }

            healthBarVisibleRemaining = Mathf.Max(0f, healthBarVisibleRemaining - deltaTime);
            if (healthBarVisibleRemaining <= 0f)
            {
                HideHealthBar();
            }
        }

        private void UpdateHealthBarFill()
        {
            if (healthBarFill == null)
            {
                return;
            }

            float normalizedHealth = maxHealth > 0f
                ? Mathf.Clamp01(currentHealth / maxHealth)
                : 0f;
            float currentWidth = healthBarFullWidth * normalizedHealth;
            Transform fillTransform = healthBarFill.transform;
            Vector3 scale = fillTransform.localScale;
            scale.x = currentWidth;
            fillTransform.localScale = scale;

            Vector3 position = fillTransform.localPosition;
            position.x = (currentWidth - healthBarFullWidth) * 0.5f;
            fillTransform.localPosition = position;
        }

        private void HideHealthBar()
        {
            healthBarVisibleRemaining = 0f;
            SetHealthBarVisible(false);
        }

        private void SetHealthBarVisible(bool visible)
        {
            if (healthBarBackground != null)
            {
                healthBarBackground.enabled = visible;
            }

            if (healthBarFill != null)
            {
                healthBarFill.enabled = visible;
            }
        }
    }
}
