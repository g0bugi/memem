using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace KMS.Editor
{
    public static class KmsGameScenesBuilder
    {
        private const string StartScenePath = "Assets/Scenes/StartScene.unity";
        private const string WeaponSelectScenePath = "Assets/Scenes/WeaponSelectScene.unity";
        private const string GameScenePath = "Assets/Scenes/GameScene.unity";
        private const string HdyScenePath = "Assets/Scenes/HDY.unity";
        private const string HdyPlayerPrefabPath = "Assets/HDY/Player.prefab";
        private const string HdyPoolManagersPrefabPath = "Assets/HDY/PoolManagers.prefab";
        private const string HdyPlayerHudPrefabPath = "Assets/HDY/UI/PlayerHUD.prefab";
        private const string HdyCursorPrefabPath = "Assets/HDY/CursorController.prefab";
        private const string MonsterPrefabPath = "Assets/KMS/Monsters/Prefabs/KmsMeleeMonster.prefab";
        private const string MonsterDataPath = "Assets/KMS/Monsters/Data/KmsMeleeNormalData.asset";
        private const string MonsterSpritePath = "Assets/KMS/Monsters/Art/KmsMonsterVisual.asset";
        private const string FieldSpritePath = "Assets/KMS/Monsters/Art/KmsTestPlayerVisual.asset";
        private const string GoldPickupPrefabPath = "Assets/KMS/Drops/Prefabs/KmsGoldPickup.prefab";
        private const string WeaponPickupPrefabPath = "Assets/KMS/Drops/Prefabs/KmsWeaponPickup.prefab";
        private const string WeaponDropTablePath = "Assets/KMS/Drops/Data/KmsWeaponDropTable.asset";

        private static readonly Color BackgroundColor = new Color(0.035f, 0.045f, 0.065f, 1f);
        private static readonly Color PanelColor = new Color(0.08f, 0.1f, 0.14f, 0.96f);
        private static readonly Color PrimaryColor = new Color(0.95f, 0.55f, 0.12f, 1f);
        private static readonly Color SecondaryColor = new Color(0.2f, 0.25f, 0.34f, 1f);

        [MenuItem("KMS/Build Game Scenes")]
        public static void Build()
        {
            ValidateDependencies();
            BuildStartScene();
            BuildWeaponSelectScene();
            BuildGameScene();
            RegisterBuildScenes();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorSceneManager.OpenScene(StartScenePath, OpenSceneMode.Single);
            Debug.Log("[KMS] StartScene → WeaponSelectScene → GameScene 흐름을 생성했습니다.");
        }

        public static void BuildFromCommandLine()
        {
            Build();
        }

        public static void BuildGameSceneFromCommandLine()
        {
            ValidateDependencies();
            BuildGameScene();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[KMS] GameScene을 현재 KMS 구성으로 다시 생성했습니다.");
        }

        [MenuItem("KMS/Apply HDY Prefabs To Game Scene")]
        public static void ApplyHdyPrefabsToGameScene()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                Debug.Log("[KMS] 현재 씬 저장이 취소되어 GameScene 프리팹 교체를 중단했습니다.");
                return;
            }

            ApplyHdyPrefabsToGameSceneInternal();
        }

        public static void ApplyHdyPrefabsToGameSceneFromCommandLine()
        {
            ApplyHdyPrefabsToGameSceneInternal();
        }

        private static void ApplyHdyPrefabsToGameSceneInternal()
        {
            ValidateDependencies();
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(GameScenePath) == null)
            {
                throw new InvalidOperationException($"메인 게임 씬을 찾을 수 없습니다: {GameScenePath}");
            }

            Scene scene = EditorSceneManager.OpenScene(GameScenePath, OpenSceneMode.Single);
            ReplaceHdyEnvironmentWithPrefabs(scene);
            SaveScene(scene, GameScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[KMS] GameScene의 HDY 오브젝트를 프리팹 인스턴스로 교체했습니다.");
        }

        [MenuItem("KMS/Apply Drops To Game Scene")]
        public static void ApplyDropsToGameScene()
        {
            ValidateDropDependencies();
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(GameScenePath) == null)
            {
                throw new InvalidOperationException($"메인 게임 씬을 찾을 수 없습니다: {GameScenePath}");
            }

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                Debug.Log("[KMS] 현재 씬 저장이 취소되어 GameScene 드롭 적용을 중단했습니다.");
                return;
            }

            Scene scene = EditorSceneManager.OpenScene(GameScenePath, OpenSceneMode.Single);
            ApplyDropSystems(scene);
            SaveScene(scene, GameScenePath);
            AssetDatabase.SaveAssets();
            Debug.Log("[KMS] GameScene에 골드·무기 드롭 시스템을 적용했습니다.");
        }

        private static void ValidateDependencies()
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(HdyScenePath) == null)
            {
                throw new InvalidOperationException($"HDY 씬을 찾을 수 없습니다: {HdyScenePath}");
            }

            ValidatePrefabComponent<PlayerStats>(HdyPlayerPrefabPath);
            ValidatePrefabComponent<ProjectilePoolManager>(HdyPoolManagersPrefabPath);
            ValidatePrefabComponent<Canvas>(HdyPlayerHudPrefabPath);
            ValidatePrefabComponent<CustomCursor>(HdyCursorPrefabPath);

            GameObject monsterPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(MonsterPrefabPath);
            if (monsterPrefab == null || monsterPrefab.GetComponent<KmsMonster>() == null)
            {
                throw new InvalidOperationException($"몬스터 프리팹을 찾을 수 없습니다: {MonsterPrefabPath}");
            }

            if (AssetDatabase.LoadAssetAtPath<KmsMonsterData>(MonsterDataPath) == null)
            {
                throw new InvalidOperationException(
                    $"기본 MonsterData를 찾을 수 없습니다. KMS 테스트 몬스터 빌더를 먼저 실행하세요: {MonsterDataPath}");
            }

            if (FindSpriteAtPath(MonsterSpritePath) == null)
            {
                throw new InvalidOperationException($"몬스터 기본 표시용 Sprite를 찾을 수 없습니다: {MonsterSpritePath}");
            }

            if (FindSpriteAtPath(FieldSpritePath) == null)
            {
                throw new InvalidOperationException($"게임 필드 표시용 Sprite를 찾을 수 없습니다: {FieldSpritePath}");
            }

            ValidateDropDependencies();
        }

        private static void ValidateDropDependencies()
        {
            GameObject goldPickupObject = AssetDatabase.LoadAssetAtPath<GameObject>(GoldPickupPrefabPath);
            if (goldPickupObject == null || goldPickupObject.GetComponent<KmsGoldPickup>() == null)
            {
                throw new InvalidOperationException($"골드 픽업 프리팹을 찾을 수 없습니다: {GoldPickupPrefabPath}");
            }

            GameObject weaponPickupObject = AssetDatabase.LoadAssetAtPath<GameObject>(WeaponPickupPrefabPath);
            if (weaponPickupObject == null || weaponPickupObject.GetComponent<KmsWeaponPickup>() == null)
            {
                throw new InvalidOperationException($"무기 픽업 프리팹을 찾을 수 없습니다: {WeaponPickupPrefabPath}");
            }

            if (AssetDatabase.LoadAssetAtPath<KmsWeaponDropTable>(WeaponDropTablePath) == null)
            {
                throw new InvalidOperationException($"무기 드롭 테이블을 찾을 수 없습니다: {WeaponDropTablePath}");
            }

            KmsDropRuntimePrefabBuilder.LoadAndValidatePrefab();
        }

        private static void BuildStartScene()
        {
            Scene scene = CreateEmptyScene();
            CreateUiCamera();
            KmsSceneNavigator navigator = CreateNavigator();
            Canvas canvas = CreateCanvas("StartCanvas");
            CreateFullScreenImage(canvas.transform, "Background", BackgroundColor);

            CreateText(canvas.transform, "Title", "초격차", 72, TextAnchor.MiddleCenter,
                new Vector2(0f, 120f), new Vector2(720f, 120f), Color.white);
            CreateText(canvas.transform, "Guide", "강력한 무기를 선택하고 압도적인 격차를 만드세요", 26,
                TextAnchor.MiddleCenter, new Vector2(0f, 45f), new Vector2(900f, 60f),
                new Color(0.75f, 0.8f, 0.9f, 1f));

            Button startButton = CreateButton(canvas.transform, "ClickToStartButton", "CLICK TO START",
                new Vector2(0f, -65f), new Vector2(420f, 76f), PrimaryColor);
            Button quitButton = CreateButton(canvas.transform, "QuitButton", "게임 종료",
                new Vector2(0f, -165f), new Vector2(420f, 64f), SecondaryColor);
            UnityEventTools.AddPersistentListener(startButton.onClick, navigator.OpenWeaponSelectScene);
            UnityEventTools.AddPersistentListener(quitButton.onClick, navigator.QuitGame);

            SaveScene(scene, StartScenePath);
        }

