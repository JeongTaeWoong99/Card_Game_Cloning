using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 힐러 — 매 턴 시작 시 다른 전방 아군 회복.
public class HealerBehaviour : CardBehaviour
{
    private const int FrontTicks = 1; // 전방 힐러의 회복 횟수
    private const int BackTicks  = 3; // 후방 힐러의 회복 횟수
    private const int HealAmount = 1; // 1틱당 회복량

    public override int    WaitTurn    => 0;
    public override string DisplayName => "힐러";

    protected override string AttackDescription  => "현재 HP 절반만큼 피해. 상대도 현재 HP 절반만큼 반격.";
    protected override string AbilityDescription => "내 턴 시작 시 회복 가능한 다른 전방 아군 HP 회복(전방=1회 / 후방=1씩 3회).";

    // 회복 가능한 다른 전방 아군을 1씩 여러 틱 회복한다. 틱마다 텀을 둬 +N 팝업이 겹치지 않게 한다
    public override IEnumerator OnTurnStartPassive(TurnPassiveContext ctx)
    {
        int ticks = ctx.IsFront ? FrontTicks : BackTicks;

        for (int tick = 0; tick < ticks; tick++)
        {
            Entity target = PickHealTarget(ctx.Self, ctx.IsMine);
            if (target == null) // 회복할 대상이 없으면 조기 종료
            {
                yield break;
            }

            target.Heal(HealAmount);

            yield return ctx.Delay;
        }
    }

    // 자신 제외, 회복 가능한 전방 아군 무작위 1명 (없으면 null)
    private static Entity PickHealTarget(Entity healer, bool isMine)
    {
        var candidates = new List<Entity>();
        foreach (Entity entity in Services.Get<IBoardState>().GetFront(isMine))
        {
            if (entity != healer && entity.CanHeal)
            {
                candidates.Add(entity);
            }
        }

        return candidates.Count == 0 ? null : candidates[Random.Range(0, candidates.Count)];
    }
}
