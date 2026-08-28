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

        private float remainingSeconds;
        private bool hasEnded;

        public float DurationSeconds => durationSeconds;
        public float RemainingSeconds => remainingSeconds;
<<<<<<< Updated upstream
        public float ElapsedSeconds => Mathf.Max(0f, durationSeconds - remainingSeconds);
        public bool HasEnded => hasEnded;
=======
        /// <summary>제한 시간이 다 되어 런이 종료될 때 발동된다(스테이지 클리어 판정에 사용).</summary>
        public event System.Action Expired;
>>>>>>> Stashed changes

        
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



public void Configure(float seconds, Text timeText)
        {
            durationSeconds = Mathf.Max(1f, seconds);
            remainingTimeText = timeText;
        }

public void EndRun()
        {
            if (hasEnded)
            {
                return;
            }

            hasEnded = true;
            remainingSeconds = 0f;
            UpdateTimeText();

            Expired?.Invoke();
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
