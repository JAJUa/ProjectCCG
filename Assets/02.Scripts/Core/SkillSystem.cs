using System.Collections.Generic;
using UnityEngine;

public class SkillSystem : MonoBehaviour
{
    private BattleSystem battleSystem;

    void Start()
    {
        battleSystem = GetComponent<BattleSystem>();
    }

    // ============================================
    // 스킬 트리거 - 시계상 계열
    // ============================================

    public void TriggerClockmasterSkills(CharacterData character, List<CharacterData> allies, List<CharacterData> enemies, bool isPlayerUnit)
    {
        string skillName = character.name;

        // 시간 공명
        if (skillName.Contains("시간 공명"))
        {
            character.currentSpeed = Mathf.Max(1, character.currentSpeed - 2);

            foreach (var ally in allies)
            {
                if (ally != character)
                {
                    ally.currentSpeed += 2;
                }
            }

            Debug.Log($"[시간 공명] 아군 속도 증가!");
        }

        // 시계태엽 병사
        else if (skillName.Contains("시계태엽 병사"))
        {
            float chance = 0.3f;
            if (Random.value < chance)
            {
                var spirit = FindSpirit(allies);
                if (spirit != null)
                {
                    // 신령의 다음 행동을 즉시 앞으로
                    Debug.Log($"[시계태엽 병사] {spirit.name}의 행동 앞당김!");
                    // 실제 구현: 행동 큐 조작
                }
            }
        }

        // 하늘이 정해준 운명
        else if (skillName.Contains("하늘이 정해준 운명"))
        {
            var spirit = FindSpirit(allies);
            if (spirit != null)
            {
                spirit.currentSpeed += 1;
                Debug.Log($"[운명] {spirit.name} 속도 증가!");
            }
        }

        // 시간 도약
        else if (skillName.Contains("시간 도약"))
        {
            var spirit = FindSpirit(allies);
            if (spirit != null)
            {
                // 토글 효과 구현 (버프로 상태 저장)
                bool isSpeedBoosted = HasBuff(spirit, BuffType.None); // 커스텀 버프 필요

                if (!isSpeedBoosted)
                {
                    spirit.currentSpeed *= 2;
                    Debug.Log($"[시간 도약] {spirit.name} 속도 2배!");
                }
                else
                {
                    spirit.currentSpeed /= 2;
                    Debug.Log($"[시간 도약] {spirit.name} 속도 복구");
                }
            }
        }

        // 시간 정지
        else if (skillName.Contains("시간 정지"))
        {
            int duration = 1; // n턴

            foreach (var unit in allies)
            {
                AddBuff(unit, BuffType.Stun, duration);
            }
            foreach (var unit in enemies)
            {
                AddBuff(unit, BuffType.Stun, duration);
            }

            Debug.Log($"[시간 정지] 모든 유닛 {duration}턴 행동 불가!");
        }

        // 시간 균열
        else if (skillName.Contains("시간 균열"))
        {
            float chance = 0.3f;
            if (Random.value < chance)
            {
                Debug.Log($"[시간 균열] 행동 순서 재배열!");
                // 실제 구현: 행동 큐 셔플
            }
        }
    }

    // ============================================
    // 스킬 트리거 - 도박사 계열
    // ============================================

    public void TriggerGamblerSkills(CharacterData character, List<CharacterData> allies, List<CharacterData> enemies)
    {
        string skillName = character.name;

        // 몰락한 귀족 (판매 시 골드)
        if (skillName.Contains("몰락한 귀족"))
        {
            // 판매 시 처리 - ShopSystem에서 처리됨
        }

        // 주인 잃은 병사 (판매 시 골드 증가)
        else if (skillName.Contains("주인 잃은 병사"))
        {
            // 판매 시 처리 - ShopSystem에서 처리됨
        }

        // 운명의 딜러 (확률 추가 공격)
        else if (skillName.Contains("운명의 딜러"))
        {
            float chance = 0.4f;

            void TryExtraAttack()
            {
                if (Random.value < chance)
                {
                    var target = GetRandomTarget(enemies);
                    if (target != null)
                    {
                        int damage = character.currentAttack;
                        target.currentHealth -= damage;
                        Debug.Log($"[운명의 딜러] 추가 공격! {target.name}에게 {damage} 데미지!");

                        // 추가 공격에도 확률 적용 (재귀)
                        TryExtraAttack();
                    }
                }
            }

            TryExtraAttack();
        }
    }

