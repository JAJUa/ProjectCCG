using System;

/// <summary>
/// 스탯 수정자를 나타내는 읽기 전용 구조체
/// 버프, 디버프, 장비 효과 등을 표현하는 데 사용
/// </summary>
[Serializable]
public readonly struct StatModifier
{
    public readonly EStatModifier ModifierType;
    public readonly float Value;
    public readonly int SetterID;

    /// <summary>
    /// StatModifier 생성자
    /// </summary>
    /// <param name="modifierType">수정자 타입 (더하기/곱하기)</param>
    /// <param name="value">수정자 값</param>
    /// <param name="setterID">설정자의 고유 ID</param>
    public StatModifier(EStatModifier modifierType, float value, int setterID)
    {
        ModifierType = modifierType;
        Value = value;
        SetterID = setterID;
    }

    /// <summary>
    /// 수정자 정보를 문자열로 반환
    /// </summary>
    /// <returns>수정자 정보 문자열</returns>
    public override string ToString()
    {
        string typeSymbol = ModifierType == EStatModifier.Add ? "+" : "×";
        return $"{typeSymbol}{Value} (SetterID: {SetterID})";
    }
}
