using UnityEngine;

/// <summary>
/// 캐릭터가 이동 중일 때 Leg/Leg2 파츠를 좌우로 번갈아 움직여서 걷는 듯한 모션을 만든다.
/// PlayerController2D.IsMoving(입력 방향 기준)으로 이동 여부를 판단하며, 정지하면 원래 위치로 자연스럽게 돌아온다.
/// Player는 Rigidbody2D.MovePosition으로 이동하기 때문에 linearVelocity가 갱신되지 않아 속도 기반 판정을 쓸 수 없다.
/// Man_07처럼 파츠가 분리된 캐릭터의 상위(PlayerPrefab 등)에 붙여서 Leg/Leg2를 연결해 사용한다.
/// </summary>
public class CharacterLegSwing : MonoBehaviour
{
    [Header("Leg Parts")]
    [SerializeField] private Transform leg;
    [SerializeField] private Transform leg2;

    [Header("Swing")]
    [Tooltip("좌우로 흔들리는 폭(로컬 좌표 기준, 유닛)")]
    [SerializeField] private float swingAmplitude = 0.08f;
    [Tooltip("걸음 속도(스윙 주파수)")]
    [SerializeField] private float swingSpeed = 8f;
    [Tooltip("정지 시 원래 위치로 돌아가는 보간 속도")]
    [SerializeField] private float returnSpeed = 10f;
    private PlayerController2D controller;
    private Vector3 legRestPos;
    private Vector3 leg2RestPos;
    private float swingPhase;

private void Awake()
    {
        controller = GetComponentInParent<PlayerController2D>();

        if (leg != null) legRestPos = leg.localPosition;
        if (leg2 != null) leg2RestPos = leg2.localPosition;
    }

    private void Update()
    {
        bool isMoving = controller != null && controller.IsMoving;

        if (isMoving)
        {
            swingPhase += Time.deltaTime * swingSpeed;
            float offset = Mathf.Sin(swingPhase) * swingAmplitude;

            if (leg != null)
            {
                leg.localPosition = legRestPos + new Vector3(offset, 0f, 0f);
            }

            if (leg2 != null)
            {
                // 두 다리가 서로 반대 방향으로 움직여 번갈아 딛는 것처럼 보이게 한다.
                leg2.localPosition = leg2RestPos + new Vector3(-offset, 0f, 0f);
            }
        }
        else
        {
            swingPhase = 0f;

            if (leg != null)
            {
                leg.localPosition = Vector3.Lerp(leg.localPosition, legRestPos, Time.deltaTime * returnSpeed);
            }

            if (leg2 != null)
            {
                leg2.localPosition = Vector3.Lerp(leg2.localPosition, leg2RestPos, Time.deltaTime * returnSpeed);
            }
        }
    }
}
