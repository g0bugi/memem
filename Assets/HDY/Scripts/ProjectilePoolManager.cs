using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 투사체 전용 풀 매니저. 프리팹별로 ObjectPool을 하나씩 관리하며, 원거리 무기의 Projectile.Launch가
/// Get으로 인스턴스를 꺼내 쓰고 명중/수명만료 시 Return으로 되돌린다.
/// 동시에 활성화될 수 있는 투사체 총 개수(모든 프리팹 합산)에 maxActiveProjectiles 한계치를 두고,
/// 이를 넘어서면 가장 먼저 생성된(오래된) 투사체부터 강제로 풀에 반환해 개수를 유지한다.
/// </summary>
public class ProjectilePoolManager : MonoBehaviour
{
    public static ProjectilePoolManager Instance { get; private set; }

    private readonly Dictionary<GameObject, ObjectPool> pools = new Dictionary<GameObject, ObjectPool>();

    [Tooltip("동시에 활성화될 수 있는 투사체(모든 종류 합산) 최대 개수. 초과 시 가장 먼저 생성된 투사체부터 강제로 풀에 반환한다.")]
    [SerializeField] private int maxActiveProjectiles = 500;

    // 생성(Get) 순서를 기록하는 연결 리스트. 맨 앞이 가장 먼저 생성된(가장 오래된) 투사체다.
    private readonly LinkedList<GameObject> activeOrder = new LinkedList<GameObject>();
    private readonly Dictionary<GameObject, LinkedListNode<GameObject>> activeNodes = new Dictionary<GameObject, LinkedListNode<GameObject>>();


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
        GameObject instance = pool.Get(position, rotation);

        TrackActive(instance);
        EnforceActiveLimit();

        return instance;
    }

public void Return(GameObject prefab, GameObject instance)
    {
        ObjectPool pool = GetOrCreatePool(prefab, 0);
        pool.Return(instance);
        UntrackActive(instance);
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

private void TrackActive(GameObject instance)
    {
        if (instance == null) return;

        // 이미 추적 중이던 인스턴스라면(비정상 상태 방지) 먼저 제거한 뒤 맨 뒤(최신)로 다시 등록한다.
        UntrackActive(instance);

        LinkedListNode<GameObject> node = activeOrder.AddLast(instance);
        activeNodes[instance] = node;
    }

    private void UntrackActive(GameObject instance)
    {
        if (instance == null) return;

        if (activeNodes.TryGetValue(instance, out LinkedListNode<GameObject> node))
        {
            activeOrder.Remove(node);
            activeNodes.Remove(instance);
        }
    }

    /// <summary>활성 투사체 수가 한계치(maxActiveProjectiles)를 넘으면, 가장 먼저 생성된 투사체부터
    /// 순서대로 강제 반환해서 다시 한계치 이하로 맞춘다.</summary>
    private void EnforceActiveLimit()
    {
        while (activeOrder.Count > maxActiveProjectiles)
        {
            GameObject oldest = activeOrder.First.Value;
            UntrackActive(oldest);

            if (oldest == null) continue;

            Projectile projectile = oldest.GetComponent<Projectile>();
            if (projectile != null)
            {
                projectile.ForceReturnToPool();
            }
            else
            {
                // Projectile 컴포넌트가 없는 예외적인 경우에도 최소한 비활성화는 해준다.
                oldest.SetActive(false);
            }
        }
    }

}
