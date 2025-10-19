namespace SpiritAge.Core.Enums
{
    /// <summary>
    /// 유닛 타입
    /// </summary>
    public enum UnitType
    {
        None = 0,
        Follower = 1,  // 신도자
        Spirit = 2     // 신령
    }

    /// <summary>
    /// 진화 타입
    /// </summary>
    public enum EvolutionType
    {
        None = 0,

        // 기본 직업
        Swordsman = 10,
        Mage = 20,
        Researcher = 30,
        Negotiator = 40,

        // 검사 진화
        BerserkerSwordsman = 11,  // 광전사
        GuardianSwordsman = 12,    // 수호자
        WindSwordsman = 13,        // 질풍검사

        // 마법사 진화
        IceMage = 21,              // 빙결술사
        FireMage = 22,             // 화염술사
        LightningMage = 23,        // 번개술사

        // 연구자 진화
        Alchemist = 31,            // 연금술사
        Spiritist = 32,            // 영혼술사

        // 교섭가 진화
        Clockmaker = 41,           // 시계상
        Gambler = 42               // 도박사
    }

    /// <summary>
    /// 원소 속성
    /// </summary>
    public enum ElementAttribute
    {
        None = 0,
        Ice = 1,
        Fire = 2,
        Lightning = 3,
        Soul = 4,
        Time = 5,
        Madness = 6
    }

    /// <summary>
    /// 버프 타입
    /// </summary>
    public enum BuffType
    {
        None = 0,
        Freeze = 1,        // 빙결 (속도 감소)
        Burn = 2,          // 화상 (지속 데미지)
        Lightning = 3,     // 번개 (3중첩 시 기절)
        Stun = 4,          // 기절
        Soul = 5,          // 영혼 (하루살이)
        Madness = 6,       // 광기 (공격력↑ 방어력↓)
        SpeedBoost = 7,
        AttackBoost = 8,
        DefenseBoost = 9,
        Shield = 10
    }

    /// <summary>
    /// 스킬 발동 타입
    /// </summary>
    public enum SkillTriggerType
    {
        None = 0,
        OnAttack = 1,
        OnHit = 2,
        OnTurnStart = 3,
        OnTurnEnd = 4,
        OnDeath = 5,
        OnAllyDeath = 6,
        OnEnemyDeath = 7,
        OnBattleStart = 8,
        OnBattleEnd = 9,
        Passive = 10
    }

    /// <summary>
    /// 게임 페이즈
    /// </summary>
    public enum GamePhase
    {
        None = 0,
        Lobby = 1,
        Matchmaking = 2,
        Shop = 3,
        Formation = 4,
        Battle = 5,
        Result = 6
    }

    /// <summary>
    /// 전투 결과
    /// </summary>
    public enum BattleResult
    {
        None = 0,
        Victory = 1,
        Defeat = 2,
        Draw = 3
    }
}