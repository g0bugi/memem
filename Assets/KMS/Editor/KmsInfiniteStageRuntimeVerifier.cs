using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace KMS.Editor
{
    [InitializeOnLoad]
    public static class KmsInfiniteStageRuntimeVerifier
    {
        private const string ScenePath = "Assets/KMS/TestScene_KMS.unity";
        private const string NormalDataPath =
            "Assets/KMS/Monsters/Data/KmsMeleeNormalData.asset";
        private const string BossDataPath =
            "Assets/KMS/Monsters/Data/KmsMeleeBossData.asset";
        private const string RunningKey = "KMS.InfiniteStageVerification.Running";
        private const string FinishingKey = "KMS.InfiniteStageVerification.Finishing";
        private const string ExitCodeKey = "KMS.InfiniteStageVerification.ExitCode";

        private static int stage;
        private static int lastHandledTrialWave;
        private static bool pressureKillsApplied;
        private static bool trialBossLeadObserved;
        private static bool firstTrialWaveVerified;
        private static bool subsequentTrialBossLeadObserved;
        private static double stageStartedAt;
        private static int initialChunkCreationCount;

        private static KmsInfiniteStageScroller scroller;
        private static KmsMonsterSpawner spawner;
        private static KmsWaveDirector director;
        private static PlayerStats player;
        private static Rigidbody2D playerBody;
        private static Camera mainCamera;

        static KmsInfiniteStageRuntimeVerifier()
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

            if (state == PlayModeStateChange.EnteredEditMode &&
                SessionState.GetBool(FinishingKey, false))
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
            lastHandledTrialWave = 0;
            pressureKillsApplied = false;
            trialBossLeadObserved = false;
            firstTrialWaveVerified = false;
            subsequentTrialBossLeadObserved = false;
            stageStartedAt = EditorApplication.timeSinceStartup;
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
                        BeginChunkVerification();
                        break;
                    case 1:
                        VerifyHorizontalRecycle();
                        break;
                    case 2:
                        VerifyVerticalRecycle();
                        break;
                    case 3:
                        VerifyCornerRecycleAndUnboundedSpawn();
                        break;
                    case 4:
                        VerifyTrialRuntimePath();
                        break;
                    case 5:
                        VerifyInclusiveEightyPercentSetup();
                        break;
                    case 6:
                        VerifyInclusiveEightyPercentResult();
                        break;
                    case 7:
                        VerifyBelowEightyPercentSetup();
                        break;
                    case 8:
                        VerifyBelowEightyPercentResult();
                        break;
                    case 9:
                        VerifyCapLimitedPartialWave();
                        break;
                }
            }
            catch (Exception exception)
            {
                FinishWithFailure(exception);
            }
        }

        private static void BeginChunkVerification()
        {
            if (EditorApplication.timeSinceStartup - stageStartedAt < 0.25d)
            {
                return;
            }

            scroller = UnityEngine.Object.FindFirstObjectByType<KmsInfiniteStageScroller>();
            spawner = UnityEngine.Object.FindFirstObjectByType<KmsMonsterSpawner>();
            director = UnityEngine.Object.FindFirstObjectByType<KmsWaveDirector>();
            player = UnityEngine.Object.FindFirstObjectByType<PlayerStats>();
            mainCamera = Camera.main;

            Require(scroller != null && scroller.IsInitialized,
                "Play Mode에서 무한 스테이지 청크가 초기화되지 않았습니다.");
            Require(spawner != null, "Play Mode에서 KmsMonsterSpawner를 찾을 수 없습니다.");
            Require(director != null, "Play Mode에서 KmsWaveDirector를 찾을 수 없습니다.");
            Require(player != null, "Play Mode에서 PlayerStats를 찾을 수 없습니다.");
            Require(mainCamera != null, "Play Mode에서 Main Camera를 찾을 수 없습니다.");

            VerifyDirectedSpawnPatterns();

            playerBody = player.GetComponent<Rigidbody2D>();
            Require(playerBody != null, "무한 이동 검증용 Player Rigidbody2D가 없습니다.");
            Require(scroller.ActiveChunkCount == 9 &&
                scroller.RuntimeChunkCreationCount == 9 &&
                scroller.ChunkSize == new Vector2(20f, 20f) &&
                scroller.GridSize == new Vector2Int(3, 3),
                "무한 스테이지는 20×20 청크 9개를 3×3으로 한 번만 생성해야 합니다.");
            Require(scroller.CenterChunkCoordinate == Vector2Int.zero,
                "원점 시작 시 중앙 청크 좌표는 (0, 0)이어야 합니다.");

            Require(Mathf.Approximately(
                    spawner.MinimumSpawnRadius,
                    KmsMonsterSpawner.DefaultInnerSpawnRadius) &&
                Mathf.Approximately(
                    spawner.MaximumSpawnRadius,
                    KmsMonsterSpawner.DefaultOuterSpawnRadius),
                "Play Mode의 몬스터 생성 반경은 플레이어 기준 12~24여야 합니다.");
            Require(!KmsWaveDirector.MeetsDeathPressureCondition(90, 71, 0.8f) &&
                KmsWaveDirector.MeetsDeathPressureCondition(90, 72, 0.8f),
                "처치 부진 규칙은 최근 3웨이브 생존율 80% 경계를 포함해야 합니다.");
            Require(!KmsWaveDirector.MeetsTrialCondition(2, 3, 0, 30) &&
                KmsWaveDirector.MeetsTrialCondition(3, 3, 29, 30) &&
                !KmsWaveDirector.MeetsTrialCondition(3, 3, 30, 30),
                "시련은 3웨이브부터 활성 수가 다음 요청 수보다 엄격히 작을 때만 발동해야 합니다.");

            initialChunkCreationCount = scroller.RuntimeChunkCreationCount;
            Time.timeScale = 0f;
            MovePlayer(new Vector2(21.25f, 0f));
            stage = 1;
            stageStartedAt = EditorApplication.timeSinceStartup;
        }

        private static void VerifyDirectedSpawnPatterns()
        {
            MethodInfo buildPositions = typeof(KmsMonsterSpawner).GetMethod(
                "TryBuildSpawnPositions",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Require(buildPositions != null,
                "지시형 웨이브 위치 계산기를 찾을 수 없습니다.");

            List<Vector3> clockwisePositions = new List<Vector3>();
            object[] clockwiseArguments =
            {
                KmsWaveSpawnPattern.Clockwise,
                40,
                21031,
                clockwisePositions
            };
            bool clockwiseBuilt = (bool)buildPositions.Invoke(spawner, clockwiseArguments);
            Require(clockwiseBuilt && clockwisePositions.Count == 40,
                "시계 방향 웨이브가 요청 수만큼 위치를 만들지 못했습니다.");

            Vector3 center = player.transform.position;
            float minimumDirectedRadius = Mathf.Lerp(
                spawner.MinimumSpawnRadius,
                spawner.MaximumSpawnRadius,
                0.65f);
            for (int index = 0; index < clockwisePositions.Count; index++)
            {
                Vector2 offset = clockwisePositions[index] - center;
                float distance = offset.magnitude;
                Require(distance >= minimumDirectedRadius - 0.01f &&
                    distance <= spawner.MaximumSpawnRadius + 0.01f,
                    $"시계 방향 생성 위치가 지시 반경을 벗어났습니다: {distance:0.00}");

                if (index + 1 >= clockwisePositions.Count)
                {
                    continue;
                }

                Vector2 nextOffset = clockwisePositions[index + 1] - center;
                float angle = Mathf.Atan2(offset.y, offset.x) * Mathf.Rad2Deg;
                float nextAngle = Mathf.Atan2(nextOffset.y, nextOffset.x) * Mathf.Rad2Deg;
                Require(Mathf.DeltaAngle(angle, nextAngle) < 0f,
                    "시계 방향 위치 목록의 진행 순서가 반시계 방향으로 역전됐습니다.");
            }

            List<Vector3> perimeterPositions = new List<Vector3>();
            object[] perimeterArguments =
            {
                KmsWaveSpawnPattern.ScreenPerimeter,
                100,
                21049,
                perimeterPositions
            };
            bool perimeterBuilt = (bool)buildPositions.Invoke(spawner, perimeterArguments);
            Require(perimeterBuilt && perimeterPositions.Count == 100,
                "화면 테두리 웨이브가 요청 수만큼 위치를 만들지 못했습니다.");

            Vector3 cameraCenter = mainCamera.transform.position;
            float minimumHalfHeight = mainCamera.orthographicSize + 1f;
            float minimumHalfWidth = (mainCamera.orthographicSize * mainCamera.aspect) + 1f;
            foreach (Vector3 position in perimeterPositions)
            {
                float xDistance = Mathf.Abs(position.x - cameraCenter.x);
                float yDistance = Mathf.Abs(position.y - cameraCenter.y);
                Require(xDistance >= minimumHalfWidth - 0.01f ||
                    yDistance >= minimumHalfHeight - 0.01f,
                    "화면 테두리 생성 위치가 카메라 바깥 사각 테두리보다 안쪽에 있습니다.");
            }

            KmsMonsterData normal = AssetDatabase.LoadAssetAtPath<KmsMonsterData>(NormalDataPath);
            Require(normal != null,
                "지시형 위치 생성 검증용 Normal MonsterData를 찾을 수 없습니다.");
            Require(spawner.TrySpawnAt(normal, clockwisePositions[0]) &&
                spawner.TrySpawnAt(normal, perimeterPositions[0]),
                "계산된 지시형 위치에서 실제 몬스터 생성에 실패했습니다.");
            Require(spawner.ActiveCount == 2,
                "지시형 위치 검증에서 정확히 2마리가 활성화되지 않았습니다.");
            spawner.DespawnAll();
        }

        private static void VerifyHorizontalRecycle()
        {
            WaitForStageFrame();
            Require(scroller.CenterChunkCoordinate == new Vector2Int(1, 0),
                "플레이어가 오른쪽 청크로 이동해도 바닥 중앙 좌표가 갱신되지 않았습니다.");
            VerifyChunkReuseAndCoverage();
            Require(Mathf.Abs(mainCamera.transform.position.x - player.transform.position.x) < 0.05f,
                "플레이어 연속 좌표 이동 뒤 카메라가 플레이어 중앙을 따라오지 않았습니다.");

            MovePlayer(new Vector2(21.25f, -21.25f));
            stage = 2;
            stageStartedAt = EditorApplication.timeSinceStartup;
        }

        private static void VerifyVerticalRecycle()
        {
            WaitForStageFrame();
            Require(scroller.CenterChunkCoordinate == new Vector2Int(1, -1),
                "플레이어가 아래 청크로 이동해도 바닥 중앙 좌표가 갱신되지 않았습니다.");
            VerifyChunkReuseAndCoverage();

            MovePlayer(new Vector2(-21.25f, 21.25f));
            stage = 3;
            stageStartedAt = EditorApplication.timeSinceStartup;
        }

        private static void VerifyCornerRecycleAndUnboundedSpawn()
        {
            WaitForStageFrame();
            Require(scroller.CenterChunkCoordinate == new Vector2Int(-1, 1),
                "플레이어가 대각선으로 청크 경계를 넘어도 2축 중앙 좌표가 갱신돼야 합니다.");
            Require(scroller.RepositionCount >= 3,
                "가로·세로·대각선 이동에서 청크 재배치가 모두 발생하지 않았습니다.");
            VerifyChunkReuseAndCoverage();

            spawner.DespawnAll();
            KmsMonsterData normal = AssetDatabase.LoadAssetAtPath<KmsMonsterData>(NormalDataPath);
            Require(normal != null, "무경계 스폰 검증용 MonsterData를 찾을 수 없습니다.");
            Require(spawner.TrySpawn(normal),
                "기존 유한 필드 밖 플레이어 위치에서 몬스터 생성에 실패했습니다.");

            KmsMonster spawned = UnityEngine.Object
                .FindObjectsByType<KmsMonster>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)
                .FirstOrDefault(candidate => candidate.IsPrepared && candidate.Data == normal);
            Require(spawned != null, "무경계 생성된 활성 몬스터를 찾을 수 없습니다.");
            float spawnDistance = Vector2.Distance(spawned.transform.position, player.transform.position);
            Require(spawnDistance >= 12f - 0.01f && spawnDistance <= 24f + 0.01f,
                $"무경계 스폰이 플레이어 기준 12~24 반경을 벗어났습니다: {spawnDistance:0.00}");

            spawner.DespawnAll();
            MovePlayer(Vector2.zero);
            player.gameObject.SetActive(false);
            director.ResetForNewRun();
            Time.timeScale = 5f;
            lastHandledTrialWave = 0;
            trialBossLeadObserved = false;
            firstTrialWaveVerified = false;
            subsequentTrialBossLeadObserved = false;
            stage = 4;
            stageStartedAt = EditorApplication.timeSinceStartup;
        }

        private static void VerifyTrialRuntimePath()
        {
            EnsureStageTimeout(9d, "시련 런타임 검증이 4웨이브까지 진행되지 않았습니다.");

            if (director.CurrentWaveNumber >= 1 && lastHandledTrialWave < 1)
            {
                spawner.DespawnAll();
                lastHandledTrialWave = 1;
            }

            if (director.CurrentWaveNumber >= 2 && lastHandledTrialWave < 2)
            {
                spawner.DespawnAll();
                lastHandledTrialWave = 2;
            }

            if (director.CurrentWaveNumber == 2 && director.IsTrialActive &&
                !trialBossLeadObserved)
            {
                KmsMonsterData boss = AssetDatabase.LoadAssetAtPath<KmsMonsterData>(BossDataPath);
                Require(boss != null, "시련 선행 스폰 검증용 우두머리 MonsterData가 없습니다.");
                KmsMonster[] activeBosses = UnityEngine.Object
                    .FindObjectsByType<KmsMonster>(
                        FindObjectsInactive.Exclude,
                        FindObjectsSortMode.None)
                    .Where(candidate => candidate.IsPrepared && candidate.Data == boss)
                    .ToArray();
                Require(activeBosses.Length == 1 && spawner.ActiveCount == 1,
                    "시련 웨이브 시작에는 일반 몬스터보다 우두머리 한 마리만 먼저 생성돼야 합니다.");
                Require(director.SecondsUntilNextWave > 0f &&
                    director.SecondsUntilNextWave <= 1f,
                    "우두머리 생성 뒤 일반 몬스터 스폰까지 약 1초 대기가 필요합니다.");
                trialBossLeadObserved = true;
            }

            if (director.CurrentWaveNumber < 3)
            {
                return;
            }

            KmsWaveSpawnResult thirdWave = director.LastWaveResult;
            if (!firstTrialWaveVerified)
            {
                Require(director.TrialLevel == 1 && director.IsTrialActive &&
                    thirdWave != null && thirdWave.WaveNumber == 3 &&
                    thirdWave.RequestedMonsterCount == 15 && thirdWave.IsTrialActive &&
                    thirdWave.TrialBossRequested && thirdWave.TrialBossSpawned &&
                    thirdWave.TotalSuccessfulSpawnCount == 16 && trialBossLeadObserved,
                    "3웨이브 시련은 우두머리 한 마리를 먼저, 일반 몬스터 15마리를 약 1초 뒤에 생성해야 합니다.");
                firstTrialWaveVerified = true;
                return;
            }

            if (director.CurrentWaveNumber == 3 && !subsequentTrialBossLeadObserved)
            {
                KmsMonsterData boss = AssetDatabase.LoadAssetAtPath<KmsMonsterData>(BossDataPath);
                Require(boss != null, "후속 시련 웨이브 검증용 우두머리 MonsterData가 없습니다.");
                int activeBossCount = UnityEngine.Object
                    .FindObjectsByType<KmsMonster>(
                        FindObjectsInactive.Exclude,
                        FindObjectsSortMode.None)
                    .Count(candidate => candidate.IsPrepared && candidate.Data == boss);
                if (activeBossCount == 2)
                {
                    Require(spawner.ActiveCount == 17 && director.SecondsUntilNextWave > 0f &&
                        director.SecondsUntilNextWave <= 1f,
                        "시련 지속 중인 다음 웨이브도 기존 몬스터보다 우두머리 한 마리를 1초 먼저 추가해야 합니다.");
                    subsequentTrialBossLeadObserved = true;
                }
            }

            if (director.CurrentWaveNumber < 4)
            {
                return;
            }

            KmsWaveSpawnResult fourthWave = director.LastWaveResult;
            Require(director.TrialLevel == 1 && fourthWave != null &&
                fourthWave.WaveNumber == 4 && fourthWave.RequestedMonsterCount == 15 &&
                fourthWave.TrialBossRequested && fourthWave.TrialBossSpawned &&
                fourthWave.TotalSuccessfulSpawnCount == 16 &&
                subsequentTrialBossLeadObserved,
                "시련이 유지되는 4웨이브에도 우두머리가 정확히 한 마리 추가돼야 합니다.");

            BeginPressureScenario(5);
        }

        private static void VerifyInclusiveEightyPercentSetup()
        {
            EnsureStageTimeout(6d, "80% 포함 경계 검증이 3웨이브까지 진행되지 않았습니다.");
            if (director.CurrentWaveNumber < 3)
            {
                return;
            }

            if (!pressureKillsApplied)
            {
                Require(spawner.ActiveCount == 45,
                    $"80% 경계 검증 전 1~3웨이브가 각각 15마리씩 생성돼야 합니다: {spawner.ActiveCount}");
                KillActiveMonsters(9);
                Require(spawner.ActiveCount == 36,
                    "45마리 중 9마리 처치 후 정확히 36마리가 생존해야 합니다.");
                pressureKillsApplied = true;
            }

            if (director.CurrentWaveNumber < 4)
            {
                return;
            }

            stage = 6;
            VerifyInclusiveEightyPercentResult();
        }

        private static void VerifyInclusiveEightyPercentResult()
        {
            KmsWaveSpawnResult fourthWave = director.LastWaveResult;
            Require(director.IsDeathPressureActive && fourthWave != null &&
                fourthWave.WaveNumber == 4 && fourthWave.BaseMonsterCount == 15 &&
                fourthWave.RequestedMonsterCount == 30 &&
                fourthWave.IsDeathPressureActive,
                "최근 3웨이브에서 정확히 80%가 생존하면 4웨이브 요청이 기본 15→30으로 바뀌어야 합니다.");
            Require(director.LastUnderperformanceSpawnCount == 45 &&
                director.LastUnderperformanceSurvivorCount == 36 &&
                Mathf.Approximately(director.LastUnderperformanceSurvivorRatio, 0.8f),
                "80% 포함 경계가 실제 생성 성공 수 45와 생존 수 36으로 계산되지 않았습니다.");

            BeginPressureScenario(7);
        }

        private static void VerifyBelowEightyPercentSetup()
        {
            EnsureStageTimeout(6d, "80% 미만 경계 검증이 3웨이브까지 진행되지 않았습니다.");
            if (director.CurrentWaveNumber < 3)
            {
                return;
            }

            if (!pressureKillsApplied)
            {
                Require(spawner.ActiveCount == 45,
                    $"80% 미만 검증 전 1~3웨이브가 각각 15마리씩 생성돼야 합니다: {spawner.ActiveCount}");
                KillActiveMonsters(10);
                Require(spawner.ActiveCount == 35,
                    "45마리 중 10마리 처치 후 정확히 35마리가 생존해야 합니다.");
                pressureKillsApplied = true;
            }

            if (director.CurrentWaveNumber < 4)
            {
                return;
            }

            stage = 8;
            VerifyBelowEightyPercentResult();
        }

        private static void VerifyBelowEightyPercentResult()
        {
            KmsWaveSpawnResult fourthWave = director.LastWaveResult;
            Require(!director.IsDeathPressureActive && fourthWave != null &&
                fourthWave.WaveNumber == 4 && fourthWave.RequestedMonsterCount == 15 &&
                !fourthWave.IsDeathPressureActive,
                "최근 3웨이브 생존율이 80% 미만이면 4웨이브 요청은 기본 15마리를 유지해야 합니다.");
            Require(director.LastUnderperformanceSpawnCount == 45 &&
                director.LastUnderperformanceSurvivorCount == 35 &&
                director.LastUnderperformanceSurvivorRatio < 0.8f,
                "80% 미만 경계가 실제 생성 성공 수 45와 생존 수 35로 계산되지 않았습니다.");

            BeginCapLimitedScenario();
        }

        private static void BeginCapLimitedScenario()
        {
            director.ResetForNewRun();
            player.gameObject.SetActive(false);
            SetSpawnerActiveLimit(5);
            Time.timeScale = 20f;
            stage = 9;
            stageStartedAt = EditorApplication.timeSinceStartup;
        }

        private static void VerifyCapLimitedPartialWave()
        {
            EnsureStageTimeout(4d, "활성 상한 부분 생성 검증이 첫 웨이브까지 진행되지 않았습니다.");
            if (director.CurrentWaveNumber < 1)
            {
                return;
            }

            KmsWaveSpawnResult firstWave = director.LastWaveResult;
            Require(firstWave != null && firstWave.WaveNumber == 1 &&
                firstWave.RequestedMonsterCount == 15 &&
                firstWave.SuccessfulSpawnCount == 5 && firstWave.FailedSpawnCount == 10 &&
                spawner.ActiveCount == 5,
                "활성 상한이 5이면 15마리 요청을 안전하게 5마리 성공·10마리 미생성으로 기록해야 합니다.");

            SetSpawnerActiveLimit(KmsMonsterSpawner.DefaultMaximumActive);
            director.ResetForNewRun();
            player.gameObject.SetActive(true);
            Time.timeScale = 1f;
            Debug.Log(
                "[KMS] 무한 스테이지 Play Mode 검증 통과: 20×20 청크 3×3 재사용, " +
                "가로·세로·모서리 연속 이동, 기존 유한 필드 밖 12~24 반경 스폰, " +
                "3·4웨이브 시련 우두머리 매 웨이브 1마리·1초 선행과 엄격한 < 조건, " +
                "최근 3웨이브 36/45 포함·35/45 미포함 경계, " +
                "80% 이상 생존 시 다음 웨이브 기본 수량 2배 요청, " +
                "시계 방향·화면 테두리 위치 계산과 실제 지시 위치 생성, " +
                "활성 상한 도달 시 안전한 부분 생성을 확인했습니다.");
            RequestExit(0);
        }

        private static void SetSpawnerActiveLimit(int value)
        {
            SerializedObject serializedSpawner = new SerializedObject(spawner);
            serializedSpawner.FindProperty("absoluteMaxActive").intValue = Mathf.Max(1, value);
            serializedSpawner.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void BeginPressureScenario(int nextStage)
        {
            director.ResetForNewRun();
            player.gameObject.SetActive(false);
            Time.timeScale = 20f;
            pressureKillsApplied = false;
            stage = nextStage;
            stageStartedAt = EditorApplication.timeSinceStartup;
        }

        private static void KillActiveMonsters(int count)
        {
            KmsMonster[] active = UnityEngine.Object
                .FindObjectsByType<KmsMonster>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)
                .Where(candidate => candidate.IsPrepared && !candidate.IsDead)
                .Take(count)
                .ToArray();
            Require(active.Length == count,
                $"경계 검증에 필요한 활성 몬스터 {count}마리를 찾지 못했습니다.");

            foreach (KmsMonster monster in active)
            {
                monster.TakeDamage(monster.MaxHealth + 1f);
            }
        }

        private static void VerifyChunkReuseAndCoverage()
        {
            Require(scroller.ActiveChunkCount == 9 &&
                scroller.RuntimeChunkCreationCount == initialChunkCreationCount,
                "플레이어 이동 중 청크를 추가 생성하거나 제거했습니다.");
            Require(scroller.CoversWorldPosition(player.transform.position),
                "재배치된 3×3 청크가 플레이어 위치를 덮지 못했습니다.");
        }

        private static void MovePlayer(Vector2 position)
        {
            playerBody.position = position;
            player.transform.position = position;
        }

        private static void WaitForStageFrame()
        {
            if (EditorApplication.timeSinceStartup - stageStartedAt < 0.15d)
            {
                throw new VerificationPendingException();
            }
        }

        private static void EnsureStageTimeout(double seconds, string message)
        {
            if (EditorApplication.timeSinceStartup - stageStartedAt > seconds)
            {
                throw new InvalidOperationException(message);
            }
        }

        private static void FinishWithFailure(Exception exception)
        {
            if (exception is VerificationPendingException)
            {
                return;
            }

            Debug.LogException(exception);
            RequestExit(1);
        }

        private static void RequestExit(int exitCode)
        {
            Time.timeScale = 1f;
            if (player != null)
            {
                player.gameObject.SetActive(true);
            }

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

        private sealed class VerificationPendingException : Exception
        {
        }
    }
}
