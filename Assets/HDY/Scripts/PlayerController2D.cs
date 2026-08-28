using UnityEngine;

/// <summary>
/// 구버전 Input Manager(Input.GetAxis)를 사용한 2D WASD 이동.
/// - 이동: 입력 방향으로 즉시 이동한다(관성 없음). 이동속도는 PlayerStats.MoveSpeed.
/// - 대쉬: 스페이스바로 발동, 발동 중에는 Collider2D를 비활성화해
///   (추후 추가될 몬스터 등과의) 충돌을 무시한다. 쿨타임 5초에 1회.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(PlayerStats))]
public class PlayerController2D : MonoBehaviour
{
    [Header("Dash")]
    [SerializeField] private float dashSpeed = 15f;
    [SerializeField] private float dashDuration = 0.2f;
    [SerializeField] private float dashCooldown = 5f;

    private Rigidbody2D rb;
    private PlayerStats stats;
    private Collider2D col;

    private Vector2 moveInput;

    private Vector2 lastFacingDir = Vector2.down;

    private bool isDashing;
    private float dashTimer;
    private float dashCooldownTimer;
    private Vector2 dashDirection;

    public bool IsDashing => isDashing;
    public float DashCooldownRemaining => Mathf.Max(0f, dashCooldownTimer);

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        stats = GetComponent<PlayerStats>();
        col = GetComponent<Collider2D>();
    }

    private void Update()
    {
        // 프로젝트 기본 Input Manager 축("Horizontal"/"Vertical")은
        // A/D, Left/Right 및 W/S, Up/Down 을 기본으로 매핑하고 있다.
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        moveInput = new Vector2(h, v);
        if (moveInput.sqrMagnitude > 1f) moveInput.Normalize();

        if (moveInput.sqrMagnitude > 0.0001f)
        {
            lastFacingDir = moveInput.normalized;
        }

        if (dashCooldownTimer > 0f)
        {
            dashCooldownTimer -= Time.deltaTime;
        }

        if (Input.GetKeyDown(KeyCode.Space) && !isDashing && dashCooldownTimer <= 0f)
        {
            StartDash();
        }
    }

private void FixedUpdate()
{
    if (isDashing)
    {
        dashTimer -= Time.fixedDeltaTime;
        rb.MovePosition(rb.position + dashDirection * dashSpeed * Time.fixedDeltaTime);

        if (dashTimer <= 0f)
        {
            EndDash();
        }
        return;
    }

    Vector2 velocity = moveInput * stats.MoveSpeed;
    rb.MovePosition(rb.position + velocity * Time.fixedDeltaTime);
}

    private void StartDash()
    {
        isDashing = true;
        dashTimer = dashDuration;
        dashCooldownTimer = dashCooldown;
        dashDirection = moveInput.sqrMagnitude > 0.0001f ? moveInput.normalized : lastFacingDir;

        if (col != null) col.enabled = false;
    }

private void EndDash()
{
    isDashing = false;
    if (col != null) col.enabled = true;
}
}
