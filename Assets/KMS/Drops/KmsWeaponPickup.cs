using System.Collections.Generic;
using UnityEngine;

namespace KMS
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider2D))]
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class KmsWeaponPickup : MonoBehaviour
    {
        [Header("Presentation")]
        [SerializeField] private Transform visualRoot;
        [SerializeField] private SpriteRenderer visualRenderer;
        [SerializeField, Min(0.05f)] private float scatterDuration = 0.38f;
        [SerializeField, Min(0f)] private float hopHeight = 0.7f;
        [SerializeField] private float spinSpeed = 400f;
        [SerializeField, Min(0.05f)] private float collectionRadius = 0.55f;

        [Header("Runtime Item")]
        [SerializeField] private string weaponId;
        [SerializeField] private ItemGrade grade = ItemGrade.Common;

        private Collider2D pickupCollider;
        private WeaponInventory weaponInventory;
        private Vector3 startPosition;
        private Vector3 landingPosition;
        private Vector3 visualBaseLocalPosition;
        private Vector3 visualBaseLocalScale;
        private float scatterElapsed;
        private bool isScattering;
        private bool isCollected;
        private bool acquisitionBlocked;

        public string WeaponId => weaponId;
        public ItemGrade Grade => grade;
        public bool IsCollectible => !isScattering && !isCollected && !acquisitionBlocked;

        private void Awake()
        {
            pickupCollider = GetComponent<Collider2D>();
            pickupCollider.isTrigger = true;

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

        private void FixedUpdate()
        {
            if (isScattering || isCollected || string.IsNullOrWhiteSpace(weaponId))
            {
                return;
            }

            if (weaponInventory == null)
            {
                weaponInventory = FindFirstObjectByType<WeaponInventory>();
            }

            if (weaponInventory == null)
            {
                return;
            }

            float radius = Mathf.Max(0.05f, collectionRadius);
            Vector2 difference = weaponInventory.transform.position - transform.position;
            if (acquisitionBlocked)
            {
                Collider2D inventoryCollider = weaponInventory.GetComponent<Collider2D>();
                bool isTouchingInventory = inventoryCollider != null
                    && pickupCollider.IsTouching(inventoryCollider);
                if (!isTouchingInventory && difference.sqrMagnitude > radius * radius)
                {
                    acquisitionBlocked = false;
                }

                return;
            }

            if (difference.sqrMagnitude <= radius * radius)
            {
                TryAcquire(weaponInventory);
            }
        }

        public void Initialize(string selectedWeaponId, ItemGrade selectedGrade, Vector3 origin, Vector2 scatterOffset)
        {
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
            pickupCollider.enabled = false;

            if (visualRoot != transform)
            {
                visualRoot.localPosition = visualBaseLocalPosition;
            }

            visualRoot.localScale = visualBaseLocalScale * 0.15f;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            TryAcquire(other.GetComponentInParent<WeaponInventory>());
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            TryAcquire(other.GetComponentInParent<WeaponInventory>());
        }

private void TryAcquire(WeaponInventory inventory)
{
    if (!IsCollectible || inventory == null)
    {
        return;
    }

    // 무기는 중복 획득을 허용한다(별도 슬롯 추가 방식). 이미 보유한 무기라도
    // 다시 주우면 WeaponInventory에 새 인스턴스가 추가되어 HUD에 슬롯이 하나 더 생긴다.
    int ownedCountBefore = inventory.ActiveWeapons.Count;
    inventory.AcquireWeapon(weaponId);
    bool acquired = inventory.ActiveWeapons.Count > ownedCountBefore;

    if (!acquired)
    {
        acquisitionBlocked = true;
        Debug.LogWarning(
            $"[KMS] 무기 ID '{weaponId}' 획득이 거부되었습니다. 플레이어가 거리를 벗어나면 다시 시도합니다.",
            this);
        return;
    }

    weaponInventory = inventory;
    isCollected = true;
    pickupCollider.enabled = false;
    Destroy(gameObject);
}

        private void AnimateVisual(float normalizedTime)
        {
            float hopOffset = Mathf.Sin(normalizedTime * Mathf.PI) * hopHeight;
            if (visualRoot != transform)
            {
                visualRoot.localPosition = visualBaseLocalPosition + (Vector3.up * hopOffset);
            }

            visualRoot.Rotate(0f, 0f, spinSpeed * Time.deltaTime);

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
            pickupCollider.enabled = true;
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
