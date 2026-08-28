using UnityEngine;

/// <summary>
/// WeaponInventory가 보유한 무기마다 쿨타임 슬롯(WeaponSlotUI)을 하나씩 생성해
/// 이 오브젝트(GridLayoutGroup) 아래에 배치한다.
/// WeaponInventory.WeaponAcquired 이벤트 하나로 씬 시작 시 지급되는 기본무기와
/// 이후 런타임에 획득하는 무기를 모두 처리한다. Awake에서 구독하므로
/// WeaponInventory.Start()가 실행되기 전에 항상 준비되어 있다.
/// 그리드 자체의 위치/크기 배치는 직접 인스펙터에서 조정하면 된다.
/// </summary>
public class WeaponSlotGrid : MonoBehaviour
{
    [Header("References")]
    [Tooltip("비워두면 Player 태그를 가진 오브젝트에서 자동으로 찾는다.")]
    [SerializeField] private WeaponInventory inventory;
    [Tooltip("슬롯 하나의 프리팹. Background/Icon/CooldownFill/CooldownText와 WeaponSlotUI를 포함해야 한다.")]
    [SerializeField] private GameObject slotPrefab;

    private void Awake()
    {
        if (inventory == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                inventory = playerObj.GetComponent<WeaponInventory>();
            }
        }

        if (inventory != null)
        {
            inventory.WeaponAcquired += OnWeaponAcquired;
        }
        else
        {
            Debug.LogWarning("[WeaponSlotGrid] WeaponInventory를 찾을 수 없어 쿨타임 슬롯을 생성할 수 없습니다.");
        }
    }

    private void OnDestroy()
    {
        if (inventory != null)
        {
            inventory.WeaponAcquired -= OnWeaponAcquired;
        }
    }

    private void OnWeaponAcquired(ActiveWeapon weapon)
    {
        if (slotPrefab == null)
        {
            Debug.LogWarning("[WeaponSlotGrid] slotPrefab이 비어있어 슬롯을 생성할 수 없습니다.");
            return;
        }

        GameObject slotObj = Instantiate(slotPrefab, transform);
        slotObj.SetActive(true);

        WeaponSlotUI slot = slotObj.GetComponent<WeaponSlotUI>();
        if (slot != null)
        {
            slot.Setup(weapon);
        }
    }
}
