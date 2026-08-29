using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HDY;

/// <summary>
/// 보유 무기(WeaponInventory.ActiveWeapons)를 매 프레임 순회하며 쿨타임이 다 된 무기의 공격을 실행한다.
/// Orbit 타입은 이 쿨타임 루프를 타지 않고 OrbitWeaponController가 별도로 관리한다.
/// 콤보 보너스(사거리/투사체 개수/폭발 반경)는 실제 공격을 실행하는 이 시점에 그때그때 실시간으로 적용되며,
/// WeaponData 원본 값은 절대 수정하지 않는다.
/// </summary>
public class PlayerAttack : MonoBehaviour
{
    private WeaponInventory inventory;
    private PlayerStats stats;
    private ComboManager combo;
    private Camera mainCamera;
    private Vector2 lastAimDirection = Vector2.right;

    /// <summary>근접 무기 공격이 실제로 실행될 때마다(명중 여부와 무관하게) (WeaponData, 조준 방향)과 함께
    /// 발동된다. WeaponSwingAnimator가 이 이벤트를 구독해서 스윙 연출을 판정 시점과 동기화한다.</summary>
    public event System.Action<WeaponData, Vector2> MeleeAttackPerformed;

    private void Awake()
    {
        inventory = GetComponent<WeaponInventory>();
        stats = GetComponent<PlayerStats>();
        combo = GetComponent<ComboManager>();
        mainCamera = Camera.main;
    }

    private void Update()
    {
        if (inventory == null) return;

        foreach (ActiveWeapon weapon in inventory.ActiveWeapons)
        {
            if (weapon.Data == null || weapon.Data.attackType == WeaponAttackType.Orbit) continue;

            weapon.CooldownTimer -= Time.deltaTime;
            if (weapon.CooldownTimer > 0f) continue;

            weapon.CooldownTimer = Mathf.Max(0.01f, weapon.Data.cooldown);

            Vector2 aimDirection = GetAimDirection(weapon.Data);
            TryPerformAttack(weapon.Data, aimDirection);
        }
    }

    private Vector2 GetAimDirection(WeaponData data)
    {
        switch (data.aimType)
        {
            case WeaponAimType.EnemyTracking:
            {
                Transform nearest = FindNearestEnemy(Mathf.Max(0.1f, data.outerRadius));
                if (nearest != null)
                {
                    Vector2 dir = (Vector2)nearest.position - (Vector2)transform.position;
                    if (dir.sqrMagnitude > 0.0001f)
                    {
                        lastAimDirection = dir.normalized;
                    }
                }
                return lastAimDirection;
            }
            case WeaponAimType.AreaSelf:
                return lastAimDirection;
            case WeaponAimType.MouseTracking:
            default:
            {
                if (mainCamera == null)
                {
                    mainCamera = Camera.main;
                }
                if (mainCamera == null) return lastAimDirection;

                Vector3 mouseWorld = mainCamera.ScreenToWorldPoint(Input.mousePosition);
                Vector2 dir = (Vector2)mouseWorld - (Vector2)transform.position;
                if (dir.sqrMagnitude > 0.0001f)
                {
                    lastAimDirection = dir.normalized;
                }
                return lastAimDirection;
            }
        }
    }

    private void TryPerformAttack(WeaponData data, Vector2 aimDirection)
    {
        switch (data.attackType)
        {
            case WeaponAttackType.Melee:
                PerformMeleeConeAttack(data, aimDirection);
                break;
            case WeaponAttackType.Ranged:
            case WeaponAttackType.Homing:
                PerformRangedAttack(data, aimDirection);
                break;
            case WeaponAttackType.Area:
                PerformMeteorAttack(data, aimDirection);
                break;
        }

        PlayAttackSound(data);
    }

private void PerformMeleeConeAttack(WeaponData data, Vector2 aimDirection)
    {
        MeleeAttackPerformed?.Invoke(data, aimDirection);

        float outerRadius = data.outerRadius + (combo != null ? combo.RangeBonus : 0f);

        if (data.ResolvedMeleeImpactPrefab != null && EffectPoolManager.Instance != null)
        {
            Vector3 impactPosition = transform.position + (Vector3)(aimDirection.normalized * outerRadius * 0.5f);
            float meleeVisualScale = data.outerRadius > 0f ? outerRadius / data.outerRadius : 1f;
            PlayScaledMeleeImpact(data.ResolvedMeleeImpactPrefab, impactPosition, data.meleeImpactLifetime, meleeVisualScale);
        }

        StartCoroutine(MeleeHitWindowRoutine(data, aimDirection));
    }

