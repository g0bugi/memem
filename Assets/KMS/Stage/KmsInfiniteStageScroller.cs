using System.Collections.Generic;
using UnityEngine;

namespace KMS
{
    [DisallowMultipleComponent]
    public sealed class KmsInfiniteStageScroller : MonoBehaviour
    {
        public static readonly Vector2 DefaultChunkSize = new Vector2(20f, 20f);
        public static readonly Vector2Int DefaultGridSize = new Vector2Int(3, 3);

        [Header("References")]
        [SerializeField] private Transform playerTarget;
        [SerializeField] private SpriteRenderer floorTemplate;

        [Header("Chunk Grid")]
        [SerializeField] private Vector2 chunkSize = new Vector2(20f, 20f);
        [SerializeField] private Vector2Int gridSize = new Vector2Int(3, 3);

        private readonly List<RuntimeChunk> runtimeChunks = new List<RuntimeChunk>();
        private Transform runtimeChunkRoot;
        private Vector2Int centerChunkCoordinate;
        private bool isInitialized;
        private bool warnedMissingTarget;

        public bool IsInitialized => isInitialized;
        public int ActiveChunkCount => runtimeChunks.Count;
        public int RuntimeChunkCreationCount { get; private set; }
        public int RepositionCount { get; private set; }
        public Vector2 ChunkSize => GetValidChunkSize();
        public Vector2Int GridSize => GetValidGridSize();
        public Vector2Int CenterChunkCoordinate => centerChunkCoordinate;

        private void Awake()
        {
            InitializeRuntimeChunks();
        }

        private void LateUpdate()
        {
            if (!isInitialized)
            {
                InitializeRuntimeChunks();
                if (!isInitialized)
                {
                    return;
                }
            }

            if (!ResolvePlayerTarget())
            {
                return;
            }

            Vector2Int nextCenter = GetChunkCoordinate(playerTarget.position, ChunkSize);
            if (nextCenter == centerChunkCoordinate)
            {
                return;
            }

            centerChunkCoordinate = nextCenter;
            RepositionRuntimeChunks();
            RepositionCount++;
        }

        public void Configure(
            Transform target,
            SpriteRenderer template,
            Vector2 configuredChunkSize,
            Vector2Int configuredGridSize)
        {
            playerTarget = target;
            floorTemplate = template;
            chunkSize = configuredChunkSize;
            gridSize = configuredGridSize;
        }

        public bool CoversWorldPosition(Vector3 worldPosition)
        {
            if (!isInitialized)
            {
                return false;
            }

            Vector2 validChunkSize = ChunkSize;
            Vector2Int validGridSize = GridSize;
            Vector2 center = new Vector2(
                centerChunkCoordinate.x * validChunkSize.x,
                centerChunkCoordinate.y * validChunkSize.y);
            Vector2 halfExtent = new Vector2(
                validGridSize.x * validChunkSize.x * 0.5f,
                validGridSize.y * validChunkSize.y * 0.5f);

            return worldPosition.x >= center.x - halfExtent.x &&
                worldPosition.x <= center.x + halfExtent.x &&
                worldPosition.y >= center.y - halfExtent.y &&
                worldPosition.y <= center.y + halfExtent.y;
        }

        public static Vector2Int GetChunkCoordinate(Vector3 worldPosition, Vector2 configuredChunkSize)
        {
            float width = Mathf.Max(0.01f, configuredChunkSize.x);
            float height = Mathf.Max(0.01f, configuredChunkSize.y);
            return new Vector2Int(
                Mathf.FloorToInt((worldPosition.x + (width * 0.5f)) / width),
                Mathf.FloorToInt((worldPosition.y + (height * 0.5f)) / height));
        }

        private void InitializeRuntimeChunks()
        {
            if (isInitialized || floorTemplate == null || floorTemplate.sprite == null ||
                !ResolvePlayerTarget())
            {
                return;
            }

            GameObject rootObject = new GameObject("RuntimeChunks");
            runtimeChunkRoot = rootObject.transform;
            runtimeChunkRoot.SetParent(transform, false);

            Vector2Int validGridSize = GridSize;
            int halfColumns = validGridSize.x / 2;
            int halfRows = validGridSize.y / 2;

            for (int row = -halfRows; row <= halfRows; row++)
            {
                for (int column = -halfColumns; column <= halfColumns; column++)
                {
                    RuntimeChunk chunk = CreateRuntimeChunk(new Vector2Int(column, row));
                    runtimeChunks.Add(chunk);
                    RuntimeChunkCreationCount++;
                }
            }

            floorTemplate.enabled = false;
            centerChunkCoordinate = GetChunkCoordinate(playerTarget.position, ChunkSize);
            RepositionRuntimeChunks();
            isInitialized = true;
        }

