using UnityEngine;

namespace KMS
{
    [DisallowMultipleComponent]
    public sealed class KmsMonsterTestHud : MonoBehaviour
    {
        [SerializeField] private PlayerStats playerStats;
        [SerializeField] private PlayerController2D playerController;
        [SerializeField] private WeaponInventory weaponInventory;
        [SerializeField] private KmsMonsterSpawner spawner;
        [SerializeField] private KmsWaveDirector waveDirector;
        [SerializeField] private KmsMonsterProjectilePool projectilePool;

        public void Configure(
            PlayerStats stats,
            PlayerController2D controller,
            WeaponInventory inventory,
            KmsMonsterSpawner monsterSpawner,
            KmsWaveDirector director = null,
            KmsMonsterProjectilePool monsterProjectilePool = null)
        {
            playerStats = stats;
            playerController = controller;
            weaponInventory = inventory;
            spawner = monsterSpawner;
            waveDirector = director;
            projectilePool = monsterProjectilePool;
        }

        private void OnGUI()
        {
            if (playerStats == null || playerController == null || weaponInventory == null || spawner == null)
            {
                return;
            }

            const float width = 430f;
            const float height = 278f;
            Rect area = new Rect(16f, 16f, width, height);
            GUI.Box(area, GUIContent.none);

            GUILayout.BeginArea(new Rect(area.x + 12f, area.y + 8f, width - 24f, height - 16f));
            GUILayout.Label("HDY 원본 테스트 환경 + KMS 몬스터");
            GUILayout.Label("Source: Assets/Scenes/HDY.unity");
            GUILayout.Label($"Player HP: {playerStats.CurrentHealth:0} / {playerStats.MaxHealth:0}");
            GUILayout.Label(
                $"Move: {playerStats.MoveSpeed:0.0} · Attack Stat: {playerStats.AttackPower:0.0} · Gold: {playerStats.Gold}");
            GUILayout.Label("Dash: Space");
            GUILayout.Label($"Weapons ({weaponInventory.ActiveWeapons.Count}): {GetWeaponSummary()}");
            GUILayout.Label(
                $"Enemy: {spawner.ActiveCount} active / {spawner.SpawnedCount} total spawns · " +
                $"pool {spawner.InactivePooledCount}/{spawner.TotalPooledInstanceCount} idle");
            GUILayout.Label(
                $"Melee: {spawner.GetActiveCount(KmsMonsterBehaviorType.ChaseContact)} · " +
                $"Ranged: {spawner.GetActiveCount(KmsMonsterBehaviorType.KeepDistanceProjectile)}");

            if (waveDirector != null)
            {
                GUILayout.Label($"Wave phase: {waveDirector.CurrentPhaseName}");
            }

            if (projectilePool != null)
            {
                GUILayout.Label(
                    $"Enemy projectile: {projectilePool.ActiveCount} active · " +
                    $"{projectilePool.InactiveCount}/{projectilePool.TotalInstanceCount} idle · " +
                    $"{projectilePool.TotalLaunchCount} launched");
            }

            GUILayout.Label("WASD 이동 · Space 대시 · 마우스 방향 자동 공격");
            GUILayout.EndArea();
        }

        private string GetWeaponSummary()
        {
            if (weaponInventory.ActiveWeapons.Count == 0)
            {
                return "로딩 중 또는 없음";
            }

            string summary = string.Empty;
            for (int index = 0; index < weaponInventory.ActiveWeapons.Count; index++)
            {
                ActiveWeapon weapon = weaponInventory.ActiveWeapons[index];
                if (index > 0)
                {
                    summary += ", ";
                }

                summary += $"{weapon.Data.weaponName} ({weapon.Data.id})";
            }

            return summary;
        }
    }
}
