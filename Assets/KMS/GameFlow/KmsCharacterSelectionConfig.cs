using UnityEngine;

namespace KMS
{
    /// <summary>선택 가능한 캐릭터 원본을 런타임 적용 코드에 전달하는 KMS 로컬 설정.</summary>
    public sealed class KmsCharacterSelectionConfig : ScriptableObject
    {
        [SerializeField] private GameObject daggerCharacterPrefab;
        [SerializeField] private GameObject bowCharacterPrefab;

        public GameObject GetPrefab(KmsCharacterChoice choice)
        {
            return choice == KmsCharacterChoice.BowMan06
                ? bowCharacterPrefab
                : daggerCharacterPrefab;
        }
    }
}
