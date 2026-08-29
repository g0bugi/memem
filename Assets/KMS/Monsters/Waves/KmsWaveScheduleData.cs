using System;
using System.Collections.Generic;
using UnityEngine;

namespace KMS
{
    public enum KmsWaveSpawnPattern
    {
        RandomAnnulus = 0,
        Clockwise = 1,
        ScreenPerimeter = 2
    }

    public sealed class KmsWavePlan
    {
        internal KmsWavePlan(
            int waveNumber,
            int baseMonsterCount,
            bool deathPressureActive,
            KmsWaveSpawnPattern spawnPattern,
            float spawnDurationSeconds,
            int positionSeed,
            List<KmsMonsterData> monsterRequests)
        {
            WaveNumber = waveNumber;
            BaseMonsterCount = baseMonsterCount;
            IsDeathPressureActive = deathPressureActive;
            SpawnPattern = spawnPattern;
            SpawnDurationSeconds = spawnDurationSeconds;
            PositionSeed = positionSeed;
            MonsterRequests = monsterRequests != null
                ? monsterRequests.ToArray()
                : Array.Empty<KmsMonsterData>();
        }

        public int WaveNumber { get; }
        public int BaseMonsterCount { get; }
        public bool IsDeathPressureActive { get; }
        public int RequestedMonsterCount => MonsterRequests.Count;
        public KmsWaveSpawnPattern SpawnPattern { get; }
        public float SpawnDurationSeconds { get; }
        public int PositionSeed { get; }
        public IReadOnlyList<KmsMonsterData> MonsterRequests { get; }
    }

    [Serializable]
    public sealed class KmsExclusiveWaveRule
    {
        [SerializeField] private KmsMonsterData monster;
        [SerializeField] private List<int> waveNumbers = new List<int>();

        public KmsMonsterData Monster => monster;
        public IReadOnlyList<int> WaveNumbers => waveNumbers;

        public bool AppliesTo(int waveNumber)
        {
            return monster != null && waveNumbers != null && waveNumbers.Contains(waveNumber);
        }
    }

    [Serializable]
    public sealed class KmsFixedWaveMonsterRule
    {
        [SerializeField, Min(1)] private int waveNumber = 1;
        [SerializeField] private KmsMonsterData monster;
        [SerializeField, Min(0)] private int count;
        [SerializeField] private bool excludeFromRandomFill = true;

        public int WaveNumber => Mathf.Max(1, waveNumber);
        public KmsMonsterData Monster => monster;
        public int Count => Mathf.Max(0, count);
        public bool ExcludeFromRandomFill => excludeFromRandomFill;
    }

    [CreateAssetMenu(fileName = "KmsWaveSchedule", menuName = "KMS/Monsters/Wave Schedule")]
    public sealed class KmsWaveScheduleData : ScriptableObject
    {
        private const int DeathPressureMultiplier = 2;
        private const float MaximumClockwiseSpawnDurationSeconds = 0.5f;

        [Header("Timing")]
        [SerializeField, Min(0f)] private float firstWaveDelaySeconds = 3f;
        [SerializeField, Min(0.05f)] private float waveIntervalSeconds = 10f;

        [Header("Wave Size - Ten Waves Per Phase")]
        [SerializeField, Min(1)] private int wavesPerPhase = 10;
        [SerializeField] private List<int> phaseMonsterCounts =
            new List<int> { 15, 20, 40, 65, 80, 100 };

        [Header("Death Pressure")]
        [SerializeField, Min(1)] private int underperformanceWindowWaveCount = 3;
        [SerializeField, Range(0f, 1f)] private float underperformanceSurvivorRatio = 0.8f;

        [Header("Trial Detection")]
        [SerializeField, Min(1)] private int trialEvaluationStartWave = 3;

        [Header("Trial Boss")]
        [SerializeField] private KmsMonsterData trialBossData;
        [SerializeField, Min(0f)] private float trialBossLeadSeconds = 1f;

        [Header("Regular Monster Progression")]
        [SerializeField] private List<KmsMonsterData> monsters = new List<KmsMonsterData>();
        [SerializeField] private List<int> firstAvailableWaves = new List<int> { 1, 3, 12, 5 };
        [SerializeField] private List<KmsExclusiveWaveRule> exclusiveWaveRules =
            new List<KmsExclusiveWaveRule>();
        [SerializeField] private List<KmsFixedWaveMonsterRule> fixedWaveMonsterRules =
            new List<KmsFixedWaveMonsterRule>();

