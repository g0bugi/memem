using UnityEngine;

/// <summary>
/// 요술봉 등 Orbit 타입 무기의 구슬들을 캐릭터 주변에서 계속 회전시키는 컨트롤러.
/// 쿨타임 개념이 없는 패시브 무기라, WeaponInventory가 무기 획득 시점에 생성해서 붙여준다.
/// 구슬 프리팹이 없으면 판정용 콜라이더만 가진 빈 오브젝트로 대체해 로직은 바로 테스트할 수 있게 한다.
/// </summary>
public class OrbitWeaponController : MonoBehaviour
{
    private Transform followTarget;
    private float rotationSpeedDeg;
    private float currentAngle;
    private Transform[] orbs;

    public void Setup(Transform followTarget, WeaponData data, LayerMask targetLayers)
    {
        this.followTarget = followTarget;
        transform.position = followTarget.position;

        int orbCount = Mathf.Max(1, data.orbCount);
        rotationSpeedDeg = data.orbRotationPeriod > 0f ? 360f / data.orbRotationPeriod : 0f;

        orbs = new Transform[orbCount];
        for (int i = 0; i < orbCount; i++)
        {
            GameObject orbObj;
            if (data.orbPrefab != null)
            {
                orbObj = Instantiate(data.orbPrefab, transform);
            }
            else
            {
                orbObj = new GameObject($"Orb_{i}");
                orbObj.transform.SetParent(transform);
                CircleCollider2D col = orbObj.AddComponent<CircleCollider2D>();
                col.isTrigger = true;
                col.radius = 0.3f;
            }

            OrbHitbox hitbox = orbObj.GetComponent<OrbHitbox>();
            if (hitbox == null) hitbox = orbObj.AddComponent<OrbHitbox>();
            hitbox.Init(data.damage, targetLayers);

            orbs[i] = orbObj.transform;
        }

        PositionOrbs(data.orbRadius);
    }

    private float orbRadius;

    private void PositionOrbs(float radius)
    {
        orbRadius = radius;
        UpdateOrbPositions();
    }

    private void LateUpdate()
    {
        if (followTarget == null || orbs == null) return;

        transform.position = followTarget.position;
        currentAngle += rotationSpeedDeg * Time.deltaTime;
        UpdateOrbPositions();
    }

    private void UpdateOrbPositions()
    {
        if (orbs == null) return;

        for (int i = 0; i < orbs.Length; i++)
        {
            float angle = currentAngle + (360f / orbs.Length) * i;
            float rad = angle * Mathf.Deg2Rad;
            Vector3 offset = new Vector3(Mathf.Cos(rad), Mathf.Sin(rad), 0f) * orbRadius;
            orbs[i].position = transform.position + offset;
        }
    }
}
