using System.Collections.Generic;
using UnityEngine;

namespace KMS
{
    [CreateAssetMenu(fileName = "KmsWaveSchedule", menuName = "KMS/Monsters/Wave Schedule")]
    public sealed class KmsWaveScheduleData : ScriptableObject
    {
        private const int DeathPressureMultiplier = 2;

        [Header("Timing")]
        [SerializeField, Min(0f)] private float firstWaveDelaySeconds = 3f;
        [SerializeField, Min(0.05f)] private float waveIntervalSeconds = 10f;

        [Header("Wave Size")]
        [SerializeField, Min(1)] private int baseMonsterCount = 20;

        [Header("Death Pressure")]
        [SerializeField, Min(1)] private int underperformanceWindowWaveCount = 3;
        [SerializeField, Range(0f, 1f)] private float underperformanceSurvivorRatio = 0.8f;

        [Header("Trial Detection")]
        [SerializeField, Min(1)] private int trialEvaluationStartWave = 3;

        [Header("Temporary Random Monster Pool")]
        [SerializeField] private List<KmsMonsterData> monsters = new List<KmsMonsterData>();

        public float FirstWaveDelaySeconds => Mathf.Max(0f, firstWaveDelaySeconds);
        public float WaveIntervalSeconds => Mathf.Max(0.05f, waveIntervalSeconds);
        public int BaseMonsterCount => Mathf.Max(1, baseMonsterCount);
        public int DeathPressureMonsterCount => BaseMonsterCount * DeathPressureMultiplier;
        public int UnderperformanceWindowWaveCount =>
            Mathf.Max(1, underperformanceWindowWaveCount);
        public float UnderperformanceSurvivorRatio =>
            Mathf.Clamp01(underperformanceSurvivorRatio);
        public int TrialEvaluationStartWave => Mathf.Max(1, trialEvaluationStartWave);
        public IReadOnlyList<KmsMonsterData> Monsters => monsters;

        public int GetPlannedMonsterCount(bool deathPressureActive)
        {
            return deathPressureActive ? DeathPressureMonsterCount : BaseMonsterCount;
        }

        public bool TrySelectMonster(float unitRoll, out KmsMonsterData selectedData)
        {
            selectedData = null;
            if (monsters == null)
            {
                return false;
            }

            int validCount = 0;
            foreach (KmsMonsterData monster in monsters)
            {
                if (monster != null)
                {
                    validCount++;
                }
            }

            if (validCount <= 0)
            {
                return false;
            }

            float clampedRoll = Mathf.Clamp(unitRoll, 0f, 0.99999994f);
            int selectedIndex = Mathf.FloorToInt(clampedRoll * validCount);
            int validIndex = 0;
            foreach (KmsMonsterData monster in monsters)
            {
                if (monster == null)
                {
                    continue;
                }

                if (validIndex == selectedIndex)
                {
                    selectedData = monster;
                    return true;
                }

                validIndex++;
            }

            return false;
        }
    }
}
