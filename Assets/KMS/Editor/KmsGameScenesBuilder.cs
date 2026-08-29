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
        private const string CharacterSelectionConfigPath =
            "Assets/KMS/Resources/KmsCharacterSelectionConfig.asset";
        private const string Man03PrefabPath =
            "Assets/Layer Lab/2D Characters-MinimalCharacters/Prefabs/Man_03.prefab";
        private const string Man04PrefabPath =
            "Assets/Layer Lab/2D Characters-MinimalCharacters/Prefabs/Man_04.prefab";
        private const string Man06PrefabPath =
            "Assets/Layer Lab/2D Characters-MinimalCharacters/Prefabs/Man_06.prefab";
        private const string Man07PrefabPath =
            "Assets/Layer Lab/2D Characters-MinimalCharacters/Prefabs/Man_07.prefab";
        private const string DaggerDataPath = "Assets/HDY/Data/common/Dagger.asset";
        private const string BowDataPath = "Assets/HDY/Data/common/Bow.asset";
        private const string LongswordDataPath = "Assets/HDY/Data/common/Longsword.asset";
        private const string CrossbowDataPath = "Assets/HDY/Data/common/Crossbow.asset";
        private const string ModernLockIconPath =
            "Assets/Modern UI Pack/Textures/Icon/System/Lock Filled.png";
        private const string TrialStageSpritePath = "Assets/KMS/Resources/111.png";
        private const string ProofStageSpritePath = "Assets/KMS/Resources/222.png";
        private const string ChangeStageSpritePath = "Assets/KMS/Resources/333.png";
        private const string RecognitionStageSpritePath = "Assets/KMS/Resources/444.png";

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

        public static void BuildWeaponSelectSceneFromCommandLine()
        {
            BuildWeaponSelectScene();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[KMS] 캐릭터·스테이지 선택 화면을 생성했습니다.");
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
            ValidateWeaponSelectDependencies();
            CreateOrUpdateCharacterSelectionConfig();

            Scene scene = CreateEmptyScene();
            CreateUiCamera();
            KmsSceneNavigator navigator = CreateNavigator();
            Canvas canvas = CreateCanvas("WeaponSelectCanvas");
            CreateFullScreenImage(canvas.transform, "Background", BackgroundColor);

            GameObject flowObject = new GameObject("WeaponSelectFlow");
            KmsWeaponSelectFlowUI flow = flowObject.AddComponent<KmsWeaponSelectFlowUI>();

            Image characterPanel = BuildCharacterSelectionPanel(canvas.transform, flow, navigator);
            (Image panel, Text selectedCharacterText) stagePanel =
                BuildStageSelectionPanel(canvas.transform, flow);

            SerializedObject serializedFlow = new SerializedObject(flow);
            serializedFlow.FindProperty("characterSelectionPanel").objectReferenceValue = characterPanel.gameObject;
            serializedFlow.FindProperty("stageSelectionPanel").objectReferenceValue = stagePanel.panel.gameObject;
            serializedFlow.FindProperty("selectedCharacterText").objectReferenceValue = stagePanel.selectedCharacterText;
            serializedFlow.FindProperty("sceneNavigator").objectReferenceValue = navigator;
            serializedFlow.ApplyModifiedPropertiesWithoutUndo();

            stagePanel.panel.gameObject.SetActive(false);

            BuildStatUpgradePanel(canvas.transform);

            SaveScene(scene, WeaponSelectScenePath);
        }

        private static Image BuildCharacterSelectionPanel(
            Transform canvasTransform, KmsWeaponSelectFlowUI flow, KmsSceneNavigator navigator)
        {
            Image panel = CreateImage(canvasTransform, "CharacterSelectionPanel", PanelColor,
                Vector2.zero, new Vector2(1600f, 820f));
            CreateText(panel.transform, "Title", "캐릭터 선택", 52, TextAnchor.MiddleCenter,
                new Vector2(0f, 330f), new Vector2(720f, 80f), Color.white);
            CreateText(panel.transform, "Guide", "플레이할 캐릭터를 선택하세요", 24, TextAnchor.MiddleCenter,
                new Vector2(0f, 275f), new Vector2(720f, 48f),
                new Color(0.72f, 0.78f, 0.88f, 1f));

            CreateCharacterCard(panel.transform, "DaggerCharacterButton", Man07PrefabPath, DaggerDataPath,
                "단검", new Vector2(-510f, 10f), true,
                flow.SelectDaggerCharacter);
            CreateCharacterCard(panel.transform, "BowCharacterButton", Man06PrefabPath, BowDataPath,
                "활", new Vector2(-170f, 10f), true,
                flow.SelectBowCharacter);
            CreateCharacterCard(panel.transform, "LongswordCharacterButton", Man03PrefabPath, LongswordDataPath,
                "장검", new Vector2(170f, 10f), false, null);
            CreateCharacterCard(panel.transform, "CrossbowCharacterButton", Man04PrefabPath, CrossbowDataPath,
                "석궁", new Vector2(510f, 10f), false, null);

            Button quitButton = CreateButton(panel.transform, "QuitButton", "게임 종료",
                new Vector2(0f, -340f), new Vector2(300f, 58f), SecondaryColor);
            UnityEventTools.AddPersistentListener(quitButton.onClick, navigator.QuitGame);
            return panel;
        }

        private static (Image panel, Text selectedCharacterText) BuildStageSelectionPanel(
            Transform canvasTransform, KmsWeaponSelectFlowUI flow)
        {
            Image panel = CreateImage(canvasTransform, "StageSelectionPanel", PanelColor,
                Vector2.zero, new Vector2(1600f, 820f));
            CreateText(panel.transform, "Title", "스테이지 선택", 52, TextAnchor.MiddleCenter,
                new Vector2(0f, 330f), new Vector2(720f, 80f), Color.white);
            Text selectedText = CreateText(panel.transform, "SelectedCharacterText", "선택 무기  ·  단검", 23,
                TextAnchor.MiddleCenter, new Vector2(0f, 276f), new Vector2(720f, 45f),
                new Color(0.95f, 0.72f, 0.28f, 1f));

            CreateStageCard(panel.transform, "Stage01Button", "시련", TrialStageSpritePath,
                new Vector2(-510f, 20f), true, flow.EnterStageOne);
            CreateStageCard(panel.transform, "Stage02Button", "증명", ProofStageSpritePath,
                new Vector2(-170f, 20f), false, null);
            CreateStageCard(panel.transform, "Stage03Button", "변화", ChangeStageSpritePath,
                new Vector2(170f, 20f), false, null);
            CreateStageCard(panel.transform, "Stage04Button", "인정", RecognitionStageSpritePath,
                new Vector2(510f, 20f), false, null);

            Button backButton = CreateButton(panel.transform, "BackButton", "캐릭터 다시 선택",
                new Vector2(0f, -330f), new Vector2(340f, 58f), SecondaryColor);
            UnityEventTools.AddPersistentListener(backButton.onClick, flow.ShowCharacterSelection);
            return (panel, selectedText);
        }

        private static void CreateCharacterCard(
            Transform parent,
            string buttonName,
            string characterPrefabPath,
            string weaponDataPath,
            string weaponName,
            Vector2 position,
            bool interactable,
            UnityEngine.Events.UnityAction onClick)
        {
            Color cardColor = interactable
                ? new Color(0.15f, 0.19f, 0.27f, 1f)
                : new Color(0.07f, 0.08f, 0.11f, 1f);
            Vector2 cardSize = new Vector2(280f, 360f);
            Button button = CreateButton(parent, buttonName, string.Empty, position, cardSize, cardColor);
            button.interactable = interactable;

            WeaponData weaponData = AssetDatabase.LoadAssetAtPath<WeaponData>(weaponDataPath);
            GameObject characterPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(characterPrefabPath);
            Sprite weaponSprite = weaponData.ResolvedIcon;
            if (weaponSprite == null)
            {
                SpriteRenderer weaponRenderer = characterPrefab.GetComponentsInChildren<SpriteRenderer>(true)
                    .FirstOrDefault(renderer => renderer.gameObject.name == "Weapon");
                weaponSprite = weaponRenderer != null ? weaponRenderer.sprite : null;
            }

            Image weaponIcon = CreateImage(button.transform, "WeaponIcon", Color.white,
                new Vector2(95f, 145f), new Vector2(46f, 46f));
            weaponIcon.sprite = weaponSprite;
            weaponIcon.preserveAspect = true;
            weaponIcon.raycastTarget = false;
            weaponIcon.color = new Color(1f, 1f, 1f, interactable ? 0.42f : 0.2f);

            BuildCharacterPreview(button.transform, characterPrefab, new Vector2(0f, -15f), 118f);
            weaponIcon.transform.SetAsLastSibling();

            CreateText(button.transform, "WeaponName", weaponName, 25, TextAnchor.MiddleCenter,
                new Vector2(0f, -142f), new Vector2(240f, 38f),
                interactable
                    ? new Color(0.95f, 0.7f, 0.26f, 1f)
                    : new Color(0.35f, 0.36f, 0.4f, 1f));

            if (interactable)
            {
                UnityEventTools.AddPersistentListener(button.onClick, onClick);
                return;
            }

            Image shade = CreateImage(button.transform, "LockedShade", new Color(0f, 0f, 0f, 0.48f),
                Vector2.zero, cardSize);
            shade.raycastTarget = false;
            CreateText(shade.transform, "LockedLabel", "준비 중", 25, TextAnchor.MiddleCenter,
                new Vector2(0f, 0f), new Vector2(200f, 52f), new Color(0.7f, 0.72f, 0.78f, 0.9f));
        }

        private static void BuildCharacterPreview(
            Transform parent, GameObject characterPrefab, Vector2 center, float pixelsPerUnit)
        {
            SpriteRenderer[] renderers = characterPrefab.GetComponentsInChildren<SpriteRenderer>(true)
                .OrderBy(renderer => renderer.sortingOrder)
                .ToArray();

            foreach (SpriteRenderer renderer in renderers)
            {
                Sprite sprite = renderer.sprite;
                if (sprite == null) continue;

                Vector3 relativePosition = characterPrefab.transform.InverseTransformPoint(renderer.transform.position);
                Vector3 relativeScale = renderer.transform.lossyScale;
                Vector2 spriteSize = sprite.bounds.size * pixelsPerUnit;

                Image part = CreateImage(parent, $"Preview_{renderer.gameObject.name}", renderer.color,
                    center + new Vector2(relativePosition.x, relativePosition.y) * pixelsPerUnit,
                    new Vector2(spriteSize.x * Mathf.Abs(relativeScale.x),
                        spriteSize.y * Mathf.Abs(relativeScale.y)));
                part.sprite = sprite;
                part.preserveAspect = true;
                part.raycastTarget = false;
                part.rectTransform.localEulerAngles = new Vector3(0f, 0f, renderer.transform.eulerAngles.z);
                part.rectTransform.localScale = new Vector3(
                    (renderer.flipX ? -1f : 1f) * Mathf.Sign(relativeScale.x),
                    (renderer.flipY ? -1f : 1f) * Mathf.Sign(relativeScale.y),
                    1f);
            }
        }

        private static void CreateStageCard(
            Transform parent,
            string buttonName,
            string stageName,
            string stageSpritePath,
            Vector2 position,
            bool interactable,
            UnityEngine.Events.UnityAction onClick)
        {
            Color cardColor = interactable
                ? new Color(0.16f, 0.21f, 0.3f, 1f)
                : new Color(0.065f, 0.075f, 0.1f, 1f);
            Vector2 cardSize = new Vector2(280f, 280f);
            Button button = CreateButton(parent, buttonName, string.Empty, position, cardSize, cardColor);
            button.interactable = interactable;

            Image stageImage = CreateImage(button.transform, "StageImage", Color.white,
                Vector2.zero, new Vector2(252f, 252f));
            stageImage.sprite = FindSpriteAtPath(stageSpritePath);
            stageImage.preserveAspect = true;
            stageImage.raycastTarget = false;

            Image labelBackground = CreateImage(button.transform, "StageNameBackground",
                new Color(0.02f, 0.025f, 0.035f, 0.78f),
                new Vector2(0f, -102f), new Vector2(252f, 48f));
            labelBackground.raycastTarget = false;
            CreateText(labelBackground.transform, "StageName", stageName, 27, TextAnchor.MiddleCenter,
                Vector2.zero, new Vector2(230f, 44f),
                interactable ? Color.white : new Color(0.58f, 0.6f, 0.66f, 1f));

            if (interactable)
            {
                UnityEventTools.AddPersistentListener(button.onClick, onClick);
                return;
            }

            Image shade = CreateImage(button.transform, "LockedShade", new Color(0f, 0f, 0f, 0.46f),
                Vector2.zero, cardSize);
            shade.raycastTarget = false;
            Image lockIcon = CreateImage(shade.transform, "LockIcon", Color.white,
                new Vector2(0f, 20f), new Vector2(72f, 72f));
            lockIcon.sprite = FindSpriteAtPath(ModernLockIconPath);
            lockIcon.preserveAspect = true;
            lockIcon.raycastTarget = false;
            lockIcon.color = new Color(1f, 1f, 1f, 0.72f);
        }

        private static void ValidateWeaponSelectDependencies()
        {
            string[] characterPaths = { Man03PrefabPath, Man04PrefabPath, Man06PrefabPath, Man07PrefabPath };
            foreach (string path in characterPaths)
            {
                GameObject character = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (character == null || character.GetComponentInChildren<SpriteRenderer>(true) == null)
                {
                    throw new InvalidOperationException($"캐릭터 프리팹을 찾을 수 없습니다: {path}");
                }
            }

            string[] weaponPaths = { DaggerDataPath, BowDataPath, LongswordDataPath, CrossbowDataPath };
            foreach (string path in weaponPaths)
            {
                WeaponData weaponData = AssetDatabase.LoadAssetAtPath<WeaponData>(path);
                if (weaponData == null)
                {
                    throw new InvalidOperationException($"무기 데이터를 찾을 수 없습니다: {path}");
                }
            }

            if (FindSpriteAtPath(ModernLockIconPath) == null)
            {
                throw new InvalidOperationException($"Modern UI Pack 자물쇠 아이콘을 찾을 수 없습니다: {ModernLockIconPath}");
            }

            string[] stageSpritePaths =
            {
                TrialStageSpritePath,
                ProofStageSpritePath,
                ChangeStageSpritePath,
                RecognitionStageSpritePath
            };
            foreach (string path in stageSpritePaths)
            {
                if (FindSpriteAtPath(path) == null)
                {
                    throw new InvalidOperationException($"스테이지 선택 Sprite를 찾을 수 없습니다: {path}");
                }
            }
        }

        private static void CreateOrUpdateCharacterSelectionConfig()
        {
            const string resourcesFolder = "Assets/KMS/Resources";
            if (!AssetDatabase.IsValidFolder(resourcesFolder))
            {
                AssetDatabase.CreateFolder("Assets/KMS", "Resources");
            }

            KmsCharacterSelectionConfig config =
                AssetDatabase.LoadAssetAtPath<KmsCharacterSelectionConfig>(CharacterSelectionConfigPath);
            if (config == null)
            {
                config = ScriptableObject.CreateInstance<KmsCharacterSelectionConfig>();
                AssetDatabase.CreateAsset(config, CharacterSelectionConfigPath);
            }

            SerializedObject serializedConfig = new SerializedObject(config);
            serializedConfig.FindProperty("daggerCharacterPrefab").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<GameObject>(Man07PrefabPath);
            serializedConfig.FindProperty("bowCharacterPrefab").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<GameObject>(Man06PrefabPath);
            serializedConfig.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(config);
        }

        private static void BuildStatUpgradePanel(Transform canvasTransform)
        {
            Image modalRoot = CreateImage(canvasTransform, "UpgradeModalRoot",
                new Color(0f, 0f, 0f, 0.68f), Vector2.zero, Vector2.zero);
            RectTransform modalRect = modalRoot.rectTransform;
            modalRect.anchorMin = Vector2.zero;
            modalRect.anchorMax = Vector2.one;
            modalRect.offsetMin = Vector2.zero;
            modalRect.offsetMax = Vector2.zero;

            Image upgradePanel = CreateImage(modalRoot.transform, "UpgradePanel", PanelColor,
                Vector2.zero, new Vector2(760f, 460f));

            CreateText(upgradePanel.transform, "Title", "스탯 강화", 34, TextAnchor.MiddleCenter,
                new Vector2(0f, 180f), new Vector2(680f, 60f), Color.white);
            Text goldText = CreateText(upgradePanel.transform, "GoldText", "보유 골드  0", 24, TextAnchor.MiddleCenter,
                new Vector2(0f, 130f), new Vector2(680f, 40f), new Color(0.95f, 0.8f, 0.3f, 1f));

            (Text levelText, Text costText, Button button) healthRow =
                BuildUpgradeRow(upgradePanel.transform, "Health", "체력", 55f);
            (Text levelText, Text costText, Button button) moveSpeedRow =
                BuildUpgradeRow(upgradePanel.transform, "MoveSpeed", "이동속도", -25f);
            (Text levelText, Text costText, Button button) attackRow =
                BuildUpgradeRow(upgradePanel.transform, "AttackPower", "공격력", -105f);

            Button closeButton = CreateButton(upgradePanel.transform, "CloseButton", "확인",
                new Vector2(0f, -190f), new Vector2(220f, 56f), SecondaryColor);

            KmsStatUpgradePanelUI upgradeUi = modalRoot.gameObject.AddComponent<KmsStatUpgradePanelUI>();
            SerializedObject serializedUi = new SerializedObject(upgradeUi);
            serializedUi.FindProperty("panelRoot").objectReferenceValue = modalRoot.gameObject;
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
            Collider2D spawnArea = CreateGameField(scene, player.transform);

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
            timer.Configure(600f, timerText);

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

        private static Collider2D CreateGameField(Scene scene, Transform playerTarget)
        {
            KmsInfiniteStageTestSceneConfigurator.RebuildStage(
                scene,
                FindSpriteAtPath(FieldSpritePath),
                playerTarget,
                "GameField",
                KmsInfiniteStageGameSceneConfigurator.LightGreenFloorColor);
            return null;
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
