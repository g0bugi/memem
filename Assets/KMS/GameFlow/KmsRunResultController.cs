using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace KMS
{
    /// <summary>
    /// 플레이어 사망 또는 스테이지 클리어(KmsRunTimer 시간 종료) 시 결과창(획득 골드/처치 몬스터 수/획득 무기)을
    /// 띄우는 컨트롤러. GameScene에 하나 배치한다. 결과창을 띄운 시점에
    /// KmsSceneNavigator.HasFinishedFirstRun을 true로 설정해서, WeaponSelectScene의 스탯 강화 UI가
    /// 다음 방문부터 보이게 만든다. 결과창의 확인 버튼은 기존 KmsSceneNavigator.OpenWeaponSelectScene을
    /// 그대로 사용하면 된다(별도 배선 불필요).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class KmsRunResultController : MonoBehaviour
    {
        [Header("Gameplay References (비워두면 자동으로 찾음)")]
        [SerializeField] private KmsRunTimer runTimer;
        [SerializeField] private PlayerStats playerStats;
        [SerializeField] private KmsMonsterSpawner monsterSpawner;
        [SerializeField] private WeaponInventory weaponInventory;

        [Header("Result Panel UI")]
        [SerializeField] private GameObject resultPanel;
        [SerializeField] private Text titleText;
        [SerializeField] private Text statsText;
        [SerializeField] private Transform weaponListContainer;

        [Header("Weapon Icon")]
        [SerializeField, Min(8f)] private float weaponIconSize = 64f;

        private int killCount;
        private int goldEarned;
        private bool hasShownResult;
        private bool isSubscribed;

        private void Awake()
        {
            if (runTimer == null) runTimer = FindFirstObjectByType<KmsRunTimer>();

            if (playerStats == null)
            {
                GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
                if (playerObject != null) playerStats = playerObject.GetComponent<PlayerStats>();
            }

            if (monsterSpawner == null) monsterSpawner = FindFirstObjectByType<KmsMonsterSpawner>();
            if (weaponInventory == null && playerStats != null) weaponInventory = playerStats.GetComponent<WeaponInventory>();
        }

        private void OnEnable()
        {
            Subscribe();
        }

        private void Start()
        {
            Subscribe();

            if (resultPanel != null)
            {
                resultPanel.SetActive(false);
            }
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        private void Subscribe()
        {
            if (isSubscribed) return;

            if (runTimer != null) runTimer.Expired += HandleClear;
            if (playerStats != null)
            {
                playerStats.Died += HandleDeath;
                playerStats.GoldGained += HandleGoldGained;
            }
            if (monsterSpawner != null) monsterSpawner.MonsterDied += HandleMonsterDied;

            isSubscribed = true;
        }

        private void Unsubscribe()
        {
            if (!isSubscribed) return;

            if (runTimer != null) runTimer.Expired -= HandleClear;
            if (playerStats != null)
            {
                playerStats.Died -= HandleDeath;
                playerStats.GoldGained -= HandleGoldGained;
            }
            if (monsterSpawner != null) monsterSpawner.MonsterDied -= HandleMonsterDied;

            isSubscribed = false;
        }

        private void HandleGoldGained(int amount)
        {
            goldEarned += amount;
        }

        private void HandleMonsterDied(KmsMonster monster)
        {
            killCount++;
        }

        private void HandleClear()
        {
            ShowResult("스테이지 클리어");
        }

        private void HandleDeath()
        {
            ShowResult("사망");
            if (runTimer != null)
            {
                runTimer.EndRun();
            }
        }

        private void ShowResult(string outcomeTitle)
        {
            if (hasShownResult) return;
            hasShownResult = true;

            KmsSceneNavigator.HasFinishedFirstRun = true;

            if (titleText != null) titleText.text = outcomeTitle;
            if (statsText != null) statsText.text = $"획득 골드  {goldEarned}\n처치한 몬스터  {killCount}마리";

            BuildWeaponList();

            if (resultPanel != null) resultPanel.SetActive(true);
            Time.timeScale = 0f;
        }

        private void BuildWeaponList()
        {
            if (weaponListContainer == null || weaponInventory == null) return;

            for (int i = weaponListContainer.childCount - 1; i >= 0; i--)
            {
                Destroy(weaponListContainer.GetChild(i).gameObject);
            }

            var counts = new Dictionary<WeaponData, int>();
            var order = new List<WeaponData>();
            foreach (ActiveWeapon weapon in weaponInventory.ActiveWeapons)
            {
                if (weapon?.Data == null) continue;
                if (!counts.ContainsKey(weapon.Data))
                {
                    counts[weapon.Data] = 0;
                    order.Add(weapon.Data);
                }
                counts[weapon.Data]++;
            }

            foreach (WeaponData data in order)
            {
                CreateWeaponEntry(data, counts[data]);
            }
        }

        private void CreateWeaponEntry(WeaponData data, int count)
        {
            GameObject entry = new GameObject($"Weapon_{data.id}", typeof(RectTransform));
            entry.transform.SetParent(weaponListContainer, false);
            RectTransform entryRect = entry.GetComponent<RectTransform>();
            entryRect.sizeDelta = new Vector2(weaponIconSize, weaponIconSize);

            GameObject iconObj = new GameObject("Icon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            iconObj.transform.SetParent(entry.transform, false);
            Image icon = iconObj.GetComponent<Image>();
            icon.sprite = data.ResolvedIcon;
            icon.preserveAspect = true;
            RectTransform iconRect = icon.rectTransform;
            iconRect.anchorMin = Vector2.zero;
            iconRect.anchorMax = Vector2.one;
            iconRect.offsetMin = Vector2.zero;
            iconRect.offsetMax = Vector2.zero;

            if (count > 1)
            {
                GameObject badgeObj = new GameObject("CountBadge", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
                badgeObj.transform.SetParent(entry.transform, false);
                Text badgeText = badgeObj.GetComponent<Text>();
                badgeText.text = $"x{count}";
                badgeText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                badgeText.fontSize = 16;
                badgeText.alignment = TextAnchor.LowerRight;
                badgeText.color = Color.white;
                RectTransform badgeRect = badgeText.rectTransform;
                badgeRect.anchorMin = Vector2.zero;
                badgeRect.anchorMax = Vector2.one;
                badgeRect.offsetMin = Vector2.zero;
                badgeRect.offsetMax = Vector2.zero;
            }
        }
    }
}
