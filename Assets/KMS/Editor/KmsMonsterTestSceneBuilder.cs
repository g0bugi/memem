using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace KMS.Editor
{
    public static class KmsMonsterTestSceneBuilder
    {
        private const string ScenePath = "Assets/KMS/TestScene_KMS.unity";
        private const string HdyScenePath = "Assets/Scenes/HDY.unity";
        private const string MonsterFolder = "Assets/KMS/Monsters";
        private const string PrefabFolder = MonsterFolder + "/Prefabs";
        private const string ArtFolder = MonsterFolder + "/Art";
        private const string MonsterSpritePath = ArtFolder + "/KmsMonsterVisual.asset";
        private const string PlayerSpritePath = ArtFolder + "/KmsTestPlayerVisual.asset";
        private const string MonsterPrefabPath = PrefabFolder + "/KmsMeleeMonster.prefab";

        [MenuItem("KMS/Build Monster Test Scene")]
        public static void Build()
        {
            int enemyLayer = LayerMask.NameToLayer("Enemy");
            if (enemyLayer < 0)
            {
                throw new System.InvalidOperationException("Enemy 레이어가 ProjectSettings/TagManager.asset에 없습니다.");
            }

            EnsureFolders();

            Sprite monsterSprite = CreateSpriteAsset(
                MonsterSpritePath,
                "KmsMonsterSprite",
                new Color(0.9f, 0.18f, 0.12f, 1f),
                true);
            Sprite playerSprite = CreateSpriteAsset(
                PlayerSpritePath,
                "KmsTestPlayerSprite",
                new Color(0.15f, 0.65f, 1f, 1f),
                false);

            KmsMonster monsterPrefab = CreateMonsterPrefab(enemyLayer, monsterSprite, playerSprite);
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            HdyTestEnvironment hdyEnvironment = CloneHdyTestEnvironment(scene, enemyLayer);
            KmsMonsterSpawner spawner = CreateSpawner(monsterPrefab, hdyEnvironment.PlayerStats.transform);
            CreateHud(
                hdyEnvironment.PlayerStats,
                hdyEnvironment.PlayerController,
                hdyEnvironment.WeaponInventory,
                spawner);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Selection.activeObject = AssetDatabase.LoadAssetAtPath<GameObject>(MonsterPrefabPath);
            Debug.Log("[KMS] HDY 테스트 환경과 KMS 몬스터를 TestScene_KMS에 통합했습니다.");
        }

        public static void BuildFromCommandLine()
        {
            Build();
        }

        private static void EnsureFolders()
        {
            Directory.CreateDirectory(PrefabFolder);
            Directory.CreateDirectory(ArtFolder);
        }

        private static Sprite CreateSpriteAsset(string path, string spriteName, Color color, bool circle)
        {
            Sprite existingSprite = FindSpriteAtPath(path, spriteName);
            if (existingSprite != null)
            {
                return existingSprite;
            }

            const int size = 32;
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = spriteName + "Texture",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };

            Color[] pixels = new Color[size * size];
            Vector2 center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
            float radiusSquared = (size * 0.46f) * (size * 0.46f);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    bool visible = !circle || ((new Vector2(x, y) - center).sqrMagnitude <= radiusSquared);
                    pixels[(y * size) + x] = visible ? color : Color.clear;
                }
            }

            texture.SetPixels(pixels);
            texture.Apply(false, false);

            Sprite sprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, size, size),
                new Vector2(0.5f, 0.5f),
                size,
                0,
                SpriteMeshType.FullRect);
            sprite.name = spriteName;

            AssetDatabase.CreateAsset(texture, path);
            AssetDatabase.AddObjectToAsset(sprite, texture);
            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            return FindSpriteAtPath(path, spriteName);
        }

        private static Sprite FindSpriteAtPath(string path, string spriteName)
        {
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);
            foreach (Object asset in assets)
            {
                if (asset is Sprite sprite && sprite.name == spriteName)
                {
                    return sprite;
                }
            }

            return null;
        }

        private static KmsMonster CreateMonsterPrefab(int enemyLayer, Sprite sprite, Sprite barSprite)
        {
            GameObject existingPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(MonsterPrefabPath);
            if (existingPrefab != null)
            {
                return existingPrefab.GetComponent<KmsMonster>();
            }

            GameObject monsterObject = new GameObject("KmsMeleeMonster");
            monsterObject.layer = enemyLayer;

            SpriteRenderer renderer = monsterObject.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = 1;

            Rigidbody2D body = monsterObject.AddComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Dynamic;
            body.gravityScale = 0f;
            body.constraints = RigidbodyConstraints2D.FreezeRotation;
            body.collisionDetectionMode = CollisionDetectionMode2D.Discrete;
            body.interpolation = RigidbodyInterpolation2D.None;

            CircleCollider2D collider = monsterObject.AddComponent<CircleCollider2D>();
            collider.radius = 0.45f;
            collider.excludeLayers = 1 << enemyLayer;

            CreateHealthBar(monsterObject.transform, barSprite, out SpriteRenderer background, out SpriteRenderer fill);

            KmsMonster monster = monsterObject.AddComponent<KmsMonster>();
            SerializedObject serializedMonster = new SerializedObject(monster);
            serializedMonster.FindProperty("healthBarBackground").objectReferenceValue = background;
            serializedMonster.FindProperty("healthBarFill").objectReferenceValue = fill;
            serializedMonster.FindProperty("healthBarVisibleDuration").floatValue = 1.25f;
            serializedMonster.FindProperty("healthBarFullWidth").floatValue = 0.8f;
            serializedMonster.ApplyModifiedPropertiesWithoutUndo();

            GameObject prefabObject = PrefabUtility.SaveAsPrefabAsset(monsterObject, MonsterPrefabPath);
            Object.DestroyImmediate(monsterObject);
            return prefabObject.GetComponent<KmsMonster>();
        }

        private static void CreateHealthBar(
            Transform monsterTransform,
            Sprite barSprite,
            out SpriteRenderer background,
            out SpriteRenderer fill)
        {
            GameObject healthBarObject = new GameObject("HealthBar");
            healthBarObject.transform.SetParent(monsterTransform, false);
            healthBarObject.transform.localPosition = new Vector3(0f, -0.65f, 0f);

            GameObject backgroundObject = new GameObject("Background");
            backgroundObject.transform.SetParent(healthBarObject.transform, false);
            backgroundObject.transform.localScale = new Vector3(0.86f, 0.1f, 1f);
            background = backgroundObject.AddComponent<SpriteRenderer>();
            background.sprite = barSprite;
            background.color = new Color(0.03f, 0.03f, 0.03f, 0.9f);
            background.sortingOrder = 3;
            background.enabled = false;

            GameObject fillObject = new GameObject("Fill");
            fillObject.transform.SetParent(healthBarObject.transform, false);
            fillObject.transform.localPosition = Vector3.zero;
            fillObject.transform.localScale = new Vector3(0.8f, 0.06f, 1f);
            fill = fillObject.AddComponent<SpriteRenderer>();
            fill.sprite = barSprite;
            fill.color = new Color(0.2f, 0.9f, 0.25f, 1f);
            fill.sortingOrder = 4;
            fill.enabled = false;
        }

        private static HdyTestEnvironment CloneHdyTestEnvironment(Scene targetScene, int enemyLayer)
        {
            Scene hdyScene = EditorSceneManager.OpenScene(HdyScenePath, OpenSceneMode.Additive);
            try
            {
                GameObject sourcePlayer = FindRequiredRoot(hdyScene, "Player");
                GameObject sourceCamera = FindRequiredRoot(hdyScene, "Main Camera");
                GameObject sourcePoolManagers = FindRequiredRoot(hdyScene, "PoolManagers");

                RequireComponent<PlayerStats>(sourcePlayer);
                RequireComponent<PlayerController2D>(sourcePlayer);
                RequireComponent<PlayerAttack>(sourcePlayer);
                WeaponInventory sourceInventory = RequireComponent<WeaponInventory>(sourcePlayer);
                RequireComponent<Camera>(sourceCamera);
                RequireComponent<CameraFollow2D>(sourceCamera);
                RequireComponent<ProjectilePoolManager>(sourcePoolManagers);
                RequireComponent<EffectPoolManager>(sourcePoolManagers);

                int targetLayerMask = sourceInventory.TargetLayers.value;
                if ((targetLayerMask & (1 << enemyLayer)) == 0)
                {
                    throw new System.InvalidOperationException(
                        "HDY PlayerAttack의 targetLayers에 Enemy 레이어가 포함되어 있지 않습니다.");
                }

                DestroyRootIfPresent(targetScene, "TestPlayer");
                DestroyRootIfPresent(targetScene, "Player");
                DestroyRootIfPresent(targetScene, "Main Camera");
                DestroyRootIfPresent(targetScene, "PoolManagers");

                SceneManager.SetActiveScene(targetScene);
                GameObject player = CloneRootToScene(sourcePlayer, targetScene);
                GameObject cameraObject = CloneRootToScene(sourceCamera, targetScene);
                GameObject poolManagers = CloneRootToScene(sourcePoolManagers, targetScene);

                PlayerStats playerStats = RequireComponent<PlayerStats>(player);
                PlayerController2D playerController = RequireComponent<PlayerController2D>(player);
                WeaponInventory weaponInventory = RequireComponent<WeaponInventory>(player);
                RequireComponent<PlayerAttack>(player);

                RequireComponent<Camera>(cameraObject);
                CameraFollow2D cameraFollow = RequireComponent<CameraFollow2D>(cameraObject);
                SerializedObject serializedCameraFollow = new SerializedObject(cameraFollow);
                serializedCameraFollow.FindProperty("target").objectReferenceValue = player.transform;
                serializedCameraFollow.ApplyModifiedPropertiesWithoutUndo();

                RequireComponent<ProjectilePoolManager>(poolManagers);
                RequireComponent<EffectPoolManager>(poolManagers);

                return new HdyTestEnvironment(playerStats, playerController, weaponInventory);
            }
            finally
            {
                EditorSceneManager.CloseScene(hdyScene, true);
                SceneManager.SetActiveScene(targetScene);
            }
        }

        private static GameObject FindRequiredRoot(Scene scene, string objectName)
        {
            foreach (GameObject rootObject in scene.GetRootGameObjects())
            {
                if (rootObject.name == objectName)
                {
                    return rootObject;
                }
            }

            throw new System.InvalidOperationException(
                $"{HdyScenePath}에서 필수 루트 오브젝트 '{objectName}'을 찾을 수 없습니다.");
        }

        private static GameObject CloneRootToScene(GameObject source, Scene targetScene)
        {
            GameObject clone = Object.Instantiate(source);
            clone.name = source.name;
            SceneManager.MoveGameObjectToScene(clone, targetScene);
            return clone;
        }

        private static T RequireComponent<T>(GameObject gameObject) where T : Component
        {
            T component = gameObject.GetComponent<T>();
            if (component == null)
            {
                throw new System.InvalidOperationException(
                    $"{gameObject.name}에 필수 컴포넌트 {typeof(T).Name}이 없습니다.");
            }

            return component;
        }

        private static void DestroyRootIfPresent(Scene scene, string objectName)
        {
            foreach (GameObject rootObject in scene.GetRootGameObjects())
            {
                if (rootObject.name == objectName)
                {
                    Object.DestroyImmediate(rootObject);
                    return;
                }
            }
        }

        private static KmsMonsterSpawner CreateSpawner(KmsMonster prefab, Transform playerTarget)
        {
            DestroyExisting("KmsMonsterSpawner");

            GameObject spawnerObject = new GameObject("KmsMonsterSpawner");
            spawnerObject.transform.position = new Vector3(6f, 0f, 0f);
            KmsMonsterSpawner spawner = spawnerObject.AddComponent<KmsMonsterSpawner>();
            spawner.Configure(prefab, playerTarget, 1);
            return spawner;
        }

        private static void CreateHud(
            PlayerStats playerStats,
            PlayerController2D playerController,
            WeaponInventory weaponInventory,
            KmsMonsterSpawner spawner)
        {
            DestroyExisting("KmsMonsterTestHud");

            GameObject hudObject = new GameObject("KmsMonsterTestHud");
            KmsMonsterTestHud hud = hudObject.AddComponent<KmsMonsterTestHud>();
            hud.Configure(playerStats, playerController, weaponInventory, spawner);
        }

        private static void DestroyExisting(string objectName)
        {
            GameObject existing = GameObject.Find(objectName);
            if (existing != null)
            {
                Object.DestroyImmediate(existing);
            }
        }

        private sealed class HdyTestEnvironment
        {
            public HdyTestEnvironment(
                PlayerStats playerStats,
                PlayerController2D playerController,
                WeaponInventory weaponInventory)
            {
                PlayerStats = playerStats;
                PlayerController = playerController;
                WeaponInventory = weaponInventory;
            }

            public PlayerStats PlayerStats { get; }
            public PlayerController2D PlayerController { get; }
            public WeaponInventory WeaponInventory { get; }
        }
    }
}
