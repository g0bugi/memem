using System.Collections.Generic;
using UnityEngine;

namespace KMS
{
    public sealed class KmsWaveSpawnResult
    {
        internal KmsWaveSpawnResult(
            int waveNumber,
            int baseMonsterCount,
            int requestedMonsterCount,
            int successfulSpawnCount,
            bool deathPressureActive,
            bool trialActive,
            bool trialBossRequested,
            bool trialBossSpawned,
            KmsWaveSpawnPattern spawnPattern,
            float spawnDurationSeconds)
        {
            WaveNumber = waveNumber;
            BaseMonsterCount = baseMonsterCount;
            RequestedMonsterCount = requestedMonsterCount;
            SuccessfulSpawnCount = successfulSpawnCount;
            IsDeathPressureActive = deathPressureActive;
            IsTrialActive = trialActive;
            TrialBossRequested = trialBossRequested;
            TrialBossSpawned = trialBossSpawned;
            SpawnPattern = spawnPattern;
            SpawnDurationSeconds = spawnDurationSeconds;
        }

        public int WaveNumber { get; }
        public int BaseMonsterCount { get; }
        public int RequestedMonsterCount { get; }
        public int SuccessfulSpawnCount { get; }
        public int FailedSpawnCount => RequestedMonsterCount - SuccessfulSpawnCount;
        public bool IsDeathPressureActive { get; }
        public bool IsTrialActive { get; }
        public bool TrialBossRequested { get; }
        public bool TrialBossSpawned { get; }
        public bool TrialBossFailed => TrialBossRequested && !TrialBossSpawned;
        public int TotalSuccessfulSpawnCount =>
            SuccessfulSpawnCount + (TrialBossSpawned ? 1 : 0);
        public KmsWaveSpawnPattern SpawnPattern { get; }
        public float SpawnDurationSeconds { get; }
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
        private readonly List<Vector3> pendingSpawnPositions = new List<Vector3>();

        private float nextWaveElapsedSeconds;
        private float pendingRegularSpawnElapsedSeconds;
        private float pendingDeploymentEndElapsedSeconds;
        private int pendingSpawnAttemptIndex;
        private int pendingSuccessfulSpawnCount;
        private bool pendingTrialActive;
        private bool pendingTrialBossRequested;
        private bool pendingTrialBossSpawned;
        private bool pendingDeploymentStarted;
        private bool pendingPatternPositionsReady;
        private bool pendingStoppedByActiveCap;
        private bool hasPendingWave;
        private bool clearedAfterRunEnd;
        private bool waveStateInitialized;
        private bool warnedMissingReferences;
        private bool warnedInvalidWavePlan;
        private bool warnedMissingTrialBoss;
        private bool warnedPatternFallback;
        private KmsWavePlan pendingPlan;

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
        public int NextPlannedMonsterCount => hasPendingWave && pendingPlan != null
            ? pendingPlan.RequestedMonsterCount
            : schedule != null ? schedule.GetBaseMonsterCount(UpcomingWaveNumber) : 0;

