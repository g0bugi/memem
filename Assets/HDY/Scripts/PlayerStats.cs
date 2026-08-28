using UnityEngine;

/// <summary>
/// 캐릭터의 기본 스탯(체력, 이동속도, 공격력, 골드)을 보관하고 관리한다.
/// 이동속도는 즉시 적용되며 관성이 없다.
/// </summary>
public class PlayerStats : MonoBehaviour
{
    [Header("Health")]
    [SerializeField] private float maxHealth = 100f;

    [Header("Move Speed")]
    [SerializeField] private float moveSpeed = 6f;

    [Header("Attack")]
    [SerializeField] private float attackPower = 10f;

    [Header("Gold")]
    [SerializeField] private int gold = 0;

    private const string GoldPrefsKey = "HDY_Gold";

    public float MaxHealth => maxHealth;
    public float CurrentHealth { get; private set; }

    public float MoveSpeed => moveSpeed;

    public float AttackPower => attackPower;
    public int Gold => gold;

private void Awake()
    {
        CurrentHealth = maxHealth;
        gold = Mathf.Max(0, PlayerPrefs.GetInt(GoldPrefsKey, gold));
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
        SaveGold();
    }

public bool SpendGold(int amount)
    {
        if (amount <= 0 || gold < amount) return false;
        gold -= amount;
        SaveGold();
        return true;
    }

private void SaveGold()
    {
        PlayerPrefs.SetInt(GoldPrefsKey, gold);
        PlayerPrefs.Save();
    }

}
