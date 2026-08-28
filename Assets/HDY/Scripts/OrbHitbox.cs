using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 요술봉 구슬 하나의 트리거 히트박스. 몬스터가 트리거에서 벗어났다가
/// 다시 들어올 때만 재판정하도록, 현재 겹쳐있는 대상 집합을 추적한다.
/// </summary>
public class OrbHitbox : MonoBehaviour
{
    private float damage;
    private LayerMask targetLayers;
    private readonly HashSet<Collider2D> currentlyInside = new HashSet<Collider2D>();

    public void Init(float damage, LayerMask targetLayers)
    {
        this.damage = damage;
        this.targetLayers = targetLayers;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (((1 << other.gameObject.layer) & targetLayers) == 0) return;
        if (!currentlyInside.Add(other)) return;

        var damageable = other.GetComponent<IDamageable>();
        if (damageable != null)
        {
            damageable.TakeDamage(damage);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        currentlyInside.Remove(other);
    }
}
