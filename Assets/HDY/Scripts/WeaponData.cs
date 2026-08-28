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
    [Tooltip("아이템 등급. 1(신화)이 가장 높고 7(일반)이 가장 낮다.")]
    public ItemGrade grade = ItemGrade.Common;

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

    [Header("Ranged (투사체)")]
    [Tooltip("투사체 이동 속도(유니트/초)")]
    public float projectileSpeed = 10f;
    [Tooltip("적을 맞추지 못했을 때 투사체가 유지되는 최대 시간(초). 이 시간이 지나면 자동으로 풀로 반환된다.")]
    public float projectileLifetime = 5f;

    [Header("Orbit (요술봉 등 패시브 구슬)")]
    [Tooltip("캐릭터 주변을 도는 구슬 개수")]
    public int orbCount = 3;
    [Tooltip("캐릭터 중심에서 구슬까지의 거리")]
    public float orbRadius = 2f;
    [Tooltip("구슬이 한 바퀴 도는 데 걸리는 시간(초)")]
    public float orbRotationPeriod = 3f;
    [Tooltip("구슬 비주얼 프리팹")]
    public GameObject orbPrefab;

    [Header("Area / Meteor (범위 공격)")]
    [Tooltip("착탄 지점에서 폭발 판정이 적용되는 반경 (outerRadius는 목표를 찾는 탐색 반경으로 재사용)")]
    public float explosionRadius = 2f;
    [Tooltip("하늘에서 떨어져 착탄하기까지 걸리는 시간(초)")]
    public float fallDuration = 0.5f;
    [Tooltip("폭발 후 생기는 지속피해 장판 비주얼 프리팹")]
    public GameObject fireFloorPrefab;
    [Tooltip("불장판이 유지되는 시간(초)")]
    public float fireFloorDuration = 2f;
    [Tooltip("불장판 틱 1회당 데미지")]
    public float fireFloorTickDamage = 10f;
    [Tooltip("불장판 데미지 틱 간격(초)")]
    public float fireFloorTickInterval = 0.5f;

    [Header("Pooling")]
    [Tooltip("무기 획득 시 미리 생성해둘 풀 초기 개수")]
    public int poolPrewarmCount = 4;
}