private static void BuildWeaponSelectScene()
        {
            Scene scene = CreateEmptyScene();
            CreateUiCamera();
            KmsSceneNavigator navigator = CreateNavigator();
            Canvas canvas = CreateCanvas("WeaponSelectCanvas");
            CreateFullScreenImage(canvas.transform, "Background", BackgroundColor);

            Image panel = CreateImage(canvas.transform, "SelectionPanel", PanelColor,
                Vector2.zero, new Vector2(760f, 520f));
            CreateText(panel.transform, "Title", "무기 선택 / 스펙 업", 48, TextAnchor.MiddleCenter,
                new Vector2(0f, 160f), new Vector2(680f, 80f), Color.white);
            CreateText(panel.transform, "TemporaryNotice",
                "현재는 임시 화면입니다.\n첫 시작에는 이곳에서 Dagger를 선택하고 게임을 시작합니다.",
                24, TextAnchor.MiddleCenter, new Vector2(0f, 55f), new Vector2(650f, 110f),
                new Color(0.78f, 0.83f, 0.92f, 1f));

            Button gameButton = CreateButton(panel.transform, "EnterGameButton", "임시: GAME SCENE으로",
                new Vector2(0f, -70f), new Vector2(480f, 76f), PrimaryColor);
            Button quitButton = CreateButton(panel.transform, "QuitButton", "게임 종료",
                new Vector2(0f, -165f), new Vector2(480f, 64f), SecondaryColor);
            UnityEventTools.AddPersistentListener(gameButton.onClick, navigator.OpenGameScene);
            UnityEventTools.AddPersistentListener(quitButton.onClick, navigator.QuitGame);

            BuildStatUpgradePanel(canvas.transform);

            SaveScene(scene, WeaponSelectScenePath);
        }

