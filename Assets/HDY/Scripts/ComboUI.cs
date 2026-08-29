using UnityEngine;
using TMPro;
using System.Collections;

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

        [Header("증감 팝업 연출")]
        [Tooltip("콤보가 오르내릴 때 변화량(+N/-N) 팝업을 띄운다.")]
        [SerializeField] private bool showDeltaPopup = true;
        [SerializeField] private Color increaseColor = new Color(0.2f, 0.9f, 0.2f);
        [SerializeField] private Color decreaseColor = new Color(0.95f, 0.2f, 0.2f);
        [SerializeField] private float deltaPopupFontSize = 24f;
        [SerializeField] private float deltaPopupMoveDistance = 40f;
        [SerializeField] private float deltaPopupDuration = 0.6f;

        [Header("SizeUP 연출 (티어 상승 시)")]
        [Tooltip("100콤보 단위(ComboTier)가 오를 때 'SizeUP' 텍스트를 띄운다.")]
        [SerializeField] private bool showSizeUpPopup = true;
        [SerializeField] private string sizeUpText = "SizeUP";
        [SerializeField] private Color sizeUpColor = new Color(1f, 0.6f, 0f);
        [SerializeField] private float sizeUpFontSize = 28f;
        [SerializeField] private float sizeUpMoveDistance = 50f;
        [SerializeField] private float sizeUpDuration = 0.7f;

        private TMP_Text comboText;
        private float baseFontSize;
        private int lastCombo = int.MinValue;
        private int lastTier;
        private bool hasInitialized;

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

            int newCombo = target != null ? target.Combo : 0;
            int newTier = target != null ? target.ComboTier : 0;

            if (hasInitialized)
            {
                int delta = newCombo - lastCombo;
                if (showDeltaPopup && delta != 0)
                {
                    SpawnDeltaPopup(delta);
                }

                if (showSizeUpPopup && newTier > lastTier)
                {
                    SpawnSizeUpPopup();
                }
            }

            lastCombo = newCombo;
            lastTier = newTier;
            hasInitialized = true;

            comboText.text = lastCombo.ToString();
            comboText.fontSize = baseFontSize + newTier * fontSizePerTier;
        }

private void SpawnDeltaPopup(int delta)
        {
            string popupText = delta > 0 ? "+" + delta.ToString() : delta.ToString();
            Color color = delta > 0 ? increaseColor : decreaseColor;
            SpawnPopup(popupText, color, deltaPopupFontSize, deltaPopupMoveDistance, deltaPopupDuration);
        }

        private void SpawnSizeUpPopup()
        {
            SpawnPopup(sizeUpText, sizeUpColor, sizeUpFontSize, sizeUpMoveDistance, sizeUpDuration);
        }

        private void SpawnPopup(string text, Color color, float fontSize, float moveDistance, float duration)
        {
            var uiText = comboText as TMPro.TextMeshProUGUI;
            if (uiText == null) return;

            GameObject popupObj = new GameObject("ComboPopup", typeof(RectTransform));
            RectTransform popupRect = popupObj.GetComponent<RectTransform>();
            popupRect.SetParent(uiText.rectTransform.parent, false);
            popupRect.anchorMin = uiText.rectTransform.anchorMin;
            popupRect.anchorMax = uiText.rectTransform.anchorMax;
            popupRect.pivot = uiText.rectTransform.pivot;
            popupRect.anchoredPosition = uiText.rectTransform.anchoredPosition;
            popupRect.sizeDelta = uiText.rectTransform.sizeDelta;

            TMPro.TextMeshProUGUI popupText = popupObj.AddComponent<TMPro.TextMeshProUGUI>();
            popupText.font = uiText.font;
            popupText.fontStyle = uiText.fontStyle;
            popupText.alignment = uiText.alignment;
            popupText.fontSize = fontSize;
            popupText.color = color;
            popupText.text = text;
            popupText.raycastTarget = false;

            popupRect.SetAsLastSibling();

            StartCoroutine(AnimatePopup(popupRect, popupText, moveDistance, duration));
        }

        private IEnumerator AnimatePopup(RectTransform rect, TMPro.TextMeshProUGUI text, float moveDistance, float duration)
        {
            Vector2 startPos = rect.anchoredPosition;
            Vector2 endPos = startPos + new Vector2(0f, moveDistance);
            Color startColor = text.color;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                rect.anchoredPosition = Vector2.Lerp(startPos, endPos, t);
                Color c = startColor;
                c.a = Mathf.Lerp(1f, 0f, t);
                text.color = c;
                yield return null;
            }

            Destroy(rect.gameObject);
        }

    }
}
