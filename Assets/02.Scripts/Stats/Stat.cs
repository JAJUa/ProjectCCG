using System.Runtime.CompilerServices;
using System;

namespace tools.Stats
{
    /// <summary>
    /// 게임 내 스탯(체력, 공격력, 방어력 등)을 관리하는 클래스
    /// 기본값에 다양한 수정자(버프/디버프)를 적용하여 최종 값을 계산
    /// </summary>
    [Serializable]
    public class Stat
    {
        // 스탯 값 변경 시 호출
        public delegate void OnValueChangeDelegate(float value);
        // 스탯 값 변경 시 호출2
        public event OnValueChangeDelegate OnValueChanged;

        private StatModifierCollection _addModifiers; // 더하기
        private StatModifierCollection _multiplyModifiers; // 곱하기

        private float _baseValue; // 기본값
        private float _cachedValue;
        private bool _isDirty;

        /// <summary>
        /// Stat 인스턴스를 생성하는 팩토리 메서드
        /// </summary>
        /// <param name="baseValue">기본값</param>
        /// <param name="initialCapacity">수정자 컬렉션의 초기 용량 (성능 최적화)</param>
        /// <returns>새로운 Stat 인스턴스</returns>
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
        /// 최종 계산된 스탯 값 (기본값 + 모든 수정자 적용)
        /// 성능을 위해 값이 변경되지 않았다면 캐시된 값을 반환
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
        /// 수정자가 적용되지 않은 기본 스탯 값
        /// </summary>
        public float BaseValue
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _baseValue;
        }

        /// <summary>
        /// 현재 값이 기본값 대비 몇 배인지 비율 계산
        /// (예: 기본값 100, 현재값 150이면 1.5 반환)
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
        /// 수정자가 적용되지 않은 기본 스탯 값
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetBaseValue(float value)
        {
            _baseValue = value;
            ChangeValueHandler();
        }

        /// <summary>
        /// 스탯의 기본값을 설정하고 변경 이벤트를 발생시킴
        /// </summary>
        /// <param name="value">새로운 기본값</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private float CalculateFinalValue()
        {
            float finalValue = _baseValue;

            finalValue += _addModifiers.GetSum();
            finalValue *= _multiplyModifiers.GetMultiplicationFactor();

            return finalValue;
        }

        /// <summary>
        /// 스탯에 수정자를 추가 (버프, 디버프, 장비 효과 등)
        /// </summary>
        /// <param name="modifier">추가할 수정자</param>
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
        /// 특정 수정자를 제거
        /// </summary>
        /// <param name="modifier">제거할 수정자</param>
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
        /// 특정 인스턴스 ID로부터 온 모든 수정자를 제거
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void RemoveAllModifiersFromInstanceID(int instanceID)
        {
            var removed = _addModifiers.Remove(instanceID) || _multiplyModifiers.Remove(instanceID);

            if (removed)
                ChangeValueHandler();
        }

        /// <summary>
        /// 모든 수정자를 제거하고 기본값으로 초기화
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
        /// Stat을 float로 암시적 변환 가능하게 함
        /// 예: float damage = playerStat; // playerStat.Value와 동일
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator float(Stat stat) => stat.Value;
    }
}
