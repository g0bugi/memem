using UnityEngine;

/// <summary>
/// 구버전 Input Manager(Input.GetAxis)를 사용한 2D WASD 이동.
/// - 관성: 정지 상태에서 입력이 들어오면 MinMoveSpeed로 즉시 출발하여
///   AccelerationTime(기본 1초) 동안 MaxMoveSpeed까지 가속한다.
///   입력 방향이 바뀌면 목표 속도 벡터 자체가 바뀌므로, 기존 속도에서
///   새 방향으로 자연스럽게(선형 보간) 전환되어 회전에도 관성이 반영된다.
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
    private Vector2 currentVelocity;
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

        float accelRate = (stats.MaxMoveSpeed - stats.MinMoveSpeed) / Mathf.Max(0.0001f, stats.AccelerationTime);
        float decelRate = stats.MaxMoveSpeed / Mathf.Max(0.0001f, stats.DecelerationTime);

        if (moveInput.sqrMagnitude > 0.0001f)
        {
            // 정지 상태에서 막 움직이기 시작했다면 최소 속도로 즉시 출발한다.
            bool wasStopped = currentVelocity.sqrMagnitude < (stats.MinMoveSpeed * stats.MinMoveSpeed * 0.01f);
            if (wasStopped)
            {
                currentVelocity = moveInput * stats.MinMoveSpeed;
            }

            Vector2 targetVelocity = moveInput * stats.MaxMoveSpeed;
            currentVelocity = Vector2.MoveTowards(currentVelocity, targetVelocity, accelRate * Time.fixedDeltaTime);
        }
        else
        {
            currentVelocity = Vector2.MoveTowards(currentVelocity, Vector2.zero, decelRate * Time.fixedDeltaTime);
        }

        rb.MovePosition(rb.position + currentVelocity * Time.fixedDeltaTime);
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

        // 대쉬 종료 후에도 최고 속도로 이어서 이동하도록 관성 속도를 유지한다.
        currentVelocity = dashDirection * stats.MaxMoveSpeed;
    }
}
