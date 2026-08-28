/// <summary>
/// 무기의 공격 방식(근접/원거리/범위/추적 등). 필요할 때마다 항목을 추가한다.
/// </summary>
public enum WeaponAttackType
{
    Melee,
    Ranged,
    Area,
    Homing
}

/// <summary>
/// 무기의 조준 방식(마우스추적/적추적/자기중심 범위 등).
/// </summary>
public enum WeaponAimType
{
    MouseTracking,
    EnemyTracking,
    AreaSelf
}
