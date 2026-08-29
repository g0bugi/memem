using UnityEngine;
using TMPro;

namespace HDY
{
    /// <summary>
    /// 플레이어의 콤보를 TMP 텍스트에 표시한다. PlayerHUD의 T_Combo 오브젝트에 붙여서 사용한다.
    /// PlayerGoldUI와 동일한 폴링 방식으로 매 프레임 값을 확인해서 바뀌었을 때만 갱신한다.
    /// 콤보 100단위(ComboTier)마다 폰트 크기가 fontSizePerTier만큼 커지고,
    /// 콤보가 초기화되면 다음 프레임에 즉시 기본 폰트 크기로 돌아간다.
    /// </summary>
    [RequireComponent(typeof(TMP_Text))]
    public class ComboUI : MonoBehaviour
    {
        [Header("Target")]
        [Tooltip("비워두면 Player 태그를 가진 오브젝트에서 ComboManager를 자동으로 찾는다.")]
        [SerializeField] private ComboManager target;

        [Header("폰트 크기 (100콤보당 증가)")]
        [SerializeField] private float fontSizePerTier = 10f;

        private TMP_Text comboText;
        private float baseFontSize;
        private int lastCombo = int.MinValue;

        private void Awake()
        {
            comboText = GetComponent<TMP_Text>();
            baseFontSize = comboText.fontSize;
        }

        private void Start()
        {
            if (target == null)
            {
                GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
                if (playerObj != null)
                {
                    target = playerObj.GetComponent<ComboManager>();
                }
            }

            RefreshText();
        }

        private void Update()
        {
            if (target == null) return;
            if (target.Combo == lastCombo) return;

            RefreshText();
        }

        private void RefreshText()
        {
            if (comboText == null) return;

            lastCombo = target != null ? target.Combo : 0;
            int tier = target != null ? target.ComboTier : 0;

            comboText.text = lastCombo.ToString();
            comboText.fontSize = baseFontSize + tier * fontSizePerTier;
        }
    }
}
