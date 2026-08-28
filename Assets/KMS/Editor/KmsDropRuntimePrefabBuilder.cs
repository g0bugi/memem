using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace KMS.Editor
{
    internal static class KmsDropRuntimePrefabBuilder
    {
        internal const string PrefabPath = "Assets/KMS/Drops/Prefabs/KmsDropRuntime.prefab";
        private const string RuntimeObjectName = "KmsDropRuntime";
        private const string LegacyGoldObjectName = "KmsGoldDropController";
        private const string LegacyWeaponObjectName = "KmsWeaponDropController";

        internal static GameObject BuildOrUpdatePrefab(
            KmsGoldPickup goldPickupPrefab,
            KmsWeaponPickup weaponPickupPrefab,
            KmsWeaponDropTable dropTable)
        {
            if (goldPickupPrefab == null || weaponPickupPrefab == null || dropTable == null)
            {
                throw new InvalidOperationException("드롭 런타임 프리팹을 구성할 에셋 참조가 부족합니다.");
            }

            GameObject existingPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (existingPrefab == null)
            {
                GameObject runtimeObject = new GameObject(RuntimeObjectName);
                try
                {
                    ConfigurePrefabContents(runtimeObject, goldPickupPrefab, weaponPickupPrefab, dropTable);
                    return PrefabUtility.SaveAsPrefabAsset(runtimeObject, PrefabPath);
                }
                finally
                {
                    UnityEngine.Object.DestroyImmediate(runtimeObject);
                }
            }

            GameObject prefabContents = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                ConfigurePrefabContents(prefabContents, goldPickupPrefab, weaponPickupPrefab, dropTable);
                return PrefabUtility.SaveAsPrefabAsset(prefabContents, PrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefabContents);
            }
        }

        internal static GameObject InstantiateOrReplaceLegacy(Scene scene)
        {
            KmsPickupManager[] managers = FindSceneComponents<KmsPickupManager>(scene);
            if (managers.Length > 1)
            {
                throw new InvalidOperationException(
                    $"{scene.path}에 {nameof(KmsPickupManager)}가 {managers.Length}개 있어 자동 적용할 수 없습니다.");
            }

            if (managers.Length == 1)
            {
                GameObject existingRuntime = managers[0].gameObject;
                string sourcePath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(existingRuntime);
                if (sourcePath != PrefabPath)
                {
                    throw new InvalidOperationException(
                        $"{scene.path}의 {RuntimeObjectName}이 공통 드롭 프리팹 인스턴스가 아닙니다.");
                }

                ValidateRuntimeInstance(scene, existingRuntime);
                return existingRuntime;
            }

            KmsGoldDropController[] goldControllers = FindSceneComponents<KmsGoldDropController>(scene);
            KmsWeaponDropController[] weaponControllers = FindSceneComponents<KmsWeaponDropController>(scene);
            if (goldControllers.Length > 1 || weaponControllers.Length > 1)
            {
                throw new InvalidOperationException(
                    $"{scene.path}에 레거시 드롭 컨트롤러가 중복되어 자동 교체할 수 없습니다.");
            }

            if (goldControllers.Length == 1)
            {
                ValidateLegacyController(goldControllers[0], LegacyGoldObjectName);
            }

            if (weaponControllers.Length == 1)
            {
                ValidateLegacyController(weaponControllers[0], LegacyWeaponObjectName);
            }

            GameObject conflictingRoot = scene.GetRootGameObjects()
                .FirstOrDefault(candidate => candidate.name == RuntimeObjectName);
            if (conflictingRoot != null)
            {
                throw new InvalidOperationException(
                    $"{scene.path}에 이름이 '{RuntimeObjectName}'인 다른 루트 오브젝트가 있습니다.");
            }

            if (goldControllers.Length == 1)
            {
                UnityEngine.Object.DestroyImmediate(goldControllers[0].gameObject);
            }

            if (weaponControllers.Length == 1)
            {
                UnityEngine.Object.DestroyImmediate(weaponControllers[0].gameObject);
            }

            GameObject runtimePrefab = LoadAndValidatePrefab();
            GameObject instance = PrefabUtility.InstantiatePrefab(runtimePrefab, scene) as GameObject;
            if (instance == null)
            {
                throw new InvalidOperationException($"드롭 런타임 프리팹 인스턴스 생성에 실패했습니다: {PrefabPath}");
            }

            ValidateRuntimeInstance(scene, instance);
            return instance;
        }

        internal static GameObject LoadAndValidatePrefab()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (prefab == null)
            {
                throw new InvalidOperationException($"드롭 런타임 프리팹을 찾을 수 없습니다: {PrefabPath}");
            }

            ValidateRuntimeComponents(prefab, PrefabPath);
            return prefab;
        }

        private static void ConfigurePrefabContents(
            GameObject root,
            KmsGoldPickup goldPickupPrefab,
            KmsWeaponPickup weaponPickupPrefab,
            KmsWeaponDropTable dropTable)
        {
            root.name = RuntimeObjectName;
            root.transform.localPosition = Vector3.zero;
            root.transform.localRotation = Quaternion.identity;
            root.transform.localScale = Vector3.one;

            KmsPickupManager manager = GetOrAddSingleComponent<KmsPickupManager>(root);
            GetOrAddSingleComponent<KmsGoldDropController>(root);
            KmsWeaponDropController weaponController = GetOrAddSingleComponent<KmsWeaponDropController>(root);

            manager.ConfigureAssets(goldPickupPrefab, weaponPickupPrefab);
            weaponController.ConfigureDropTable(dropTable);
            EditorUtility.SetDirty(manager);
            EditorUtility.SetDirty(weaponController);

            ValidateRuntimeComponents(root, PrefabPath);
        }

        private static T GetOrAddSingleComponent<T>(GameObject root) where T : Component
        {
            T[] components = root.GetComponents<T>();
            if (components.Length > 1)
            {
                throw new InvalidOperationException(
                    $"{PrefabPath} 루트에 {typeof(T).Name}이 {components.Length}개 있습니다.");
            }

            return components.Length == 1 ? components[0] : root.AddComponent<T>();
        }

        private static void ValidateRuntimeComponents(GameObject root, string location)
        {
            if (root.GetComponents<KmsPickupManager>().Length != 1
                || root.GetComponents<KmsGoldDropController>().Length != 1
                || root.GetComponents<KmsWeaponDropController>().Length != 1)
            {
                throw new InvalidOperationException(
                    $"{location}의 루트에는 픽업 매니저와 골드·무기 드롭 컨트롤러가 각각 1개 필요합니다.");
            }
        }

        private static void ValidateRuntimeInstance(Scene scene, GameObject root)
        {
            if (root.transform.parent != null || root.gameObject.scene != scene)
            {
                throw new InvalidOperationException($"{scene.path}의 {RuntimeObjectName}은 씬 루트여야 합니다.");
            }

            ValidateRuntimeComponents(root, scene.path);

            KmsGoldDropController[] goldControllers = FindSceneComponents<KmsGoldDropController>(scene);
            KmsWeaponDropController[] weaponControllers = FindSceneComponents<KmsWeaponDropController>(scene);
            if (goldControllers.Length != 1 || weaponControllers.Length != 1
                || goldControllers[0].gameObject != root || weaponControllers[0].gameObject != root)
            {
                throw new InvalidOperationException(
                    $"{scene.path}에는 공통 드롭 런타임 외의 드롭 컨트롤러가 없어야 합니다.");
            }
        }

        private static void ValidateLegacyController(Component controller, string expectedName)
        {
            GameObject controllerObject = controller.gameObject;
            bool hasOnlyExpectedComponents = controllerObject.GetComponents<Component>()
                .All(component => component is Transform || component == controller);
            if (controllerObject.name != expectedName
                || controllerObject.transform.parent != null
                || controllerObject.transform.childCount != 0
                || !hasOnlyExpectedComponents)
            {
                throw new InvalidOperationException(
                    $"'{controllerObject.name}'에 보존해야 할 구성요소가 있어 레거시 컨트롤러를 자동 제거하지 않았습니다.");
            }
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
