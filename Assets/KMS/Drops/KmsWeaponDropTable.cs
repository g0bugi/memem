using System.Collections.Generic;
using UnityEngine;

namespace KMS
{
    [CreateAssetMenu(fileName = "KmsWeaponDropTable", menuName = "KMS/Weapon Drop Table")]
    public sealed class KmsWeaponDropTable : ScriptableObject
    {
        [SerializeField] private List<WeaponData> droppableWeapons = new List<WeaponData>();

        public IReadOnlyList<WeaponData> DroppableWeapons => droppableWeapons;

        public bool HasConfiguredWeapon(ItemGrade grade)
        {
            for (int index = 0; index < droppableWeapons.Count; index++)
            {
                WeaponData weapon = droppableWeapons[index];
                if (IsValidForGrade(weapon, grade) && IsFirstIdOccurrence(index, weapon.id))
                {
                    return true;
                }
            }

            return false;
        }

        public bool TrySelect(
            ItemGrade grade,
            IReadOnlyList<ActiveWeapon> ownedWeapons,
            float unitRoll,
            out WeaponData selectedWeapon)
        {
            selectedWeapon = null;
            int eligibleCount = CountEligibleWeapons(grade, ownedWeapons);
            if (eligibleCount == 0)
            {
                return false;
            }

            float clampedRoll = Mathf.Clamp(unitRoll, 0f, 0.99999994f);
            int selectedIndex = Mathf.FloorToInt(clampedRoll * eligibleCount);
            int eligibleIndex = 0;

            for (int index = 0; index < droppableWeapons.Count; index++)
            {
                WeaponData weapon = droppableWeapons[index];
                if (!IsEligible(index, weapon, grade, ownedWeapons))
                {
                    continue;
                }

                if (eligibleIndex == selectedIndex)
                {
                    selectedWeapon = weapon;
                    return true;
                }

                eligibleIndex++;
            }

            return false;
        }

        private int CountEligibleWeapons(ItemGrade grade, IReadOnlyList<ActiveWeapon> ownedWeapons)
        {
            int count = 0;
            for (int index = 0; index < droppableWeapons.Count; index++)
            {
                WeaponData weapon = droppableWeapons[index];
                if (IsEligible(index, weapon, grade, ownedWeapons))
                {
                    count++;
                }
            }

            return count;
        }

private bool IsEligible(
    int index,
    WeaponData weapon,
    ItemGrade grade,
    IReadOnlyList<ActiveWeapon> ownedWeapons)
{
    // 무기는 중복 획득을 허용하므로(별도 슬롯 추가 방식) 이미 보유 중이어도 드롭 후보에서 제외하지 않는다.
    return IsValidForGrade(weapon, grade)
        && IsFirstIdOccurrence(index, weapon.id);
}

        private bool IsFirstIdOccurrence(int index, string weaponId)
        {
            for (int previousIndex = 0; previousIndex < index; previousIndex++)
            {
                WeaponData previousWeapon = droppableWeapons[previousIndex];
                if (previousWeapon != null && previousWeapon.id == weaponId)
                {
                    return false;
                }
            }

            return true;
        }

        private static bool IsValidForGrade(WeaponData weapon, ItemGrade grade)
        {
            return weapon != null
                && !string.IsNullOrWhiteSpace(weapon.id)
                && weapon.grade == grade;
        }


    }
}
