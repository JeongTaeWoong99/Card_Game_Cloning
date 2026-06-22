using UnityEngine;

// 카드 속성(종류). 속성별 효과(데미지/반격/광역/힐)는 차후 구현하며,
// 현재는 공격 대기시간 산정에만 사용한다.
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
}

// 카드 한 종류의 원본 데이터. percent는 덱 뽑기 가중치(높을수록 자주 등장).
[System.Serializable]
public class Item
{
    public string    name;
    public ECardType type;    // 카드 속성 (효과는 차후, 현재는 대기시간 산정에 사용)
    public string    ability; // 카드 고유 능력 설명 (내용은 차후 입력, 현재는 비움)
    public int       attack;
    public int       health;
    public Sprite    sprite;
    public float     percent;
}

[CreateAssetMenu(fileName = "ItemSO", menuName = "Scriptable Object/ItemSO")]
public class ItemSO : ScriptableObject
{
    public Item[] items;
}
