using System.Collections.Generic;
using UnityEngine;

namespace KMS
{
    public sealed class KmsWaveSpawnResult
    {
        internal KmsWaveSpawnResult(
            int waveNumber,
            int requestedMonsterCount,
            int successfulSpawnCount,
            bool deathPressureActive,
            bool trialActive)
        {
            WaveNumber = waveNumber;
            RequestedMonsterCount = requestedMonsterCount;
            SuccessfulSpawnCount = successfulSpawnCount;
            IsDeathPressureActive = deathPressureActive;
            IsTrialActive = trialActive;
        }

        public int WaveNumber { get; }
        public int RequestedMonsterCount { get; }
        public int SuccessfulSpawnCount { get; }
        public int FailedSpawnCount => RequestedMonsterCount - SuccessfulSpawnCount;
        public bool IsDeathPressureActive { get; }
        public bool IsTrialActive { get; }
    }

    [DisallowMultipleComponent]
    public sealed class KmsWaveDirector : MonoBehaviour
    {
        private const int MaximumWavesPerFrame = 8;

        [Header("References")]
        [SerializeField] private KmsWaveScheduleData schedule;
        [SerializeField] private KmsMonsterSpawner spawner;
        [SerializeField] private KmsRunTimer runTimer;

        [Header("Run End")]
        [SerializeField] private bool clearEnemiesWhenRunEnds = true;

        private readonly List<KmsWaveSpawnResult> waveHistory =
            new List<KmsWaveSpawnResult>();
        private readonly Dictionary<KmsMonster, int> originWaveByActiveMonster =
            new Dictionary<KmsMonster, int>();
        private readonly List<KmsMonster> staleTrackedMonsters = new List<KmsMonster>();

        private float nextWaveElapsedSeconds;
        private bool clearedAfterRunEnd;
        private bool waveStateInitialized;
        private bool warnedMissingReferences;
        private bool warnedInvalidMonsterPool;

        public int CurrentWaveNumber { get; private set; }
        public int UpcomingWaveNumber => CurrentWaveNumber + 1;
        public bool IsDeathPressureActive { get; private set; }
        public const int MaxTrialLevel = 10;
        public int TrialLevel { get; private set; }
        public bool IsTrialActive => TrialLevel > 0;

        /// <summary>시련 단계가 바뀔 때(리셋 포함) 발동. 인자는 변경된 새 단계(0~10).</summary>
        public event System.Action<int> TrialLevelChanged;
        public int LastUnderperformanceSpawnCount { get; private set; }
        public int LastUnderperformanceSurvivorCount { get; private set; }
        public float LastUnderperformanceSurvivorRatio { get; private set; } = -1f;
        public KmsWaveSpawnResult LastWaveResult =>
            waveHistory.Count > 0 ? waveHistory[waveHistory.Count - 1] : null;

        public int NextPlannedMonsterCount =>
            schedule != null ? schedule.GetPlannedMonsterCount(IsDeathPressureActive) : 0;

        public float SecondsUntilNextWave
        {
            get
            {
                if (runTimer == null || runTimer.HasEnded)
                {
                    return 0f;
                }

                return Mathf.Max(0f, nextWaveElapsedSeconds - runTimer.ElapsedSeconds);
            }
        }

        private void Awake()
        {
            InitializeWaveState();
        }

        private void OnDestroy()
        {
            ClearTrackedMonsters();
        }

        private void Update()
        {
            if (!ResolveReferences())
            {
                return;
            }

            if (!waveStateInitialized)
            {
                InitializeWaveState();
            }

            if (runTimer.HasEnded)
            {
                if (clearEnemiesWhenRunEnds && !clearedAfterRunEnd)
                {
                    clearedAfterRunEnd = true;
                    spawner.DespawnAll();
                    ClearTrackedMonsters();
                }

                return;
            }

            clearedAfterRunEnd = false;
            int wavesSpawnedThisFrame = 0;
            while (runTimer.ElapsedSeconds >= nextWaveElapsedSeconds &&
                wavesSpawnedThisFrame < MaximumWavesPerFrame)
            {
                SpawnUpcomingWave();
                nextWaveElapsedSeconds += schedule.WaveIntervalSeconds;
                wavesSpawnedThisFrame++;
            }
        }

