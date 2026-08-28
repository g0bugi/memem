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

        /// <summary>스테이지(런)을 최소 한 번이라도 마치고 돎아온 적이 있는지. 정적 필드라 플레이 세션 내내 유지되며,
        /// WeaponSelectScene의 스탯 강화 UI를 처음 방문 때는 숨기고 이후부터 보여주는 데 쓴다.
        /// KmsRunResultController가 결과창을 띄우는 시점에 true로 설정한다.</summary>
        public static bool HasFinishedFirstRun { get; set; }

        
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
