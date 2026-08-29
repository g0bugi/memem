using UnityEngine;

/// <summary>
/// 투사체 프리팹에 붙이면 이동 방향과 무관하게 스프라이트가 매 프레임 시계방향으로 계속 회전한다.
/// (회전하는 도끼/표창 등 스핀 비주얼용). ProjectileAxisRotator와 함께 붙이면 서로의 회전을
/// 덮어쓸 수 있으니, 스핀 비주얼을 원하는 무기에는 이 컴포넌트만 붙이는 것을 권장한다.
/// </summary>
public class ProjectileSpinRotator : MonoBehaviour
{
    [Tooltip("초당 회전 각도(도). 값이 클수록 더 빠르게 시계방향으로 회전한다.")]
    [SerializeField] private float degreesPerSecond = 360f;

    private void Update()
    {
        transform.Rotate(0f, 0f, -degreesPerSecond * Time.deltaTime);
    }
}
