using System.Collections;
using UnityEngine;

/// <summary>
/// 폭발 등으로 생기는 지속피해 장판. 활성화되면 duration 동안 tickInterval마다
/// radius 내의 적에게 tickDamage를 주고, 시간이 다 되면 풀로 반환된다.
/// onHit은 틱 한 번에 적을 맞힐 때마다(대상 하나당 한 번씩) 호출된다 — 이 틱이 적을 죽이는
/// 마무리타격이어도 데미지는 이미 들어갔으므로 콤보 시스템이 정상적으로 콤보를 올리게 된다.
/// </summary>
public class FireFloorHazard : MonoBehaviour
{
    private GameObject prefabKey;

public void Activate(GameObject prefabKey, float duration, float tickDamage, float tickInterval, float radius, LayerMask targetLayers, System.Action onHit = null)
    {
        this.prefabKey = prefabKey;
        StopAllCoroutines();
        StartCoroutine(TickRoutine(duration, tickDamage, Mathf.Max(0.05f, tickInterval), radius, targetLayers, onHit));
    }

private IEnumerator TickRoutine(float duration, float tickDamage, float tickInterval, float radius, LayerMask targetLayers, System.Action onHit)
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
                    onHit?.Invoke();
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