    // ============================================
    // 스킬 트리거 - 광전사 계열 (공격력 검사)
    // ============================================

    public void TriggerBerserkerSkills(CharacterData character, List<CharacterData> allies, List<CharacterData> enemies)
    {
        string skillName = character.name;
        var spirit = FindSpirit(allies);

        // 피의 수도승
        if (skillName.Contains("피의 수도승"))
        {
            character.currentAttack = Mathf.Max(1, character.currentAttack - 2);

            foreach (var ally in allies)
            {
                ally.currentAttack += 2;
            }

            Debug.Log($"[피의 수도승] 모든 아군 공격력 증가!");
        }

        // 광기의 성직자
        else if (skillName.Contains("광기의 성직자") && spirit != null)
        {
            character.currentAttack = spirit.currentAttack;
            Debug.Log($"[광기의 성직자] {spirit.name}의 공격력과 동기화!");
        }

        // 분노의 사제
        else if (skillName.Contains("분노의 사제"))
        {
            float chance = 0.3f;
            if (Random.value < chance)
            {
                var randomAlly = GetRandomTarget(allies);
                if (randomAlly != null)
                {
                    randomAlly.currentHealth += 2;
                    Debug.Log($"[분노의 사제] {randomAlly.name} 체력 증가!");
                }
            }
        }
    }

    // ============================================
    // 스킬 트리거 - 수호자 계열 (체력 검사)
    // ============================================

    public void TriggerGuardianSkills(CharacterData character, List<CharacterData> allies, List<CharacterData> enemies)
    {
        string skillName = character.name;
        var spirit = FindSpirit(allies);

        // 성벽의 수도승
        if (skillName.Contains("성벽의 수도승"))
        {
            character.currentHealth = Mathf.Max(1, character.currentHealth - 2);

            foreach (var ally in allies)
            {
                ally.currentHealth += 2;
            }

            Debug.Log($"[성벽의 수도승] 모든 아군 체력 증가!");
        }

        // 헌신의 성직자
        else if (skillName.Contains("헌신의 성직자") && spirit != null)
        {
            character.currentHealth = spirit.currentHealth;
            Debug.Log($"[헌신의 성직자] {spirit.name}의 체력과 동기화!");
        }

        // 수호의 사제
        else if (skillName.Contains("수호의 사제"))
        {
            float chance = 0.3f;
            if (Random.value < chance)
            {
                var randomAlly = GetRandomTarget(allies);
                if (randomAlly != null)
                {
                    randomAlly.currentHealth += 2;
                    Debug.Log($"[수호의 사제] {randomAlly.name} 체력 증가!");
                }
            }
        }

        // 수호의 신관 (사망 시)
        else if (skillName.Contains("수호의 신관") && character.currentHealth <= 0)
        {
            foreach (var ally in allies)
            {
                ally.currentHealth += 2;
            }
            Debug.Log($"[수호의 신관] 사망 시 모든 아군 체력 증가!");
        }
    }

    // ============================================
    // 스킬 트리거 - 질풍검사 계열 (속도 검사)
    // ============================================

    public void TriggerWindSwordsmanSkills(CharacterData character, List<CharacterData> allies, List<CharacterData> enemies)
    {
        string skillName = character.name;
        var spirit = FindSpirit(allies);

        // 시간의 성직자
        if (skillName.Contains("시간의 성직자") && spirit != null)
        {
            character.currentSpeed = spirit.currentSpeed;
            Debug.Log($"[시간의 성직자] {spirit.name}의 속도와 동기화!");
        }

        // 시간의 사제 (아군 재행동 시)
        else if (skillName.Contains("시간의 사제"))
        {
            // 재행동 감지 로직 필요
            Debug.Log($"[시간의 사제] 공격력 증가!");
        }

        // 질풍의 신관
        else if (skillName.Contains("질풍의 신관"))
        {
            float chance = 0.3f;
            if (Random.value < chance)
            {
                var randomAlly = GetRandomTarget(allies);
                if (randomAlly != null)
                {
                    Debug.Log($"[질풍의 신관] {randomAlly.name} 재행동!");
                    // 재행동 로직 구현 필요
                }
            }
        }

        // 순풍의 무녀
        else if (skillName.Contains("순풍의 무녀") && spirit != null)
        {
            float chance = 0.3f;
            if (Random.value < chance)
            {
                int attackReduction = Mathf.RoundToInt(spirit.currentAttack * 0.3f);
                spirit.currentAttack -= attackReduction;
                Debug.Log($"[순풍의 무녀] {spirit.name} 재행동 대신 공격력 감소!");
                // 재행동 부여 로직
            }
        }
    }

