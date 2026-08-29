using System.Collections;
using UnityEngine;

namespace HDY
{
    /// <summary>
    /// 몬스터 타격 시 타격점(월드 좌표)에 데미지 숫자를 띄우는 창구.
    /// 새로운 풀을 따로 만들지 않고 기존 EffectPoolManager의 오브젝트 풀링을 그대로 재사용한다
    /// (프리팹 하나당 풀 하나를 자동으로 관리해주는 EffectPoolManager.Get/Return 활용).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class DamageNumberManager : MonoBehaviour
    {
        public static DamageNumberManager Instance { get; private set; }

        [Header("데미지 숫자 (EffectPoolManager 풀링 재사용)")]
        [Tooltip("World Space Canvas + TextMeshProUGUI + DamageNumberPopup가 붙어있는 프리팹")]
        [SerializeField] private DamageNumberPopup damageNumberPrefab;
        [SerializeField, Min(0)] private int prewarmCount = 20;
        [Tooltip("타격 위치 기준 표시 오프셋(몬스터 머리 위쪽으로 살짝 띄우는 용도)")]
        [SerializeField] private Vector3 spawnOffset = new Vector3(0f, 0.9f, 0f);

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        private void Start()
        {
            if (damageNumberPrefab != null && EffectPoolManager.Instance != null)
            {
                EffectPoolManager.Instance.Prewarm(damageNumberPrefab.gameObject, prewarmCount);
            }
        }

        /// <summary>지정한 월드 좌표(타격점)에 데미지 숫자를 하나 띄운다.</summary>
        public void ShowDamage(float amount, Vector3 worldPosition)
        {
            if (damageNumberPrefab == null || EffectPoolManager.Instance == null)
            {
                return;
            }

            GameObject prefabObject = damageNumberPrefab.gameObject;
            GameObject instance = EffectPoolManager.Instance.Get(prefabObject, worldPosition + spawnOffset, Quaternion.identity);
            DamageNumberPopup popup = instance.GetComponent<DamageNumberPopup>();
            if (popup == null)
            {
                EffectPoolManager.Instance.Return(prefabObject, instance);
                return;
            }

            popup.Play(FormatDamageText(amount));
            StartCoroutine(ReturnAfterDelay(prefabObject, instance, popup.TotalDuration));
        }

        private static string FormatDamageText(float amount)
        {
            return Mathf.RoundToInt(amount).ToString();
        }

        private IEnumerator ReturnAfterDelay(GameObject prefab, GameObject instance, float delay)
        {
            yield return new WaitForSeconds(delay);
            if (EffectPoolManager.Instance != null)
            {
                EffectPoolManager.Instance.Return(prefab, instance);
            }
        }
    }
}
