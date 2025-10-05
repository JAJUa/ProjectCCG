using System.Runtime.CompilerServices;
using System;

/// <summary>
/// ���� �����ڵ��� ȿ�������� �����ϴ� ����ü
/// �迭 ������� �����Ǿ� �޸� ȿ������ ������ ����ȭ
/// </summary>
[Serializable]
public struct StatModifierCollection
{
    /// <summary>
    /// ������ ��Ʈ���� ��Ÿ���� ���� ����ü
    /// </summary>
    private struct ModifierEntry
    {
        public readonly int InstanceID;
        public float Value;
        public bool IsActive;

        /// <summary>
        /// ModifierEntry ������
        /// </summary>
        /// <param name="instanceID">�ν��Ͻ� ID</param>
        /// <param name="value">������ ��</param>
        public ModifierEntry(int instanceID, float value)
        {
            InstanceID = instanceID;
            Value = value;
            IsActive = true;
        }
    }

    // ModifierEntry ���� �迭��
    private ModifierEntry[] _entries; 
    private int _count;
    private int _capacity;

    /// <summary>
    /// StatModifierCollection ������
    /// </summary>
    /// <param name="initialCapacity">�ʱ� �迭 �뷮</param>
    public StatModifierCollection(int initialCapacity)
    {
        _entries = new ModifierEntry[initialCapacity];
        _count = 0;
        _capacity = initialCapacity;
    }

    /// <summary>
    /// �����ڸ� �߰��ϰų� ���� �������� ���� ������Ʈ
    /// ���� �ν��Ͻ� ID�� ������ ���� ������Ʈ�ϰ�, ������ ���� �߰�
    /// </summary>
    /// <param name="instanceID">�ν��Ͻ� ID</param>
    /// <param name="value">������ ��</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Add(int instanceID, float value)
    {
        for (int i = 0; i < _capacity; i++)
        {
            if (i < _count && _entries[i].IsActive && _entries[i].InstanceID == instanceID)
            {
                _entries[i].Value = value;
                return;
            }

            if (i == _count || !_entries[i].IsActive)
            {
                if (i == _count && _count >= _capacity)
                {
                    Resize();
                }

                _entries[i] = new ModifierEntry(instanceID, value);
                if (i == _count) _count++;
                return;
            }
        }
    }

    /// <summary>
    /// �迭 ũ�⸦ �� ��� Ȯ���ϴ� ���� �޼���
    /// </summary>
    private void Resize()
    {
        var newCapacity = _capacity * 2;
        var newEntries = new ModifierEntry[newCapacity];

        Array.Copy(_entries, newEntries, _capacity);

        _entries = newEntries;
        _capacity = newCapacity;
    }

    /// <summary>
    /// Ư�� �ν��Ͻ� ID�� �����ڸ� ����
    /// ������ �迭���� �������� �ʰ� IsActive�� false�� ����
    /// </summary>
    /// <param name="instanceID">������ �ν��Ͻ� ID</param>
    /// <returns>���� ���� ����</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Remove(int instanceID)
    {
        for (int i = 0; i < _count; i++)
        {
            if (_entries[i].IsActive && _entries[i].InstanceID == instanceID)
            {
                _entries[i].IsActive = false;
                // �迭 ���κ��̸� ī��Ʈ ����
                if (i == _count - 1)
                {
                    _count--;
                }

                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// ��� �����ڸ� ��Ȱ��ȭ (���� ���� X)
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Clear()
    {
        for (int i = 0; i < _count; i++)
        {
            _entries[i].IsActive = false;
        }

        _count = 0;
    }

    /// <summary>
    /// Ȱ��ȭ�� ��� ������ ���� �հ踦 ��� (���ϱ� ��� �����ڿ�)
    /// </summary>
    /// <returns>�����ڵ��� �հ�</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public float GetSum()
    {
        var sum = 0f;
        for (int i = 0; i < _count; i++)
        {
            if (_entries[i].IsActive)
            {
                sum += _entries[i].Value;
            }
        }

        return sum;
    }

    /// <summary>
    /// Ȱ��ȭ�� ��� �������� ���� ����� ��� (���ϱ� ��� �����ڿ�)
    /// </summary>
    /// <returns>�����ڵ��� ���� ���</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public float GetMultiplicationFactor()
    {
        var factor = 1f;
        for (int i = 0; i < _count; i++)
        {
            if (_entries[i].IsActive)
            {
                factor *= _entries[i].Value;
            }
        }

        return factor;
    }

    /// <summary>
    /// ���� Ȱ��ȭ�� �������� ����
    /// </summary>
    public int Count
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            int activeCount = 0;
            for (int i = 0; i < _count; i++)
            {
                if (_entries[i].IsActive)
                {
                    activeCount++;
                }
            }

            return activeCount;
        }
    }

    /// <summary>
    /// ����׿� ���ڿ� ǥ��
    /// </summary>
    /// <returns>�÷��� ���� ����</returns>
    public override string ToString()
    {
        return $"StatModifierCollection: {Count} active modifiers out of {_count} total entries";
    }
}
