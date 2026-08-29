using System.Collections;
using UnityEngine;

/// <summary>
/// 지정한 무기 id로 원거리 공격이 발생할 때마다, Weapon을 지정된 발사 자세(공격 위치/회전)로 이동시켰다가
/// 잠시 유지한 뒤 다시 기본 자세로 되돌리는 연출을 재생한다(활의 조준/발사 모션).
/// WeaponSwingAnimator(근접 스윙)와 달리 조준 방향에 따라 회전을 계산하지 않고, 항상 고정된
/// Position/Rotation(캐릭터가 우측을 바라볼 때 기준)만 사용한다. Weapon은 캐릭터 반전 시
/// (CharacterFacingFlip이 characterVisual의 localScale.x를 뒤집음) 그 하위 파츠로서 함께 자동으로
/// 좌우 반전되므로, 좌측을 바라볼 때도 별도 계산 없이 동일한 로컬 값을 그대로 써도 올바르게 반전되어 보인다.
/// PlayerAttack.RangedAttackPerformed 이벤트를 구독해서 실제 발사 시점과 정확히 동기화된다.
/// WeaponSwingAnimator처럼 Man_07(캐릭터 비주얼) 하위에 Weapon을 연결해 사용한다.
/// </summary>
public class WeaponDrawPoseAnimator : MonoBehaviour
{
    [Header("Target")]
    [Tooltip("이동/회전시킬 무기 파츠(Weapon) 트랜스폼")]
    [SerializeField] private Transform weapon;
    [Tooltip("이 무기 id로 원거리 공격할 때만 연출을 재생한다")]
    [SerializeField] private string weaponId = "bow";

    [Header("Attack Pose (우측을 바라볼 때 기준)")]
    [Tooltip("공격 시 Weapon이 이동할 로컬 위치. 캐릭터가 좌측을 바라보고 있어도(부모가 반전되어 있으므로) 이 값을 그대로 쓰면 자동으로 좌우 반전되어 보인다.")]
    [SerializeField] private Vector3 attackLocalPosition = new Vector3(0.2f, 0.67f, 0f);
    [Tooltip("공격 시 Weapon이 회전할 로컬 오일러 각도")]
    [SerializeField] private Vector3 attackLocalEulerAngles = new Vector3(0f, 0f, 73f);

    [Header("Timing")]
    [Tooltip("기본 자세에서 공격 자세로 이동하는 데 걸리는 시간(초)")]
    [SerializeField] private float approachDuration = 0.05f;
    [Tooltip("공격 자세를 유지하는 시간(초) — 조준/발사 순간을 표현한다")]
    [SerializeField] private float holdDuration = 0.15f;
    [Tooltip("공격 자세에서 기본 자세(Weapon의 원래 Position/Rotation)로 되돌아오는 데 걸리는 시간(초)")]
    [SerializeField] private float returnDuration = 0.1f;

    private PlayerAttack playerAttack;
    private Vector3 restLocalPosition;
    private Quaternion restLocalRotation;
    private Coroutine poseRoutine;

    private void Awake()
    {
        playerAttack = GetComponentInParent<PlayerAttack>();

        if (weapon != null)
        {
            restLocalPosition = weapon.localPosition;
            restLocalRotation = weapon.localRotation;
        }
    }

    private void OnEnable()
    {
        if (playerAttack != null)
        {
            playerAttack.RangedAttackPerformed += OnRangedAttackPerformed;
        }
    }

    private void OnDisable()
    {
        if (playerAttack != null)
        {
            playerAttack.RangedAttackPerformed -= OnRangedAttackPerformed;
        }
    }

    private void OnRangedAttackPerformed(WeaponData data, Vector2 aimDirection)
    {
        if (weapon == null || data == null || data.id != weaponId) return;

        if (poseRoutine != null)
        {
            StopCoroutine(poseRoutine);
        }

        poseRoutine = StartCoroutine(PoseRoutine());
    }

    private IEnumerator PoseRoutine()
    {
        Quaternion attackRot = Quaternion.Euler(attackLocalEulerAngles);

        // 1단계: 접근 - 기본 자세에서 공격 자세(attackLocalPosition/attackLocalEulerAngles)로 이동/회전한다.
        float t = 0f;
        while (t < approachDuration)
        {
            t += Time.deltaTime;
            float ratio = approachDuration > 0f ? Mathf.Clamp01(t / approachDuration) : 1f;
            weapon.localPosition = Vector3.Lerp(restLocalPosition, attackLocalPosition, ratio);
            weapon.localRotation = Quaternion.Slerp(restLocalRotation, attackRot, ratio);
            yield return null;
        }
        weapon.localPosition = attackLocalPosition;
        weapon.localRotation = attackRot;

        // 2단계: 유지 - 조준/발사 자세를 holdDuration만큼 그대로 유지한다.
        float holdTimer = 0f;
        while (holdTimer < holdDuration)
        {
            holdTimer += Time.deltaTime;
            yield return null;
        }

        // 3단계: 복귀 - 공격 자세에서 기본 자세로 되돌아온다.
        t = 0f;
        while (t < returnDuration)
        {
            t += Time.deltaTime;
            float ratio = returnDuration > 0f ? Mathf.Clamp01(t / returnDuration) : 1f;
            weapon.localPosition = Vector3.Lerp(attackLocalPosition, restLocalPosition, ratio);
            weapon.localRotation = Quaternion.Slerp(attackRot, restLocalRotation, ratio);
            yield return null;
        }
        weapon.localPosition = restLocalPosition;
        weapon.localRotation = restLocalRotation;

        poseRoutine = null;
    }
}