        public void Configure(
            KmsWaveScheduleData waveSchedule,
            KmsMonsterSpawner monsterSpawner,
            KmsRunTimer timer,
            bool clearAtRunEnd = true)
        {
            schedule = waveSchedule;
            spawner = monsterSpawner;
            runTimer = timer;
            clearEnemiesWhenRunEnds = clearAtRunEnd;

            if (Application.isPlaying)
            {
                InitializeWaveState();
            }
        }

        public void ResetForNewRun()
        {
            spawner?.DespawnAll();
            runTimer?.ResetForNewRun();
            InitializeWaveState();
        }

        public static bool MeetsDeathPressureCondition(
            int successfulSpawnCount,
            int survivingMonsterCount,
            float survivorRatioThreshold)
        {
            if (successfulSpawnCount <= 0)
            {
                return false;
            }

            int survivors = Mathf.Clamp(
                survivingMonsterCount,
                0,
                successfulSpawnCount);
            float survivorRatio = (float)survivors / successfulSpawnCount;
            float threshold = Mathf.Clamp01(survivorRatioThreshold);
            return survivorRatio > threshold || Mathf.Approximately(survivorRatio, threshold);
        }

public static bool MeetsTrialCondition(
            int upcomingWaveNumber,
            int trialEvaluationStartWave,
            int activeMonsterCount,
            int nextPlannedMonsterCount)
        {
            // 방어코드: trialEvaluationStartWave 설정값과 무관하게 1웰이브에서는 절대 시련이 발동하지 않는다.
            if (upcomingWaveNumber <= 1)
            {
                return false;
            }

            return upcomingWaveNumber >= Mathf.Max(1, trialEvaluationStartWave) &&
                Mathf.Max(0, activeMonsterCount) < Mathf.Max(1, nextPlannedMonsterCount);
        }

        private void InitializeWaveState()
        {
            ClearTrackedMonsters();
            waveHistory.Clear();
            CurrentWaveNumber = 0;
            IsDeathPressureActive = false;
            TrialLevel = 0;
            TrialLevelChanged?.Invoke(TrialLevel);
            LastUnderperformanceSpawnCount = 0;
            LastUnderperformanceSurvivorCount = 0;
            LastUnderperformanceSurvivorRatio = -1f;
            nextWaveElapsedSeconds = schedule != null ? schedule.FirstWaveDelaySeconds : 0f;
            clearedAfterRunEnd = false;
            warnedInvalidMonsterPool = false;
            waveStateInitialized = true;
        }

        private void SpawnUpcomingWave()
        {
            int waveNumber = UpcomingWaveNumber;
            EvaluateDeathPressure(waveNumber);

            int requestedMonsterCount =
                schedule.GetPlannedMonsterCount(IsDeathPressureActive);
            EvaluateTrial(waveNumber, requestedMonsterCount);

            int successfulSpawnCount = 0;
            for (int attempt = 0; attempt < requestedMonsterCount; attempt++)
            {
                if (spawner.ActiveCount >= spawner.AbsoluteMaxActive)
                {
                    break;
                }

                if (!schedule.TrySelectMonster(
                        UnityEngine.Random.value,
                        out KmsMonsterData selectedData))
                {
                    if (!warnedInvalidMonsterPool)
                    {
                        warnedInvalidMonsterPool = true;
                        Debug.LogError(
                            "[KMS] 웨이브에 사용할 유효한 MonsterData가 없습니다.",
                            this);
                    }

                    break;
                }

                if (!spawner.TrySpawn(selectedData, out KmsMonster spawnedMonster))
                {
                    continue;
                }

                TrackMonster(spawnedMonster, waveNumber);
                successfulSpawnCount++;
            }

            CurrentWaveNumber = waveNumber;
            KmsWaveSpawnResult result = new KmsWaveSpawnResult(
                waveNumber,
                requestedMonsterCount,
                successfulSpawnCount,
                IsDeathPressureActive,
                IsTrialActive);
            waveHistory.Add(result);
        }

