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
    // ��ų Ʈ���� - �ð�� �迭
    // ============================================

    public void TriggerClockmasterSkills(CharacterData character, List<CharacterData> allies, List<CharacterData> enemies, bool isPlayerUnit)
    {
        string skillName = character.name;

        // �ð� ����
        if (skillName.Contains("�ð� ����"))
        {
            character.currentSpeed = Mathf.Max(1, character.currentSpeed - 2);

            foreach (var ally in allies)
            {
                if (ally != character)
                {
                    ally.currentSpeed += 2;
                }
            }

            Debug.Log($"[�ð� ����] �Ʊ� �ӵ� ����!");
        }

        // �ð��¿� ����
        else if (skillName.Contains("�ð��¿� ����"))
        {
            float chance = 0.3f;
            if (Random.value < chance)
            {
                var spirit = FindSpirit(allies);
                if (spirit != null)
                {
                    // �ŷ��� ���� �ൿ�� ��� ������
                    Debug.Log($"[�ð��¿� ����] {spirit.name}�� �ൿ �մ��!");
                    // ���� ����: �ൿ ť ����
                }
            }
        }

        // �ϴ��� ������ ���
        else if (skillName.Contains("�ϴ��� ������ ���"))
        {
            var spirit = FindSpirit(allies);
            if (spirit != null)
            {
                spirit.currentSpeed += 1;
                Debug.Log($"[���] {spirit.name} �ӵ� ����!");
            }
        }

        // �ð� ����
        else if (skillName.Contains("�ð� ����"))
        {
            var spirit = FindSpirit(allies);
            if (spirit != null)
            {
                // ��� ȿ�� ���� (������ ���� ����)
                bool isSpeedBoosted = HasBuff(spirit, BuffType.None); // Ŀ���� ���� �ʿ�

                if (!isSpeedBoosted)
                {
                    spirit.currentSpeed *= 2;
                    Debug.Log($"[�ð� ����] {spirit.name} �ӵ� 2��!");
                }
                else
                {
                    spirit.currentSpeed /= 2;
                    Debug.Log($"[�ð� ����] {spirit.name} �ӵ� ����");
                }
            }
        }

        // �ð� ����
        else if (skillName.Contains("�ð� ����"))
        {
            int duration = 1; // n��

            foreach (var unit in allies)
            {
                AddBuff(unit, BuffType.Stun, duration);
            }
            foreach (var unit in enemies)
            {
                AddBuff(unit, BuffType.Stun, duration);
            }

            Debug.Log($"[�ð� ����] ��� ���� {duration}�� �ൿ �Ұ�!");
        }

        // �ð� �տ�
        else if (skillName.Contains("�ð� �տ�"))
        {
            float chance = 0.3f;
            if (Random.value < chance)
            {
                Debug.Log($"[�ð� �տ�] �ൿ ���� ��迭!");
                // ���� ����: �ൿ ť ����
            }
        }
    }

    // ============================================
    // ��ų Ʈ���� - ���ڻ� �迭
    // ============================================

    public void TriggerGamblerSkills(CharacterData character, List<CharacterData> allies, List<CharacterData> enemies)
    {
        string skillName = character.name;

        // ������ ���� (�Ǹ� �� ���)
        if (skillName.Contains("������ ����"))
        {
            // �Ǹ� �� ó�� - ShopSystem���� ó����
        }

        // ���� ���� ���� (�Ǹ� �� ��� ����)
        else if (skillName.Contains("���� ���� ����"))
        {
            // �Ǹ� �� ó�� - ShopSystem���� ó����
        }

        // ����� ���� (Ȯ�� �߰� ����)
        else if (skillName.Contains("����� ����"))
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
                        Debug.Log($"[����� ����] �߰� ����! {target.name}���� {damage} ������!");

                        // �߰� ���ݿ��� Ȯ�� ���� (���)
                        TryExtraAttack();
                    }
                }
            }

            TryExtraAttack();
        }
    }

    // ============================================
    // ��ų Ʈ���� - ������ �迭 (���ݷ� �˻�)
    // ============================================

    public void TriggerBerserkerSkills(CharacterData character, List<CharacterData> allies, List<CharacterData> enemies)
    {
        string skillName = character.name;
        var spirit = FindSpirit(allies);

        // ���� ������
        if (skillName.Contains("���� ������"))
        {
            character.currentAttack = Mathf.Max(1, character.currentAttack - 2);

            foreach (var ally in allies)
            {
                ally.currentAttack += 2;
            }

            Debug.Log($"[���� ������] ��� �Ʊ� ���ݷ� ����!");
        }

        // ������ ������
        else if (skillName.Contains("������ ������") && spirit != null)
        {
            character.currentAttack = spirit.currentAttack;
            Debug.Log($"[������ ������] {spirit.name}�� ���ݷ°� ����ȭ!");
        }

        // �г��� ����
        else if (skillName.Contains("�г��� ����"))
        {
            float chance = 0.3f;
            if (Random.value < chance)
            {
                var randomAlly = GetRandomTarget(allies);
                if (randomAlly != null)
                {
                    randomAlly.currentHealth += 2;
                    Debug.Log($"[�г��� ����] {randomAlly.name} ü�� ����!");
                }
            }
        }
    }

    // ============================================
    // ��ų Ʈ���� - ��ȣ�� �迭 (ü�� �˻�)
    // ============================================

    public void TriggerGuardianSkills(CharacterData character, List<CharacterData> allies, List<CharacterData> enemies)
    {
        string skillName = character.name;
        var spirit = FindSpirit(allies);

        // ������ ������
        if (skillName.Contains("������ ������"))
        {
            character.currentHealth = Mathf.Max(1, character.currentHealth - 2);

            foreach (var ally in allies)
            {
                ally.currentHealth += 2;
            }

            Debug.Log($"[������ ������] ��� �Ʊ� ü�� ����!");
        }

        // ����� ������
        else if (skillName.Contains("����� ������") && spirit != null)
        {
            character.currentHealth = spirit.currentHealth;
            Debug.Log($"[����� ������] {spirit.name}�� ü�°� ����ȭ!");
        }

        // ��ȣ�� ����
        else if (skillName.Contains("��ȣ�� ����"))
        {
            float chance = 0.3f;
            if (Random.value < chance)
            {
                var randomAlly = GetRandomTarget(allies);
                if (randomAlly != null)
                {
                    randomAlly.currentHealth += 2;
                    Debug.Log($"[��ȣ�� ����] {randomAlly.name} ü�� ����!");
                }
            }
        }

        // ��ȣ�� �Ű� (��� ��)
        else if (skillName.Contains("��ȣ�� �Ű�") && character.currentHealth <= 0)
        {
            foreach (var ally in allies)
            {
                ally.currentHealth += 2;
            }
            Debug.Log($"[��ȣ�� �Ű�] ��� �� ��� �Ʊ� ü�� ����!");
        }
    }

    // ============================================
    // ��ų Ʈ���� - ��ǳ�˻� �迭 (�ӵ� �˻�)
    // ============================================

    public void TriggerWindSwordsmanSkills(CharacterData character, List<CharacterData> allies, List<CharacterData> enemies)
    {
        string skillName = character.name;
        var spirit = FindSpirit(allies);

        // �ð��� ������
        if (skillName.Contains("�ð��� ������") && spirit != null)
        {
            character.currentSpeed = spirit.currentSpeed;
            Debug.Log($"[�ð��� ������] {spirit.name}�� �ӵ��� ����ȭ!");
        }

        // �ð��� ���� (�Ʊ� ���ൿ ��)
        else if (skillName.Contains("�ð��� ����"))
        {
            // ���ൿ ���� ���� �ʿ�
            Debug.Log($"[�ð��� ����] ���ݷ� ����!");
        }

        // ��ǳ�� �Ű�
        else if (skillName.Contains("��ǳ�� �Ű�"))
        {
            float chance = 0.3f;
            if (Random.value < chance)
            {
                var randomAlly = GetRandomTarget(allies);
                if (randomAlly != null)
                {
                    Debug.Log($"[��ǳ�� �Ű�] {randomAlly.name} ���ൿ!");
                    // ���ൿ ���� ���� �ʿ�
                }
            }
        }

        // ��ǳ�� ����
        else if (skillName.Contains("��ǳ�� ����") && spirit != null)
        {
            float chance = 0.3f;
            if (Random.value < chance)
            {
                int attackReduction = Mathf.RoundToInt(spirit.currentAttack * 0.3f);
                spirit.currentAttack -= attackReduction;
                Debug.Log($"[��ǳ�� ����] {spirit.name} ���ൿ ��� ���ݷ� ����!");
                // ���ൿ �ο� ����
            }
        }
    }

    // ============================================
    // ��ų Ʈ���� - �Ӽ� ������ �迭
    // ============================================

    public void TriggerElementalMageSkills(CharacterData character, List<CharacterData> allies, List<CharacterData> enemies, BuffType elementType)
    {
        string skillName = character.name;

        // �ð��� ���� / ���� ���� / ������ ����
        if (skillName.Contains("����"))
        {
            int rangeN = 2; // nĭ
            int characterIndex = allies.IndexOf(character);

            for (int i = characterIndex; i < allies.Count && i < characterIndex + rangeN; i++)
            {
                if (!allies[i].attributes.Contains(GetAttributeString(elementType)))
                {
                    allies[i].attributes.Add(GetAttributeString(elementType));
                    Debug.Log($"[{skillName}] {allies[i].name}���� {elementType} �Ӽ� �ο�!");
                }
            }
        }

        // �ñ��� ������ / ������ ������ / ������ ������
        else if (skillName.Contains("������"))
        {
            // �Ʊ��� ���� �� ����� �ο�
            // �̺�Ʈ ��� ó�� �ʿ�
        }

        // ����/ȭ��/������ ����
        else if (skillName.Contains("����"))
        {
            foreach (var enemy in enemies)
            {
                AddBuff(enemy, elementType, 2);
            }
            Debug.Log($"[{skillName}] ��� ������ {elementType} �ο�!");
        }

        // ����/�Ҳ�/������ ����
        else if (skillName.Contains("����"))
        {
            // Ư�� ���� �� ���� �� ������ ����
            // ���� ������ üũ �ʿ�
        }

        // ����/ȭ�� ������
        else if (skillName.Contains("������"))
        {
            int debuffCount = CountDebuffs(enemies, elementType);
            float attackBonus = debuffCount * 0.1f; // 10% per stack

            foreach (var ally in allies)
            {
                ally.currentAttack = Mathf.RoundToInt(ally.currentAttack * (1 + attackBonus));
            }

            Debug.Log($"[{skillName}] {elementType} ���� {debuffCount}��, �Ʊ� ���ݷ� {attackBonus * 100}% ����!");
        }

        // ������ ������ (���� Ư��)
        else if (skillName.Contains("������ ������"))
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
                    Debug.Log($"[������ ������] {randomEnemy.name}���� ���� ����!");
                }
            }
        }

        // ������ �ֱ�
        else if (skillName.Contains("������ �ֱ�"))
        {
            foreach (var enemy in enemies)
            {
                if (HasBuff(enemy, BuffType.Stun))
                {
                    int damage = Mathf.RoundToInt(character.currentAttack * 1.5f);
                    enemy.currentHealth -= damage;
                    Debug.Log($"[������ �ֱ�] ���� ���� {enemy.name}���� {damage} �߰� ������!");
                }
            }
        }
    }

    // ============================================
    // ��ų Ʈ���� - ��ȥ���� �迭
    // ============================================

    public void TriggerSpiritistSkills(CharacterData character, List<CharacterData> allies, List<CharacterData> enemies)
    {
        string skillName = character.name;

        // ������ ����
        if (skillName.Contains("������ ����"))
        {
            int summonCount = 2;
            var lowestHPAllies = allies.OrderBy(a => a.currentHealth).Take(summonCount).ToList();

            foreach (var ally in lowestHPAllies)
            {
                CharacterData soul = ally.Clone();
                soul.name = ally.name + " (��ȥ)";
                soul.currentHealth = 1;
                soul.buffs.Add(new BuffData(BuffType.Soul));
                allies.Add(soul);

                Debug.Log($"[������ ����] {soul.name} ��ȯ!");
            }
        }

        // ��ȥ�� ������
        else if (skillName.Contains("��ȥ�� ������"))
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

            Debug.Log($"[��ȥ�� ������] ��ȥ {soulCount}��, ��ȥ ���ݷ� {attackBonus * 100}% ����!");
        }

        // ȥ�� �ε���
        else if (skillName.Contains("ȥ�� �ε���"))
        {
            // ��ȥ�� ����� ���� ���
            // ��� �̺�Ʈ���� ó��
        }

        // ������ ����
        else if (skillName.Contains("������ ����") && character.currentHealth <= 0)
        {
            int summonCount = 3;
            int statPerSoul = character.currentAttack / summonCount;

            for (int i = 0; i < summonCount; i++)
            {
                CharacterData soul = character.Clone();
                soul.name = character.name + " (�н�)";
                soul.currentAttack = statPerSoul;
                soul.currentHealth = 1;
                soul.buffs.Add(new BuffData(BuffType.Soul));
                allies.Add(soul);
            }

            Debug.Log($"[������ ����] ��� �� {summonCount}���� ��ȥ ��ȯ!");
        }
    }

    // ============================================
    // ��ƿ��Ƽ �Լ�
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

// System.Linq Ȯ��
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