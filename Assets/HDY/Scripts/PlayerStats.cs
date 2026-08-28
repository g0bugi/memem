using UnityEngine;

/// <summary>
/// 캐릭터의 기본 스탯(체력, 이동속도, 공격력, 골드)을 보관하고 관리한다.
/// 이동속도는 관성 이동을 위해 최소/최대 두 값으로 관리한다.
/// </summary>
public class PlayerStats : MonoBehaviour
{
    [Header("Health")]
    [SerializeField] private float maxHealth = 100f;

    [Header("Move Speed (관성 이동)")]
    [Tooltip("이동을 시작하는 순간의 속도")]
    [SerializeField] private float minMoveSpeed = 2f;
    [Tooltip("가속이 끝났을 때 도달하는 최고 속도")]
    [SerializeField] private float maxMoveSpeed = 6f;
    [Tooltip("minMoveSpeed에서 maxMoveSpeed까지 가속하는 데 걸리는 시간(초)")]
    [SerializeField] private float accelerationTime = 1f;
    [Tooltip("입력이 없을 때 정지하는 데 걸리는 시간(초)")]
    [SerializeField] private float decelerationTime = 1f;

    [Header("Attack")]
    [SerializeField] private float attackPower = 10f;

    [Header("Gold")]
    [SerializeField] private int gold = 0;

    public float MaxHealth => maxHealth;
    public float CurrentHealth { get; private set; }

    public float MinMoveSpeed => minMoveSpeed;
    public float MaxMoveSpeed => maxMoveSpeed;
    public float AccelerationTime => accelerationTime;
    public float DecelerationTime => decelerationTime;

    public float AttackPower => attackPower;
    public int Gold => gold;

    private void Awake()
    {
        CurrentHealth = maxHealth;
    }

    public void TakeDamage(float amount)
    {
        if (amount <= 0f) return;
        CurrentHealth = Mathf.Max(0f, CurrentHealth - amount);
    }

    public void Heal(float amount)
    {
        if (amount <= 0f) return;
        CurrentHealth = Mathf.Min(maxHealth, CurrentHealth + amount);
    }

    public void AddGold(int amount)
    {
        gold = Mathf.Max(0, gold + amount);
    }

    public bool SpendGold(int amount)
    {
        if (amount <= 0 || gold < amount) return false;
        gold -= amount;
        return true;
    }
}
