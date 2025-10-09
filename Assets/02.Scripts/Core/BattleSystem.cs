using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class BattleSystem : MonoBehaviour
{
    // 전투 중인 캐릭터들
    private List<CharacterData> playerTeam;
    private List<CharacterData> enemyTeam;

    // 전투 로그
    private BattleResult battleResult;

    // 전투 설정
    private float actionDelay = 0.5f;

    // 전투 상태
    private bool isBattleActive = false;

    // UI 참조
    private GameUIManager uiManager;

    // 전투 UI 업데이트를 위한 이벤트
    public System.Action<List<CharacterData>, List<CharacterData>> OnBattleUIUpdate;

    void Start()
    {
        uiManager = GetComponent<GameUIManager>();
    }

    // ============================================
    // 전투 시작
    // ============================================

    public void StartBattle(List<CharacterData> playerFormation, List<CharacterData> enemyFormation)
    {
        if (isBattleActive)
        {
            Debug.LogWarning("⚠️ 이미 전투가 진행 중입니다");
            return;
        }

        // 팀 복사 (원본 데이터 보호)
        playerTeam = new List<CharacterData>();
        foreach (var character in playerFormation)
        {
            playerTeam.Add(character.Clone());
        }

        enemyTeam = new List<CharacterData>();
        foreach (var character in enemyFormation)
        {
            enemyTeam.Add(character.Clone());
        }

        battleResult = new BattleResult();
        isBattleActive = true;

        // 🎯 전투 시작 로그 (상세)
        Debug.Log("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
        Debug.Log("⚔️ <color=yellow><b>전투 시작!</b></color>");
        Debug.Log("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");

        Debug.Log("\n<color=cyan>【 아군 편성 】</color>");
        for (int i = 0; i < playerTeam.Count; i++)
        {
            var c = playerTeam[i];
            Debug.Log($"  {i + 1}. <color=cyan><b>{c.name}</b></color> " +
                     $"[{c.type}] [{c.evolutionType}] " +
                     $"| ATK:{c.currentAttack} HP:{c.currentHealth} SPD:{c.currentSpeed} " +
                     $"| 스킬: {c.skillDescription}");
            if (c.attributes.Count > 0)
                Debug.Log($"     속성: {string.Join(", ", c.attributes)}");
        }

        Debug.Log("\n<color=red>【 적군 편성 】</color>");
        for (int i = 0; i < enemyTeam.Count; i++)
        {
            var c = enemyTeam[i];
            Debug.Log($"  {i + 1}. <color=red><b>{c.name}</b></color> " +
                     $"[{c.type}] [{c.evolutionType}] " +
                     $"| ATK:{c.currentAttack} HP:{c.currentHealth} SPD:{c.currentSpeed} " +
                     $"| 스킬: {c.skillDescription}");
            if (c.attributes.Count > 0)
                Debug.Log($"     속성: {string.Join(", ", c.attributes)}");
        }
        Debug.Log("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━\n");

        StartCoroutine(BattleLoop());
    }

    // ============================================
    // 전투 루프
    // ============================================

    private IEnumerator BattleLoop()
    {
        int turnCount = 0;
        const int maxTurns = 100;

        while (isBattleActive && turnCount < maxTurns)
        {
            turnCount++;

            Debug.Log($"\n╔═══════════════════════════════════╗");
            Debug.Log($"║       <color=yellow>턴 {turnCount} 시작</color>              ║");
            Debug.Log($"╚═══════════════════════════════════╝");

            // 턴 시작 처리
            ProcessTurnStart();

            // 행동 순서 결정 (속도 기반)
            List<(CharacterData character, bool isPlayer)> actionOrder = DetermineActionOrder();

            Debug.Log("\n<color=magenta>【 행동 순서 】</color>");
            for (int i = 0; i < actionOrder.Count; i++)
            {
                var (character, isPlayer) = actionOrder[i];
                string team = isPlayer ? "<color=cyan>아군</color>" : "<color=red>적군</color>";
                Debug.Log($"  {i + 1}. {team} {character.name} (SPD:{character.currentSpeed})");
            }

            // 순서대로 행동 실행
            foreach (var (character, isPlayer) in actionOrder)
            {
                if (character.currentHealth <= 0) continue;

                Debug.Log($"\n─────────────────────────────");
                LogCharacterTurn(character, isPlayer);

                // 버프/디버프 처리
                ProcessBuffs(character);

                // 기절 상태 체크
                if (HasBuff(character, BuffType.Stun))
                {
                    Debug.Log($"<color=gray>💫 {character.name}은(는) 기절 상태로 행동 불가!</color>");
                    AddBattleLog($"{character.name}은(는) 기절 상태입니다!");
                    yield return new WaitForSeconds(actionDelay);
                    continue;
                }

                // 공격 실행
                ExecuteAttack(character, isPlayer);

                // UI 업데이트
                UpdateBattleUI();

                yield return new WaitForSeconds(actionDelay);

                // 전투 종료 체크
                if (CheckBattleEnd())
                {
                    break;
                }
            }

            // 턴 종료 처리
            ProcessTurnEnd();

            // 턴 종료 후 상태 출력
            LogTeamStatus();

            // UI 업데이트
            UpdateBattleUI();

            // 전투 종료 체크
            if (CheckBattleEnd())
            {
                break;
            }

            yield return new WaitForSeconds(actionDelay);
        }

        EndBattle();
    }

    // ============================================
    // 캐릭터 턴 로그
    // ============================================

    private void LogCharacterTurn(CharacterData character, bool isPlayer)
    {
        string team = isPlayer ? "<color=cyan>【아군】</color>" : "<color=red>【적군】</color>";
        Debug.Log($"{team} <b>{character.name}</b>의 차례!");
        Debug.Log($"  현재 스탯 → ATK:<color=yellow>{character.currentAttack}</color> " +
                 $"HP:<color=green>{character.currentHealth}</color> " +
                 $"SPD:<color=cyan>{character.currentSpeed}</color>");

        if (character.buffs.Count > 0)
        {
            Debug.Log($"  활성 버프/디버프:");
            foreach (var buff in character.buffs)
            {
                string buffColor = GetBuffLogColor(buff.type);
                string durationText = buff.duration > 0 ? $"(남은 턴:{buff.duration})" : "(영구)";
                string stackText = buff.stack > 1 ? $" x{buff.stack}" : "";
                Debug.Log($"    {buffColor}{buff.type}{stackText}</color> {durationText}");
            }
        }

        if (!string.IsNullOrEmpty(character.skillDescription))
        {
            Debug.Log($"  스킬: <color=orange>{character.skillDescription}</color>");
        }
    }

    // ============================================
    // 팀 상태 로그
    // ============================================

    private void LogTeamStatus()
    {
        Debug.Log("\n<color=cyan>━━━ 아군 상태 ━━━</color>");
        foreach (var c in playerTeam)
        {
            if (c.currentHealth > 0)
            {
                Debug.Log($"  {c.name}: HP {c.currentHealth} / ATK {c.currentAttack} / SPD {c.currentSpeed}");
            }
            else
            {
                Debug.Log($"  <color=gray>{c.name}: 전투불능</color>");
            }
        }

        Debug.Log("\n<color=red>━━━ 적군 상태 ━━━</color>");
        foreach (var c in enemyTeam)
        {
            if (c.currentHealth > 0)
            {
                Debug.Log($"  {c.name}: HP {c.currentHealth} / ATK {c.currentAttack} / SPD {c.currentSpeed}");
            }
            else
            {
                Debug.Log($"  <color=gray>{c.name}: 전투불능</color>");
            }
        }
    }

    // ============================================
    // UI 업데이트
    // ============================================

    private void UpdateBattleUI()
    {
        OnBattleUIUpdate?.Invoke(playerTeam, enemyTeam);

        if (uiManager != null)
        {
            // UI 매니저를 통해 실시간 업데이트
            // uiManager.UpdateBattleUnits(playerTeam, enemyTeam);
        }
    }

    // ============================================
    // 행동 순서 결정
    // ============================================

    private List<(CharacterData character, bool isPlayer)> DetermineActionOrder()
    {
        List<(CharacterData, bool)> allCharacters = new List<(CharacterData, bool)>();

        foreach (var character in playerTeam)
        {
            if (character.currentHealth > 0)
                allCharacters.Add((character, true));
        }

        foreach (var character in enemyTeam)
        {
            if (character.currentHealth > 0)
                allCharacters.Add((character, false));
        }

        // 속도로 정렬 (높은 속도부터)
        allCharacters.Sort((a, b) => b.Item1.currentSpeed.CompareTo(a.Item1.currentSpeed));

        // 속도가 2배 이상 차이나면 추가 행동
        List<(CharacterData, bool)> finalOrder = new List<(CharacterData, bool)>();

        foreach (var entry in allCharacters)
        {
            finalOrder.Add(entry);

            // 가장 느린 상대보다 2배 빠르면 한 번 더 행동
            var opponents = allCharacters.Where(e => e.Item2 != entry.Item2).ToList();
            if (opponents.Count > 0)
            {
                int minOpponentSpeed = opponents.Min(e => e.Item1.currentSpeed);
                if (entry.Item1.currentSpeed >= minOpponentSpeed * 2)
                {
                    finalOrder.Add(entry);
                    Debug.Log($"<color=yellow>⚡ {entry.Item1.name}은(는) 속도가 2배 이상 빨라 재행동!</color>");
                }
            }
        }

        return finalOrder;
    }

    // ============================================
    // 공격 실행
    // ============================================

    private void ExecuteAttack(CharacterData attacker, bool isPlayerUnit)
    {
        List<CharacterData> targets = isPlayerUnit ? enemyTeam : playerTeam;
        CharacterData target = GetFrontTarget(targets);

        if (target == null) return;

        string attackerTeam = isPlayerUnit ? "<color=cyan>아군</color>" : "<color=red>적군</color>";
        string targetTeam = isPlayerUnit ? "<color=red>적군</color>" : "<color=cyan>아군</color>";

        int damage = CalculateDamage(attacker, target);
        int previousHealth = target.currentHealth;
        target.currentHealth -= damage;
        target.currentHealth = Mathf.Max(0, target.currentHealth);

        Debug.Log($"⚔️ {attackerTeam} <b>{attacker.name}</b>이(가) {targetTeam} <b>{target.name}</b>을(를) 공격!");
        Debug.Log($"   데미지: <color=red>{damage}</color> " +
                 $"| {target.name} HP: {previousHealth} → <color=red>{target.currentHealth}</color>");

        AddBattleLog($"{attacker.name}이(가) {target.name}에게 {damage} 데미지!");

        // 속성 공격 적용
        ApplyElementalEffects(attacker, target);

        // 대상이 죽었는지 체크
        if (target.currentHealth <= 0)
        {
            Debug.Log($"<color=red>💀 {target.name}이(가) 쓰러졌습니다!</color>");
            AddBattleLog($"{target.name}이(가) 쓰러졌습니다!");
            OnCharacterDeath(target, !isPlayerUnit);
        }

        // 스킬 발동
        TriggerSkills(attacker, target, isPlayerUnit);
    }

    private CharacterData GetFrontTarget(List<CharacterData> targets)
    {
        foreach (var target in targets)
        {
            if (target.currentHealth > 0)
                return target;
        }
        return null;
    }

    private int CalculateDamage(CharacterData attacker, CharacterData target)
    {
        int baseDamage = attacker.currentAttack;

        // 광기 버프가 있으면 공격력 증가
        if (HasBuff(attacker, BuffType.Madness))
        {
            int madnessBonus = Mathf.RoundToInt(baseDamage * 0.5f);
            Debug.Log($"   <color=red>🔥 광기 효과!</color> 공격력 {baseDamage} → {baseDamage + madnessBonus}");
            baseDamage = Mathf.RoundToInt(baseDamage * 1.5f);
        }

        // 빙결 상태의 적에 대한 추가 데미지
        if (HasBuff(target, BuffType.Freeze))
        {
            int freezeBonus = Mathf.RoundToInt(baseDamage * 0.2f);
            Debug.Log($"   <color=cyan>❄️ 빙결 추가 데미지!</color> +{freezeBonus}");
            baseDamage = Mathf.RoundToInt(baseDamage * 1.2f);
        }

        return Mathf.Max(1, baseDamage);
    }

    // ============================================
    // 속성 효과 적용
    // ============================================

    private void ApplyElementalEffects(CharacterData attacker, CharacterData target)
    {
        foreach (string attribute in attacker.attributes)
        {
            switch (attribute)
            {
                case "Ice":
                    AddBuff(target, BuffType.Freeze, 2);
                    Debug.Log($"   <color=cyan>❄️ 빙결 속성 공격!</color> {target.name}의 속도 감소");
                    AddBattleLog($"{target.name}이(가) 빙결 상태가 되었습니다!");
                    break;

                case "Fire":
                    AddBuff(target, BuffType.Burn, 3);
                    Debug.Log($"   <color=red>🔥 화염 속성 공격!</color> {target.name}에게 지속 데미지");
                    AddBattleLog($"{target.name}이(가) 화상 상태가 되었습니다!");
                    break;

                case "Lightning":
                    AddBuff(target, BuffType.Lightning, -1, 1);
                    int lightningStack = GetBuffStack(target, BuffType.Lightning);
                    Debug.Log($"   <color=yellow>⚡ 번개 속성 공격!</color> {target.name} 번개 스택: {lightningStack}/3");
                    AddBattleLog($"{target.name}에게 번개 ({lightningStack}/3)!");

                    if (lightningStack >= 3)
                    {
                        AddBuff(target, BuffType.Stun, 1);
                        RemoveBuff(target, BuffType.Lightning);
                        Debug.Log($"   <color=yellow>💫 번개 3중첩!</color> {target.name} 기절!");
                        AddBattleLog($"{target.name}이(가) 기절했습니다!");
                    }
                    break;
            }
        }
    }

    // ============================================
    // 스킬 트리거
    // ============================================

    private void TriggerSkills(CharacterData attacker, CharacterData target, bool isPlayerUnit)
    {
        switch (attacker.evolutionType)
        {
            case EvolutionType.BerserkerSwordsman:
                Debug.Log($"   <color=orange>🗡️ [광전사 스킬] 발동!</color>");
                TriggerBerserkerSkill(attacker, isPlayerUnit);
                break;

            case EvolutionType.IceMage:
                Debug.Log($"   <color=cyan>❄️ [빙결술사 스킬] 추가 빙결 효과</color>");
                break;

            case EvolutionType.FireMage:
                Debug.Log($"   <color=red>🔥 [화염술사 스킬] 추가 화상 효과</color>");
                break;

            case EvolutionType.LightningMage:
                Debug.Log($"   <color=yellow>⚡ [번개술사 스킬] 추가 번개 효과</color>");
                break;

            case EvolutionType.Gambler:
                if (Random.value < 0.3f)
                {
                    int extraDamage = Random.Range(1, 4);
                    target.currentHealth -= extraDamage;
                    Debug.Log($"   <color=green>🎲 [도박사 스킬] 행운의 추가 공격! {extraDamage} 데미지</color>");
                    AddBattleLog($"[도박] {target.name}에게 {extraDamage} 추가 데미지!");
                }
                break;
        }
    }

    private void TriggerBerserkerSkill(CharacterData berserker, bool isPlayerUnit)
    {
        List<CharacterData> targets = isPlayerUnit ? enemyTeam : playerTeam;
        int splashDamage = Mathf.RoundToInt(berserker.currentAttack * 0.5f);

        int hitCount = 0;
        foreach (var target in targets)
        {
            if (target.currentHealth > 0 && hitCount < 2)
            {
                int prevHP = target.currentHealth;
                target.currentHealth -= splashDamage;
                target.currentHealth = Mathf.Max(0, target.currentHealth);
                Debug.Log($"      → {target.name}에게 광역 {splashDamage} 데미지! (HP: {prevHP} → {target.currentHealth})");
                AddBattleLog($"[광전사] {target.name}에게 {splashDamage} 광역 데미지!");
                hitCount++;
            }
        }
    }

    // ============================================
    // 버프/디버프 관리
    // ============================================

    private void ProcessBuffs(CharacterData character)
    {
        if (character.buffs.Count == 0) return;

        Debug.Log($"  버프/디버프 처리:");
        List<BuffData> buffsToRemove = new List<BuffData>();

        foreach (var buff in character.buffs)
        {
            switch (buff.type)
            {
                case BuffType.Freeze:
                    int speedReduction = 2;
                    character.currentSpeed = Mathf.Max(1, character.currentSpeed - speedReduction);
                    Debug.Log($"    <color=cyan>❄️ 빙결</color>: 속도 -{speedReduction}");
                    break;

                case BuffType.Burn:
                    int burnDamage = 2;
                    int prevHP = character.currentHealth;
                    character.currentHealth -= burnDamage;
                    character.currentHealth = Mathf.Max(0, character.currentHealth);
                    Debug.Log($"    <color=red>🔥 화상</color>: {burnDamage} 데미지 (HP: {prevHP} → {character.currentHealth})");
                    AddBattleLog($"{character.name}이(가) 화상으로 {burnDamage} 데미지!");
                    break;

                case BuffType.Madness:
                    Debug.Log($"    <color=red>😈 광기</color>: 공격력 +50%");
                    break;
            }

            // 지속시간 감소
            if (buff.duration > 0)
            {
                buff.duration--;
                if (buff.duration <= 0)
                {
                    buffsToRemove.Add(buff);
                    Debug.Log($"    {buff.type} 효과 종료");
                }
            }
        }

        foreach (var buff in buffsToRemove)
        {
            character.buffs.Remove(buff);
        }
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

    private bool HasBuff(CharacterData character, BuffType type)
    {
        return character.buffs.Any(b => b.type == type);
    }

    private int GetBuffStack(CharacterData character, BuffType type)
    {
        var buff = character.buffs.Find(b => b.type == type);
        return buff != null ? buff.stack : 0;
    }

    private void RemoveBuff(CharacterData character, BuffType type)
    {
        character.buffs.RemoveAll(b => b.type == type);
    }

    private string GetBuffLogColor(BuffType type)
    {
        switch (type)
        {
            case BuffType.Freeze: return "<color=cyan>";
            case BuffType.Burn: return "<color=red>";
            case BuffType.Lightning: return "<color=yellow>";
            case BuffType.Soul: return "<color=magenta>";
            case BuffType.Madness: return "<color=red>";
            case BuffType.Stun: return "<color=gray>";
            default: return "<color=white>";
        }
    }

    // ============================================
    // 턴 처리
    // ============================================

    private void ProcessTurnStart()
    {
        // 턴 시작시 효과 처리
    }

    private void ProcessTurnEnd()
    {
        Debug.Log("\n<color=yellow>━━━ 턴 종료 효과 ━━━</color>");

        // 턴 종료시 효과 처리
        foreach (var character in playerTeam)
        {
            if (character.evolutionType == EvolutionType.Negotiator && character.currentHealth > 0)
            {
                int earnedGold = Random.Range(1, 4);
                BackendGameManager.Instance.AddGold(earnedGold);
                Debug.Log($"<color=green>💰 [교섭가]</color> {character.name}이(가) {earnedGold} 골드 획득!");
                AddBattleLog($"[교섭가] {earnedGold} 골드 획득!");
            }
        }
    }

    // ============================================
    // 캐릭터 사망 처리
    // ============================================

    private void OnCharacterDeath(CharacterData deadCharacter, bool isPlayerUnit)
    {
        List<CharacterData> team = isPlayerUnit ? playerTeam : enemyTeam;

        foreach (var character in team)
        {
            if (character.evolutionType == EvolutionType.Spiritist && character.currentHealth > 0)
            {
                CharacterData soul = deadCharacter.Clone();
                soul.name = soul.name + " (영혼)";
                soul.currentHealth = 1;
                soul.buffs.Add(new BuffData(BuffType.Soul));
                team.Add(soul);

                Debug.Log($"<color=magenta>👻 [영혼술사 스킬]</color> {character.name}이(가) {soul.name}을(를) 소환!");
                AddBattleLog($"[영혼술사] {soul.name}을(를) 소환했습니다!");
                break;
            }
        }
    }

    // ============================================
    // 전투 종료 체크
    // ============================================

    private bool CheckBattleEnd()
    {
        bool playerAlive = playerTeam.Any(c => c.currentHealth > 0);
        bool enemyAlive = enemyTeam.Any(c => c.currentHealth > 0);

        if (!playerAlive || !enemyAlive)
        {
            isBattleActive = false;
            return true;
        }

        return false;
    }

    private void EndBattle()
    {
        bool playerAlive = playerTeam.Any(c => c.currentHealth > 0);
        bool enemyAlive = enemyTeam.Any(c => c.currentHealth > 0);

        battleResult.isWin = playerAlive && !enemyAlive;
        battleResult.damageDealt = battleResult.isWin ? 10 : 0;
        battleResult.goldEarned = battleResult.isWin ? 5 : 3;

        Debug.Log("\n╔═══════════════════════════════════╗");
        if (battleResult.isWin)
        {
            Debug.Log("║   <color=yellow>🎉 전투 승리! 🎉</color>           ║");
        }
        else
        {
            Debug.Log("║   <color=red>💀 전투 패배...</color>              ║");
        }
        Debug.Log("╚═══════════════════════════════════╝");
        Debug.Log($"데미지: {battleResult.damageDealt}");
        Debug.Log($"획득 골드: {battleResult.goldEarned}");

        BackendGameManager.Instance.OnBattleComplete(battleResult);
    }

    // ============================================
    // 유틸리티
    // ============================================

    private void AddBattleLog(string message)
    {
        battleResult.battleLog.Add(message);

        if (uiManager != null)
        {
            uiManager.AddBattleLog(message);
        }
    }
}