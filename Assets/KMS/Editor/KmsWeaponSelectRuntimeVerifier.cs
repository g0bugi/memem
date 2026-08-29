using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace KMS.Editor
{
    [InitializeOnLoad]
    public static class KmsWeaponSelectRuntimeVerifier
    {
        private const string ScenePath = "Assets/Scenes/WeaponSelectScene.unity";
        private const string RunningKey = "KMS.WeaponSelectSmoke.Running";
        private const string FinishingKey = "KMS.WeaponSelectSmoke.Finishing";
        private const string ExitCodeKey = "KMS.WeaponSelectSmoke.ExitCode";
        private static int stage;
        private static double stageStartedAt;

        static KmsWeaponSelectRuntimeVerifier()
        {
            if (!SessionState.GetBool(RunningKey, false)) return;

            EditorApplication.playModeStateChanged -= HandlePlayModeStateChanged;
            EditorApplication.playModeStateChanged += HandlePlayModeStateChanged;
            if (EditorApplication.isPlaying) BeginPlayModeVerification();
        }

        public static void RunFromCommandLine()
        {
            SessionState.SetBool(RunningKey, true);
            SessionState.SetBool(FinishingKey, false);
            SessionState.SetInt(ExitCodeKey, 1);
            KmsSceneNavigator.HasFinishedFirstRun = false;
            KmsCharacterSelectionState.SelectDaggerMan07();

            EditorApplication.playModeStateChanged -= HandlePlayModeStateChanged;
            EditorApplication.playModeStateChanged += HandlePlayModeStateChanged;
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            EditorApplication.EnterPlaymode();
        }

        private static void HandlePlayModeStateChanged(PlayModeStateChange state)
        {
            if (!SessionState.GetBool(RunningKey, false)) return;

            if (state == PlayModeStateChange.EnteredPlayMode)
            {
                BeginPlayModeVerification();
                return;
            }

            if (state == PlayModeStateChange.EnteredEditMode &&
                SessionState.GetBool(FinishingKey, false))
            {
                int exitCode = SessionState.GetInt(ExitCodeKey, 1);
                SessionState.EraseBool(RunningKey);
                SessionState.EraseBool(FinishingKey);
                SessionState.EraseInt(ExitCodeKey);
                EditorApplication.playModeStateChanged -= HandlePlayModeStateChanged;
                EditorApplication.Exit(exitCode);
            }
        }

        private static void BeginPlayModeVerification()
        {
            stage = 0;
            stageStartedAt = EditorApplication.timeSinceStartup;
            EditorApplication.update -= Tick;
            EditorApplication.update += Tick;
        }

        private static void Tick()
        {
            try
            {
                switch (stage)
                {
                    case 0:
                        if (!Waited(0.2d)) return;
                        VerifyInitialCharacterSelection();
                        Advance();
                        break;
                    case 1:
                        RequireFlow().SelectBowCharacter();
                        Advance();
                        break;
                    case 2:
                        VerifyStageSelection();
                        RequireFlow().EnterStageOne();
                        Advance();
                        break;
                    case 3:
                        if (!WaitForScene(KmsSceneNavigator.GameSceneName, 5d)) return;
                        VerifySelectedLoadout("bow", "Man_06");
                        PlayerStats player = UnityEngine.Object.FindFirstObjectByType<PlayerStats>();
                        Require(player != null, "사망 복귀 검증 중 PlayerStats를 찾지 못했습니다.");
                        player.TakeDamage(float.MaxValue);
                        Advance();
                        break;
                    case 4:
                        if (!Waited(0.1d)) return;
                        Require(KmsSceneNavigator.HasFinishedFirstRun,
                            "플레이어 사망 뒤 첫 런 완료 상태가 기록되지 않았습니다.");
                        Require(FindGameObject("GameOverPanel").activeInHierarchy,
                            "플레이어 사망 뒤 결과 패널이 열리지 않았습니다.");
                        FindButton("ReturnToWeaponSelectButton").onClick.Invoke();
                        Advance();
                        break;
                    case 5:
                        if (!WaitForScene(KmsSceneNavigator.WeaponSelectSceneName, 5d) || !Waited(0.2d)) return;
                        GameObject upgradePanel = FindGameObject("UpgradePanel");
                        Require(upgradePanel.activeInHierarchy,
                            "런 종료 후 WeaponSelectScene에 돌아왔지만 강화 패널이 표시되지 않았습니다.");
                        FindButton("CloseButton").onClick.Invoke();
                        KmsWeaponSelectFlowUI flow = RequireFlow();
                        flow.SelectDaggerCharacter();
                        flow.EnterStageOne();
                        Advance();
                        break;
                    case 6:
                        if (!WaitForScene(KmsSceneNavigator.GameSceneName, 5d)) return;
                        VerifySelectedLoadout("dagger", "Man_07");
                        KmsRunTimer timer = UnityEngine.Object.FindFirstObjectByType<KmsRunTimer>();
                        Require(timer != null, "성공 종료 복귀 검증 중 KmsRunTimer를 찾지 못했습니다.");
                        timer.EndRun();
                        Advance();
                        break;
                    case 7:
                        if (!Waited(0.1d)) return;
                        Require(FindGameObject("GameOverPanel").activeInHierarchy,
                            "타이머 종료 뒤 결과 패널이 열리지 않았습니다.");
                        FindButton("ReturnToWeaponSelectButton").onClick.Invoke();
                        Advance();
                        break;
                    case 8:
                        if (!WaitForScene(KmsSceneNavigator.WeaponSelectSceneName, 5d) || !Waited(0.2d)) return;
                        Require(FindGameObject("UpgradePanel").activeInHierarchy,
                            "타이머 종료 후 WeaponSelectScene에 돌아왔지만 강화 패널이 표시되지 않았습니다.");
                        FinishSuccessfully();
                        break;
                }
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                RequestExit(1);
            }
        }

        private static void VerifyInitialCharacterSelection()
        {
            Require(SceneManager.GetActiveScene().name == KmsSceneNavigator.WeaponSelectSceneName,
                "WeaponSelectScene에서 검증이 시작되지 않았습니다.");
            Require(FindGameObject("CharacterSelectionPanel").activeInHierarchy,
                "첫 방문에 캐릭터 선택 패널이 보이지 않습니다.");
            Require(!FindGameObject("StageSelectionPanel").activeSelf,
                "첫 방문에 스테이지 선택 패널이 먼저 열렸습니다.");
            Require(!FindGameObject("UpgradePanel").activeSelf,
                "첫 방문에는 강화 패널이 숨겨져 있어야 합니다.");

            Require(FindButton("DaggerCharacterButton").interactable,
                "Man_07 단검 캐릭터가 선택 가능하지 않습니다.");
            Require(FindButton("BowCharacterButton").interactable,
                "Man_06 활 캐릭터가 선택 가능하지 않습니다.");
            Require(!FindButton("LongswordCharacterButton").interactable,
                "Man_03 장검 캐릭터는 선택 불가여야 합니다.");
            Require(!FindButton("CrossbowCharacterButton").interactable,
                "Man_04 석궁 캐릭터는 선택 불가여야 합니다.");
            Require(!FindSceneObjects<Text>().Any(text => text.text.Contains("Man_")),
                "캐릭터 선택 화면에 내부 에셋명이 노출되고 있습니다.");
        }

        private static void VerifyStageSelection()
        {
            Require(!FindGameObject("CharacterSelectionPanel").activeSelf,
                "캐릭터 선택 뒤 캐릭터 패널이 닫히지 않았습니다.");
            Require(FindGameObject("StageSelectionPanel").activeInHierarchy,
                "캐릭터 선택 뒤 스테이지 선택 패널이 열리지 않았습니다.");
            Require(!FindSceneObjects<Text>().Any(text => text.text.Contains("Man_")),
                "스테이지 선택 화면에 내부 에셋명이 노출되고 있습니다.");
            Require(FindButton("Stage01Button").interactable,
                "첫 번째 스테이지가 선택 가능하지 않습니다.");
            Require(!FindButton("Stage02Button").interactable &&
                    !FindButton("Stage03Button").interactable &&
                    !FindButton("Stage04Button").interactable,
                "잠긴 스테이지 중 선택 가능한 버튼이 있습니다.");

            int lockCount = FindSceneObjects<Image>()
                .Count(image => image.gameObject.name == "LockIcon" && image.sprite != null);
            Require(lockCount == 3, $"잠긴 스테이지 자물쇠 아이콘은 3개여야 하지만 {lockCount}개입니다.");

            string[] buttonNames = { "Stage01Button", "Stage02Button", "Stage03Button", "Stage04Button" };
            string[] spriteNames = { "111", "222", "333", "444" };
            string[] stageNames = { "시련", "증명", "변화", "인정" };
            for (int i = 0; i < buttonNames.Length; i++)
            {
                GameObject card = FindGameObject(buttonNames[i]);
                RectTransform cardRect = card.GetComponent<RectTransform>();
                Require(cardRect != null && Mathf.Approximately(cardRect.sizeDelta.x, cardRect.sizeDelta.y),
                    $"{buttonNames[i]}의 버튼 영역이 정사각형이 아닙니다.");

                Image stageImage = card.GetComponentsInChildren<Image>(true)
                    .FirstOrDefault(image => image.gameObject.name == "StageImage");
                Require(stageImage != null && stageImage.sprite != null && stageImage.sprite.name == spriteNames[i],
                    $"{buttonNames[i]}에 예상 스프라이트 {spriteNames[i]}가 연결되지 않았습니다.");

                Text stageName = card.GetComponentsInChildren<Text>(true)
                    .FirstOrDefault(text => text.gameObject.name == "StageName");
                Require(stageName != null && stageName.text == stageNames[i],
                    $"{buttonNames[i]}의 스테이지 이름이 '{stageNames[i]}'가 아닙니다.");
            }
        }

        private static void VerifySelectedLoadout(string weaponId, string characterName)
        {
            WeaponInventory inventory = UnityEngine.Object.FindFirstObjectByType<WeaponInventory>();
            Require(inventory != null, "GameScene에서 WeaponInventory를 찾지 못했습니다.");
            Require(inventory.ActiveWeapons.Count == 1 &&
                    inventory.ActiveWeapons[0].Data != null &&
                    inventory.ActiveWeapons[0].Data.id == weaponId,
                $"선택한 시작 무기 '{weaponId}'가 단독으로 적용되지 않았습니다.");

            Transform character = inventory.GetComponentsInChildren<Transform>(true)
                .FirstOrDefault(candidate => candidate.name == characterName);
            Require(character != null, $"선택한 캐릭터 외형 '{characterName}'가 적용되지 않았습니다.");
        }

        private static KmsWeaponSelectFlowUI RequireFlow()
        {
            KmsWeaponSelectFlowUI flow = UnityEngine.Object.FindFirstObjectByType<KmsWeaponSelectFlowUI>();
            Require(flow != null, "KmsWeaponSelectFlowUI를 찾지 못했습니다.");
            return flow;
        }

        private static Button FindButton(string name)
        {
            Button button = FindSceneObjects<Button>().FirstOrDefault(candidate => candidate.name == name);
            Require(button != null, $"버튼을 찾지 못했습니다: {name}");
            return button;
        }

        private static GameObject FindGameObject(string name)
        {
            Transform transform = FindSceneObjects<Transform>().FirstOrDefault(candidate => candidate.name == name);
            Require(transform != null, $"오브젝트를 찾지 못했습니다: {name}");
            return transform.gameObject;
        }

        private static T[] FindSceneObjects<T>() where T : Component
        {
            Scene scene = SceneManager.GetActiveScene();
            return scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<T>(true))
                .ToArray();
        }

        private static bool WaitForScene(string sceneName, double timeout)
        {
            if (SceneManager.GetActiveScene().name == sceneName) return true;
            if (!Waited(timeout)) return false;
            throw new InvalidOperationException($"제한 시간 안에 씬이 열리지 않았습니다: {sceneName}");
        }

        private static bool Waited(double seconds)
        {
            return EditorApplication.timeSinceStartup - stageStartedAt >= seconds;
        }

        private static void Advance()
        {
            stage++;
            stageStartedAt = EditorApplication.timeSinceStartup;
        }

        private static void FinishSuccessfully()
        {
            Debug.Log(
                "[KMS] WeaponSelect Play Mode 스모크 통과: 4개 캐릭터 버튼 상태, " +
                "4개 스테이지 버튼과 3개 자물쇠, Man_06/활 및 Man_07/단검 시작, " +
                "플레이어 사망 및 타이머 종료 결과와 각 WeaponSelectScene 복귀 강화 패널 표시를 확인했습니다.");
            RequestExit(0);
        }

        private static void RequestExit(int exitCode)
        {
            EditorApplication.update -= Tick;
            SessionState.SetInt(ExitCodeKey, exitCode);
            SessionState.SetBool(FinishingKey, true);
            EditorApplication.ExitPlaymode();
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }
    }
}