        private RuntimeChunk CreateRuntimeChunk(Vector2Int gridOffset)
        {
            GameObject chunkObject = new GameObject(
                $"FloorChunk_{gridOffset.x:+0;-0;0}_{gridOffset.y:+0;-0;0}");
            chunkObject.transform.SetParent(runtimeChunkRoot, false);
            chunkObject.transform.rotation = floorTemplate.transform.rotation;

            SpriteRenderer renderer = chunkObject.AddComponent<SpriteRenderer>();
            renderer.sprite = floorTemplate.sprite;
            renderer.color = floorTemplate.color;
            renderer.sharedMaterial = floorTemplate.sharedMaterial;
            renderer.sortingLayerID = floorTemplate.sortingLayerID;
            renderer.sortingOrder = floorTemplate.sortingOrder;
            renderer.flipX = floorTemplate.flipX;
            renderer.flipY = floorTemplate.flipY;
            renderer.maskInteraction = floorTemplate.maskInteraction;

            Vector2 spriteSize = floorTemplate.sprite.bounds.size;
            Vector2 validChunkSize = ChunkSize;
            chunkObject.transform.localScale = new Vector3(
                validChunkSize.x / Mathf.Max(0.01f, spriteSize.x),
                validChunkSize.y / Mathf.Max(0.01f, spriteSize.y),
                1f);

            return new RuntimeChunk(chunkObject.transform, gridOffset);
        }

        private void RepositionRuntimeChunks()
        {
            Vector2 validChunkSize = ChunkSize;
            float zPosition = floorTemplate != null ? floorTemplate.transform.position.z : 0f;

            foreach (RuntimeChunk chunk in runtimeChunks)
            {
                Vector2Int worldCoordinate = centerChunkCoordinate + chunk.GridOffset;
                chunk.Transform.position = new Vector3(
                    worldCoordinate.x * validChunkSize.x,
                    worldCoordinate.y * validChunkSize.y,
                    zPosition);
            }
        }

        private bool ResolvePlayerTarget()
        {
            if (playerTarget == null)
            {
                GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
                if (playerObject != null)
                {
                    playerTarget = playerObject.transform;
                }
            }

            if (playerTarget != null)
            {
                warnedMissingTarget = false;
                return true;
            }

            if (!warnedMissingTarget)
            {
                warnedMissingTarget = true;
                Debug.LogError("[KMS] 무한 스테이지가 Player 태그 대상을 찾을 수 없습니다.", this);
            }

            return false;
        }

        private Vector2 GetValidChunkSize()
        {
            return new Vector2(
                Mathf.Max(1f, chunkSize.x),
                Mathf.Max(1f, chunkSize.y));
        }

        private Vector2Int GetValidGridSize()
        {
            return new Vector2Int(
                MakeOddAtLeastThree(gridSize.x),
                MakeOddAtLeastThree(gridSize.y));
        }

        private static int MakeOddAtLeastThree(int value)
        {
            int normalized = Mathf.Max(3, value);
            return normalized % 2 == 0 ? normalized + 1 : normalized;
        }

        private void OnDrawGizmosSelected()
        {
            Vector2 validChunkSize = GetValidChunkSize();
            Vector2Int validGridSize = GetValidGridSize();
            Vector2Int center = playerTarget != null
                ? GetChunkCoordinate(playerTarget.position, validChunkSize)
                : Vector2Int.zero;

            Gizmos.color = new Color(0.1f, 0.85f, 1f, 0.65f);
            for (int row = -(validGridSize.y / 2); row <= validGridSize.y / 2; row++)
            {
                for (int column = -(validGridSize.x / 2); column <= validGridSize.x / 2; column++)
                {
                    Vector3 chunkCenter = new Vector3(
                        (center.x + column) * validChunkSize.x,
                        (center.y + row) * validChunkSize.y,
                        transform.position.z);
                    Gizmos.DrawWireCube(chunkCenter, validChunkSize);
                }
            }
        }

        private readonly struct RuntimeChunk
        {
            public RuntimeChunk(Transform transform, Vector2Int gridOffset)
            {
                Transform = transform;
                GridOffset = gridOffset;
            }

            public Transform Transform { get; }
            public Vector2Int GridOffset { get; }
        }
    }
}
