using UnityEngine;

// 스킬 발동 대상 종류. None은 즉시 발동, MyEntity는 내 엔티티 1체를 선택해야 발동한다.
public enum ESkillTargeting
{
    None,     // 대상 선택 없이 즉시 발동
    MyEntity, // 내 엔티티 1체 선택 후 발동
}

// 스킬 효과의 "종류" 카탈로그. 실제 동작은 SkillSystem이 이 enum으로 분기해 처리한다.
// 새 종류를 추가할 때만 코드를 건드리고, 같은 종류의 수치/이름 변형은 SO에서만 만든다. (OCP)
public enum ESkillEffect
{
    RandomEnemyDamage, // 무작위 적 전방 1체에 value 피해
    HealAllFront,      // 내 전방 전체 HP를 value만큼 회복 (최대치 한도)
    BuffAllFront,      // 내 전방 전체 HP를 value만큼 버프 (최대치 초과 허용)
    BuffSingle,        // 선택한 내 엔티티 HP를 value만큼 버프 (최대치 초과 허용)
}

// 스킬 한 종류의 원본 데이터. 수치·이름·설명·아이콘·드로우 가중치는 여기서 튜닝한다.
[System.Serializable]
public class Skill : IWeighted
{
    public string          name;
    public ESkillEffect    effect;
    public ESkillTargeting targeting;
    public int             manaCost;
    public int             value;       // 효과 수치 (피해/회복/버프량)
    public string          description;
    public Sprite          sprite;
    public float           percent;     // 드로우 가중치 (Deck 재사용)

    public float Percent => percent;
}

[CreateAssetMenu(fileName = "SkillSO", menuName = "Scriptable Object/SkillSO")]
public class SkillSO : ScriptableObject
{
    public Skill[] skills;
}
