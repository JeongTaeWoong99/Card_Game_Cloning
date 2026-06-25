using UnityEngine;

// 카드 속성(종류). 전방(전투) 효과·후방(대기) 효과·공격 대기시간이 속성별로 갈린다.
public enum ECardType
{
    Normal,  // 일반
    Ranged,  // 원거리
    Musou,   // 무쌍
    Healer,  // 힐러
    Shield,  // 방패 (도발 + 피해 경감)
    Vampire, // 흡혈 (공격 시 회복)
}

// ECardType에서 파생되는 표시값을 노출하는 확장. 실제 데이터/로직은 ICardBehaviour 구현이 소유하고,
// 여기서는 기존 호출부 호환을 위해 확장 메서드 형태만 유지한 채 CardBehaviours로 위임한다.
public static class CardTypeExtensions
{
    // 속성별 공격 대기시간(공격 가능까지 보내야 하는 자기 턴 수)
    public static int GetWaitTurn(this ECardType type) => CardBehaviours.Of(type).WaitTurn;

    // 카드에 표시할 속성 한글 이름
    public static string GetDisplayName(this ECardType type) => CardBehaviours.Of(type).DisplayName;

    // 공격(전투) 시 효과 설명 (+ 공격 대기시간 안내)
    public static string GetAttackText(this ECardType type) => CardBehaviours.Of(type).AttackText;

    // 패시브·지속 능력 설명
    public static string GetAbilityText(this ECardType type) => CardBehaviours.Of(type).AbilityText;
}

// 카드 한 종류의 원본 데이터. percent는 덱 뽑기 가중치(높을수록 자주 등장).
// 공격 데미지 = 공격자의 현재 HP 규칙이므로 별도 attack 스탯은 두지 않는다.
[System.Serializable]
public class Item : IWeighted
{
    public string    name;
    public ECardType type;    // 카드 속성 (전투 효과·대기시간 산정에 사용)
    public int       health;
    public Sprite    sprite;
    public float     percent;

    public float Percent => percent;
}

[CreateAssetMenu(fileName = "ItemSO", menuName = "Scriptable Object/ItemSO")]
public class ItemSO : ScriptableObject
{
    public Item[] items;
}
