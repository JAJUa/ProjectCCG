using System;
using System.Collections.Generic;
using UnityEngine;

// ============================================
// ������ ����
// ============================================

public enum CharacterType
{
    Spirit,      // �ŷ�
    Follower     // �ŵ���
}

public enum EvolutionType
{
    None,
    Swordsman,        // �˻�
    BerserkerSwordsman,  // ������ (���ݷ�)
    GuardianSwordsman,   // ��ȣ�� (ü��)
    WindSwordsman,       // ��ǳ�˻� (�ӵ�)
    Mage,            // ������
    IceMage,         // �������
    FireMage,        // ȭ������
    LightningMage,   // ��������
    Researcher,      // ������
    Alchemist,       // ���ݼ���
    Spiritist,       // ��ȥ����
    Negotiator,      // ������
    Clockmaker,      // �ð��
    Gambler          // ���ڻ�
}

public enum BuffType
{
    None,
    Freeze,      // ���� (�ӵ� ����)
    Burn,        // ȭ�� (�ൿ�� ������)
    Lightning,   // ���� (3��ø�� ����)
    Soul,        // ��ȥ (�Ϸ����)
    Madness,     // ���� (���ݷ� ����, ���� ����)
    Stun         // ���� (�ӵ� 0)
}

// ============================================
// ĳ���� ������ Ŭ����
// ============================================

[Serializable]
public class CharacterData
{
    public string id;
    public string name;
    public CharacterType type;
    public EvolutionType evolutionType;

    // �⺻ ����
    public int buyCost;
    public int baseAttack;
    public int baseHealth;
    public int baseSpeed;

    // ���� ����
    public int currentAttack;
    public int currentHealth;
    public int currentSpeed;

    // ����/�����
    public List<BuffData> buffs = new List<BuffData>();

    // ��ȭ ���� ����
    public int attackThreshold;  // ��ȭ�� ���� ���ݷ� �Ӱ谪
    public int healthThreshold;  // ��ȭ�� ���� ü�� �Ӱ谪
    public int speedThreshold;   // ��ȭ�� ���� �ӵ� �Ӱ谪

    // Ư�� ��ų
    public string skillDescription;
    public List<string> attributes = new List<string>(); // ��: "Ice", "Fire", "Lightning"

    public CharacterData Clone()
    {
        CharacterData clone = new CharacterData
        {
            id = this.id,
            name = this.name,
            type = this.type,
            evolutionType = this.evolutionType,
            buyCost = this.buyCost,
            baseAttack = this.baseAttack,
            baseHealth = this.baseHealth,
            baseSpeed = this.baseSpeed,
            currentAttack = this.currentAttack,
            currentHealth = this.currentHealth,
            currentSpeed = this.currentSpeed,
            attackThreshold = this.attackThreshold,
            healthThreshold = this.healthThreshold,
            speedThreshold = this.speedThreshold,
            skillDescription = this.skillDescription,
            attributes = new List<string>(this.attributes),
            buffs = new List<BuffData>()
        };

        foreach (var buff in this.buffs)
        {
            clone.buffs.Add(new BuffData(buff.type, buff.duration, buff.stack));
        }

        return clone;
    }
}

// ============================================
// ���� ������
// ============================================

[Serializable]
public class BuffData
{
    public BuffType type;
    public int duration;
    public int stack;

    public BuffData(BuffType type, int duration = -1, int stack = 1)
    {
        this.type = type;
        this.duration = duration;
        this.stack = stack;
    }
}

// ============================================
// �÷��̾� �� ������
// ============================================

[Serializable]
public class PlayerDeckData
{
    public string playerId;
    public List<CharacterData> ownedCharacters = new List<CharacterData>();
    public List<CharacterData> currentFormation = new List<CharacterData>(); // �ִ� 6��
    public int gold;
    public int health;
    public int round;

    public PlayerDeckData()
    {
        gold = 10;
        health = 100;
        round = 1;
    }
}

// ============================================
// ��Ī ������
// ============================================

[Serializable]
public class MatchData
{
    public string matchId;
    public string player1Id;
    public string player2Id;
    public PlayerDeckData player1Deck;
    public PlayerDeckData player2Deck;
    public int currentRound;
    public string currentPhase; // "Shop", "Battle", "Result"
    public string winnerId;
}

// ============================================
// ���� ������
// ============================================

[Serializable]
public class ShopData
{
    public List<CharacterData> availableCharacters = new List<CharacterData>();
    public CharacterData availableSpirit; // 2���� ���� ����
    public int refreshCost = 1;
}

// ============================================
// ���� ���
// ============================================

[Serializable]
public class BattleResult
{
    public bool isWin;
    public int damageDealt;
    public int goldEarned;
    public List<string> battleLog = new List<string>();
}

// ============================================
// ���� ���� ������
// ============================================

[Serializable]
public class GameConfig
{
    // ���� ����
    public int shopSlotCount = 5;
    public int spiritUnlockRound = 2;
    public int maxFormationSlots = 6;

    // ��� ����
    public int winGold = 5;
    public int loseGold = 3;
    public int startGold = 10;

    // ü�� ����
    public int startHealth = 100;
    public int damagePerLoss = 10;

    // ���� ����
    public float actionDelay = 0.5f;
    public int stunLightningStack = 3; // ���� 3��ø�� ����
}

// ============================================
// �����ͺ��̽� (����)
// ============================================

