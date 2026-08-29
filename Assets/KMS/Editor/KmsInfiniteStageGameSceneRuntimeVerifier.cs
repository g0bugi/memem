using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace KMS.Editor
{
    [InitializeOnLoad]
    public static class KmsInfiniteStageGameSceneRuntimeVerifier
    {
        private const string ScenePath = "Assets/Scenes/GameScene.unity";
        private const string NormalDataPath =
            "Assets/KMS/Monsters/Data/KmsMeleeNormalData.asset";
        private const string RunningKey = "KMS.GameInfiniteStageVerification.Running";
        private const string FinishingKey = "KMS.GameInfiniteStageVerification.Finishing";
        private const string ExitCodeKey = "KMS.GameInfiniteStageVerification.ExitCode";

        private static int stage;
        private static int initialChunkCreationCount;
        private static double stageStartedAt;

        private static KmsInfiniteStageScroller scroller;
        private static KmsMonsterSpawner spawner;
        private static KmsWaveDirector director;
        private static KmsRunTimer timer;
        private static PlayerStats player;
        private static Rigidbody2D playerBody;
        private static Camera mainCamera;

        static KmsInfiniteStageGameSceneRuntimeVerifier()
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
                        BeginVerification();
                        break;
                    case 1:
                        VerifyHorizontalRecycle();
                        break;
                    case 2:
                        VerifyVerticalRecycle();
                        break;
                    case 3:
                        VerifyFarCornerAndSpawn();
                        break;
                    case 4:
                        VerifyFirstWaveAfterReset();
                        break;
                }
            }
            catch (Exception exception)
            {
                FinishWithFailure(exception);
            }
        }

        private static void BeginVerification()
        {
            if (EditorApplication.timeSinceStartup - stageStartedAt < 0.25d)
            {
                return;
            }

            Require(SceneManager.GetActiveScene().path == ScenePath,
                "GameScene 무한맵 검증이 올바른 Scene에서 실행되지 않았습니다.");

            scroller = UnityEngine.Object.FindFirstObjectByType<KmsInfiniteStageScroller>();
            spawner = UnityEngine.Object.FindFirstObjectByType<KmsMonsterSpawner>();
            director = UnityEngine.Object.FindFirstObjectByType<KmsWaveDirector>();
            timer = UnityEngine.Object.FindFirstObjectByType<KmsRunTimer>();
            player = UnityEngine.Object.FindFirstObjectByType<PlayerStats>();
            mainCamera = Camera.main;

            Require(scroller != null && scroller.IsInitialized,
                "GameScene에서 무한 스테이지 청크가 초기화되지 않았습니다.");
            Require(spawner != null && director != null && timer != null,
                "GameScene 무한맵 검증에 필요한 Spawner, WaveDirector 또는 Timer가 없습니다.");
            Require(player != null && mainCamera != null,
                "GameScene 무한맵 검증에 필요한 Player 또는 Main Camera가 없습니다.");

            playerBody = player.GetComponent<Rigidbody2D>();
            Require(playerBody != null, "GameScene Player에 Rigidbody2D가 없습니다.");
            Require(scroller.ActiveChunkCount == 9 &&
                scroller.RuntimeChunkCreationCount == 9 &&
                scroller.ChunkSize == KmsInfiniteStageScroller.DefaultChunkSize &&
                scroller.GridSize == KmsInfiniteStageScroller.DefaultGridSize,
                "GameScene 무한 스테이지는 20×20 청크 9개를 3×3으로 생성해야 합니다.");
            Require(Mathf.Approximately(
                    spawner.MinimumSpawnRadius,
                    KmsMonsterSpawner.DefaultInnerSpawnRadius) &&
                Mathf.Approximately(
                    spawner.MaximumSpawnRadius,
                    KmsMonsterSpawner.DefaultOuterSpawnRadius) &&
                spawner.AbsoluteMaxActive == KmsMonsterSpawner.DefaultMaximumActive,
                "GameScene 몬스터 생성 반경은 12~24, 전체 활성 상한은 600이어야 합니다.");

            SpriteRenderer[] visibleFloorChunks = scroller
                .GetComponentsInChildren<SpriteRenderer>(true)
                .Where(renderer => renderer.enabled)
                .ToArray();
            Require(visibleFloorChunks.Length == 9 &&
                visibleFloorChunks.All(renderer => ApproximatelyColor(
                    renderer.color,
                    KmsInfiniteStageGameSceneConfigurator.LightGreenFloorColor)),
                "GameScene 런타임 바닥 청크 9개가 모두 지정된 연두색이어야 합니다.");

            initialChunkCreationCount = scroller.RuntimeChunkCreationCount;
            Time.timeScale = 0f;
            MovePlayer(new Vector2(21.25f, 0f));
            AdvanceStage(1);
        }

        private static void VerifyHorizontalRecycle()
        {
            if (!HasStageSettled())
            {
                return;
            }

            Require(scroller.CenterChunkCoordinate == new Vector2Int(1, 0),
                "GameScene에서 오른쪽 청크 경계를 넘을 때 바닥이 재배치되지 않았습니다.");
            VerifyChunkReuseCoverageAndCamera();

            MovePlayer(new Vector2(21.25f, -21.25f));
            AdvanceStage(2);
        }

        private static void VerifyVerticalRecycle()
        {
            if (!HasStageSettled())
            {
                return;
            }

            Require(scroller.CenterChunkCoordinate == new Vector2Int(1, -1),
                "GameScene에서 세로 청크 경계를 넘을 때 바닥이 재배치되지 않았습니다.");
            VerifyChunkReuseCoverageAndCamera();

            MovePlayer(new Vector2(41.25f, 41.25f));
            AdvanceStage(3);
        }

        private static void VerifyFarCornerAndSpawn()
        {
            if (!HasStageSettled())
            {
                return;
            }

            Require(scroller.CenterChunkCoordinate == new Vector2Int(2, 2),
                "GameScene에서 먼 대각선 좌표로 이동할 때 양축 청크가 재배치되지 않았습니다.");
            Require(scroller.RepositionCount >= 3,
                "GameScene 가로·세로·대각선 이동에서 청크 재배치가 모두 발생하지 않았습니다.");
            VerifyChunkReuseCoverageAndCamera();

            spawner.DespawnAll();
            KmsMonsterData normal = AssetDatabase.LoadAssetAtPath<KmsMonsterData>(NormalDataPath);
            Require(normal != null, "GameScene 무경계 스폰 검증용 MonsterData가 없습니다.");
            Require(spawner.TrySpawn(normal),
                "GameScene의 기존 유한 필드 밖에서 몬스터 생성에 실패했습니다.");

            KmsMonster spawned = UnityEngine.Object
                .FindObjectsByType<KmsMonster>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)
                .FirstOrDefault(candidate => candidate.IsPrepared && candidate.Data == normal);
            Require(spawned != null, "GameScene 무경계 스폰으로 생성된 몬스터를 찾지 못했습니다.");
            float spawnDistance = Vector2.Distance(
                spawned.transform.position,
                player.transform.position);
            Require(spawnDistance >= 12f - 0.01f && spawnDistance <= 24f + 0.01f,
                $"GameScene 몬스터가 12~24 생성 반경을 벗어났습니다: {spawnDistance:0.00}");
            Require(Mathf.Abs(spawned.transform.position.x) > 9f ||
                Mathf.Abs(spawned.transform.position.y) > 5.5f,
                "GameScene 무경계 스폰 검증 몬스터가 기존 유한 필드 내부에 생성됐습니다.");

            spawner.DespawnAll();
            MovePlayer(Vector2.zero);
            player.gameObject.SetActive(false);
            director.ResetForNewRun();
            Time.timeScale = 20f;
            AdvanceStage(4);
        }

        private static void VerifyFirstWaveAfterReset()
        {
            if (EditorApplication.timeSinceStartup - stageStartedAt > 6d)
            {
                throw new InvalidOperationException(
                    "GameScene 무한맵에서 초기화 후 첫 웨이브가 제한 시간 안에 생성되지 않았습니다.");
            }

            if (director.CurrentWaveNumber < 1)
            {
                return;
            }

            KmsWaveSpawnResult firstWave = director.LastWaveResult;
            Require(firstWave != null && firstWave.WaveNumber == 1 &&
                firstWave.RequestedMonsterCount == 15 &&
                firstWave.SuccessfulSpawnCount == 15,
                "GameScene 무한맵의 첫 웨이브가 Normal 15마리 전부를 생성하지 못했습니다.");
            Require(Mathf.Approximately(timer.DurationSeconds, 600f) && !timer.HasEnded,
                "GameScene 무한맵 적용 뒤 600초 런 타이머가 정상 동작하지 않습니다.");

            director.ResetForNewRun();
            player.gameObject.SetActive(true);
            Time.timeScale = 1f;
            Debug.Log(
                "[KMS] GameScene 무한 스테이지 Play Mode 검증 통과: 연두색 20×20 청크 3×3, " +
                "9개 청크 재사용, 연속 좌표 가로·세로·대각선 이동과 카메라 추적, " +
                "기존 유한 필드 밖 12~24 스폰, 600 상한과 첫 Normal 15마리 웨이브를 확인했습니다.");
            RequestExit(0);
        }

        private static void VerifyChunkReuseCoverageAndCamera()
        {
            Require(scroller.ActiveChunkCount == 9 &&
                scroller.RuntimeChunkCreationCount == initialChunkCreationCount,
                "GameScene 플레이어 이동 중 바닥 청크를 추가 생성하거나 제거했습니다.");
            Require(scroller.CoversWorldPosition(player.transform.position),
                "GameScene 재배치 바닥이 현재 플레이어 위치를 덮지 못했습니다.");
            Require(Mathf.Abs(mainCamera.transform.position.x - player.transform.position.x) < 0.05f &&
                Mathf.Abs(mainCamera.transform.position.y - player.transform.position.y) < 0.05f,
                "GameScene 카메라가 연속 좌표로 이동한 플레이어를 중앙 추적하지 못했습니다.");

            Require(mainCamera.orthographic,
                "GameScene 무한 스테이지 검증은 직교 카메라를 전제로 합니다.");
            float halfHeight = mainCamera.orthographicSize;
            float halfWidth = halfHeight * mainCamera.aspect;
            Vector3 cameraPosition = mainCamera.transform.position;
            Vector3[] viewportCorners =
            {
                cameraPosition + new Vector3(-halfWidth, -halfHeight, 0f),
                cameraPosition + new Vector3(-halfWidth, halfHeight, 0f),
                cameraPosition + new Vector3(halfWidth, -halfHeight, 0f),
                cameraPosition + new Vector3(halfWidth, halfHeight, 0f)
            };
            Require(viewportCorners.All(scroller.CoversWorldPosition),
                "GameScene 재배치 바닥이 카메라 뷰포트 네 모서리를 모두 덮지 못했습니다.");
        }

        private static void MovePlayer(Vector2 position)
        {
            playerBody.position = position;
            player.transform.position = position;
        }

        private static void AdvanceStage(int nextStage)
        {
            stage = nextStage;
            stageStartedAt = EditorApplication.timeSinceStartup;
        }

        private static bool HasStageSettled()
        {
            return EditorApplication.timeSinceStartup - stageStartedAt >= 0.15d;
        }

        private static bool ApproximatelyColor(Color left, Color right)
        {
            return Mathf.Approximately(left.r, right.r) &&
                Mathf.Approximately(left.g, right.g) &&
                Mathf.Approximately(left.b, right.b) &&
                Mathf.Approximately(left.a, right.a);
        }

        private static void FinishWithFailure(Exception exception)
        {
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
    }
}
