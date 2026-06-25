using UnityEngine;
using DG.Tweening;

// 흡혈 — 공격 후 생존 시 회복.
public class VampireBehaviour : CardBehaviour
{
    private const int HealAmount = 3; // 공격 후 살아남았을 때 회복하는 고정량

    public override int    WaitTurn    => 0;
    public override string DisplayName => "흡혈";

    protected override string AttackDescription  => "현재 HP 절반만큼 피해. 상대도 현재 HP 절반만큼 반격. 살아남으면 자신 HP +3 회복.";
    protected override string AbilityDescription => "없음.";

    // 흡혈 공격 — 근접 이동 후 대상에 피해 → 반격을 받고 → 제자리로 돌아온 뒤 살아남았으면 회복(+HealAmount)
    public override void Attack(Entity attacker, Entity defender)
    {
        attacker.GetComponent<Order>().SetMostFrontOrder(true);

        ICombatSystem cs = Services.Get<ICombatSystem>();
        bool attackerDied = false;

        DOTween.Sequence()
            .Append(attacker.transform.DOMove(defender.originPos, CombatSystem.MoveTime)).SetEase(Ease.InSine)
            .AppendCallback(() =>
            {
                int defenderHp = defender.health; // 반격용 — 공격 전 HP 캡처
                int toDefender = defender.ApplyDefense(CombatSystem.CalcDamage(attacker.health, CombatSystem.DamageRatio));

                defender.Damaged(toDefender);
                cs.ShowDamagePopup(toDefender, defender.transform);

                int toAttacker = attacker.ApplyDefense(CombatSystem.CalcDamage(defenderHp, CombatSystem.CounterRatio));
                attackerDied   = attacker.Damaged(toAttacker);
                cs.ShowDamagePopup(toAttacker, attacker.transform);
            })
            .Append(attacker.transform.DOMove(attacker.originPos, CombatSystem.MoveTime)).SetEase(Ease.OutSine)
            .AppendCallback(() =>
            {
                if (!attackerDied) // 제자리로 돌아온 뒤 생존 시 회복
                {
                    attacker.Heal(HealAmount);
                }
            })
            .OnComplete(() => cs.FinishAttack(attacker, defender));
    }
}
