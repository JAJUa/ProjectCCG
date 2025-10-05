using System.Runtime.CompilerServices;
using System;

namespace tools.Stats
{
    /// <summary>
    /// ���� �� ����(ü��, ���ݷ�, ���� ��)�� �����ϴ� Ŭ����
    /// �⺻���� �پ��� ������(����/�����)�� �����Ͽ� ���� ���� ���
    /// </summary>
    [Serializable]
    public class Stat
    {
        // ���� �� ���� �� ȣ��
        public delegate void OnValueChangeDelegate(float value);
        // ���� �� ���� �� ȣ��2
        public event OnValueChangeDelegate OnValueChanged;

        private StatModifierCollection _addModifiers; // ���ϱ�
        private StatModifierCollection _multiplyModifiers; // ���ϱ�

        private float _baseValue; // �⺻��
        private float _cachedValue;
        private bool _isDirty;

        /// <summary>
        /// Stat �ν��Ͻ��� �����ϴ� ���丮 �޼���
        /// </summary>
        /// <param name="baseValue">�⺻��</param>
        /// <param name="initialCapacity">������ �÷����� �ʱ� �뷮 (���� ����ȭ)</param>
        /// <returns>���ο� Stat �ν��Ͻ�</returns>
        public static Stat Create(float baseValue = 0, int initialCapacity = 4)
        {
            var stat = new Stat
            {
                _baseValue = baseValue,
                _cachedValue = baseValue,
                _isDirty = true,
                _addModifiers = new StatModifierCollection(initialCapacity),
                _multiplyModifiers = new StatModifierCollection(initialCapacity)
            };

            return stat;
        }

        /// <summary>
        /// ���� ���� ���� �� (�⺻�� + ��� ������ ����)
        /// ������ ���� ���� ������� �ʾҴٸ� ĳ�õ� ���� ��ȯ
        /// </summary>
        public float Value
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                if (_isDirty)
                {
                    _cachedValue = CalculateFinalValue();
                    _isDirty = false;
                }
                return _cachedValue;
            }
        }

        /// <summary>
        /// �����ڰ� ������� ���� �⺻ ���� ��
        /// </summary>
        public float BaseValue
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _baseValue;
        }

        /// <summary>
        /// ���� ���� �⺻�� ��� �� ������ ���� ���
        /// (��: �⺻�� 100, ���簪 150�̸� 1.5 ��ȯ)
        /// </summary>
        public float Ratio
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                if (BaseValue == 0)
                    return 0;

                return Value / BaseValue;
            }
        }

        /// <summary>
        /// �����ڰ� ������� ���� �⺻ ���� ��
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetBaseValue(float value)
        {
            _baseValue = value;
            ChangeValueHandler();
        }

        /// <summary>
        /// ������ �⺻���� �����ϰ� ���� �̺�Ʈ�� �߻���Ŵ
        /// </summary>
        /// <param name="value">���ο� �⺻��</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private float CalculateFinalValue()
        {
            float finalValue = _baseValue;

            finalValue += _addModifiers.GetSum();
            finalValue *= _multiplyModifiers.GetMultiplicationFactor();

            return finalValue;
        }

        /// <summary>
        /// ���ȿ� �����ڸ� �߰� (����, �����, ��� ȿ�� ��)
        /// </summary>
        /// <param name="modifier">�߰��� ������</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddModifier(StatModifier modifier)
        {
            switch (modifier.ModifierType)
            {
                case EStatModifier.Add:
                    _addModifiers.Add(modifier.SetterID, modifier.Value);
                    break;
                case EStatModifier.Multiply:
                    _multiplyModifiers.Add(modifier.SetterID, modifier.Value);
                    break;
            }

            ChangeValueHandler();
        }

        /// <summary>
        /// Ư�� �����ڸ� ����
        /// </summary>
        /// <param name="modifier">������ ������</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void RemoveModifier(StatModifier modifier)
        {
            bool removed = false;

            switch (modifier.ModifierType)
            {
                case EStatModifier.Add:
                    removed = _addModifiers.Remove(modifier.SetterID);
                    break;
                case EStatModifier.Multiply:
                    removed = _multiplyModifiers.Remove(modifier.SetterID);
                    break;
            }

            if (removed)
                ChangeValueHandler();
        }

        /// <summary>
        /// Ư�� �ν��Ͻ� ID�κ��� �� ��� �����ڸ� ����
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void RemoveAllModifiersFromInstanceID(int instanceID)
        {
            var removed = _addModifiers.Remove(instanceID) || _multiplyModifiers.Remove(instanceID);

            if (removed)
                ChangeValueHandler();
        }

        /// <summary>
        /// ��� �����ڸ� �����ϰ� �⺻������ �ʱ�ȭ
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void RemoveAllModifiers()
        {
            var anyModifiers = false;

            if (_addModifiers.Count > 0)
            {
                _addModifiers.Clear();
                anyModifiers = true;
            }

            if (_multiplyModifiers.Count > 0)
            {
                _multiplyModifiers.Clear();
                anyModifiers = true;
            }

            if (anyModifiers)
            {
                ChangeValueHandler();
            }
        }

        private void ChangeValueHandler()
        {
            _isDirty = true;
            OnValueChanged?.Invoke(Value);
        }

        /// <summary>
        /// Stat�� float�� �Ͻ��� ��ȯ �����ϰ� ��
        /// ��: float damage = playerStat; // playerStat.Value�� ����
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator float(Stat stat) => stat.Value;
    }
}