    // ============================================
    // 스킬 트리거 - 속성 마법사 계열
    // ============================================

    public void TriggerElementalMageSkills(CharacterData character, List<CharacterData> allies, List<CharacterData> enemies, BuffType elementType)
    {
        string skillName = character.name;

        // 냉결의 사제 / 불의 사제 / 뇌광의 사제
        if (skillName.Contains("사제"))
        {
            int rangeN = 2; // n칸
            int characterIndex = allies.IndexOf(character);

            for (int i = characterIndex; i < allies.Count && i < characterIndex + rangeN; i++)
            {
                if (!allies[i].attributes.Contains(GetAttributeString(elementType)))
                {
                    allies[i].attributes.Add(GetAttributeString(elementType));
                    Debug.Log($"[{skillName}] {allies[i].name}에게 {elementType} 속성 부여!");
                }
            }
        }

        // 냉기의 성직자 / 열기의 성직자 / 전류의 성직자
        else if (skillName.Contains("성직자"))
        {
            // 아군이 공격 시 디버프 부여
            // 이벤트 기반 처리 필요
        }

        // 빙결/화염/번개의 요정
        else if (skillName.Contains("요정"))
        {
            foreach (var enemy in enemies)
            {
                AddBuff(enemy, elementType, 2);
            }
            Debug.Log($"[{skillName}] 모든 적에게 {elementType} 부여!");
        }

        // 서리/불꽃/낙뢰의 성자
        else if (skillName.Contains("성자"))
        {
            // 특정 상태 적 공격 시 데미지 증가
            // 공격 시점에 체크 필요
        }

        // 빙결/화염 전도사
        else if (skillName.Contains("전도사"))
        {
            int debuffCount = CountDebuffs(enemies, elementType);
            float attackBonus = debuffCount * 0.1f; // 10% per stack

            foreach (var ally in allies)
            {
                ally.currentAttack = Mathf.RoundToInt(ally.currentAttack * (1 + attackBonus));
            }

            Debug.Log($"[{skillName}] {elementType} 상태 {debuffCount}명, 아군 공격력 {attackBonus * 100}% 증가!");
        }

        // 심판의 전도자 (번개 특수)
        else if (skillName.Contains("심판의 전도자"))
        {
            int targetCount = 2;
            for (int i = 0; i < targetCount; i++)
            {
                var randomEnemy = GetRandomTarget(enemies);
                if (randomEnemy != null)
                {
                    int damage = Mathf.RoundToInt(character.currentAttack * 0.5f);
                    randomEnemy.currentHealth -= damage;
                    AddBuff(randomEnemy, BuffType.Lightning, -1, 1);
                    Debug.Log($"[심판의 전도자] {randomEnemy.name}에게 번개 공격!");
                }
            }
        }

        // 폭뢰의 주교
        else if (skillName.Contains("폭뢰의 주교"))
        {
            foreach (var enemy in enemies)
            {
                if (HasBuff(enemy, BuffType.Stun))
                {
                    int damage = Mathf.RoundToInt(character.currentAttack * 1.5f);
                    enemy.currentHealth -= damage;
                    Debug.Log($"[폭뢰의 주교] 기절 상태 {enemy.name}에게 {damage} 추가 데미지!");
                }
            }
        }
    }

    // ============================================
    // 스킬 트리거 - 영혼술사 계열
    // ============================================

