using System.Collections;
using UnityEngine;

/// <summary>
/// 하늘에서 목표 지점으로 떨어지는 메테오. 착탄하면 반경(explosionRadius) 내 모든 적에게
/// 데미지를 주고, 지정된 불장판(FireFloorHazard) 프리팹을 그 자리에 남긴 뒤 풀로 반환된다.
/// </summary>
public class MeteorProjectile : MonoBehaviour
{
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
        float fireFloorTickInterval)
    {
        this.prefabKey = prefabKey;
        StopAllCoroutines();
        StartCoroutine(FallRoutine(targetPos, fallDuration, explosionRadius, damage, targetLayers,
            fireFloorPrefab, fireFloorDuration, fireFloorTickDamage, fireFloorTickInterval));
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
        float fireFloorTickInterval)
    {
        Vector3 startPos = transform.position;
        float t = 0f;

        while (t < fallDuration)
        {
            t += Time.deltaTime;
            float ratio = fallDuration > 0f ? Mathf.Clamp01(t / fallDuration) : 1f;
            transform.position = Vector3.Lerp(startPos, targetPos, ratio);
            yield return null;
        }

        transform.position = targetPos;

        Explode(targetPos, explosionRadius, damage, targetLayers, fireFloorPrefab, fireFloorDuration, fireFloorTickDamage, fireFloorTickInterval);
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
        float fireFloorTickInterval)
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
