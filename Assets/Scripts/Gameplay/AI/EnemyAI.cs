using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 상대(컴퓨터) 턴 행동을 담당한다.
// 카드를 한 장 배치한 뒤, 공격 가능한 자신의 엔티티들로 아군(보스 포함)을 무작위·시간차로 공격하고 턴을 종료한다.
public class EnemyAI : MonoBehaviour
{
    public static EnemyAI Inst { get; private set; }

    private readonly WaitForSeconds _putDelay    = new WaitForSeconds(1f);
    private readonly WaitForSeconds _attackDelay = new WaitForSeconds(2f);

    private void Awake()
    {
        Inst = this;
    }

    public void Play()
    {
        StartCoroutine(PlayCo());
    }

    private IEnumerator PlayCo()
    {
        CardManager.Inst.TryPutCard(false);

        yield return _putDelay;

        // 공격 가능한 상대 엔티티만 모아 순서를 섞는다
        var attackers = new List<Entity>(EntityManager.Inst.OtherEntities).FindAll(x => x.attackable);
        Utils.Shuffle(attackers);

        foreach (var attacker in attackers)
        {
            // 매 공격마다 최신 아군 목록 + 보스를 후보로 삼아 무작위 타겟을 고른다
            var defenders = new List<Entity>(EntityManager.Inst.MyEntities) { EntityManager.Inst.MyBoss };
            int rand      = Random.Range(0, defenders.Count);
            CombatSystem.Inst.Attack(attacker, defenders[rand]);

            // 공격 도중 게임이 끝나면(보스 사망 등) 즉시 중단한다
            if (TurnManager.Inst.isLoading)
            {
                yield break;
            }

            yield return _attackDelay;
        }

        TurnManager.Inst.EndTurn();
    }
}
