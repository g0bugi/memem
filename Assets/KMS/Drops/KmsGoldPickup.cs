using UnityEngine;

namespace KMS
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider2D))]
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class KmsGoldPickup : MonoBehaviour
    {
        public const int GoldValue = 1;

        [Header("Presentation")]
        [SerializeField] private Transform visualRoot;
        [SerializeField, Min(0.05f)] private float scatterDuration = 0.32f;
        [SerializeField, Min(0f)] private float hopHeight = 0.55f;
        [SerializeField] private float spinSpeed = 540f;
        [SerializeField, Min(0.05f)] private float collectionRadius = 0.4f;

        private Collider2D pickupCollider;
        private Vector3 startPosition;
        private Vector3 landingPosition;
        private Vector3 visualBaseLocalPosition;
        private Vector3 visualBaseLocalScale;
        private float scatterElapsed;
        private bool isScattering;
        private bool isCollected;
        private PlayerStats playerStats;

        public bool IsCollectible => !isScattering && !isCollected;

        private void Awake()
        {
            pickupCollider = GetComponent<Collider2D>();
            pickupCollider.isTrigger = true;

            if (visualRoot == null)
            {
                visualRoot = transform;
            }

            visualBaseLocalPosition = visualRoot == transform
                ? Vector3.zero
                : visualRoot.localPosition;
            visualBaseLocalScale = visualRoot.localScale;
        }

        private void Update()
        {
            if (!isScattering)
            {
                return;
            }

            scatterElapsed += Time.deltaTime;
            float duration = Mathf.Max(0.05f, scatterDuration);
            float normalizedTime = Mathf.Clamp01(scatterElapsed / duration);
            float moveProgress = EaseOutCubic(normalizedTime);

            transform.position = Vector3.LerpUnclamped(startPosition, landingPosition, moveProgress);
            AnimateVisual(normalizedTime);

            if (normalizedTime >= 1f)
            {
                FinishScatter();
            }
        }

        public void Launch(Vector3 origin, Vector2 scatterOffset)
        {
            startPosition = origin;
            landingPosition = origin + (Vector3)scatterOffset;
            scatterElapsed = 0f;
            isScattering = true;
            isCollected = false;

            transform.position = origin;
            pickupCollider.enabled = false;

            if (visualRoot != transform)
            {
                visualRoot.localPosition = visualBaseLocalPosition;
            }

            visualRoot.localScale = visualBaseLocalScale * 0.15f;
        }

        private void AnimateVisual(float normalizedTime)
        {
            float hopOffset = Mathf.Sin(normalizedTime * Mathf.PI) * hopHeight;
            if (visualRoot != transform)
            {
                visualRoot.localPosition = visualBaseLocalPosition + (Vector3.up * hopOffset);
            }

            visualRoot.Rotate(0f, 0f, spinSpeed * Time.deltaTime);

            float scale;
            if (normalizedTime < 0.65f)
            {
                float popProgress = EaseOutCubic(normalizedTime / 0.65f);
                scale = Mathf.LerpUnclamped(0.15f, 1.2f, popProgress);
            }
            else
            {
                float settleProgress = (normalizedTime - 0.65f) / 0.35f;
                scale = Mathf.Lerp(1.2f, 1f, settleProgress);
            }

            visualRoot.localScale = visualBaseLocalScale * scale;
        }

        private void FinishScatter()
        {
            transform.position = landingPosition;
            isScattering = false;

            if (visualRoot != transform)
            {
                visualRoot.localPosition = visualBaseLocalPosition;
            }

            visualRoot.localScale = visualBaseLocalScale;
            pickupCollider.enabled = true;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            TryCollect(other);
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            TryCollect(other);
        }

        private void FixedUpdate()
        {
            if (!IsCollectible)
            {
                return;
            }

            if (playerStats == null)
            {
                playerStats = FindFirstObjectByType<PlayerStats>();
            }

            if (playerStats == null)
            {
                return;
            }

            float radius = Mathf.Max(0.05f, collectionRadius);
            Vector2 difference = playerStats.transform.position - transform.position;
            if (difference.sqrMagnitude <= radius * radius)
            {
                Collect(playerStats);
            }
        }

        private void TryCollect(Collider2D other)
        {
            if (!IsCollectible)
            {
                return;
            }

            PlayerStats playerStats = other.GetComponentInParent<PlayerStats>();
            if (playerStats == null)
            {
                return;
            }

            Collect(playerStats);
        }

        private void Collect(PlayerStats collectedBy)
        {
            isCollected = true;
            pickupCollider.enabled = false;
            collectedBy.AddGold(GoldValue);
            Destroy(gameObject);
        }

        private static float EaseOutCubic(float value)
        {
            float inverse = 1f - Mathf.Clamp01(value);
            return 1f - (inverse * inverse * inverse);
        }
    }
}
