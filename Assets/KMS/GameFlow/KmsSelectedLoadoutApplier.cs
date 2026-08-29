using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace KMS
{
    /// <summary>
    /// GameScene의 공유 Player 프리팹을 수정하지 않고, Start 전에 선택한 기본 무기와 외형을 적용한다.
    /// </summary>
    public static class KmsSelectedLoadoutApplier
    {
        private const string ConfigResourceName = "KmsCharacterSelectionConfig";
        private static readonly FieldInfo WeaponIdsField = typeof(WeaponInventory).GetField(
            "weaponIds", BindingFlags.Instance | BindingFlags.NonPublic);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void RegisterSceneLoadedHandler()
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            SceneManager.sceneLoaded += HandleSceneLoaded;
        }

        private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name != KmsSceneNavigator.GameSceneName)
            {
                return;
            }

            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player == null)
            {
                Debug.LogWarning("[KMS] 선택 캐릭터를 적용할 Player 오브젝트를 찾지 못했습니다.");
                return;
            }

            ApplyStartingWeapon(player);
            ApplyCharacterVisual(player);
        }

        private static void ApplyStartingWeapon(GameObject player)
        {
            WeaponInventory inventory = player.GetComponent<WeaponInventory>();
            if (inventory == null || WeaponIdsField == null)
            {
                Debug.LogWarning("[KMS] WeaponInventory 시작 무기 목록을 찾지 못해 기본값을 유지합니다.", player);
                return;
            }

            if (!(WeaponIdsField.GetValue(inventory) is List<string> weaponIds))
            {
                Debug.LogWarning("[KMS] WeaponInventory 시작 무기 목록 형식이 예상과 다릅니다.", player);
                return;
            }

            weaponIds.Clear();
            weaponIds.Add(KmsCharacterSelectionState.StartingWeaponId);
        }

        private static void ApplyCharacterVisual(GameObject player)
        {
            KmsCharacterSelectionConfig config = Resources.Load<KmsCharacterSelectionConfig>(ConfigResourceName);
            GameObject sourcePrefab = config != null
                ? config.GetPrefab(KmsCharacterSelectionState.CurrentChoice)
                : null;

            if (sourcePrefab == null)
            {
                Debug.LogWarning("[KMS] 선택 캐릭터 설정 또는 원본 프리팹을 찾지 못해 기본 외형을 유지합니다.", player);
                return;
            }

            Transform targetRoot = FindCharacterVisualRoot(player.transform);
            if (targetRoot == null)
            {
                Debug.LogWarning("[KMS] Player 아래에서 캐릭터 외형 루트를 찾지 못했습니다.", player);
                return;
            }

            CopyCharacterParts(sourcePrefab.transform, targetRoot);
            targetRoot.name = sourcePrefab.name;
        }

        private static Transform FindCharacterVisualRoot(Transform playerRoot)
        {
            Transform[] transforms = playerRoot.GetComponentsInChildren<Transform>(true);
            foreach (Transform candidate in transforms)
            {
                if (candidate.name.StartsWith("Man_"))
                {
                    return candidate;
                }
            }

            return null;
        }

        private static void CopyCharacterParts(Transform sourceRoot, Transform targetRoot)
        {
            var targetRenderers = new Dictionary<string, SpriteRenderer>();
            foreach (SpriteRenderer renderer in targetRoot.GetComponentsInChildren<SpriteRenderer>(true))
            {
                targetRenderers[renderer.gameObject.name] = renderer;
                renderer.enabled = false;
            }

            foreach (SpriteRenderer sourceRenderer in sourceRoot.GetComponentsInChildren<SpriteRenderer>(true))
            {
                if (!targetRenderers.TryGetValue(sourceRenderer.gameObject.name, out SpriteRenderer targetRenderer))
                {
                    GameObject part = new GameObject(sourceRenderer.gameObject.name, typeof(SpriteRenderer));
                    part.transform.SetParent(targetRoot, false);
                    targetRenderer = part.GetComponent<SpriteRenderer>();
                }

                Transform sourceTransform = sourceRenderer.transform;
                Transform targetTransform = targetRenderer.transform;
                targetTransform.localPosition = sourceTransform.localPosition;
                targetTransform.localRotation = sourceTransform.localRotation;
                targetTransform.localScale = sourceTransform.localScale;

                targetRenderer.sprite = sourceRenderer.sprite;
                targetRenderer.color = sourceRenderer.color;
                targetRenderer.sharedMaterial = sourceRenderer.sharedMaterial;
                targetRenderer.sortingLayerID = sourceRenderer.sortingLayerID;
                targetRenderer.sortingOrder = sourceRenderer.sortingOrder;
                targetRenderer.flipX = sourceRenderer.flipX;
                targetRenderer.flipY = sourceRenderer.flipY;
                targetRenderer.enabled = sourceRenderer.enabled;
            }
        }
    }
}
