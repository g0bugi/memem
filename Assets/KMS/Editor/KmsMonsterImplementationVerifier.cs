using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace KMS.Editor
{
    public static class KmsMonsterImplementationVerifier
    {
        private const string ScenePath = "Assets/KMS/TestScene_KMS.unity";
        private const string GameScenePath = "Assets/Scenes/GameScene.unity";
        private const string NormalDataPath = "Assets/KMS/Monsters/Data/KmsMeleeNormalData.asset";
        private const string FastDataPath = "Assets/KMS/Monsters/Data/KmsMeleeFastData.asset";
        private const string TankDataPath = "Assets/KMS/Monsters/Data/KmsMeleeTankData.asset";
        private const string BossDataPath = "Assets/KMS/Monsters/Data/KmsMeleeBossData.asset";
        private const string RangedDataPath = "Assets/KMS/Monsters/Data/KmsRangedNormalData.asset";
        private const string SchedulePath =
            "Assets/KMS/Monsters/Waves/KmsMonsterTestWaveSchedule.asset";
        private const string SwingClipPath =
            "Assets/KMS/Monsters/Animations/KmsGoblinMeleeSwing.anim";
        private const float GoblinOneCalibratedVisualScale = 0.13364035f;
        private const float GoblinTwoCalibratedVisualScale = 0.1608969f;
        private const float GoblinThreeCalibratedVisualScale = 0.09388336f;
        private const float GoblinBossCalibratedVisualScale = 0.15368852f;
        private const float WitchCalibratedVisualScale = 0.12314649f;

        [MenuItem("KMS/Verify Monster Wave Assets")]
        public static void VerifyAssets()
        {
            KmsMonsterData normal = LoadRequired<KmsMonsterData>(NormalDataPath);
            KmsMonsterData fast = LoadRequired<KmsMonsterData>(FastDataPath);
            KmsMonsterData tank = LoadRequired<KmsMonsterData>(TankDataPath);
            KmsMonsterData boss = LoadRequired<KmsMonsterData>(BossDataPath);
            KmsMonsterData ranged = LoadRequired<KmsMonsterData>(RangedDataPath);
            KmsWaveScheduleData schedule = LoadRequired<KmsWaveScheduleData>(SchedulePath);
            AnimationClip swingClip = LoadRequired<AnimationClip>(SwingClipPath);

            ValidateData(normal);
            ValidateData(fast);
            ValidateData(tank);
            ValidateData(boss);
            ValidateData(ranged);

            Require(normal.Prefab == fast.Prefab && fast.Prefab == tank.Prefab &&
                tank.Prefab == boss.Prefab,
                "일반·속도·탱커·우두머리 SO가 하나의 공용 근거리 프리팹을 참조해야 합니다.");
            Require(ranged.Prefab != normal.Prefab,
                "원거리 몬스터는 발사 위치를 가진 원거리 프리팹을 사용해야 합니다.");
            Require(ranged.ProjectilePrefab != null,
                "원거리 몬스터 투사체 프리팹이 필요합니다.");
            Require(normal.Sprite != null && normal.Sprite.name == "Goblin_1_Body",
                "일반 근거리는 다리가 분리된 Goblin_1 본체 스프라이트를 사용해야 합니다.");
            ValidateSeparatedLegData(normal, "Goblin_1_Leg", "Goblin_1_Leg2");
            Require(Mathf.Approximately(normal.VisualScale, GoblinOneCalibratedVisualScale),
                "일반 근거리의 표시 높이는 주인공 대비 0.8 보정값이어야 합니다.");
            Require(normal.MeleeWeaponSprite != null &&
                normal.MeleeWeaponSprite.name == "Goblin_1_1" &&
                Mathf.Approximately(normal.MeleeWeaponScale, 0.7f) &&
                !normal.MeleeWeaponFlipX,
                "일반 근거리는 Goblin_1_1 몽둥이를 0.7 배율로 사용해야 합니다.");
            Require(Mathf.Approximately(normal.AttackRange, 0.04f) &&
                normal.UsesAnimatedMeleeAttack,
                "일반 근거리의 공격 여유 거리는 0.04이고 애니메이션 공격을 사용해야 합니다.");
            Require(tank.Sprite != null && tank.Sprite.name == "Goblin_2_Body",
                "탱커형 근거리는 다리가 분리된 Goblin_2 본체 스프라이트를 사용해야 합니다.");
            ValidateSeparatedLegData(tank, "Goblin_2_Leg", "Goblin_2_Leg2");
            Require(Mathf.Approximately(tank.VisualScale, GoblinTwoCalibratedVisualScale),
                "탱커형 근거리의 표시 높이는 주인공 대비 0.8 보정값이어야 합니다.");
            Require(tank.MeleeWeaponSprite != null &&
                tank.MeleeWeaponSprite.name == "Goblin_2_1" &&
                Mathf.Approximately(tank.MeleeWeaponScale, 1f) &&
                !tank.MeleeWeaponFlipX,
                "탱커형 근거리는 Goblin_2_1 몽둥이를 1.0 배율로 사용해야 합니다.");
            Require(Mathf.Approximately(tank.AttackRange, 0.04f) &&
                tank.UsesAnimatedMeleeAttack,
                "탱커형 근거리의 공격 여유 거리는 0.04이고 애니메이션 공격을 사용해야 합니다.");
            Require(fast.Sprite != null && fast.Sprite.name == "Goblin_3_Body",
                "속도형 근거리는 다리가 분리된 Goblin_3 본체 스프라이트를 사용해야 합니다.");
            ValidateSeparatedLegData(fast, "Goblin_3_Leg", "Goblin_3_Leg2");
            Require(Mathf.Approximately(fast.Sprite.rect.width, 703f) &&
                Mathf.Approximately(fast.Sprite.rect.height, 735f),
                "Goblin_3 본체는 겹친 몽둥이를 제외한 전용 크롭을 사용해야 합니다.");
            Require(Mathf.Approximately(fast.VisualScale, GoblinThreeCalibratedVisualScale),
                "속도형 근거리의 표시 높이는 주인공 대비 0.66 보정값이어야 합니다.");
            Require(fast.MeleeWeaponSprite != null &&
                fast.MeleeWeaponSprite.name == "Goblin_3_1" &&
                Mathf.Approximately(fast.MeleeWeaponScale, 1f) &&
                fast.MeleeWeaponFlipX,
                "속도형 근거리는 Goblin_3_1 몽둥이를 1.0 배율로 X 반전해 사용해야 합니다.");
            Require(Mathf.Approximately(fast.AttackRange, 0.04f) &&
                fast.UsesAnimatedMeleeAttack,
                "속도형 근거리의 공격 여유 거리는 0.04이고 애니메이션 공격을 사용해야 합니다.");
            Require(boss.Sprite != null && boss.Sprite.name == "Goblin_Boss_Body",
                "우두머리는 다리가 분리된 Goblin_Boss 본체 스프라이트를 사용해야 합니다.");
            ValidateSeparatedLegData(boss, "Goblin_Boss_Leg", "Goblin_Boss_Leg2");
            Require(Mathf.Approximately(boss.VisualScale, GoblinBossCalibratedVisualScale),
                "우두머리의 표시 높이는 주인공 대비 1.5 보정값이어야 합니다.");
            Require(boss.MeleeWeaponSprite != null &&
                boss.MeleeWeaponSprite.name == "Goblin_Boss_5" &&
                Mathf.Approximately(boss.MeleeWeaponScale, 1f) &&
                !boss.MeleeWeaponFlipX,
                "우두머리는 Goblin_Boss_5 도끼를 원본 배율과 방향으로 사용해야 합니다.");
            Require(boss.BehaviorType == KmsMonsterBehaviorType.ChaseContact &&
                Mathf.Approximately(boss.MaxHealth, normal.MaxHealth * 4f) &&
                Mathf.Approximately(boss.AttackDamage, normal.AttackDamage * 2f) &&
                Mathf.Approximately(boss.MoveSpeed, normal.MoveSpeed) &&
                Mathf.Approximately(boss.AttackCooldown, normal.AttackCooldown) &&
                Mathf.Approximately(boss.AttackRange, normal.AttackRange) &&
                boss.UsesAnimatedMeleeAttack,
                "우두머리는 일반 근거리 대비 체력 4배·공격력 2배·동일 이동속도와 도끼 애니메이션 공격을 사용해야 합니다.");
            Require(ranged.Sprite != null && ranged.Sprite.name == "Witch_Body" &&
                ranged.MeleeWeaponSprite == null,
                "일반 원거리는 다리가 분리된 Witch 본체를 사용해야 합니다.");
            ValidateSeparatedLegData(ranged, "Witch_Leg", "Witch_Leg2");
            Require(Mathf.Approximately(ranged.VisualScale, WitchCalibratedVisualScale),
                "일반 원거리의 표시 높이는 주인공 대비 0.8 보정값이어야 합니다.");
            ValidateSwingClip(swingClip);

            Require(Mathf.Approximately(schedule.FirstWaveDelaySeconds, 3f),
                "첫 웨이브는 런 시작 3초 뒤에 생성돼야 합니다.");
            Require(Mathf.Approximately(schedule.WaveIntervalSeconds, 10f),
                "웨이브 간격은 10초여야 합니다.");
            int[] boundaryWaves = { 1, 10, 11, 20, 21, 30, 31, 40, 41, 50, 51, 60 };
            int[] boundaryCounts = { 15, 15, 20, 20, 40, 40, 65, 65, 80, 80, 100, 100 };
            Require(schedule.WavesPerPhase == 10 && schedule.MaximumWaveNumber == 60,
                "웨이브 스케줄은 10웨이브씩 6세트, 총 60웨이브여야 합니다.");
            for (int index = 0; index < boundaryWaves.Length; index++)
            {
                int waveNumber = boundaryWaves[index];
                int baseCount = boundaryCounts[index];
                Require(schedule.GetBaseMonsterCount(waveNumber) == baseCount &&
                    schedule.GetPlannedMonsterCount(waveNumber, false) == baseCount &&
                    schedule.GetPlannedMonsterCount(waveNumber, true) == baseCount * 2,
                    $"{waveNumber}웨이브는 기본 {baseCount}, 처치 압박 {baseCount * 2}마리를 요청해야 합니다.");
            }
            Require(schedule.GetBaseMonsterCount(61) == 0,
                "60웨이브 뒤에는 추가 일반 웨이브 계획이 없어야 합니다.");
            Require(schedule.UnderperformanceWindowWaveCount == 3 &&
                Mathf.Approximately(schedule.UnderperformanceSurvivorRatio, 0.8f),
                "처치 부진은 직전 3개 웨이브의 생존율 80%를 기준으로 해야 합니다.");
            Require(schedule.TrialEvaluationStartWave == 3,
                "시련 조건은 3웨이브 생성 직전부터 검사해야 합니다.");
            Require(schedule.TrialBossData == boss &&
                Mathf.Approximately(schedule.TrialBossLeadSeconds, 1f),
                "시련 웨이브는 Goblin Boss를 일반 몬스터보다 1초 먼저 생성해야 합니다.");
            Require(schedule.Monsters != null && schedule.Monsters.Count == 4 &&
                schedule.Monsters.Contains(normal) &&
                schedule.Monsters.Contains(fast) &&
                schedule.Monsters.Contains(tank) &&
                schedule.Monsters.Contains(ranged) &&
                !schedule.Monsters.Contains(boss),
                "웨이브 무작위 풀은 일반 네 종류만 포함하고 우두머리는 분리해야 합니다.");
            Require(schedule.FirstAvailableWaves.SequenceEqual(new[] { 1, 3, 12, 5 }),
                "Normal/Fast/Tank/Ranged의 최초 등장 웨이브는 1/3/12/5여야 합니다.");

            Require(schedule.TryCreateWavePlan(1, false, 101, out KmsWavePlan firstWavePlan) &&
                firstWavePlan.RequestedMonsterCount == 15 &&
                firstWavePlan.MonsterRequests.All(candidate => candidate == normal),
                "1웨이브는 Normal 15마리만 계획해야 합니다.");
            Require(schedule.TryCreateWavePlan(3, false, 103, out KmsWavePlan thirdWavePlan) &&
                thirdWavePlan.MonsterRequests.All(candidate => candidate == normal || candidate == fast),
                "3웨이브는 Normal과 새로 해금된 Fast만 편성할 수 있습니다.");
            Require(schedule.TryCreateWavePlan(5, false, 105, out KmsWavePlan fifthWavePlan) &&
                fifthWavePlan.RequestedMonsterCount == 15 &&
                fifthWavePlan.MonsterRequests.Count(candidate => candidate == ranged) == 2 &&
                fifthWavePlan.MonsterRequests.All(
                    candidate => candidate == normal || candidate == fast || candidate == ranged),
                "5웨이브는 Ranged Normal 정확히 2마리와 Normal/Fast로 15마리를 채워야 합니다.");
            Require(schedule.TryCreateWavePlan(5, true, 205, out KmsWavePlan pressuredFifthWavePlan) &&
                pressuredFifthWavePlan.RequestedMonsterCount == 30 &&
                pressuredFifthWavePlan.MonsterRequests.Count(candidate => candidate == ranged) == 2,
                "5웨이브가 2배 요청이어도 Ranged Normal 고정 수량은 정확히 2마리여야 합니다.");
            ValidateExclusiveWavePlans(
                schedule,
                normal,
                new[] { 1, 2, 11, 21, 31, 41 },
                "Normal");
            ValidateExclusiveWavePlans(schedule, fast, new[] { 7, 33 }, "Fast");
            ValidateExclusiveWavePlans(schedule, ranged, new[] { 20, 49 }, "Ranged Normal");
            ValidateExclusiveWavePlans(schedule, tank, new[] { 15, 39, 40 }, "Tank");

            for (int waveNumber = 1; waveNumber <= schedule.MaximumWaveNumber; waveNumber++)
            {
                Require(schedule.TryCreateWavePlan(
                        waveNumber,
                        false,
                        1000 + waveNumber,
                        out KmsWavePlan completePlan) &&
                    completePlan.RequestedMonsterCount == schedule.GetBaseMonsterCount(waveNumber) &&
                    completePlan.MonsterRequests.All(candidate => candidate != null && candidate != boss),
                    $"{waveNumber}웨이브 계획은 기본 요청 수를 일반 몬스터로 정확히 채워야 합니다.");
            }
            Require(schedule.TryCreateWavePlan(31, true, 131, out KmsWavePlan pressurePlan) &&
                pressurePlan.BaseMonsterCount == 65 && pressurePlan.RequestedMonsterCount == 130 &&
                pressurePlan.MonsterRequests.All(candidate => candidate == normal),
                "31웨이브 처치 압박은 Normal 전용 편성을 유지하며 65→130마리를 요청해야 합니다.");
            Require(schedule.TryCreateWavePlan(49, false, 149, out KmsWavePlan rangedOnlyPlan) &&
                rangedOnlyPlan.RequestedMonsterCount == 80 &&
                rangedOnlyPlan.MonsterRequests.All(candidate => candidate == ranged),
                "49웨이브는 Ranged Normal 80마리만 계획해야 합니다.");
            Require(schedule.TryCreateWavePlan(51, false, 151, out KmsWavePlan latePlan) &&
                latePlan.RequestedMonsterCount == 100 &&
                latePlan.MonsterRequests.All(candidate => candidate != null && candidate != boss),
                "51웨이브 이후 무작위 편성은 일반 몬스터 100마리이며 Boss를 포함하면 안 됩니다.");
            Require(schedule.DirectedSpawnPatternStartWave == 21 &&
                schedule.SelectSpawnPattern(20, 0.9d) == KmsWaveSpawnPattern.RandomAnnulus &&
                schedule.SelectSpawnPattern(21, 0d) == KmsWaveSpawnPattern.RandomAnnulus &&
                schedule.SelectSpawnPattern(21, 0.34d) == KmsWaveSpawnPattern.Clockwise &&
                schedule.SelectSpawnPattern(21, 0.67d) == KmsWaveSpawnPattern.ScreenPerimeter &&
                schedule.ClockwiseSpawnDurationSeconds > 0f &&
                schedule.ClockwiseSpawnDurationSeconds <= 0.5f,
                "1~20웨이브는 기존 생성, 21웨이브부터 세 패턴과 최대 0.5초 시계 방향 생성을 사용해야 합니다.");

            Require(!KmsWaveDirector.MeetsDeathPressureCondition(0, 0, 0.8f),
                "실제 생성 성공 수가 0이면 처치 부진 상태에 진입하면 안 됩니다.");
            Require(!KmsWaveDirector.MeetsDeathPressureCondition(90, 71, 0.8f) &&
                KmsWaveDirector.MeetsDeathPressureCondition(90, 72, 0.8f),
                "처치 부진 생존율은 80% 이상 경계를 포함해야 합니다.");
            Require(!KmsWaveDirector.MeetsTrialCondition(2, 3, 0, 30) &&
                KmsWaveDirector.MeetsTrialCondition(3, 3, 29, 30) &&
                !KmsWaveDirector.MeetsTrialCondition(3, 3, 30, 30),
                "시련은 3웨이브부터 현재 활성 수가 다음 요청 수보다 엄격히 작을 때만 감지해야 합니다.");
            Require(
                KmsWaveDirector.CalculateDisplayedWaveNumber(0f, 3f, 10f, 60) == 1 &&
                KmsWaveDirector.CalculateDisplayedWaveNumber(12.99f, 3f, 10f, 60) == 1 &&
                KmsWaveDirector.CalculateDisplayedWaveNumber(13f, 3f, 10f, 60) == 2 &&
                KmsWaveDirector.CalculateDisplayedWaveNumber(593f, 3f, 10f, 60) == 60,
                "웨이브 표시는 첫 생성 전 1을 보여주고 이후 10초마다 60까지 전환돼야 합니다.");
            Require(
                Mathf.Approximately(
                    KmsWaveDirector.CalculateWaveRemainingNormalized(
                        0f, 600f, 3f, 10f, 60),
                    1f) &&
                Mathf.Approximately(
                    KmsWaveDirector.CalculateWaveRemainingNormalized(
                        1.5f, 600f, 3f, 10f, 60),
                    1f) &&
                Mathf.Approximately(
                    KmsWaveDirector.CalculateWaveRemainingNormalized(
                        3f, 600f, 3f, 10f, 60),
                    1f) &&
                Mathf.Approximately(
                    KmsWaveDirector.CalculateWaveRemainingNormalized(
                        8f, 600f, 3f, 10f, 60),
                    0.5f) &&
                Mathf.Approximately(
                    KmsWaveDirector.CalculateWaveRemainingNormalized(
                        13f, 600f, 3f, 10f, 60),
                    1f) &&
                Mathf.Approximately(
                    KmsWaveDirector.CalculateWaveRemainingNormalized(
                        600f, 600f, 3f, 10f, 60),
                    0f),
                "웨이브 막대는 첫 3초 동안 가득 찬 채 멈추고, 이후 감소·재충전되며 600초에 비어야 합니다.");
            Require(
                Mathf.Approximately(KmsWaveDirector.CalculateWaveHealthMultiplier(1, 10), 1f) &&
                Mathf.Approximately(KmsWaveDirector.CalculateWaveHealthMultiplier(10, 10), 1f) &&
                Mathf.Approximately(KmsWaveDirector.CalculateWaveHealthMultiplier(11, 10), 2f) &&
                Mathf.Approximately(KmsWaveDirector.CalculateWaveHealthMultiplier(21, 10), 3f) &&
                Mathf.Approximately(KmsWaveDirector.CalculateWaveHealthMultiplier(51, 10), 6f) &&
                Mathf.Approximately(KmsWaveDirector.CalculateWaveHealthMultiplier(60, 10), 6f),
                "몬스터 체력 배율은 1·11·21·31·41·51웨이브에서 1~6배로 증가해야 합니다.");

            ValidatePrefab(normal.Prefab);
            ValidatePrefab(ranged.Prefab);
            ValidateProjectilePrefab(ranged.ProjectilePrefab);
            ValidateScene();
            ValidateGameScene(schedule);

            Debug.Log("[KMS] 몬스터 SO·공용 프리팹·풀·웨이브 테스트 씬·GameScene 정적 검증을 통과했습니다.");
        }

        private static void ValidateExclusiveWavePlans(
            KmsWaveScheduleData schedule,
            KmsMonsterData expectedMonster,
            int[] waveNumbers,
            string label)
        {
            foreach (int waveNumber in waveNumbers)
            {
                int baseCount = schedule.GetBaseMonsterCount(waveNumber);
                Require(schedule.TryCreateWavePlan(
                        waveNumber,
                        false,
                        waveNumber,
                        out KmsWavePlan basePlan) &&
                    basePlan.RequestedMonsterCount == baseCount &&
                    basePlan.MonsterRequests.All(candidate => candidate == expectedMonster),
                    $"{waveNumber}웨이브는 {label} 전용 기본 편성을 유지해야 합니다.");
                Require(schedule.TryCreateWavePlan(
                        waveNumber,
                        true,
                        100 + waveNumber,
                        out KmsWavePlan doubledPlan) &&
                    doubledPlan.RequestedMonsterCount == baseCount * 2 &&
                    doubledPlan.MonsterRequests.All(candidate => candidate == expectedMonster),
                    $"{waveNumber}웨이브는 2배 요청에서도 {label} 전용 편성을 유지해야 합니다.");
            }
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

        private static void ValidateSeparatedLegData(
            KmsMonsterData data,
            string expectedLegName,
            string expectedLeg2Name)
        {
            Require(data.UsesSeparatedLegs &&
                data.LegSprite.name == expectedLegName &&
                data.Leg2Sprite.name == expectedLeg2Name,
                $"{data.name}: 분리된 Leg/Leg2 스프라이트가 필요합니다.");
            Require(Mathf.Approximately(data.LegSwingAmplitude, 0.08f) &&
                Mathf.Approximately(data.LegSwingSpeed, 8f) &&
                Mathf.Approximately(data.LegReturnSpeed, 10f),
                $"{data.name}: 플레이어와 같은 다리 스윙 0.08/8/10 설정이 필요합니다.");
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
            Require(serializedMonster.FindProperty("legSwing").objectReferenceValue != null,
                $"{prefab.name}: 분리 다리 스윙 참조가 필요합니다.");
            KmsMonsterLegSwing legSwing = prefab.GetComponent<KmsMonsterLegSwing>();
            Require(legSwing != null, $"{prefab.name}: KmsMonsterLegSwing이 필요합니다.");
            SerializedObject serializedLegSwing = new SerializedObject(legSwing);
            Require(serializedLegSwing.FindProperty("visualRoot").objectReferenceValue != null &&
                serializedLegSwing.FindProperty("legRenderer").objectReferenceValue != null &&
                serializedLegSwing.FindProperty("leg2Renderer").objectReferenceValue != null,
                $"{prefab.name}: Visual/Leg/Leg2 참조가 모두 필요합니다.");

            if (prefab.name == "KmsMeleeMonster")
            {
                Require(serializedMonster.FindProperty("meleeWeaponPivot").objectReferenceValue != null,
                    "근거리 프리팹에 몽둥이 회전축 참조가 필요합니다.");
                Require(serializedMonster.FindProperty("meleeWeaponRenderer").objectReferenceValue != null,
                    "근거리 프리팹에 몽둥이 Renderer 참조가 필요합니다.");
                Animator animator = prefab.GetComponent<Animator>();
                Require(animator != null && animator.runtimeAnimatorController != null,
                    "근거리 프리팹에 공격 Animator Controller가 필요합니다.");
            }
        }

        private static void ValidateSwingClip(AnimationClip clip)
        {
            AnimationEvent[] events = AnimationUtility.GetAnimationEvents(clip);
            Require(events.Count(animationEvent =>
                    animationEvent.functionName == nameof(KmsMonster.ApplyAnimatedMeleeDamage)) == 1,
                "몽둥이 공격 Clip에는 피해 Animation Event가 정확히 하나 필요합니다.");
            Require(events.Count(animationEvent =>
                    animationEvent.functionName == nameof(KmsMonster.CompleteAnimatedMeleeAttack)) == 1,
                "몽둥이 공격 Clip에는 종료 Animation Event가 정확히 하나 필요합니다.");
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
                Require(FindSceneComponents<KmsInfiniteStageScroller>(scene).Length == 1,
                    "TestScene_KMS에는 KmsInfiniteStageScroller가 정확히 하나 필요합니다.");

                KmsMonsterSpawner spawner = FindSceneComponents<KmsMonsterSpawner>(scene)[0];
                SerializedObject serializedSpawner = new SerializedObject(spawner);
                SerializedProperty knownData = serializedSpawner.FindProperty("knownMonsterData");
                Require(knownData != null && knownData.arraySize == 4,
                    "Spawner에는 테스트 MonsterData 네 종류가 필요합니다.");
                Require(serializedSpawner.FindProperty("playerTarget").objectReferenceValue != null,
                    "Spawner의 Player 타깃 참조가 필요합니다.");
                Require(serializedSpawner.FindProperty("spawnArea").objectReferenceValue == null,
                    "무한 스테이지 Spawner는 고정 유효 생성 영역을 사용하면 안 됩니다.");
                Require(Mathf.Approximately(
                        serializedSpawner.FindProperty("innerSpawnRadius").floatValue,
                        KmsMonsterSpawner.DefaultInnerSpawnRadius) &&
                    Mathf.Approximately(
                        serializedSpawner.FindProperty("outerSpawnRadius").floatValue,
                        KmsMonsterSpawner.DefaultOuterSpawnRadius),
                    "TestScene_KMS의 몬스터 생성 반경은 플레이어 기준 12~24여야 합니다.");
                Require(serializedSpawner.FindProperty("positionAttemptCount").intValue == 64,
                    "무경계 대규모 웨이브 검증을 위해 생성 위치 시도 횟수는 64여야 합니다.");
                Require(!serializedSpawner.FindProperty("spawnOnStart").boolValue,
                    "WaveDirector와 초기 자동 스폰을 동시에 사용하면 안 됩니다.");
                int absoluteMaxActive =
                    serializedSpawner.FindProperty("absoluteMaxActive").intValue;
                int hardCapacityPerPrefab =
                    serializedSpawner.FindProperty("hardCapacityPerPrefab").intValue;
                Require(absoluteMaxActive == KmsMonsterSpawner.DefaultMaximumActive,
                    "TestScene_KMS의 전체 활성 몬스터 제한은 600이어야 합니다.");
                Require(hardCapacityPerPrefab >= absoluteMaxActive,
                    "프리팹별 풀 제한이 전체 활성 제한 600보다 작으면 안 됩니다.");

                KmsInfiniteStageScroller scroller =
                    FindSceneComponents<KmsInfiniteStageScroller>(scene)[0];
                ValidatePhaseHud(
                    scene,
                    FindSceneComponents<KmsWaveDirector>(scene)[0],
                    "TestScene_KMS");
                SerializedObject serializedScroller = new SerializedObject(scroller);
                Vector2 chunkSize = serializedScroller.FindProperty("chunkSize").vector2Value;
                Vector2Int gridSize = serializedScroller.FindProperty("gridSize").vector2IntValue;
                Require(serializedScroller.FindProperty("playerTarget").objectReferenceValue != null &&
                    serializedScroller.FindProperty("floorTemplate").objectReferenceValue != null,
                    "무한 스테이지에 Player와 FloorTemplate 참조가 필요합니다.");
                Require(chunkSize == KmsInfiniteStageScroller.DefaultChunkSize &&
                    gridSize == KmsInfiniteStageScroller.DefaultGridSize,
                    "TestScene_KMS 무한 스테이지는 20×20 청크를 3×3으로 유지해야 합니다.");

                string[] removedBoundaryNames =
                {
                    "TopBoundary",
                    "BottomBoundary",
                    "LeftBoundary",
                    "RightBoundary",
                    "SpawnArea"
                };
                foreach (string boundaryName in removedBoundaryNames)
                {
                    Require(scene.GetRootGameObjects()
                        .SelectMany(root => root.GetComponentsInChildren<Transform>(true))
                        .All(candidate => candidate.name != boundaryName),
                        $"무한 스테이지에는 유한 필드 오브젝트가 남으면 안 됩니다: {boundaryName}");
                }
            }
            finally
            {
                if (closeAfterValidation && scene.IsValid() && scene.isLoaded)
                {
                    EditorSceneManager.CloseScene(scene, true);
                }
            }
        }

        private static void ValidateGameScene(KmsWaveScheduleData expectedSchedule)
        {
            Scene scene = SceneManager.GetSceneByPath(GameScenePath);
            bool closeAfterValidation = !scene.IsValid() || !scene.isLoaded;
            if (closeAfterValidation)
            {
                scene = EditorSceneManager.OpenScene(GameScenePath, OpenSceneMode.Additive);
            }

            try
            {
                KmsMonsterSpawner[] spawners = FindSceneComponents<KmsMonsterSpawner>(scene);
                KmsWaveDirector[] directors = FindSceneComponents<KmsWaveDirector>(scene);
                KmsRunTimer[] timers = FindSceneComponents<KmsRunTimer>(scene);
                KmsInfiniteStageScroller[] scrollers =
                    FindSceneComponents<KmsInfiniteStageScroller>(scene);
                PlayerStats[] players = FindSceneComponents<PlayerStats>(scene);
                CameraFollow2D[] cameraFollowers = FindSceneComponents<CameraFollow2D>(scene);

                Require(spawners.Length == 1,
                    "GameScene에는 KmsMonsterSpawner가 정확히 하나 필요합니다.");
                Require(directors.Length == 1,
                    "GameScene에는 KmsWaveDirector가 정확히 하나 필요합니다.");
                Require(timers.Length == 1,
                    "GameScene에는 KmsRunTimer가 정확히 하나 필요합니다.");
                Require(scrollers.Length == 1,
                    "GameScene에는 KmsInfiniteStageScroller가 정확히 하나 필요합니다.");
                Require(players.Length == 1,
                    "GameScene에는 PlayerStats가 정확히 하나 필요합니다.");
                Require(cameraFollowers.Length == 1,
                    "GameScene에는 CameraFollow2D가 정확히 하나 필요합니다.");

                SerializedObject serializedSpawner = new SerializedObject(spawners[0]);
                int absoluteMaxActive =
                    serializedSpawner.FindProperty("absoluteMaxActive").intValue;
                int hardCapacityPerPrefab =
                    serializedSpawner.FindProperty("hardCapacityPerPrefab").intValue;
                Require(absoluteMaxActive == KmsMonsterSpawner.DefaultMaximumActive,
                    "GameScene의 전체 활성 몬스터 제한은 600이어야 합니다.");
                Require(hardCapacityPerPrefab >= absoluteMaxActive,
                    "GameScene의 프리팹별 풀 제한이 전체 활성 제한 600보다 작으면 안 됩니다.");
                Require(!serializedSpawner.FindProperty("spawnOnStart").boolValue,
                    "GameScene에서는 WaveDirector와 초기 자동 스폰을 동시에 사용하면 안 됩니다.");
                Require(serializedSpawner.FindProperty("spawnArea").objectReferenceValue == null,
                    "GameScene 무한 스테이지 Spawner는 고정 생성 영역을 사용하면 안 됩니다.");
                Require(Mathf.Approximately(
                        serializedSpawner.FindProperty("innerSpawnRadius").floatValue,
                        KmsMonsterSpawner.DefaultInnerSpawnRadius) &&
                    Mathf.Approximately(
                        serializedSpawner.FindProperty("outerSpawnRadius").floatValue,
                        KmsMonsterSpawner.DefaultOuterSpawnRadius) &&
                    serializedSpawner.FindProperty("positionAttemptCount").intValue == 64,
                    "GameScene 무경계 스폰은 플레이어 기준 12~24 반경과 위치 시도 64회를 사용해야 합니다.");

                SerializedObject serializedScroller = new SerializedObject(scrollers[0]);
                SpriteRenderer floorTemplate = serializedScroller
                    .FindProperty("floorTemplate").objectReferenceValue as SpriteRenderer;
                UnityEngine.Object spawnerPlayerTarget =
                    serializedSpawner.FindProperty("playerTarget").objectReferenceValue;
                UnityEngine.Object scrollerPlayerTarget =
                    serializedScroller.FindProperty("playerTarget").objectReferenceValue;
                Require(scrollers[0].gameObject.name == "GameField" &&
                    scrollerPlayerTarget == players[0].transform &&
                    spawnerPlayerTarget == players[0].transform &&
                    floorTemplate != null && floorTemplate.sprite != null,
                    "GameScene 무한 스테이지와 Spawner가 동일한 Player 및 FloorTemplate을 참조해야 합니다.");
                Require(scrollers[0].transform.parent == null &&
                    scrollers[0].transform.position == Vector3.zero &&
                    scrollers[0].transform.rotation == Quaternion.identity &&
                    scrollers[0].transform.localScale == Vector3.one &&
                    scrollers[0].GetComponentsInChildren<Collider2D>(true).Length == 0,
                    "GameScene GameField는 원점·단위 스케일의 루트이며 Collider가 없어야 합니다.");
                Require(serializedScroller.FindProperty("chunkSize").vector2Value ==
                        KmsInfiniteStageScroller.DefaultChunkSize &&
                    serializedScroller.FindProperty("gridSize").vector2IntValue ==
                        KmsInfiniteStageScroller.DefaultGridSize,
                    "GameScene 무한 스테이지는 20×20 청크를 3×3으로 사용해야 합니다.");
                Require(ApproximatelyColor(
                        floorTemplate.color,
                        KmsInfiniteStageGameSceneConfigurator.LightGreenFloorColor) &&
                    floorTemplate.sortingOrder == -20,
                    "GameScene 무한 스테이지 바닥은 지정된 연두색과 정렬 순서 -20을 사용해야 합니다.");

                SerializedObject serializedCameraFollow =
                    new SerializedObject(cameraFollowers[0]);
                Require(serializedCameraFollow.FindProperty("target").objectReferenceValue ==
                        players[0].transform,
                    "GameScene 카메라는 무한 스테이지와 동일한 Player를 추적해야 합니다.");

                string[] removedFiniteFieldNames =
                {
                    "TopBoundary",
                    "BottomBoundary",
                    "LeftBoundary",
                    "RightBoundary",
                    "SpawnArea"
                };
                foreach (string objectName in removedFiniteFieldNames)
                {
                    Require(scene.GetRootGameObjects()
                        .SelectMany(root => root.GetComponentsInChildren<Transform>(true))
                        .All(candidate => candidate.name != objectName),
                        $"GameScene 무한 스테이지에 유한 필드 오브젝트가 남아 있습니다: {objectName}");
                }

                Require(Mathf.Approximately(timers[0].DurationSeconds, 600f),
                    "GameScene의 런 제한 시간은 600초(10분)여야 합니다.");

                SerializedObject serializedDirector = new SerializedObject(directors[0]);
                Require(serializedDirector.FindProperty("schedule").objectReferenceValue == expectedSchedule,
                    "GameScene의 WaveDirector는 현재 KMS 웨이브 스케줄을 참조해야 합니다.");
                Require(serializedDirector.FindProperty("spawner").objectReferenceValue == spawners[0],
                    "GameScene의 WaveDirector는 Scene의 KmsMonsterSpawner를 참조해야 합니다.");
                Require(serializedDirector.FindProperty("runTimer").objectReferenceValue == timers[0],
                    "GameScene의 WaveDirector는 10분 KmsRunTimer를 참조해야 합니다.");
                ValidatePhaseHud(scene, directors[0], "GameScene");
            }
            finally
            {
                if (closeAfterValidation && scene.IsValid() && scene.isLoaded)
                {
                    EditorSceneManager.CloseScene(scene, true);
                }
            }
        }

        private static void ValidatePhaseHud(
            Scene scene,
            KmsWaveDirector expectedDirector,
            string sceneLabel)
        {
            KmsPhaseHud[] phaseHuds = FindSceneComponents<KmsPhaseHud>(scene);
            Require(phaseHuds.Length == 1,
                $"{sceneLabel}에는 KmsPhaseHud가 정확히 하나 필요합니다.");

            KmsPhaseHud phaseHud = phaseHuds[0];
            SerializedObject serializedHud = new SerializedObject(phaseHud);
            Text phaseText = serializedHud.FindProperty("phaseText").objectReferenceValue as Text;
            Image remainingFill =
                serializedHud.FindProperty("remainingFill").objectReferenceValue as Image;
            Require(phaseHud.gameObject.name == "WaveHud" &&
                serializedHud.FindProperty("waveDirector").objectReferenceValue == expectedDirector,
                $"{sceneLabel} 웨이브 HUD는 Scene의 WaveDirector를 참조해야 합니다.");
            Require(phaseText != null && phaseText.text == "WAVE 1" &&
                phaseText.alignment == TextAnchor.MiddleCenter,
                $"{sceneLabel} 웨이브 HUD에는 중앙 정렬된 WAVE 숫자 텍스트가 필요합니다.");
            Require(remainingFill != null &&
                remainingFill.sprite == null &&
                remainingFill.type == Image.Type.Simple,
                $"{sceneLabel} 웨이브 HUD 막대는 변형 없는 단색 Simple Image여야 합니다.");
            RectTransform fillRect = remainingFill.rectTransform;
            Require(fillRect.anchorMin == new Vector2(0f, 0.5f) &&
                fillRect.anchorMax == new Vector2(0f, 0.5f) &&
                fillRect.pivot == new Vector2(0f, 0.5f) &&
                Mathf.Approximately(fillRect.anchoredPosition.x, 2f),
                $"{sceneLabel} 웨이브 HUD 막대는 왼쪽 끝이 고정된 RectTransform이어야 합니다.");

            RectTransform rect = phaseHud.GetComponent<RectTransform>();
            Require(rect != null &&
                rect.anchorMin == new Vector2(0.5f, 1f) &&
                rect.anchorMax == new Vector2(0.5f, 1f) &&
                rect.anchoredPosition == new Vector2(0f, -106f),
                $"{sceneLabel} 웨이브 HUD는 상단 중앙 시간 표시 아래에 고정돼야 합니다.");
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

        private static bool ApproximatelyColor(Color left, Color right)
        {
            return Mathf.Approximately(left.r, right.r) &&
                Mathf.Approximately(left.g, right.g) &&
                Mathf.Approximately(left.b, right.b) &&
                Mathf.Approximately(left.a, right.a);
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
