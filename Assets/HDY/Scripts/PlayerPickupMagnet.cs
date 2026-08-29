using UnityEngine;

namespace HDY
{
    /// <summary>
    /// 캐릭터 주변 원형 범위 안의 골드/무기 픽업을 자석처럼 끌어당겨 흡수하는 기능.
    /// Collider나 물리 레이어를 전혀 쓰지 않고 순수 스크립트 거리 계산(KmsGoldPickup/KmsWeaponPickup의
    /// Tick에서 이 컴포넌트의 Radius 값을 읽어감)으로만 동작한다 — 그래서 공격 판정, 몬스터 충돌/공격
    /// 범위와는 완전히 분리되어 있고 서로 영향을 주지 않는다. 오직 골드/무기 획득 범위 전용.
    /// Radius는 실행 중에도 자유롭게 바꿀 수 있다(예: 나중에 강화 아이템/스탯으로 확장 가능).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PlayerPickupMagnet : MonoBehaviour
    {
        [Header("자석 범위 (골드/무기 획득 전용 — 공격/몬스터 판정과 무관)")]
        [Tooltip("이 반경 안에 들어온 골드/무기 픽업을 캐릭터 쪽으로 끌어당긴다.")]
        [SerializeField, Min(0f)] private float radius = 3f;

        /// <summary>현재 자석 반경. 실행 중에 값을 바꾸면 즉시 반영된다.</summary>
        public float Radius
        {
            get => radius;
            set => radius = Mathf.Max(0f, value);
        }

        public void SetRadius(float newRadius)
        {
            Radius = newRadius;
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.85f, 0.2f, 0.35f);
            Gizmos.DrawWireSphere(transform.position, Mathf.Max(0f, radius));
        }
#endif
    }
}
