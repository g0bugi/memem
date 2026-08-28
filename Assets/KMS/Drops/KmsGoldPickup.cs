using UnityEngine;

namespace KMS
{
    [DisallowMultipleComponent]
    public sealed class KmsGoldPickup : MonoBehaviour
    {
        public const int GoldValue = 1;

        [Header("Presentation")]
        [SerializeField] private Transform visualRoot;
        [SerializeField, Min(0.05f)] private float scatterDuration = 0.32f;
        [SerializeField, Min(0f)] private float hopHeight = 0.55f;
        [SerializeField] private float spinSpeed = 540f;
        [SerializeField, Min(0.05f)] private float collectionRadius = 0.4f;

        private Vector3 startPosition;
        private Vector3 landingPosition;
        private Vector3 visualBaseLocalPosition;
        private Vector3 visualBaseLocalScale;
        private Quaternion visualBaseLocalRotation;
        private float scatterElapsed;
        private bool isScattering;
        private bool isCollected;

        public bool IsCollectible => !isScattering && !isCollected;

        private void Awake()
        {
            if (visualRoot == null)
            {
                visualRoot = transform;
            }

            visualBaseLocalPosition = visualRoot == transform
                ? Vector3.zero
                : visualRoot.localPosition;
            visualBaseLocalScale = visualRoot.localScale;
            visualBaseLocalRotation = visualRoot.localRotation;
        }

        public void Launch(Vector3 origin, Vector2 scatterOffset)
        {
            ResetVisual();
            startPosition = origin;
            landingPosition = origin + (Vector3)scatterOffset;
            scatterElapsed = 0f;
            isScattering = true;
            isCollected = false;

            transform.position = origin;
            visualRoot.localScale = visualBaseLocalScale * 0.15f;
        }

        internal bool Tick(float deltaTime, PlayerStats collector)
        {
            if (isCollected)
            {
                return true;
            }

            if (isScattering)
            {
                UpdateScatter(deltaTime);
                return false;
            }

            if (collector == null)
            {
                return false;
            }

            float radius = Mathf.Max(0.05f, collectionRadius);
            Vector2 difference = collector.transform.position - transform.position;
            if (difference.sqrMagnitude > radius * radius)
            {
                return false;
            }

            isCollected = true;
            collector.AddGold(GoldValue);
            return true;
        }

        internal void ResetForPool()
        {
            scatterElapsed = 0f;
            isScattering = false;
            isCollected = false;
            ResetVisual();
        }

        private void UpdateScatter(float deltaTime)
        {
            scatterElapsed += deltaTime;
            float duration = Mathf.Max(0.05f, scatterDuration);
            float normalizedTime = Mathf.Clamp01(scatterElapsed / duration);
            float moveProgress = EaseOutCubic(normalizedTime);

            transform.position = Vector3.LerpUnclamped(startPosition, landingPosition, moveProgress);
            AnimateVisual(normalizedTime, deltaTime);

            if (normalizedTime >= 1f)
            {
                FinishScatter();
            }
        }

        private void AnimateVisual(float normalizedTime, float deltaTime)
        {
            float hopOffset = Mathf.Sin(normalizedTime * Mathf.PI) * hopHeight;
            if (visualRoot != transform)
            {
                visualRoot.localPosition = visualBaseLocalPosition + (Vector3.up * hopOffset);
            }

            visualRoot.Rotate(0f, 0f, spinSpeed * deltaTime);

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
        }

        private void ResetVisual()
        {
            if (visualRoot == null)
            {
                return;
            }

            if (visualRoot != transform)
            {
                visualRoot.localPosition = visualBaseLocalPosition;
            }

            visualRoot.localRotation = visualBaseLocalRotation;
            visualRoot.localScale = visualBaseLocalScale;
        }

        private static float EaseOutCubic(float value)
        {
            float inverse = 1f - Mathf.Clamp01(value);
            return 1f - (inverse * inverse * inverse);
        }
    }
}
