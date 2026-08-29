using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace KMS.Editor
{
    public static class KmsHealthDropImplementationVerifier
    {
        private const string TestScenePath = "Assets/KMS/TestScene_KMS.unity";
        private const string GameScenePath = "Assets/Scenes/GameScene.unity";
        private const string RuntimePrefabPath =
            "Assets/KMS/Drops/Prefabs/KmsDropRuntime.prefab";
        private const string HealthPrefabPath =
            "Assets/KMS/Drops/Prefabs/KmsHealthPickup.prefab";

        [MenuItem("KMS/Verify Health Drop Assets")]
        public static void VerifyAssets()
        {
            Require(Mathf.Approximately(KmsHealthDropController.DefaultHealthDropChance, 0.01f),
                "회복 아이템 기본 드롭 확률은 1%여야 합니다.");
            Require(KmsHealthDropController.ShouldDrop(0.009f, 0.01f)
                && !KmsHealthDropController.ShouldDrop(0.01f, 0.01f)
                && !KmsHealthDropController.ShouldDrop(0f, 0f)
                && KmsHealthDropController.ShouldDrop(1f, 1f),
                "회복 아이템 드롭 확률의 포함·제외 경계가 올바르지 않습니다.");
            Require(Mathf.Approximately(
                    KmsHealthPickup.CalculateHealAmount(
                        100f,
                        KmsHealthPickup.DefaultMaxHealthFraction),
                    20f)
                && Mathf.Approximately(
                    KmsHealthPickup.CalculateHealAmount(
                        177f,
                        KmsHealthPickup.DefaultMaxHealthFraction),
                    35.4f),
                "회복량은 플레이어 최대 체력의 20%로 비례해야 합니다.");

            GameObject healthPrefab = LoadRequired<GameObject>(HealthPrefabPath);
            KmsHealthPickup healthPickup = healthPrefab.GetComponent<KmsHealthPickup>();
            Require(healthPickup != null, "회복 픽업 프리팹에 KmsHealthPickup이 없습니다.");
            Require(Mathf.Approximately(
                    healthPickup.MaxHealthFraction,
                    KmsHealthPickup.DefaultMaxHealthFraction),
                "회복 픽업 프리팹의 회복 비율은 최대 체력의 20%여야 합니다.");

            GameObject runtimePrefab = LoadRequired<GameObject>(RuntimePrefabPath);
            KmsPickupManager manager = RequireSingle<KmsPickupManager>(runtimePrefab, RuntimePrefabPath);
            RequireSingle<KmsGoldDropController>(runtimePrefab, RuntimePrefabPath);
            RequireSingle<KmsWeaponDropController>(runtimePrefab, RuntimePrefabPath);
            KmsHealthDropController healthController =
                RequireSingle<KmsHealthDropController>(runtimePrefab, RuntimePrefabPath);
            Require(Mathf.Approximately(
                    healthController.HealthDropChance,
                    KmsHealthDropController.DefaultHealthDropChance),
                "공통 드롭 런타임 프리팹의 회복 드롭 확률은 1%여야 합니다.");

            SerializedObject serializedManager = new SerializedObject(manager);
            Require(serializedManager.FindProperty("healthPickupPrefab").objectReferenceValue
                    == healthPickup,
                "픽업 매니저에 회복 픽업 프리팹이 연결되지 않았습니다.");
            Require(serializedManager.FindProperty("initialHealthPoolSize").intValue > 0,
                "회복 픽업 초기 풀 크기는 1개 이상이어야 합니다.");

            ValidateScene(TestScenePath);
            ValidateScene(GameScenePath);

            Debug.Log(
                "[KMS] 회복 드롭 정적 검증 통과: 독립 1% 판정, 최대 체력 20% 회복, " +
                "회복 픽업·풀·두 씬의 공통 런타임 연결을 확인했습니다.");
        }

        public static void VerifyAssetsFromCommandLine()
        {
            VerifyAssets();
        }

        private static void ValidateScene(string scenePath)
        {
            Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            KmsPickupManager[] managers = scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<KmsPickupManager>(true))
                .ToArray();
            Require(managers.Length == 1,
                $"{scenePath}에는 KmsPickupManager가 정확히 하나 있어야 합니다.");

            GameObject runtimeRoot = managers[0].gameObject;
            Require(PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(runtimeRoot)
                    == RuntimePrefabPath,
                $"{scenePath}의 드롭 런타임이 공통 KMS 프리팹 인스턴스가 아닙니다.");
            Require(runtimeRoot.GetComponents<KmsHealthDropController>().Length == 1,
                $"{scenePath}의 공통 드롭 런타임에 회복 드롭 컨트롤러가 없습니다.");
        }

        private static T LoadRequired<T>(string path) where T : UnityEngine.Object
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null)
            {
                throw new InvalidOperationException($"필수 KMS 에셋을 찾을 수 없습니다: {path}");
            }

            return asset;
        }

        private static T RequireSingle<T>(GameObject root, string location) where T : Component
        {
            T[] components = root.GetComponents<T>();
            Require(components.Length == 1,
                $"{location}에 {typeof(T).Name}이 정확히 하나 있어야 합니다.");
            return components[0];
        }

        private static void Require(bool condition, string message)
        {
            if (!condition)
            {
                throw new InvalidOperationException(message);
            }
        }
    }
}
