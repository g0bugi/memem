using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace KMS.Editor
{
    internal static class KmsMonsterArtSetupBuilder
    {
        private const string MonsterFolder = "Assets/KMS/Monsters";
        private const string ArtFolder = MonsterFolder + "/Art";
        private const string GeneratedArtFolder = ArtFolder + "/Generated";
        private const string AnimationFolder = MonsterFolder + "/Animations";
        private const string DataFolder = MonsterFolder + "/Data";
        private const string PrefabFolder = MonsterFolder + "/Prefabs";

        private const string GoblinOnePath = ArtFolder + "/Goblin_1.png";
        private const string GoblinTwoPath = ArtFolder + "/Goblin_2.png";
        private const string GoblinThreePath = ArtFolder + "/Goblin_3.png";
        private const string GoblinBossPath = ArtFolder + "/Goblin_Boss.png";
        private const string WitchPath = ArtFolder + "/Witch.png";
        private const string SwingClipPath = AnimationFolder + "/KmsGoblinMeleeSwing.anim";
        private const string AnimatorControllerPath =
            AnimationFolder + "/KmsGoblinMelee.controller";

        private const string MeleePrefabPath = PrefabFolder + "/KmsMeleeMonster.prefab";
        private const string RangedPrefabPath = PrefabFolder + "/KmsRangedMonster.prefab";
        private const string NormalDataPath = DataFolder + "/KmsMeleeNormalData.asset";
        private const string FastDataPath = DataFolder + "/KmsMeleeFastData.asset";
        private const string TankDataPath = DataFolder + "/KmsMeleeTankData.asset";
        private const string BossDataPath = DataFolder + "/KmsMeleeBossData.asset";
        private const string RangedDataPath = DataFolder + "/KmsRangedNormalData.asset";

        // TestScene_KMS의 현재 주인공 표시 높이를 1로 두고, 각 몬스터의 실제 화면상
        // 불투명 영역이 0.8이 되도록 스프라이트별 투명 여백 차이를 보정한 값입니다.
        private const float GoblinOneTargetHeight = 1.2869565f;
        private const float GoblinTwoTargetHeight = 1.3531429f;
        // Goblin_3 전용 703 px 가시 높이를 월드 0.66으로 맞춘 735 px 크롭 높이입니다.
        private const float GoblinThreeTargetHeight = 0.6900427f;
        // Goblin_Boss_0의 976 px 가시 높이를 플레이어 대비 1.5로 맞춘 값입니다.
        private const float GoblinBossTargetHeight = 1.5030738f;
        private const float WitchTargetHeight = 1.1588085f;

        [MenuItem("KMS/Apply Monster Art And Melee Animation")]
        public static void ApplyToExistingContent()
        {
            Directory.CreateDirectory(AnimationFolder);
            Directory.CreateDirectory(GeneratedArtFolder);
            AssetDatabase.Refresh();
            EnsureBossDataAsset();

            SeparatedArt goblinOneArt = BuildSeparatedArt(
                GoblinOnePath,
                "Goblin_1_0",
                "Goblin_1",
                new[]
                {
                    new Vector2Int(396, 691), new Vector2Int(476, 698),
                    new Vector2Int(466, 769), new Vector2Int(451, 780),
                    new Vector2Int(354, 777), new Vector2Int(338, 764),
                    new Vector2Int(355, 725), new Vector2Int(378, 707)
                },
                new[]
                {
                    new Vector2Int(570, 700), new Vector2Int(640, 694),
                    new Vector2Int(655, 725), new Vector2Int(669, 764),
                    new Vector2Int(658, 778), new Vector2Int(570, 780),
                    new Vector2Int(560, 768)
                });
            SeparatedArt goblinTwoArt = BuildSeparatedArt(
                GoblinTwoPath,
                "Goblin_2_0",
                "Goblin_2",
                new[]
                {
                    new Vector2Int(428, 696), new Vector2Int(498, 713),
                    new Vector2Int(483, 751), new Vector2Int(473, 796),
                    new Vector2Int(457, 807), new Vector2Int(365, 807),
                    new Vector2Int(353, 791), new Vector2Int(366, 757),
                    new Vector2Int(412, 743)
                },
                new[]
                {
                    new Vector2Int(601, 715), new Vector2Int(669, 696),
                    new Vector2Int(698, 737), new Vector2Int(717, 790),
                    new Vector2Int(704, 807), new Vector2Int(620, 807),
                    new Vector2Int(607, 796), new Vector2Int(598, 758)
                });
            SeparatedArt goblinThreeArt = BuildSeparatedArt(
                GoblinThreePath,
                "Goblin_3_0",
                "Goblin_3",
                new[]
                {
                    new Vector2Int(623, 824), new Vector2Int(682, 828),
                    new Vector2Int(687, 848), new Vector2Int(680, 866),
                    new Vector2Int(658, 894), new Vector2Int(657, 939),
                    new Vector2Int(557, 939), new Vector2Int(554, 923),
                    new Vector2Int(579, 891), new Vector2Int(596, 878),
                    new Vector2Int(607, 862), new Vector2Int(614, 849),
                    new Vector2Int(620, 839)
                },
                new[]
                {
                    new Vector2Int(820, 827), new Vector2Int(861, 827),
                    new Vector2Int(877, 848), new Vector2Int(876, 874),
                    new Vector2Int(900, 890), new Vector2Int(923, 923),
                    new Vector2Int(921, 939), new Vector2Int(828, 939),
                    new Vector2Int(826, 892), new Vector2Int(803, 866),
                    new Vector2Int(807, 852), new Vector2Int(815, 839)
                },
                // Goblin_3_0의 원래 Rect는 왼쪽 Goblin_3_1 몽둥이까지 겹쳐 포함합니다.
                // 몽둥이 Rect의 오른쪽 경계(x=436)부터 캐릭터만 잘라 중복과 중심 밀림을 막습니다.
                new RectInt(436, 240, 703, 735));
            SeparatedArt goblinBossArt = BuildSeparatedArt(
                GoblinBossPath,
                "Goblin_Boss_0",
                "Goblin_Boss",
                new[]
                {
                    new Vector2Int(350, 783), new Vector2Int(449, 825),
                    new Vector2Int(452, 881), new Vector2Int(451, 912),
                    new Vector2Int(282, 912), new Vector2Int(276, 895),
                    new Vector2Int(291, 853), new Vector2Int(326, 814)
                },
                new[]
                {
                    new Vector2Int(789, 780), new Vector2Int(821, 815),
                    new Vector2Int(851, 848), new Vector2Int(869, 894),
                    new Vector2Int(863, 913), new Vector2Int(680, 913),
                    new Vector2Int(673, 895), new Vector2Int(687, 847),
                    new Vector2Int(704, 820)
                });
            SeparatedArt witchArt = BuildSeparatedArt(
                WitchPath,
                "Witch_0",
                "Witch",
                new[]
                {
                    new Vector2Int(391, 811), new Vector2Int(458, 831),
                    new Vector2Int(443, 865), new Vector2Int(439, 903),
                    new Vector2Int(425, 914), new Vector2Int(333, 914),
                    new Vector2Int(316, 900), new Vector2Int(325, 860),
                    new Vector2Int(350, 841), new Vector2Int(383, 832)
                },
                new[]
                {
                    new Vector2Int(554, 833), new Vector2Int(621, 811),
                    new Vector2Int(655, 836), new Vector2Int(679, 901),
                    new Vector2Int(664, 914), new Vector2Int(580, 914),
                    new Vector2Int(566, 905)
                });

            AnimationClip swingClip = BuildOrUpdateSwingClip();
            AnimatorController controller = BuildOrUpdateAnimatorController(swingClip);
            ConfigureMeleePrefab(controller);
            ConfigureSeparatedLegPrefab(MeleePrefabPath);
            ConfigureSeparatedLegPrefab(RangedPrefabPath);

            ConfigureMonsterData(
                NormalDataPath,
                goblinOneArt.Body,
                goblinOneArt.Leg,
                goblinOneArt.Leg2,
                LoadSprite(GoblinOnePath, "Goblin_1_1"),
                targetHeight: GoblinOneTargetHeight,
                weaponScale: 0.7f,
                weaponFlipX: false,
                attackRange: 0.04f);
            ConfigureMonsterData(
                TankDataPath,
                goblinTwoArt.Body,
                goblinTwoArt.Leg,
                goblinTwoArt.Leg2,
                LoadSprite(GoblinTwoPath, "Goblin_2_1"),
                targetHeight: GoblinTwoTargetHeight,
                weaponScale: 1f,
                weaponFlipX: false,
                attackRange: 0.04f);
            ConfigureMonsterData(
                FastDataPath,
                goblinThreeArt.Body,
                goblinThreeArt.Leg,
                goblinThreeArt.Leg2,
                LoadSprite(GoblinThreePath, "Goblin_3_1"),
                targetHeight: GoblinThreeTargetHeight,
                weaponScale: 1f,
                weaponFlipX: true,
                attackRange: 0.04f);
            ConfigureMonsterData(
                BossDataPath,
                goblinBossArt.Body,
                goblinBossArt.Leg,
                goblinBossArt.Leg2,
                LoadSprite(GoblinBossPath, "Goblin_Boss_5"),
                targetHeight: GoblinBossTargetHeight,
                weaponScale: 1f,
                weaponFlipX: false,
                attackRange: 0.04f);
            ConfigureMonsterData(
                RangedDataPath,
                witchArt.Body,
                witchArt.Leg,
                witchArt.Leg2,
                weaponSprite: null,
                targetHeight: WitchTargetHeight,
                weaponScale: 1f,
                weaponFlipX: false,
                attackRange: null);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[KMS] Goblin/Boss/Witch 외형과 근거리 공격 애니메이션 연결을 완료했습니다.");
        }

        public static void ApplyFromCommandLine()
        {
            ApplyToExistingContent();
        }

        private static AnimationClip BuildOrUpdateSwingClip()
        {
            AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(SwingClipPath);
            if (clip == null)
            {
                clip = new AnimationClip();
                AssetDatabase.CreateAsset(clip, SwingClipPath);
            }

            clip.name = "KmsGoblinMeleeSwing";
            clip.frameRate = 60f;
            clip.wrapMode = WrapMode.Once;

            AnimationCurve rotationCurve = new AnimationCurve(
                new Keyframe(0f, 0f),
                new Keyframe(0.12f, 20f),
                new Keyframe(0.3f, -105f),
                new Keyframe(0.58f, 0f),
                new Keyframe(0.6f, 0f));
            for (int index = 0; index < rotationCurve.length; index++)
            {
                AnimationUtility.SetKeyLeftTangentMode(
                    rotationCurve,
                    index,
                    AnimationUtility.TangentMode.Linear);
                AnimationUtility.SetKeyRightTangentMode(
                    rotationCurve,
                    index,
                    AnimationUtility.TangentMode.Linear);
            }

            EditorCurveBinding rotationBinding = EditorCurveBinding.FloatCurve(
                "Visual/WeaponPivot",
                typeof(Transform),
                "localEulerAnglesRaw.z");
            AnimationUtility.SetEditorCurve(clip, rotationBinding, rotationCurve);
            AnimationUtility.SetAnimationEvents(
                clip,
                new[]
                {
                    new AnimationEvent
                    {
                        time = 0.3f,
                        functionName = nameof(KmsMonster.ApplyAnimatedMeleeDamage)
                    },
                    new AnimationEvent
                    {
                        time = 0.58f,
                        functionName = nameof(KmsMonster.CompleteAnimatedMeleeAttack)
                    }
                });

            SerializedObject serializedClip = new SerializedObject(clip);
            SerializedProperty loopTime =
                serializedClip.FindProperty("m_AnimationClipSettings.m_LoopTime");
            if (loopTime != null)
            {
                loopTime.boolValue = false;
                serializedClip.ApplyModifiedPropertiesWithoutUndo();
            }

            EditorUtility.SetDirty(clip);
            return clip;
        }

        private static AnimatorController BuildOrUpdateAnimatorController(AnimationClip swingClip)
        {
            AnimatorController controller =
                AssetDatabase.LoadAssetAtPath<AnimatorController>(AnimatorControllerPath);
            if (controller == null)
            {
                controller = AnimatorController.CreateAnimatorControllerAtPath(
                    AnimatorControllerPath);
            }

            AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;
            AnimatorState idle = FindState(stateMachine, "Idle") ?? stateMachine.AddState("Idle");
            AnimatorState swing =
                FindState(stateMachine, "MeleeSwing") ?? stateMachine.AddState("MeleeSwing");
            idle.motion = null;
            swing.motion = swingClip;
            swing.speed = 1f;
            stateMachine.defaultState = idle;
            EditorUtility.SetDirty(controller);
            return controller;
        }

        private static AnimatorState FindState(AnimatorStateMachine stateMachine, string name)
        {
            return stateMachine.states
                .Select(child => child.state)
                .FirstOrDefault(state => state != null && state.name == name);
        }

        private static void ConfigureMeleePrefab(RuntimeAnimatorController controller)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(MeleePrefabPath);
            if (prefab == null)
            {
                throw new InvalidOperationException($"근거리 몬스터 프리팹이 없습니다: {MeleePrefabPath}");
            }

            GameObject root = PrefabUtility.LoadPrefabContents(MeleePrefabPath);
            try
            {
                KmsMonster monster = root.GetComponent<KmsMonster>();
                if (monster == null)
                {
                    throw new InvalidOperationException("근거리 몬스터 프리팹에 KmsMonster가 없습니다.");
                }

                Transform visual = root.transform.Find("Visual");
                if (visual == null)
                {
                    throw new InvalidOperationException("근거리 몬스터 프리팹에 Visual 루트가 없습니다.");
                }

                Transform weaponPivot = EnsureChild(visual, "WeaponPivot");
                weaponPivot.localPosition = Vector3.zero;
                weaponPivot.localRotation = Quaternion.identity;
                weaponPivot.localScale = Vector3.one;

                Transform weaponVisual = EnsureChild(weaponPivot, "WeaponVisual");
                weaponVisual.localPosition = Vector3.zero;
                weaponVisual.localRotation = Quaternion.identity;
                weaponVisual.localScale = Vector3.one;
                SpriteRenderer weaponRenderer = weaponVisual.GetComponent<SpriteRenderer>();
                if (weaponRenderer == null)
                {
                    weaponRenderer = weaponVisual.gameObject.AddComponent<SpriteRenderer>();
                }

                weaponRenderer.sprite = null;
                weaponRenderer.color = Color.white;
                weaponRenderer.sortingOrder = 2;
                weaponRenderer.enabled = false;

                Animator animator = root.GetComponent<Animator>();
                if (animator == null)
                {
                    animator = root.AddComponent<Animator>();
                }

                animator.runtimeAnimatorController = controller;
                animator.applyRootMotion = false;
                animator.updateMode = AnimatorUpdateMode.Normal;
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;

                SerializedObject serializedMonster = new SerializedObject(monster);
                serializedMonster.FindProperty("meleeWeaponPivot").objectReferenceValue = weaponPivot;
                serializedMonster.FindProperty("meleeWeaponRenderer").objectReferenceValue =
                    weaponRenderer;
                serializedMonster.FindProperty("meleeAnimator").objectReferenceValue = animator;
                serializedMonster.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(root, MeleePrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void ConfigureSeparatedLegPrefab(string prefabPath)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
            {
                throw new InvalidOperationException($"몬스터 프리팹이 없습니다: {prefabPath}");
            }

            GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
            try
            {
                KmsMonster monster = root.GetComponent<KmsMonster>();
                Transform visual = root.transform.Find("Visual");
                if (monster == null || visual == null)
                {
                    throw new InvalidOperationException(
                        $"몬스터 프리팹의 KmsMonster 또는 Visual이 없습니다: {prefabPath}");
                }

                SpriteRenderer legRenderer = ConfigureLegRenderer(EnsureChild(visual, "Leg"));
                SpriteRenderer leg2Renderer = ConfigureLegRenderer(EnsureChild(visual, "Leg2"));
                KmsMonsterLegSwing legSwing = root.GetComponent<KmsMonsterLegSwing>();
                if (legSwing == null)
                {
                    legSwing = root.AddComponent<KmsMonsterLegSwing>();
                }

                SerializedObject serializedLegSwing = new SerializedObject(legSwing);
                serializedLegSwing.FindProperty("visualRoot").objectReferenceValue = visual;
                serializedLegSwing.FindProperty("legRenderer").objectReferenceValue = legRenderer;
                serializedLegSwing.FindProperty("leg2Renderer").objectReferenceValue = leg2Renderer;
                serializedLegSwing.ApplyModifiedPropertiesWithoutUndo();

                SerializedObject serializedMonster = new SerializedObject(monster);
                serializedMonster.FindProperty("legSwing").objectReferenceValue = legSwing;
                serializedMonster.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static SpriteRenderer ConfigureLegRenderer(Transform legTransform)
        {
            legTransform.localPosition = Vector3.zero;
            legTransform.localRotation = Quaternion.identity;
            legTransform.localScale = Vector3.one;
            SpriteRenderer renderer = legTransform.GetComponent<SpriteRenderer>();
            if (renderer == null)
            {
                renderer = legTransform.gameObject.AddComponent<SpriteRenderer>();
            }

            renderer.sprite = null;
            renderer.color = Color.white;
            renderer.sortingOrder = 0;
            renderer.enabled = false;
            return renderer;
        }

        private static void EnsureBossDataAsset()
        {
            GameObject meleePrefabObject = AssetDatabase.LoadAssetAtPath<GameObject>(MeleePrefabPath);
            KmsMonster meleePrefab = meleePrefabObject != null
                ? meleePrefabObject.GetComponent<KmsMonster>()
                : null;
            if (meleePrefab == null)
            {
                throw new InvalidOperationException($"근거리 몬스터 프리팹이 없습니다: {MeleePrefabPath}");
            }

            KmsMonsterData data = AssetDatabase.LoadAssetAtPath<KmsMonsterData>(BossDataPath);
            if (data == null)
            {
                data = ScriptableObject.CreateInstance<KmsMonsterData>();
                AssetDatabase.CreateAsset(data, BossDataPath);
            }

            SerializedObject serializedData = new SerializedObject(data);
            serializedData.FindProperty("monsterId").stringValue = "melee_boss";
            serializedData.FindProperty("displayName").stringValue = "우두머리 돌격형";
            serializedData.FindProperty("behaviorType").enumValueIndex =
                (int)KmsMonsterBehaviorType.ChaseContact;
            serializedData.FindProperty("prefab").objectReferenceValue = meleePrefab;
            serializedData.FindProperty("maxHealth").floatValue = 120f;
            serializedData.FindProperty("moveSpeed").floatValue = 1f;
            serializedData.FindProperty("attackDamage").floatValue = 10f;
            serializedData.FindProperty("attackCooldown").floatValue = 1f;
            serializedData.FindProperty("attackRange").floatValue = 0.04f;
            serializedData.FindProperty("preferredDistance").floatValue = 0f;
            serializedData.FindProperty("distanceTolerance").floatValue = 0f;
            serializedData.FindProperty("projectilePrefab").objectReferenceValue = null;
            serializedData.FindProperty("projectileSpeed").floatValue = 0f;
            serializedData.FindProperty("projectileLifetime").floatValue = 1f;
            serializedData.FindProperty("color").colorValue = Color.white;
            serializedData.FindProperty("hitFlashDuration").floatValue = 0.08f;
            serializedData.FindProperty("hitFlashColor").colorValue = Color.white;
            serializedData.FindProperty("healthBarVisibleDuration").floatValue = 1.25f;
            serializedData.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(data);
        }

        private static void ConfigureMonsterData(
            string dataPath,
            Sprite bodySprite,
            Sprite legSprite,
            Sprite leg2Sprite,
            Sprite weaponSprite,
            float targetHeight,
            float weaponScale,
            bool weaponFlipX,
            float? attackRange)
        {
            KmsMonsterData data = AssetDatabase.LoadAssetAtPath<KmsMonsterData>(dataPath);
            if (data == null)
            {
                throw new InvalidOperationException($"몬스터 데이터가 없습니다: {dataPath}");
            }

            float bodyHeight = bodySprite.bounds.size.y;
            if (bodyHeight <= 0f)
            {
                throw new InvalidOperationException($"스프라이트 높이가 유효하지 않습니다: {bodySprite.name}");
            }

            Bounds bodyBounds = bodySprite.bounds;
            Vector2 weaponAnchor = weaponSprite != null
                ? new Vector2(
                    Mathf.Lerp(bodyBounds.min.x, bodyBounds.max.x, 0.82f),
                    Mathf.Lerp(bodyBounds.min.y, bodyBounds.max.y, 0.35f))
                : Vector2.zero;

            SerializedObject serializedData = new SerializedObject(data);
            serializedData.FindProperty("sprite").objectReferenceValue = bodySprite;
            serializedData.FindProperty("color").colorValue = Color.white;
            serializedData.FindProperty("visualScale").floatValue = targetHeight / bodyHeight;
            serializedData.FindProperty("meleeWeaponSprite").objectReferenceValue = weaponSprite;
            serializedData.FindProperty("meleeWeaponScale").floatValue = weaponScale;
            serializedData.FindProperty("meleeWeaponAnchor").vector2Value = weaponAnchor;
            serializedData.FindProperty("meleeWeaponFlipX").boolValue = weaponFlipX;
            serializedData.FindProperty("legSprite").objectReferenceValue = legSprite;
            serializedData.FindProperty("leg2Sprite").objectReferenceValue = leg2Sprite;
            serializedData.FindProperty("legSwingAmplitude").floatValue = 0.08f;
            serializedData.FindProperty("legSwingSpeed").floatValue = 8f;
            serializedData.FindProperty("legReturnSpeed").floatValue = 10f;
            if (attackRange.HasValue)
            {
                serializedData.FindProperty("attackRange").floatValue = attackRange.Value;
            }

            serializedData.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(data);
        }

        private static SeparatedArt BuildSeparatedArt(
            string sourcePath,
            string sourceSpriteName,
            string outputPrefix,
            Vector2Int[] legTopPolygon,
            Vector2Int[] leg2TopPolygon,
            RectInt? sourceTopCrop = null)
        {
            Sprite sourceSprite = LoadSprite(sourcePath, sourceSpriteName);
            Texture2D sourceTexture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!ImageConversion.LoadImage(sourceTexture, File.ReadAllBytes(sourcePath), false))
            {
                UnityEngine.Object.DestroyImmediate(sourceTexture);
                throw new InvalidOperationException($"원본 PNG를 읽을 수 없습니다: {sourcePath}");
            }

            Rect spriteRect = sourceSprite.rect;
            RectInt crop = sourceTopCrop.HasValue
                ? ConvertTopRect(sourceTopCrop.Value, sourceTexture.height)
                : new RectInt(
                    Mathf.RoundToInt(spriteRect.x),
                    Mathf.RoundToInt(spriteRect.y),
                    Mathf.RoundToInt(spriteRect.width),
                    Mathf.RoundToInt(spriteRect.height));
            Vector2[] legPolygon = ConvertTopPolygon(legTopPolygon, sourceTexture.height);
            Vector2[] leg2Polygon = ConvertTopPolygon(leg2TopPolygon, sourceTexture.height);
            Color32[] sourcePixels = sourceTexture.GetPixels32();
            Color32[] bodyPixels = new Color32[crop.width * crop.height];
            Color32[] legPixels = new Color32[bodyPixels.Length];
            Color32[] leg2Pixels = new Color32[bodyPixels.Length];

            for (int localY = 0; localY < crop.height; localY++)
            {
                for (int localX = 0; localX < crop.width; localX++)
                {
                    int sourceX = crop.x + localX;
                    int sourceY = crop.y + localY;
                    int sourceIndex = (sourceY * sourceTexture.width) + sourceX;
                    int outputIndex = (localY * crop.width) + localX;
                    Color32 pixel = sourcePixels[sourceIndex];
                    bool belongsToLeg = IsInsidePolygon(sourceX + 0.5f, sourceY + 0.5f, legPolygon);
                    bool belongsToLeg2 =
                        IsInsidePolygon(sourceX + 0.5f, sourceY + 0.5f, leg2Polygon);

                    if (belongsToLeg)
                    {
                        legPixels[outputIndex] = pixel;
                    }
                    else if (belongsToLeg2)
                    {
                        leg2Pixels[outputIndex] = pixel;
                    }
                    else
                    {
                        bodyPixels[outputIndex] = pixel;
                    }
                }
            }

            string bodyPath = GeneratedArtFolder + $"/{outputPrefix}_Body.png";
            string legPath = GeneratedArtFolder + $"/{outputPrefix}_Leg.png";
            string leg2Path = GeneratedArtFolder + $"/{outputPrefix}_Leg2.png";
            WriteTexture(bodyPath, crop.width, crop.height, bodyPixels);
            WriteTexture(legPath, crop.width, crop.height, legPixels);
            WriteTexture(leg2Path, crop.width, crop.height, leg2Pixels);
            UnityEngine.Object.DestroyImmediate(sourceTexture);

            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            ConfigureGeneratedSprite(bodyPath, sourceSprite.pixelsPerUnit);
            ConfigureGeneratedSprite(legPath, sourceSprite.pixelsPerUnit);
            ConfigureGeneratedSprite(leg2Path, sourceSprite.pixelsPerUnit);
            return new SeparatedArt(
                LoadSingleSprite(bodyPath),
                LoadSingleSprite(legPath),
                LoadSingleSprite(leg2Path));
        }

        private static void WriteTexture(
            string assetPath,
            int width,
            int height,
            Color32[] pixels)
        {
            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            texture.SetPixels32(pixels);
            texture.Apply(false, false);
            byte[] encoded = texture.EncodeToPNG();
            UnityEngine.Object.DestroyImmediate(texture);

            if (!File.Exists(assetPath) || !File.ReadAllBytes(assetPath).SequenceEqual(encoded))
            {
                File.WriteAllBytes(assetPath, encoded);
            }
        }

        private static void ConfigureGeneratedSprite(string assetPath, float pixelsPerUnit)
        {
            TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null)
            {
                throw new InvalidOperationException($"생성 스프라이트 Importer가 없습니다: {assetPath}");
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = pixelsPerUnit;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            TextureImporterSettings settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            importer.SetTextureSettings(settings);
            importer.SaveAndReimport();
        }

        private static Sprite LoadSingleSprite(string assetPath)
        {
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
            if (sprite == null)
            {
                throw new InvalidOperationException($"생성 스프라이트를 찾을 수 없습니다: {assetPath}");
            }

            return sprite;
        }

        private static Vector2[] ConvertTopPolygon(Vector2Int[] topPolygon, int textureHeight)
        {
            return topPolygon
                .Select(point => new Vector2(point.x, textureHeight - 1 - point.y))
                .ToArray();
        }

        private static RectInt ConvertTopRect(RectInt topRect, int textureHeight)
        {
            return new RectInt(
                topRect.x,
                textureHeight - topRect.y - topRect.height,
                topRect.width,
                topRect.height);
        }

        private static bool IsInsidePolygon(float x, float y, Vector2[] polygon)
        {
            bool inside = false;
            for (int current = 0, previous = polygon.Length - 1;
                 current < polygon.Length;
                 previous = current++)
            {
                Vector2 currentPoint = polygon[current];
                Vector2 previousPoint = polygon[previous];
                bool crosses = (currentPoint.y > y) != (previousPoint.y > y) &&
                    x < ((previousPoint.x - currentPoint.x) * (y - currentPoint.y) /
                        (previousPoint.y - currentPoint.y)) + currentPoint.x;
                if (crosses)
                {
                    inside = !inside;
                }
            }

            return inside;
        }

        private static Sprite LoadSprite(string assetPath, string spriteName)
        {
            Sprite sprite = AssetDatabase.LoadAllAssetsAtPath(assetPath)
                .OfType<Sprite>()
                .FirstOrDefault(candidate => candidate.name == spriteName);
            if (sprite == null)
            {
                throw new InvalidOperationException(
                    $"스프라이트를 찾을 수 없습니다: {assetPath} / {spriteName}");
            }

            return sprite;
        }

        private static Transform EnsureChild(Transform parent, string name)
        {
            Transform child = parent.Find(name);
            if (child != null)
            {
                return child;
            }

            GameObject childObject = new GameObject(name);
            childObject.transform.SetParent(parent, false);
            return childObject.transform;
        }

        private readonly struct SeparatedArt
        {
            public SeparatedArt(Sprite body, Sprite leg, Sprite leg2)
            {
                Body = body;
                Leg = leg;
                Leg2 = leg2;
            }

            public Sprite Body { get; }
            public Sprite Leg { get; }
            public Sprite Leg2 { get; }
        }
    }
}
