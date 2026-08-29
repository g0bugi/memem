using UnityEngine;

namespace HDY
{
    /// <summary>
    /// Orbit 타입 무기(요술봉 등)의 런타임 컨트롤러. 캐릭터 주위를 도는 구슬들을 생성하고 매 프레임
    /// 위치를 갱신한다. 구슬 하나가 적을 맞출 때마다 OrbHitbox.HitLanded가 개별적으로 발생하고,
    /// 그때마다 바로 콤보를 1씩 올린다(무기 하나가 한 바퀴에 여러 번 맞히면 그만큼 여러 번 오른다).
    /// </summary>
    public class OrbitWeaponController : MonoBehaviour
    {
        private Transform owner;
        private WeaponData data;
        private ComboManager combo;

        private Transform[] orbs;
        private float baseOrbRadius;
        private float rotationSpeedDegPerSec;
        private float currentAngle;

        public void Setup(Transform owner, WeaponData data, LayerMask targetLayers, float attackPower, ComboManager comboManager = null)
        {
            this.owner = owner;
            this.data = data;
            this.combo = comboManager;

            baseOrbRadius = data.orbRadius;
            float period = Mathf.Max(0.01f, data.orbRotationPeriod);
            rotationSpeedDegPerSec = 360f / period;

            float damage = data.damage + attackPower;
            SpawnOrbs(damage, targetLayers);
        }

        private void SpawnOrbs(float damage, LayerMask targetLayers)
        {
            int count = Mathf.Max(1, data.orbCount);
            orbs = new Transform[count];
            GameObject prefab = data.ResolvedOrbPrefab;

            for (int i = 0; i < count; i++)
            {
                GameObject instance = prefab != null
                    ? Instantiate(prefab, transform)
                    : new GameObject($"Orb_{i}");

                if (prefab == null)
                {
                    instance.transform.SetParent(transform);
                }

                OrbHitbox hitbox = instance.GetComponent<OrbHitbox>();
                if (hitbox == null)
                {
                    hitbox = instance.AddComponent<OrbHitbox>();
                }

                hitbox.Init(damage, targetLayers);
                hitbox.HitLanded += HandleOrbHit;

                orbs[i] = instance.transform;
            }
        }

        private void HandleOrbHit()
        {
            combo?.RegisterHit();
        }

        private void LateUpdate()
        {
            if (owner == null || orbs == null || orbs.Length == 0) return;

            currentAngle += rotationSpeedDegPerSec * Time.deltaTime;

            float orbRadius = baseOrbRadius + (combo != null ? combo.OrbitRadiusBonus : 0f);
            float angleStep = 360f / orbs.Length;

            for (int i = 0; i < orbs.Length; i++)
            {
                if (orbs[i] == null) continue;

                float angleRad = (currentAngle + angleStep * i) * Mathf.Deg2Rad;
                Vector3 offset = new Vector3(Mathf.Cos(angleRad), Mathf.Sin(angleRad), 0f) * orbRadius;
                orbs[i].position = owner.position + offset;
            }
        }

        private void OnDestroy()
        {
            if (orbs == null) return;

            foreach (Transform orb in orbs)
            {
                if (orb == null) continue;

                OrbHitbox hitbox = orb.GetComponent<OrbHitbox>();
                if (hitbox != null)
                {
                    hitbox.HitLanded -= HandleOrbHit;
                }
            }
        }
    }
}
