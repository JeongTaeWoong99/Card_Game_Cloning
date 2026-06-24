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

// ECardType에서 파생되는 값을 한곳에서 관리한다.
public static class CardTypeExtensions
{
    // 속성별 공격 대기시간(공격 가능까지 보내야 하는 자기 턴 수): 원거리1 / 무쌍2 / 그 외 0
    public static int GetWaitTurn(this ECardType type)
    {
        return type switch
        {
            ECardType.Ranged => 1,
            ECardType.Musou  => 2,
            _                => 0, // Normal, Healer, Shield, Vampire
        };
    }

    // 카드에 표시할 속성 한글 이름
    public static string GetDisplayName(this ECardType type)
    {
        return type switch
        {
            ECardType.Normal  => "일반",
            ECardType.Ranged  => "원거리",
            ECardType.Musou   => "무쌍",
            ECardType.Healer  => "힐러",
            ECardType.Shield  => "방패",
            ECardType.Vampire => "흡혈",
            _                 => "",
        };
    }

    // 공격(전투) 시 효과 설명. 모든 전투 피해는 현재 HP의 절반(버림·최소 1). 끝에 공격 쿨타임 표기 (공격텍스트)
    public static string GetAttackText(this ECardType type)
    {
        string attack = type switch
        {
            ECardType.Normal  => "현재 HP 절반만큼 피해. 상대도 현재 HP 절반만큼 반격.",
            ECardType.Ranged  => "현재 HP 절반만큼 피해. 반격 없음. 도발 무시(방패를 건너뛰고 공격).",
            ECardType.Musou   => "대상에게 현재 HP 50% 피해 + 인접 적 1장에 25% 피해. 상대도 현재 HP 절반만큼 반격.",
            ECardType.Healer  => "현재 HP 절반만큼 피해. 상대도 현재 HP 절반만큼 반격.",
            ECardType.Shield  => "현재 HP 절반만큼 피해. 상대도 현재 HP 절반만큼 반격.",
            ECardType.Vampire => "현재 HP 절반만큼 피해. 상대도 현재 HP 절반만큼 반격. 살아남으면 자신 HP +3 회복.",
            _                 => "",
        };

        int    waitTurn = type.GetWaitTurn();
        string waitText = waitTurn == 0
            ? "(전방 배치 후 바로 공격 가능)"
            : $"(전방 배치 후 {waitTurn}턴 동안 대기 상태)";

        return $"<공격>\n{attack}{waitText}";
    }

    // 패시브·지속 능력 설명. 위치 의존 효과는 문구에 명시한다 (능력텍스트)
    public static string GetAbilityText(this ECardType type)
    {
        string ability = type switch
        {
            ECardType.Normal  => "없음.",
            ECardType.Ranged  => "후방에 있을 때, 내 턴 시작 시 적 전방 무작위 1장에 공격력의 1/3(소수점 버림) 피해.",
            ECardType.Musou   => "후방에 있을 때, 내 턴 시작 시 50% 확률로 자신 HP +1 충전.",
            ECardType.Healer  => "내 턴 시작 시 회복 가능한 다른 전방 아군 HP 회복(전방=1회 / 후방=1씩 3회).",
            ECardType.Shield  => "받는 모든 피해 -2(최소 1). 적 근접 공격은 이 카드만 노림(도발).",
            ECardType.Vampire => "없음.",
            _                 => "",
        };

        return $"<능력>\n{ability}";
    }
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