private static void BuildStatUpgradePanel(Transform canvasTransform)
        {
            Image upgradePanel = CreateImage(canvasTransform, "UpgradePanel", PanelColor,
                new Vector2(-40f, 0f), new Vector2(760f, 420f));
            RectTransform upgradeRect = upgradePanel.rectTransform;
            upgradeRect.anchorMin = new Vector2(1f, 0.5f);
            upgradeRect.anchorMax = new Vector2(1f, 0.5f);
            upgradeRect.pivot = new Vector2(1f, 0.5f);
            upgradeRect.anchoredPosition = new Vector2(-40f, 0f);

            CreateText(upgradePanel.transform, "Title", "스탯 강화", 34, TextAnchor.MiddleCenter,
                new Vector2(0f, 165f), new Vector2(680f, 60f), Color.white);
            Text goldText = CreateText(upgradePanel.transform, "GoldText", "보유 골드  0", 24, TextAnchor.MiddleCenter,
                new Vector2(0f, 115f), new Vector2(680f, 40f), new Color(0.95f, 0.8f, 0.3f, 1f));

            (Text levelText, Text costText, Button button) healthRow =
                BuildUpgradeRow(upgradePanel.transform, "Health", "체력", 45f);
            (Text levelText, Text costText, Button button) moveSpeedRow =
                BuildUpgradeRow(upgradePanel.transform, "MoveSpeed", "이동속도", -35f);
            (Text levelText, Text costText, Button button) attackRow =
                BuildUpgradeRow(upgradePanel.transform, "AttackPower", "공격력", -115f);

            Button closeButton = CreateButton(upgradePanel.transform, "CloseButton", "확인",
                new Vector2(0f, -180f), new Vector2(220f, 56f), SecondaryColor);

            KmsStatUpgradePanelUI upgradeUi = upgradePanel.gameObject.AddComponent<KmsStatUpgradePanelUI>();
            SerializedObject serializedUi = new SerializedObject(upgradeUi);
            serializedUi.FindProperty("panelRoot").objectReferenceValue = upgradePanel.gameObject;
            serializedUi.FindProperty("goldText").objectReferenceValue = goldText;
            serializedUi.FindProperty("healthLevelText").objectReferenceValue = healthRow.levelText;
            serializedUi.FindProperty("healthCostText").objectReferenceValue = healthRow.costText;
            serializedUi.FindProperty("healthUpgradeButton").objectReferenceValue = healthRow.button;
            serializedUi.FindProperty("moveSpeedLevelText").objectReferenceValue = moveSpeedRow.levelText;
            serializedUi.FindProperty("moveSpeedCostText").objectReferenceValue = moveSpeedRow.costText;
            serializedUi.FindProperty("moveSpeedUpgradeButton").objectReferenceValue = moveSpeedRow.button;
            serializedUi.FindProperty("attackLevelText").objectReferenceValue = attackRow.levelText;
            serializedUi.FindProperty("attackCostText").objectReferenceValue = attackRow.costText;
            serializedUi.FindProperty("attackUpgradeButton").objectReferenceValue = attackRow.button;
            serializedUi.FindProperty("closeButton").objectReferenceValue = closeButton;
            serializedUi.ApplyModifiedPropertiesWithoutUndo();
        }

