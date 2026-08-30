using UnityEngine;

/// <summary>
/// 씬에 종속되지 않는 플레이어 진행 데이터(골드, 스탯 강화 단계)를 PlayerPrefs에 영구 저장하는 정적 클래스.
/// WeaponSelectScene처럼 Player 오브젝트가 아예 없는 씬에서도 골드/강화 단계를 조회·수정할 수 있도록
/// PlayerStats와 분리해서 둔다. PlayerStats.AddGold/SpendGold는 내부적으로 이 클래스에 위임한다.
/// </summary>
public static class PlayerProgress
{
    public enum StatType
    {
        Health,
        MoveSpeed,
        AttackPower
    }

    public const int MaxUpgradeLevel = 7;

    /// <summary>단계별 강화 비용(1단계 진입 비용 → 7단계 진입 비용 순). 밸런스 조정은 이 배열 숫자만 바꾸면 된다.</summary>
    private static readonly int[] UpgradeCosts = { 1000, 5000, 10000, 100000, 500000, 1000000, 5000000 };

    private const string GoldPrefsKey = "HDY_Gold";
    private const string HealthLevelPrefsKey = "HDY_UpgradeLevel_Health";
    private const string MoveSpeedLevelPrefsKey = "HDY_UpgradeLevel_MoveSpeed";
    private const string AttackPowerLevelPrefsKey = "HDY_UpgradeLevel_AttackPower";

    public static int Gold => Mathf.Max(0, PlayerPrefs.GetInt(GoldPrefsKey, 0));

    public static void AddGold(int amount)
    {
        if (amount <= 0) return;
        int newGold = Mathf.Max(0, Gold + amount);
        PlayerPrefs.SetInt(GoldPrefsKey, newGold);
        PlayerPrefs.Save();
    }

    public static bool SpendGold(int amount)
    {
        if (amount <= 0 || Gold < amount) return false;
        PlayerPrefs.SetInt(GoldPrefsKey, Gold - amount);
        PlayerPrefs.Save();
        return true;
    }

    public static int GetLevel(StatType stat)
    {
        return Mathf.Clamp(PlayerPrefs.GetInt(GetLevelKey(stat), 0), 0, MaxUpgradeLevel);
    }

    /// <summary>다음 단계로 올리는 데 필요한 골드. 이미 최대 단계면 -1을 반환한다.</summary>
    public static int GetNextUpgradeCost(StatType stat)
    {
        int level = GetLevel(stat);
        if (level >= MaxUpgradeLevel) return -1;
        return UpgradeCosts[level];
    }

    /// <summary>골드를 소비해 해당 스탯을 1단계 강화한다. 골드가 부족하거나 이미 최대 단계면 실패한다.</summary>
    public static bool TryUpgrade(StatType stat)
    {
        int cost = GetNextUpgradeCost(stat);
        if (cost < 0) return false;
        if (!SpendGold(cost)) return false;

        int level = GetLevel(stat);
        PlayerPrefs.SetInt(GetLevelKey(stat), level + 1);
        PlayerPrefs.Save();
        return true;
    }

    private static string GetLevelKey(StatType stat)
    {
        switch (stat)
        {
            case StatType.Health: return HealthLevelPrefsKey;
            case StatType.MoveSpeed: return MoveSpeedLevelPrefsKey;
            case StatType.AttackPower: return AttackPowerLevelPrefsKey;
            default: return HealthLevelPrefsKey;
        }
    }
}
