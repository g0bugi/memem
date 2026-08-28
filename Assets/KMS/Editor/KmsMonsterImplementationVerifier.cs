using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace KMS.Editor
{
    public static class KmsMonsterImplementationVerifier
    {
        private const string ScenePath = "Assets/KMS/TestScene_KMS.unity";
        private const string NormalDataPath = "Assets/KMS/Monsters/Data/KmsMeleeNormalData.asset";
        private const string FastDataPath = "Assets/KMS/Monsters/Data/KmsMeleeFastData.asset";
        private const string TankDataPath = "Assets/KMS/Monsters/Data/KmsMeleeTankData.asset";
        private const string RangedDataPath = "Assets/KMS/Monsters/Data/KmsRangedNormalData.asset";
        private const string SchedulePath =
            "Assets/KMS/Monsters/Waves/KmsMonsterTestWaveSchedule.asset";

        [MenuItem("KMS/Verify Monster Wave Assets")]
        public static void VerifyAssets()
        {
            KmsMonsterData normal = LoadRequired<KmsMonsterData>(NormalDataPath);
            KmsMonsterData fast = LoadRequired<KmsMonsterData>(FastDataPath);
            KmsMonsterData tank = LoadRequired<KmsMonsterData>(TankDataPath);
            KmsMonsterData ranged = LoadRequired<KmsMonsterData>(RangedDataPath);
            KmsWaveScheduleData schedule = LoadRequired<KmsWaveScheduleData>(SchedulePath);

            ValidateData(normal);
            ValidateData(fast);
            ValidateData(tank);
            ValidateData(ranged);

            Require(normal.Prefab == fast.Prefab && fast.Prefab == tank.Prefab,
                "근거리 세 SO가 하나의 공용 프리팹을 참조해야 합니다.");
            Require(ranged.Prefab != normal.Prefab,
                "원거리 몬스터는 발사 위치를 가진 원거리 프리팹을 사용해야 합니다.");
            Require(ranged.ProjectilePrefab != null,
                "원거리 몬스터 투사체 프리팹이 필요합니다.");

            Require(schedule.TryGetPhase(0f, out KmsWavePhase first),
                "0초 웨이브 페이즈가 필요합니다.");
            Require(schedule.TryGetPhase(15f, out KmsWavePhase second) && second != first,
                "15초 테스트 페이즈 전환이 필요합니다.");
            Require(schedule.TryGetPhase(35f, out KmsWavePhase third) && third != second,
                "35초 테스트 페이즈 전환이 필요합니다.");
            Require(KmsWaveScheduleData.TrySelectMonster(first, 0f, out KmsMonsterData selected) &&
                selected != null, "첫 웨이브의 가중치 선택이 유효해야 합니다.");

            ValidatePrefab(normal.Prefab);
            ValidatePrefab(ranged.Prefab);
            ValidateProjectilePrefab(ranged.ProjectilePrefab);
            ValidateScene();

            Debug.Log("[KMS] 몬스터 SO·공용 프리팹·풀·웨이브 테스트 씬 정적 검증을 통과했습니다.");
        }

        public static void VerifyAssetsFromCommandLine()
        {
            VerifyAssets();
        }

        private static void ValidateData(KmsMonsterData data)
        {
            Require(data.TryValidate(out string error), error);
            Require(data.MaxHealth > 0f, $"{data.name}: 체력이 0보다 커야 합니다.");
            Require(data.MoveSpeed >= 0f, $"{data.name}: 이동속도가 음수일 수 없습니다.");
        }

        private static void ValidatePrefab(KmsMonster prefab)
        {
            Require(prefab != null, "몬스터 프리팹이 필요합니다.");
            Require(prefab.GetComponent<Rigidbody2D>() != null,
                $"{prefab.name}: Rigidbody2D가 필요합니다.");
            Require(prefab.GetComponent<Collider2D>() != null,
                $"{prefab.name}: Collider2D가 필요합니다.");

            SerializedObject serializedMonster = new SerializedObject(prefab);
            Require(serializedMonster.FindProperty("visualRoot").objectReferenceValue != null,
                $"{prefab.name}: Visual 루트 참조가 필요합니다.");
            Require(serializedMonster.FindProperty("visualRenderer").objectReferenceValue != null,
                $"{prefab.name}: Visual Renderer 참조가 필요합니다.");
            Require(serializedMonster.FindProperty("healthBarFill").objectReferenceValue != null,
                $"{prefab.name}: 체력바 Fill 참조가 필요합니다.");
        }

        private static void ValidateProjectilePrefab(KmsMonsterProjectile prefab)
        {
            Require(prefab.GetComponent<Rigidbody2D>() != null,
                "적 투사체 Rigidbody2D가 필요합니다.");
            Collider2D collider = prefab.GetComponent<Collider2D>();
            Require(collider != null && collider.isTrigger,
                "적 투사체 Trigger Collider2D가 필요합니다.");
        }

        private static void ValidateScene()
        {
            Scene scene = SceneManager.GetSceneByPath(ScenePath);
            bool closeAfterValidation = !scene.IsValid() || !scene.isLoaded;
            if (closeAfterValidation)
            {
                scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            }

            try
            {
                Require(FindSceneComponents<KmsMonsterSpawner>(scene).Length == 1,
                    "TestScene_KMS에는 KmsMonsterSpawner가 정확히 하나 필요합니다.");
                Require(FindSceneComponents<KmsWaveDirector>(scene).Length == 1,
                    "TestScene_KMS에는 KmsWaveDirector가 정확히 하나 필요합니다.");
                Require(FindSceneComponents<KmsMonsterProjectilePool>(scene).Length == 1,
                    "TestScene_KMS에는 적 투사체 풀이 정확히 하나 필요합니다.");
                Require(FindSceneComponents<KmsRunTimer>(scene).Length == 1,
                    "TestScene_KMS에는 웨이브 시간 제공자가 정확히 하나 필요합니다.");

                KmsMonsterSpawner spawner = FindSceneComponents<KmsMonsterSpawner>(scene)[0];
                SerializedObject serializedSpawner = new SerializedObject(spawner);
                SerializedProperty knownData = serializedSpawner.FindProperty("knownMonsterData");
                Require(knownData != null && knownData.arraySize == 4,
                    "Spawner에는 테스트 MonsterData 네 종류가 필요합니다.");
                Require(serializedSpawner.FindProperty("playerTarget").objectReferenceValue != null,
                    "Spawner의 Player 타깃 참조가 필요합니다.");
                Require(serializedSpawner.FindProperty("spawnArea").objectReferenceValue != null,
                    "Spawner의 유효 생성 영역 참조가 필요합니다.");
                Require(!serializedSpawner.FindProperty("spawnOnStart").boolValue,
                    "WaveDirector와 초기 자동 스폰을 동시에 사용하면 안 됩니다.");
            }
            finally
            {
                if (closeAfterValidation && scene.IsValid() && scene.isLoaded)
                {
                    EditorSceneManager.CloseScene(scene, true);
                }
            }
        }

        private static T[] FindSceneComponents<T>(Scene scene) where T : Component
        {
            return scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<T>(true))
                .Where(component => component.gameObject.scene == scene)
                .ToArray();
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

        private static void Require(bool condition, string message)
        {
            if (!condition)
            {
                throw new InvalidOperationException(message);
            }
        }
    }
}
