using UnityEngine;
using KMS;

namespace HDY
{
    [DisallowMultipleComponent]
    public sealed class ComboManager : MonoBehaviour
    {
        [Header("콤보 보너스 (100콤보당 1씩 증가)")]
        [SerializeField, Min(0f)] private float bonusPerTier = 1f;

        [Header("콤보 감소 (미타격 시간 경과)")]
        [Tooltip("몬스터를 맞추지 못한 채 이 시간(초)이 지날 때마다 콤보가 decayAmount만큼 줄어든다. 필드에 몬스터가 없을 때는 감소하지 않는다(예외).")]
        [SerializeField, Min(0.05f)] private float decayInterval = 1f;
        [Tooltip("decayInterval마다 줄어드는 콤보 양")]
        [SerializeField, Min(0)] private int decayAmount = 10;

        private float sinceLastHit;

        public int Combo { get; private set; }

        /// <summary>콤보 수치가 바뀔 때마다(증가/감소 모두) 발동된다. HUD가 이 이벤트 없이도 폴링으로 동작하지만,
        /// 다른 시스템이 필요하면 구독해서 쓸 수 있도록 남겨둔다.</summary>
        public event System.Action<int> ComboChanged;

        public int ComboTier => Combo / 100;

        public float RangeBonus => ComboTier * bonusPerTier;
        public float ProjectileCountBonus => ComboTier * bonusPerTier;
        public float MagnetRadiusBonus => ComboTier * bonusPerTier;
        public float ExplosionRadiusBonus => ComboTier * bonusPerTier;
        public float OrbitRadiusBonus => ComboTier * bonusPerTier;

        /// <summary>무기가 몬스터 한 마리에게 데미지를 입힐 때마다 호출한다. 한 번의 공격(근접 부채꼴, 관통 투사체,
        /// 메테오 폭발 등)이 여러 마리를 동시에 맞히면 그 횟수만큼 여러 번 호출해서 그만큼 콤보가 여러 번 오른다.</summary>
        public void RegisterHit()
        {
            Combo++;
            sinceLastHit = 0f;
            ComboChanged?.Invoke(Combo);
        }

        private void Update()
        {
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
