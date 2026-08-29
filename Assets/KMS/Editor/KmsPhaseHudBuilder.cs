using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace KMS.Editor
{
    public static class KmsPhaseHudBuilder
    {
        private const string TestScenePath = "Assets/KMS/TestScene_KMS.unity";
        private const string GameScenePath = "Assets/Scenes/GameScene.unity";
        private static readonly Color NormalColor = new Color(1f, 0.78f, 0.22f, 1f);
        private static readonly Color WarningColor = new Color(0.95f, 0.28f, 0.12f, 1f);

        [MenuItem("KMS/Apply Phase HUD")]
        public static void Apply()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                Debug.Log("[KMS] 현재 씬 저장이 취소되어 웨이브 HUD 적용을 중단했습니다.");
                return;
            }

            ApplyToScenes();
        }

        public static void ApplyFromCommandLine()
        {
            ApplyToScenes();
        }

        public static KmsPhaseHud CreateOrReplace(
            Transform canvasTransform,
            KmsWaveDirector waveDirector)
        {
            if (canvasTransform == null)
            {
                throw new ArgumentNullException(nameof(canvasTransform));
            }

            if (waveDirector == null)
            {
                throw new ArgumentNullException(nameof(waveDirector));
            }

            foreach (string existingName in new[] { "PhaseHud", "WaveHud" })
            {
                Transform existing = canvasTransform.Find(existingName);
                if (existing != null)
                {
                    UnityEngine.Object.DestroyImmediate(existing.gameObject);
                }
            }

            GameObject root = new GameObject("WaveHud", typeof(RectTransform), typeof(KmsPhaseHud));
            root.transform.SetParent(canvasTransform, false);
            RectTransform rootRect = root.GetComponent<RectTransform>();
            SetTopCenterRect(rootRect, new Vector2(0f, -106f), new Vector2(300f, 52f));

            Text phaseText = CreateText(
                root.transform,
                "WaveText",
                "WAVE 1",
                22,
                new Vector2(0f, 0f),
                new Vector2(300f, 28f));

            Image background = CreateImage(
                root.transform,
                "WaveBarBackground",
                new Color(0f, 0f, 0f, 0.55f));
            SetTopCenterRect(background.rectTransform, new Vector2(0f, -32f), new Vector2(280f, 12f));

            Image fill = CreateImage(background.transform, "WaveBarFill", NormalColor);

            KmsPhaseHud hud = root.GetComponent<KmsPhaseHud>();
            hud.Configure(waveDirector, phaseText, fill, NormalColor, WarningColor, 0.2f);
            return hud;
        }

        public static void CreateOrReplaceTestHud(
            Scene scene,
            KmsWaveDirector waveDirector,
            KmsRunTimer runTimer)
        {
            Canvas canvas = FindSceneComponents<Canvas>(scene)
                .FirstOrDefault(candidate => candidate.gameObject.name == "KmsPhaseHudCanvas");
            if (canvas == null)
            {
                GameObject canvasObject = new GameObject(
                    "KmsPhaseHudCanvas",
                    typeof(RectTransform),
                    typeof(Canvas),
                    typeof(CanvasScaler),
                    typeof(GraphicRaycaster));
                SceneManager.MoveGameObjectToScene(canvasObject, scene);
                canvas = canvasObject.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;

                CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920f, 1080f);
                scaler.matchWidthOrHeight = 0.5f;
            }

            Transform oldTimerText = canvas.transform.Find("RemainingTimeText");
            if (oldTimerText != null)
            {
                UnityEngine.Object.DestroyImmediate(oldTimerText.gameObject);
            }

            Text timerText = CreateText(
                canvas.transform,
                "RemainingTimeText",
                "TIME  01:00",
                34,
                new Vector2(0f, -42f),
                new Vector2(360f, 62f));
            SerializedObject serializedTimer = new SerializedObject(runTimer);
            serializedTimer.FindProperty("remainingTimeText").objectReferenceValue = timerText;
            serializedTimer.ApplyModifiedPropertiesWithoutUndo();

            CreateOrReplace(canvas.transform, waveDirector);
        }

        private static void ApplyToScenes()
        {
            Scene testScene = EditorSceneManager.OpenScene(TestScenePath, OpenSceneMode.Single);
            CreateOrReplaceTestHud(
                testScene,
                FindUniqueSceneComponent<KmsWaveDirector>(testScene),
                FindUniqueSceneComponent<KmsRunTimer>(testScene));
            EditorSceneManager.MarkSceneDirty(testScene);
            EditorSceneManager.SaveScene(testScene, TestScenePath);

            Scene gameScene = EditorSceneManager.OpenScene(GameScenePath, OpenSceneMode.Single);
            Canvas gameCanvas = FindSceneComponents<Canvas>(gameScene)
                .Single(candidate => candidate.gameObject.name == "GameCanvas");
            CreateOrReplace(gameCanvas.transform, FindUniqueSceneComponent<KmsWaveDirector>(gameScene));
            EditorSceneManager.MarkSceneDirty(gameScene);
            EditorSceneManager.SaveScene(gameScene, GameScenePath);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[KMS] TestScene_KMS와 GameScene에 웨이브 HUD를 적용했습니다.");
        }

        private static T FindUniqueSceneComponent<T>(Scene scene) where T : Component
        {
            T[] components = FindSceneComponents<T>(scene);
            if (components.Length != 1)
            {
                throw new InvalidOperationException(
                    $"{scene.path}에 {typeof(T).Name}이 정확히 하나 필요하지만 {components.Length}개입니다.");
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

        private static Text CreateText(
            Transform parent,
            string name,
            string content,
            int fontSize,
            Vector2 position,
            Vector2 size)
        {
            GameObject textObject = new GameObject(
                name,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Text));
            textObject.transform.SetParent(parent, false);
            Text text = textObject.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.text = content;
            text.fontSize = fontSize;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.raycastTarget = false;
            SetTopCenterRect(text.rectTransform, position, size);
            return text;
        }

        private static Image CreateImage(Transform parent, string name, Color color)
        {
            GameObject imageObject = new GameObject(
                name,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));
            imageObject.transform.SetParent(parent, false);
            Image image = imageObject.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        private static void SetTopCenterRect(RectTransform rect, Vector2 position, Vector2 size)
        {
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }
    }
}
