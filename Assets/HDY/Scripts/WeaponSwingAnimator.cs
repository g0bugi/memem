using System.Collections;
using UnityEngine;

/// <summary>
/// 지정한 무기 id로 근접 공격이 발생할 때마다, Weapon을 공격 위치(attackLocalPosition)로 이동시키고
/// point(자식, 손잡이 위치)를 회전 중심으로 삼아 무기 판정 각도(WeaponData.angle)만큼 부채꼴로 휘두른다.
/// 평소(대기 상태)에는 Weapon이 자신의 기본 위치/회전(에디터에서 설정한 값, Rotation은 0)에 고정되어 있다가,
/// 공격이 시작되면 공격 위치로 이동 + 마우스 방향으로 회전하고, 공격이 끝나면 다시 기본 위치/회전으로 보간되며 돌아온다.
/// PlayerAttack.MeleeAttackPerformed 이벤트를 구독해서 실제 공격 판정 시점과 정확히 동기화된다.
/// Man_07처럼 파츠가 분리된 캐릭터의 상위(PlayerPrefab 등)에 붙여서 Weapon/point를 연결해 사용한다.
/// </summary>
public class WeaponSwingAnimator : MonoBehaviour
{
    [Header("Target")]
    [Tooltip("이동/회전시킬 무기 파츠(Weapon) 트랜스폼")]
    [SerializeField] private Transform weapon;
    [Tooltip("Weapon의 자식 오브젝트로, 손잡이 위치이자 회전 중심이 되는 point")]
    [SerializeField] private Transform point;
    [Tooltip("이 무기 id로 근접 공격할 때만 스윙을 재생한다")]
    [SerializeField] private string weaponId = "dagger";

    [Header("Attack Position")]
    [Tooltip("공격 시 point가 위치할 목표 지점의 로컬 좌표(Weapon의 부모 기준 좌표계)")]
    [SerializeField] private Vector3 attackLocalPosition = new Vector3(0f, 1f, 0f);

    [Header("Timing")]
    [Tooltip("공격 시작 시 기본 위치에서 공격 위치로 이동하며 스윙 시작 각도까지 회전하는 데 걸리는 시간(초)")]
    [SerializeField] private float approachDuration = 0.05f;
    [Tooltip("공격 위치(point 고정)에서 실제 부채꼴 스윙이 재생되는 시간(초)")]
    [SerializeField] private float swingDuration = 0.15f;
    [Tooltip("스윙이 끝난 뒤 기본 위치/회전(Rotation 0)으로 돌아오는 데 걸리는 시간(초)")]
    [SerializeField] private float returnDuration = 0.1f;

    [Header("Sprite Orientation")]
    [Tooltip("무기 스프라이트가 그려진 기본 방향과 실제 조준 각도(atan2 기준, 0=+X) 사이의 보정값(도). " +
        "스윙 방향이 실제 공격 판정(부채꼴)과 어긋나 보이면 이 값을 조정해서 맞춘다.")]
    [SerializeField] private float spriteForwardOffsetDeg = -150f;

    private PlayerAttack playerAttack;
    private Vector3 restLocalPosition;
    private Quaternion restLocalRotation;
    private Vector3 pointLocalOffset;
    private Coroutine swingRoutine;

private void Awake()
    {
        playerAttack = GetComponentInParent<PlayerAttack>();

        if (weapon != null)
        {
            restLocalPosition = weapon.localPosition;
            restLocalRotation = weapon.localRotation;
        }

        if (point != null)
        {
            pointLocalOffset = point.localPosition;
        }
    }

    private void OnEnable()
    {
        if (playerAttack != null)
        {
            playerAttack.MeleeAttackPerformed += OnMeleeAttackPerformed;
        }
    }

    private void OnDisable()
    {
        if (playerAttack != null)
        {
            playerAttack.MeleeAttackPerformed -= OnMeleeAttackPerformed;
        }
    }

