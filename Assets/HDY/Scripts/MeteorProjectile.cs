using System.Collections;
using UnityEngine;

/// <summary>
/// 하늘에서 목표 지점으로 떨어지는 메테오. 착탄하면 반경(explosionRadius) 내 모든 적에게
/// 데미지를 주고, 지정된 불장판(FireFloorHazard) 프리팹을 그 자리에 남긴 뒤 풀로 반환된다.
/// </summary>
public class MeteorProjectile : MonoBehaviour
{
    [Tooltip("스프라이트 기본 상태(회전 0도)에서 머리(뾰족한 끝)가 향하는 각도. 0=오른쪽(+X), 90=위(+Y), -90=아래, 180=왼쪽. 낙하 방향 각도에 이 값만큼 보정을 더해 머리가 항상 떨어지는 방향(적)을 향하게 한다.")]
    [SerializeField] private float spriteForwardOffsetDeg = 0f;

    private GameObject prefabKey;

public void Launch(
        GameObject prefabKey,
        Vector3 targetPos,
        float fallDuration,
        float explosionRadius,
        float damage,
        LayerMask targetLayers,
        GameObject fireFloorPrefab,
        float fireFloorDuration,
        float fireFloorTickDamage,
        float fireFloorTickInterval,
        GameObject explosionPrefab,
        float explosionEffectLifetime)
    {
        this.prefabKey = prefabKey;
        StopAllCoroutines();
        StartCoroutine(FallRoutine(targetPos, fallDuration, explosionRadius, damage, targetLayers,
            fireFloorPrefab, fireFloorDuration, fireFloorTickDamage, fireFloorTickInterval,
            explosionPrefab, explosionEffectLifetime));
    }

private IEnumerator FallRoutine(
        Vector3 targetPos,
        float fallDuration,
        float explosionRadius,
        float damage,
        LayerMask targetLayers,
        GameObject fireFloorPrefab,
        float fireFloorDuration,
        float fireFloorTickDamage,
        float fireFloorTickInterval,
        GameObject explosionPrefab,
        float explosionEffectLifetime)
    {
        Vector3 startPos = transform.position;
        float t = 0f;

        Vector3 travelDir = targetPos - startPos;
        if (travelDir.sqrMagnitude > 0.0001f)
        {
            float angleDeg = Mathf.Atan2(travelDir.y, travelDir.x) * Mathf.Rad2Deg + spriteForwardOffsetDeg;
            transform.rotation = Quaternion.Euler(0f, 0f, angleDeg);
        }

        while (t < fallDuration)
        {
            t += Time.deltaTime;
            float ratio = fallDuration > 0f ? Mathf.Clamp01(t / fallDuration) : 1f;
            transform.position = Vector3.Lerp(startPos, targetPos, ratio);
            yield return null;
        }

        transform.position = targetPos;

        Explode(targetPos, explosionRadius, damage, targetLayers, fireFloorPrefab, fireFloorDuration, fireFloorTickDamage, fireFloorTickInterval,
            explosionPrefab, explosionEffectLifetime);
        ReturnToPool();
    }

private void Explode(
        Vector3 pos,
        float explosionRadius,
        float damage,
        LayerMask targetLayers,
        GameObject fireFloorPrefab,
        float fireFloorDuration,
        float fireFloorTickDamage,
        float fireFloorTickInterval,
        GameObject explosionPrefab,
        float explosionEffectLifetime)
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(pos, explosionRadius, targetLayers);
        foreach (var hit in hits)
        {
            var damageable = hit.GetComponent<IDamageable>();
            if (damageable != null)
            {
                damageable.TakeDamage(damage);
            }
        }

        if (explosionPrefab != null && EffectPoolManager.Instance != null)
        {
            EffectPoolManager.Instance.PlayImpact(explosionPrefab, pos, Quaternion.identity, explosionEffectLifetime);
        }

        if (fireFloorPrefab != null && EffectPoolManager.Instance != null)
        {
            GameObject hazard = EffectPoolManager.Instance.Get(fireFloorPrefab, pos, Quaternion.identity);
            FireFloorHazard floor = hazard.GetComponent<FireFloorHazard>();
            if (floor != null)
            {
                floor.Activate(fireFloorPrefab, fireFloorDuration, fireFloorTickDamage, fireFloorTickInterval, explosionRadius, targetLayers);
            }
        }
    }

    private void ReturnToPool()
    {
        if (ProjectilePoolManager.Instance != null && prefabKey != null)
        {
            ProjectilePoolManager.Instance.Return(prefabKey, gameObject);
        }
        else
        {
            gameObject.SetActive(false);
        }
    }
}