private static (Text levelText, Text costText, Button button) BuildUpgradeRow(
            Transform parent, string idName, string label, float yPosition)
        {
            CreateText(parent, $"{idName}Label", label, 26, TextAnchor.MiddleLeft,
                new Vector2(-260f, yPosition), new Vector2(160f, 40f), Color.white);
            Text levelText = CreateText(parent, $"{idName}LevelText", "Lv. 0 / 7", 22, TextAnchor.MiddleLeft,
                new Vector2(-90f, yPosition), new Vector2(140f, 40f), new Color(0.78f, 0.83f, 0.92f, 1f));
            Text costText = CreateText(parent, $"{idName}CostText", "50 골드", 22, TextAnchor.MiddleCenter,
                new Vector2(90f, yPosition), new Vector2(140f, 40f), new Color(0.95f, 0.8f, 0.3f, 1f));
            Button button = CreateButton(parent, $"{idName}UpgradeButton", "강화",
                new Vector2(280f, yPosition), new Vector2(140f, 48f), PrimaryColor);
            return (levelText, costText, button);
        }



private static void BuildGameScene()
        {
            Scene scene = CreateEmptyScene();
            GameObject player = CloneHdyEnvironment(scene);
            WeaponInventory weaponInventory = RequireComponent<WeaponInventory>(player);
            ConfigureStartingWeapon(weaponInventory);
            Collider2D spawnArea = CreateGameField();

            KmsSceneNavigator navigator = CreateNavigator();
            Canvas canvas = CreateCanvas("GameCanvas");
            Text timerText = CreateText(canvas.transform, "RemainingTimeText", "TIME  03:00", 34,
                TextAnchor.MiddleCenter, new Vector2(0f, -42f), new Vector2(360f, 62f), Color.white);
            SetTopAnchored(timerText.rectTransform);

            Image resultPanel = CreateImage(canvas.transform, "GameOverPanel", new Color(0.035f, 0.045f, 0.065f, 0.97f),
                Vector2.zero, new Vector2(760f, 560f));
            Text titleText = CreateText(resultPanel.transform, "Title", "게임 종료", 52, TextAnchor.MiddleCenter,
                new Vector2(0f, 220f), new Vector2(600f, 90f), Color.white);
            Text statsText = CreateText(resultPanel.transform, "Message", "획득 골드  0\n처치한 몬스터  0마리", 24,
                TextAnchor.MiddleCenter, new Vector2(0f, 130f), new Vector2(650f, 90f),
                new Color(0.78f, 0.83f, 0.92f, 1f));

            Transform weaponListContainer = CreateWeaponListContainer(resultPanel.transform, new Vector2(0f, 20f));

            Button returnButton = CreateButton(resultPanel.transform, "ReturnToWeaponSelectButton", "무기 선택으로",
                new Vector2(0f, -220f), new Vector2(420f, 76f), PrimaryColor);
            UnityEventTools.AddPersistentListener(returnButton.onClick, navigator.OpenWeaponSelectScene);

            GameObject timerObject = new GameObject("RunTimer");
            KmsRunTimer timer = timerObject.AddComponent<KmsRunTimer>();
            timer.Configure(180f, timerText);

            int enemyLayer = LayerMask.NameToLayer("Enemy");
            Sprite monsterSprite = FindSpriteAtPath(MonsterSpritePath);
            Sprite fieldSprite = FindSpriteAtPath(FieldSpritePath);
            KmsMonsterWaveContentBuilder.Content monsterContent =
                KmsMonsterWaveContentBuilder.BuildOrUpdateContent(enemyLayer, monsterSprite, fieldSprite);
            KmsMonsterWaveContentBuilder.Runtime monsterRuntime =
                KmsMonsterWaveContentBuilder.CreateOrReplaceRuntime(
                    scene,
                    monsterContent,
                    player.transform,
                    spawnArea,
                    timer);
            KmsMonsterSpawner spawner = monsterRuntime.Spawner;
            KmsDropRuntimePrefabBuilder.InstantiateOrReplaceLegacy(scene);

            GameObject resultControllerObject = new GameObject("RunResultController");
            KmsRunResultController resultController = resultControllerObject.AddComponent<KmsRunResultController>();
            SerializedObject serializedController = new SerializedObject(resultController);
            serializedController.FindProperty("runTimer").objectReferenceValue = timer;
            serializedController.FindProperty("playerStats").objectReferenceValue = RequireComponent<PlayerStats>(player);
            serializedController.FindProperty("monsterSpawner").objectReferenceValue = spawner;
            serializedController.FindProperty("weaponInventory").objectReferenceValue = weaponInventory;
            serializedController.FindProperty("resultPanel").objectReferenceValue = resultPanel.gameObject;
            serializedController.FindProperty("titleText").objectReferenceValue = titleText;
            serializedController.FindProperty("statsText").objectReferenceValue = statsText;
            serializedController.FindProperty("weaponListContainer").objectReferenceValue = weaponListContainer;
            serializedController.ApplyModifiedPropertiesWithoutUndo();

            SaveScene(scene, GameScenePath);
        }

