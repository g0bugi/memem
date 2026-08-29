using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 플레이어 아래에 고정되어 따라다니는 체력바(Slider) UI.
/// PlayerHUD 캔버스 아래 자식으로 존재하며, 매 프레임 플레이어의 화면 위치를 따라가고
/// PlayerStats의 현재/최대 체력 비율을 슬라이더 값에 반영한다.
/// Canvas의 Render Mode(Overlay/Camera/World Space) 어떤 것이든 동작하도록
/// RectTransformUtility.ScreenPointToLocalPointInRectangle로 좌표를 변환한다.
/// </summary>
[RequireComponent(typeof(Slider))]
public class PlayerHealthBarUI : MonoBehaviour
{
    [Header("Target")]
    [Tooltip("비워두면 Player 태그를 가진 오브젝트에서 PlayerStats를 자동으로 찾는다.")]
    [SerializeField] private PlayerStats target;

    [Header("Position")]
    [Tooltip("플레이어 기준 월드 공간 오프셋. 기본값은 플레이어 아래쪽으로 고정.")]
    [SerializeField] private Vector3 worldOffset = new Vector3(0f, -0.7f, 0f);

    [Header("Smoothing")]
    [Tooltip("따라가는 위치에 적용할 보간 시간(초). 플레이어가 FixedUpdate에서 MovePosition으로 이동하는 반면 이 UI는 LateUpdate(렌더 프레임)마다 위치를 갱신하기 때문에, 이동속도가 빨라질수록 물리 스텝 사이의 위치 점프가 커져 체력바가 떨리는 것처럼 보인다. SmoothDamp로 이 점프를 부드럽게 보간해서 완화한다. 0이면 보간 없이 즉시 따라간다.")]
    [SerializeField, Min(0f)] private float positionSmoothTime = 0.08f;

    private Vector3 smoothedWorldPos;
    private Vector3 smoothVelocity;
    private bool smoothedPosInitialized;


    private Slider slider;
    private RectTransform rectTransform;
    private Canvas canvas;
    private RectTransform canvasRect;
    private Camera mainCamera;

    private void Awake()
    {
        slider = GetComponent<Slider>();
        rectTransform = transform as RectTransform;
        canvas = GetComponentInParent<Canvas>();
        canvasRect = canvas != null ? canvas.transform as RectTransform : null;
        mainCamera = Camera.main;

        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.interactable = false;
    }

    private void Start()
    {
        if (target == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                target = playerObj.GetComponent<PlayerStats>();
            }
        }
    }

private void LateUpdate()
    {
        if (target == null || canvasRect == null) return;

        slider.value = target.MaxHealth > 0f ? target.CurrentHealth / target.MaxHealth : 0f;

        if (mainCamera == null)
        {
            mainCamera = Camera.main;
            if (mainCamera == null) return;
        }

        Vector3 desiredWorldPos = target.transform.position + worldOffset;

        if (!smoothedPosInitialized)
        {
            // 처음 따라붙는 순간에는 보간 없이 바로 실제 위치로 맞춰서, 원점에서
            // 미끄럼지는 등 초기 슬라이드를 방지한다.
            smoothedWorldPos = desiredWorldPos;
            smoothVelocity = Vector3.zero;
            smoothedPosInitialized = true;
        }
        else
        {
            smoothedWorldPos = Vector3.SmoothDamp(smoothedWorldPos, desiredWorldPos, ref smoothVelocity, positionSmoothTime);
        }

        Vector2 screenPos = mainCamera.WorldToScreenPoint(smoothedWorldPos);
        Camera eventCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPos, eventCamera, out Vector2 localPoint))
        {
            rectTransform.localPosition = localPoint;
        }
    }
}
