using UnityEngine;

// 스킬 효과를 실제로 적용하는 실행기. ESkillEffect로 분기해 동작을 처리한다 (SRP: 효과 적용만 담당).
// 새 효과를 추가할 때만 여기에 case를 더하고, 같은 효과의 수치 변형은 SkillSO에서만 만든다 (OCP).
public class SkillSystem : MonoBehaviour
{
    public static SkillSystem Inst { get; private set; }

    [CenterHeader("< 투사체 스킬 발사 위치(포크포인트) >")]
    [SerializeField] private Transform _myCastPoint;    // 내 투사체 스킬 화살 시작 위치
    [SerializeField] private Transform _otherCastPoint; // 상대 투사체 스킬 화살 시작 위치

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
                DealRandomEnemyDamage(isMine, skill.value);
                break;

            case ESkillEffect.HealAllFront:
                HealAllFront(isMine, skill.value);
                break;

            case ESkillEffect.BuffAllFront:
                BuffAllFront(isMine, skill.value, skill.buffDuration);
                break;

            case ESkillEffect.BuffSingle:
                BuffSingle(target, skill.value, skill.buffDuration);
                break;
        }
    }

    // 무작위 적 전방 1체에게 포크포인트에서 화살을 쏜다 (도착 시 피해·정리·승패는 CombatSystem이 처리)
    // (스킬 RandomEnemyDamage 효과가 호출)
    public void DealRandomEnemyDamage(bool isMine, int value)
    {
        Entity target = EntityManager.Inst.GetRandomEnemyFront(isMine);
        if (target == null)
        {
            return;
        }

        // 포크포인트에서 화살을 쏘고, 도착하면 피해를 적용한다 (미설정 시 대상 위치에서 즉시 처리)
        Transform castPoint = isMine ? _myCastPoint : _otherCastPoint;
        Vector3   from      = castPoint != null ? castPoint.position : target.transform.position;
        CombatSystem.Inst.FirePokeArrow(from, target, value);
    }

    // 내 전방 전체 HP 회복 (최대치 한도)
    private void HealAllFront(bool isMine, int value)
    {
        foreach (Entity entity in EntityManager.Inst.GetFront(isMine))
        {
            entity.Heal(value);
        }
    }

    // 내 전방 전체 HP 버프 (최대치 초과 허용 = 공격력 증가). duration으로 지속시간 조절
    private void BuffAllFront(bool isMine, int value, int duration)
    {
        foreach (Entity entity in EntityManager.Inst.GetFront(isMine))
        {
            entity.BuffHp(value, duration);
        }
    }

    // 선택한 내 엔티티 1체 HP 버프. duration으로 지속시간 조절
    private void BuffSingle(Entity target, int value, int duration)
    {
        if (target != null)
        {
            target.BuffHp(value, duration);
        }
    }
}
