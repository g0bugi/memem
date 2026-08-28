using UnityEngine;

/// <summary>
/// 직선으로 날아가는 투사체(화살 등). 적과 충돌하면 데미지를 주고 즉시 풀로 반환되며,
/// lifetime 동안 아무것도 맞추지 못하면 자동으로 풀로 반환된다.
/// 프리팹에는 Collider2D(Is Trigger 체크)가 있어야 충돌 판정이 동작한다.
/// </summary>
public class Projectile : MonoBehaviour
{
    [Tooltip("스프라이트 기본 상태(회전 0도)에서 머리(뾰족한 끝)가 향하는 각도. 0=오른쪽(+X), 90=위(+Y), -90=아래, 180=왼쪽. 이동 방향 각도에 이 값만큼 보정을 더해 머리가 항상 진행 방향(적)을 향하게 한다.")]
    [SerializeField] private float spriteForwardOffsetDeg = 0f;

    private GameObject prefabKey;
    private Vector2 direction;
    private float speed;
    private float damage;
    private float lifetimeRemaining;
    private LayerMask targetLayers;
    private bool isActive;

    public void Launch(GameObject prefabKey, Vector2 direction, float speed, float damage, float lifetime, LayerMask targetLayers)
    {
        this.prefabKey = prefabKey;
        this.direction = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector2.right;
        this.speed = speed;
        this.damage = damage;
        this.lifetimeRemaining = lifetime;
        this.targetLayers = targetLayers;
        isActive = true;

        float angleDeg = Mathf.Atan2(this.direction.y, this.direction.x) * Mathf.Rad2Deg + spriteForwardOffsetDeg;
        transform.rotation = Quaternion.Euler(0f, 0f, angleDeg);
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
            damageable.TakeDamage(damage);
        }

        ReturnToPool();
    }

    private void ReturnToPool()
    {
        isActive = false;

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
