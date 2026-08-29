using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace KMS.Editor
{
    [InitializeOnLoad]
    public static class KmsHealthDropRuntimeVerifier
    {
        private const string ScenePath = "Assets/KMS/TestScene_KMS.unity";
        private const string NormalDataPath =
            "Assets/KMS/Monsters/Data/KmsMeleeNormalData.asset";
        private const string RunningKey = "KMS.HealthDropSmoke.Running";
        private const string FinishingKey = "KMS.HealthDropSmoke.Finishing";
        private const string ExitCodeKey = "KMS.HealthDropSmoke.ExitCode";

        private static int stage;
        private static double stageStartedAt;
        private static PlayerStats player;
        private static KmsPickupManager pickupManager;
        private static KmsHealthDropController healthDropController;
        private static KmsHealthPickup spawnedPickup;
        private static float expectedHealthAfterPickup;

        static KmsHealthDropRuntimeVerifier()
        {
            if (!SessionState.GetBool(RunningKey, false))
            {
                return;
            }

            EditorApplication.playModeStateChanged -= HandlePlayModeStateChanged;
            EditorApplication.playModeStateChanged += HandlePlayModeStateChanged;
            if (EditorApplication.isPlaying)
            {
                BeginPlayModeVerification();
            }
        }

        public static void RunFromCommandLine()
        {
            SessionState.SetBool(RunningKey, true);
            SessionState.SetBool(FinishingKey, false);
            SessionState.SetInt(ExitCodeKey, 1);
            EditorApplication.playModeStateChanged -= HandlePlayModeStateChanged;
            EditorApplication.playModeStateChanged += HandlePlayModeStateChanged;

            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            EditorApplication.EnterPlaymode();
        }

        private static void HandlePlayModeStateChanged(PlayModeStateChange state)
        {
            if (!SessionState.GetBool(RunningKey, false))
            {
                return;
            }

            if (state == PlayModeStateChange.EnteredPlayMode)
            {
                BeginPlayModeVerification();
                return;
            }

            if (state == PlayModeStateChange.EnteredEditMode
                && SessionState.GetBool(FinishingKey, false))
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
            player = null;
            pickupManager = null;
            healthDropController = null;
            spawnedPickup = null;
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
                        BeginForcedDrop();
                        break;
                    case 1:
                        MovePlayerOntoCollectiblePickup();
                        break;
                    case 2:
                        VerifyCollection();
                        break;
                }
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                RequestExit(1);
            }
        }

        private static void BeginForcedDrop()
        {
            if (EditorApplication.timeSinceStartup - stageStartedAt < 0.25d)
            {
                return;
            }

            KmsMonsterSpawner spawner =
                UnityEngine.Object.FindFirstObjectByType<KmsMonsterSpawner>();
            KmsWaveDirector director =
                UnityEngine.Object.FindFirstObjectByType<KmsWaveDirector>();
            player = UnityEngine.Object.FindFirstObjectByType<PlayerStats>();
            pickupManager = UnityEngine.Object.FindFirstObjectByType<KmsPickupManager>();
            healthDropController =
                UnityEngine.Object.FindFirstObjectByType<KmsHealthDropController>();

            Require(spawner != null, "회복 드롭 검증 중 KmsMonsterSpawner를 찾을 수 없습니다.");
            Require(director != null, "회복 드롭 검증 중 KmsWaveDirector를 찾을 수 없습니다.");
            Require(player != null, "회복 드롭 검증 중 PlayerStats를 찾을 수 없습니다.");
            Require(pickupManager != null, "회복 드롭 검증 중 KmsPickupManager를 찾을 수 없습니다.");
            Require(healthDropController != null,
                "회복 드롭 검증 중 KmsHealthDropController를 찾을 수 없습니다.");

            director.enabled = false;
            spawner.DespawnAll();

            player.TakeDamage(player.MaxHealth * 0.5f);
            float damagedHealth = player.CurrentHealth;
            expectedHealthAfterPickup = Mathf.Min(
                player.MaxHealth,
                damagedHealth + KmsHealthPickup.CalculateHealAmount(
                    player.MaxHealth,
                    KmsHealthPickup.DefaultMaxHealthFraction));

            SerializedObject serializedController = new SerializedObject(healthDropController);
            SerializedProperty chance = serializedController.FindProperty("healthDropChance");
            chance.floatValue = 1f;
            serializedController.ApplyModifiedPropertiesWithoutUndo();

            int spawnedCountBefore = healthDropController.TotalSpawnedPickupCount;
            KmsMonsterData normal = AssetDatabase.LoadAssetAtPath<KmsMonsterData>(NormalDataPath);
            Require(normal != null, "회복 드롭 검증용 MonsterData를 찾을 수 없습니다.");
            Require(spawner.TrySpawnAt(
                    normal,
                    player.transform.position + new Vector3(2f, 0f, 0f)),
                "회복 드롭 검증용 몬스터 생성에 실패했습니다.");
            KmsMonster monster = UnityEngine.Object
                .FindObjectsByType<KmsMonster>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)
                .First(candidate => candidate.IsPrepared && candidate.Data == normal);
            monster.TakeDamage(normal.MaxHealth + 1f);

            chance.floatValue = KmsHealthDropController.DefaultHealthDropChance;
            serializedController.ApplyModifiedPropertiesWithoutUndo();

            Require(healthDropController.TotalSpawnedPickupCount == spawnedCountBefore + 1,
                "100% 임시 판정에서 회복 픽업이 정확히 하나 생성되지 않았습니다.");
            Require(pickupManager.ActiveHealthCount == 1,
                "생성된 회복 픽업이 활성 풀 목록에 정확히 하나 있어야 합니다.");
            Require(Mathf.Approximately(
                    healthDropController.HealthDropChance,
                    KmsHealthDropController.DefaultHealthDropChance),
                "검증 후 회복 드롭 확률이 1%로 복구되지 않았습니다.");

            spawnedPickup = UnityEngine.Object.FindFirstObjectByType<KmsHealthPickup>();
            Require(spawnedPickup != null && spawnedPickup.gameObject.activeInHierarchy,
                "생성된 회복 픽업 인스턴스를 찾을 수 없습니다.");
            stage = 1;
            stageStartedAt = EditorApplication.timeSinceStartup;
        }

        private static void MovePlayerOntoCollectiblePickup()
        {
            if (spawnedPickup != null && spawnedPickup.IsCollectible)
            {
                player.transform.position = spawnedPickup.transform.position;
                stage = 2;
                stageStartedAt = EditorApplication.timeSinceStartup;
                return;
            }

            Require(EditorApplication.timeSinceStartup - stageStartedAt < 2d,
                "회복 픽업의 흩뿌리기 연출이 제한 시간 안에 끝나지 않았습니다.");
        }

        private static void VerifyCollection()
        {
            if (pickupManager.ActiveHealthCount > 0
                && EditorApplication.timeSinceStartup - stageStartedAt < 1d)
            {
                return;
            }

            Require(pickupManager.ActiveHealthCount == 0,
                "플레이어 접촉 후 회복 픽업이 즉시 풀로 반환되지 않았습니다.");
            Require(Mathf.Approximately(player.CurrentHealth, expectedHealthAfterPickup),
                $"회복 후 체력이 예상값과 다릅니다. expected={expectedHealthAfterPickup}, " +
                $"actual={player.CurrentHealth}");
            Require(spawnedPickup != null && !spawnedPickup.gameObject.activeSelf,
                "수집한 회복 픽업 인스턴스가 비활성 풀 상태가 아닙니다.");

            Debug.Log(
                "[KMS] 회복 드롭 Play Mode 검증 통과: 런타임에서 확률을 100%로 임시 적용해 " +
                "몬스터 사망 시 단일 픽업 생성, 최대 체력 20% 회복, 접촉 즉시 소비·풀 반환, " +
                "드롭 확률 1% 복구를 확인했습니다.");
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
            if (!condition)
            {
                throw new InvalidOperationException(message);
            }
        }
    }
}
