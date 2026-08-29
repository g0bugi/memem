using UnityEngine;

/// <summary>
/// 이 컴포넌트가 존재하는 씬에서만 마우스 커서를 지정한 텍스처로 교체한다.
/// 오브젝트가 비활성화/파괴되면(씬 전환 등) 자동으로 기본 커서로 되돌아간다.
/// 따라서 이 컴포넌트를 넣은 씬(HDY, 이후 전투 씬 등)에서만 커스텀 커서가 적용되고,
/// 다른 씬에서는 별도 처리 없이 기존(OS 기본) 커서가 그대로 사용된다.
///
/// 사용법:
/// 1) 십자 스프라이트 이미지를 프로젝트에 임포트한다.
/// 2) Import Settings에서 Read/Write Enabled를 켠다(안 켜면 SetCursor가 실패한다).
/// 3) 스프라이트 아틀라스로 패킹되지 않은 단독 텍스처여야 한다(패킹되면 좌표가 틀어진다).
/// 4) cursorTexture 필드에 그 텍스처를 연결한다.
/// 십자가의 교차점이 텍스처 정중앙이라면 autoCenterHotspot(기본 on)만으로 충분하다.
/// </summary>
public class CustomCursor : MonoBehaviour
{
    /// <summary>
    /// 현재 씬에서 활성화되어 있는 CustomCursor 인스턴스(없으면 null). 결과창/일시정지 팝업을 닫고
    /// 게임플레이로 복귀할 때(KmsRunResultController.Resume() 등) 커스텀 커서를 다시 적용하는 데 사용한다.
    /// </summary>
    public static CustomCursor Instance { get; private set; }

    [Header("Cursor Sprite")]
    [Tooltip("커서로 사용할 텍스처. Read/Write Enabled가 켜져 있어야 하고, 스프라이트 아틀라스에 " +
             "패킹되지 않은 단독 텍스처여야 한다.")]
    [SerializeField] private Texture2D cursorTexture;

    [Tooltip("켜두면 텍스처의 정중앙을 조준점(hotspot)으로 사용한다. " +
             "십자 스프라이트의 교차점이 텍스처 중앙에 있다면 이 옵션만으로 충분하다.")]
    [SerializeField] private bool autoCenterHotspot = true;

    [Tooltip("autoCenterHotspot이 꺼져 있을 때 사용하는 수동 hotspot(텍스처 좌상단 기준 픽셀 좌표).")]
    [SerializeField] private Vector2 manualHotspot = Vector2.zero;

    [Tooltip("빌드에서 하드웨어 커서(Auto)가 실제 클릭 위치와 어긋나 결과창/일시정지 패널의 버튼이 눌리지 않는 " +
             "문제가 보고되어 기본값을 ForceSoftware(소프트웨어 렌더링, 항상 Input.mousePosition과 정확히 일치)로 " +
             "바꿨다. 성능상 문제가 없다면 이 값을 유지할 것.")]
    [SerializeField] private CursorMode cursorMode = CursorMode.ForceSoftware;

    private void OnEnable()
    {
        Instance = this;
        ApplyCustomCursor();
    }

    private void OnDisable()
    {
        if (Instance == this) Instance = null;
        ResetToDefaultCursor();
    }

    private void ApplyCustomCursor()
    {
        if (cursorTexture == null)
        {
            Debug.LogWarning("[CustomCursor] cursorTexture가 비어있어 기본 커서를 유지합니다.");
            return;
        }

        Vector2 hotspot = autoCenterHotspot
            ? new Vector2(cursorTexture.width * 0.5f, cursorTexture.height * 0.5f)
            : manualHotspot;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        Cursor.SetCursor(cursorTexture, hotspot, cursorMode);
    }

    /// <summary>
    /// 결과창/일시정지 팝업 등에서 커스텀 커서를 잠깐 끈 뒤, 게임플레이로 복귀할 때 다시 켜기 위해
    /// 외부(KmsRunResultController 등)에서 호출한다.
    /// </summary>
    public void ReapplyCursor()
    {
        ApplyCustomCursor();
    }

    private void ResetToDefaultCursor()
    {
        ResetCursorToDefault();
    }

    /// <summary>
    /// 커서가 커스텀 텍스처로 바뀌어 있는 상태에서(이 컴포넌트가 계속 활성화된 채) 결과창/팝업이 띄는
    /// 경우(Time.timeScale = 0로 멈춤만 걸리고 이 오브젝트는 비활성화되지 않으므로 OnDisable이 호출되지 않음),
    /// 커서 핫스팟이 버튼 위치와 어귀나서 클릭이 안 되는 문제가 생긴다. 결과창을 띄우는 쪽(KmsRunResultController 등)에서
    /// 방어코드로 이 메서드를 호출해서 커서를 OS 기본 모양으로 강제로 되돌려놓을 수 있다.
    /// </summary>
    public static void ResetCursorToDefault()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
    }
}