private static Transform CreateWeaponListContainer(Transform parent, Vector2 position)
        {
            GameObject container = new GameObject("WeaponListContainer", typeof(RectTransform),
                typeof(HorizontalLayoutGroup), typeof(ContentSizeFitter));
            container.transform.SetParent(parent, false);
            SetRect(container.GetComponent<RectTransform>(), position, new Vector2(680f, 80f));

            HorizontalLayoutGroup layout = container.GetComponent<HorizontalLayoutGroup>();
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.spacing = 8f;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            ContentSizeFitter fitter = container.GetComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;

            return container.transform;
        }


        private static Scene CreateEmptyScene()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            SceneManager.SetActiveScene(scene);
            return scene;
        }

        private static KmsSceneNavigator CreateNavigator()
        {
            GameObject navigationObject = new GameObject("SceneNavigation");
            return navigationObject.AddComponent<KmsSceneNavigator>();
        }

        private static void CreateUiCamera()
        {
            GameObject cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            cameraObject.tag = "MainCamera";
            cameraObject.transform.position = new Vector3(0f, 0f, -10f);

            Camera camera = cameraObject.GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = BackgroundColor;
            camera.orthographic = true;
        }

        private static GameObject CloneHdyEnvironment(Scene targetScene)
        {
            int enemyLayer = LayerMask.NameToLayer("Enemy");
            if (enemyLayer < 0)
            {
                throw new InvalidOperationException("Enemy 레이어가 ProjectSettings/TagManager.asset에 없습니다.");
            }

            Scene hdyScene = EditorSceneManager.OpenScene(HdyScenePath, OpenSceneMode.Additive);
            try
            {
                GameObject sourceCamera = FindRequiredRoot(hdyScene, "Main Camera");
                GameObject playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(HdyPlayerPrefabPath);
                GameObject poolManagersPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(HdyPoolManagersPrefabPath);
                GameObject playerHudPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(HdyPlayerHudPrefabPath);
                GameObject cursorPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(HdyCursorPrefabPath);

                RequireComponent<PlayerStats>(playerPrefab);
                RequireComponent<PlayerController2D>(playerPrefab);
                RequireComponent<PlayerAttack>(playerPrefab);
                WeaponInventory sourceInventory = RequireComponent<WeaponInventory>(playerPrefab);
                RequireComponent<Camera>(sourceCamera);
                RequireComponent<CameraFollow2D>(sourceCamera);
                RequireComponent<ProjectilePoolManager>(poolManagersPrefab);
                RequireComponent<EffectPoolManager>(poolManagersPrefab);
                RequireComponent<Canvas>(playerHudPrefab);
                RequireComponent<CustomCursor>(cursorPrefab);

                int targetLayers = sourceInventory.TargetLayers.value;
                if ((targetLayers & (1 << enemyLayer)) == 0)
                {
                    throw new InvalidOperationException("HDY PlayerAttack의 targetLayers에 Enemy 레이어가 없습니다.");
                }

                SceneManager.SetActiveScene(targetScene);
                GameObject player = InstantiatePrefabInScene(playerPrefab, targetScene);
                GameObject cameraObject = CloneRootToScene(sourceCamera, targetScene);
                InstantiatePrefabInScene(poolManagersPrefab, targetScene);
                InstantiatePrefabInScene(playerHudPrefab, targetScene);
                InstantiatePrefabInScene(cursorPrefab, targetScene);

                CameraFollow2D cameraFollow = RequireComponent<CameraFollow2D>(cameraObject);
                SerializedObject cameraData = new SerializedObject(cameraFollow);
                cameraData.FindProperty("target").objectReferenceValue = player.transform;
                cameraData.ApplyModifiedPropertiesWithoutUndo();

                player.transform.position = Vector3.zero;
                return player;
            }
            finally
            {
                EditorSceneManager.CloseScene(hdyScene, true);
                SceneManager.SetActiveScene(targetScene);
            }
        }

        private static void ReplaceHdyEnvironmentWithPrefabs(Scene scene)
        {
            GameObject oldPlayer = FindUniqueSceneRoot(scene, "Player", true);
            GameObject oldPoolManagers = FindUniqueSceneRoot(scene, "PoolManagers", true);
            GameObject oldPlayerHud = FindUniqueSceneRoot(scene, "PlayerHUD", false);
            GameObject oldCursor = FindUniqueSceneRoot(scene, "CursorController", false);

            CameraFollow2D cameraFollow = FindUniqueSceneComponent<CameraFollow2D>(scene);
            KmsMonsterSpawner spawner = FindUniqueSceneComponent<KmsMonsterSpawner>(scene);

            GameObject playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(HdyPlayerPrefabPath);
            GameObject poolManagersPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(HdyPoolManagersPrefabPath);
            GameObject playerHudPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(HdyPlayerHudPrefabPath);
            GameObject cursorPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(HdyCursorPrefabPath);

            WeaponInventory prefabInventory = RequireComponent<WeaponInventory>(playerPrefab);
            RequireComponent<ProjectilePoolManager>(poolManagersPrefab);
            RequireComponent<EffectPoolManager>(poolManagersPrefab);
            RequireComponent<Canvas>(playerHudPrefab);
            RequireComponent<CustomCursor>(cursorPrefab);

            int enemyLayer = LayerMask.NameToLayer("Enemy");
            if (enemyLayer < 0 || (prefabInventory.TargetLayers.value & (1 << enemyLayer)) == 0)
            {
                throw new InvalidOperationException("HDY Player 프리팹의 targetLayers에 Enemy 레이어가 없습니다.");
            }

            UnityEngine.Object.DestroyImmediate(oldPlayer);
            UnityEngine.Object.DestroyImmediate(oldPoolManagers);
            if (oldPlayerHud != null)
            {
                UnityEngine.Object.DestroyImmediate(oldPlayerHud);
            }

            if (oldCursor != null)
            {
                UnityEngine.Object.DestroyImmediate(oldCursor);
            }

            SceneManager.SetActiveScene(scene);
            GameObject player = InstantiatePrefabInScene(playerPrefab, scene);
            InstantiatePrefabInScene(poolManagersPrefab, scene);
            InstantiatePrefabInScene(playerHudPrefab, scene);
            InstantiatePrefabInScene(cursorPrefab, scene);
            player.transform.position = Vector3.zero;

            WeaponInventory weaponInventory = RequireComponent<WeaponInventory>(player);
            ConfigureStartingWeapon(weaponInventory);
            PrefabUtility.RecordPrefabInstancePropertyModifications(weaponInventory);

            SerializedObject cameraData = new SerializedObject(cameraFollow);
            cameraData.FindProperty("target").objectReferenceValue = player.transform;
            cameraData.ApplyModifiedPropertiesWithoutUndo();

            SerializedObject spawnerData = new SerializedObject(spawner);
            spawnerData.FindProperty("playerTarget").objectReferenceValue = player.transform;
            spawnerData.ApplyModifiedPropertiesWithoutUndo();
        }

        private static Collider2D CreateGameField()
        {
            GameObject field = new GameObject("GameField");
            SpriteRenderer fieldRenderer = field.AddComponent<SpriteRenderer>();
            fieldRenderer.sprite = FindSpriteAtPath(FieldSpritePath);
            fieldRenderer.color = new Color(0.12f, 0.18f, 0.2f, 1f);
            fieldRenderer.sortingOrder = -20;
            field.transform.localScale = new Vector3(18f, 11f, 1f);

            CreateBoundary(field.transform, "TopBoundary", new Vector2(0f, 5.5f), new Vector2(18f, 0.5f));
            CreateBoundary(field.transform, "BottomBoundary", new Vector2(0f, -5.5f), new Vector2(18f, 0.5f));
            CreateBoundary(field.transform, "LeftBoundary", new Vector2(-9f, 0f), new Vector2(0.5f, 11f));
            CreateBoundary(field.transform, "RightBoundary", new Vector2(9f, 0f), new Vector2(0.5f, 11f));

            GameObject spawnAreaObject = new GameObject("SpawnArea");
            spawnAreaObject.transform.SetParent(field.transform, false);
            spawnAreaObject.transform.localScale = new Vector3(
                1f / field.transform.localScale.x,
                1f / field.transform.localScale.y,
                1f);
            BoxCollider2D spawnArea = spawnAreaObject.AddComponent<BoxCollider2D>();
            spawnArea.size = new Vector2(17f, 10f);
            spawnArea.isTrigger = true;
            return spawnArea;
        }

        private static void CreateBoundary(Transform parent, string name, Vector2 position, Vector2 size)
        {
            GameObject boundary = new GameObject(name);
            boundary.transform.SetParent(parent, false);
            boundary.transform.localPosition = new Vector3(position.x / parent.localScale.x, position.y / parent.localScale.y, 0f);
            boundary.transform.localScale = new Vector3(1f / parent.localScale.x, 1f / parent.localScale.y, 1f);
            BoxCollider2D collider = boundary.AddComponent<BoxCollider2D>();
            collider.size = size;
        }

        private static KmsMonsterSpawner CreateSpawner(Transform playerTarget)
        {
            KmsMonsterData monsterData = AssetDatabase.LoadAssetAtPath<KmsMonsterData>(MonsterDataPath);
            if (monsterData == null)
            {
                throw new InvalidOperationException(
                    $"기본 MonsterData를 찾을 수 없습니다. KMS 테스트 몬스터 빌더를 먼저 실행하세요: {MonsterDataPath}");
            }

            GameObject spawnerObject = new GameObject("KmsMonsterSpawner");
            spawnerObject.transform.position = new Vector3(3f, 0f, 0f);
            KmsMonsterSpawner spawner = spawnerObject.AddComponent<KmsMonsterSpawner>();
            spawner.Configure(new[] { monsterData }, playerTarget, null, null, true);
            return spawner;
        }

        private static void ApplyDropSystems(Scene scene)
        {
            WeaponInventory weaponInventory = FindUniqueSceneComponent<WeaponInventory>(scene);
            FindUniqueSceneComponent<KmsMonsterSpawner>(scene);

            ConfigureStartingWeapon(weaponInventory);
            KmsDropRuntimePrefabBuilder.InstantiateOrReplaceLegacy(scene);
        }

        private static void ConfigureStartingWeapon(WeaponInventory weaponInventory)
        {
            SerializedObject serializedInventory = new SerializedObject(weaponInventory);
            SerializedProperty weaponIds = serializedInventory.FindProperty("weaponIds");
            if (weaponIds == null)
            {
                throw new InvalidOperationException("WeaponInventory에서 시작 무기 ID 목록을 찾을 수 없습니다.");
            }

            weaponIds.arraySize = 1;
            weaponIds.GetArrayElementAtIndex(0).stringValue = "dagger";
            serializedInventory.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(weaponInventory);
        }

        private static T FindUniqueSceneComponent<T>(Scene scene) where T : Component
        {
            T[] components = FindSceneComponents<T>(scene);
            if (components.Length != 1)
            {
                throw new InvalidOperationException(
                    $"{GameScenePath}에 {typeof(T).Name}이 정확히 1개 필요하지만 {components.Length}개입니다.");
            }

            return components[0];
        }

        private static T[] FindSceneComponents<T>(Scene scene) where T : Component
        {
            return scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<T>(true))
                .Where(component => component.gameObject.scene == scene)
                .ToArray();
        }

        private static Canvas CreateCanvas(string name)
        {
            GameObject canvasObject = new GameObject(name, typeof(RectTransform), typeof(Canvas),
                typeof(CanvasScaler), typeof(GraphicRaycaster));
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            GameObject eventSystem = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            eventSystem.transform.SetAsLastSibling();
            return canvas;
        }

        private static Image CreateFullScreenImage(Transform parent, string name, Color color)
        {
            Image image = CreateImage(parent, name, color, Vector2.zero, Vector2.zero);
            RectTransform rect = image.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.SetAsFirstSibling();
            return image;
        }

        private static Image CreateImage(Transform parent, string name, Color color, Vector2 position, Vector2 size)
        {
            GameObject imageObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            imageObject.transform.SetParent(parent, false);
            Image image = imageObject.GetComponent<Image>();
            image.color = color;
            SetRect(image.rectTransform, position, size);
            return image;
        }

        private static Text CreateText(Transform parent, string name, string content, int fontSize,
            TextAnchor alignment, Vector2 position, Vector2 size, Color color)
        {
            GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            textObject.transform.SetParent(parent, false);
            Text text = textObject.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.text = content;
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = color;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            SetRect(text.rectTransform, position, size);
            return text;
        }

        private static Button CreateButton(Transform parent, string name, string label, Vector2 position,
            Vector2 size, Color color)
        {
            GameObject buttonObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer),
                typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(parent, false);
            Image image = buttonObject.GetComponent<Image>();
            image.color = color;
            SetRect(buttonObject.GetComponent<RectTransform>(), position, size);

            Button button = buttonObject.GetComponent<Button>();
            ColorBlock colors = button.colors;
            colors.highlightedColor = Color.Lerp(color, Color.white, 0.16f);
            colors.pressedColor = Color.Lerp(color, Color.black, 0.18f);
            button.colors = colors;

            Text text = CreateText(buttonObject.transform, "Label", label, 25, TextAnchor.MiddleCenter,
                Vector2.zero, size, Color.white);
            text.raycastTarget = false;
            return button;
        }

        private static void SetRect(RectTransform rect, Vector2 position, Vector2 size)
        {
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private static void SetTopAnchored(RectTransform rect)
        {
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
        }

        private static void SaveScene(Scene scene, string path)
        {
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, path))
            {
                throw new InvalidOperationException($"씬 저장에 실패했습니다: {path}");
            }
        }

        private static GameObject FindRequiredRoot(Scene scene, string objectName)
        {
            GameObject root = scene.GetRootGameObjects().FirstOrDefault(candidate => candidate.name == objectName);
            if (root == null)
            {
                throw new InvalidOperationException($"{HdyScenePath}에서 필수 루트 '{objectName}'을 찾을 수 없습니다.");
            }

            return root;
        }

        private static GameObject FindUniqueSceneRoot(Scene scene, string objectName, bool required)
        {
            GameObject[] matches = scene.GetRootGameObjects()
                .Where(candidate => candidate.name == objectName)
                .ToArray();
            int minimumCount = required ? 1 : 0;
            if (matches.Length < minimumCount || matches.Length > 1)
            {
                throw new InvalidOperationException(
                    $"{GameScenePath}에 이름이 '{objectName}'인 루트 오브젝트가 " +
                    $"{(required ? "정확히 1개" : "최대 1개")} 필요하지만 {matches.Length}개입니다.");
            }

            return matches.Length == 1 ? matches[0] : null;
        }

        private static GameObject CloneRootToScene(GameObject source, Scene targetScene)
        {
            GameObject clone = UnityEngine.Object.Instantiate(source);
            clone.name = source.name;
            SceneManager.MoveGameObjectToScene(clone, targetScene);
            return clone;
        }

        private static GameObject InstantiatePrefabInScene(GameObject prefab, Scene targetScene)
        {
            GameObject instance = PrefabUtility.InstantiatePrefab(prefab, targetScene) as GameObject;
            if (instance == null)
            {
                throw new InvalidOperationException($"프리팹 인스턴스 생성에 실패했습니다: {AssetDatabase.GetAssetPath(prefab)}");
            }

            return instance;
        }

        private static void ValidatePrefabComponent<T>(string path) where T : Component
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null || prefab.GetComponent<T>() == null)
            {
                throw new InvalidOperationException($"{typeof(T).Name} 컴포넌트가 있는 프리팹을 찾을 수 없습니다: {path}");
            }
        }

        private static T RequireComponent<T>(GameObject gameObject) where T : Component
        {
            T component = gameObject.GetComponent<T>();
            if (component == null)
            {
                throw new InvalidOperationException($"{gameObject.name}에 {typeof(T).Name} 컴포넌트가 없습니다.");
            }

            return component;
        }

        private static Sprite FindSpriteAtPath(string path)
        {
            return AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().FirstOrDefault();
        }

        private static void RegisterBuildScenes()
        {
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(StartScenePath, true),
                new EditorBuildSettingsScene(WeaponSelectScenePath, true),
                new EditorBuildSettingsScene(GameScenePath, true)
            };
        }
    }
}