public static class CharacterDatabase
{
    public static List<CharacterData> GetAllCharacters()
    {
        return new List<CharacterData>
        {
            // �˻� �迭
            new CharacterData
            {
                id = "swordsman_basic",
                name = "�˻�",
                type = CharacterType.Follower,
                evolutionType = EvolutionType.Swordsman,
                buyCost = 3,
                baseAttack = 10,
                baseHealth = 15,
                baseSpeed = 8,
                currentAttack = 10,
                currentHealth = 15,
                currentSpeed = 8,
                attackThreshold = 20,
                healthThreshold = 30,
                speedThreshold = 15,
                skillDescription = "���� ���� �޼��� ��ȭ",
                attributes = new List<string>()
            },
            
            // ������ �迭
            new CharacterData
            {
                id = "mage_basic",
                name = "������",
                type = CharacterType.Follower,
                evolutionType = EvolutionType.Mage,
                buyCost = 3,
                baseAttack = 8,
                baseHealth = 10,
                baseSpeed = 10,
                currentAttack = 8,
                currentHealth = 10,
                currentSpeed = 10,
                skillDescription = "�Ʊ��� ���� ��� �� ���ݷ� ����",
                attributes = new List<string>()
            },
            
            // ������� �ŵ���
            new CharacterData
            {
                id = "ice_follower",
                name = "������ �ŵ���",
                type = CharacterType.Follower,
                evolutionType = EvolutionType.IceMage,
                buyCost = 2,
                baseAttack = 5,
                baseHealth = 8,
                baseSpeed = 7,
                currentAttack = 5,
                currentHealth = 8,
                currentSpeed = 7,
                skillDescription = "���ݿ� ����Ӽ� �ο�",
                attributes = new List<string> { "Ice" }
            },
            
            // ȭ������ �ŵ���
            new CharacterData
            {
                id = "fire_follower",
                name = "ȭ���� �ŵ���",
                type = CharacterType.Follower,
                evolutionType = EvolutionType.FireMage,
                buyCost = 2,
                baseAttack = 6,
                baseHealth = 7,
                baseSpeed = 7,
                currentAttack = 6,
                currentHealth = 7,
                currentSpeed = 7,
                skillDescription = "���ݿ� ȭ���Ӽ� �ο�",
                attributes = new List<string> { "Fire" }
            },
            
            // �������� �ŵ���
            new CharacterData
            {
                id = "lightning_follower",
                name = "������ �ŵ���",
                type = CharacterType.Follower,
                evolutionType = EvolutionType.LightningMage,
                buyCost = 2,
                baseAttack = 7,
                baseHealth = 6,
                baseSpeed = 8,
                currentAttack = 7,
                currentHealth = 6,
                currentSpeed = 8,
                skillDescription = "���ݿ� �����Ӽ� �ο�",
                attributes = new List<string> { "Lightning" }
            },
            
            // ������
            new CharacterData
            {
                id = "researcher_basic",
                name = "������",
                type = CharacterType.Follower,
                evolutionType = EvolutionType.Researcher,
                buyCost = 4,
                baseAttack = 6,
                baseHealth = 12,
                baseSpeed = 6,
                currentAttack = 6,
                currentHealth = 12,
                currentSpeed = 6,
                skillDescription = "���ǿ� ���� ��ȭ",
                attributes = new List<string>()
            },
            
            // ������
            new CharacterData
            {
                id = "negotiator_basic",
                name = "������",
                type = CharacterType.Follower,
                evolutionType = EvolutionType.Negotiator,
                buyCost = 2,
                baseAttack = 4,
                baseHealth = 8,
                baseSpeed = 9,
                currentAttack = 4,
                currentHealth = 8,
                currentSpeed = 9,
                skillDescription = "�� ����� 1~3��� ȹ��",
                attributes = new List<string>()
            }
        };
    }

    public static List<CharacterData> GetSpiritCharacters()
    {
        return new List<CharacterData>
        {
            new CharacterData
            {
                id = "spirit_time",
                name = "�ð��� �ŷ�",
                type = CharacterType.Spirit,
                evolutionType = EvolutionType.Clockmaker,
                buyCost = 8,
                baseAttack = 15,
                baseHealth = 25,
                baseSpeed = 12,
                currentAttack = 15,
                currentHealth = 25,
                currentSpeed = 12,
                skillDescription = "��� ĳ������ �ӵ� ����",
                attributes = new List<string> { "Time" }
            },

            new CharacterData
            {
                id = "spirit_fortune",
                name = "����� �ŷ�",
                type = CharacterType.Spirit,
                evolutionType = EvolutionType.Gambler,
                buyCost = 8,
                baseAttack = 12,
                baseHealth = 20,
                baseSpeed = 10,
                currentAttack = 12,
                currentHealth = 20,
                currentSpeed = 10,
                skillDescription = "Ȯ�� �̺�Ʈ �ߵ��� �߰� ����",
                attributes = new List<string> { "Luck" }
            },

            new CharacterData
            {
                id = "spirit_ice",
                name = "������ �ŷ�",
                type = CharacterType.Spirit,
                evolutionType = EvolutionType.IceMage,
                buyCost = 8,
                baseAttack = 14,
                baseHealth = 22,
                baseSpeed = 11,
                currentAttack = 14,
                currentHealth = 22,
                currentSpeed = 11,
                skillDescription = "���� �Ӽ� ��ȭ",
                attributes = new List<string> { "Ice" }
            }
        };
    }
}