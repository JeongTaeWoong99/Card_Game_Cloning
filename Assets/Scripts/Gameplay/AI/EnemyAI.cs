using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 상대(컴퓨터)의 행동을 담당한다.
// Setup 페이즈: 자기 6장을 앞줄 3 + 뒷줄 3에 자동 배치.
// Battle 페이즈: 공격 가능한 앞줄 엔티티로 내 앞줄 카드를 무작위·시간차로 공격하고 턴을 종료.
public class EnemyAI : MonoService<IEnemyAI>, IEnemyAI
{
    private const int   RowCount        = 3;    // 한 행에 놓을 카드 수
    private const float SkillCastChance = 0.6f; // 보유 스킬을 한 번 더 시전할 확률

    private readonly WaitForSeconds _putDelay    = new WaitForSeconds(1f);
    private readonly WaitForSeconds _attackDelay = new WaitForSeconds(2f);

    #region 배치 페이즈

    // 상대 자동 배치 시작 (TurnManager.StartGameCo가 호출)
    public void SetupPlace()
    {
        StartCoroutine(SetupPlaceCo());
    }

    // 앞줄 3장 → 뒷줄 3장 순으로 자기 손패를 배치한다
    private IEnumerator SetupPlaceCo()
    {
        bool fast = Services.Get<ITurnManager>().IsFastMode; // 페스트 모드면 딜레이 없이 한순간에 배치

        for (int i = 0; i < RowCount; i++)
        {
            Services.Get<ICardManager>().TryPutCard(false, true); // 앞줄
            if (!fast)
            {
                yield return _putDelay;
            }
        }

        for (int i = 0; i < RowCount; i++)
        {
            Services.Get<ICardManager>().TryPutCard(false, false); // 뒷줄
            if (!fast)
            {
                yield return _putDelay;
            }
        }

        Services.Get<ICardManager>().DrawSkillCards(false, TurnManager.SetupSkillDraw); // 배치 완료 시 상대 스킬 4장
    }

    #endregion

    #region 전투 페이즈

    // 상대 턴 행동 시작 (상대 턴 시작 효과가 끝난 뒤 TurnManager.StartTurnCo가 호출)
    public void Play()
    {
        StartCoroutine(PlayCo());
    }

    // 공격 가능한 앞줄 엔티티로 내 앞줄 카드를 무작위·시간차로 공격 → 턴 종료
    private IEnumerator PlayCo()
    {
        yield return _putDelay;

        // 마나가 되면 일정 확률로 보유 스킬을 시전한다 (간단 AI)
        yield return UseSkillsCo();

        // 스킬(무작위 피해)로 게임이 끝났으면 즉시 중단한다
        if (Services.Get<ITurnManager>().isLoading)
        {
            yield break;
        }

        // 공격 가능한 상대 앞줄 엔티티만 모아 순서를 섞는다
        var attackers = new List<Entity>(Services.Get<IBoardState>().OtherFront).FindAll(x => x.attackable);
        Utils.Shuffle(attackers);

        foreach (var attacker in attackers)
        {
            // 반격으로 먼저 죽은 공격자는 건너뛴다
            if (attacker == null || attacker.isDie)
            {
                continue;
            }

            // 매 공격마다 최신 내 앞줄을 후보로 삼아 무작위 타겟을 고른다 (도발: 방패형이 있으면 방패형만)
            var defenders = new List<Entity>(Services.Get<IBoardState>().MyFront)
                .FindAll(target => Services.Get<IBoardState>().CanMeleeTarget(attacker, target));
            if (defenders.Count == 0)
            {
                break;
            }

            // 도발 무시 공격자(원거리)는 도발 유발자가 아닌 카드를 우선 저격하고, 도발 유발자만 남으면 그것을 친다
            if (attacker.Behaviour.IgnoresTaunt)
            {
                var nonTaunter = defenders.FindAll(target => !target.Behaviour.IsTaunter);
                if (nonTaunter.Count > 0)
                {
                    defenders = nonTaunter;
                }
            }

            int rand = Random.Range(0, defenders.Count);
            Services.Get<ICombatSystem>().Attack(attacker, defenders[rand]);

            // 공격 도중 게임이 끝나면(전멸 등) 즉시 중단한다
            if (Services.Get<ITurnManager>().isLoading)
            {
                yield break;
            }

            yield return _attackDelay;
        }

        Services.Get<ITurnManager>().EndTurn();
    }

    // 마나가 되는 동안 확률적으로 보유 스킬을 시전한다 (게임 종료 시 즉시 중단)
    private IEnumerator UseSkillsCo()
    {
        while (Random.value < SkillCastChance)
        {
            if (!Services.Get<ICardManager>().TryCastOtherSkill())
            {
                break;
            }

            yield return _attackDelay;

            if (Services.Get<ITurnManager>().isLoading)
            {
                yield break;
            }
        }
    }

    #endregion
}
