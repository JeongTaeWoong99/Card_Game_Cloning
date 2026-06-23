using UnityEngine;

// 카드 속성(종류). 전방(전투) 효과·후방(대기) 효과·공격 대기시간이 속성별로 갈린다.
public enum ECardType
{
    Normal, // 일반
    Ranged, // 원거리
    Musou,  // 무쌍
    Healer, // 힐러
}

// ECardType에서 파생되는 값을 한곳에서 관리한다.
public static class CardTypeExtensions
{
    // 속성별 공격 대기시간(공격 가능까지 보내야 하는 자기 턴 수): 일반0 / 원거리1 / 무쌍2 / 힐러0
    public static int GetWaitTurn(this ECardType type)
    {
        return type switch
        {
            ECardType.Ranged => 1,
            ECardType.Musou  => 2,
            _                => 0, // Normal, Healer
        };
    }

    // 카드에 표시할 속성 한글 이름 (README 4종: 일반 / 원거리 / 무쌍 / 힐러)
    public static string GetDisplayName(this ECardType type)
    {
        return type switch
        {
            ECardType.Normal => "일반",
            ECardType.Ranged => "원거리",
            ECardType.Musou  => "무쌍",
            ECardType.Healer => "힐러",
            _                => "",
        };
    }

    // 전방(공개) 배치 시의 전투 효과 설명 (README §4). 끝에 공격 쿨타임을 함께 표기한다 (전방텍스트)
    public static string GetFrontEffectText(this ECardType type)
    {
        string effect = type switch
        {
            ECardType.Normal => "자신의 현재 HP만큼 피해 및 상대 카드의 현재 HP만큼 반격 피해를 받음.",
            ECardType.Ranged => "자신의 현재 HP만큼 피해. 반격 피해 없음.",
            ECardType.Musou  => "대상에게 현재 HP 100% 피해, 인접 적 무작위 1장에 50% 추가 피해.",
            ECardType.Healer => "턴 시작 시 회복 가능한 다른 전방 아군 HP를 1씩 3회 회복. 공격은 일반과 동일.",
            _                => "",
        };

        return $"{effect}(공격 쿨타임 {type.GetWaitTurn()})";
    }

    // 후방(대기) 배치 시 자기 턴 시작마다 발동되는 효과 설명 (후방텍스트)
    public static string GetBackEffectText(this ECardType type)
    {
        return type switch
        {
            ECardType.Normal => "없음.",
            ECardType.Ranged => "내 턴 시작 시 적 전방 무작위 1장에 공격력의 1/3(소수점 버림) 피해.",
            ECardType.Musou  => "내 턴 시작 시 50% 확률로 자신 HP +1 충전.",
            ECardType.Healer => "내 턴 시작 시 회복 가능한 다른 전방 아군 HP를 1 회복.",
            _                => "",
        };
    }
}

// 카드 한 종류의 원본 데이터. percent는 덱 뽑기 가중치(높을수록 자주 등장).
// 공격 데미지 = 공격자의 현재 HP 규칙이므로 별도 attack 스탯은 두지 않는다.
[System.Serializable]
public class Item : IWeighted
{
    public string    name;
    public ECardType type;    // 카드 속성 (전투 효과·대기시간 산정에 사용)
    public string    ability; // 카드 고유 능력 설명
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
