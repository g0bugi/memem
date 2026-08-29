using UnityEngine;

public class Projectile : MonoBehaviour
{
    private GameObject prefabKey;
    private Vector2 direction;
    private float speed;
    private float weaponDamage;
    private PlayerStats stats;
    private float lifetimeRemaining;
    private LayerMask targetLayers;
    private bool isActive;
    private bool pierce;
    private System.Action onHit;

    public Vector2 Direction => direction;

    /// <summary>onHit은 이 투사체가 적을 맞힐 때마다 호출된다(관통 투사체는 여러 번 맞을 수 있으므로
    /// 그때마다 매번 호출된다 — 콤보 시스템이 이 콜백 호출 횟수만큼 콤보를 올린다).</summary>
public void Launch(GameObject prefabKey, Vector2 direction, float speed, float weaponDamage, float lifetime, LayerMask targetLayers, bool pierce = false, PlayerStats stats = null, System.Action onHit = null)
    {
        this.prefabKey = prefabKey;
        this.direction = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector2.right;
        this.speed = speed;
        this.weaponDamage = weaponDamage;
        this.lifetimeRemaining = lifetime;
        this.targetLayers = targetLayers;
        this.pierce = pierce;
        this.stats = stats;
        this.onHit = onHit;
        isActive = true;
    }

    private void Update()
    {
        if (!isActive) return;

        transform.position += (Vector3)(direction * speed * Time.deltaTime);
        lifetimeRemaining -= Time.deltaTime;
        if (lifetimeRemaining <= 0f)
        {
            ReturnToPool();
        }
    }

private void OnTriggerEnter2D(Collider2D other)
    {
        if (!isActive) return;
        if (((1 << other.gameObject.layer) & targetLayers) == 0) return;

        var damageable = other.GetComponent<IDamageable>();
        if (damageable != null)
        {
            float rolledDamage = weaponDamage + (stats != null ? stats.RollAttackPower() : 0f);
            damageable.TakeDamage(rolledDamage);
            onHit?.Invoke();
        }

        if (!pierce)
        {
            ReturnToPool();
        }
    }

/// <summary>ProjectilePoolManager가 활성 투사체 한계치(maxActiveProjectiles)를 넘겼을 때, 아직 수명이
    /// 남아있거나 아무것도 맞히지 못한 상태라도 이 투사체를 강제로 풀에 반환하기 위해 호출한다
    /// (가장 먼저 생성된 투사체부터 회수되므로, 이 투사체는 onHit 호출 없이 그냥 사라진다).</summary>
    public void ForceReturnToPool()
    {
        if (!isActive) return;
        ReturnToPool();
    }


    private void ReturnToPool()
    {
        isActive = false;
        onHit = null;

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
