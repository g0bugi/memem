using UnityEngine;

/// <summary>
/// 마우스 커서가 캐릭터 기준 왼쪽에 있으면 캐릭터(Man_07)를 좌우로 반전시키고,
/// 오른쪽에 있으면 원래 방향으로 되돌린다. characterVisual(Man_07)의 localScale.x를 ±로 토글하는 방식이라,
/// 이 하위에 있는 모든 파츠(Head, Body, Weapon 등)가 함께 반전된다.
/// WeaponSwingAnimator는 이 반전 상태(Man_07.localScale.x의 부호)를 직접 읽어서 스윙 회전 계산을 보정한다.
/// </summary>
public class CharacterFacingFlip : MonoBehaviour
{
    [Tooltip("반전시킬 대상(Man_07) 트랜스폼")]
    [SerializeField] private Transform characterVisual;

    private Camera mainCamera;
    private bool isFacingLeft;

    public bool IsFacingLeft => isFacingLeft;

    private void Awake()
    {
        mainCamera = Camera.main;
    }

    private void Update()
    {
        if (characterVisual == null) return;
        if (mainCamera == null) mainCamera = Camera.main;
        if (mainCamera == null) return;

        Vector3 mouseWorldPos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        bool shouldFaceLeft = mouseWorldPos.x < transform.position.x;

        if (shouldFaceLeft == isFacingLeft) return;

        isFacingLeft = shouldFaceLeft;
        Vector3 scale = characterVisual.localScale;
        float absX = Mathf.Abs(scale.x);
        scale.x = isFacingLeft ? -absX : absX;
        characterVisual.localScale = scale;
    }
}
