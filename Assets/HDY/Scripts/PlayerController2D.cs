using UnityEngine;

/// <summary>
/// 구버전 Input Manager(Input.GetAxis)를 사용한 2D WASD 이동.
/// 입력 방향으로 즉시 이동한다(관성 없음). 이동속도는 PlayerStats.MoveSpeed.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(PlayerStats))]
public class PlayerController2D : MonoBehaviour
{
    

    private Rigidbody2D rb;
    private PlayerStats stats;


    private Vector2 moveInput;







    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        stats = GetComponent<PlayerStats>();

    }

private void Update()
{
    // 프로젝트 기본 Input Manager 축("Horizontal"/"Vertical")은
    // A/D, Left/Right 및 W/S, Up/Down 을 기본으로 매핑하고 있다.
    float h = Input.GetAxisRaw("Horizontal");
    float v = Input.GetAxisRaw("Vertical");
    moveInput = new Vector2(h, v);
    if (moveInput.sqrMagnitude > 1f) moveInput.Normalize();
}

private void FixedUpdate()
{
    Vector2 velocity = moveInput * stats.MoveSpeed;
    rb.MovePosition(rb.position + velocity * Time.fixedDeltaTime);
}




}
