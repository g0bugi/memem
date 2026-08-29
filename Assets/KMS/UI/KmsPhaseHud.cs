using UnityEngine;
using UnityEngine.UI;

namespace KMS
{
    [DisallowMultipleComponent]
    public sealed class KmsPhaseHud : MonoBehaviour
    {
        private const float HorizontalInset = 2f;
        private const float VerticalInset = 2f;

        [Header("References")]
        [SerializeField] private KmsWaveDirector waveDirector;
        [SerializeField] private Text phaseText;
        [SerializeField] private Image remainingFill;

        [Header("Presentation")]
        [SerializeField] private Color normalColor = new Color(1f, 0.78f, 0.22f, 1f);
        [SerializeField] private Color warningColor = new Color(0.95f, 0.28f, 0.12f, 1f);
        [SerializeField, Range(0.01f, 1f)] private float warningThreshold = 0.2f;

        public void Configure(
            KmsWaveDirector director,
            Text label,
            Image fill,
            Color phaseColor,
            Color phaseWarningColor,
            float phaseWarningThreshold = 0.2f)
        {
            waveDirector = director;
            phaseText = label;
            remainingFill = fill;
            normalColor = phaseColor;
            warningColor = phaseWarningColor;
            warningThreshold = Mathf.Clamp(phaseWarningThreshold, 0.01f, 1f);
            PrepareFillImage();
            if (Application.isPlaying)
            {
                UpdateView();
            }
            else
            {
                SetEditorPreview();
            }
        }

        private void Awake()
        {
            PrepareFillImage();
            UpdateView();
        }

        private void Update()
        {
            UpdateView();
        }

        private void PrepareFillImage()
        {
            if (remainingFill == null)
            {
                return;
            }

            remainingFill.type = Image.Type.Simple;
            remainingFill.fillAmount = 1f;
            remainingFill.raycastTarget = false;

            RectTransform fillRect = remainingFill.rectTransform;
            fillRect.anchorMin = new Vector2(0f, 0.5f);
            fillRect.anchorMax = new Vector2(0f, 0.5f);
            fillRect.pivot = new Vector2(0f, 0.5f);
        }

        private void UpdateView()
        {
            if (waveDirector != null && waveDirector.HasRunEnded)
            {
                return;
            }

            int waveNumber = waveDirector != null ? waveDirector.DisplayedWaveNumber : 0;
            float remaining = waveDirector != null
                ? waveDirector.WaveRemainingNormalized
                : 0f;

            if (phaseText != null)
            {
                phaseText.text = waveNumber > 0 ? $"WAVE {waveNumber}" : "WAVE -";
            }

            if (remainingFill == null)
            {
                return;
            }

            SetFillWidth(remaining);
            float colorProgress = Mathf.Clamp01(remaining / Mathf.Max(0.01f, warningThreshold));
            remainingFill.color = Color.Lerp(warningColor, normalColor, colorProgress);
        }

        private void SetFillWidth(float normalizedWidth)
        {
            RectTransform fillRect = remainingFill.rectTransform;
            RectTransform trackRect = fillRect.parent as RectTransform;
            if (trackRect == null)
            {
                return;
            }

            float innerWidth = Mathf.Max(0f, trackRect.rect.width - HorizontalInset * 2f);
            float innerHeight = Mathf.Max(0f, trackRect.rect.height - VerticalInset * 2f);
            fillRect.anchoredPosition = new Vector2(HorizontalInset, 0f);
            fillRect.sizeDelta = new Vector2(
                innerWidth * Mathf.Clamp01(normalizedWidth),
                innerHeight);
        }

        private void SetEditorPreview()
        {
            if (phaseText != null)
            {
                phaseText.text = "WAVE 1";
            }

            if (remainingFill != null)
            {
                SetFillWidth(1f);
                remainingFill.color = normalColor;
            }
        }
    }
}
