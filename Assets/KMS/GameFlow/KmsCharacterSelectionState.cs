namespace KMS
{
    public enum KmsCharacterChoice
    {
        DaggerMan07,
        BowMan06
    }

    /// <summary>
    /// WeaponSelectScene에서 고른 캐릭터를 다음 GameScene 로드까지 유지한다.
    /// 런마다 씬 오브젝트는 다시 만들어지지만, 이 선택만 현재 플레이 세션 동안 유지된다.
    /// </summary>
    public static class KmsCharacterSelectionState
    {
        public static KmsCharacterChoice CurrentChoice { get; private set; } = KmsCharacterChoice.DaggerMan07;

        public static string StartingWeaponId =>
            CurrentChoice == KmsCharacterChoice.BowMan06 ? "bow" : "dagger";

        public static string CharacterPrefabName =>
            CurrentChoice == KmsCharacterChoice.BowMan06 ? "Man_06" : "Man_07";

        public static void SelectDaggerMan07()
        {
            CurrentChoice = KmsCharacterChoice.DaggerMan07;
        }

        public static void SelectBowMan06()
        {
            CurrentChoice = KmsCharacterChoice.BowMan06;
        }
    }
}
