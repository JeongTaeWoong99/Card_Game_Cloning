using System.Collections;
using UnityEngine;
using DG.Tweening;

// 무쌍 — 광역 공격, 후방에서 충전. 배치 후 2턴 대기.
public class MusouBehaviour : CardBehaviour
{
    private const float ChargeChance = 0.5f; // 후방 충전 발동 확률
    private const int   ChargeAmount = 1;    // 충전 시 HP 증가량(영구)
    private const float SplashRatio  = 0.5f; // 인접 추가 피해 비율(본체 피해의 절반 = 원래 HP의 25%)

    public override int    WaitTurn    => 2;
    public override string DisplayName => "무쌍";

    protected override string AttackDescription  => "대상에게 현재 HP 50% 피해 + 인접 적 1장에 25% 피해. 상대도 현재 HP 절반만큼 반격.";
    protected override string AbilityDescription => "후방에 있을 때, 내 턴 시작 시 50% 확률로 자신 HP +1 충전.";

    // 무쌍 공격 — 이동 후 대상에 현재 HP 50%, 인접 적 1체(랜덤)에 25% 피해 + 대상의 반격(현재 HP 절반)을 받음
    public override void Attack(Entity attacker, Entity defender)
    {
        attacker.GetComponent<Order>().SetMostFrontOrder(true);

        ICombatSystem cs = Services.Get<ICombatSystem>();
        Entity splashTarget = Services.Get<IBoardState>().GetRandomAdjacentFront(defender);

        DOTween.Sequence()
            .Append(attacker.transform.DOMove(defender.originPos, CombatSystem.MoveTime)).SetEase(Ease.InSine)
            .AppendCallback(() =>
            {
                int defenderHp = defender.health; // 반격용 — 공격 전 HP 캡처
                int mainDamage = defender.ApplyDefense(CombatSystem.CalcDamage(attacker.health, CombatSystem.DamageRatio));

                defender.Damaged(mainDamage);
                cs.ShowDamagePopup(mainDamage, defender.transform);

                if (splashTarget != null)
                {
                    int splashDamage = splashTarget.ApplyDefense(CombatSystem.CalcDamage(attacker.health, CombatSystem.DamageRatio * SplashRatio));
                    splashTarget.Damaged(splashDamage);
                    cs.ShowDamagePopup(splashDamage, splashTarget.transform);
                }

                int toAttacker = attacker.ApplyDefense(CombatSystem.CalcDamage(defenderHp, CombatSystem.CounterRatio));
                attacker.Damaged(toAttacker);
                cs.ShowDamagePopup(toAttacker, attacker.transform);
            })
            .Append(attacker.transform.DOMove(attacker.originPos, CombatSystem.MoveTime)).SetEase(Ease.OutSine)
            .OnComplete(() => cs.FinishAttack(attacker, defender, splashTarget));
    }

    // 후방에 있을 때만, 내 턴 시작 시 50% 확률로 자신 HP +1 영구 버프(승격 시 강해짐)
    public override IEnumerator OnTurnStartPassive(TurnPassiveContext ctx)
    {
        if (ctx.IsFront || Random.value >= ChargeChance)
        {
            yield break;
        }

        ctx.Self.BuffHp(ChargeAmount, 0);

        yield return ctx.Delay;
    }
}
