using System.Collections;
using UnityEngine;

/// <summary>
/// Area(메테오) 공격의 낙하~폭발 연출을 담당한다. ProjectilePoolManager로 재사용되는 낙하 비주얼
/// 오브젝트이며, 착탄 시 explosionRadius 안의 적 전체에게 한 번에 피해를 준다.
/// </summary>
public class MeteorProjectile : MonoBehaviour
{
    private GameObject prefabKey;

    /// <summary>onHit은 폭발 판정에 맞은 적 한 마리당 한 번씩 호출된다(여러 마리를 동시에 맞히면
    /// 그 마릿수만큼 여러 번 호출된다 — 콤보 시스템이 이 콜백 호출 횟수만큼 콤보를 올린다).</summary>
public void Launch(
        GameObject prefabKey,
        Vector3 targetPosition,
        float fallDuration,
        float weaponDamage,
        PlayerStats stats,
        float explosionRadius,
        LayerMask targetLayers,
        GameObject explosionPrefab,
        float explosionEffectLifetime,
        GameObject fireFloorPrefab,
        float fireFloorDuration,
        float fireFloorTickDamage,
        float fireFloorTickInterval,
        float visualScale = 1f,
        System.Action onHit = null)
    {
        this.prefabKey = prefabKey;
        StopAllCoroutines();
        StartCoroutine(FallRoutine(
            targetPosition, fallDuration, weaponDamage, stats, explosionRadius, targetLayers,
            explosionPrefab, explosionEffectLifetime, fireFloorPrefab, fireFloorDuration,
            fireFloorTickDamage, fireFloorTickInterval, visualScale, onHit));
    }

private IEnumerator FallRoutine(
        Vector3 targetPosition, float fallDuration, float weaponDamage, PlayerStats stats, float explosionRadius, LayerMask targetLayers,
        GameObject explosionPrefab, float explosionEffectLifetime, GameObject fireFloorPrefab, float fireFloorDuration,
        float fireFloorTickDamage, float fireFloorTickInterval, float visualScale, System.Action onHit)
    {
        Vector3 startPosition = targetPosition + Vector3.up * 5f;
        transform.position = startPosition;

        float duration = Mathf.Max(0.01f, fallDuration);
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            transform.position = Vector3.Lerp(startPosition, targetPosition, t);
            yield return null;
        }

        transform.position = targetPosition;

        Explode(targetPosition, weaponDamage, stats, explosionRadius, targetLayers, explosionPrefab, explosionEffectLifetime,
            fireFloorPrefab, fireFloorDuration, fireFloorTickDamage, fireFloorTickInterval, visualScale, onHit);

        ReturnToPool();
    }

private void Explode(
        Vector3 position, float weaponDamage, PlayerStats stats, float explosionRadius, LayerMask targetLayers,
        GameObject explosionPrefab, float explosionEffectLifetime, GameObject fireFloorPrefab, float fireFloorDuration,
        float fireFloorTickDamage, float fireFloorTickInterval, float visualScale, System.Action onHit)
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(position, explosionRadius, targetLayers);
        foreach (var hit in hits)
        {
            var damageable = hit.GetComponent<IDamageable>();
            if (damageable != null)
            {
                float rolledDamage = weaponDamage + (stats != null ? stats.RollAttackPower() : 0f);
                damageable.TakeDamage(rolledDamage);
                onHit?.Invoke();
            }
        }

        if (explosionPrefab != null && EffectPoolManager.Instance != null)
        {
            GameObject explosionInstance = EffectPoolManager.Instance.Get(explosionPrefab, position, Quaternion.identity);
            ApplyVisualScale(explosionInstance, explosionPrefab, visualScale);
            StartCoroutine(ReturnEffectAfterDelay(explosionPrefab, explosionInstance, explosionEffectLifetime));
        }

        if (fireFloorPrefab != null && EffectPoolManager.Instance != null)
        {
            GameObject fireFloorInstance = EffectPoolManager.Instance.Get(fireFloorPrefab, position, Quaternion.identity);
            ApplyVisualScale(fireFloorInstance, fireFloorPrefab, visualScale);

            FireFloorHazard hazard = fireFloorInstance.GetComponent<FireFloorHazard>();
            if (hazard != null)
            {
                hazard.Activate(fireFloorPrefab, fireFloorDuration, fireFloorTickDamage, fireFloorTickInterval, explosionRadius, targetLayers, onHit);
            }
        }
    }

    // 폭발/불장판 비주얼도 콤보로 커진 explosionRadius에 비례해서 함께 커지도록, 원본 프리팹 자체의
    // localScale을 기준(baseline)으로 삼아 스케일을 곱한다. 풀에서 재사용되는 인스턴스의 현재 scale을
    // 기준으로 삼으면 재사용될 때마다 스케일이 누적(복리)되므로 반드시 프리팹 원본 scale을 써야 한다.
    private static void ApplyVisualScale(GameObject instance, GameObject prefab, float visualScale)
    {
        if (instance == null || prefab == null) return;
        instance.transform.localScale = prefab.transform.localScale * Mathf.Max(0.01f, visualScale);
    }

    private IEnumerator ReturnEffectAfterDelay(GameObject prefab, GameObject instance, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (EffectPoolManager.Instance != null)
        {
            EffectPoolManager.Instance.Return(prefab, instance);
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
