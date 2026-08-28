using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 모든 무기(아이템) 데이터를 미리 등록해두는 목록.
/// Resources 폴더에 배치해두면 코드에서 자동으로 로드하는 싱글톤으로 접근한다.
/// id 기준 Dictionary를 한 번 빌드해두고 이후에는 O(1)로 조회한다.
/// </summary>
[CreateAssetMenu(fileName = "ItemCatalog", menuName = "HDY/Item Catalog", order = -1)]
public class ItemCatalog : ScriptableObject
{
    private const string ResourcePath = "ItemCatalog";

    [SerializeField] private List<WeaponData> allWeapons = new List<WeaponData>();

    private Dictionary<string, WeaponData> lookup;

    private static ItemCatalog instance;
    public static ItemCatalog Instance
    {
        get
        {
            if (instance == null)
            {
                instance = Resources.Load<ItemCatalog>(ResourcePath);
                if (instance == null)
                {
                    Debug.LogError($"[ItemCatalog] Resources/{ResourcePath}.asset 을 찾을 수 없습니다. Assets/HDY/Resources/ 아래에 만들어야 합니다.");
                }
            }
            return instance;
        }
    }

    private void BuildLookupIfNeeded()
    {
        if (lookup != null) return;

        lookup = new Dictionary<string, WeaponData>();
        foreach (var weapon in allWeapons)
        {
            if (weapon == null || string.IsNullOrEmpty(weapon.id)) continue;

            if (!lookup.ContainsKey(weapon.id))
            {
                lookup.Add(weapon.id, weapon);
            }
            else
            {
                Debug.LogWarning($"[ItemCatalog] 중복된 무기 id 발견: '{weapon.id}' ({weapon.name})");
            }
        }
    }

    public bool TryGetWeapon(string id, out WeaponData data)
    {
        BuildLookupIfNeeded();
        return lookup.TryGetValue(id, out data);
    }
}