        [Header("Spawn Direction")]
        [SerializeField, Min(1)] private int directedSpawnPatternStartWave = 21;
        [SerializeField, Range(0f, MaximumClockwiseSpawnDurationSeconds)]
        private float clockwiseSpawnDurationSeconds = 0.35f;

        public float FirstWaveDelaySeconds => Mathf.Max(0f, firstWaveDelaySeconds);
        public float WaveIntervalSeconds => Mathf.Max(0.05f, waveIntervalSeconds);
        public int WavesPerPhase => Mathf.Max(1, wavesPerPhase);
        public int MaximumWaveNumber =>
            phaseMonsterCounts != null ? WavesPerPhase * phaseMonsterCounts.Count : 0;
        public int UnderperformanceWindowWaveCount =>
            Mathf.Max(1, underperformanceWindowWaveCount);
        public float UnderperformanceSurvivorRatio =>
            Mathf.Clamp01(underperformanceSurvivorRatio);
        public int TrialEvaluationStartWave => Mathf.Max(1, trialEvaluationStartWave);
        public KmsMonsterData TrialBossData => trialBossData;
        public float TrialBossLeadSeconds =>
            Mathf.Clamp(trialBossLeadSeconds, 0f, WaveIntervalSeconds);
        public IReadOnlyList<KmsMonsterData> Monsters => monsters;
        public IReadOnlyList<int> FirstAvailableWaves => firstAvailableWaves;
        public IReadOnlyList<KmsExclusiveWaveRule> ExclusiveWaveRules => exclusiveWaveRules;
        public IReadOnlyList<KmsFixedWaveMonsterRule> FixedWaveMonsterRules =>
            fixedWaveMonsterRules;
        public int DirectedSpawnPatternStartWave => Mathf.Max(1, directedSpawnPatternStartWave);
        public float ClockwiseSpawnDurationSeconds =>
            Mathf.Clamp(
                clockwiseSpawnDurationSeconds,
                0f,
                MaximumClockwiseSpawnDurationSeconds);

        public int GetBaseMonsterCount(int waveNumber)
        {
            if (waveNumber <= 0 || phaseMonsterCounts == null || phaseMonsterCounts.Count == 0)
            {
                return 0;
            }

            int phaseIndex = (waveNumber - 1) / WavesPerPhase;
            if (phaseIndex < 0 || phaseIndex >= phaseMonsterCounts.Count)
            {
                return 0;
            }

            return Mathf.Max(1, phaseMonsterCounts[phaseIndex]);
        }

        public int GetPlannedMonsterCount(int waveNumber, bool deathPressureActive)
        {
            int baseCount = GetBaseMonsterCount(waveNumber);
            if (baseCount <= 0)
            {
                return 0;
            }

            return deathPressureActive ? baseCount * DeathPressureMultiplier : baseCount;
        }

        public bool TryCreateWavePlan(
            int waveNumber,
            bool deathPressureActive,
            int randomSeed,
            out KmsWavePlan plan)
        {
            plan = null;
            int baseCount = GetBaseMonsterCount(waveNumber);
            int requestedCount = GetPlannedMonsterCount(waveNumber, deathPressureActive);
            if (baseCount <= 0 || requestedCount <= 0)
            {
                return false;
            }

            System.Random random = new System.Random(randomSeed);
            List<KmsMonsterData> requests = new List<KmsMonsterData>(requestedCount);
            if (!TryBuildMonsterRequests(waveNumber, requestedCount, random, requests))
            {
                return false;
            }

            KmsWaveSpawnPattern pattern = SelectSpawnPattern(waveNumber, random.NextDouble());
            float spawnDuration = pattern == KmsWaveSpawnPattern.Clockwise
                ? ClockwiseSpawnDurationSeconds
                : 0f;
            int positionSeed = random.Next();
            plan = new KmsWavePlan(
                waveNumber,
                baseCount,
                deathPressureActive,
                pattern,
                spawnDuration,
                positionSeed,
                requests);
            return true;
        }

