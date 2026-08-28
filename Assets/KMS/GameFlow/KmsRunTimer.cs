using UnityEngine;
using UnityEngine.UI;

namespace KMS
{
    [DisallowMultipleComponent]
    public sealed class KmsRunTimer : MonoBehaviour
    {
        [Header("Run Duration")]
        [SerializeField, Min(1f)] private float durationSeconds = 20f;

        [Header("UI")]
        [SerializeField] private Text remainingTimeText;
        [SerializeField] private GameObject gameOverPanel;

        private float remainingSeconds;
        private bool hasEnded;

        public float DurationSeconds => durationSeconds;
        public float RemainingSeconds => remainingSeconds;
        public float ElapsedSeconds => Mathf.Max(0f, durationSeconds - remainingSeconds);
        public bool HasEnded => hasEnded;

        private void Awake()
        {
            ResetForNewRun();
        }

        public void ResetForNewRun()
        {
            Time.timeScale = 1f;
            remainingSeconds = Mathf.Max(1f, durationSeconds);
            hasEnded = false;

            if (gameOverPanel != null)
            {
                gameOverPanel.SetActive(false);
            }

            UpdateTimeText();
        }

        private void Update()
        {
            if (hasEnded)
            {
                return;
            }

            remainingSeconds = Mathf.Max(0f, remainingSeconds - Time.deltaTime);
            UpdateTimeText();

            if (remainingSeconds <= 0f)
            {
                EndRun();
            }
        }

        private void OnDisable()
        {
            if (hasEnded)
            {
                Time.timeScale = 1f;
            }
        }

        public void Configure(float seconds, Text timeText, GameObject endPanel)
        {
            durationSeconds = Mathf.Max(1f, seconds);
            remainingTimeText = timeText;
            gameOverPanel = endPanel;

            if (gameOverPanel != null)
            {
                gameOverPanel.SetActive(false);
            }
        }

        [ContextMenu("End Run Now")]
        public void EndRun()
        {
            if (hasEnded)
            {
                return;
            }

            hasEnded = true;
            remainingSeconds = 0f;
            UpdateTimeText();

            if (gameOverPanel != null)
            {
                gameOverPanel.SetActive(true);
            }
            else
            {
                Debug.LogError("[KMS] 게임 종료 패널 참조가 없습니다.", this);
            }

            Time.timeScale = 0f;
        }

        private void UpdateTimeText()
        {
            if (remainingTimeText == null)
            {
                return;
            }

            int totalSeconds = Mathf.CeilToInt(remainingSeconds);
            int minutes = totalSeconds / 60;
            int seconds = totalSeconds % 60;
            remainingTimeText.text = $"TIME  {minutes:00}:{seconds:00}";
        }
    }
}
