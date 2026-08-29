using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 하나의 프리팹에 대한 재사용 오브젝트 풀. Destroy 없이 비활성화/활성화로 회수·재사용한다.
/// maxActiveCount(기본 무제한)를 넘어서 Get()이 호출되면, 가장 먼저 활성화됐던(가장 오래된) 인스턴스를
/// 강제로 풀에 반환해서 활성 개수를 유지한다 — 중후반에 오브젝트가 무한정 쌓여 렉이 생기는 것을 방지한다.
/// Return()은 이미 반환된(비활성) 인스턴스에 대해서는 아무 것도 하지 않는 멱등(idempotent) 동작이다.
/// 그래서 강제로 회수된 인스턴스를 원래 호출부(타이머 코루틴 등)가 나중에 다시 Return()하더라도
/// 같은 인스턴스가 큐에 중복으로 들어가는 일이 없다(이 방어 로직이 없으면, 강제 회수 후 그 인스턴스를
/// 다른 곳에서 재사용 중일 때 원래 호출부가 뒤늦게 또 반환하면서 같은 오브젝트가 두 곳에서 동시에
/// "활성"으로 취급되는 버그가 생길 수 있다).
/// </summary>
public class ObjectPool
{
    private readonly GameObject prefab;
    private readonly Transform poolParent;
    private readonly int maxActiveCount;
    private readonly Queue<GameObject> inactive = new Queue<GameObject>();

    // Get() 순서를 기록하는 연결 리스트. 맨 앞이 가장 먼저 활성화된(가장 오래된) 인스턴스다.
    private readonly LinkedList<GameObject> activeOrder = new LinkedList<GameObject>();
    private readonly Dictionary<GameObject, LinkedListNode<GameObject>> activeNodes = new Dictionary<GameObject, LinkedListNode<GameObject>>();

    public int ActiveCount => activeOrder.Count;
    public int InactiveCount => inactive.Count;

    /// <param name="maxActiveCount">동시에 활성화될 수 있는 최대 개수. 생략하면 사실상 무제한이다.</param>
    public ObjectPool(GameObject prefab, Transform poolParent, int prewarmCount, int maxActiveCount = int.MaxValue)
    {
        this.prefab = prefab;
        this.poolParent = poolParent;
        this.maxActiveCount = Mathf.Max(1, maxActiveCount);

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

    /// <summary>풀에서 하나를 꺼내 지정한 위치/회전으로 활성화한다. 부족하면 새로 생성한다.
    /// 활성 개수가 maxActiveCount를 넘으면, 가장 오래된 활성 인스턴스부터 강제로 회수해서 한계를 유지한다.</summary>
    public GameObject Get(Vector3 position, Quaternion rotation)
    {
        GameObject obj = inactive.Count > 0 ? inactive.Dequeue() : CreateInstance();
        obj.transform.SetPositionAndRotation(position, rotation);
        obj.SetActive(true);

        TrackActive(obj);
        EnforceActiveLimit();

        return obj;
    }

    /// <summary>사용이 끝난 오브젝트를 비활성화해 풀로 되돌린다. 이미 반환된(비활성) 인스턴스라면 아무 것도 하지 않는다.</summary>
    public void Return(GameObject obj)
    {
        if (obj == null || !UntrackActive(obj))
        {
            return;
        }

        obj.SetActive(false);
        obj.transform.SetParent(poolParent);
        inactive.Enqueue(obj);
    }

    private void TrackActive(GameObject obj)
    {
        // 이미 추적 중이던 인스턴스라면(비정상 상태 방지) 먼저 제거한 뒤 맨 뒤(최신)로 다시 등록한다.
        UntrackActive(obj);

        LinkedListNode<GameObject> node = activeOrder.AddLast(obj);
        activeNodes[obj] = node;
    }

    /// <returns>실제로 추적 목록에서 제거했으면 true, 이미 추적 중이 아니었다면(=이미 반환됨) false.</returns>
    private bool UntrackActive(GameObject obj)
    {
        if (!activeNodes.TryGetValue(obj, out LinkedListNode<GameObject> node))
        {
            return false;
        }

        activeOrder.Remove(node);
        activeNodes.Remove(obj);
        return true;
    }

    private void EnforceActiveLimit()
    {
        while (activeOrder.Count > maxActiveCount)
        {
            GameObject oldest = activeOrder.First.Value;
            Return(oldest);
        }
    }
}
