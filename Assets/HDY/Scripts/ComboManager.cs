using UnityEngine;
using KMS;

namespace HDY
{
    [DisallowMultipleComponent]
    public sealed class ComboManager : MonoBehaviour
    {
        [Header("콤보 보너스 (100콤보당 1씩 증가)")]
        
        [Tooltip("근접 공격 부채꼴 판정 각도가 100콤보당 양옆으로 이만큼(도) 더 벌어진다(전체 각도는 이 값의 2배만큼 증가).")]
        [SerializeField, Min(0f)] private float angleBonusPerTier = 2.5f;
[SerializeField, Min(0f)] private float bonusPerTier = 1f;
        [Tooltip("자석(픽업 흡수) 반경이 100콤보당 이만큼(월드 단위) 늘어난다. 다른 보너스와 달리 전용 배율을 쓴다.")]
        [SerializeField, Min(0f)] private float magnetRadiusBonusPerTier = 0.2f;

        [Header("콤보 보너스 상한(안전장치)")]
        [Tooltip("사거리/자석 반경/폭발 반경/궤도 반경 등 반지름 계열 보너스가 아무리 콤보가 높아져도 이 값(가산량 기준)을 넘지 않도록 막는다.")]
        [SerializeField, Min(0f)] private float radiusBonusMax = 10f;
        [Tooltip("근접 부채꼴 반각(half-angle) 보너스의 최대치(도). AngleBonus는 반각에 더해지는 값이라, 360이면 이미 보너스만으로 한 바퀴를 넘는 셈이라 그 이상은 의미가 없다.")]
        [SerializeField, Min(0f)] private float angleBonusMax = 360f;
        [Tooltip("원거리 무기 추가 발사(투사체) 개수 보너스의 최대치.")]
        [SerializeField, Min(0f)] private float projectileCountBonusMax = 10f;

        [Header("콤보 감소 (미타격 시간 경과)")]
        [Tooltip("몬스터를 맞추지 못한 채 이 시간(초)이 지날 때마다 콤보가 decayAmount만큼 줄어든다. 필드에 몬스터가 없을 때는 감소하지 않는다(예외).")]
        [SerializeField, Min(0.05f)] private float decayInterval = 2f;
        [Tooltip("decayInterval마다 줄어드는 콤보 양")]
        [SerializeField, Min(0)] private int decayAmount = 10;

        private float sinceLastHit;

        public int Combo { get; private set; }

        /// <summary>콤보 수치가 바뀔 때마다(증가/감소 모두) 발동된다. HUD가 이 이벤트 없이도 폴링으로 동작하지만,
        /// 다른 시스템이 필요하면 구독해서 쓸 수 있도록 남겨둔다.</summary>
        public event System.Action<int> ComboChanged;

        public int ComboTier => Combo / 100;

        public float RangeBonus => Mathf.Min(ComboTier * bonusPerTier, radiusBonusMax);
        public float ProjectileCountBonus => Mathf.Min(ComboTier * bonusPerTier, projectileCountBonusMax);
        public float MagnetRadiusBonus => Mathf.Min(ComboTier * magnetRadiusBonusPerTier, radiusBonusMax);
        public float ExplosionRadiusBonus => Mathf.Min(ComboTier * bonusPerTier, radiusBonusMax);
        
        /// <summary>근접 공격 부채꼴의 반각(half-angle)에 더할 보너스(도). 양옆으로 이만큼씩 늘어나므로
        /// 실제 전체 각도는 이 값의 2배만큼 넓어진다.</summary>
        public float AngleBonus => Mathf.Min(ComboTier * angleBonusPerTier, angleBonusMax);
public float OrbitRadiusBonus => Mathf.Min(ComboTier * bonusPerTier, radiusBonusMax);

        /// <summary>무기가 몬스터 한 마리에게 데미지를 입힐 때마다 호출한다. 한 번의 공격(근접 부채꼴, 관통 투사체,
        /// 메테오 폭발 등)이 여러 마리를 동시에 맞히면 그 횟수만큼 여러 번 호출해서 그만큼 콤보가 여러 번 오른다.</summary>
        public void RegisterHit()
        {
            Combo++;
            sinceLastHit = 0f;
            ComboChanged?.Invoke(Combo);
        }

        private void LateUpdate()
        {
            // RegisterHit()는 PlayerAttack/OrbitWeaponController 등 다른 컴포넌트의 Update()나
            // 물리 콜백(OnTriggerEnter2D)에서 호출된다. 스크립트 실행 순서가 명시적으로 고정되어
            // 있지 않으므로, 이 컴포넌트의 감소 판정을 Update() 대신 LateUpdate()에서 수행해서
            // "이번 프레임에 명중이 있었는데도 실행 순서 때문에 감소가 먼저 적용되는" 경합을 원천 차단한다.
            if (Combo <= 0)
            {
                sinceLastHit = 0f;
                return;
            }

            if (!IsAnyMonsterOnField())
            {
                // 필드에 몬스터가 없는 동안은 예외적으로 감소하지 않는다.
                sinceLastHit = 0f;
                return;
            }

            sinceLastHit += Time.deltaTime;
            while (sinceLastHit >= decayInterval && Combo > 0)
            {
                sinceLastHit -= decayInterval;
                Combo = Mathf.Max(0, Combo - decayAmount);
                ComboChanged?.Invoke(Combo);
            }
        }

        private static bool IsAnyMonsterOnField()
        {
            KmsMonsterSpawner spawner = KmsMonsterSpawner.ActiveInstance;
            return spawner != null && spawner.ActiveCount > 0;
        }
    }
}
