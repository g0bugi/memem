using System;
using System.Collections.Generic;
using UnityEngine;

namespace KMS
{
    [Serializable]
    public sealed class KmsWaveMonsterEntry
    {
        [SerializeField] private KmsMonsterData monsterData;
        [SerializeField, Min(0)] private int weight = 1;

        public KmsMonsterData MonsterData => monsterData;
        public int Weight => Mathf.Max(0, weight);
    }

    [Serializable]
    public sealed class KmsWavePhase
    {
        [SerializeField] private string phaseName = "Phase";
        [SerializeField, Min(0f)] private float startTimeSeconds;
        [SerializeField, Min(0.05f)] private float spawnInterval = 1f;
        [SerializeField, Min(1)] private int spawnCountPerBatch = 1;
        [SerializeField, Min(1)] private int maxActiveMonsters = 20;
        [SerializeField] private List<KmsWaveMonsterEntry> monsters = new List<KmsWaveMonsterEntry>();

        public string PhaseName => phaseName;
        public float StartTimeSeconds => Mathf.Max(0f, startTimeSeconds);
        public float SpawnInterval => Mathf.Max(0.05f, spawnInterval);
        public int SpawnCountPerBatch => Mathf.Max(1, spawnCountPerBatch);
        public int MaxActiveMonsters => Mathf.Max(1, maxActiveMonsters);
        public IReadOnlyList<KmsWaveMonsterEntry> Monsters => monsters;
    }

    [CreateAssetMenu(fileName = "KmsWaveSchedule", menuName = "KMS/Monsters/Wave Schedule")]
    public sealed class KmsWaveScheduleData : ScriptableObject
    {
        [SerializeField] private List<KmsWavePhase> phases = new List<KmsWavePhase>();

        public IReadOnlyList<KmsWavePhase> Phases => phases;

        public bool TryGetPhase(float elapsedSeconds, out KmsWavePhase selectedPhase)
        {
            selectedPhase = null;
            if (phases == null)
            {
                return false;
            }

            float elapsed = Mathf.Max(0f, elapsedSeconds);
            float latestStart = float.NegativeInfinity;
            foreach (KmsWavePhase phase in phases)
            {
                if (phase == null)
                {
                    continue;
                }

                float start = phase.StartTimeSeconds;
                if (start <= elapsed && start >= latestStart)
                {
                    latestStart = start;
                    selectedPhase = phase;
                }
            }

            return selectedPhase != null;
        }

        public static bool TrySelectMonster(
            KmsWavePhase phase,
            float unitRoll,
            out KmsMonsterData selectedData)
        {
            selectedData = null;
            if (phase == null || phase.Monsters == null)
            {
                return false;
            }

            int totalWeight = 0;
            foreach (KmsWaveMonsterEntry entry in phase.Monsters)
            {
                if (entry != null && entry.MonsterData != null && entry.Weight > 0)
                {
                    totalWeight += entry.Weight;
                }
            }

            if (totalWeight <= 0)
            {
                return false;
            }

            float roll = Mathf.Clamp(unitRoll, 0f, 0.99999994f) * totalWeight;
            int cumulativeWeight = 0;
            foreach (KmsWaveMonsterEntry entry in phase.Monsters)
            {
                if (entry == null || entry.MonsterData == null || entry.Weight <= 0)
                {
                    continue;
                }

                cumulativeWeight += entry.Weight;
                if (roll < cumulativeWeight)
                {
                    selectedData = entry.MonsterData;
                    return true;
                }
            }

            return false;
        }
    }
}
