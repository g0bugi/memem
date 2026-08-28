using UnityEngine;

/// <summary>
/// 부채꼴 히트박스 기반 근접 공격.
/// - 쿨타임마다 자동으로 발동(입력 불필요), 공격 방향은 마우스 커서 방향.
/// - 판정은 실제 콜라이더 없이 OverlapCircleAll + 각도 필터로 계산한다.
/// - 무기별 수치(반지름/각도/쿨타임/데미지)는 WeaponData 에셋으로 분리되어 있어
///   같은 스크립트로 다른 무기를 그대로 재사용할 수 있다.
/// </summary>
public class PlayerAttack : MonoBehaviour
{
    [Header("Weapon")]
    [SerializeField] private WeaponData weaponData;

    [Header("Target")]
    [SerializeField] private LayerMask targetLayers;

    [Header("Debug")]
    [SerializeField] private bool drawGizmo = true;

    private Camera mainCamera;
    private float cooldownTimer;
    private Vector2 lastAimDirection = Vector2.right;

    private void Awake()
    {
        mainCamera = Camera.main;
    }

    private void Update()
    {
        if (weaponData == null) return;

        lastAimDirection = GetAimDirection();

        cooldownTimer -= Time.deltaTime;
        if (cooldownTimer <= 0f)
        {
            PerformAttack(lastAimDirection);
            cooldownTimer = weaponData.cooldown;
        }
    }

    private Vector2 GetAimDirection()
    {
        if (mainCamera == null) return lastAimDirection;

        Vector3 mouseWorld = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        Vector2 dir = (Vector2)mouseWorld - (Vector2)transform.position;
        return dir.sqrMagnitude > 0.0001f ? dir.normalized : lastAimDirection;
    }

    private void PerformAttack(Vector2 aimDirection)
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, weaponData.outerRadius, targetLayers);
        float halfAngle = weaponData.angle * 0.5f;

        foreach (var hit in hits)
        {
            Vector2 toTarget = (Vector2)hit.transform.position - (Vector2)transform.position;
            float distance = toTarget.magnitude;
            if (distance < weaponData.innerRadius) continue;

            float angleToTarget = Vector2.Angle(aimDirection, toTarget);
            if (angleToTarget > halfAngle) continue;

            var damageable = hit.GetComponent<IDamageable>();
            if (damageable != null)
            {
                damageable.TakeDamage(weaponData.damage);
            }
            else
            {
                Debug.Log($"[PlayerAttack] Hit {hit.name} (IDamageable 미구현 - 데미지 미적용)");
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (!drawGizmo || weaponData == null) return;
#if UNITY_EDITOR
        Vector3 origin = transform.position;
        Vector2 dir = Application.isPlaying ? lastAimDirection : Vector2.right;
        float halfAngle = weaponData.angle * 0.5f;
        Vector3 fromDir = Quaternion.Euler(0, 0, -halfAngle) * (Vector3)dir;

        UnityEditor.Handles.color = new Color(1f, 0.2f, 0.2f, 0.25f);
        UnityEditor.Handles.DrawSolidArc(origin, Vector3.forward, fromDir, weaponData.angle, weaponData.outerRadius);

        if (weaponData.innerRadius > 0f)
        {
            UnityEditor.Handles.color = new Color(0f, 0f, 0f, 0.6f);
            UnityEditor.Handles.DrawSolidArc(origin, Vector3.forward, fromDir, weaponData.angle, weaponData.innerRadius);
        }
#endif
    }
}
