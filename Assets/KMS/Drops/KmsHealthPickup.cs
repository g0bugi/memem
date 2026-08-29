using TMPro;
using UnityEngine;

namespace KMS
{
    [DisallowMultipleComponent]
    public sealed class KmsHealthPickup : MonoBehaviour
    {
        public const float DefaultMaxHealthFraction = 0.20f;

        private const float FlashDuration = 0.18f;
        private const float PopupPopDuration = 0.12f;
        private const float PopupSettleDuration = 0.06f;
        private const float PopupHoldDuration = 0.15f;
        private const float PopupFadeDuration = 0.25f;
        private const float PopupVerticalOffset = 0.72f;
        private const float PopupBaseScale = 0.11f;
        private static readonly Color RecoveryTextColor = new Color(1f, 0.22f, 0.42f, 1f);
        private static readonly Color RecoveryOutlineColor = new Color(0.35f, 0.015f, 0.06f, 1f);

        private static Sprite whiteFlashSprite;
        private static Texture2D whiteFlashTexture;

        [Header("Presentation")]
        [SerializeField] private Transform visualRoot;
        [SerializeField, Min(0.05f)] private float scatterDuration = 0.34f;
        [SerializeField, Min(0f)] private float hopHeight = 0.6f;
        [SerializeField] private float spinSpeed = 420f;
        [SerializeField, Min(0.05f)] private float collectionRadius = 0.45f;

        [Header("Recovery")]
        [SerializeField, Range(0.01f, 1f)]
        private float maxHealthFraction = DefaultMaxHealthFraction;

        private Vector3 startPosition;
        private Vector3 landingPosition;
        private Vector3 visualBaseLocalPosition;
        private Vector3 visualBaseLocalScale;
        private Quaternion visualBaseLocalRotation;
        private SpriteRenderer visualRenderer;
        private Color visualBaseColor;
        private SpriteRenderer flashRenderer;
        private TextMeshPro recoveryText;
        private float scatterElapsed;
        private float collectionElapsed;
        private bool isScattering;
        private bool isCollected;

        public bool IsCollectible => !isScattering && !isCollected;
        public float MaxHealthFraction => maxHealthFraction;
        public bool IsPlayingCollectionFeedback => isCollected;
        public bool IsWhiteFlashVisible => flashRenderer != null
            && flashRenderer.enabled
            && flashRenderer.color.a > 0f;
        public bool IsRecoveryPopupVisible => recoveryText != null
            && recoveryText.gameObject.activeSelf
            && recoveryText.color.a > 0f;
        public string RecoveryPopupText => recoveryText != null ? recoveryText.text : string.Empty;
        public Vector3 RecoveryPopupWorldPosition => recoveryText != null
            ? recoveryText.transform.position
            : transform.position;

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
            visualRenderer = visualRoot.GetComponentInChildren<SpriteRenderer>(true);
            visualBaseColor = visualRenderer != null ? visualRenderer.color : Color.white;

            CreateWhiteFlashOverlay();
            CreateRecoveryPopup();
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
                return UpdateCollectionFeedback(deltaTime);
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

            collector.Heal(CalculateHealAmount(collector.MaxHealth, maxHealthFraction));
            BeginCollectionFeedback();
            return false;
        }

        internal void ResetForPool()
        {
            scatterElapsed = 0f;
            collectionElapsed = 0f;
            isScattering = false;
            isCollected = false;
            ResetVisual();
            ResetCollectionFeedback();
        }

