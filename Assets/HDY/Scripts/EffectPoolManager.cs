using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 스스로 이동하지 않는 이펙트/장판류 오브젝트 전용 풀 매니저.
/// 근접 공격의 순수 비주얼 이펙트뿐 아니라, 메테오 폭발 후 생기는 불장판(FireFloorHazard)처럼
/// 한 자리에 머무는 오브젝트도 여기서 함께 관리한다. (움직이는 화살/메테오는 ProjectilePoolManager)
/// 무기마다 프리팹을 등록해두면, 같은 프리팹을 쓰는 무기끼리는 자연히 풀을 공유한다.
/// </summary>
public class EffectPoolManager : MonoBehaviour
{
    public static EffectPoolManager Instance { get; private set; }

    [Tooltip("프리팝(종류)별로 동시에 활성화될 수 있는 이펙트 최대 개수. 초과 시 가장 오래된 인스턴스부터 강제로 풀에 반환된다.")]
    [SerializeField, Min(1)] private int maxActivePerPrefab = 500;

    private readonly Dictionary<GameObject, ObjectPool> pools = new Dictionary<GameObject, ObjectPool>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    /// <summary>무기 획득 시 호출해 해당 프리팹의 풀을 미리 준비해둔다.</summary>
    public void Prewarm(GameObject prefab, int count)
    {
        if (prefab == null) return;
        GetOrCreatePool(prefab, count);
    }

    /// <summary>풀에서 이펙트를 꺼내 재생하고, lifetime 후 자동으로 풀에 반환한다. (순수 비주얼 이펙트용)</summary>
    public void PlayImpact(GameObject prefab, Vector3 position, Quaternion rotation, float lifetime)
    {
        if (prefab == null) return;
        ObjectPool pool = GetOrCreatePool(prefab, 0);
        GameObject instance = pool.Get(position, rotation);
        StartCoroutine(ReturnAfterDelay(pool, instance, lifetime));
    }

    /// <summary>풀에서 하나를 꺼내 활성화한다. 반환 타이밍은 호출부(예: FireFloorHazard)가 직접 관리한다.</summary>
    public GameObject Get(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        ObjectPool pool = GetOrCreatePool(prefab, 0);
        return pool.Get(position, rotation);
    }

    public void Return(GameObject prefab, GameObject instance)
    {
        ObjectPool pool = GetOrCreatePool(prefab, 0);
        pool.Return(instance);
    }

    private ObjectPool GetOrCreatePool(GameObject prefab, int prewarmCount)
    {
        if (!pools.TryGetValue(prefab, out ObjectPool pool))
        {
            pool = new ObjectPool(prefab, transform, prewarmCount, maxActivePerPrefab);
            pools.Add(prefab, pool);
        }
        return pool;
    }

    private IEnumerator ReturnAfterDelay(ObjectPool pool, GameObject instance, float delay)
    {
        yield return new WaitForSeconds(delay);
        pool.Return(instance);
    }
}