        public KmsWaveSpawnPattern SelectSpawnPattern(int waveNumber, double unitRoll)
        {
            if (waveNumber < DirectedSpawnPatternStartWave)
            {
                return KmsWaveSpawnPattern.RandomAnnulus;
            }

            double clampedRoll = Math.Max(0d, Math.Min(0.999999999d, unitRoll));
            int patternIndex = (int)(clampedRoll * 3d);
            return (KmsWaveSpawnPattern)patternIndex;
        }

        private bool TryBuildMonsterRequests(
            int waveNumber,
            int requestedCount,
            System.Random random,
            List<KmsMonsterData> output)
        {
            output.Clear();

            if (TryGetExclusiveMonster(waveNumber, out KmsMonsterData exclusiveMonster))
            {
                if (!IsRegularMonster(exclusiveMonster))
                {
                    return false;
                }

                for (int index = 0; index < requestedCount; index++)
                {
                    output.Add(exclusiveMonster);
                }

                return true;
            }

            HashSet<KmsMonsterData> excludedFromFill = new HashSet<KmsMonsterData>();
            if (fixedWaveMonsterRules != null)
            {
                foreach (KmsFixedWaveMonsterRule rule in fixedWaveMonsterRules)
                {
                    if (rule == null || rule.WaveNumber != waveNumber || rule.Count <= 0 ||
                        !IsRegularMonster(rule.Monster))
                    {
                        continue;
                    }

                    int remaining = requestedCount - output.Count;
                    int fixedCount = Mathf.Min(remaining, rule.Count);
                    for (int index = 0; index < fixedCount; index++)
                    {
                        output.Add(rule.Monster);
                    }

                    if (rule.ExcludeFromRandomFill)
                    {
                        excludedFromFill.Add(rule.Monster);
                    }

                    if (output.Count >= requestedCount)
                    {
                        return true;
                    }
                }
            }

            List<KmsMonsterData> eligibleMonsters = new List<KmsMonsterData>();
            if (monsters != null)
            {
                for (int index = 0; index < monsters.Count; index++)
                {
                    KmsMonsterData monster = monsters[index];
                    int firstWave = firstAvailableWaves != null && index < firstAvailableWaves.Count
                        ? Mathf.Max(1, firstAvailableWaves[index])
                        : 1;
                    if (!IsRegularMonster(monster) || firstWave > waveNumber ||
                        excludedFromFill.Contains(monster) || eligibleMonsters.Contains(monster))
                    {
                        continue;
                    }

                    eligibleMonsters.Add(monster);
                }
            }

            if (eligibleMonsters.Count == 0)
            {
                return false;
            }

            while (output.Count < requestedCount)
            {
                output.Add(eligibleMonsters[random.Next(eligibleMonsters.Count)]);
            }

            Shuffle(output, random);
            return true;
        }

        private bool TryGetExclusiveMonster(int waveNumber, out KmsMonsterData monster)
        {
            monster = null;
            if (exclusiveWaveRules == null)
            {
                return false;
            }

            foreach (KmsExclusiveWaveRule rule in exclusiveWaveRules)
            {
                if (rule != null && rule.AppliesTo(waveNumber))
                {
                    monster = rule.Monster;
                    return true;
                }
            }

            return false;
        }

        private bool IsRegularMonster(KmsMonsterData monster)
        {
            return monster != null && monster != trialBossData;
        }

        private static void Shuffle(List<KmsMonsterData> values, System.Random random)
        {
            for (int index = values.Count - 1; index > 0; index--)
            {
                int swapIndex = random.Next(index + 1);
                (values[index], values[swapIndex]) = (values[swapIndex], values[index]);
            }
        }

        private void OnValidate()
        {
            wavesPerPhase = Mathf.Max(1, wavesPerPhase);
            directedSpawnPatternStartWave = Mathf.Max(1, directedSpawnPatternStartWave);
            clockwiseSpawnDurationSeconds = Mathf.Clamp(
                clockwiseSpawnDurationSeconds,
                0f,
                MaximumClockwiseSpawnDurationSeconds);

            if (phaseMonsterCounts != null)
            {
                for (int index = 0; index < phaseMonsterCounts.Count; index++)
                {
                    phaseMonsterCounts[index] = Mathf.Max(1, phaseMonsterCounts[index]);
                }
            }

            if (firstAvailableWaves != null)
            {
                for (int index = 0; index < firstAvailableWaves.Count; index++)
                {
                    firstAvailableWaves[index] = Mathf.Max(1, firstAvailableWaves[index]);
                }
            }
        }
    }
}
