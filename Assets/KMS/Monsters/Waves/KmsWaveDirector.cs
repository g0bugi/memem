using System.Collections.Generic;
using UnityEngine;

namespace KMS
{
    [DisallowMultipleComponent]
    public sealed class KmsWaveDirector : MonoBehaviour
    {
        private const int MaximumSpawnTicksPerFrame = 8;

        [Header("References")]
        [SerializeField] private KmsWaveScheduleData schedule;
        [SerializeField] private KmsMonsterSpawner spawner;
        [SerializeField] private KmsRunTimer runTimer;

        [Header("Run End")]
        [SerializeField] private bool clearEnemiesWhenRunEnds = true;

        private readonly HashSet<KmsWavePhase> warnedInvalidPhases = new HashSet<KmsWavePhase>();
        private float spawnAccumulator;
        private bool clearedAfterRunEnd;
        private bool warnedMissingReferences;

        public KmsWavePhase CurrentPhase { get; private set; }
        public string CurrentPhaseName => CurrentPhase != null ? CurrentPhase.PhaseName : "None";

        private void Update()
        {
            if (!ResolveReferences())
            {
                return;
            }

            if (runTimer.HasEnded)
            {
                if (clearEnemiesWhenRunEnds && !clearedAfterRunEnd)
                {
                    clearedAfterRunEnd = true;
                    spawner.DespawnAll();
                }

                return;
            }

            clearedAfterRunEnd = false;
            if (!schedule.TryGetPhase(runTimer.ElapsedSeconds, out KmsWavePhase phase))
            {
                CurrentPhase = null;
                return;
            }

            CurrentPhase = phase;
            spawnAccumulator += Time.deltaTime;
            int spawnTicks = 0;
            while (spawnAccumulator >= phase.SpawnInterval && spawnTicks < MaximumSpawnTicksPerFrame)
            {
                spawnAccumulator -= phase.SpawnInterval;
                SpawnBatch(phase);
                spawnTicks++;
            }

            if (spawnTicks >= MaximumSpawnTicksPerFrame)
            {
                spawnAccumulator = Mathf.Min(spawnAccumulator, phase.SpawnInterval);
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
        }

        public void ResetForNewRun()
        {
            spawner?.DespawnAll();
            runTimer?.ResetForNewRun();
            spawnAccumulator = 0f;
            clearedAfterRunEnd = false;
            CurrentPhase = null;
            warnedInvalidPhases.Clear();
        }

        private void SpawnBatch(KmsWavePhase phase)
        {
            int phaseLimit = Mathf.Min(phase.MaxActiveMonsters, spawner.AbsoluteMaxActive);
            int availableSlots = Mathf.Max(0, phaseLimit - spawner.ActiveCount);
            int spawnCount = Mathf.Min(phase.SpawnCountPerBatch, availableSlots);

            for (int index = 0; index < spawnCount; index++)
            {
                if (!KmsWaveScheduleData.TrySelectMonster(
                        phase,
                        Random.value,
                        out KmsMonsterData selectedData))
                {
                    if (warnedInvalidPhases.Add(phase))
                    {
                        Debug.LogError(
                            $"[KMS] 웨이브 '{phase.PhaseName}'에 가중치가 1 이상인 MonsterData가 없습니다.",
                            this);
                    }

                    return;
                }

                if (!spawner.TrySpawn(selectedData))
                {
                    return;
                }
            }
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
                Debug.LogError("[KMS] WaveDirector의 Schedule, Spawner, RunTimer 참조가 필요합니다.", this);
            }

            return false;
        }
    }
}
