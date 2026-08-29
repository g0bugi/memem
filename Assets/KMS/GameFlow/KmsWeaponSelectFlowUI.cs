using UnityEngine;
using UnityEngine.UI;

namespace KMS
{
    /// <summary>캐릭터 선택 → 스테이지 선택 흐름을 담당한다.</summary>
    [DisallowMultipleComponent]
    public sealed class KmsWeaponSelectFlowUI : MonoBehaviour
    {
        [SerializeField] private GameObject characterSelectionPanel;
        [SerializeField] private GameObject stageSelectionPanel;
        [SerializeField] private Text selectedCharacterText;
        [SerializeField] private KmsSceneNavigator sceneNavigator;

        private void Start()
        {
            ShowCharacterSelection();
        }

        public void SelectDaggerCharacter()
        {
            KmsCharacterSelectionState.SelectDaggerMan07();
            ShowStageSelection();
        }

        public void SelectBowCharacter()
        {
            KmsCharacterSelectionState.SelectBowMan06();
            ShowStageSelection();
        }

        public void ShowCharacterSelection()
        {
            if (characterSelectionPanel != null) characterSelectionPanel.SetActive(true);
            if (stageSelectionPanel != null) stageSelectionPanel.SetActive(false);
        }

        public void ShowStageSelection()
        {
            if (characterSelectionPanel != null) characterSelectionPanel.SetActive(false);
            if (stageSelectionPanel != null) stageSelectionPanel.SetActive(true);

            if (selectedCharacterText != null)
            {
                selectedCharacterText.text = "선택 무기  ·  " +
                    (KmsCharacterSelectionState.StartingWeaponId == "bow" ? "활" : "단검");
            }
        }

        public void EnterStageOne()
        {
            if (sceneNavigator != null)
            {
                sceneNavigator.OpenGameScene();
            }
        }
    }
}
