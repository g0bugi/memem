using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace KMS.Editor
{
    public static class KmsInfiniteStageGameSceneConfigurator
    {
        private const string ScenePath = "Assets/Scenes/GameScene.unity";
        private const string FloorSpritePath =
            "Assets/KMS/Monsters/Art/KmsTestPlayerVisual.asset";
        private const string StageRootName = "GameField";

        internal static readonly Color LightGreenFloorColor =
            new Color(0.62f, 0.82f, 0.38f, 1f);

        [MenuItem("KMS/Stage/Apply Infinite Stage To Game Scene")]
        public static void ApplyWithConfirmation()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                Debug.Log("[KMS] 현재 씬 저장이 취소되어 GameScene 무한 스테이지 적용을 중단했습니다.");
                return;
            }

            if (!EditorUtility.DisplayDialog(
                    "Apply KMS Infinite Stage To GameScene",
                    "GameScene의 유한 필드와 경계를 연두색 20×20 청크 3×3 무한 스테이지로 교체합니다.",
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

        private static void ApplyInternal()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            Sprite floorSprite = AssetDatabase.LoadAssetAtPath<Sprite>(FloorSpritePath);
            PlayerStats player = FindSceneComponents<PlayerStats>(scene).SingleOrDefault();
            KmsMonsterSpawner spawner =
                FindSceneComponents<KmsMonsterSpawner>(scene).SingleOrDefault();

            if (player == null)
            {
                throw new InvalidOperationException("GameScene에서 PlayerStats를 하나 찾지 못했습니다.");
            }

            if (spawner == null)
            {
                throw new InvalidOperationException("GameScene에서 KmsMonsterSpawner를 하나 찾지 못했습니다.");
            }

            KmsInfiniteStageTestSceneConfigurator.RebuildStage(
                scene,
                floorSprite,
                player.transform,
                StageRootName,
                LightGreenFloorColor);

            SerializedObject serializedSpawner = new SerializedObject(spawner);
            serializedSpawner.FindProperty("spawnArea").objectReferenceValue = null;
            serializedSpawner.FindProperty("innerSpawnRadius").floatValue =
                KmsMonsterSpawner.DefaultInnerSpawnRadius;
            serializedSpawner.FindProperty("outerSpawnRadius").floatValue =
                KmsMonsterSpawner.DefaultOuterSpawnRadius;
            serializedSpawner.FindProperty("positionAttemptCount").intValue = 64;
            serializedSpawner.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, ScenePath))
            {
                throw new InvalidOperationException("GameScene 무한 스테이지 저장에 실패했습니다.");
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log(
                "[KMS] GameScene에 연두색 20×20 청크 3×3 무한 스테이지와 " +
                "12~24 무경계 몬스터 스폰을 적용했습니다.");
        }

        private static T[] FindSceneComponents<T>(Scene scene) where T : Component
        {
            return scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<T>(true))
                .Where(component => component.gameObject.scene == scene)
                .ToArray();
        }
    }
}
