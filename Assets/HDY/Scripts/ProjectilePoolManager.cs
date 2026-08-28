using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 투사체 전용 풀 매니저. 아직 원거리 무기 로직이 없어 Prewarm만 실제로 쓰이고,
/// Get/Return은 이후 원거리 공격을 구현할 때 사용할 수 있도록 미리 만들어둔다.
/// </summary>
public class ProjectilePoolManager : MonoBehaviour
{
    public static ProjectilePoolManager Instance { get; private set; }

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

    public void Prewarm(GameObject prefab, int count)
    {
        if (prefab == null) return;
        GetOrCreatePool(prefab, count);
    }

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
            pool = new ObjectPool(prefab, transform, prewarmCount);
            pools.Add(prefab, pool);
        }
        return pool;
    }
}
