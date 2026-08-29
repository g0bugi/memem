using System.Collections.Generic;
using UnityEngine;
using HDY;

/// <summary>
/// 보유 무기 하나의 런타임 상태(쿨타임 타이머)를 담는다.
/// </summary>
public class ActiveWeapon
{
    public WeaponData Data { get; }
    public float CooldownTimer { get; set; }

    /// <summary>씬 시작 시 WeaponInventory.weaponIds로 자동 지급된 시작무기면 true.
    /// 이후 드랍 등으로 런타임에 획득한 무기는 항상 false — 궤도 아이콘 표시(WeaponOrbitDisplay) 등에서
    /// "시작무기는 제외"를 판단하는 용도로 쓴다.</summary>
    public bool IsStartingWeapon { get; }

    public ActiveWeapon(WeaponData data, bool isStartingWeapon = false)
    {
        Data = data;
        CooldownTimer = 0f;
        IsStartingWeapon = isStartingWeapon;
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

    /// <summary>무기를 새로 획득할 때마다 호출된다. UI(쿨타임 슬롯 등)가 이 이벤트만 구독하면
    /// 씬 시작 시 지급되는 기본무기와 이후 런타임 획득 무기를 동일한 경로로 처리할 수 있다.</summary>
    public event System.Action<ActiveWeapon> WeaponAcquired;

    private void Start()
    {
        foreach (string id in weaponIds)
        {
            AcquireWeapon(id, isStartingWeapon: true);
        }
    }

    /// <summary>새 무기를 인벤토리에 추가한다. 아이템 획득 등 외부에서 호출한다.
    /// 중복 획득도 허용한다: 같은 id를 다시 넘기면 별도의 ActiveWeapon 인스턴스가 하나 더 추가되어
    /// HUD에 새 슬롯이 생기고 독립적인 쿨타임으로 동작한다(무기 스택/강화가 아니라 개별 인스턴스 추가).</summary>
    public void AcquireWeapon(string weaponId)
    {
        AcquireWeapon(weaponId, isStartingWeapon: false);
    }

    private void AcquireWeapon(string weaponId, bool isStartingWeapon)
    {
        if (string.IsNullOrEmpty(weaponId)) return;

        if (ItemCatalog.Instance == null || !ItemCatalog.Instance.TryGetWeapon(weaponId, out WeaponData data))
        {
            Debug.LogWarning($"[WeaponInventory] ItemCatalog에서 무기 id '{weaponId}' 를 찾을 수 없습니다.");
            return;
        }

        var newWeapon = new ActiveWeapon(data, isStartingWeapon);
        activeWeapons.Add(newWeapon);
        PrewarmPoolsFor(data);

        if (data.attackType == WeaponAttackType.Orbit)
        {
            SpawnOrbitWeapon(data);
        }

        WeaponAcquired?.Invoke(newWeapon);
    }

private void PrewarmPoolsFor(WeaponData data)
{
    if (data.ResolvedMeleeImpactPrefab != null && EffectPoolManager.Instance != null)
    {
        EffectPoolManager.Instance.Prewarm(data.ResolvedMeleeImpactPrefab, data.poolPrewarmCount);
    }

    if (data.ResolvedFireFloorPrefab != null && EffectPoolManager.Instance != null)
    {
        EffectPoolManager.Instance.Prewarm(data.ResolvedFireFloorPrefab, data.poolPrewarmCount);
    }

    if (data.ResolvedProjectilePrefab != null && ProjectilePoolManager.Instance != null)
    {
        ProjectilePoolManager.Instance.Prewarm(data.ResolvedProjectilePrefab, data.poolPrewarmCount);
    }
}

private void SpawnOrbitWeapon(WeaponData data)
{
    GameObject controllerObj = new GameObject($"OrbitWeapon_{data.id}");
    OrbitWeaponController controller = controllerObj.AddComponent<OrbitWeaponController>();
    PlayerStats stats = GetComponent<PlayerStats>();
    ComboManager comboManager = GetComponent<ComboManager>();
    controller.Setup(transform, data, targetLayers, stats, comboManager);
}

}
