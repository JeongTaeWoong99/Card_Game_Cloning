using System.Collections.Generic;
using UnityEngine;

// 스킬 효과 실행의 진입점·라우터. ESkillEffect → ISkillEffect 매핑에서 효과를 꺼내 실행한다.
// 효과 로직은 각 ISkillEffect 구현이 담당하고(SRP), 새 효과는 구현 클래스 추가 + 등록 1줄로 확장한다(OCP).
public class SkillSystem : MonoService<ISkillSystem>, ISkillSystem
{
    [CenterHeader("< 투사체 스킬 발사 위치(포크포인트) >")]
    [SerializeField] private Transform _myCastPoint;    // 내 투사체 스킬 화살 시작 위치
    [SerializeField] private Transform _otherCastPoint; // 상대 투사체 스킬 화살 시작 위치

    // 효과 종류 → 실행 객체. 새 효과를 추가할 때만 이 표에 한 줄을 더한다 (Cast·기존 효과는 무수정)
    private Dictionary<ESkillEffect, ISkillEffect> _effects;

    // 서비스 등록(베이스) + 효과 매핑 구성 (Unity 메시지)
    protected override void Awake()
    {
        base.Awake();

        _effects = new Dictionary<ESkillEffect, ISkillEffect>
        {
            { ESkillEffect.RandomEnemyDamage, new RandomEnemyDamageEffect() },
            { ESkillEffect.HealAllFront,      new HealAllFrontEffect()      },
            { ESkillEffect.BuffAllFront,      new BuffAllFrontEffect()      },
            { ESkillEffect.BuffSingle,        new BuffSingleEffect()        },
        };
    }

    // 스킬 효과를 실행한다. 대상 O 스킬은 target을 받는다 (CardManager·EnemyAI가 호출)
    public void Cast(Skill skill, bool isMine, Entity target = null)
    {
        if (!_effects.TryGetValue(skill.effect, out ISkillEffect effect))
        {
            return; // 미등록 효과 방어
        }

        // 투사체 시작 위치(포크포인트)를 진영별로 정하고, 미할당이면 효과 측에서 대상 위치로 폴백한다
        Transform castPoint    = isMine ? _myCastPoint : _otherCastPoint;
        bool      hasCastPoint = castPoint != null;
        Vector3   castFrom     = hasCastPoint ? castPoint.position : Vector3.zero;

        var ctx = new SkillContext(isMine, target, skill.value, skill.buffDuration, castFrom, hasCastPoint);
        effect.Execute(in ctx);
    }
}
