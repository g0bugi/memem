using UnityEngine;
using TMPro;

/// <summary>
/// 플레이어가 소지한 골드를 TMP 텍스트에 표시한다.
/// PlayerHUD의 T_Gold 오브젝트에 붙여서 사용한다.
/// </summary>
[RequireComponent(typeof(TMP_Text))]
public class PlayerGoldUI : MonoBehaviour
{
    [Header("Target")]
    [Tooltip("비워두면 Player 태그를 가진 오브젝트에서 PlayerStats를 자동으로 찾는다.")]
    [SerializeField] private PlayerStats target;

    private TMP_Text goldText;
    private int lastGold = int.MinValue;

    private void Awake()
    {
        goldText = GetComponent<TMP_Text>();
    }

    private void Start()
    {
        if (target == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                target = playerObj.GetComponent<PlayerStats>();
            }
        }

        RefreshText();
    }

    private void Update()
    {
        if (target == null) return;
        if (target.Gold == lastGold) return;

        RefreshText();
    }

    private void RefreshText()
    {
        if (goldText == null) return;

        lastGold = target != null ? target.Gold : 0;
        goldText.text = lastGold.ToString();
    }
}
