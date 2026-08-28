using System;
using UnityEngine;

namespace KMS
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
    public sealed class KmsMonster : MonoBehaviour, IDamageable
    {
        [Header("Presentation")]
        [SerializeField] private Transform visualRoot;
        [SerializeField] private SpriteRenderer visualRenderer;

        [Header("Projectile Origin")]
        [SerializeField] private Transform projectileSpawnPoint;

        [Header("Fallback Feedback")]
        [SerializeField, Min(0f)] private float hitFlashDuration = 0.08f;
        [SerializeField] private Color hitFlashColor = Color.white;

        [Header("Health Bar")]
        [SerializeField] private SpriteRenderer healthBarBackground;
        [SerializeField] private SpriteRenderer healthBarFill;
        [SerializeField, Min(0.1f)] private float healthBarVisibleDuration = 1.25f;
        [SerializeField, Min(0.01f)] private float healthBarFullWidth = 0.8f;

        private Rigidbody2D body;
        private Collider2D bodyCollider;
        private Sprite fallbackSprite;
        private Color fallbackColor;
        private Vector3 fallbackVisualScale;
        private Color baseColor;

        private KmsMonsterData monsterData;
        private KmsMonsterProjectilePool projectilePool;
        private Transform playerTarget;
        private Collider2D playerCollider;
        private PlayerStats playerStats;

        private float currentHealth;
        private float attackCooldownRemaining;
        private float hitFlashRemaining;
        private float healthBarVisibleRemaining;
        private bool isPrepared;
        private bool isDead;
        private bool warnedMissingProjectilePool;

        public event Action<KmsMonster> Died;
        internal event Action<KmsMonster> DeathCompleted;
        internal event Action<KmsMonster> UnexpectedlyDisabled;

        public KmsMonsterData Data => monsterData;
        public float CurrentHealth => currentHealth;
        public float MaxHealth => monsterData != null ? monsterData.MaxHealth : 0f;
        public bool IsDead => isDead;
        public bool IsPrepared => isPrepared;

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            bodyCollider = GetComponent<Collider2D>();

            if (visualRenderer == null)
            {
                visualRenderer = GetComponent<SpriteRenderer>();
            }

            if (visualRoot == null && visualRenderer != null)
            {
                visualRoot = visualRenderer.transform;
            }

            if (projectileSpawnPoint == null)
            {
                projectileSpawnPoint = transform;
            }

            if (visualRenderer != null)
            {
                fallbackSprite = visualRenderer.sprite;
                fallbackColor = visualRenderer.color;
                baseColor = fallbackColor;
            }

            fallbackVisualScale = visualRoot != null ? visualRoot.localScale : Vector3.one;
        }

        private void OnEnable()
        {
            if (!isPrepared)
            {
                if (body != null)
                {
                    body.linearVelocity = Vector2.zero;
                }

                if (bodyCollider != null)
                {
                    bodyCollider.enabled = false;
                }
            }
        }

        private void OnDisable()
        {
            bool notifyUnexpectedDisable = isPrepared && !isDead;

            if (body != null)
            {
                body.linearVelocity = Vector2.zero;
                body.angularVelocity = 0f;
            }

            HideHealthBar();

            if (notifyUnexpectedDisable)
            {
                InvokeHandlersSafely(UnexpectedlyDisabled);
            }
        }

        private void FixedUpdate()
        {
            if (!isPrepared || monsterData == null)
            {
                if (body != null)
                {
                    body.linearVelocity = Vector2.zero;
                }

                return;
            }

            float deltaTime = Time.fixedDeltaTime;
            attackCooldownRemaining = Mathf.Max(0f, attackCooldownRemaining - deltaTime);
            UpdateHitFlash(deltaTime);
            UpdateHealthBarVisibility(deltaTime);

            if (isDead)
            {
                body.linearVelocity = Vector2.zero;
                return;
            }

            switch (monsterData.BehaviorType)
            {
                case KmsMonsterBehaviorType.ChaseContact:
                    UpdateChaseContactBehavior();
                    break;

                case KmsMonsterBehaviorType.KeepDistanceProjectile:
                    UpdateKeepDistanceProjectileBehavior();
                    break;

                default:
                    body.linearVelocity = Vector2.zero;
                    break;
            }
        }

        public void PrepareForSpawn(
            KmsMonsterData data,
            Transform target,
            Vector3 spawnPosition,
            KmsMonsterProjectilePool monsterProjectilePool)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            if (body == null || bodyCollider == null)
            {
                body = GetComponent<Rigidbody2D>();
                bodyCollider = GetComponent<Collider2D>();
            }

            monsterData = data;
            projectilePool = monsterProjectilePool;
            playerTarget = target;
            playerCollider = target != null ? target.GetComponent<Collider2D>() : null;
            playerStats = target != null ? target.GetComponent<PlayerStats>() : null;

            transform.position = spawnPosition;
            body.position = spawnPosition;
            body.linearVelocity = Vector2.zero;
            body.angularVelocity = 0f;

            ApplyPresentation(data);
            currentHealth = data.MaxHealth;
            attackCooldownRemaining = 0f;
            hitFlashRemaining = 0f;
            healthBarVisibleRemaining = 0f;
            warnedMissingProjectilePool = false;
            isDead = false;
            isPrepared = true;
            Died = null;
            DeathCompleted = null;
            UnexpectedlyDisabled = null;

            bodyCollider.enabled = true;
            UpdateHealthBarFill();
            HideHealthBar();
        }

        public void PrepareForPool()
        {
            StopAllCoroutines();

            isPrepared = false;
            isDead = false;
            warnedMissingProjectilePool = false;
            currentHealth = 0f;
            attackCooldownRemaining = 0f;
            hitFlashRemaining = 0f;
            healthBarVisibleRemaining = 0f;

            if (body != null)
            {
                body.linearVelocity = Vector2.zero;
                body.angularVelocity = 0f;
            }

            if (bodyCollider != null)
            {
                bodyCollider.enabled = false;
            }

            if (visualRenderer != null)
            {
                visualRenderer.color = fallbackColor;
                visualRenderer.sprite = fallbackSprite;
            }

            if (visualRoot != null)
            {
                visualRoot.localScale = fallbackVisualScale;
            }

            HideHealthBar();
            monsterData = null;
            projectilePool = null;
            playerTarget = null;
            playerCollider = null;
            playerStats = null;
            Died = null;
            DeathCompleted = null;
            UnexpectedlyDisabled = null;
        }

        public void TakeDamage(float amount)
        {
            if (!isPrepared || isDead || amount <= 0f)
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

        private void UpdateChaseContactBehavior()
        {
            if (!HasValidTarget())
            {
                body.linearVelocity = Vector2.zero;
                return;
            }

            ColliderDistance2D distance = bodyCollider.Distance(playerCollider);
            bool isTouching = distance.isOverlapped || distance.distance <= monsterData.AttackRange;

            if (!isTouching)
            {
                MoveToward(playerTarget.position);
                return;
            }

            body.linearVelocity = Vector2.zero;
            if (attackCooldownRemaining <= 0f && playerStats != null)
            {
                playerStats.TakeDamage(monsterData.AttackDamage);
                attackCooldownRemaining = monsterData.AttackCooldown;
            }
        }

        private void UpdateKeepDistanceProjectileBehavior()
        {
            if (!HasValidTarget())
            {
                body.linearVelocity = Vector2.zero;
                return;
            }

            Vector2 offset = (Vector2)playerTarget.position - body.position;
            float distance = offset.magnitude;
            float minimumPreferredDistance = Mathf.Max(
                0f,
                monsterData.PreferredDistance - monsterData.DistanceTolerance);
            float maximumPreferredDistance =
                monsterData.PreferredDistance + monsterData.DistanceTolerance;

            if (distance > maximumPreferredDistance)
            {
                body.linearVelocity = offset.sqrMagnitude > 0f
                    ? offset.normalized * monsterData.MoveSpeed
                    : Vector2.zero;
            }
            else if (distance < minimumPreferredDistance)
            {
                body.linearVelocity = offset.sqrMagnitude > 0f
                    ? -offset.normalized * monsterData.MoveSpeed
                    : Vector2.zero;
            }
            else
            {
                body.linearVelocity = Vector2.zero;
            }

            if (distance <= monsterData.AttackRange && attackCooldownRemaining <= 0f)
            {
                TryFireProjectile(offset);
            }
        }

        private bool HasValidTarget()
        {
            return playerTarget != null &&
                playerTarget.gameObject.activeInHierarchy &&
                playerCollider != null &&
                playerCollider.enabled &&
                playerStats != null &&
                playerStats.CurrentHealth > 0f;
        }

        private void MoveToward(Vector3 position)
        {
            Vector2 offset = (Vector2)position - body.position;
            body.linearVelocity = offset.sqrMagnitude > 0f
                ? offset.normalized * monsterData.MoveSpeed
                : Vector2.zero;
        }

        private void TryFireProjectile(Vector2 direction)
        {
            attackCooldownRemaining = monsterData.AttackCooldown;

            if (monsterData.ProjectilePrefab == null)
            {
                return;
            }

            if (projectilePool == null)
            {
                if (!warnedMissingProjectilePool)
                {
                    warnedMissingProjectilePool = true;
                    Debug.LogError($"[KMS] {monsterData.DisplayName}에 적 투사체 풀이 연결되지 않았습니다.", this);
                }

                return;
            }

            Vector2 normalizedDirection = direction.sqrMagnitude > 0f
                ? direction.normalized
                : Vector2.right;
            float spawnOffset = projectileSpawnPoint != null
                ? Vector2.Distance(transform.position, projectileSpawnPoint.position)
                : 0.55f;
            Vector2 origin = body.position + (normalizedDirection * spawnOffset);

            projectilePool.TryLaunch(
                monsterData.ProjectilePrefab,
                origin,
                normalizedDirection,
                monsterData.ProjectileSpeed,
                monsterData.AttackDamage,
                monsterData.ProjectileLifetime);
        }

        private void ApplyPresentation(KmsMonsterData data)
        {
            if (visualRenderer != null)
            {
                visualRenderer.sprite = data.Sprite != null ? data.Sprite : fallbackSprite;
                visualRenderer.color = data.Color;
                baseColor = data.Color;
            }

            if (visualRoot != null)
            {
                visualRoot.localScale = fallbackVisualScale * data.VisualScale;
            }
        }

        private void ApplyHitFeedback()
        {
            float duration = monsterData != null ? monsterData.HitFlashDuration : hitFlashDuration;
            Color color = monsterData != null ? monsterData.HitFlashColor : hitFlashColor;
            hitFlashRemaining = duration;

            if (visualRenderer != null)
            {
                visualRenderer.color = color;
            }
        }

        private void UpdateHitFlash(float deltaTime)
        {
            if (hitFlashRemaining <= 0f)
            {
                return;
            }

            hitFlashRemaining = Mathf.Max(0f, hitFlashRemaining - deltaTime);
            if (hitFlashRemaining <= 0f && visualRenderer != null)
            {
                visualRenderer.color = baseColor;
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
            Action<KmsMonster> completionHandlers = DeathCompleted;
            InvokeHandlersSafely(diedHandlers);

            if (completionHandlers != null)
            {
                InvokeHandlersSafely(completionHandlers);
            }
            else
            {
                PrepareForPool();
                gameObject.SetActive(false);
            }
        }

        private void InvokeHandlersSafely(Action<KmsMonster> handlers)
        {
            if (handlers == null)
            {
                return;
            }

            foreach (Delegate handler in handlers.GetInvocationList())
            {
                try
                {
                    ((Action<KmsMonster>)handler).Invoke(this);
                }
                catch (Exception exception)
                {
                    Debug.LogException(exception, this);
                }
            }
        }

        private void ShowHealthBarAfterDamage()
        {
            UpdateHealthBarFill();

            if (currentHealth <= 0f || currentHealth >= MaxHealth)
            {
                HideHealthBar();
                return;
            }

            healthBarVisibleRemaining = monsterData != null
                ? monsterData.HealthBarVisibleDuration
                : healthBarVisibleDuration;
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

            float normalizedHealth = MaxHealth > 0f
                ? Mathf.Clamp01(currentHealth / MaxHealth)
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
