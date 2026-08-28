using System.Collections.Generic;
using UnityEngine;

namespace KMS
{
    internal sealed class KmsMonsterPool
    {
        private readonly KmsMonster prefab;
        private readonly Transform parent;
        private readonly int capacity;
        private readonly Stack<KmsMonster> inactive = new Stack<KmsMonster>();

        public KmsMonsterPool(KmsMonster sourcePrefab, Transform poolParent, int prewarmCount, int hardCapacity)
        {
            prefab = sourcePrefab;
            parent = poolParent;
            capacity = Mathf.Max(1, hardCapacity);

            int count = Mathf.Clamp(prewarmCount, 0, capacity);
            for (int index = 0; index < count; index++)
            {
                KmsMonster monster = CreateInstance();
                if (monster != null)
                {
                    inactive.Push(monster);
                }
            }
        }

        public int CreatedCount { get; private set; }
        public int InactiveCount => inactive.Count;

        public KmsMonster Acquire()
        {
            while (inactive.Count > 0)
            {
                KmsMonster monster = inactive.Pop();
                if (monster != null)
                {
                    return monster;
                }

                CreatedCount = Mathf.Max(0, CreatedCount - 1);
            }

            return CreatedCount < capacity ? CreateInstance() : null;
        }

        public void Release(KmsMonster monster)
        {
            if (monster != null)
            {
                inactive.Push(monster);
            }
        }

        private KmsMonster CreateInstance()
        {
            KmsMonster monster = Object.Instantiate(prefab, parent);
            monster.name = $"{prefab.name}_Pooled_{CreatedCount + 1:000}";
            monster.PrepareForPool();
            monster.gameObject.SetActive(false);
            CreatedCount++;
            return monster;
        }
    }
}
