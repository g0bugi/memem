using System.Collections;
using UnityEngine;

/// <summary>
/// 폭발 등으로 생기는 지속피해 장판. 활성화되면 duration 동안 tickInterval마다
/// radius 내의 적에게 tickDamage를 주고, 시간이 다 되면 풀로 반환된다.
/// </summary>
public class FireFloorHazard : MonoBehaviour
{
    private GameObject prefabKey;

    public void Activate(GameObject prefabKey, float duration, float tickDamage, float tickInterval, float radius, LayerMask targetLayers)
    {
        this.prefabKey = prefabKey;
        StopAllCoroutines();
        StartCoroutine(TickRoutine(duration, tickDamage, Mathf.Max(0.05f, tickInterval), radius, targetLayers));
    }

    private IEnumerator TickRoutine(float duration, float tickDamage, float tickInterval, float radius, LayerMask targetLayers)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, radius, targetLayers);
            foreach (var hit in hits)
            {
                var damageable = hit.GetComponent<IDamageable>();
                if (damageable != null)
                {
                    damageable.TakeDamage(tickDamage);
                }
            }

            yield return new WaitForSeconds(tickInterval);
            elapsed += tickInterval;
        }

        ReturnToPool();
    }

    private void ReturnToPool()
    {
        if (EffectPoolManager.Instance != null && prefabKey != null)
        {
            EffectPoolManager.Instance.Return(prefabKey, gameObject);
        }
        else
        {
            gameObject.SetActive(false);
        }
    }
}
