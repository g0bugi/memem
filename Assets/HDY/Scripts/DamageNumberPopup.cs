using System.Collections;
using TMPro;
using UnityEngine;

namespace HDY
{
    /// <summary>
    /// 데미지 숫자 팝업 하나(=풀링되는 인스턴스 하나)의 애니메이션을 담당한다.
    /// World Space Canvas + TextMeshProUGUI가 붙어있는 프리팹에 이 스크립트를 함께 붙여서 사용한다.
    /// 실제 생성/반환(풀링)은 DamageNumberManager + 기존 EffectPoolManager가 담당하고,
    /// 이 스크립트는 "제자리에서 팝(pop) → 잠깐 유지 → 축소되며 페이드아웃" 애니메이션만 책임진다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class DamageNumberPopup : MonoBehaviour
    {
        [Header("텍스트")]
        [SerializeField] private TextMeshProUGUI damageText;

        [Header("팝 애니메이션 (제자리에서 커졌다 사라짐 - 위치 이동 없음)")]
        [SerializeField, Min(0.01f)] private float popDuration = 0.12f;
        [SerializeField, Min(0f)] private float holdDuration = 0.15f;
        [SerializeField, Min(0.01f)] private float fadeDuration = 0.25f;
        [SerializeField, Min(0f)] private float startScale = 0.3f;
        [SerializeField, Min(0f)] private float popScale = 1.2f;
        [SerializeField, Min(0f)] private float settleScale = 1f;

        private CanvasGroup canvasGroup;
        private Coroutine animationRoutine;
        private Camera targetCamera;

        /// <summary>애니메이션 전체 길이(초). DamageNumberManager가 풀 반환 타이밍을 잡는 데 사용한다.</summary>
        public float TotalDuration => popDuration + holdDuration + fadeDuration;

        private void Awake()
        {
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }
        }

        private void LateUpdate()
        {
            // World Space Canvas가 항상 카메라를 정면으로 바라보도록(빌보드) 회전만 맞춰준다.
            if (targetCamera == null)
            {
                targetCamera = Camera.main;
            }

            if (targetCamera != null)
            {
                transform.rotation = targetCamera.transform.rotation;
            }
        }

        /// <summary>텍스트를 지정하고 팝 애니메이션을 처음부터 재생한다.</summary>
        public void Play(string text)
        {
            if (damageText != null)
            {
                damageText.text = text;
            }

            if (animationRoutine != null)
            {
                StopCoroutine(animationRoutine);
            }

            animationRoutine = StartCoroutine(AnimateRoutine());
        }

        private IEnumerator AnimateRoutine()
        {
            canvasGroup.alpha = 1f;

            float elapsed = 0f;
            while (elapsed < popDuration)
            {
                elapsed += Time.deltaTime;
                float t = EaseOutCubic(Mathf.Clamp01(elapsed / popDuration));
                transform.localScale = Vector3.one * Mathf.LerpUnclamped(startScale, popScale, t);
                yield return null;
            }

            const float settleDuration = 0.06f;
            elapsed = 0f;
            while (elapsed < settleDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / settleDuration);
                transform.localScale = Vector3.one * Mathf.Lerp(popScale, settleScale, t);
                yield return null;
            }
            transform.localScale = Vector3.one * settleScale;

            if (holdDuration > 0f)
            {
                yield return new WaitForSeconds(holdDuration);
            }

            elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / fadeDuration);
                canvasGroup.alpha = 1f - t;
                transform.localScale = Vector3.Lerp(Vector3.one * settleScale, Vector3.zero, t);
                yield return null;
            }

            canvasGroup.alpha = 0f;
            animationRoutine = null;
        }

        private static float EaseOutCubic(float value)
        {
            float inverse = 1f - Mathf.Clamp01(value);
            return 1f - (inverse * inverse * inverse);
        }
    }
}
