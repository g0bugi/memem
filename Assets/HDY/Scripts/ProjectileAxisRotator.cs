using UnityEngine;

/// <summary>
/// 투사체 프리팹에 붙이면 스프라이트가 항상 날아가는 방향(화살처럼)을 바라보도록 회전시킨다.
/// 같은 오브젝트에 Projectile 컴포넌트가 있으면 그 이동 방향을 바로 사용하고,
/// 없으면 매 프레임 위치 변화량으로 이동 방향을 추정해서 동작한다(다른 이동 방식에도 재사용 가능).
/// </summary>
public class ProjectileAxisRotator : MonoBehaviour
{
    [Tooltip("스프라이트 기본 상태(회전 0도)에서 머리(뾰족한 끝)가 향하는 각도. 0=오른쪽(+X), 90=위(+Y), -90=아래, 180=왼쪽. 이동 방향 각도에 이 값만큼 보정을 더해 머리가 항상 진행 방향을 향하게 한다.")]
    [SerializeField] private float spriteForwardOffsetDeg = 0f;

    private Projectile projectile;
    private Vector3 lastPosition;

    private void Awake()
    {
        projectile = GetComponent<Projectile>();
    }

    private void OnEnable()
    {
        lastPosition = transform.position;

        if (projectile != null)
        {
            ApplyRotation(projectile.Direction);
        }
    }

    private void LateUpdate()
    {
        if (projectile != null)
        {
            ApplyRotation(projectile.Direction);
            lastPosition = transform.position;
            return;
        }

        Vector3 currentPosition = transform.position;
        Vector2 delta = currentPosition - lastPosition;
        if (delta.sqrMagnitude > 0.0000001f)
        {
            ApplyRotation(delta);
        }
        lastPosition = currentPosition;
    }

    private void ApplyRotation(Vector2 direction)
    {
        if (direction.sqrMagnitude < 0.0000001f) return;

        float angleDeg = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg + spriteForwardOffsetDeg;
        transform.rotation = Quaternion.Euler(0f, 0f, angleDeg);
    }
}
