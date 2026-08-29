using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace KMS.Editor
{
    [InitializeOnLoad]
    public static class KmsMonsterRuntimeSmokeVerifier
    {
        private const string ScenePath = "Assets/KMS/TestScene_KMS.unity";
        private const string GameScenePath = "Assets/Scenes/GameScene.unity";
        private const string RunningKey = "KMS.MonsterSmoke.Running";
        private const string FinishingKey = "KMS.MonsterSmoke.Finishing";
        private const string ExitCodeKey = "KMS.MonsterSmoke.ExitCode";
        private const string GameSceneModeKey = "KMS.MonsterSmoke.GameSceneMode";

        private const string NormalDataPath = "Assets/KMS/Monsters/Data/KmsMeleeNormalData.asset";
        private const string FastDataPath = "Assets/KMS/Monsters/Data/KmsMeleeFastData.asset";
        private const string TankDataPath = "Assets/KMS/Monsters/Data/KmsMeleeTankData.asset";
        private const string BossDataPath = "Assets/KMS/Monsters/Data/KmsMeleeBossData.asset";
        private const string RangedDataPath = "Assets/KMS/Monsters/Data/KmsRangedNormalData.asset";

        private static double enteredPlayModeAt;
        private static double legMotionSpawnedAt;
        private static double meleeSpawnedAt;
        private static double rangedSpawnedAt;
        private static double projectileObservedAt;
        private static double adaptiveVerificationStartedAt;
        private static int stage;
        private static int launchCountBeforeRanged;
        private static float playerHealthBeforeMelee;
        private static KmsMonster trackedMeleeMonster;
        private static KmsMonsterData trackedMeleeData;
        private static KmsMonsterLegSwing trackedLegSwing;
        private static KmsMonsterProjectile trackedProjectile;
        private static Vector3 projectileStartPosition;
        private static Vector2 expectedProjectileDirection;
        private static PlayerStats adaptiveVerificationPlayer;

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
            SessionState.EraseBool(GameSceneModeKey);
            EditorApplication.playModeStateChanged -= HandlePlayModeStateChanged;
            EditorApplication.playModeStateChanged += HandlePlayModeStateChanged;

            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            EditorApplication.EnterPlaymode();
        }

        public static void RunGameSceneFromCommandLine()
        {
            SessionState.SetBool(RunningKey, true);
            SessionState.SetBool(FinishingKey, false);
            SessionState.SetInt(ExitCodeKey, 1);
            SessionState.SetBool(GameSceneModeKey, true);
            EditorApplication.playModeStateChanged -= HandlePlayModeStateChanged;
            EditorApplication.playModeStateChanged += HandlePlayModeStateChanged;

            EditorSceneManager.OpenScene(GameScenePath, OpenSceneMode.Single);
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
                SessionState.EraseBool(GameSceneModeKey);
                EditorApplication.playModeStateChanged -= HandlePlayModeStateChanged;
                EditorApplication.Exit(exitCode);
            }
        }

        private static void BeginPlayModeVerification()
        {
            stage = 0;
            trackedProjectile = null;
            adaptiveVerificationPlayer = null;
            enteredPlayModeAt = EditorApplication.timeSinceStartup;
            EditorApplication.update -= Tick;
            EditorApplication.update += Tick;
        }

        private static void Tick()
        {
            try
            {
                if (SessionState.GetBool(GameSceneModeKey, false))
                {
                    VerifyGameScenePlayMode();
                    return;
                }

                if (stage == 0)
                {
                    double elapsedSincePlayMode =
                        EditorApplication.timeSinceStartup - enteredPlayModeAt;
                    if (elapsedSincePlayMode < 3.25d)
                    {
                        return;
                    }

                    KmsWaveDirector waveDirector =
                        UnityEngine.Object.FindFirstObjectByType<KmsWaveDirector>();
                    if (waveDirector == null || waveDirector.CurrentWaveNumber < 1)
                    {
                        if (elapsedSincePlayMode < 5d)
                        {
                            return;
                        }

                        throw new InvalidOperationException(
                            "첫 웨이브가 런 시작 3초 뒤 제한 시간 안에 생성되지 않았습니다.");
                    }

                    VerifyMonsterPoolReuseAndDeath();
                    stage = 1;
                    return;
                }

                if (stage == 1)
                {
                    if (EditorApplication.timeSinceStartup - legMotionSpawnedAt < 0.2d)
                    {
                        return;
                    }

                    PlayerStats player = UnityEngine.Object.FindFirstObjectByType<PlayerStats>();
                    Require(player != null, "다리 모션 확인 중 PlayerStats를 찾을 수 없습니다.");
                    Require(trackedMeleeMonster != null &&
                        trackedMeleeMonster.gameObject.activeInHierarchy,
                        "다리 모션 확인용 Goblin Boss가 활성 상태가 아닙니다.");
                    Require(trackedMeleeMonster.GetComponent<Rigidbody2D>().linearVelocity.sqrMagnitude > 0.01f,
                        "추적 중인 Goblin_3에 실제 이동 속도가 없습니다.");
                    Require(trackedLegSwing != null && trackedLegSwing.IsSwinging,
                        "추적 중인 Goblin Boss의 분리된 다리가 교차 이동하지 않습니다.");
                    Require(Mathf.Abs(trackedLegSwing.CurrentWorldOffset) > 0.01f &&
                        Mathf.Abs(trackedLegSwing.CurrentWorldOffset) <=
                            trackedMeleeData.LegSwingAmplitude + 0.0001f,
                        "Goblin Boss 다리 스윙 오프셋이 설정된 진폭 범위를 따르지 않습니다.");
                    Require(!trackedMeleeMonster.IsFacingRight,
                        "플레이어 오른쪽에서 추적 중인 Goblin Boss는 왼쪽을 바라봐야 합니다.");
                    Transform flippedVisual = GetSerializedReference<Transform>(
                        trackedMeleeMonster,
                        "visualRoot");
                    SpriteRenderer flippedWeapon = GetSerializedReference<SpriteRenderer>(
                        trackedMeleeMonster,
                        "meleeWeaponRenderer");
                    Require(flippedVisual != null && flippedVisual.localScale.x < 0f &&
                        flippedWeapon != null && !flippedWeapon.flipX,
                        "Goblin Boss는 도끼 원본 방향을 유지한 채 Visual 전체가 왼쪽으로 반전돼야 합니다.");

                    BeginMeleeAnimationVerification(player);
                    stage = 2;
                    return;
                }

                if (stage == 2)
                {
                    if (EditorApplication.timeSinceStartup - meleeSpawnedAt < 0.15d)
                    {
                        return;
                    }

                    PlayerStats player = UnityEngine.Object.FindFirstObjectByType<PlayerStats>();
                    Require(player != null, "근거리 공격 확인 중 PlayerStats를 찾을 수 없습니다.");
                    Require(trackedMeleeMonster != null &&
                        trackedMeleeMonster.gameObject.activeInHierarchy &&
                        trackedMeleeMonster.IsMeleeAttacking,
                        "Goblin Boss가 접촉 즉시 피해를 주지 않고 도끼 공격 애니메이션을 시작해야 합니다.");
                    Require(!trackedMeleeMonster.IsFacingRight,
                        "플레이어 오른쪽에 생성된 Goblin Boss는 왼쪽을 바라봐야 합니다.");
                    Require(Mathf.Approximately(player.CurrentHealth, playerHealthBeforeMelee),
                        "몽둥이 타격 Animation Event 전에 플레이어 피해가 적용됐습니다.");
                    stage = 3;
                    return;
                }

                if (stage == 3)
                {
                    if (EditorApplication.timeSinceStartup - meleeSpawnedAt < 0.75d)
                    {
                        return;
                    }

                    PlayerStats player = UnityEngine.Object.FindFirstObjectByType<PlayerStats>();
                    Require(player != null, "근거리 타격 확인 중 PlayerStats를 찾을 수 없습니다.");
                    float expectedHealth =
                        Mathf.Max(0f, playerHealthBeforeMelee - trackedMeleeData.AttackDamage);
                    Require(Mathf.Approximately(player.CurrentHealth, expectedHealth),
                        "몽둥이 타격 Animation Event가 플레이어에게 정확히 한 번 피해를 주지 않았습니다.");
                    Require(trackedMeleeMonster != null && !trackedMeleeMonster.IsMeleeAttacking,
                        "몽둥이 공격 종료 Animation Event가 공격 상태를 끝내지 않았습니다.");

                    BeginRangedVerification(player);
                    stage = 4;
                    return;
                }

                if (stage == 4)
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
                        stage = 5;
                        return;
                    }

                    if (EditorApplication.timeSinceStartup - rangedSpawnedAt > 5d)
                    {
                        throw new InvalidOperationException(
                            "원거리 몬스터가 제한 시간 안에 투사체를 발사하지 않았습니다.");
                    }
                }

                if (stage == 5)
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
                    BeginAdaptiveWaveVerification();
                    stage = 6;
                    return;
                }

                if (stage == 6)
                {
                    KmsWaveDirector director =
                        UnityEngine.Object.FindFirstObjectByType<KmsWaveDirector>();
                    Require(director != null,
                        "적응형 웨이브 검증 중 KmsWaveDirector를 찾을 수 없습니다.");

                    if (director.CurrentWaveNumber < 5)
                    {
                        if (EditorApplication.timeSinceStartup - adaptiveVerificationStartedAt < 5d)
                        {
                            return;
                        }

                        throw new InvalidOperationException(
                            "가속 검증에서 5웨이브까지 진행되지 않았습니다.");
                    }

                    KmsWaveSpawnResult fifthWave = director.LastWaveResult;
                    Require(director.IsDeathPressureActive && fifthWave != null &&
                        fifthWave.WaveNumber == 5 &&
                        fifthWave.BaseMonsterCount == 15 &&
                        fifthWave.RequestedMonsterCount == 30 &&
                        fifthWave.IsDeathPressureActive,
                        "최근 3웨이브의 80% 이상 생존 뒤 5웨이브 기본 수량 15가 30으로 배가되어야 합니다.");
                    Require(director.LastUnderperformanceSpawnCount > 0 &&
                        director.LastUnderperformanceSurvivorCount ==
                            director.LastUnderperformanceSpawnCount &&
                        Mathf.Approximately(director.LastUnderperformanceSurvivorRatio, 1f),
                        "처치 부진 판정이 최근 3웨이브의 실제 생성 성공 수와 생존 수를 사용해야 합니다.");

                    Time.timeScale = 1f;
                    Require(adaptiveVerificationPlayer != null,
                        "적응형 웨이브 검증용 플레이어 참조가 사라졌습니다.");
                    adaptiveVerificationPlayer.gameObject.SetActive(true);
                    director.ResetForNewRun();
                    Require(director.CurrentWaveNumber == 0 &&
                        !director.IsDeathPressureActive &&
                        !director.IsTrialActive &&
                        director.LastWaveResult == null &&
                        Mathf.Approximately(director.SecondsUntilNextWave, 3f),
                        "새 런에서 웨이브 번호·적응 상태·기록과 첫 3초 대기가 초기화돼야 합니다.");

                    FinishSuccessfully();
                    return;
                }
            }
            catch (Exception exception)
            {
                FinishWithFailure(exception);
            }
        }

        private static void VerifyGameScenePlayMode()
        {
            double elapsedSincePlayMode =
                EditorApplication.timeSinceStartup - enteredPlayModeAt;
            if (elapsedSincePlayMode < 3.25d)
            {
                return;
            }

            Require(SceneManager.GetActiveScene().path == GameScenePath,
                "Play Mode 검증이 GameScene에서 실행되지 않았습니다.");

            KmsRunTimer timer = UnityEngine.Object.FindFirstObjectByType<KmsRunTimer>();
            KmsMonsterSpawner spawner =
                UnityEngine.Object.FindFirstObjectByType<KmsMonsterSpawner>();
            KmsWaveDirector director =
                UnityEngine.Object.FindFirstObjectByType<KmsWaveDirector>();

            Require(timer != null, "GameScene Play Mode에서 KmsRunTimer를 찾을 수 없습니다.");
            Require(spawner != null,
                "GameScene Play Mode에서 KmsMonsterSpawner를 찾을 수 없습니다.");
            Require(director != null,
                "GameScene Play Mode에서 KmsWaveDirector를 찾을 수 없습니다.");
            Require(Mathf.Approximately(timer.DurationSeconds, 600f) &&
                !timer.HasEnded && timer.RemainingSeconds > 590f,
                "GameScene의 10분 타이머가 Play Mode에서 정상 진행되지 않습니다.");
            Require(spawner.AbsoluteMaxActive == KmsMonsterSpawner.DefaultMaximumActive,
                "GameScene의 전체 활성 몬스터 제한이 Play Mode에서 600이 아닙니다.");

            if (director.CurrentWaveNumber < 1)
            {
                if (elapsedSincePlayMode < 5d)
                {
                    return;
                }

                throw new InvalidOperationException(
                    "GameScene에서 첫 웨이브가 런 시작 3초 뒤 제한 시간 안에 생성되지 않았습니다.");
            }

            KmsWaveSpawnResult firstWave = director.LastWaveResult;
            Require(firstWave != null && firstWave.WaveNumber == 1 &&
                firstWave.RequestedMonsterCount == 15 &&
                firstWave.SuccessfulSpawnCount > 0,
                "GameScene의 첫 웨이브가 Normal 15마리 요청과 실제 성공 수를 기록하지 않았습니다.");

            Debug.Log(
                "[KMS] GameScene Play Mode 스모크 통과: 10분 타이머, 전체 활성 제한 600, " +
                "3초 뒤 첫 Normal 15마리 웨이브와 실제 생성 성공 기록을 확인했습니다.");
            RequestExit(0);
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
            KmsWaveSpawnResult firstWave = director.LastWaveResult;
            Require(director.CurrentWaveNumber == 1 && firstWave != null &&
                firstWave.WaveNumber == 1 && firstWave.RequestedMonsterCount == 15 &&
                firstWave.SuccessfulSpawnCount > 0 &&
                firstWave.SuccessfulSpawnCount == spawner.SpawnedCount,
                "첫 웨이브가 Normal 15마리 요청과 실제 생성 성공 수를 정확히 기록하지 않았습니다.");
            Require(spawner.TotalPooledInstanceCount > 0,
                "몬스터 풀이 사전 생성되지 않았습니다.");

            director.enabled = false;
            spawner.DespawnAll();
            Require(spawner.ActiveCount == 0, "웨이브 몬스터 일괄 회수에 실패했습니다.");

            KmsMonsterData normal = LoadRequired<KmsMonsterData>(NormalDataPath);
            KmsMonsterData fast = LoadRequired<KmsMonsterData>(FastDataPath);
            KmsMonsterData tank = LoadRequired<KmsMonsterData>(TankDataPath);
            KmsMonsterData boss = LoadRequired<KmsMonsterData>(BossDataPath);
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
            Require(fastInstance.IsFacingRight,
                "풀에서 생성된 Goblin_3의 기본 방향은 오른쪽이어야 합니다.");
            Transform fastVisual = GetSerializedReference<Transform>(fastInstance, "visualRoot");
            SpriteRenderer fastWeapon = GetSerializedReference<SpriteRenderer>(
                fastInstance,
                "meleeWeaponRenderer");
            Require(fastVisual != null && fastVisual.localScale.x > 0f &&
                fastWeapon != null && fastWeapon.flipX,
                "오른쪽 기본 방향의 Goblin_3에 전용 몽둥이 X 반전이 적용돼야 합니다.");

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

            Require(spawner.TrySpawnAt(boss, testPosition),
                "우두머리 돌격형의 공용 근거리 풀 생성에 실패했습니다.");
            KmsMonster bossInstance = FindActiveMonster(boss);
            Require(bossInstance.GetInstanceID() == sharedInstanceId,
                "우두머리도 기존 근거리 몬스터와 동일한 풀 인스턴스를 재사용해야 합니다.");
            Require(Mathf.Approximately(bossInstance.CurrentHealth, boss.MaxHealth) &&
                Mathf.Approximately(boss.MaxHealth, normal.MaxHealth * 4f) &&
                Mathf.Approximately(boss.AttackDamage, normal.AttackDamage * 2f) &&
                Mathf.Approximately(boss.MoveSpeed, normal.MoveSpeed),
                "우두머리의 체력·공격력·이동속도 비율이 일반 근거리 기준과 다릅니다.");
            Transform bossVisual = GetSerializedReference<Transform>(bossInstance, "visualRoot");
            SpriteRenderer bossWeapon = GetSerializedReference<SpriteRenderer>(
                bossInstance,
                "meleeWeaponRenderer");
            Require(bossInstance.IsFacingRight && bossVisual != null &&
                bossVisual.localScale.x > 0f &&
                Mathf.Approximately(Mathf.Abs(bossVisual.localScale.y), boss.VisualScale) &&
                bossWeapon != null && bossWeapon.sprite == boss.MeleeWeaponSprite &&
                !bossWeapon.flipX,
                "우두머리는 플레이어 대비 1.5 크기 보정과 오른쪽 기본 도끼 방향을 사용해야 합니다.");

            timer.EndRun();
            Require(timer.HasEnded,
                "테스트 런 종료 상태 진입에 실패했습니다.");
            Time.timeScale = 0f;
            director.ResetForNewRun();
            Require(!timer.HasEnded && Mathf.Approximately(Time.timeScale, 1f) &&
                Mathf.Approximately(timer.ElapsedSeconds, 0f),
                "WaveDirector의 새 런 초기화가 타이머와 timeScale을 복구하지 못했습니다.");

            trackedMeleeData = boss;
            Vector3 legMotionPosition = player.transform.position + new Vector3(2f, 0f, 0f);
            Require(spawner.TrySpawnAt(boss, legMotionPosition),
                "다리 모션 확인용 Goblin Boss 생성에 실패했습니다.");
            trackedMeleeMonster = FindActiveMonster(boss);
            trackedLegSwing = trackedMeleeMonster.GetComponent<KmsMonsterLegSwing>();
            legMotionSpawnedAt = EditorApplication.timeSinceStartup;
        }

        private static void BeginMeleeAnimationVerification(PlayerStats player)
        {
            KmsMonsterSpawner spawner = UnityEngine.Object.FindFirstObjectByType<KmsMonsterSpawner>();
            Require(spawner != null, "근거리 공격 검증 중 KmsMonsterSpawner를 찾을 수 없습니다.");

            spawner.DespawnAll();
            playerHealthBeforeMelee = player.CurrentHealth;
            Vector3 meleePosition = player.transform.position + new Vector3(0.2f, 0f, 0f);
            Require(spawner.TrySpawnAt(trackedMeleeData, meleePosition),
                "Animation Event 확인용 Goblin Boss 생성에 실패했습니다.");
            trackedMeleeMonster = FindActiveMonster(trackedMeleeData);
            meleeSpawnedAt = EditorApplication.timeSinceStartup;
        }

        private static void BeginRangedVerification(PlayerStats player)
        {
            KmsMonsterSpawner spawner = UnityEngine.Object.FindFirstObjectByType<KmsMonsterSpawner>();
            KmsMonsterProjectilePool projectilePool =
                UnityEngine.Object.FindFirstObjectByType<KmsMonsterProjectilePool>();
            Require(spawner != null, "원거리 검증 중 KmsMonsterSpawner를 찾을 수 없습니다.");
            Require(projectilePool != null, "원거리 검증 중 투사체 풀을 찾을 수 없습니다.");

            spawner.DespawnAll();
            KmsMonsterData ranged = LoadRequired<KmsMonsterData>(RangedDataPath);
            Vector3 rangedPosition = player.transform.position + new Vector3(5.5f, 0f, 0f);
            launchCountBeforeRanged = projectilePool.TotalLaunchCount;
            Require(spawner.TrySpawnAt(ranged, rangedPosition), "원거리 몬스터 생성에 실패했습니다.");
            KmsMonster rangedMonster = FindActiveMonster(ranged);
            Require(rangedMonster.Data == ranged,
                "원거리 몬스터에 선택된 SO가 적용되지 않았습니다.");
            rangedSpawnedAt = EditorApplication.timeSinceStartup;
        }

        private static void BeginAdaptiveWaveVerification()
        {
            KmsWaveDirector director =
                UnityEngine.Object.FindFirstObjectByType<KmsWaveDirector>();
            adaptiveVerificationPlayer =
                UnityEngine.Object.FindFirstObjectByType<PlayerStats>();
            Require(director != null,
                "적응형 웨이브 검증을 시작할 KmsWaveDirector가 없습니다.");
            Require(adaptiveVerificationPlayer != null,
                "적응형 웨이브 검증을 시작할 PlayerStats가 없습니다.");

            director.enabled = true;
            director.ResetForNewRun();
            adaptiveVerificationPlayer.gameObject.SetActive(false);
            Time.timeScale = 20f;
            adaptiveVerificationStartedAt = EditorApplication.timeSinceStartup;
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

        private static T GetSerializedReference<T>(KmsMonster monster, string propertyName)
            where T : UnityEngine.Object
        {
            SerializedProperty property = new SerializedObject(monster).FindProperty(propertyName);
            return property != null ? property.objectReferenceValue as T : null;
        }

        private static void FinishSuccessfully()
        {
            Debug.Log(
                "[KMS] Play Mode 스모크 통과: 웨이브 생성, 근거리 SO 공용 풀 재사용, " +
                "외부 비활성화 회수, 사망 데이터·드롭 전달, 런 재시작, " +
                "Goblin Boss 1.5 크기·기본 오른쪽 방향·왼쪽 반전·분리 다리 교차 이동·" +
                "도끼 Animation Event 단일 피해, " +
                "원거리 투사체 발사·이동·회수, 최근 3웨이브 생존 추적, " +
                "다음 웨이브 기본 수량 2배 처치 압박과 새 런 초기화를 확인했습니다.");
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