        public static float CalculateHealAmount(float maximumHealth, float configuredFraction)
        {
            if (maximumHealth <= 0f)
            {
                return 0f;
            }

            return maximumHealth * Mathf.Clamp01(configuredFraction);
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

            float scale = normalizedTime < 0.65f
                ? Mathf.LerpUnclamped(0.15f, 1.2f, EaseOutCubic(normalizedTime / 0.65f))
                : Mathf.Lerp(1.2f, 1f, (normalizedTime - 0.65f) / 0.35f);
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

        private void BeginCollectionFeedback()
        {
            isCollected = true;
            collectionElapsed = 0f;

            if (flashRenderer != null)
            {
                flashRenderer.enabled = true;
                flashRenderer.color = Color.white;
            }

            if (recoveryText != null)
            {
                recoveryText.text = $"+{Mathf.RoundToInt(Mathf.Clamp01(maxHealthFraction) * 100f)}%";
                recoveryText.color = RecoveryTextColor;
                recoveryText.transform.localPosition = Vector3.up * PopupVerticalOffset;
                recoveryText.transform.localRotation = Quaternion.identity;
                recoveryText.transform.localScale = Vector3.one * PopupBaseScale * 0.3f;
                recoveryText.gameObject.SetActive(true);
            }
        }

        private bool UpdateCollectionFeedback(float deltaTime)
        {
            collectionElapsed += Mathf.Max(0f, deltaTime);
            UpdateWhiteFlash();
            UpdateRecoveryPopup();

            float popupDuration = PopupPopDuration
                + PopupSettleDuration
                + PopupHoldDuration
                + PopupFadeDuration;
            return collectionElapsed >= Mathf.Max(FlashDuration, popupDuration);
        }

        private void UpdateWhiteFlash()
        {
            if (collectionElapsed >= FlashDuration)
            {
                if (flashRenderer != null)
                {
                    flashRenderer.enabled = false;
                }

                if (visualRenderer != null)
                {
                    visualRenderer.enabled = false;
                }

                return;
            }

            float normalizedTime = Mathf.Clamp01(collectionElapsed / FlashDuration);
            float flashAlpha = 1f - normalizedTime;
            float flashScale = Mathf.Lerp(1f, 1.42f, EaseOutCubic(normalizedTime));

            if (flashRenderer != null)
            {
                flashRenderer.enabled = true;
                flashRenderer.color = new Color(1f, 1f, 1f, flashAlpha);
                flashRenderer.transform.localScale = Vector3.one * flashScale;
            }

            if (visualRenderer != null)
            {
                Color fadedColor = visualBaseColor;
                fadedColor.a *= 1f - Mathf.Clamp01(normalizedTime * 1.35f);
                visualRenderer.color = fadedColor;
            }
        }

        private void UpdateRecoveryPopup()
        {
            if (recoveryText == null)
            {
                return;
            }

            float scale;
            float alpha = 1f;
            float elapsed = collectionElapsed;

            if (elapsed < PopupPopDuration)
            {
                float progress = EaseOutCubic(Mathf.Clamp01(elapsed / PopupPopDuration));
                scale = Mathf.LerpUnclamped(0.3f, 1.2f, progress);
            }
            else if (elapsed < PopupPopDuration + PopupSettleDuration)
            {
                float progress = Mathf.Clamp01(
                    (elapsed - PopupPopDuration) / PopupSettleDuration);
                scale = Mathf.Lerp(1.2f, 1f, progress);
            }
            else if (elapsed < PopupPopDuration + PopupSettleDuration + PopupHoldDuration)
            {
                scale = 1f;
            }
            else
            {
                float fadeStart = PopupPopDuration + PopupSettleDuration + PopupHoldDuration;
                float progress = Mathf.Clamp01((elapsed - fadeStart) / PopupFadeDuration);
                scale = 1f - progress;
                alpha = 1f - progress;
            }

            Color popupColor = RecoveryTextColor;
            popupColor.a = alpha;
            recoveryText.color = popupColor;
            recoveryText.transform.localScale = Vector3.one * PopupBaseScale * scale;
        }

        private void CreateWhiteFlashOverlay()
        {
            if (visualRenderer == null || visualRenderer.sprite == null)
            {
                return;
            }

            GameObject flashObject = new GameObject("WhiteFlash");
            flashObject.transform.SetParent(visualRenderer.transform, false);
            flashRenderer = flashObject.AddComponent<SpriteRenderer>();
            flashRenderer.sprite = GetOrCreateWhiteFlashSprite(visualRenderer.sprite);
            flashRenderer.sortingLayerID = visualRenderer.sortingLayerID;
            flashRenderer.sortingOrder = visualRenderer.sortingOrder + 1;
            flashRenderer.enabled = false;
        }

        private void CreateRecoveryPopup()
        {
            GameObject popupObject = new GameObject("RecoveryPopup", typeof(RectTransform));
            popupObject.transform.SetParent(transform, false);
            recoveryText = popupObject.AddComponent<TextMeshPro>();
            recoveryText.text = "+20%";
            recoveryText.alignment = TextAlignmentOptions.Center;
            recoveryText.fontStyle = FontStyles.Bold;
            recoveryText.fontSize = 3.2f;
            recoveryText.enableWordWrapping = false;
            recoveryText.color = RecoveryTextColor;
            recoveryText.outlineColor = RecoveryOutlineColor;
            recoveryText.outlineWidth = 0.16f;
            recoveryText.rectTransform.sizeDelta = new Vector2(3f, 1f);
            recoveryText.renderer.sortingOrder = visualRenderer != null
                ? visualRenderer.sortingOrder + 10
                : 17;
            popupObject.transform.localPosition = Vector3.up * PopupVerticalOffset;
            popupObject.SetActive(false);
        }

        private static Sprite GetOrCreateWhiteFlashSprite(Sprite source)
        {
            if (whiteFlashSprite != null)
            {
                return whiteFlashSprite;
            }

            Rect sourceRect = source.rect;
            int width = Mathf.Max(1, Mathf.RoundToInt(sourceRect.width));
            int height = Mathf.Max(1, Mathf.RoundToInt(sourceRect.height));
            Color[] sourcePixels = source.texture.GetPixels(
                Mathf.RoundToInt(sourceRect.x),
                Mathf.RoundToInt(sourceRect.y),
                width,
                height);

            for (int index = 0; index < sourcePixels.Length; index++)
            {
                sourcePixels[index] = new Color(1f, 1f, 1f, sourcePixels[index].a);
            }

            whiteFlashTexture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                name = "KmsHealthPickupWhiteFlash_Runtime",
                filterMode = source.texture.filterMode,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave
            };
            whiteFlashTexture.SetPixels(sourcePixels);
            whiteFlashTexture.Apply(false, true);

            Vector2 normalizedPivot = new Vector2(
                source.pivot.x / sourceRect.width,
                source.pivot.y / sourceRect.height);
            whiteFlashSprite = Sprite.Create(
                whiteFlashTexture,
                new Rect(0f, 0f, width, height),
                normalizedPivot,
                source.pixelsPerUnit,
                0,
                SpriteMeshType.FullRect);
            whiteFlashSprite.name = "KmsHealthPickupWhiteFlash_Runtime";
            whiteFlashSprite.hideFlags = HideFlags.HideAndDontSave;
            return whiteFlashSprite;
        }

        private void ResetCollectionFeedback()
        {
            if (visualRenderer != null)
            {
                visualRenderer.enabled = true;
                visualRenderer.color = visualBaseColor;
            }

            if (flashRenderer != null)
            {
                flashRenderer.enabled = false;
                flashRenderer.color = Color.white;
                flashRenderer.transform.localScale = Vector3.one;
            }

            if (recoveryText != null)
            {
                recoveryText.gameObject.SetActive(false);
                recoveryText.color = RecoveryTextColor;
                recoveryText.transform.localPosition = Vector3.up * PopupVerticalOffset;
                recoveryText.transform.localScale = Vector3.one * PopupBaseScale;
            }
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
