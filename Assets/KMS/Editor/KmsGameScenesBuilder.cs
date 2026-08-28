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
        private const string MonsterPrefabPath = "Assets/KMS/Monsters/Prefabs/KmsMeleeMonster.prefab";
        private const string FieldSpritePath = "Assets/KMS/Monsters/Art/KmsTestPlayerVisual.asset";

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

        private static void ValidateDependencies()
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(HdyScenePath) == null)
            {
                throw new InvalidOperationException($"HDY 씬을 찾을 수 없습니다: {HdyScenePath}");
            }

            GameObject monsterPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(MonsterPrefabPath);
            if (monsterPrefab == null || monsterPrefab.GetComponent<KmsMonster>() == null)
            {
                throw new InvalidOperationException($"몬스터 프리팹을 찾을 수 없습니다: {MonsterPrefabPath}");
            }

            if (FindSpriteAtPath(FieldSpritePath) == null)
            {
                throw new InvalidOperationException($"게임 필드 표시용 Sprite를 찾을 수 없습니다: {FieldSpritePath}");
            }
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

            SaveScene(scene, WeaponSelectScenePath);
        }

        private static void BuildGameScene()
        {
            Scene scene = CreateEmptyScene();
            GameObject player = CloneHdyEnvironment(scene);
            CreateGameField();
            CreateSpawner(player.transform);

            KmsSceneNavigator navigator = CreateNavigator();
            Canvas canvas = CreateCanvas("GameCanvas");
            Text timerText = CreateText(canvas.transform, "RemainingTimeText", "TIME  00:20", 34,
                TextAnchor.MiddleCenter, new Vector2(0f, -42f), new Vector2(360f, 62f), Color.white);
            SetTopAnchored(timerText.rectTransform);

            Image endPanel = CreateImage(canvas.transform, "GameOverPanel", new Color(0.035f, 0.045f, 0.065f, 0.97f),
                Vector2.zero, new Vector2(680f, 420f));
            CreateText(endPanel.transform, "Title", "게임 종료", 52, TextAnchor.MiddleCenter,
                new Vector2(0f, 105f), new Vector2(600f, 90f), Color.white);
            CreateText(endPanel.transform, "Message", "이번 런이 종료되었습니다.\n무기 선택 / 스펙 업 화면으로 이동하세요.", 24,
                TextAnchor.MiddleCenter, new Vector2(0f, 25f), new Vector2(590f, 90f),
                new Color(0.78f, 0.83f, 0.92f, 1f));
            Button returnButton = CreateButton(endPanel.transform, "ReturnToWeaponSelectButton", "무기 선택으로",
                new Vector2(0f, -105f), new Vector2(420f, 76f), PrimaryColor);
            UnityEventTools.AddPersistentListener(returnButton.onClick, navigator.OpenWeaponSelectScene);

            GameObject timerObject = new GameObject("RunTimer");
            KmsRunTimer timer = timerObject.AddComponent<KmsRunTimer>();
            timer.Configure(20f, timerText, endPanel.gameObject);

            SaveScene(scene, GameScenePath);
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
                GameObject sourcePlayer = FindRequiredRoot(hdyScene, "Player");
                GameObject sourceCamera = FindRequiredRoot(hdyScene, "Main Camera");
                GameObject sourcePools = FindRequiredRoot(hdyScene, "PoolManagers");

                RequireComponent<PlayerStats>(sourcePlayer);
                RequireComponent<PlayerController2D>(sourcePlayer);
                RequireComponent<PlayerAttack>(sourcePlayer);
                WeaponInventory sourceInventory = RequireComponent<WeaponInventory>(sourcePlayer);
                RequireComponent<Camera>(sourceCamera);
                RequireComponent<CameraFollow2D>(sourceCamera);
                RequireComponent<ProjectilePoolManager>(sourcePools);
                RequireComponent<EffectPoolManager>(sourcePools);

                int targetLayers = sourceInventory.TargetLayers.value;
                if ((targetLayers & (1 << enemyLayer)) == 0)
                {
                    throw new InvalidOperationException("HDY PlayerAttack의 targetLayers에 Enemy 레이어가 없습니다.");
                }

                SceneManager.SetActiveScene(targetScene);
                GameObject player = CloneRootToScene(sourcePlayer, targetScene);
                GameObject cameraObject = CloneRootToScene(sourceCamera, targetScene);
                CloneRootToScene(sourcePools, targetScene);

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

        private static void CreateGameField()
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

        private static void CreateSpawner(Transform playerTarget)
        {
            GameObject monsterPrefabObject = AssetDatabase.LoadAssetAtPath<GameObject>(MonsterPrefabPath);
            KmsMonster monsterPrefab = monsterPrefabObject.GetComponent<KmsMonster>();
            GameObject spawnerObject = new GameObject("KmsMonsterSpawner");
            spawnerObject.transform.position = new Vector3(3f, 0f, 0f);
            KmsMonsterSpawner spawner = spawnerObject.AddComponent<KmsMonsterSpawner>();
            spawner.Configure(monsterPrefab, playerTarget, 1);
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

        private static GameObject CloneRootToScene(GameObject source, Scene targetScene)
        {
            GameObject clone = UnityEngine.Object.Instantiate(source);
            clone.name = source.name;
            SceneManager.MoveGameObjectToScene(clone, targetScene);
            return clone;
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