        private void EvaluateDeathPressure(int upcomingWaveNumber)
        {
            if (IsDeathPressureActive)
            {
                return;
            }

            int windowSize = schedule.UnderperformanceWindowWaveCount;
            if (upcomingWaveNumber <= windowSize)
            {
                return;
            }

            CleanupStaleTrackedMonsters();
            int firstWaveInWindow = upcomingWaveNumber - windowSize;
            int lastWaveInWindow = upcomingWaveNumber - 1;
            int successfulSpawnCount = 0;
            foreach (KmsWaveSpawnResult result in waveHistory)
            {
                if (result.WaveNumber >= firstWaveInWindow &&
                    result.WaveNumber <= lastWaveInWindow)
                {
                    successfulSpawnCount += result.SuccessfulSpawnCount;
                }
            }

            int survivingMonsterCount = 0;
            foreach (int originWave in originWaveByActiveMonster.Values)
            {
                if (originWave >= firstWaveInWindow && originWave <= lastWaveInWindow)
                {
                    survivingMonsterCount++;
                }
            }

            LastUnderperformanceSpawnCount = successfulSpawnCount;
            LastUnderperformanceSurvivorCount = survivingMonsterCount;
            LastUnderperformanceSurvivorRatio = successfulSpawnCount > 0
                ? (float)survivingMonsterCount / successfulSpawnCount
                : -1f;

            if (!MeetsDeathPressureCondition(
                    successfulSpawnCount,
                    survivingMonsterCount,
                    schedule.UnderperformanceSurvivorRatio))
            {
                return;
            }

            IsDeathPressureActive = true;
        }

private void EvaluateTrial(int upcomingWaveNumber, int nextPlannedMonsterCount)
        {
            if (TrialLevel >= MaxTrialLevel)
            {
                return;
            }

            if (!MeetsTrialCondition(
                    upcomingWaveNumber,
                    schedule.TrialEvaluationStartWave,
                    spawner.ActiveCount,
                    nextPlannedMonsterCount))
            {
                return;
            }

            TrialLevel = Mathf.Min(TrialLevel + 1, MaxTrialLevel);
            TrialLevelChanged?.Invoke(TrialLevel);
        }

        private void TrackMonster(KmsMonster monster, int originWave)
        {
            if (monster == null)
            {
                return;
            }

            UntrackMonster(monster);
            originWaveByActiveMonster[monster] = originWave;
            monster.Died += HandleTrackedMonsterRemoved;
            monster.UnexpectedlyDisabled += HandleTrackedMonsterRemoved;
        }

        private void HandleTrackedMonsterRemoved(KmsMonster monster)
        {
            UntrackMonster(monster);
        }

        private void CleanupStaleTrackedMonsters()
        {
            staleTrackedMonsters.Clear();
            foreach (KmsMonster monster in originWaveByActiveMonster.Keys)
            {
                if (monster == null || !monster.IsPrepared || monster.IsDead)
                {
                    staleTrackedMonsters.Add(monster);
                }
            }

            foreach (KmsMonster monster in staleTrackedMonsters)
            {
                UntrackMonster(monster);
            }

            staleTrackedMonsters.Clear();
        }

        private void ClearTrackedMonsters()
        {
            staleTrackedMonsters.Clear();
            foreach (KmsMonster monster in originWaveByActiveMonster.Keys)
            {
                staleTrackedMonsters.Add(monster);
            }

            foreach (KmsMonster monster in staleTrackedMonsters)
            {
                UntrackMonster(monster);
            }

            staleTrackedMonsters.Clear();
            originWaveByActiveMonster.Clear();
        }

        private void UntrackMonster(KmsMonster monster)
        {
            if (object.ReferenceEquals(monster, null))
            {
                return;
            }

            if (monster != null)
            {
                monster.Died -= HandleTrackedMonsterRemoved;
                monster.UnexpectedlyDisabled -= HandleTrackedMonsterRemoved;
            }

            originWaveByActiveMonster.Remove(monster);
        }

        private bool ResolveReferences()
        {
            if (schedule != null && spawner != null && runTimer != null)
            {
                warnedMissingReferences = false;
                return true;
            }

            if (!warnedMissingReferences)
            {
                warnedMissingReferences = true;
                Debug.LogError(
                    "[KMS] WaveDirector의 Schedule, Spawner, RunTimer 참조가 필요합니다.",
                    this);
            }

            return false;
        }

    }
}
