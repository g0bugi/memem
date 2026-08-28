using System.Collections.Generic;

/// <summary>
/// 아이템(무기) 등급. 1이 가장 높은 전설 등급이고 숫자가 커질수록 낮은 등급이다.
/// 다른 코드/UI에서도 이 enum을 그대로 참조해서 등급을 비교·표시한다.
/// </summary>
public enum ItemGrade
{
    Legendary = 1,  // 전설
    Rare = 2,       // 희귀
    Common = 3      // 일반
}

/// <summary>ItemGrade를 화면에 표시할 한글 이름으로 변환하는 헬퍼.</summary>
public static class ItemGradeUtil
{
    private static readonly Dictionary<ItemGrade, string> DisplayNames = new Dictionary<ItemGrade, string>
    {
        { ItemGrade.Legendary, "전설" },
        { ItemGrade.Rare, "희귀" },
        { ItemGrade.Common, "일반" },
    };

    public static string GetDisplayName(ItemGrade grade)
    {
        return DisplayNames.TryGetValue(grade, out string name) ? name : grade.ToString();
    }
}
