using System.Collections;
using UnityEngine;
using DG.Tweening;

// 카드 타입별 행동/표시의 베이스. 공통 텍스트 포맷(<공격>/<능력>·대기 안내)을 여기서 처리하고,
// 타입별 원본 데이터(이름·설명·대기턴)는 파생 클래스가 제공한다 (Template Method).
public abstract class CardBehaviour : ICardBehaviour
{
    public abstract int    WaitTurn    { get; }
    public abstract string DisplayName { get; }

    protected abstract string AttackDescription  { get; }
    protected abstract string AbilityDescription { get; }

    // <공격> 표기 + 끝에 공격 대기시간 안내를 붙인다
    public string AttackText
    {
        get
        {
            string waitText = WaitTurn == 0
                ? "(전방 배치 후 바로 공격 가능)"
                : $"(전방 배치 후 {WaitTurn}턴 동안 대기 상태)";

            return $"<공격>\n{AttackDescription}{waitText}";
        }
    }

    // <능력> 표기
    public string AbilityText => $"<능력>\n{AbilityDescription}";

    // 전투 규칙 기본값 — 특수 타입(방패·원거리)만 override 한다
    public virtual int  ModifyIncomingDamage(int rawDamage) => rawDamage;
    public virtual bool IsTaunter    => false;
    public virtual bool IgnoresTaunt => false;

    // 반격을 받는가 — 원거리만 false (딜 예측·전투 동일 기준)
    public virtual bool ReceivesCounter => true;

    // 공격 연출/해석 기본값 = 근접 — 대상 위치로 이동 후 양측 현재 HP 절반씩 동시 피해(반격 O). 특수 타입만 override
    public virtual void Attack(Entity attacker, Entity defender)
    {
        attacker.GetComponent<Order>().SetMostFrontOrder(true);

        ICombatSystem cs = Services.Get<ICombatSystem>();

        DOTween.Sequence()
            .Append(attacker.transform.DOMove(defender.originPos, CombatSystem.MoveTime)).SetEase(Ease.InSine)
            .AppendCallback(() =>
            {
                // 순서 의존을 없애기 위해 양측 피해를 먼저 계산(현재 HP 절반·방패 경감)한 뒤 동시 적용한다
                int toDefender = defender.ApplyDefense(CombatSystem.CalcDamage(attacker.health, CombatSystem.DamageRatio));
                int toAttacker = attacker.ApplyDefense(CombatSystem.CalcDamage(defender.health, CombatSystem.CounterRatio));

                defender.Damaged(toDefender);
                attacker.Damaged(toAttacker);
                cs.ShowDamagePopup(toDefender, defender.transform);
                cs.ShowDamagePopup(toAttacker, attacker.transform);
            })
            .Append(attacker.transform.DOMove(attacker.originPos, CombatSystem.MoveTime)).SetEase(Ease.OutSine)
            .OnComplete(() => cs.FinishAttack(attacker, defender));
    }

    // 턴 시작 패시브 기본값 — 없음 (원거리·무쌍·힐러만 override)
    public virtual IEnumerator OnTurnStartPassive(TurnPassiveContext ctx)
    {
        yield break;
    }
}
