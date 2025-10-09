using System;
using System.Collections.Generic;
using UnityEngine;

// ============================================
// 열거형 정의
// ============================================

public enum CharacterType
{
    Spirit,      // 신령
    Follower     // 신도자
}

public enum EvolutionType
{
    None,
    Swordsman,        // 검사
    BerserkerSwordsman,  // 광전사 (공격력)
    GuardianSwordsman,   // 수호자 (체력)
    WindSwordsman,       // 질풍검사 (속도)
    Mage,            // 마법사
    IceMage,         // 빙결술사
    FireMage,        // 화염술사
    LightningMage,   // 번개술사
    Researcher,      // 연구자
    Alchemist,       // 연금술사
    Spiritist,       // 영혼술사
    Negotiator,      // 교섭가
    Clockmaker,      // 시계상
    Gambler          // 도박사
}

public enum BuffType
{
    None,
    Freeze,      // 빙결 (속도 저하)
    Burn,        // 화염 (행동시 데미지)
    Lightning,   // 번개 (3중첩시 기절)
    Soul,        // 영혼 (하루살이)
    Madness,     // 광기 (공격력 증가, 방어력 감소)
    Stun         // 기절 (속도 0)
}

// ============================================
// 캐릭터 데이터 클래스
// ============================================

[Serializable]
public class CharacterData
{
    public string id;
    public string name;
    public CharacterType type;
    public EvolutionType evolutionType;

    // 기본 스탯
    public int buyCost;
    public int baseAttack;
    public int baseHealth;
    public int baseSpeed;

    // 현재 스탯
    public int currentAttack;
    public int currentHealth;
    public int currentSpeed;

    // 버프/디버프
    public List<BuffData> buffs = new List<BuffData>();

    // 진화 조건 관련
    public int attackThreshold;  // 진화를 위한 공격력 임계값
    public int healthThreshold;  // 진화를 위한 체력 임계값
    public int speedThreshold;   // 진화를 위한 속도 임계값

    // 특수 스킬
    public string skillDescription;
    public List<string> attributes = new List<string>(); // 예: "Ice", "Fire", "Lightning"

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
// 버프 데이터
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
// 플레이어 덱 데이터
// ============================================

[Serializable]
public class PlayerDeckData
{
    public string playerId;
    public List<CharacterData> ownedCharacters = new List<CharacterData>();
    public List<CharacterData> currentFormation = new List<CharacterData>(); // 최대 6개
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
// 매칭 데이터
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
// 상점 데이터
// ============================================

[Serializable]
public class ShopData
{
    public List<CharacterData> availableCharacters = new List<CharacterData>();
    public CharacterData availableSpirit; // 2라운드 이후 등장
    public int refreshCost = 1;
}

// ============================================
// 전투 결과
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
// 게임 설정 데이터
// ============================================

[Serializable]
public class GameConfig
{
    // 상점 설정
    public int shopSlotCount = 5;
    public int spiritUnlockRound = 2;
    public int maxFormationSlots = 6;

    // 골드 설정
    public int winGold = 5;
    public int loseGold = 3;
    public int startGold = 10;

    // 체력 설정
    public int startHealth = 100;
    public int damagePerLoss = 10;

