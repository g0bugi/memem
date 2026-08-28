using System.Collections.Generic;
using UnityEngine;

namespace KMS
{
    [DisallowMultipleComponent]
    public sealed class KmsWeaponDropController : MonoBehaviour
    {
        public const float DefaultWeaponDropChance = 0.01f;
        public const float CommonChance = 0.70f;
        public const float RareChance = 0.20f;
        public const float LegendaryChance = 0.10f;

        [Header("References")]
        [SerializeField] private KmsWeaponDropTable dropTable;

        [Header("Drop Rule")]
        [SerializeField, Range(0f, 1f)] private float weaponDropChance = DefaultWeaponDropChance;

        [Header("Scatter")]
        [SerializeField, Min(0f)] private float minimumScatterDistance = 0.55f;
        [SerializeField, Min(0f)] private float maximumScatterDistance = 1f;

        private readonly HashSet<ItemGrade> warnedMissingGrades = new HashSet<ItemGrade>();
        private KmsMonsterSpawner monsterSpawner;
        private WeaponInventory weaponInventory;
        private KmsPickupManager pickupManager;
        private bool isSubscribed;

        public float WeaponDropChance => weaponDropChance;
        public bool HasRolledGrade { get; private set; }
        public ItemGrade LastRolledGrade { get; private set; }
        public string LastDroppedWeaponId { get; private set; }
        public int TotalSpawnedPickupCount { get; private set; }
        internal bool HasSpawnerSubscription => isSubscribed && monsterSpawner != null;

        private void Awake()
        {
            ResolveReferences(false);
        }

        private void OnEnable()
        {
            SubscribeToSpawner();
        }

        private void Start()
        {
            SubscribeToSpawner();
        }

        private void OnDisable()
        {
            UnsubscribeFromSpawner();
        }

        public void ConfigureDropTable(KmsWeaponDropTable table)
        {
            dropTable = table;
        }

        public static bool ShouldDrop(float unitRoll, float configuredChance)
        {
            float chance = Mathf.Clamp01(configuredChance);
            if (chance <= 0f)
            {
                return false;
            }

            return chance >= 1f || Mathf.Clamp01(unitRoll) < chance;
        }

        public static ItemGrade SelectGrade(float unitRoll)
        {
            float roll = Mathf.Clamp(unitRoll, 0f, 0.99999994f);
            if (roll < CommonChance)
            {
                return ItemGrade.Common;
            }

            if (roll < CommonChance + RareChance)
            {
                return ItemGrade.Rare;
            }

            return ItemGrade.Legendary;
        }

        internal void EnsureSpawnerSubscription()
        {
            if (!isActiveAndEnabled || HasSpawnerSubscription)
            {
                return;
            }

            isSubscribed = false;
            monsterSpawner = null;
            SubscribeToSpawner();
        }

        private void SubscribeToSpawner()
        {
            ResolveReferences(false);
            if (isSubscribed || monsterSpawner == null)
            {
                return;
            }

            monsterSpawner.MonsterDied += HandleMonsterDied;
            isSubscribed = true;
        }

        private void UnsubscribeFromSpawner()
        {
            if (!isSubscribed)
            {
                return;
            }

            if (monsterSpawner != null)
            {
                monsterSpawner.MonsterDied -= HandleMonsterDied;
            }

            isSubscribed = false;
        }

        private void HandleMonsterDied(KmsMonster monster)
        {
            if (!ShouldDrop(Random.value, weaponDropChance))
            {
                return;
            }

            if (!ResolveReferences(true))
            {
                return;
            }

            ItemGrade grade = SelectGrade(Random.value);
            HasRolledGrade = true;
            LastRolledGrade = grade;

            if (!dropTable.TrySelect(grade, weaponInventory.ActiveWeapons, Random.value, out WeaponData selectedWeapon))
            {
                if (!dropTable.HasConfiguredWeapon(grade) && warnedMissingGrades.Add(grade))
                {
                    Debug.LogWarning(
                        $"[KMS] {grade} 등급으로 설정된 드롭 무기가 없어 이번 무기 드롭을 생략합니다.",
                        this);
                }

                return;
            }

            if (!IsRegisteredCatalogWeapon(selectedWeapon))
            {
                return;
            }

            float minimumDistance = Mathf.Min(minimumScatterDistance, maximumScatterDistance);
            float maximumDistance = Mathf.Max(minimumScatterDistance, maximumScatterDistance);
            float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            float distance = Random.Range(minimumDistance, maximumDistance);
            Vector2 scatterOffset = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * distance;
            Vector3 origin = monster.transform.position;

            if (!pickupManager.SpawnWeapon(selectedWeapon.id, selectedWeapon.grade, origin, scatterOffset))
            {
                return;
            }

            LastDroppedWeaponId = selectedWeapon.id;
            TotalSpawnedPickupCount++;
        }

        private bool ResolveReferences(bool logErrors)
        {
            if (pickupManager == null)
            {
                pickupManager = GetComponent<KmsPickupManager>();
            }

            if (monsterSpawner == null)
            {
                monsterSpawner = FindFirstObjectByType<KmsMonsterSpawner>();
            }

            if (weaponInventory == null && pickupManager != null)
            {
                weaponInventory = pickupManager.WeaponInventory;
            }

            if (weaponInventory == null)
            {
                weaponInventory = FindFirstObjectByType<WeaponInventory>();
            }

            if (pickupManager == null)
            {
                if (logErrors)
                {
                    Debug.LogError("[KMS] 무기 픽업을 관리할 KmsPickupManager가 없습니다.", this);
                }

                return false;
            }

            if (weaponInventory == null)
            {
                if (logErrors)
                {
                    Debug.LogError("[KMS] 무기 획득에 사용할 WeaponInventory를 찾을 수 없습니다.", this);
                }

                return false;
            }

            if (dropTable == null)
            {
                if (logErrors)
                {
                    Debug.LogError("[KMS] 무기 드롭 테이블 참조가 없습니다.", this);
                }

                return false;
            }

            return true;
        }

        private bool IsRegisteredCatalogWeapon(WeaponData selectedWeapon)
        {
            ItemCatalog catalog = ItemCatalog.Instance;
            if (catalog == null || !catalog.TryGetWeapon(selectedWeapon.id, out WeaponData catalogWeapon))
            {
                Debug.LogError(
                    $"[KMS] 무기 ID '{selectedWeapon.id}'가 HDY ItemCatalog에 등록되어 있지 않습니다.",
                    this);
                return false;
            }

            if (catalogWeapon != selectedWeapon)
            {
                Debug.LogError(
                    $"[KMS] 무기 ID '{selectedWeapon.id}'의 드롭 테이블 SO와 ItemCatalog SO가 서로 다릅니다.",
                    this);
                return false;
            }

            return true;
        }
    }
}
