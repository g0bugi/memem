using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 하나의 프리팹에 대한 재사용 오브젝트 풀. Destroy 없이 비활성화/활성화로 회수·재사용한다.
/// </summary>
public class ObjectPool
{
    private readonly GameObject prefab;
    private readonly Transform poolParent;
    private readonly Queue<GameObject> inactive = new Queue<GameObject>();

    public ObjectPool(GameObject prefab, Transform poolParent, int prewarmCount)
    {
        this.prefab = prefab;
        this.poolParent = poolParent;

        for (int i = 0; i < prewarmCount; i++)
        {
            inactive.Enqueue(CreateInstance());
        }
    }

    private GameObject CreateInstance()
    {
        GameObject obj = Object.Instantiate(prefab, poolParent);
        obj.SetActive(false);
        return obj;
    }

    /// <summary>풀에서 하나를 꺼내 지정한 위치/회전으로 활성화한다. 부족하면 새로 생성한다.</summary>
    public GameObject Get(Vector3 position, Quaternion rotation)
    {
        GameObject obj = inactive.Count > 0 ? inactive.Dequeue() : CreateInstance();
        obj.transform.SetPositionAndRotation(position, rotation);
        obj.SetActive(true);
        return obj;
    }

    /// <summary>사용이 끝난 오브젝트를 비활성화해 풀로 되돌린다.</summary>
    public void Return(GameObject obj)
    {
        obj.SetActive(false);
        obj.transform.SetParent(poolParent);
        inactive.Enqueue(obj);
    }
}
