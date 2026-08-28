using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 무기 하나의 쿨타임 상태를 보여주는 슬롯 UI.
/// 배경 이미지, 무기 아이콘, 쿨타임 오버레이(라디얼 채우기 이미지), 남은 쿨타임 숫자를 표시한다.
/// WeaponSlotGrid가 무기를 획득할 때마다 프리팹으로 생성해서 Setup()을 호출해준다.
/// </summary>
public class WeaponSlotUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Image iconImage;
    [SerializeField] private Image cooldownFillImage;
    [SerializeField] private TMP_Text cooldownText;

    private ActiveWeapon weapon;

    /// <summary>이 슬롯이 어떤 무기를 표시할지 지정한다. WeaponSlotGrid가 생성 직후 호출한다.</summary>
    public void Setup(ActiveWeapon activeWeapon)
    {
        weapon = activeWeapon;

        if (iconImage != null)
        {
            iconImage.sprite = weapon.Data.icon;
            iconImage.enabled = weapon.Data.icon != null;
        }

        if (cooldownFillImage != null)
        {
            cooldownFillImage.fillAmount = 0f;
            cooldownFillImage.gameObject.SetActive(false);
        }

        if (cooldownText != null)
        {
            cooldownText.text = string.Empty;
            cooldownText.gameObject.SetActive(false);
        }
    }

    private void Update()
    {
        if (weapon == null) return;

        float cooldown = weapon.Data.cooldown;
        float remaining = Mathf.Max(0f, weapon.CooldownTimer);
        bool onCooldown = cooldown > 0f && remaining > 0f;

        if (cooldownFillImage != null)
        {
            cooldownFillImage.gameObject.SetActive(onCooldown);
            if (onCooldown)
            {
                cooldownFillImage.fillAmount = Mathf.Clamp01(remaining / cooldown);
            }
        }

        if (cooldownText != null)
        {
            cooldownText.gameObject.SetActive(onCooldown);
            if (onCooldown)
            {
                cooldownText.text = remaining.ToString("0.0");
            }
        }
    }
}
