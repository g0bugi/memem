using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace KMS.Editor
{
    public static class KmsInfiniteStageTestSceneConfigurator
    {
        private const string ScenePath = "Assets/KMS/TestScene_KMS.unity";
        private const string FloorSpritePath = "Assets/KMS/Monsters/Art/KmsTestPlayerVisual.asset";
        private const string WaveSchedulePath =
            "Assets/KMS/Monsters/Waves/KmsMonsterTestWaveSchedule.asset";
        private const string StageRootName = "KmsTestStage";

        [MenuItem("KMS/Stage/Apply Infinite Stage To Test Scene")]
        public static void ApplyWithConfirmation()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                Debug.Log("[KMS] 현재 씬 저장이 취소되어 무한 스테이지 적용을 중단했습니다.");
                return;
            }

            if (!EditorUtility.DisplayDialog(
                    "Apply KMS Infinite Stage",
                    "TestScene_KMS의 유한 바닥과 네 경계를 20×20 청크 3×3 무한 스테이지로 교체합니다.",
                    "Apply",
                    "Cancel"))
            {
                return;
            }

            ApplyInternal();
        }

        public static void ApplyFromCommandLine()
        {
            ApplyInternal();
        }

        internal static KmsInfiniteStageScroller RebuildStage(
            Scene scene,
            Sprite floorSprite,
            Transform playerTarget)
        {
            return RebuildStage(
                scene,
                floorSprite,
                playerTarget,
                StageRootName,
                new Color(0.07f, 0.13f, 0.16f, 1f));
        }

        internal static KmsInfiniteStageScroller RebuildStage(
            Scene scene,
            Sprite floorSprite,
            Transform playerTarget,
            string stageRootName,
            Color floorColor)
        {
            if (!scene.IsValid() || !scene.isLoaded)
            {
                throw new InvalidOperationException("무한 스테이지를 구성할 Scene이 로드되지 않았습니다.");
            }

            if (floorSprite == null)
            {
                throw new InvalidOperationException("무한 스테이지 바닥 Sprite가 없습니다.");
            }

            if (playerTarget == null)
            {
                throw new InvalidOperationException("무한 스테이지가 추적할 Player가 없습니다.");
            }

            if (string.IsNullOrWhiteSpace(stageRootName))
            {
                throw new InvalidOperationException("무한 스테이지 루트 이름이 비어 있습니다.");
            }

            DestroyRootIfPresent(scene, stageRootName);

            GameObject stageObject = new GameObject(stageRootName);
            SceneManager.MoveGameObjectToScene(stageObject, scene);

            GameObject templateObject = new GameObject("FloorTemplate");
            templateObject.transform.SetParent(stageObject.transform, false);
            templateObject.transform.localScale = new Vector3(
                KmsInfiniteStageScroller.DefaultChunkSize.x,
                KmsInfiniteStageScroller.DefaultChunkSize.y,
                1f);

            SpriteRenderer floorRenderer = templateObject.AddComponent<SpriteRenderer>();
            floorRenderer.sprite = floorSprite;
            floorRenderer.color = floorColor;
            floorRenderer.sortingOrder = -20;

            KmsInfiniteStageScroller scroller =
                stageObject.AddComponent<KmsInfiniteStageScroller>();
            scroller.Configure(
                playerTarget,
                floorRenderer,
                KmsInfiniteStageScroller.DefaultChunkSize,
                KmsInfiniteStageScroller.DefaultGridSize);
            return scroller;
        }

        private static void ApplyInternal()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            Sprite floorSprite = AssetDatabase.LoadAssetAtPath<Sprite>(FloorSpritePath);
            PlayerStats player = FindSceneComponents<PlayerStats>(scene).SingleOrDefault();
            KmsMonsterSpawner spawner =
                FindSceneComponents<KmsMonsterSpawner>(scene).SingleOrDefault();

            if (player == null)
            {
                throw new InvalidOperationException("TestScene_KMS에서 PlayerStats를 하나 찾지 못했습니다.");
            }

            if (spawner == null)
            {
                throw new InvalidOperationException("TestScene_KMS에서 KmsMonsterSpawner를 하나 찾지 못했습니다.");
            }

            RebuildStage(scene, floorSprite, player.transform);

            SerializedObject serializedSpawner = new SerializedObject(spawner);
            serializedSpawner.FindProperty("spawnArea").objectReferenceValue = null;
            serializedSpawner.FindProperty("innerSpawnRadius").floatValue =
                KmsMonsterSpawner.DefaultInnerSpawnRadius;
            serializedSpawner.FindProperty("outerSpawnRadius").floatValue =
                KmsMonsterSpawner.DefaultOuterSpawnRadius;
            serializedSpawner.FindProperty("positionAttemptCount").intValue = 64;
            serializedSpawner.ApplyModifiedPropertiesWithoutUndo();

            KmsWaveScheduleData schedule =
                AssetDatabase.LoadAssetAtPath<KmsWaveScheduleData>(WaveSchedulePath);
            if (schedule == null)
            {
                throw new InvalidOperationException("TestScene_KMS 웨이브 스케줄을 찾지 못했습니다.");
            }

            SerializedObject serializedSchedule = new SerializedObject(schedule);
            serializedSchedule.FindProperty("baseMonsterCount").intValue = 30;
            serializedSchedule.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(schedule);

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, ScenePath))
            {
                throw new InvalidOperationException("TestScene_KMS 무한 스테이지 저장에 실패했습니다.");
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log(
                "[KMS] TestScene_KMS에 20×20 청크 3×3 무한 스테이지, " +
                "12~24 스폰 반경, 기본 30마리 웨이브를 적용했습니다.");
        }

        private static T[] FindSceneComponents<T>(Scene scene) where T : Component
        {
            return scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<T>(true))
                .Where(component => component.gameObject.scene == scene)
                .ToArray();
        }

        private static void DestroyRootIfPresent(Scene scene, string objectName)
        {
            foreach (GameObject rootObject in scene.GetRootGameObjects())
            {
                if (rootObject.name != objectName)
                {
                    continue;
                }

                UnityEngine.Object.DestroyImmediate(rootObject);
                return;
            }
        }
    }
}
