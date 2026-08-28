using UnityEngine;

/// <summary>
/// 지정된 타겟(플레이어)을 부드럽게 따라가는 2D 카메라.
/// LateUpdate에서 처리해 플레이어가 이동을 마친 뒤에 위치를 갱신한다.
/// </summary>
public class CameraFollow2D : MonoBehaviour
{
    [SerializeField] private Transform target;
    [Tooltip("목표 지점에 도달하는 데 걸리는 대략적인 시간(초). 작을수록 더 빠르게(딱 붙게) 따라간다.")]
    [SerializeField] private float smoothTime = 0.15f;
    [SerializeField] private Vector2 offset = Vector2.zero;

    private Vector3 velocity;
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

        Vector3 desiredPosition = new Vector3(target.position.x + offset.x, target.position.y + offset.y, fixedZ);
        transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref velocity, smoothTime);
    }
}
