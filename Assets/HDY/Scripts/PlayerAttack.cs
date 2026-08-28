using UnityEngine;

/// <summary>
/// WeaponInventory가 보유한 모든 무기를 순회하며, 각자의 쿨타임에 따라 자동 공격한다.
/// 지금은 근접(Melee) + 마우스추적(MouseTracking) 조합(단검)만 실제로 구현되어 있고,
/// 나머지 attackType/aimType 조합은 추후 확장을 위한 자리만 남겨둔다.
/// </summary>
[RequireComponent(typeof(WeaponInventory))]
public class PlayerAttack : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private LayerMask targetLayers;

    [Header("Debug")]
    [SerializeField] private bool drawGizmo = true;

    private WeaponInventory inventory;
    private Camera mainCamera;
    private Vector2 lastAimDirection = Vector2.right;

    private void Awake()
    {
        inventory = GetComponent<WeaponInventory>();
        mainCamera = Camera.main;
    }

    private void Update()
    {
        lastAimDirection = GetAimDirection();

        foreach (var weapon in inventory.ActiveWeapons)
        {
            weapon.CooldownTimer -= Time.deltaTime;
            if (weapon.CooldownTimer > 0f) continue;

            if (TryPerformAttack(weapon.Data, lastAimDirection))
            {
                weapon.CooldownTimer = weapon.Data.cooldown;
            }
        }
    }

    private Vector2 GetAimDirection()
    {
        if (mainCamera == null) return lastAimDirection;

        Vector3 mouseWorld = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        Vector2 dir = (Vector2)mouseWorld - (Vector2)transform.position;
        return dir.sqrMagnitude > 0.0001f ? dir.normalized : lastAimDirection;
    }

    private bool TryPerformAttack(WeaponData data, Vector2 aimDirection)
    {
        if (data.attackType == WeaponAttackType.Melee && data.aimType == WeaponAimType.MouseTracking)
        {
            PerformMeleeConeAttack(data, aimDirection);
            return true;
        }

        // TODO: Ranged / Area / Homing, EnemyTracking 등 다른 조합은 추후 구현
        return false;
    }

    private void PerformMeleeConeAttack(WeaponData data, Vector2 aimDirection)
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, data.outerRadius, targetLayers);
        float halfAngle = data.angle * 0.5f;

        foreach (var hit in hits)
        {
            Vector2 toTarget = (Vector2)hit.transform.position - (Vector2)transform.position;
            float distance = toTarget.magnitude;
            if (distance < data.innerRadius) continue;

            float angleToTarget = Vector2.Angle(aimDirection, toTarget);
            if (angleToTarget > halfAngle) continue;

            var damageable = hit.GetComponent<IDamageable>();
            if (damageable != null)
            {
                damageable.TakeDamage(data.damage);
            }
            else
            {
                Debug.Log($"[PlayerAttack] Hit {hit.name} (IDamageable 미구현 - 데미지 미적용)");
            }
        }

        if (data.meleeImpactPrefab != null && MeleeEffectPoolManager.Instance != null)
        {
            float centerRadius = (data.innerRadius + data.outerRadius) * 0.5f;
            Vector3 spawnPos = transform.position + (Vector3)(aimDirection * centerRadius);
            float angleDeg = Mathf.Atan2(aimDirection.y, aimDirection.x) * Mathf.Rad2Deg;
            Quaternion rot = Quaternion.Euler(0f, 0f, angleDeg);
            MeleeEffectPoolManager.Instance.PlayImpact(data.meleeImpactPrefab, spawnPos, rot, data.meleeImpactLifetime);
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (!drawGizmo || inventory == null) return;
#if UNITY_EDITOR
        Vector2 dir = Application.isPlaying ? lastAimDirection : Vector2.right;

        foreach (var weapon in inventory.ActiveWeapons)
        {
            if (weapon.Data.attackType != WeaponAttackType.Melee) continue;
            DrawMeleeConeGizmo(weapon.Data, dir);
        }
#endif
    }

#if UNITY_EDITOR
    private void DrawMeleeConeGizmo(WeaponData data, Vector2 dir)
    {
        Vector3 origin = transform.position;
        float halfAngle = data.angle * 0.5f;
        Vector3 fromDir = Quaternion.Euler(0, 0, -halfAngle) * (Vector3)dir;

        UnityEditor.Handles.color = new Color(1f, 0.2f, 0.2f, 0.25f);
        UnityEditor.Handles.DrawSolidArc(origin, Vector3.forward, fromDir, data.angle, data.outerRadius);

        if (data.innerRadius > 0f)
        {
            UnityEditor.Handles.color = new Color(0f, 0f, 0f, 0.6f);
            UnityEditor.Handles.DrawSolidArc(origin, Vector3.forward, fromDir, data.angle, data.innerRadius);
        }
    }
#endif
}
