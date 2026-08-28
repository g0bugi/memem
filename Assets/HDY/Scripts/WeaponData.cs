using UnityEngine;

/// <summary>
/// 무기 하나의 전체 데이터. ScriptableObject 에셋으로 만들어 ItemCatalog에 등록해두면
/// WeaponInventory/PlayerAttack이 id로 조회해서 그대로 사용한다.
/// </summary>
[CreateAssetMenu(fileName = "NewWeaponData", menuName = "HDY/Weapon Data", order = 0)]
public class WeaponData : ScriptableObject
{
    [Header("Info")]
    [Tooltip("인벤토리/카탈로그 조회에 쓰이는 고유 id (예: \"dagger\")")]
    public string id;
    public string weaponName = "New Weapon";
    public Sprite icon;

    [Header("Type")]
    public WeaponAttackType attackType = WeaponAttackType.Melee;
    public WeaponAimType aimType = WeaponAimType.MouseTracking;

    [Header("Timing")]
    [Tooltip("공격 사이의 쿨타임(초)")]
    public float cooldown = 1f;

    [Header("Melee Hitbox (부채꼴)")]
    [Tooltip("캐릭터 중심에서 판정이 시작되는 반지름(이 반지름 안쪽은 판정에서 제외)")]
    public float innerRadius = 1f;
    [Tooltip("캐릭터 중심에서 판정이 끝나는 반지름(사거리)")]
    public float outerRadius = 2f;
    [Tooltip("부채꼴의 전체 각도(도 단위)")]
    [Range(1f, 360f)]
    public float angle = 15f;

    [Header("Damage")]
    public float damage = 10f;

    [Header("Prefabs (오브젝트 풀링 대상)")]
    [Tooltip("근접공격 시 재생되는 순수 비주얼 이펙트 프리팹 (판정에는 관여하지 않음)")]
    public GameObject meleeImpactPrefab;
    [Tooltip("근접 이펙트가 화면에 유지되는 시간(초) 이후 풀로 반환된다")]
    public float meleeImpactLifetime = 0.2f;

    [Tooltip("원거리공격 시 발사되는 투사체 프리팹")]
    public GameObject projectilePrefab;

    [Header("Pooling")]
    [Tooltip("무기 획득 시 미리 생성해둘 풀 초기 개수")]
    public int poolPrewarmCount = 4;
}