    /// <summary>
    /// 근접 판정을 스윙이 실제로 재생되는 동안(data.meleeHitWindowDuration) 매 프레임 반복 검사한다.
    /// 판정 시작 순간 단 한 프레임의 몬스터 위치만으로 명중 여부가 갈리면, 움직이는 몬스터를 상대로
    /// "분명 스친 것처럼 보이는데 씹히는" 간헐적 미스가 생기기 때문에, 스윙 지속 시간 동안 계속
    /// 재검사해서 그 창 안에서 한 번이라도 겹치면 명중으로 처리한다(대상별 중복 처리는 방지).
    /// </summary>
    private IEnumerator MeleeHitWindowRoutine(WeaponData data, Vector2 aimDirection)
    {
        var alreadyHit = new HashSet<IDamageable>();
        float windowDuration = Mathf.Max(0f, data.meleeHitWindowDuration);
        float elapsed = 0f;

        while (true)
        {
            CheckMeleeHits(data, aimDirection, alreadyHit);

            if (elapsed >= windowDuration) yield break;
            elapsed += Time.deltaTime;
            yield return null;
        }
    }

    /// <summary>실제 부채꼴 겹침 판정 1회. alreadyHit에 없는 대상만 데미지/콤보를 처리하고 목록에 추가한다.</summary>
    private void CheckMeleeHits(WeaponData data, Vector2 aimDirection, HashSet<IDamageable> alreadyHit)
    {
        float outerRadius = data.outerRadius + (combo != null ? combo.RangeBonus : 0f);
        float innerRadius = data.innerRadius;
        float halfAngle = data.angle * 0.5f + (combo != null ? combo.AngleBonus : 0f);
        float aimAngle = Mathf.Atan2(aimDirection.y, aimDirection.x) * Mathf.Rad2Deg;

        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, outerRadius, inventory.TargetLayers);
        foreach (var hit in hits)
        {
            Vector2 toTarget = (Vector2)hit.transform.position - (Vector2)transform.position;
            float distance = toTarget.magnitude;
            if (distance < innerRadius) continue;

            float targetAngle = Mathf.Atan2(toTarget.y, toTarget.x) * Mathf.Rad2Deg;
            if (Mathf.Abs(Mathf.DeltaAngle(aimAngle, targetAngle)) > halfAngle) continue;

            var damageable = hit.GetComponent<IDamageable>();
            if (damageable == null || alreadyHit.Contains(damageable)) continue;

            alreadyHit.Add(damageable);
            float totalDamage = data.damage + (stats != null ? stats.RollAttackPower() : 0f);
            damageable.TakeDamage(totalDamage);
            combo?.RegisterHit();
        }
    }

private void PlayScaledMeleeImpact(GameObject prefab, Vector3 position, float lifetime, float visualScale)
    {
        GameObject instance = EffectPoolManager.Instance.Get(prefab, position, Quaternion.identity);
        instance.transform.localScale = prefab.transform.localScale * Mathf.Max(0.01f, visualScale);
        StartCoroutine(ReturnEffectAfterDelay(prefab, instance, lifetime));
    }

    private IEnumerator ReturnEffectAfterDelay(GameObject prefab, GameObject instance, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (EffectPoolManager.Instance != null)
        {
            EffectPoolManager.Instance.Return(prefab, instance);
        }
    }


private void PerformRangedAttack(WeaponData data, Vector2 aimDirection)
    {
        GameObject projectilePrefab = data.ResolvedProjectilePrefab;
        if (projectilePrefab == null || ProjectilePoolManager.Instance == null) return;

        int shotCount = Mathf.Max(1, 1 + Mathf.RoundToInt(combo != null ? combo.ProjectileCountBonus : 0f));
        ComboManager comboRef = combo;

        for (int i = 0; i < shotCount; i++)
        {
            float spreadAngle = shotCount > 1 ? data.multiShotSpreadAngle * (i - (shotCount - 1) * 0.5f) : 0f;
            Vector2 shotDirection = Quaternion.Euler(0f, 0f, spreadAngle) * aimDirection;

            GameObject instance = ProjectilePoolManager.Instance.Get(projectilePrefab, transform.position, Quaternion.identity);
            Projectile projectile = instance.GetComponent<Projectile>();
            if (projectile == null)
            {
                projectile = instance.AddComponent<Projectile>();
            }

            projectile.Launch(projectilePrefab, shotDirection, data.projectileSpeed, data.damage, data.projectileLifetime,
                inventory.TargetLayers, data.pierce, stats, () => comboRef?.RegisterHit());
        }
    }

