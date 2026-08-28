using UnityEngine;

namespace KMS
{
    [DisallowMultipleComponent]
    public sealed class KmsMonsterTestHud : MonoBehaviour
    {
        [SerializeField] private PlayerStats playerStats;
        [SerializeField] private KmsMonsterSpawner spawner;

        public void Configure(PlayerStats stats, KmsMonsterSpawner monsterSpawner)
        {
            playerStats = stats;
            spawner = monsterSpawner;
        }

        private void OnGUI()
        {
            if (playerStats == null || spawner == null)
            {
                return;
            }

            const float width = 360f;
            const float height = 108f;
            Rect area = new Rect(16f, 16f, width, height);
            GUI.Box(area, GUIContent.none);

            GUILayout.BeginArea(new Rect(area.x + 12f, area.y + 8f, width - 24f, height - 16f));
            GUILayout.Label($"Player HP: {playerStats.CurrentHealth:0} / {playerStats.MaxHealth:0}");
            GUILayout.Label($"Enemy: {spawner.ActiveCount} active / {spawner.SpawnedCount} spawned");
            GUILayout.Label($"Configured spawnCount: {spawner.ConfiguredSpawnCount}");
            GUILayout.Label("WASD 이동 · Space 대시 · 마우스 방향 자동 공격");
            GUILayout.EndArea();
        }
    }
}
