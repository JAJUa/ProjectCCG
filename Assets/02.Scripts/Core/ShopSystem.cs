using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ShopSystem : MonoBehaviour
{
    private BackendGameManager gameManager;
    private ShopData currentShop;

    // 상점 설정
    private int shopSlotCount = 5;
    private int refreshCost = 1;

    // 캐릭터 풀
    private List<CharacterData> followerPool;
    private List<CharacterData> spiritPool;

    void Start()
    {
        gameManager = BackendGameManager.Instance;

        // 캐릭터 풀 초기화
        followerPool = CharacterDatabase.GetAllCharacters();
        spiritPool = CharacterDatabase.GetSpiritCharacters();

        // 이벤트 구독
        if (gameManager != null)
        {
            gameManager.OnShopPhaseStart += OnShopPhaseStart;
        }
    }

    void OnDestroy()
    {
        if (gameManager != null)
        {
            gameManager.OnShopPhaseStart -= OnShopPhaseStart;
        }
    }

    // ============================================
    // 상점 단계 시작
    // ============================================

    private void OnShopPhaseStart()
    {
        RefreshShop();
    }

    // ============================================
    // 상점 새로고침
    // ============================================

    public bool RefreshShop()
    {
        // 골드 소모
        if (gameManager.currentPlayerDeck.gold < refreshCost && currentShop != null)
        {
            Debug.Log("골드가 부족합니다!");
            return false;
        }

        if (currentShop != null)
        {
            gameManager.SpendGold(refreshCost);
        }

        currentShop = new ShopData();
        currentShop.availableCharacters.Clear();

        // 신도자 카드 랜덤 생성
        for (int i = 0; i < shopSlotCount; i++)
        {
            CharacterData character = GetRandomFollower();
            if (character != null)
            {
                currentShop.availableCharacters.Add(character);
            }
        }

        // 2라운드 이후 신령 추가
        if (gameManager.currentPlayerDeck.round >= 2)
        {
            currentShop.availableSpirit = GetRandomSpirit();
        }

        Debug.Log($"상점 새로고침 완료! {currentShop.availableCharacters.Count}개 카드");

        // 새로고침 이벤트 발동 (교섭가 진화 체크 등)
        CheckRefreshEvolution();

        return true;
    }

    private CharacterData GetRandomFollower()
    {
        if (followerPool.Count == 0) return null;

        // 라운드에 따라 더 강한 카드가 나올 확률 증가 (간단한 구현)
        var availableFollowers = followerPool.ToList();
        int randomIndex = Random.Range(0, availableFollowers.Count);

        return availableFollowers[randomIndex].Clone();
    }

    private CharacterData GetRandomSpirit()
    {
        if (spiritPool.Count == 0) return null;

        int randomIndex = Random.Range(0, spiritPool.Count);
        return spiritPool[randomIndex].Clone();
    }

    // ============================================
    // 카드 구매
    // ============================================

    public bool BuyCharacter(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= currentShop.availableCharacters.Count)
        {
            Debug.Log("잘못된 슬롯 인덱스입니다");
            return false;
        }

        CharacterData character = currentShop.availableCharacters[slotIndex];

        // 골드 체크
        if (gameManager.currentPlayerDeck.gold < character.buyCost)
        {
            Debug.Log($"골드 부족! 필요: {character.buyCost}, 보유: {gameManager.currentPlayerDeck.gold}");
            return false;
        }

        // 편성 슬롯 가득 찬 경우 체크
        if (gameManager.currentPlayerDeck.currentFormation.Count >= gameManager.config.maxFormationSlots)
        {
            Debug.Log("편성 슬롯이 가득 찼습니다! 먼저 캐릭터를 제거하세요.");
            return false;
        }

        // 구매 처리
        gameManager.SpendGold(character.buyCost);
        CharacterData purchasedCharacter = character.Clone();
        gameManager.currentPlayerDeck.ownedCharacters.Add(purchasedCharacter);

        // 🎯 자동 배치 (신령은 맨 앞, 일반 캐릭터는 맨 뒤)
        if (character.type == CharacterType.Spirit)
        {
            // 기존 신령 제거 (덱에 신령은 1개만)
            gameManager.currentPlayerDeck.currentFormation.RemoveAll(c => c.type == CharacterType.Spirit);
            // 맨 앞에 배치
            gameManager.currentPlayerDeck.currentFormation.Insert(0, purchasedCharacter);
            Debug.Log($"✨ 신령 {character.name}을(를) 맨 앞에 배치!");
        }
        else
        {
            // 일반 캐릭터는 맨 뒤에 배치
            gameManager.currentPlayerDeck.currentFormation.Add(purchasedCharacter);
            Debug.Log($"🎴 {character.name}을(를) 덱에 배치!");
        }

        Debug.Log($"💰 {character.name} 구매 완료! (남은 골드: {gameManager.currentPlayerDeck.gold})");

        // 구매한 카드 제거
        currentShop.availableCharacters.RemoveAt(slotIndex);

        // 진화 조건 체크
        CheckEvolution(purchasedCharacter);

        /*
        // 성취 추적
        if (AchievementSystem.Instance != null)
        {
            AchievementSystem.Instance.TrackQuestProgress("daily_buy_5");
        }*/

        return true;
    }

    public bool BuySpirit()
    {
        if (currentShop.availableSpirit == null)
        {
            Debug.Log("구매 가능한 신령이 없습니다");
            return false;
        }

        CharacterData spirit = currentShop.availableSpirit;

        // 골드 체크
        if (gameManager.currentPlayerDeck.gold < spirit.buyCost)
        {
            Debug.Log($"골드 부족! 필요: {spirit.buyCost}, 보유: {gameManager.currentPlayerDeck.gold}");
            return false;
        }

        // 기존 신령 제거 (덱에 신령은 1개만)
        gameManager.currentPlayerDeck.ownedCharacters.RemoveAll(c => c.type == CharacterType.Spirit);
        gameManager.currentPlayerDeck.currentFormation.RemoveAll(c => c.type == CharacterType.Spirit);

        // 구매 처리
        gameManager.SpendGold(spirit.buyCost);
        gameManager.currentPlayerDeck.ownedCharacters.Add(spirit.Clone());

        Debug.Log($"{spirit.name} 구매 완료!");

        // 구매 후 제거
        currentShop.availableSpirit = null;

        return true;
    }

    // ============================================
    // 카드 판매
    // ============================================

    public bool SellCharacter(CharacterData character)
    {
        if (!gameManager.currentPlayerDeck.ownedCharacters.Contains(character))
        {
            Debug.Log("보유하지 않은 캐릭터입니다");
            return false;
        }

        // 판매 가격 (구매 가격의 50%)
        int sellPrice = Mathf.Max(1, character.buyCost / 2);

        // 특수 스킬 발동 체크
        CheckSellSkill(character, ref sellPrice);

        gameManager.AddGold(sellPrice);
        gameManager.currentPlayerDeck.ownedCharacters.Remove(character);
        gameManager.currentPlayerDeck.currentFormation.Remove(character);

        Debug.Log($"{character.name} 판매 완료! {sellPrice} 골드 획득");

        return true;
    }

    // ============================================
    // 진화 체크
    // ============================================

    private void CheckEvolution(CharacterData character)
    {
        // 검사 진화 (스탯 기반)
        if (character.evolutionType == EvolutionType.Swordsman)
        {
            if (character.currentAttack >= character.attackThreshold)
            {
                EvolveCharacter(character, EvolutionType.BerserkerSwordsman);
            }
            else if (character.currentHealth >= character.healthThreshold)
            {
                EvolveCharacter(character, EvolutionType.GuardianSwordsman);
            }
            else if (character.currentSpeed >= character.speedThreshold)
            {
                EvolveCharacter(character, EvolutionType.WindSwordsman);
            }
        }

        // 마법사 진화 (속성 기반)
        if (character.evolutionType == EvolutionType.Mage)
        {
            int iceCount = CountAttributeInFormation("Ice");
            int fireCount = CountAttributeInFormation("Fire");
            int lightningCount = CountAttributeInFormation("Lightning");

            if (iceCount >= 3)
            {
                EvolveCharacter(character, EvolutionType.IceMage);
            }
            else if (fireCount >= 3)
            {
                EvolveCharacter(character, EvolutionType.FireMage);
            }
            else if (lightningCount >= 3)
            {
                EvolveCharacter(character, EvolutionType.LightningMage);
            }
        }

        // 연구자 진화
        if (character.evolutionType == EvolutionType.Researcher)
        {
            // 영혼 속성 발견시
            if (HasAttributeInOwned("Soul"))
            {
                EvolveCharacter(character, EvolutionType.Spiritist);
            }
            // 6명 편성 완료시
            else if (gameManager.currentPlayerDeck.currentFormation.Count >= 6)
            {
                EvolveCharacter(character, EvolutionType.Alchemist);
            }
        }
    }

    private void CheckRefreshEvolution()
    {
        // 교섭가 진화 (새로고침 1번시)
        foreach (var character in gameManager.currentPlayerDeck.ownedCharacters)
        {
            if (character.evolutionType == EvolutionType.Negotiator)
            {
                EvolveCharacter(character, EvolutionType.Clockmaker);
                break;
            }
        }
    }

    private void EvolveCharacter(CharacterData character, EvolutionType newType)
    {
        character.evolutionType = newType;

        // 진화에 따른 스탯 증가
        character.baseAttack += 5;
        character.baseHealth += 5;
        character.baseSpeed += 2;
        character.currentAttack = character.baseAttack;
        character.currentHealth = character.baseHealth;
        character.currentSpeed = character.baseSpeed;

        Debug.Log($"{character.name}이(가) {newType}(으)로 진화했습니다!");

        // 진화에 따른 특수 효과 적용
        ApplyEvolutionEffects(character, newType);
    }

    private void ApplyEvolutionEffects(CharacterData character, EvolutionType type)
    {
        switch (type)
        {
            case EvolutionType.IceMage:
                if (!character.attributes.Contains("Ice"))
                    character.attributes.Add("Ice");
                character.skillDescription = "아군 행동시 앞 적에게 빙결 부여";
                break;

            case EvolutionType.FireMage:
                if (!character.attributes.Contains("Fire"))
                    character.attributes.Add("Fire");
                character.skillDescription = "아군 행동시 앞 적에게 화상 부여";
                break;

            case EvolutionType.LightningMage:
                if (!character.attributes.Contains("Lightning"))
                    character.attributes.Add("Lightning");
                character.skillDescription = "아군 행동시 앞 적에게 번개 부여";
                break;

            case EvolutionType.BerserkerSwordsman:
                character.skillDescription = "공격시 앞 2명 적에게 추가 공격";
                break;

            case EvolutionType.GuardianSwordsman:
                character.currentAttack += Mathf.RoundToInt(character.currentHealth * 0.1f);
                character.skillDescription = "체력 비례 공격력 증가";
                break;

            case EvolutionType.WindSwordsman:
                character.currentSpeed *= 2;
                character.skillDescription = "속도 2배";
                break;

            case EvolutionType.Alchemist:
                character.skillDescription = "상점에서 캐릭터 합치기 가능";
                // 합치기 UI 활성화 필요
                break;

            case EvolutionType.Spiritist:
                character.skillDescription = "아군 사망시 영혼 소환";
                break;

            case EvolutionType.Clockmaker:
                character.skillDescription = "모든 캐릭터 속도 공유";
                break;

            case EvolutionType.Gambler:
                character.skillDescription = "확률 이벤트시 추가 공격";
                break;
        }
    }

    // ============================================
    // 특수 스킬 처리
    // ============================================

    private void CheckSellSkill(CharacterData character, ref int sellPrice)
    {
        // 몰락한 귀족, 주인 잃은 병사 등의 판매시 골드 획득 스킬
        if (character.name.Contains("귀족") || character.name.Contains("병사"))
        {
            sellPrice += 2; // 추가 골드
        }

        // 근육질 노예 - 판매시 신령 스탯 영구 증가
        if (character.name.Contains("근육질 노예"))
        {
            var spirit = gameManager.currentPlayerDeck.ownedCharacters.Find(c => c.type == CharacterType.Spirit);
            if (spirit != null)
            {
                if (character.name.Contains("공격"))
                {
                    spirit.baseAttack += 2;
                    spirit.currentAttack += 2;
                    Debug.Log($"{spirit.name}의 공격력이 영구 증가!");
                }
                else if (character.name.Contains("체력"))
                {
                    spirit.baseHealth += 2;
                    spirit.currentHealth += 2;
                    Debug.Log($"{spirit.name}의 체력이 영구 증가!");
                }
                else if (character.name.Contains("속도"))
                {
                    spirit.baseSpeed += 2;
                    spirit.currentSpeed += 2;
                    Debug.Log($"{spirit.name}의 속도가 영구 증가!");
                }
            }
        }
    }

    // ============================================
    // 카드 합치기 (연금술사)
    // ============================================

    public bool MergeCharacters(CharacterData char1, CharacterData char2)
    {
        // 연금술사가 있는지 체크
        bool hasAlchemist = gameManager.currentPlayerDeck.ownedCharacters.Any(
            c => c.evolutionType == EvolutionType.Alchemist
        );

        if (!hasAlchemist)
        {
            Debug.Log("연금술사가 필요합니다!");
            return false;
        }

        // 같은 타입의 신도자만 합치기 가능
        if (char1.evolutionType != char2.evolutionType || char1.type != CharacterType.Follower)
        {
            Debug.Log("같은 타입의 신도자만 합칠 수 있습니다!");
            return false;
        }

        // 합치기 (스탯 합산)
        char1.baseAttack += char2.baseAttack / 2;
        char1.baseHealth += char2.baseHealth / 2;
        char1.baseSpeed += char2.baseSpeed / 2;
        char1.currentAttack = char1.baseAttack;
        char1.currentHealth = char1.baseHealth;
        char1.currentSpeed = char1.baseSpeed;

        // 속성 합치기
        foreach (var attr in char2.attributes)
        {
            if (!char1.attributes.Contains(attr))
                char1.attributes.Add(attr);
        }

        // 두 번째 캐릭터 제거
        gameManager.currentPlayerDeck.ownedCharacters.Remove(char2);
        gameManager.currentPlayerDeck.currentFormation.Remove(char2);

        Debug.Log($"{char1.name}과(와) {char2.name}을(를) 합쳤습니다!");

        return true;
    }

    // ============================================
    // 유틸리티
    // ============================================

    private int CountAttributeInFormation(string attribute)
    {
        int count = 0;
        foreach (var character in gameManager.currentPlayerDeck.currentFormation)
        {
            if (character.attributes.Contains(attribute))
                count++;
        }
        return count;
    }

    private bool HasAttributeInOwned(string attribute)
    {
        return gameManager.currentPlayerDeck.ownedCharacters.Any(c => c.attributes.Contains(attribute));
    }

    public ShopData GetCurrentShop()
    {
        return currentShop;
    }
}