    // 전투 설정
    public float actionDelay = 0.5f;
    public int stunLightningStack = 3; // 번개 3중첩시 기절
}

// ============================================
// 데이터베이스 (예시)
// ============================================

public static class CharacterDatabase
{
    public static List<CharacterData> GetAllCharacters()
    {
        return new List<CharacterData>
        {
            // 검사 계열
            new CharacterData
            {
                id = "swordsman_basic",
                name = "검사",
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
                skillDescription = "일정 스탯 달성시 진화",
                attributes = new List<string>()
            },
            
            // 마법사 계열
            new CharacterData
            {
                id = "mage_basic",
                name = "마법사",
                type = CharacterType.Follower,
                evolutionType = EvolutionType.Mage,
                buyCost = 3,
                baseAttack = 8,
                baseHealth = 10,
                baseSpeed = 10,
                currentAttack = 8,
                currentHealth = 10,
                currentSpeed = 10,
                skillDescription = "아군이 마법 사용 시 공격력 증가",
                attributes = new List<string>()
            },
            
            // 빙결술사 신도자
            new CharacterData
            {
                id = "ice_follower",
                name = "빙결의 신도자",
                type = CharacterType.Follower,
                evolutionType = EvolutionType.IceMage,
                buyCost = 2,
                baseAttack = 5,
                baseHealth = 8,
                baseSpeed = 7,
                currentAttack = 5,
                currentHealth = 8,
                currentSpeed = 7,
                skillDescription = "공격에 빙결속성 부여",
                attributes = new List<string> { "Ice" }
            },
            
            // 화염술사 신도자
            new CharacterData
            {
                id = "fire_follower",
                name = "화염의 신도자",
                type = CharacterType.Follower,
                evolutionType = EvolutionType.FireMage,
                buyCost = 2,
                baseAttack = 6,
                baseHealth = 7,
                baseSpeed = 7,
                currentAttack = 6,
                currentHealth = 7,
                currentSpeed = 7,
                skillDescription = "공격에 화염속성 부여",
                attributes = new List<string> { "Fire" }
            },
            
            // 번개술사 신도자
            new CharacterData
            {
                id = "lightning_follower",
                name = "번개의 신도자",
                type = CharacterType.Follower,
                evolutionType = EvolutionType.LightningMage,
                buyCost = 2,
                baseAttack = 7,
                baseHealth = 6,
                baseSpeed = 8,
                currentAttack = 7,
                currentHealth = 6,
                currentSpeed = 8,
                skillDescription = "공격에 번개속성 부여",
                attributes = new List<string> { "Lightning" }
            },
            
            // 연구자
            new CharacterData
            {
                id = "researcher_basic",
                name = "연구자",
                type = CharacterType.Follower,
                evolutionType = EvolutionType.Researcher,
                buyCost = 4,
                baseAttack = 6,
                baseHealth = 12,
                baseSpeed = 6,
                currentAttack = 6,
                currentHealth = 12,
                currentSpeed = 6,
                skillDescription = "조건에 따라 진화",
                attributes = new List<string>()
            },
            
            // 교섭가
            new CharacterData
            {
                id = "negotiator_basic",
                name = "교섭가",
                type = CharacterType.Follower,
                evolutionType = EvolutionType.Negotiator,
                buyCost = 2,
                baseAttack = 4,
                baseHealth = 8,
                baseSpeed = 9,
                currentAttack = 4,
                currentHealth = 8,
                currentSpeed = 9,
                skillDescription = "턴 종료시 1~3골드 획득",
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
                name = "시간의 신령",
                type = CharacterType.Spirit,
                evolutionType = EvolutionType.Clockmaker,
                buyCost = 8,
                baseAttack = 15,
                baseHealth = 25,
                baseSpeed = 12,
                currentAttack = 15,
                currentHealth = 25,
                currentSpeed = 12,
                skillDescription = "모든 캐릭터의 속도 공유",
                attributes = new List<string> { "Time" }
            },

            new CharacterData
            {
                id = "spirit_fortune",
                name = "행운의 신령",
                type = CharacterType.Spirit,
                evolutionType = EvolutionType.Gambler,
                buyCost = 8,
                baseAttack = 12,
                baseHealth = 20,
                baseSpeed = 10,
                currentAttack = 12,
                currentHealth = 20,
                currentSpeed = 10,
                skillDescription = "확률 이벤트 발동시 추가 공격",
                attributes = new List<string> { "Luck" }
            },

            new CharacterData
            {
                id = "spirit_ice",
                name = "빙결의 신령",
                type = CharacterType.Spirit,
                evolutionType = EvolutionType.IceMage,
                buyCost = 8,
                baseAttack = 14,
                baseHealth = 22,
                baseSpeed = 11,
                currentAttack = 14,
                currentHealth = 22,
                currentSpeed = 11,
                skillDescription = "빙결 속성 강화",
                attributes = new List<string> { "Ice" }
            }
        };
    }
}