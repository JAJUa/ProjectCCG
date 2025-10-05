using System.Runtime.CompilerServices;
using System;

/// <summary>
/// 스탯 수정자들을 효율적으로 관리하는 구조체
/// 배열 기반으로 구현되어 메모리 효율성과 성능을 최적화
/// </summary>
[Serializable]
public struct StatModifierCollection
{
    /// <summary>
    /// 수정자 엔트리를 나타내는 내부 구조체
    /// </summary>
    private struct ModifierEntry
    {
        public readonly int InstanceID;
        public float Value;
        public bool IsActive;

        /// <summary>
        /// ModifierEntry 생성자
        /// </summary>
        /// <param name="instanceID">인스턴스 ID</param>
        /// <param name="value">수정자 값</param>
        public ModifierEntry(int instanceID, float value)
        {
            InstanceID = instanceID;
            Value = value;
            IsActive = true;
        }
    }

    // ModifierEntry 저장 배열들
    private ModifierEntry[] _entries; 
    private int _count;
    private int _capacity;

    /// <summary>
    /// StatModifierCollection 생성자
    /// </summary>
    /// <param name="initialCapacity">초기 배열 용량</param>
    public StatModifierCollection(int initialCapacity)
    {
        _entries = new ModifierEntry[initialCapacity];
        _count = 0;
        _capacity = initialCapacity;
    }

    /// <summary>
    /// 수정자를 추가하거나 기존 수정자의 값을 업데이트
    /// 같은 인스턴스 ID가 있으면 값을 업데이트하고, 없으면 새로 추가
    /// </summary>
    /// <param name="instanceID">인스턴스 ID</param>
    /// <param name="value">수정자 값</param>
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
    /// 배열 크기를 두 배로 확장하는 내부 메서드
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
    /// 특정 인스턴스 ID의 수정자를 제거
    /// 실제로 배열에서 삭제하지 않고 IsActive를 false로 설정
    /// </summary>
    /// <param name="instanceID">제거할 인스턴스 ID</param>
    /// <returns>제거 성공 여부</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Remove(int instanceID)
    {
        for (int i = 0; i < _count; i++)
        {
            if (_entries[i].IsActive && _entries[i].InstanceID == instanceID)
            {
                _entries[i].IsActive = false;
                // 배열 끝부분이면 카운트 감소
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
    /// 모든 수정자를 비활성화 (실제 삭제 X)
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
    /// 활성화된 모든 수정자 값의 합계를 계산 (더하기 방식 수정자용)
    /// </summary>
    /// <returns>수정자들의 합계</returns>
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
    /// 활성화된 모든 수정자의 곱셈 결과를 계산 (곱하기 방식 수정자용)
    /// </summary>
    /// <returns>수정자들의 곱셈 결과</returns>
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
    /// 현재 활성화된 수정자의 개수
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
    /// 디버그용 문자열 표현
    /// </summary>
    /// <returns>컬렉션 상태 정보</returns>
    public override string ToString()
    {
        return $"StatModifierCollection: {Count} active modifiers out of {_count} total entries";
    }
}
