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

            Camera camera = ConfigureCamera();
            PlayerStats playerStats = CreateTestPlayer(playerSprite, enemyLayer);
            KmsMonsterSpawner spawner = CreateSpawner(monsterPrefab, playerStats.transform);
            CreateHud(playerStats, spawner);

            camera.transform.position = new Vector3(0f, 0f, -10f);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Selection.activeObject = AssetDatabase.LoadAssetAtPath<GameObject>(MonsterPrefabPath);
            Debug.Log("[KMS] 몬스터 프리팹과 TestScene_KMS 구성을 완료했습니다.");
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

        private static Camera ConfigureCamera()
        {
            Camera camera = Camera.main;
            if (camera == null)
            {
                GameObject cameraObject = new GameObject("Main Camera");
                cameraObject.tag = "MainCamera";
                camera = cameraObject.AddComponent<Camera>();
                cameraObject.AddComponent<AudioListener>();
            }

            camera.orthographic = true;
            camera.orthographicSize = 10f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.06f, 0.08f, 0.12f, 1f);
            return camera;
        }

        private static PlayerStats CreateTestPlayer(Sprite sprite, int enemyLayer)
        {
            DestroyExisting("TestPlayer");

            GameObject playerObject = new GameObject("TestPlayer");
            playerObject.tag = "Player";
            playerObject.transform.position = Vector3.zero;

            SpriteRenderer renderer = playerObject.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = 2;

            Rigidbody2D body = playerObject.AddComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Dynamic;
            body.gravityScale = 0f;
            body.constraints = RigidbodyConstraints2D.FreezeRotation;
            body.collisionDetectionMode = CollisionDetectionMode2D.Discrete;
            body.interpolation = RigidbodyInterpolation2D.None;

            BoxCollider2D collider = playerObject.AddComponent<BoxCollider2D>();
            collider.size = new Vector2(0.8f, 0.8f);

            PlayerStats stats = playerObject.AddComponent<PlayerStats>();
            playerObject.AddComponent<PlayerController2D>();
            PlayerAttack attack = playerObject.AddComponent<PlayerAttack>();

            SerializedObject serializedAttack = new SerializedObject(attack);
            serializedAttack.FindProperty("weaponData").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<WeaponData>("Assets/HDY/Data/BasicAttack.asset");
            serializedAttack.FindProperty("targetLayers").intValue = 1 << enemyLayer;
            serializedAttack.ApplyModifiedPropertiesWithoutUndo();

            return stats;
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

        private static void CreateHud(PlayerStats playerStats, KmsMonsterSpawner spawner)
        {
            DestroyExisting("KmsMonsterTestHud");

            GameObject hudObject = new GameObject("KmsMonsterTestHud");
            KmsMonsterTestHud hud = hudObject.AddComponent<KmsMonsterTestHud>();
            hud.Configure(playerStats, spawner);
        }

        private static void DestroyExisting(string objectName)
        {
            GameObject existing = GameObject.Find(objectName);
            if (existing != null)
            {
                Object.DestroyImmediate(existing);
            }
        }
    }
}
