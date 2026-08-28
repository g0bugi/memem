using UnityEngine;

/// <summary>
/// WeaponInventory가 보유한 모든 무기를 순회하며, 각자의 쿨타임에 따라 자동 공격한다.
/// 지금은 근접(Melee) + 마우스추적(MouseTracking) 조합(단검)만 실제로 구현되어 있고,
/// 나머지 attackType/aimType 조합은 추후 확장을 위한 자리만 남겨둔다.
/// </summary>
[RequireComponent(typeof(WeaponInventory))]
public class PlayerAttack : MonoBehaviour
{
        [Header("Debug")]
    [SerializeField] private bool drawGizmo = true;

    private WeaponInventory inventory;
    private PlayerStats stats;
    
private Camera mainCamera;
    private Vector2 lastAimDirection = Vector2.right;

    /// <summary>근접 공격이 실제로 실행될 때마다 발발된다(명중 여부와 무관). 무기 스윈 애니메이션이 이 이벤트를 구독해서 실제 판정과 동기화된다.</summary>
    public event System.Action<WeaponData, Vector2> MeleeAttackPerformed;

    private void Awake()
    {
        inventory = GetComponent<WeaponInventory>();
        stats = GetComponent<PlayerStats>();
        
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

    if (data.attackType == WeaponAttackType.Ranged && data.aimType == WeaponAimType.MouseTracking)
    {
        PerformRangedAttack(data, aimDirection);
        return true;
    }

    if (data.attackType == WeaponAttackType.Area && data.aimType == WeaponAimType.EnemyTracking)
    {
        return PerformMeteorAttack(data);
    }

    // Orbit는 WeaponInventory가 획득 시점에 별도 컴포넌트로 설치해서 관리하므로 여기서는 건드리지 않는다.
    return false;
}

private void PerformMeleeConeAttack(WeaponData data, Vector2 aimDirection)
    {
        

        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlayRandomSfx(data.ResolvedAttackSounds, data.attackSoundVolume);
        }
MeleeAttackPerformed?.Invoke(data, aimDirection);

        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, data.outerRadius, inventory.TargetLayers);
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
                damageable.TakeDamage(data.damage + stats.AttackPower);
            }
            else
            {
                Debug.Log($"[PlayerAttack] Hit {hit.name} (IDamageable 미구현 - 데미지 미적용)");
            }
        }

        if (data.ResolvedMeleeImpactPrefab != null && EffectPoolManager.Instance != null)
        {
            float centerRadius = (data.innerRadius + data.outerRadius) * 0.5f;
            Vector3 spawnPos = transform.position + (Vector3)(aimDirection * centerRadius);
            float angleDeg = Mathf.Atan2(aimDirection.y, aimDirection.x) * Mathf.Rad2Deg;
            Quaternion rot = Quaternion.Euler(0f, 0f, angleDeg);
            EffectPoolManager.Instance.PlayImpact(data.ResolvedMeleeImpactPrefab, spawnPos, rot, data.meleeImpactLifetime);
        }
    }

private void PerformRangedAttack(WeaponData data, Vector2 aimDirection)
{
    GameObject projectilePrefab = data.ResolvedProjectilePrefab;
    

    if (SoundManager.Instance != null)
    {
        SoundManager.Instance.PlayRandomSfx(data.ResolvedAttackSounds, data.attackSoundVolume);
    }
if (projectilePrefab == null || ProjectilePoolManager.Instance == null) return;

    Vector3 spawnPos = transform.position + (Vector3)(aimDirection * 0.5f);
    GameObject instance = ProjectilePoolManager.Instance.Get(projectilePrefab, spawnPos, Quaternion.identity);
    Projectile projectile = instance.GetComponent<Projectile>();
    if (projectile != null)
    {
        projectile.Launch(projectilePrefab, aimDirection, data.projectileSpeed, data.damage + stats.AttackPower, data.projectileLifetime, inventory.TargetLayers);
    }
}

private bool PerformMeteorAttack(WeaponData data)
    {

    Collider2D[] candidates = Physics2D.OverlapCircleAll(transform.position, data.outerRadius, inventory.TargetLayers);

if (candidates.Length == 0) return false;

    if (SoundManager.Instance != null)
    {
        SoundManager.Instance.PlayRandomSfx(data.ResolvedAttackSounds, data.attackSoundVolume);
    }

    
Collider2D target = candidates[Random.Range(0, candidates.Length)];
    Vector3 targetPos = target.transform.position;

    GameObject projectilePrefab = data.ResolvedProjectilePrefab;
    if (projectilePrefab != null && ProjectilePoolManager.Instance != null)
    {
        Vector3 spawnPos = targetPos + Vector3.up * 6f;
        GameObject instance = ProjectilePoolManager.Instance.Get(projectilePrefab, spawnPos, Quaternion.identity);
        MeteorProjectile meteor = instance.GetComponent<MeteorProjectile>();
        if (meteor != null)
        {
            meteor.Launch(projectilePrefab, targetPos, data.fallDuration, data.explosionRadius, data.damage + stats.AttackPower, inventory.TargetLayers,
                data.ResolvedFireFloorPrefab, data.fireFloorDuration, data.fireFloorTickDamage, data.fireFloorTickInterval);
        }
    }

    return true;
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
