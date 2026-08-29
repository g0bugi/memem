using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace KMS.Editor
{
    internal static class KmsMonsterWaveContentBuilder
    {
        private const string MonsterFolder = "Assets/KMS/Monsters";
        private const string DataFolder = MonsterFolder + "/Data";
        private const string PrefabFolder = MonsterFolder + "/Prefabs";
        private const string ProjectileFolder = MonsterFolder + "/Projectiles";
        private const string WaveFolder = MonsterFolder + "/Waves";

        private const string MeleePrefabPath = PrefabFolder + "/KmsMeleeMonster.prefab";
        private const string RangedPrefabPath = PrefabFolder + "/KmsRangedMonster.prefab";
        private const string ProjectilePrefabPath = PrefabFolder + "/KmsMonsterProjectile.prefab";
        private const string NormalMeleeDataPath = DataFolder + "/KmsMeleeNormalData.asset";
        private const string FastMeleeDataPath = DataFolder + "/KmsMeleeFastData.asset";
        private const string TankMeleeDataPath = DataFolder + "/KmsMeleeTankData.asset";
        private const string RangedDataPath = DataFolder + "/KmsRangedNormalData.asset";
        private const string TestWaveSchedulePath = WaveFolder + "/KmsMonsterTestWaveSchedule.asset";

        public static Content BuildOrUpdateContent(int enemyLayer, Sprite monsterSprite, Sprite barSprite)
        {
            EnsureFolders();

            KmsMonster meleePrefab = BuildOrUpdateMonsterPrefab(
                MeleePrefabPath,
                "KmsMeleeMonster",
                enemyLayer,
                monsterSprite,
                barSprite,
                0.45f);
            KmsMonster rangedPrefab = BuildOrUpdateMonsterPrefab(
                RangedPrefabPath,
                "KmsRangedMonster",
                enemyLayer,
                monsterSprite,
                barSprite,
                0.42f);
            KmsMonsterProjectile projectilePrefab = BuildOrUpdateProjectilePrefab(
                enemyLayer,
                monsterSprite);

            KmsMonsterData normalMelee = BuildOrUpdateMonsterData(
                NormalMeleeDataPath,
                "melee_normal",
                "일반 근거리",
                KmsMonsterBehaviorType.ChaseContact,
                meleePrefab,
                projectilePrefab: null,
                maxHealth: 30f,
                moveSpeed: 1f,
                attackDamage: 5f,
                attackCooldown: 1f,
                attackRange: 0.02f,
                preferredDistance: 0f,
                distanceTolerance: 0f,
                projectileSpeed: 0f,
                projectileLifetime: 1f,
                sprite: monsterSprite,
                color: new Color(0.9f, 0.18f, 0.12f, 1f),
                visualScale: 1f);
            KmsMonsterData fastMelee = BuildOrUpdateMonsterData(
                FastMeleeDataPath,
                "melee_fast",
                "속도형 근거리",
                KmsMonsterBehaviorType.ChaseContact,
                meleePrefab,
                projectilePrefab: null,
                maxHealth: 18f,
                moveSpeed: 2.2f,
                attackDamage: 4f,
                attackCooldown: 0.75f,
                attackRange: 0.02f,
                preferredDistance: 0f,
                distanceTolerance: 0f,
                projectileSpeed: 0f,
                projectileLifetime: 1f,
                sprite: monsterSprite,
                color: new Color(1f, 0.55f, 0.08f, 1f),
                visualScale: 0.82f);
            KmsMonsterData tankMelee = BuildOrUpdateMonsterData(
                TankMeleeDataPath,
                "melee_tank",
                "탱커형 근거리",
                KmsMonsterBehaviorType.ChaseContact,
                meleePrefab,
                projectilePrefab: null,
                maxHealth: 90f,
                moveSpeed: 0.55f,
                attackDamage: 10f,
                attackCooldown: 1.4f,
                attackRange: 0.02f,
                preferredDistance: 0f,
                distanceTolerance: 0f,
                projectileSpeed: 0f,
                projectileLifetime: 1f,
                sprite: monsterSprite,
                color: new Color(0.48f, 0.08f, 0.08f, 1f),
                visualScale: 1.28f);
            KmsMonsterData ranged = BuildOrUpdateMonsterData(
                RangedDataPath,
                "ranged_normal",
                "일반 원거리",
                KmsMonsterBehaviorType.KeepDistanceProjectile,
                rangedPrefab,
                projectilePrefab,
                maxHealth: 24f,
                moveSpeed: 0.9f,
                attackDamage: 6f,
                attackCooldown: 1.6f,
                attackRange: 7f,
                preferredDistance: 5.5f,
                distanceTolerance: 0.75f,
                projectileSpeed: 6f,
                projectileLifetime: 4f,
                sprite: monsterSprite,
                color: new Color(0.58f, 0.22f, 0.95f, 1f),
                visualScale: 0.92f);

            KmsMonsterData[] allMonsters = { normalMelee, fastMelee, tankMelee, ranged };
            KmsWaveScheduleData testSchedule = BuildOrUpdateTestWaveSchedule(allMonsters);
            KmsMonsterArtSetupBuilder.ApplyToExistingContent();
            AssetDatabase.SaveAssets();

            return new Content(allMonsters, projectilePrefab, testSchedule, meleePrefab);
        }

        public static Runtime CreateOrReplaceRuntime(
            Scene scene,
            Content content,
            Transform playerTarget,
            Collider2D spawnArea,
            KmsRunTimer sharedRunTimer = null)
        {
            DestroyRootIfPresent(scene, "KmsMonsterRuntime");
            DestroyRootIfPresent(scene, "KmsMonsterSpawner");
            DestroyRootIfPresent(scene, "KmsWaveDirector");
            DestroyRootIfPresent(scene, "KmsMonsterProjectilePool");
            DestroyRootIfPresent(scene, "KmsWaveRunTimer");

            GameObject runtimeRoot = new GameObject("KmsMonsterRuntime");
            SceneManager.MoveGameObjectToScene(runtimeRoot, scene);

            GameObject projectilePoolObject = new GameObject("KmsMonsterProjectilePool");
            projectilePoolObject.transform.SetParent(runtimeRoot.transform, false);
            KmsMonsterProjectilePool projectilePool =
                projectilePoolObject.AddComponent<KmsMonsterProjectilePool>();
            projectilePool.Configure(new[] { content.ProjectilePrefab }, 16, 128);

            GameObject spawnerObject = new GameObject("KmsMonsterSpawner");
            spawnerObject.transform.SetParent(runtimeRoot.transform, false);
            spawnerObject.transform.position = new Vector3(6f, 0f, 0f);
            KmsMonsterSpawner spawner = spawnerObject.AddComponent<KmsMonsterSpawner>();
            spawner.Configure(content.MonsterData, playerTarget, spawnArea, projectilePool, false);

            GameObject runEndedMarker = new GameObject("KmsWaveRunEnded");
            runEndedMarker.transform.SetParent(runtimeRoot.transform, false);
            runEndedMarker.SetActive(false);

            KmsRunTimer timer = sharedRunTimer;
            if (timer == null)
            {
                GameObject timerObject = new GameObject("KmsWaveRunTimer");
                timerObject.transform.SetParent(runtimeRoot.transform, false);
                timer = timerObject.AddComponent<KmsRunTimer>();
                timer.Configure(60f, null, runEndedMarker);
            }

            GameObject directorObject = new GameObject("KmsWaveDirector");
            directorObject.transform.SetParent(runtimeRoot.transform, false);
            KmsWaveDirector director = directorObject.AddComponent<KmsWaveDirector>();
            director.Configure(content.TestSchedule, spawner, timer, true);

            return new Runtime(spawner, projectilePool, director, timer);
        }

        private static void EnsureFolders()
        {
            Directory.CreateDirectory(DataFolder);
            Directory.CreateDirectory(PrefabFolder);
            Directory.CreateDirectory(ProjectileFolder);
            Directory.CreateDirectory(WaveFolder);
            AssetDatabase.Refresh();
        }

        private static KmsMonster BuildOrUpdateMonsterPrefab(
            string path,
            string objectName,
            int enemyLayer,
            Sprite sprite,
            Sprite barSprite,
            float colliderRadius)
        {
            bool existing = AssetDatabase.LoadAssetAtPath<GameObject>(path) != null;
            GameObject root = existing
                ? PrefabUtility.LoadPrefabContents(path)
                : new GameObject(objectName);

            try
            {
                root.name = objectName;
                root.layer = enemyLayer;

                Rigidbody2D body = root.GetComponent<Rigidbody2D>();
                if (body == null)
                {
                    body = root.AddComponent<Rigidbody2D>();
                }

                body.bodyType = RigidbodyType2D.Dynamic;
                body.gravityScale = 0f;
                body.constraints = RigidbodyConstraints2D.FreezeRotation;
                body.collisionDetectionMode = CollisionDetectionMode2D.Discrete;
                body.interpolation = RigidbodyInterpolation2D.None;

                CircleCollider2D collider = root.GetComponent<CircleCollider2D>();
                if (collider == null)
                {
                    Collider2D existingCollider = root.GetComponent<Collider2D>();
                    if (existingCollider != null)
                    {
                        UnityEngine.Object.DestroyImmediate(existingCollider);
                    }

                    collider = root.AddComponent<CircleCollider2D>();
                }

                collider.radius = colliderRadius;
                collider.isTrigger = false;
                collider.excludeLayers = 1 << enemyLayer;

                Transform visualRoot = EnsureChild(root.transform, "Visual");
                visualRoot.localPosition = Vector3.zero;
                visualRoot.localRotation = Quaternion.identity;
                visualRoot.localScale = Vector3.one;
                SpriteRenderer visualRenderer = visualRoot.GetComponent<SpriteRenderer>();
                if (visualRenderer == null)
                {
                    visualRenderer = visualRoot.gameObject.AddComponent<SpriteRenderer>();
                }

                SpriteRenderer legacyRenderer = root.GetComponent<SpriteRenderer>();
                if (legacyRenderer != null)
                {
                    if (legacyRenderer.sprite != null)
                    {
                        sprite = legacyRenderer.sprite;
                    }

                    visualRenderer.sortingOrder = legacyRenderer.sortingOrder;
                    UnityEngine.Object.DestroyImmediate(legacyRenderer);
                }

                visualRenderer.sprite = sprite;
                visualRenderer.color = Color.white;
                visualRenderer.sortingOrder = 1;

                Transform projectileOrigin = EnsureChild(root.transform, "ProjectileSpawnPoint");
                projectileOrigin.localPosition = new Vector3(0.55f, 0f, 0f);

                EnsureHealthBar(root.transform, barSprite, out SpriteRenderer background, out SpriteRenderer fill);

                KmsMonster monster = root.GetComponent<KmsMonster>();
                if (monster == null)
                {
                    monster = root.AddComponent<KmsMonster>();
                }

                SerializedObject serializedMonster = new SerializedObject(monster);
                serializedMonster.FindProperty("visualRoot").objectReferenceValue = visualRoot;
                serializedMonster.FindProperty("visualRenderer").objectReferenceValue = visualRenderer;
                serializedMonster.FindProperty("projectileSpawnPoint").objectReferenceValue = projectileOrigin;
                serializedMonster.FindProperty("healthBarBackground").objectReferenceValue = background;
                serializedMonster.FindProperty("healthBarFill").objectReferenceValue = fill;
                serializedMonster.FindProperty("healthBarVisibleDuration").floatValue = 1.25f;
                serializedMonster.FindProperty("healthBarFullWidth").floatValue = 0.8f;
                serializedMonster.ApplyModifiedPropertiesWithoutUndo();

                GameObject saved = PrefabUtility.SaveAsPrefabAsset(root, path);
                return saved.GetComponent<KmsMonster>();
            }
            finally
            {
                if (existing)
                {
                    PrefabUtility.UnloadPrefabContents(root);
                }
                else
                {
                    UnityEngine.Object.DestroyImmediate(root);
                }
            }
        }

        private static KmsMonsterProjectile BuildOrUpdateProjectilePrefab(int enemyLayer, Sprite sprite)
        {
            bool existing = AssetDatabase.LoadAssetAtPath<GameObject>(ProjectilePrefabPath) != null;
            GameObject root = existing
                ? PrefabUtility.LoadPrefabContents(ProjectilePrefabPath)
                : new GameObject("KmsMonsterProjectile");

            try
            {
                root.name = "KmsMonsterProjectile";
                root.layer = enemyLayer;
                root.transform.localScale = Vector3.one * 0.35f;

                SpriteRenderer renderer = root.GetComponent<SpriteRenderer>();
                if (renderer == null)
                {
                    renderer = root.AddComponent<SpriteRenderer>();
                }

                renderer.sprite = sprite;
                renderer.color = new Color(0.72f, 0.35f, 1f, 1f);
                renderer.sortingOrder = 2;

                Rigidbody2D body = root.GetComponent<Rigidbody2D>();
                if (body == null)
                {
                    body = root.AddComponent<Rigidbody2D>();
                }

                body.bodyType = RigidbodyType2D.Dynamic;
                body.gravityScale = 0f;
                body.constraints = RigidbodyConstraints2D.FreezeRotation;
                body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
                body.interpolation = RigidbodyInterpolation2D.Interpolate;

                CircleCollider2D collider = root.GetComponent<CircleCollider2D>();
                if (collider == null)
                {
                    Collider2D existingCollider = root.GetComponent<Collider2D>();
                    if (existingCollider != null)
                    {
                        UnityEngine.Object.DestroyImmediate(existingCollider);
                    }

                    collider = root.AddComponent<CircleCollider2D>();
                }

                collider.radius = 0.32f;
                collider.isTrigger = true;
                collider.excludeLayers = 1 << enemyLayer;

                if (root.GetComponent<KmsMonsterProjectile>() == null)
                {
                    root.AddComponent<KmsMonsterProjectile>();
                }

                GameObject saved = PrefabUtility.SaveAsPrefabAsset(root, ProjectilePrefabPath);
                return saved.GetComponent<KmsMonsterProjectile>();
            }
            finally
            {
                if (existing)
                {
                    PrefabUtility.UnloadPrefabContents(root);
                }
                else
                {
                    UnityEngine.Object.DestroyImmediate(root);
                }
            }
        }

        private static KmsMonsterData BuildOrUpdateMonsterData(
            string path,
            string id,
            string displayName,
            KmsMonsterBehaviorType behaviorType,
            KmsMonster prefab,
            KmsMonsterProjectile projectilePrefab,
            float maxHealth,
            float moveSpeed,
            float attackDamage,
            float attackCooldown,
            float attackRange,
            float preferredDistance,
            float distanceTolerance,
            float projectileSpeed,
            float projectileLifetime,
            Sprite sprite,
            Color color,
            float visualScale)
        {
            KmsMonsterData data = AssetDatabase.LoadAssetAtPath<KmsMonsterData>(path);
            if (data != null)
            {
                return data;
            }

            data = ScriptableObject.CreateInstance<KmsMonsterData>();
            AssetDatabase.CreateAsset(data, path);

            SerializedObject serializedData = new SerializedObject(data);
            serializedData.FindProperty("monsterId").stringValue = id;
            serializedData.FindProperty("displayName").stringValue = displayName;
            serializedData.FindProperty("behaviorType").enumValueIndex = (int)behaviorType;
            serializedData.FindProperty("prefab").objectReferenceValue = prefab;
            serializedData.FindProperty("maxHealth").floatValue = maxHealth;
            serializedData.FindProperty("moveSpeed").floatValue = moveSpeed;
            serializedData.FindProperty("attackDamage").floatValue = attackDamage;
            serializedData.FindProperty("attackCooldown").floatValue = attackCooldown;
            serializedData.FindProperty("attackRange").floatValue = attackRange;
            serializedData.FindProperty("preferredDistance").floatValue = preferredDistance;
            serializedData.FindProperty("distanceTolerance").floatValue = distanceTolerance;
            serializedData.FindProperty("projectilePrefab").objectReferenceValue = projectilePrefab;
            serializedData.FindProperty("projectileSpeed").floatValue = projectileSpeed;
            serializedData.FindProperty("projectileLifetime").floatValue = projectileLifetime;
            serializedData.FindProperty("sprite").objectReferenceValue = sprite;
            serializedData.FindProperty("color").colorValue = color;
            serializedData.FindProperty("visualScale").floatValue = visualScale;
            serializedData.FindProperty("hitFlashDuration").floatValue = 0.08f;
            serializedData.FindProperty("hitFlashColor").colorValue = Color.white;
            serializedData.FindProperty("healthBarVisibleDuration").floatValue = 1.25f;
            serializedData.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(data);
            return data;
        }

        private static KmsWaveScheduleData BuildOrUpdateTestWaveSchedule(KmsMonsterData[] monsters)
        {
            KmsWaveScheduleData schedule =
                AssetDatabase.LoadAssetAtPath<KmsWaveScheduleData>(TestWaveSchedulePath);
            if (schedule != null)
            {
                return schedule;
            }

            schedule = ScriptableObject.CreateInstance<KmsWaveScheduleData>();
            AssetDatabase.CreateAsset(schedule, TestWaveSchedulePath);

            SerializedObject serializedSchedule = new SerializedObject(schedule);
            SerializedProperty phases = serializedSchedule.FindProperty("phases");
            phases.arraySize = 3;
            ConfigurePhase(phases.GetArrayElementAtIndex(0), "초반 테스트", 0f, 2f, 1, 12,
                new[] { monsters[0], monsters[1] }, new[] { 80, 20 });
            ConfigurePhase(phases.GetArrayElementAtIndex(1), "혼합 테스트", 15f, 1.4f, 1, 20,
                new[] { monsters[0], monsters[1], monsters[2], monsters[3] }, new[] { 45, 25, 15, 15 });
            ConfigurePhase(phases.GetArrayElementAtIndex(2), "압박 테스트", 35f, 0.9f, 2, 32,
                new[] { monsters[0], monsters[1], monsters[2], monsters[3] }, new[] { 30, 30, 15, 25 });
            serializedSchedule.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(schedule);
            return schedule;
        }

        private static void ConfigurePhase(
            SerializedProperty phase,
            string phaseName,
            float startTime,
            float interval,
            int batchCount,
            int maxActive,
            KmsMonsterData[] monsters,
            int[] weights)
        {
            phase.FindPropertyRelative("phaseName").stringValue = phaseName;
            phase.FindPropertyRelative("startTimeSeconds").floatValue = startTime;
            phase.FindPropertyRelative("spawnInterval").floatValue = interval;
            phase.FindPropertyRelative("spawnCountPerBatch").intValue = batchCount;
            phase.FindPropertyRelative("maxActiveMonsters").intValue = maxActive;

            SerializedProperty entries = phase.FindPropertyRelative("monsters");
            entries.arraySize = monsters.Length;
            for (int index = 0; index < monsters.Length; index++)
            {
                SerializedProperty entry = entries.GetArrayElementAtIndex(index);
                entry.FindPropertyRelative("monsterData").objectReferenceValue = monsters[index];
                entry.FindPropertyRelative("weight").intValue = weights[index];
            }
        }

        private static Transform EnsureChild(Transform parent, string childName)
        {
            Transform child = parent.Find(childName);
            if (child != null)
            {
                return child;
            }

            GameObject childObject = new GameObject(childName);
            childObject.transform.SetParent(parent, false);
            return childObject.transform;
        }

        private static void EnsureHealthBar(
            Transform monsterTransform,
            Sprite barSprite,
            out SpriteRenderer background,
            out SpriteRenderer fill)
        {
            Transform healthBar = EnsureChild(monsterTransform, "HealthBar");
            healthBar.localPosition = new Vector3(0f, -0.65f, 0f);

            Transform backgroundTransform = EnsureChild(healthBar, "Background");
            backgroundTransform.localScale = new Vector3(0.86f, 0.1f, 1f);
            background = backgroundTransform.GetComponent<SpriteRenderer>();
            if (background == null)
            {
                background = backgroundTransform.gameObject.AddComponent<SpriteRenderer>();
            }

            background.sprite = barSprite;
            background.color = new Color(0.03f, 0.03f, 0.03f, 0.9f);
            background.sortingOrder = 3;
            background.enabled = false;

            Transform fillTransform = EnsureChild(healthBar, "Fill");
            fillTransform.localPosition = Vector3.zero;
            fillTransform.localScale = new Vector3(0.8f, 0.06f, 1f);
            fill = fillTransform.GetComponent<SpriteRenderer>();
            if (fill == null)
            {
                fill = fillTransform.gameObject.AddComponent<SpriteRenderer>();
            }

            fill.sprite = barSprite;
            fill.color = new Color(0.2f, 0.9f, 0.25f, 1f);
            fill.sortingOrder = 4;
            fill.enabled = false;
        }

        private static void DestroyRootIfPresent(Scene scene, string objectName)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (root.name == objectName)
                {
                    UnityEngine.Object.DestroyImmediate(root);
                    return;
                }
            }
        }

        internal sealed class Content
        {
            public Content(
                KmsMonsterData[] monsterData,
                KmsMonsterProjectile projectilePrefab,
                KmsWaveScheduleData testSchedule,
                KmsMonster meleePrefab)
            {
                MonsterData = monsterData;
                ProjectilePrefab = projectilePrefab;
                TestSchedule = testSchedule;
                MeleePrefab = meleePrefab;
            }

            public KmsMonsterData[] MonsterData { get; }
            public KmsMonsterProjectile ProjectilePrefab { get; }
            public KmsWaveScheduleData TestSchedule { get; }
            public KmsMonster MeleePrefab { get; }
        }

        internal sealed class Runtime
        {
            public Runtime(
                KmsMonsterSpawner spawner,
                KmsMonsterProjectilePool projectilePool,
                KmsWaveDirector director,
                KmsRunTimer timer)
            {
                Spawner = spawner;
                ProjectilePool = projectilePool;
                Director = director;
                Timer = timer;
            }

            public KmsMonsterSpawner Spawner { get; }
            public KmsMonsterProjectilePool ProjectilePool { get; }
            public KmsWaveDirector Director { get; }
            public KmsRunTimer Timer { get; }
        }
    }
}