    private void OnMeleeAttackPerformed(WeaponData data, Vector2 aimDirection)
    {
        if (weapon == null || point == null || data == null || data.id != weaponId) return;

        if (swingRoutine != null)
        {
            StopCoroutine(swingRoutine);
        }

        swingRoutine = StartCoroutine(SwingRoutine(data.angle, aimDirection));
    }

private IEnumerator SwingRoutine(float coneAngle, Vector2 aimDirection)
    {
        Transform parent = weapon.parent;

        // Man_07(parent)의 localScale.x가 음수면 캐릭터가 좌우 반전된 상태다(CharacterFacingFlip).
        // 이 경우 스프라이트가 거울처럼 보이기 때문에, 계산된 조준 각도를 대칭(미러) 시켜서 사용해야
        // 실제 화면에서도 마우스를 정확히 향한다.
        //
        // 주의: 이 아래 모든 계산은 weapon.position/rotation(월드) 대신 weapon.localPosition/localRotation만 쓴다.
        // Unity는 음수 스케일을 가진 부모 밑에서 '월드 회전값'을 직접 설정할 때 내부적으로 역행렬을 푸는 과정에서
        // 의도하지 않은 왜곡(shear)이 생길 수 있어서, 로커(부모 기준) 계산만으로 처리해야 안전하다.
        bool facingLeft = parent != null && parent.localScale.x < 0f;

        float trueAimAngle = Mathf.Atan2(aimDirection.y, aimDirection.x) * Mathf.Rad2Deg;
        float halfAngle = coneAngle * 0.5f;

        // 미러링(180-각도) 보정: 부모가 X축으로 반전되면, 회전으로 생긴 시각적 방향이 X축 기준으로 또 한번 뒤집힌다.
        // 따라서 맞추려면 입력 각도를 (180-실제각도)로, 부채꼴 반폭(halfAngle)은 부호를 뒤집어서 쓰면 된다.
        float effectiveAimAngle = facingLeft ? (180f - trueAimAngle) : trueAimAngle;
        float effectiveHalfAngle = facingLeft ? -halfAngle : halfAngle;
        float centerAngle = effectiveAimAngle + spriteForwardOffsetDeg;

        Quaternion startRot = Quaternion.Euler(0f, 0f, centerAngle - effectiveHalfAngle);
        Quaternion endRot = Quaternion.Euler(0f, 0f, centerAngle + effectiveHalfAngle);

        // point의 기본(대기) 위치를 Man_07 로커 좌표계로 계산(weapon의 로커 위치/회전만 사용, 스케일 없음)
        Vector3 restPointLocalPos = restLocalPosition + restLocalRotation * pointLocalOffset;

        // 1단계: 접근 - point가 기본 위치에서 공격 위치(attackLocalPosition)로 이동하며,
        // 회전도 스윙 시작 각도까지 함께 맞춘다. Weapon의 로커 위치(피벗)는 'point가 목표 지점에 오도록' 역산한다.
        float t = 0f;
        while (t < approachDuration)
        {
            t += Time.deltaTime;
            float ratio = approachDuration > 0f ? Mathf.Clamp01(t / approachDuration) : 1f;
            Quaternion currentRot = Quaternion.Slerp(restLocalRotation, startRot, ratio);
            Vector3 pointLocalPos = Vector3.Lerp(restPointLocalPos, attackLocalPosition, ratio);
            weapon.localRotation = currentRot;
            weapon.localPosition = pointLocalPos - currentRot * pointLocalOffset;
            yield return null;
        }

        // 2단계: 스윙 - point를 공격 위치에 고정한 채, 무기가 판정 콘 각도(coneAngle)만큼 부채꼴로 휘두른다.
        t = 0f;
        while (t < swingDuration)
        {
            t += Time.deltaTime;
            float ratio = swingDuration > 0f ? Mathf.Clamp01(t / swingDuration) : 1f;
            Quaternion currentRot = Quaternion.Slerp(startRot, endRot, ratio);
            weapon.localRotation = currentRot;
            weapon.localPosition = attackLocalPosition - currentRot * pointLocalOffset;
            yield return null;
        }

        // 3단계: 복귀 - point를 공격 위치에서 기본 위치로, 회전도 원래 자세(Rotation 0)로 동시에 되돌린다.
        t = 0f;
        while (t < returnDuration)
        {
            t += Time.deltaTime;
            float ratio = returnDuration > 0f ? Mathf.Clamp01(t / returnDuration) : 1f;
            Quaternion currentRot = Quaternion.Slerp(endRot, restLocalRotation, ratio);
            Vector3 pointLocalPos = Vector3.Lerp(attackLocalPosition, restPointLocalPos, ratio);
            weapon.localRotation = currentRot;
            weapon.localPosition = pointLocalPos - currentRot * pointLocalOffset;
            yield return null;
        }
        weapon.localPosition = restLocalPosition;
        weapon.localRotation = restLocalRotation;

        swingRoutine = null;
    }
}