        public float SecondsUntilNextWave
        {
            get
            {
                if (runTimer == null || runTimer.HasEnded)
                {
                    return 0f;
                }

                float targetElapsedSeconds = hasPendingWave
                    ? pendingRegularSpawnElapsedSeconds
                    : nextWaveElapsedSeconds;
                return Mathf.Max(0f, targetElapsedSeconds - runTimer.ElapsedSeconds);
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
                if (!clearedAfterRunEnd)
                {
                    clearedAfterRunEnd = true;
                    ClearPendingWave();
                    if (clearEnemiesWhenRunEnds)
                    {
                        spawner.DespawnAll();
                        ClearTrackedMonsters();
                    }
                }

                return;
            }

            clearedAfterRunEnd = false;
            int wavesCompletedThisFrame = 0;
            while (wavesCompletedThisFrame < MaximumWavesPerFrame)
            {
                if (hasPendingWave)
                {
                    if (!UpdatePendingWave())
                    {
                        break;
                    }

                    wavesCompletedThisFrame++;
                    continue;
                }

                if (runTimer.ElapsedSeconds < nextWaveElapsedSeconds)
                {
                    break;
                }

                BeginUpcomingWave(nextWaveElapsedSeconds);
                nextWaveElapsedSeconds += schedule.WaveIntervalSeconds;
                if (!hasPendingWave)
                {
                    wavesCompletedThisFrame++;
                }
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

            int survivors = Mathf.Clamp(survivingMonsterCount, 0, successfulSpawnCount);
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
            warnedInvalidWavePlan = false;
            warnedMissingTrialBoss = false;
            warnedPatternFallback = false;
            ClearPendingWave();
            waveStateInitialized = true;
        }

        private void BeginUpcomingWave(float scheduledWaveElapsedSeconds)
        {
            int waveNumber = UpcomingWaveNumber;
            EvaluateDeathPressure(waveNumber);

            if (!schedule.TryCreateWavePlan(
                    waveNumber,
                    IsDeathPressureActive,
                    UnityEngine.Random.Range(0, int.MaxValue),
                    out KmsWavePlan plan))
            {
                if (!warnedInvalidWavePlan)
                {
                    warnedInvalidWavePlan = true;
                    Debug.LogError(
                        $"[KMS] {waveNumber}웨이브 계획을 만들 수 없습니다. 스케줄 편성을 확인해 주세요.",
                        this);
                }

                CurrentWaveNumber = waveNumber;
                return;
            }

            EvaluateTrial(waveNumber, plan.RequestedMonsterCount);

            pendingPlan = plan;
            pendingTrialActive = IsTrialActive;
            pendingTrialBossRequested = IsTrialActive;
            pendingTrialBossSpawned = false;
            pendingRegularSpawnElapsedSeconds = scheduledWaveElapsedSeconds +
                (pendingTrialBossRequested ? schedule.TrialBossLeadSeconds : 0f);
            hasPendingWave = true;
            TrySpawnPendingTrialBoss();
        }

        private bool UpdatePendingWave()
        {
            TrySpawnPendingTrialBoss();
            if (runTimer.ElapsedSeconds < pendingRegularSpawnElapsedSeconds)
            {
                return false;
            }

            if (!pendingDeploymentStarted)
            {
                BeginRegularDeployment();
            }

            int targetAttemptCount = pendingPlan.RequestedMonsterCount;
            if (pendingPlan.SpawnPattern == KmsWaveSpawnPattern.Clockwise &&
                pendingPlan.SpawnDurationSeconds > 0f &&
                runTimer.ElapsedSeconds < pendingDeploymentEndElapsedSeconds)
            {
                float progress = Mathf.InverseLerp(
                    pendingRegularSpawnElapsedSeconds,
                    pendingDeploymentEndElapsedSeconds,
                    runTimer.ElapsedSeconds);
                targetAttemptCount = Mathf.Clamp(
                    Mathf.FloorToInt(progress * pendingPlan.RequestedMonsterCount) + 1,
                    1,
                    pendingPlan.RequestedMonsterCount);
            }

            SpawnPendingRequestsUntil(targetAttemptCount);
            if (pendingStoppedByActiveCap)
            {
                CompletePendingWave();
                return true;
            }

            if (pendingSpawnAttemptIndex < pendingPlan.RequestedMonsterCount)
            {
                return false;
            }

            CompletePendingWave();
            return true;
        }

        private void BeginRegularDeployment()
        {
            pendingDeploymentStarted = true;
            pendingDeploymentEndElapsedSeconds = pendingRegularSpawnElapsedSeconds +
                pendingPlan.SpawnDurationSeconds;
            pendingSpawnPositions.Clear();
            pendingPatternPositionsReady = false;

            if (pendingPlan.SpawnPattern == KmsWaveSpawnPattern.RandomAnnulus)
            {
                return;
            }

            pendingPatternPositionsReady = spawner.TryBuildSpawnPositions(
                pendingPlan.SpawnPattern,
                pendingPlan.RequestedMonsterCount,
                pendingPlan.PositionSeed,
                pendingSpawnPositions);
            if (!pendingPatternPositionsReady && !warnedPatternFallback)
            {
                warnedPatternFallback = true;
                Debug.LogWarning(
                    "[KMS] 지시형 웨이브 위치를 계산하지 못해 12~24 무작위 생성으로 대체합니다.",
                    this);
            }
        }

        private void SpawnPendingRequestsUntil(int targetAttemptCount)
        {
            int clampedTarget = Mathf.Clamp(
                targetAttemptCount,
                pendingSpawnAttemptIndex,
                pendingPlan.RequestedMonsterCount);
            while (pendingSpawnAttemptIndex < clampedTarget)
            {
                if (spawner.ActiveCount >= spawner.AbsoluteMaxActive)
                {
                    pendingStoppedByActiveCap = true;
                    pendingSpawnAttemptIndex = pendingPlan.RequestedMonsterCount;
                    return;
                }

                int requestIndex = pendingSpawnAttemptIndex;
                KmsMonsterData monsterData = pendingPlan.MonsterRequests[requestIndex];
                bool spawned = pendingPatternPositionsReady
                    ? spawner.TrySpawnAt(
                        monsterData,
                        pendingSpawnPositions[requestIndex],
                        out KmsMonster directedMonster) && TrackSpawnedMonster(directedMonster)
                    : spawner.TrySpawn(monsterData, out KmsMonster randomMonster) &&
                        TrackSpawnedMonster(randomMonster);

                pendingSpawnAttemptIndex++;
                if (spawned)
                {
                    pendingSuccessfulSpawnCount++;
                }
            }
        }

        private bool TrackSpawnedMonster(KmsMonster monster)
        {
            if (monster == null)
            {
                return false;
            }

            TrackMonster(monster, pendingPlan.WaveNumber);
            return true;
        }

        private void TrySpawnPendingTrialBoss()
        {
            if (!hasPendingWave || !pendingTrialBossRequested || pendingTrialBossSpawned)
            {
                return;
            }

            KmsMonsterData bossData = schedule.TrialBossData;
            if (bossData == null)
            {
                if (!warnedMissingTrialBoss)
                {
                    warnedMissingTrialBoss = true;
                    Debug.LogError(
                        "[KMS] 시련 웨이브에 사용할 우두머리 MonsterData가 없습니다.",
                        this);
                }

                return;
            }

            if (!spawner.TrySpawn(bossData, out KmsMonster spawnedBoss))
            {
                return;
            }

            TrackMonster(spawnedBoss, pendingPlan.WaveNumber);
            pendingTrialBossSpawned = true;
        }

        private void CompletePendingWave()
        {
            CurrentWaveNumber = pendingPlan.WaveNumber;
            KmsWaveSpawnResult result = new KmsWaveSpawnResult(
                pendingPlan.WaveNumber,
                pendingPlan.BaseMonsterCount,
                pendingPlan.RequestedMonsterCount,
                pendingSuccessfulSpawnCount,
                pendingPlan.IsDeathPressureActive,
                pendingTrialActive,
                pendingTrialBossRequested,
                pendingTrialBossSpawned,
                pendingPlan.SpawnPattern,
                pendingPlan.SpawnDurationSeconds);
            waveHistory.Add(result);
            ClearPendingWave();
        }

        private void ClearPendingWave()
        {
            pendingPlan = null;
            pendingRegularSpawnElapsedSeconds = 0f;
            pendingDeploymentEndElapsedSeconds = 0f;
            pendingSpawnAttemptIndex = 0;
            pendingSuccessfulSpawnCount = 0;
            pendingTrialActive = false;
            pendingTrialBossRequested = false;
            pendingTrialBossSpawned = false;
            pendingDeploymentStarted = false;
            pendingPatternPositionsReady = false;
            pendingStoppedByActiveCap = false;
            pendingSpawnPositions.Clear();
            hasPendingWave = false;
        }

        private void EvaluateDeathPressure(int upcomingWaveNumber)
        {
            IsDeathPressureActive = false;
            LastUnderperformanceSpawnCount = 0;
            LastUnderperformanceSurvivorCount = 0;
            LastUnderperformanceSurvivorRatio = -1f;

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
                    successfulSpawnCount += result.TotalSuccessfulSpawnCount;
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
            IsDeathPressureActive = MeetsDeathPressureCondition(
                successfulSpawnCount,
                survivingMonsterCount,
                schedule.UnderperformanceSurvivorRatio);
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
