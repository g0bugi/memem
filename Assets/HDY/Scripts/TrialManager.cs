using UnityEngine;
using KMS;

namespace HDY
{
    /// <summary>
    /// "시련" 시스템. 실제 레벨업 판정(웨이브 전환 시 남은 몬스터 수 &lt; 이번 웨이브 계획 생성 수 비교)은
    /// KMS 웨이브 시스템(KmsWaveDirector.TrialLevel/TrialLevelChanged)이 담당하고, 이 클래스는 그 결과를
    /// 구독해서 게임플레이 효과(몬스터 강화, 골드, 아이템 드랍확률)로 변환하는 단일 창구 역할만 한다.
    ///
    /// 몬스터 공격력/이동속도 증가와 플레이어 슬로우/데미지감소는 10단계 상세 밸런스가 아직 미정이라
    /// 지금은 전부 "효과 없음"(배열 값 0)으로 비워두었다. 나중에 기획이 확정되면 인스펙터에서
    /// monsterAttackMultiplierByLevel 등의 배열 값만 채우면 되고 코드 수정은 필요 없다.
    /// 몬스터 체력 증가만 요청대로 단계당 +20%씩 누적 적용 중이다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class TrialManager : MonoBehaviour
    {
        public const int MaxLevel = KmsWaveDirector.MaxTrialLevel;

        [Header("몬스터 체력 (요청대로 단계당 +20%씩 누적)")]
        [Tooltip("시련 1단계당 몬스터 체력 증가율. 1단계 +20%, 2단계 +40% ... 10단계 +200%")]
        [SerializeField, Min(0f)] private float monsterHealthIncreasePerLevel = 0.2f;

        [Header("몬스터 강화 (10단계 상세 리스트 확정 전까지는 전부 0 = 효과 없음)")]
        [SerializeField] private float[] monsterAttackMultiplierByLevel = new float[MaxLevel + 1];
        [SerializeField] private float[] monsterMoveSpeedMultiplierByLevel = new float[MaxLevel + 1];

        [Header("플레이어 디버프 (10단계 상세 리스트 확정 전까지는 전부 0 = 효과 없음)")]
        [SerializeField] private float[] playerSlowMultiplierByLevel = new float[MaxLevel + 1];
        [SerializeField] private float[] playerDamageTakenBonusByLevel = new float[MaxLevel + 1];

        [Header("골드 (기획 확정값)")]
        [Tooltip("시련 1단계당 골드 1개의 가치 증가량")]
        [SerializeField, Min(0)] private int goldValueBonusPerLevel = 1;
        [Tooltip("시련 1단계당 골드 드랍 개수(최소/최대 모두) 증가량")]
        [SerializeField, Min(0)] private int goldDropCountBonusPerLevel = 1;

        [Header("아이템 드랍확률 (기획 확정값)")]
        [Tooltip("시련 1단계당 드랍확률 증가량(가산, %p). 1단계 +5%p, 2단계 +10%p ...")]
        [SerializeField, Range(0f, 1f)] private float dropRateBonusPerLevel = 0.05f;
        [Tooltip("최종 드랍확률 상한(클램프). 기본 100%")]
        [SerializeField, Range(0f, 1f)] private float dropRateCap = 1f;

        private KmsWaveDirector waveDirector;

        public static TrialManager Instance { get; private set; }

        /// <summary>현재 시련 단계(0 = 시련 없음, 최대 10).</summary>
        public int CurrentLevel { get; private set; }

        /// <summary>시련 단계가 바뀔 때(리셋 포함) 발동. 인자는 변경된 새 단계.</summary>
        public event System.Action<int> LevelChanged;

        public float MonsterHealthMultiplier => 1f + (monsterHealthIncreasePerLevel * CurrentLevel);
        public float MonsterAttackMultiplier => 1f + GetArrayValue(monsterAttackMultiplierByLevel, CurrentLevel);
        public float MonsterMoveSpeedMultiplier => 1f + GetArrayValue(monsterMoveSpeedMultiplierByLevel, CurrentLevel);

        /// <summary>플레이어 이동속도에 곱할 배율(슬로우). 1 = 정상, 0.7 = 30% 감소 등.</summary>
        public float PlayerSlowMultiplier => Mathf.Clamp01(1f - GetArrayValue(playerSlowMultiplierByLevel, CurrentLevel));

        /// <summary>플레이어가 받는 피해에 곱할 배율. "데미지 감소" 디버프는 이 값을 1보다 크게 채워서 표현한다.</summary>
        public float PlayerDamageTakenMultiplier => 1f + GetArrayValue(playerDamageTakenBonusByLevel, CurrentLevel);

        public int GoldValueBonus => goldValueBonusPerLevel * CurrentLevel;
        public int MinGoldDropCount => KmsGoldDropController.MinimumDropCount + (goldDropCountBonusPerLevel * CurrentLevel);
        public int MaxGoldDropCount => KmsGoldDropController.MaximumDropCount + (goldDropCountBonusPerLevel * CurrentLevel);

        public float GetDropRateBonus()
        {
            return dropRateBonusPerLevel * CurrentLevel;
        }

        public float ApplyDropRateBonus(float baseChance)
        {
            return Mathf.Clamp(baseChance + GetDropRateBonus(), 0f, dropRateCap);
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        private void OnEnable()
        {
            TrySubscribe();
        }

        private void OnDisable()
        {
            if (waveDirector != null)
            {
                waveDirector.TrialLevelChanged -= HandleTrialLevelChanged;
            }

            waveDirector = null;
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void Update()
        {
            if (waveDirector == null)
            {
                TrySubscribe();
            }
        }

        private void TrySubscribe()
        {
            if (waveDirector != null)
            {
                return;
            }

            waveDirector = FindFirstObjectByType<KmsWaveDirector>();
            if (waveDirector == null)
            {
                return;
            }

            waveDirector.TrialLevelChanged += HandleTrialLevelChanged;
            CurrentLevel = waveDirector.TrialLevel;
        }

        private void HandleTrialLevelChanged(int newLevel)
        {
            CurrentLevel = newLevel;
            LevelChanged?.Invoke(newLevel);
        }

        private static float GetArrayValue(float[] array, int level)
        {
            if (array == null || level < 0 || level >= array.Length)
            {
                return 0f;
            }

            return array[level];
        }
    }
}
