using UnityEngine;
using UnityEngine.UI;

namespace KMS
{
    /// <summary>
    /// WeaponSelectScene에서 골드로 체력/이동속도/공격력을 강화하는 UI.
    /// 최소 한 번 스테이지를 마치고 돌아온 뒤부터 표시된다(KmsSceneNavigator.HasFinishedFirstRun).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class KmsStatUpgradePanelUI : MonoBehaviour
    {
[Header("Panel")]
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private Text goldText;
        [SerializeField] private Button closeButton;

        [Header("Health")]
        [SerializeField] private Text healthLevelText;
        [SerializeField] private Text healthCostText;
        [SerializeField] private Button healthUpgradeButton;

        [Header("Move Speed")]
        [SerializeField] private Text moveSpeedLevelText;
        [SerializeField] private Text moveSpeedCostText;
        [SerializeField] private Button moveSpeedUpgradeButton;

        [Header("Attack Power")]
        [SerializeField] private Text attackLevelText;
        [SerializeField] private Text attackCostText;
        [SerializeField] private Button attackUpgradeButton;

        
private void Start()
        {
            bool shouldShow = KmsSceneNavigator.HasFinishedFirstRun;
            if (panelRoot != null)
            {
                panelRoot.SetActive(shouldShow);
            }

            if (closeButton != null)
            {
                closeButton.onClick.AddListener(HandleClose);
            }

            if (!shouldShow)
            {
                return;
            }

            if (healthUpgradeButton != null)
            {
                healthUpgradeButton.onClick.AddListener(() => HandleUpgrade(PlayerProgress.StatType.Health));
            }
            if (moveSpeedUpgradeButton != null)
            {
                moveSpeedUpgradeButton.onClick.AddListener(() => HandleUpgrade(PlayerProgress.StatType.MoveSpeed));
            }
            if (attackUpgradeButton != null)
            {
                attackUpgradeButton.onClick.AddListener(() => HandleUpgrade(PlayerProgress.StatType.AttackPower));
            }

            Refresh();
        }

        private void HandleUpgrade(PlayerProgress.StatType stat)
        {
            PlayerProgress.TryUpgrade(stat);
            Refresh();
        }

private void HandleClose()
        {
            if (panelRoot != null)
            {
                panelRoot.SetActive(false);
            }
        }


        private void Refresh()
        {
            if (goldText != null)
            {
                goldText.text = $"보유 골드  {PlayerProgress.Gold}";
            }

            RefreshRow(PlayerProgress.StatType.Health, healthLevelText, healthCostText, healthUpgradeButton);
            RefreshRow(PlayerProgress.StatType.MoveSpeed, moveSpeedLevelText, moveSpeedCostText, moveSpeedUpgradeButton);
            RefreshRow(PlayerProgress.StatType.AttackPower, attackLevelText, attackCostText, attackUpgradeButton);
        }

        private void RefreshRow(PlayerProgress.StatType stat, Text levelText, Text costText, Button button)
        {
            int level = PlayerProgress.GetLevel(stat);
            int cost = PlayerProgress.GetNextUpgradeCost(stat);

            if (levelText != null)
            {
                levelText.text = $"Lv. {level} / {PlayerProgress.MaxUpgradeLevel}";
            }

            bool isMax = cost < 0;
            if (costText != null)
            {
                costText.text = isMax ? "MAX" : $"{cost} 골드";
            }

            if (button != null)
            {
                button.interactable = !isMax && PlayerProgress.Gold >= cost;

                Text buttonLabel = button.GetComponentInChildren<Text>(true);
                if (buttonLabel != null)
                {
                    buttonLabel.text = isMax ? "최대" : "강화";
                }
            }
        }
    }
}
