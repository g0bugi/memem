using UnityEngine;

/// <summary>
/// 무기/공격 하나의 수치 데이터. ScriptableObject 에셋으로 만들어 두면
/// 기본공격 외에 다양한 무기·아이템에서 같은 구조로 재사용할 수 있다.
/// </summary>
[CreateAssetMenu(fileName = "NewWeaponData", menuName = "HDY/Weapon Data", order = 0)]
public class WeaponData : ScriptableObject
{
    [Header("Info")]
    public string weaponName = "Basic Attack";

    [Header("Timing")]
    [Tooltip("공격 사이의 쿨타임(초)")]
    public float cooldown = 1f;

    [Header("Hitbox (부채꼴)")]
    [Tooltip("캐릭터 중심에서 판정이 시작되는 반지름(이 반지름 안쪽은 판정에서 제외)")]
    public float innerRadius = 0.5f;
    [Tooltip("캐릭터 중심에서 판정이 끝나는 반지름(사거리)")]
    public float outerRadius = 2.5f;
    [Tooltip("부채꼴의 전체 각도(도 단위)")]
    [Range(1f, 360f)]
    public float angle = 60f;

    [Header("Damage")]
    public float damage = 10f;
}
