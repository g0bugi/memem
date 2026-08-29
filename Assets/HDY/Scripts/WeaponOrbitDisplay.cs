using System.Collections.Generic;
using UnityEngine;

namespace HDY
{
    /// <summary>
    /// 획득한 무기(시작무기 제외)의 아이콘을 캐릭터 주위에 Y축 궤도로 표시하는 순수 비주얼 컴포넌트.
    /// 콜라이더/리지드바디 없이 SpriteRenderer만 붙은 자식 오브젝트를 회전시킨다. 실제 전투 판정을
    /// 갖는 Orbit 타입 무기(OrbitWeaponController)와는 완전히 별개의 장식용 시스템이다.
    ///
    /// Y축 회전(캐릭터 앞뒤로 지나가는 효과)을 2D 화면에서 표현하기 위해, 좌우 위치는 sin(각도)로만
    /// 움직이고(세로 위치는 고정), 앞/뒤 여부는 cos(각도) 부호로 판단해서 캐릭터 기준 SpriteRenderer의
    /// sortingOrder와 아이콘 크기를 함께 바꾼다(뒤로 갈수록 작아지고 캐릭터보다 뒤에 그려짐).
    /// </summary>
    [DisallowMultipleComponent]
    public class WeaponOrbitDisplay : MonoBehaviour
    {
        private class OrbitIcon
        {
            public Transform Transform;
            public SpriteRenderer Renderer;
            public float BaseScale;
        }

        [Header("Anchor")]
        [Tooltip("아이콘들을 자식으로 붙일 앵커. 비워두면 런타임에 자동으로 하나 생성한다. " +
            "좌우 반전(CharacterFacingFlip)되는 파츠의 자식이 아니어야 궤도가 뒤집히지 않는다.")]
        [SerializeField] private Transform anchor;

        [Header("Orbit")]
        [SerializeField, Min(0.1f)] private float radius = 1.2f;
        [Tooltip("초당 회전 각도(도)")]
        [SerializeField] private float rotationSpeedDegPerSec = 90f;

        [Header("Depth (Y축 회전 표현: 정렬순서 + 크기)")]
        [Tooltip("앞/뒤 판단 기준이 되는 캐릭터의 SpriteRenderer. 비워두면 자식 중에서 자동으로 하나 찾는다.")]
        [SerializeField] private SpriteRenderer characterRenderer;
        [Tooltip("캐릭터보다 앞(코사인 > 0)일 때 캐릭터 sortingOrder에 더할 값")]
        [SerializeField] private int frontSortingOrderOffset = 1;
        [Tooltip("캐릭터보다 뒤(코사인 <= 0)일 때 캐릭터 sortingOrder에 더할 값")]
        [SerializeField] private int backSortingOrderOffset = -1;
        [SerializeField, Range(0.1f, 1f)] private float minScaleWhenBehind = 0.6f;
        [SerializeField] private float maxScaleWhenFront = 1f;

        [Header("Icon")]
        [Tooltip("아이콘 스프라이트를 이 월드 유닛 크기(가로/세로 중 큰 쪽 기준)로 맞춘다.")]
        [SerializeField, Min(0.01f)] private float iconWorldSize = 0.5f;
        [Tooltip("characterRenderer를 못 찾았을 때만 쓰이는 대체 정렬 레이어 이름")]
        [SerializeField] private string fallbackSortingLayerName = "Default";

        private WeaponInventory inventory;
        private readonly List<OrbitIcon> icons = new List<OrbitIcon>();
        private float currentAngle;

        private void Awake()
        {
            inventory = GetComponent<WeaponInventory>();
            if (inventory == null)
            {
                inventory = GetComponentInParent<WeaponInventory>();
            }

            if (anchor == null)
            {
                GameObject anchorObj = new GameObject("WeaponOrbitAnchor");
                anchorObj.transform.SetParent(transform, false);
                anchor = anchorObj.transform;
            }

            if (characterRenderer == null)
            {
                characterRenderer = GetComponentInChildren<SpriteRenderer>();
            }
        }

        private void OnEnable()
        {
            if (inventory != null)
            {
                inventory.WeaponAcquired += HandleWeaponAcquired;
            }
        }

        private void OnDisable()
        {
            if (inventory != null)
            {
                inventory.WeaponAcquired -= HandleWeaponAcquired;
            }
        }

        private void HandleWeaponAcquired(ActiveWeapon weapon)
        {
            if (weapon == null || weapon.IsStartingWeapon) return;

            Sprite icon = weapon.Data != null ? weapon.Data.ResolvedIcon : null;
            if (icon == null) return;

            CreateIcon(icon);
        }

        private void CreateIcon(Sprite sprite)
        {
            GameObject iconObj = new GameObject("WeaponOrbitIcon");
            iconObj.transform.SetParent(anchor, false);

            SpriteRenderer spriteRenderer = iconObj.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = sprite;
            spriteRenderer.sortingLayerName = characterRenderer != null ? characterRenderer.sortingLayerName : fallbackSortingLayerName;

            float spriteSize = Mathf.Max(sprite.bounds.size.x, sprite.bounds.size.y, 0.0001f);
            float baseScale = iconWorldSize / spriteSize;
            iconObj.transform.localScale = new Vector3(baseScale, baseScale, 1f);

            icons.Add(new OrbitIcon { Transform = iconObj.transform, Renderer = spriteRenderer, BaseScale = baseScale });
        }

        private void Update()
        {
            if (icons.Count == 0) return;

            currentAngle += rotationSpeedDegPerSec * Time.deltaTime;
            float angleStep = 360f / icons.Count;
            int baseSortingOrder = characterRenderer != null ? characterRenderer.sortingOrder : 0;

            for (int i = 0; i < icons.Count; i++)
            {
                OrbitIcon entry = icons[i];
                if (entry.Transform == null) continue;

                float angleRad = (currentAngle + angleStep * i) * Mathf.Deg2Rad;
                float sin = Mathf.Sin(angleRad);
                float cos = Mathf.Cos(angleRad);

                entry.Transform.localPosition = new Vector3(radius * sin, 0f, 0f);

                float depthT = (cos + 1f) * 0.5f; // 0(캐릭터 바로 뒤) ~ 1(캐릭터 바로 앞)
                float scale = entry.BaseScale * Mathf.Lerp(minScaleWhenBehind, maxScaleWhenFront, depthT);
                entry.Transform.localScale = new Vector3(scale, scale, 1f);

                if (entry.Renderer != null)
                {
                    entry.Renderer.sortingOrder = baseSortingOrder + (cos > 0f ? frontSortingOrderOffset : backSortingOrderOffset);
                }
            }
        }
    }
}
