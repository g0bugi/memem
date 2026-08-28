using UnityEngine;

/// <summary>
/// 지정된 타겟(플레이어)을 즉시(보간 없이) 따라가는 2D 카메라.
/// LateUpdate에서 처리해 플레이어가 이동을 마친 뒤에 위치를 갱신한다.
/// </summary>
public class CameraFollow2D : MonoBehaviour
{
    [SerializeField] private Transform target;
    
    [SerializeField] private Vector2 offset = Vector2.zero;


    private float fixedZ;

    private void Awake()
    {
        fixedZ = transform.position.z;

        if (target == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null) target = playerObj.transform;
        }
    }

private void LateUpdate()
{
    if (target == null) return;

    transform.position = new Vector3(target.position.x + offset.x, target.position.y + offset.y, fixedZ);
}
}