private void PerformMeteorAttack(WeaponData data, Vector2 aimDirection)
    {
        GameObject meteorPrefab = data.ResolvedProjectilePrefab;
        if (meteorPrefab == null || ProjectilePoolManager.Instance == null) return;

        float searchRadius = Mathf.Max(0.1f, data.outerRadius);
        Transform nearestEnemy = FindNearestEnemy(searchRadius);
        Vector3 targetPosition = nearestEnemy != null
            ? nearestEnemy.position
            : transform.position + (Vector3)(aimDirection.normalized * searchRadius);

        float explosionRadiusFinal = data.explosionRadius + (combo != null ? combo.ExplosionRadiusBonus : 0f);
        float visualScale = data.explosionRadius > 0f ? explosionRadiusFinal / data.explosionRadius : 1f;
        ComboManager comboRef = combo;

        GameObject instance = ProjectilePoolManager.Instance.Get(meteorPrefab, targetPosition, Quaternion.identity);
        MeteorProjectile meteor = instance.GetComponent<MeteorProjectile>();
        if (meteor == null)
        {
            meteor = instance.AddComponent<MeteorProjectile>();
        }

        meteor.Launch(
            meteorPrefab,
            targetPosition,
            data.fallDuration,
            data.damage,
            stats,
            explosionRadiusFinal,
            inventory.TargetLayers,
            data.ResolvedExplosionPrefab,
            data.explosionEffectLifetime,
            data.ResolvedFireFloorPrefab,
            data.fireFloorDuration,
            data.fireFloorTickDamage,
            data.fireFloorTickInterval,
            visualScale,
            () => comboRef?.RegisterHit());
    }

    private Transform FindNearestEnemy(float radius)
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, radius, inventory.TargetLayers);
        Transform nearest = null;
        float nearestSqrDist = float.MaxValue;

        foreach (var hit in hits)
        {
            float sqrDist = ((Vector2)hit.transform.position - (Vector2)transform.position).sqrMagnitude;
            if (sqrDist < nearestSqrDist)
            {
                nearestSqrDist = sqrDist;
                nearest = hit.transform;
            }
        }

        return nearest;
    }

    private static void PlayAttackSound(WeaponData data)
    {
        if (SoundManager.Instance == null) return;
        SoundManager.Instance.PlayRandomSfx(data.ResolvedAttackSounds, data.attackSoundVolume);
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (inventory == null) return;

        foreach (ActiveWeapon weapon in inventory.ActiveWeapons)
        {
            if (weapon.Data == null || weapon.Data.attackType != WeaponAttackType.Melee) continue;
            DrawMeleeConeGizmo(weapon.Data);
        }
    }

    private void DrawMeleeConeGizmo(WeaponData data)
    {
        float outerRadius = data.outerRadius + (combo != null ? combo.RangeBonus : 0f);
        float innerRadius = data.innerRadius;
        float halfAngle = data.angle * 0.5f + (combo != null ? combo.AngleBonus : 0f);
        float aimAngle = Mathf.Atan2(lastAimDirection.y, lastAimDirection.x) * Mathf.Rad2Deg;

        Gizmos.color = new Color(1f, 0.3f, 0.3f, 0.6f);

        Vector3 origin = transform.position;
        Quaternion startRot = Quaternion.Euler(0f, 0f, aimAngle - halfAngle);
        Quaternion endRot = Quaternion.Euler(0f, 0f, aimAngle + halfAngle);

        Gizmos.DrawLine(origin + startRot * Vector3.right * innerRadius, origin + startRot * Vector3.right * outerRadius);
        Gizmos.DrawLine(origin + endRot * Vector3.right * innerRadius, origin + endRot * Vector3.right * outerRadius);

        const int segments = 16;
        Vector3 previousPoint = origin + startRot * Vector3.right * outerRadius;
        for (int i = 1; i <= segments; i++)
        {
            float t = (float)i / segments;
            Quaternion rot = Quaternion.Slerp(startRot, endRot, t);
            Vector3 point = origin + rot * Vector3.right * outerRadius;
            Gizmos.DrawLine(previousPoint, point);
            previousPoint = point;
        }
    }
#endif
}
