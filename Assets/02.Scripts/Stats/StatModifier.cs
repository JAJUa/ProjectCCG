using System;

/// <summary>
/// ���� �����ڸ� ��Ÿ���� �б� ���� ����ü
/// ����, �����, ��� ȿ�� ���� ǥ���ϴ� �� ���
/// </summary>
[Serializable]
public readonly struct StatModifier
{
    public readonly EStatModifier ModifierType;
    public readonly float Value;
    public readonly int SetterID;

    /// <summary>
    /// StatModifier ������
    /// </summary>
    /// <param name="modifierType">������ Ÿ�� (���ϱ�/���ϱ�)</param>
    /// <param name="value">������ ��</param>
    /// <param name="setterID">�������� ���� ID</param>
    public StatModifier(EStatModifier modifierType, float value, int setterID)
    {
        ModifierType = modifierType;
        Value = value;
        SetterID = setterID;
    }

    /// <summary>
    /// ������ ������ ���ڿ��� ��ȯ
    /// </summary>
    /// <returns>������ ���� ���ڿ�</returns>
    public override string ToString()
    {
        string typeSymbol = ModifierType == EStatModifier.Add ? "+" : "��";
        return $"{typeSymbol}{Value} (SetterID: {SetterID})";
    }
}
