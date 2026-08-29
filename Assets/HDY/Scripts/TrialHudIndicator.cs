using UnityEngine;
using UnityEngine.UI;

namespace HDY
{
    /// <summary>
    /// 시련(Trial) 단계에 따라 캐릭터 HUD 이미지의 활성화 여부와 색상을 갱신한다.
    /// - 시련 0단계: 이미지 비활성화(아직 시련이 시작되지 않음).
    /// - 시련 1단계: 이미지 활성화(기본 색상, baseColor).
    /// - 시련 2~10단계: 9단계로 균등하게 나눠서 기본 색상에서 점점 빨간색(maxColor)으로 물들어가고,
    ///   10단계에서 완전한 빨간색이 된다.
    /// 이미지 배치와 연결(어떤 Image를 쓸지)은 직접 하기로 했으므로, targetImage 필드에 인스펙터에서
    /// 대상 Image 컴포넌트를 드래그해서 연결해 쓰면 된다. TrialManager.Instance.CurrentLevel을
    /// ComboUI/PlayerGoldUI와 동일한 패턴(Update에서 값이 바뀔 때만 갱신)으로 폴링한다.
    /// </summary>
    public class TrialHudIndicator : MonoBehaviour
    {
        [Tooltip("시련 단계에 따라 활성화/색상이 바뀔 대상 Image. 인스펙터에서 직접 연결한다.")]
        [SerializeField] private Image targetImage;

        [Tooltip("시련 1단계일 때(아직 빨갛게 물들기 전)의 기본 색상.")]
        [SerializeField] private Color baseColor = Color.white;

        [Tooltip("시련 10단계(최대)에 도달했을 때의 색상.")]
        [SerializeField] private Color maxColor = Color.red;

        private int lastAppliedLevel = -1;

        private void Awake()
        {
            if (targetImage != null)
            {
                targetImage.color = baseColor;
                targetImage.enabled = false;
            }
        }

        private void Update()
        {
            if (targetImage == null) return;

            int level = TrialManager.Instance != null ? TrialManager.Instance.CurrentLevel : 0;
            if (level == lastAppliedLevel) return;

            lastAppliedLevel = level;
            ApplyLevel(level);
        }

        private void ApplyLevel(int level)
        {
            if (level <= 0)
            {
                targetImage.enabled = false;
                targetImage.color = baseColor;
                return;
            }

            targetImage.enabled = true;

            // 1단계 = 기본 색상(t=0). 2~10단계 = 9구간으로 나눠서 점점 빨갛게, 10단계에서 t=1(완전한 빨간색).
            float t = Mathf.Clamp01((level - 1) / 9f);
            targetImage.color = Color.Lerp(baseColor, maxColor, t);
        }
    }
}
