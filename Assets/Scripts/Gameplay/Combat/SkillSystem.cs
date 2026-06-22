using UnityEngine;

// 스킬 효과를 실제로 적용하는 실행기. ESkillEffect로 분기해 동작을 처리한다 (SRP: 효과 적용만 담당).
// 새 효과를 추가할 때만 여기에 case를 더하고, 같은 효과의 수치 변형은 SkillSO에서만 만든다 (OCP).
public class SkillSystem : MonoBehaviour
{
    public static SkillSystem Inst { get; private set; }

    // 싱글톤 등록 (Unity 메시지)
    private void Awake()
    {
        Inst = this;
    }

    // 스킬 효과를 실행한다. 대상 O 스킬은 target을 받는다 (CardManager·EnemyAI가 호출)
    public void Cast(Skill skill, bool isMine, Entity target = null)
    {
        switch (skill.effect)
        {
            case ESkillEffect.RandomEnemyDamage:
                RandomEnemyDamage(isMine, skill.value);
                break;

            case ESkillEffect.HealAllFront:
                HealAllFront(isMine, skill.value);
                break;

            case ESkillEffect.BuffAllFront:
                BuffAllFront(isMine, skill.value);
                break;

            case ESkillEffect.BuffSingle:
                BuffSingle(target, skill.value);
                break;
        }
    }

    // 무작위 적 전방 1체에 피해를 준다. 피해로 사망 가능하므로 정리·승패 판정까지 진행
    private void RandomEnemyDamage(bool isMine, int value)
    {
        Entity target = EntityManager.Inst.GetRandomEnemyFront(isMine);
        if (target == null)
        {
            return;
        }

        target.Damaged(value);
        CombatSystem.Inst.ShowDamagePopup(value, target.transform);

        EntityManager.Inst.RemoveDeadAndRealign(target);
        GameManager.Inst.CheckBattleResult();
    }

    // 내 전방 전체 HP 회복 (최대치 한도)
    private void HealAllFront(bool isMine, int value)
    {
        foreach (Entity entity in EntityManager.Inst.GetFront(isMine))
        {
            entity.Heal(value);
        }
    }

    // 내 전방 전체 HP 버프 (최대치 초과 허용 = 공격력 증가)
    private void BuffAllFront(bool isMine, int value)
    {
        foreach (Entity entity in EntityManager.Inst.GetFront(isMine))
        {
            entity.BuffHp(value);
        }
    }

    // 선택한 내 엔티티 1체 HP 버프
    private void BuffSingle(Entity target, int value)
    {
        if (target != null)
        {
            target.BuffHp(value);
        }
    }
}
