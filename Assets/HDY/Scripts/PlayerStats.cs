using UnityEngine;

/// <summary>
/// 캐릭터의 기본 스탯(체력, 이동속도, 공격력, 골드)을 보관하고 관리한다.
/// 이동속도는 즉시 적용되며 관성이 없다.
/// </summary>
public class PlayerStats : MonoBehaviour
{
    [Header("Health (기본값, 강화 전)")]
    [SerializeField] private float baseMaxHealth = 100f;

    [Header("Move Speed (기본값, 강화 전)")]
    [SerializeField] private float baseMoveSpeed = 6f;

    [Header("Attack (기본값, 강화 전)")]
    [SerializeField] private float baseAttackPower = 10f;

    [Header("Upgrade Bonus (1단계당)")]
    [Tooltip("체력 강화 1단계당 증가량")]
    [SerializeField] private float healthPerLevel = 77f;
    [Tooltip("이동속도 강화 1단계당 증가율(기본값 기준 가산, 0.07 = 7%)")]
    [SerializeField] private float moveSpeedPercentPerLevel = 0.07f;
    [Tooltip("공격력 강화 1단계당 증가량")]
    [SerializeField] private float attackPowerPerLevel = 7f;

    public float MaxHealth { get; private set; }
    public float CurrentHealth { get; private set; }
    public float MoveSpeed { get; private set; }
    public float AttackPower { get; private set; }
    public int Gold => PlayerProgress.Gold;
    public bool IsDead { get; private set; }

    /// <summary>체력이 0이 되어 사망한 순간 딱 한 번 발동된다.</summary>
    public event System.Action Died;
    /// <summary>골드를 실제로 획득했을 때(양수만) 발동된다. 결과창에서 "이번 런 획득 골드"를 집계하는 용도.</summary>
    public event System.Action<int> GoldGained;

private void Awake()
    {
        int healthLevel = PlayerProgress.GetLevel(PlayerProgress.StatType.Health);
        int moveSpeedLevel = PlayerProgress.GetLevel(PlayerProgress.StatType.MoveSpeed);
        int attackLevel = PlayerProgress.GetLevel(PlayerProgress.StatType.AttackPower);

        MaxHealth = baseMaxHealth + healthPerLevel * healthLevel;
        MoveSpeed = baseMoveSpeed * (1f + moveSpeedPercentPerLevel * moveSpeedLevel);
        AttackPower = baseAttackPower + attackPowerPerLevel * attackLevel;

        CurrentHealth = MaxHealth;
        IsDead = false;
    }

public void TakeDamage(float amount)
    {
        if (IsDead || amount <= 0f) return;

        CurrentHealth = Mathf.Max(0f, CurrentHealth - amount);

        if (CurrentHealth <= 0f)
        {
            IsDead = true;
            Died?.Invoke();
        }
    }

public void Heal(float amount)
    {
        if (amount <= 0f || IsDead) return;
        CurrentHealth = Mathf.Min(MaxHealth, CurrentHealth + amount);
    }

public void AddGold(int amount)
    {
        if (amount <= 0) return;
        PlayerProgress.AddGold(amount);
        GoldGained?.Invoke(amount);
    }

public bool SpendGold(int amount)
    {
        return PlayerProgress.SpendGold(amount);
    }



}
