namespace SpiritAge.Core.Enums
{
    /// <summary>
    /// ���� Ÿ��
    /// </summary>
    public enum UnitType
    {
        None = 0,
        Follower = 1,  // �ŵ���
        Spirit = 2     // �ŷ�
    }

    /// <summary>
    /// ��ȭ Ÿ��
    /// </summary>
    public enum EvolutionType
    {
        None = 0,

        // �⺻ ����
        Swordsman = 10,
        Mage = 20,
        Researcher = 30,
        Negotiator = 40,

        // �˻� ��ȭ
        BerserkerSwordsman = 11,  // ������
        GuardianSwordsman = 12,    // ��ȣ��
        WindSwordsman = 13,        // ��ǳ�˻�

        // ������ ��ȭ
        IceMage = 21,              // �������
        FireMage = 22,             // ȭ������
        LightningMage = 23,        // ��������

        // ������ ��ȭ
        Alchemist = 31,            // ���ݼ���
        Spiritist = 32,            // ��ȥ����

        // ������ ��ȭ
        Clockmaker = 41,           // �ð��
        Gambler = 42               // ���ڻ�
    }

    /// <summary>
    /// ���� �Ӽ�
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
    /// ���� Ÿ��
    /// </summary>
    public enum BuffType
    {
        None = 0,
        Freeze = 1,        // ���� (�ӵ� ����)
        Burn = 2,          // ȭ�� (���� ������)
        Lightning = 3,     // ���� (3��ø �� ����)
        Stun = 4,          // ����
        Soul = 5,          // ��ȥ (�Ϸ����)
        Madness = 6,       // ���� (���ݷ¡� ���¡�)
        SpeedBoost = 7,
        AttackBoost = 8,
        DefenseBoost = 9,
        Shield = 10
    }

    /// <summary>
    /// ��ų �ߵ� Ÿ��
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
    /// ���� ������
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
    /// ���� ���
    /// </summary>
    public enum BattleResult
    {
        None = 0,
        Victory = 1,
        Defeat = 2,
        Draw = 3
    }
}