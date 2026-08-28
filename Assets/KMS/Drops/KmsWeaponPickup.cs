using System.Collections.Generic;
using UnityEngine;

namespace KMS
{
    [DisallowMultipleComponent]
    public sealed class KmsWeaponPickup : MonoBehaviour
    {
        [Header("Presentation")]
        [SerializeField] private Transform visualRoot;
        [SerializeField] private SpriteRenderer visualRenderer;
        [SerializeField, Min(0.05f)] private float scatterDuration = 0.38f;
        [SerializeField, Min(0f)] private float hopHeight = 0.7f;
        [SerializeField] private float spinSpeed = 400f;
        [SerializeField, Min(0.05f)] private float collectionRadius = 0.55f;
        [SerializeField, Min(0f)] private float retryResetMargin = 0.25f;

        [Header("Runtime Item")]
        [SerializeField] private string weaponId;
        [SerializeField] private ItemGrade grade = ItemGrade.Common;

        private Vector3 startPosition;
        private Vector3 landingPosition;
        private Vector3 visualBaseLocalPosition;
        private Vector3 visualBaseLocalScale;
        private Quaternion visualBaseLocalRotation;
        private float scatterElapsed;
        private bool isScattering;
        private bool isCollected;
        private bool acquisitionBlocked;

        public string WeaponId => weaponId;
        public ItemGrade Grade => grade;
        public bool IsCollectible => !isScattering && !isCollected && !acquisitionBlocked;

        private void Awake()
        {
            if (visualRoot == null)
            {
                visualRoot = transform;
            }

            if (visualRenderer == null)
            {
                visualRenderer = GetComponentInChildren<SpriteRenderer>();
            }

            visualBaseLocalPosition = visualRoot == transform
                ? Vector3.zero
                : visualRoot.localPosition;
            visualBaseLocalScale = visualRoot.localScale;
            visualBaseLocalRotation = visualRoot.localRotation;
        }

        public void Initialize(string selectedWeaponId, ItemGrade selectedGrade, Vector3 origin, Vector2 scatterOffset)
        {
            ResetVisual();
            weaponId = selectedWeaponId;
            grade = selectedGrade;
            acquisitionBlocked = string.IsNullOrWhiteSpace(weaponId);
            ApplyGradeColor();

            startPosition = origin;
            landingPosition = origin + (Vector3)scatterOffset;
            scatterElapsed = 0f;
            isScattering = true;
            isCollected = false;

            transform.position = origin;
            visualRoot.localScale = visualBaseLocalScale * 0.15f;
        }

        internal bool Tick(float deltaTime, WeaponInventory inventory)
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

            if (inventory == null || string.IsNullOrWhiteSpace(weaponId))
            {
                return false;
            }

            float radius = Mathf.Max(0.05f, collectionRadius);
            Vector2 difference = inventory.transform.position - transform.position;
            if (acquisitionBlocked)
            {
                float resetRadius = radius + Mathf.Max(0f, retryResetMargin);
                if (difference.sqrMagnitude > resetRadius * resetRadius)
                {
                    acquisitionBlocked = false;
                }

                return false;
            }

            return difference.sqrMagnitude <= radius * radius && TryAcquire(inventory);
        }

        internal void ResetForPool()
        {
            weaponId = string.Empty;
            grade = ItemGrade.Common;
            scatterElapsed = 0f;
            isScattering = false;
            isCollected = false;
            acquisitionBlocked = false;
            ApplyGradeColor();
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

        private bool TryAcquire(WeaponInventory inventory)
{
    if (!IsCollectible || inventory == null)
    {
        return false;
    }

    // 같은 무기도 별도 슬롯으로 중복 획득한다.
    int ownedCountBefore = inventory.ActiveWeapons.Count;
    inventory.AcquireWeapon(weaponId);

    if (inventory.ActiveWeapons.Count <= ownedCountBefore)
    {
        acquisitionBlocked = true;
        Debug.LogWarning(
            $"[KMS] 무기 ID '{weaponId}' 획득이 거부되었습니다. 플레이어가 수집 반경을 벗어나면 다시 시도합니다.",
            this);
        return false;
    }

    isCollected = true;
    return true;
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
                ? Mathf.LerpUnclamped(0.15f, 1.25f, EaseOutCubic(normalizedTime / 0.65f))
                : Mathf.Lerp(1.25f, 1f, (normalizedTime - 0.65f) / 0.35f);
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

        private void ApplyGradeColor()
        {
            if (visualRenderer == null)
            {
                return;
            }

            visualRenderer.color = grade switch
            {
                ItemGrade.Legendary => new Color(1f, 0.2f, 0.75f, 1f),
                ItemGrade.Rare => new Color(0.2f, 0.55f, 1f, 1f),
                _ => new Color(0.45f, 1f, 0.55f, 1f)
            };
        }



        private static float EaseOutCubic(float value)
        {
            float inverse = 1f - Mathf.Clamp01(value);
            return 1f - (inverse * inverse * inverse);
        }
    }
}
