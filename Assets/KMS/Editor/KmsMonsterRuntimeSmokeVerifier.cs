using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace KMS.Editor
{
    [InitializeOnLoad]
    public static class KmsMonsterRuntimeSmokeVerifier
    {
        private const string ScenePath = "Assets/KMS/TestScene_KMS.unity";
        private const string RunningKey = "KMS.MonsterSmoke.Running";
        private const string FinishingKey = "KMS.MonsterSmoke.Finishing";
        private const string ExitCodeKey = "KMS.MonsterSmoke.ExitCode";

        private const string NormalDataPath = "Assets/KMS/Monsters/Data/KmsMeleeNormalData.asset";
        private const string FastDataPath = "Assets/KMS/Monsters/Data/KmsMeleeFastData.asset";
        private const string TankDataPath = "Assets/KMS/Monsters/Data/KmsMeleeTankData.asset";
        private const string RangedDataPath = "Assets/KMS/Monsters/Data/KmsRangedNormalData.asset";

        private static double enteredPlayModeAt;
        private static double rangedSpawnedAt;
        private static double projectileObservedAt;
        private static int stage;
        private static int launchCountBeforeRanged;
        private static KmsMonsterProjectile trackedProjectile;
        private static Vector3 projectileStartPosition;
        private static Vector2 expectedProjectileDirection;

        static KmsMonsterRuntimeSmokeVerifier()
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
            trackedProjectile = null;
            enteredPlayModeAt = EditorApplication.timeSinceStartup;
            EditorApplication.update -= Tick;
            EditorApplication.update += Tick;
        }

        private static void Tick()
        {
            try
            {
                if (stage == 0)
                {
                    if (EditorApplication.timeSinceStartup - enteredPlayModeAt < 3d)
                    {
                        return;
                    }

                    VerifyMonsterPoolReuseAndDeath();
                    stage = 1;
                    rangedSpawnedAt = EditorApplication.timeSinceStartup;
                    return;
                }

                if (stage == 1)
                {
                    KmsMonsterProjectilePool projectilePool =
                        UnityEngine.Object.FindFirstObjectByType<KmsMonsterProjectilePool>();
                    Require(projectilePool != null, "Play Mode에서 적 투사체 풀을 찾을 수 없습니다.");

                    if (projectilePool.TotalLaunchCount > launchCountBeforeRanged)
                    {
                        trackedProjectile = UnityEngine.Object
                            .FindObjectsByType<KmsMonsterProjectile>(
                                FindObjectsInactive.Exclude,
                                FindObjectsSortMode.None)
                            .FirstOrDefault(candidate => candidate.IsActiveProjectile);
                        Require(trackedProjectile != null,
                            "발사 집계 뒤 활성 적 투사체를 찾을 수 없습니다.");

                        Rigidbody2D projectileBody = trackedProjectile.GetComponent<Rigidbody2D>();
                        Require(projectileBody != null && projectileBody.linearVelocity.sqrMagnitude > 0.01f,
                            "활성화된 적 투사체에 발사 속도가 적용되지 않았습니다.");
                        projectileStartPosition = trackedProjectile.transform.position;
                        expectedProjectileDirection = projectileBody.linearVelocity.normalized;
                        projectileObservedAt = EditorApplication.timeSinceStartup;
                        stage = 2;
                        return;
                    }

                    if (EditorApplication.timeSinceStartup - rangedSpawnedAt > 5d)
                    {
                        throw new InvalidOperationException(
                            "원거리 몬스터가 제한 시간 안에 투사체를 발사하지 않았습니다.");
                    }
                }

                if (stage == 2)
                {
                    if (EditorApplication.timeSinceStartup - projectileObservedAt < 0.15d)
                    {
                        return;
                    }

                    Require(trackedProjectile != null && trackedProjectile.gameObject.activeInHierarchy,
                        "이동을 확인하기 전에 적 투사체가 비활성화됐습니다.");
                    Vector2 displacement =
                        trackedProjectile.transform.position - projectileStartPosition;
                    Require(displacement.magnitude >= 0.25f,
                        $"적 투사체가 실제로 이동하지 않았습니다. 이동 거리: {displacement.magnitude:0.000}");
                    Require(Vector2.Dot(displacement.normalized, expectedProjectileDirection) >= 0.9f,
                        "적 투사체가 발사 방향과 다른 방향으로 이동했습니다.");

                    KmsMonsterProjectilePool projectilePool =
                        UnityEngine.Object.FindFirstObjectByType<KmsMonsterProjectilePool>();
                    Require(projectilePool != null, "Play Mode에서 적 투사체 풀을 찾을 수 없습니다.");
                    trackedProjectile.gameObject.SetActive(false);
                    Require(projectilePool.ActiveCount == 0,
                        "외부 비활성화된 적 투사체가 활성 풀 집계에 남았습니다.");

                    KmsMonsterSpawner spawner =
                        UnityEngine.Object.FindFirstObjectByType<KmsMonsterSpawner>();
                    spawner?.DespawnAll();
                    Require(projectilePool.ActiveCount == 0,
                        "DespawnAll 후 활성 적 투사체가 남았습니다.");
                    FinishSuccessfully();
                    return;
                }
            }
            catch (Exception exception)
            {
                FinishWithFailure(exception);
            }
        }

        private static void VerifyMonsterPoolReuseAndDeath()
        {
            KmsMonsterSpawner spawner = UnityEngine.Object.FindFirstObjectByType<KmsMonsterSpawner>();
            KmsWaveDirector director = UnityEngine.Object.FindFirstObjectByType<KmsWaveDirector>();
            KmsMonsterProjectilePool projectilePool =
                UnityEngine.Object.FindFirstObjectByType<KmsMonsterProjectilePool>();
            PlayerStats player = UnityEngine.Object.FindFirstObjectByType<PlayerStats>();
            KmsRunTimer timer = UnityEngine.Object.FindFirstObjectByType<KmsRunTimer>();
            KmsGoldDropController goldDrops =
                UnityEngine.Object.FindFirstObjectByType<KmsGoldDropController>();

            Require(spawner != null, "Play Mode에서 KmsMonsterSpawner를 찾을 수 없습니다.");
            Require(director != null, "Play Mode에서 KmsWaveDirector를 찾을 수 없습니다.");
            Require(projectilePool != null, "Play Mode에서 KmsMonsterProjectilePool을 찾을 수 없습니다.");
            Require(player != null, "Play Mode에서 PlayerStats를 찾을 수 없습니다.");
            Require(timer != null, "Play Mode에서 KmsRunTimer를 찾을 수 없습니다.");
            Require(goldDrops != null, "Play Mode에서 KmsGoldDropController를 찾을 수 없습니다.");
            Require(spawner.SpawnedCount > 0 && spawner.ActiveCount > 0,
                "WaveDirector가 첫 몬스터를 생성하지 않았습니다.");
            Require(spawner.TotalPooledInstanceCount > 0,
                "몬스터 풀이 사전 생성되지 않았습니다.");

            director.enabled = false;
            spawner.DespawnAll();
            Require(spawner.ActiveCount == 0, "웨이브 몬스터 일괄 회수에 실패했습니다.");

            KmsMonsterData normal = LoadRequired<KmsMonsterData>(NormalDataPath);
            KmsMonsterData fast = LoadRequired<KmsMonsterData>(FastDataPath);
            KmsMonsterData tank = LoadRequired<KmsMonsterData>(TankDataPath);
            KmsMonsterData ranged = LoadRequired<KmsMonsterData>(RangedDataPath);
            Vector3 testPosition = player.transform.position + new Vector3(2f, 0f, 0f);

            Require(spawner.TrySpawnAt(normal, testPosition), "일반 근거리 풀 생성에 실패했습니다.");
            KmsMonster normalInstance = FindActiveMonster(normal);
            int sharedInstanceId = normalInstance.GetInstanceID();
            Require(Mathf.Approximately(normalInstance.CurrentHealth, normal.MaxHealth),
                "일반 근거리 체력 초기화가 올바르지 않습니다.");

            spawner.DespawnAll();
            Require(spawner.TrySpawnAt(fast, testPosition), "속도형 근거리 풀 재사용에 실패했습니다.");
            KmsMonster fastInstance = FindActiveMonster(fast);
            Require(fastInstance.GetInstanceID() == sharedInstanceId,
                "같은 근거리 프리팹이 동일 풀 인스턴스를 재사용하지 않았습니다.");
            Require(Mathf.Approximately(fastInstance.CurrentHealth, fast.MaxHealth),
                "속도형 근거리 체력이 이전 SO 값에서 초기화되지 않았습니다.");

            fastInstance.gameObject.SetActive(false);
            Require(spawner.ActiveCount == 0,
                "외부 비활성화된 몬스터가 활성 목록에 남았습니다.");
            Require(spawner.TrySpawnAt(tank, testPosition), "탱커형 근거리 풀 재사용에 실패했습니다.");
            KmsMonster tankInstance = FindActiveMonster(tank);
            Require(tankInstance.GetInstanceID() == sharedInstanceId,
                "탱커형 근거리도 공용 근거리 풀을 사용해야 합니다.");
            Require(Mathf.Approximately(tankInstance.CurrentHealth, tank.MaxHealth),
                "탱커형 근거리 체력이 이전 SO 값에서 초기화되지 않았습니다.");

            int goldBeforeDeath = goldDrops.TotalSpawnedPickupCount;
            KmsMonsterData dataObservedByDirectDeathSubscriber = null;
            bool activeObservedByDirectDeathSubscriber = false;
            tankInstance.Died += monster =>
            {
                dataObservedByDirectDeathSubscriber = monster.Data;
                activeObservedByDirectDeathSubscriber = monster.gameObject.activeInHierarchy;
            };
            tankInstance.TakeDamage(tank.MaxHealth + 1f);
            Require(spawner.ActiveCount == 0, "사망한 몬스터가 활성 목록에 남았습니다.");
            Require(dataObservedByDirectDeathSubscriber == tank && activeObservedByDirectDeathSubscriber,
                "직접 Died 구독자가 풀 반환 전의 유효한 몬스터 상태를 받아야 합니다.");
            Require(goldDrops.TotalSpawnedPickupCount > goldBeforeDeath,
                "몬스터 사망 이벤트가 골드 드롭으로 정확히 전달되지 않았습니다.");

            timer.EndRun();
            Require(timer.HasEnded && Mathf.Approximately(Time.timeScale, 0f),
                "테스트 런 종료 상태 진입에 실패했습니다.");
            director.ResetForNewRun();
            Require(!timer.HasEnded && Mathf.Approximately(Time.timeScale, 1f) &&
                Mathf.Approximately(timer.ElapsedSeconds, 0f),
                "WaveDirector의 새 런 초기화가 타이머와 timeScale을 복구하지 못했습니다.");

            Vector3 rangedPosition = player.transform.position + new Vector3(5.5f, 0f, 0f);
            launchCountBeforeRanged = projectilePool.TotalLaunchCount;
            Require(spawner.TrySpawnAt(ranged, rangedPosition), "원거리 몬스터 생성에 실패했습니다.");
            Require(FindActiveMonster(ranged).Data == ranged,
                "원거리 몬스터에 선택된 SO가 적용되지 않았습니다.");
        }

        private static KmsMonster FindActiveMonster(KmsMonsterData expectedData)
        {
            KmsMonster monster = UnityEngine.Object
                .FindObjectsByType<KmsMonster>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)
                .FirstOrDefault(candidate => candidate.IsPrepared && candidate.Data == expectedData);
            if (monster == null)
            {
                throw new InvalidOperationException($"활성 몬스터를 찾을 수 없습니다: {expectedData.name}");
            }

            return monster;
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

        private static void FinishSuccessfully()
        {
            Debug.Log(
                "[KMS] Play Mode 스모크 통과: 웨이브 생성, 근거리 SO 공용 풀 재사용, " +
                "외부 비활성화 회수, 사망 데이터·드롭 전달, 런 재시작, " +
                "원거리 투사체 발사·이동·회수를 확인했습니다.");
            RequestExit(0);
        }

        private static void FinishWithFailure(Exception exception)
        {
            Debug.LogException(exception);
            RequestExit(1);
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
