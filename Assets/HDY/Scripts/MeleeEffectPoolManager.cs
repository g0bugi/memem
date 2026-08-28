using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 근접 공격 이펙트(순수 비주얼) 전용 풀 매니저.
/// 무기마다 프리팹을 등록해두면, 같은 프리팹을 쓰는 무기끼리는 자연히 풀을 공유한다.
/// </summary>
public class MeleeEffectPoolManager : MonoBehaviour
{
    public static MeleeEffectPoolManager Instance { get; private set; }

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

    /// <summary>풀에서 이펙트를 꺼내 재생하고, lifetime 후 자동으로 풀에 반환한다.</summary>
    public void PlayImpact(GameObject prefab, Vector3 position, Quaternion rotation, float lifetime)
    {
        if (prefab == null) return;
        ObjectPool pool = GetOrCreatePool(prefab, 0);
        GameObject instance = pool.Get(position, rotation);
        StartCoroutine(ReturnAfterDelay(pool, instance, lifetime));
    }

    private ObjectPool GetOrCreatePool(GameObject prefab, int prewarmCount)
    {
        if (!pools.TryGetValue(prefab, out ObjectPool pool))
        {
            pool = new ObjectPool(prefab, transform, prewarmCount);
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
