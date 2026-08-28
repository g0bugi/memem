using UnityEngine;
using UnityEngine.SceneManagement;

namespace KMS
{
    [DisallowMultipleComponent]
    public sealed class KmsSceneNavigator : MonoBehaviour
    {
        public const string StartSceneName = "StartScene";
        public const string WeaponSelectSceneName = "WeaponSelectScene";
        public const string GameSceneName = "GameScene";

        private bool isTransitioning;

        private void Awake()
        {
            Time.timeScale = 1f;
        }

        public void OpenWeaponSelectScene()
        {
            LoadScene(WeaponSelectSceneName);
        }

        public void OpenGameScene()
        {
            LoadScene(GameSceneName);
        }

        public void QuitGame()
        {
#if UNITY_EDITOR
            Debug.Log("[KMS] 게임 종료 버튼이 호출되었습니다. 빌드에서는 애플리케이션을 종료합니다.", this);
#else
            Application.Quit();
#endif
        }

        private void LoadScene(string sceneName)
        {
            if (isTransitioning)
            {
                return;
            }

            isTransitioning = true;
            Time.timeScale = 1f;
            SceneManager.LoadScene(sceneName);
        }
    }
}
