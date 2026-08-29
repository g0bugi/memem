using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace HDY
{
    /// <summary>
    /// Common 등급이 아닌 무기를 이번 런에서 처음 획득했을 때, 무기 아이콘과 이름을 잠깐(기본 1초)
    /// 보여주는 HUD 팝업. PlayerHUD 프리팹의 자식으로 배치해서 쓴다.
    /// 같은 무기 종류를 이후 다시(중복) 획득해도 이번 런에서 이미 한 번 떴다면 다시 뜨지 않는다 —
    /// seenWeaponIds는 이 컴포넌트의 인스턴스 필드라 GameScene이 새로 로드될 때마다(=새 런) 자동으로
    /// 비워진 상태로 다시 시작된다.
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class WeaponAcquiredPopupUI : MonoBehaviour
    {
        [Header("Target")]
        [Tooltip("비워두면 Player 태그를 가진 오브젝트에서 WeaponInventory를 자동으로 찾는다.")]
        [SerializeField] private WeaponInventory target;

        [Header("표시할 UI")]
        [SerializeField] private Image iconImage;
        [SerializeField] private TMP_Text nameText;

        [Header("타이밍")]
        [Tooltip("팝업이 화면에 유지되는 시간(초)")]
        [SerializeField, Min(0f)] private float displayDuration = 1f;

        private CanvasGroup canvasGroup;
        private Coroutine hideRoutine;
        private readonly HashSet<string> seenWeaponIds = new HashSet<string>();

        private void Awake()
        {
            canvasGroup = GetComponent<CanvasGroup>();
            SetVisible(false);
        }

        private void Start()
        {
            if (target == null)
            {
                GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
                if (playerObj != null)
                {
                    target = playerObj.GetComponent<WeaponInventory>();
                }
            }

            if (target != null)
            {
                target.WeaponAcquired += HandleWeaponAcquired;
            }
        }

        private void OnDestroy()
        {
            if (target != null)
            {
                target.WeaponAcquired -= HandleWeaponAcquired;
            }
        }

        private void HandleWeaponAcquired(ActiveWeapon weapon)
        {
            WeaponData data = weapon != null ? weapon.Data : null;
            if (data == null || data.grade == ItemGrade.Common) return;
            if (!seenWeaponIds.Add(data.id)) return; // 이번 런에서 이미 한 번 떴던 무기 종류면 무시

            Show(data);
        }

        private void Show(WeaponData data)
        {
            if (iconImage != null)
            {
                Sprite icon = data.ResolvedIcon;
                iconImage.sprite = icon;
                iconImage.enabled = icon != null;
            }

            if (nameText != null)
            {
                nameText.text = data.weaponName;
            }

            if (hideRoutine != null)
            {
                StopCoroutine(hideRoutine);
            }

            SetVisible(true);
            hideRoutine = StartCoroutine(HideAfterDelay());
        }

        private IEnumerator HideAfterDelay()
        {
            yield return new WaitForSeconds(displayDuration);
            SetVisible(false);
            hideRoutine = null;
        }

        private void SetVisible(bool visible)
        {
            if (canvasGroup == null) return;
            canvasGroup.alpha = visible ? 1f : 0f;
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;
        }
    }
}
