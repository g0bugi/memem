using UnityEngine;

/// <summary>
/// 무기 "계열"별로 공유되는 비주얼(아이콘, 근접 임팩트/원거리 투사체/궤도 구슬/불장판 프리팹)을
/// 묶어두는 프리셋. WeaponData.visualPreset에 연결해두면, WeaponData 쪽 개별 필드가 비어있을 때
/// (null) 이 프리셋 값으로 자동 대체된다(WeaponData의 Resolved* 프로퍼티 참고).
/// 프리셋 하나만 바꾸면 그 계열 무기 전부의 비주얼이 한번에 바뀐다.
/// 개별 무기에서 특정 필드만 다르게 하고 싶으면 WeaponData 쪽 필드를 직접 채우면 그 값이 우선한다(오버라이드).
/// </summary>
[CreateAssetMenu(fileName = "NewWeaponVisualPreset", menuName = "HDY/Weapon Visual Preset", order = 1)]
public class WeaponVisualPreset : ScriptableObject
{
    [Header("Info")]
    [Tooltip("에디터에서 구분하기 위한 프리셋 이름(예: 검류, 도끼류, 화살류, 마법류)")]
    public string presetName = "New Preset";

    [Header("Icon")]
    public Sprite icon;

    [Header("Melee")]
    public GameObject meleeImpactPrefab;
    public float meleeImpactLifetime = 0.2f;

    [Header("Ranged")]
    public GameObject projectilePrefab;

    [Header("Orbit")]
    public GameObject orbPrefab;

    [Header("Area / Meteor")]
    public GameObject explosionPrefab;

    [Header("Sound")]
    [Tooltip("이 프리셋을 쓰는 무기들의 공용 공격 사운드. 무기 자신의 attackSounds가 비어있을 때 대체로 사용된다.")]
    public AudioClip[] attackSounds;
public GameObject fireFloorPrefab;
}
