using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 보유 무기 하나의 런타임 상태(쿨타임 타이머)를 담는다.
/// </summary>
public class ActiveWeapon
{
    public WeaponData Data { get; }
    public float CooldownTimer { get; set; }

    public ActiveWeapon(WeaponData data)
    {
        Data = data;
        CooldownTimer = 0f;
    }
}

/// <summary>
/// 플레이어가 보유한 무기 id 목록. 각 id는 ItemCatalog에서 조회해
/// 쿨타임 타이머를 갖는 런타임 인스턴스(ActiveWeapon)로 변환된다.
/// 기본값에 기본무기(dagger)가 들어있어 씬 시작 시 자동으로 지급된다.
/// </summary>
public class WeaponInventory : MonoBehaviour
{
    [Tooltip("보유한 무기 id 목록. 기본값의 dagger는 씬 시작 시 자동 지급되는 기본무기다.")]
    [SerializeField] private List<string> weaponIds = new List<string> { "dagger" };
    [Tooltip("공격 판정이 대상으로 삼는 레이어(예: Enemy)")]
    [SerializeField] private LayerMask targetLayers;

    public LayerMask TargetLayers => targetLayers;

    private readonly List<ActiveWeapon> activeWeapons = new List<ActiveWeapon>();
    public IReadOnlyList<ActiveWeapon> ActiveWeapons => activeWeapons;

    private void Start()
    {
        foreach (string id in weaponIds)
        {
            AcquireWeapon(id);
        }
    }

    /// <summary>새 무기를 인벤토리에 추가한다. 아이템 획득 등 외부에서 호출한다.</summary>
public void AcquireWeapon(string weaponId)
{
    if (string.IsNullOrEmpty(weaponId)) return;

    if (activeWeapons.Exists(w => w.Data.id == weaponId))
    {
        return; // 이미 보유 중
    }

    if (ItemCatalog.Instance == null || !ItemCatalog.Instance.TryGetWeapon(weaponId, out WeaponData data))
    {
        Debug.LogWarning($"[WeaponInventory] ItemCatalog에서 무기 id '{weaponId}' 를 찾을 수 없습니다.");
        return;
    }

    activeWeapons.Add(new ActiveWeapon(data));
    PrewarmPoolsFor(data);

    if (data.attackType == WeaponAttackType.Orbit)
    {
        SpawnOrbitWeapon(data);
    }
}

    private void PrewarmPoolsFor(WeaponData data)
    {
        if (data.meleeImpactPrefab != null && EffectPoolManager.Instance != null)
        {
            EffectPoolManager.Instance.Prewarm(data.meleeImpactPrefab, data.poolPrewarmCount);
        }

        if (data.projectilePrefab != null && ProjectilePoolManager.Instance != null)
        {
            ProjectilePoolManager.Instance.Prewarm(data.projectilePrefab, data.poolPrewarmCount);
        }
    }

private void SpawnOrbitWeapon(WeaponData data)
{
    GameObject controllerObj = new GameObject($"OrbitWeapon_{data.id}");
    OrbitWeaponController controller = controllerObj.AddComponent<OrbitWeaponController>();
    controller.Setup(transform, data, targetLayers);
}

}