    public void TriggerSpiritistSkills(CharacterData character, List<CharacterData> allies, List<CharacterData> enemies)
    {
        string skillName = character.name;

        // 죽음의 목자
        if (skillName.Contains("죽음의 목자"))
        {
            int summonCount = 2;
            var lowestHPAllies = allies.OrderBy(a => a.currentHealth).Take(summonCount).ToList();

            foreach (var ally in lowestHPAllies)
            {
                CharacterData soul = ally.Clone();
                soul.name = ally.name + " (영혼)";
                soul.currentHealth = 1;
                soul.buffs.Add(new BuffData(BuffType.Soul));
                allies.Add(soul);

                Debug.Log($"[죽음의 목자] {soul.name} 소환!");
            }
        }

        // 영혼의 전도자
        else if (skillName.Contains("영혼의 전도자"))
        {
            int soulCount = allies.Count(a => HasBuff(a, BuffType.Soul));
            float attackBonus = soulCount * 0.1f;

            foreach (var ally in allies)
            {
                if (HasBuff(ally, BuffType.Soul))
                {
                    ally.currentAttack = Mathf.RoundToInt(ally.currentAttack * (1 + attackBonus));
                }
            }

            Debug.Log($"[영혼의 전도자] 영혼 {soulCount}명, 영혼 공격력 {attackBonus * 100}% 증가!");
        }

        // 혼령 인도자
        else if (skillName.Contains("혼령 인도자"))
        {
            // 영혼이 사망시 스탯 흡수
            // 사망 이벤트에서 처리
        }

        // 영겁의 사제
        else if (skillName.Contains("영겁의 사제") && character.currentHealth <= 0)
        {
            int summonCount = 3;
            int statPerSoul = character.currentAttack / summonCount;

            for (int i = 0; i < summonCount; i++)
            {
                CharacterData soul = character.Clone();
                soul.name = character.name + " (분신)";
                soul.currentAttack = statPerSoul;
                soul.currentHealth = 1;
                soul.buffs.Add(new BuffData(BuffType.Soul));
                allies.Add(soul);
            }

            Debug.Log($"[영겁의 사제] 사망 시 {summonCount}명의 영혼 소환!");
        }
    }

    // ============================================
    // 유틸리티 함수
    // ============================================

    private CharacterData FindSpirit(List<CharacterData> characters)
    {
        return characters.Find(c => c.type == CharacterType.Spirit);
    }

    private CharacterData GetRandomTarget(List<CharacterData> targets)
    {
        var aliveTargets = targets.Where(t => t.currentHealth > 0).ToList();
        if (aliveTargets.Count == 0) return null;

        return aliveTargets[Random.Range(0, aliveTargets.Count)];
    }

    private bool HasBuff(CharacterData character, BuffType type)
    {
        return character.buffs.Any(b => b.type == type);
    }

    private void AddBuff(CharacterData character, BuffType type, int duration, int stack = 1)
    {
        var existingBuff = character.buffs.Find(b => b.type == type);

        if (existingBuff != null)
        {
            existingBuff.stack += stack;
            if (duration > 0)
                existingBuff.duration = Mathf.Max(existingBuff.duration, duration);
        }
        else
        {
            character.buffs.Add(new BuffData(type, duration, stack));
        }
    }

    private int CountDebuffs(List<CharacterData> characters, BuffType type)
    {
        return characters.Count(c => HasBuff(c, type));
    }

    private string GetAttributeString(BuffType type)
    {
        switch (type)
        {
            case BuffType.Freeze: return "Ice";
            case BuffType.Burn: return "Fire";
            case BuffType.Lightning: return "Lightning";
            default: return "";
        }
    }
}

// System.Linq 확장
public static class LinqExtensions
{
    public static IEnumerable<T> OrderBy<T, TKey>(this IEnumerable<T> source, System.Func<T, TKey> keySelector)
    {
        return System.Linq.Enumerable.OrderBy(source, keySelector);
    }

    public static IEnumerable<T> Take<T>(this IEnumerable<T> source, int count)
    {
        return System.Linq.Enumerable.Take(source, count);
    }

    public static List<T> ToList<T>(this IEnumerable<T> source)
    {
        return new List<T>(source);
    }

    public static IEnumerable<T> Where<T>(this IEnumerable<T> source, System.Func<T, bool> predicate)
    {
        return System.Linq.Enumerable.Where(source, predicate);
    }

    public static bool Any<T>(this IEnumerable<T> source, System.Func<T, bool> predicate)
    {
        return System.Linq.Enumerable.Any(source, predicate);
    }

    public static int Count<T>(this IEnumerable<T> source, System.Func<T, bool> predicate)
    {
        return System.Linq.Enumerable.Count(source, predicate);
    }
}