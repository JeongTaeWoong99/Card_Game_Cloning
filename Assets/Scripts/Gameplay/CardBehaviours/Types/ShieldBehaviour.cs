using UnityEngine;

// 방패 — 피해 경감 + 도발(적 근접이 이 카드만 노림).
public class ShieldBehaviour : CardBehaviour
{
    private const int DamageReduction = 2; // 받는 피해에서 차감(최소 1 보장)

    public override int    WaitTurn    => 0;
    public override string DisplayName => "방패";

    protected override string AttackDescription  => "현재 HP 절반만큼 피해. 상대도 현재 HP 절반만큼 반격.";
    protected override string AbilityDescription => "받는 모든 피해 -2(최소 1). 적 근접 공격은 이 카드만 노림(도발).";

    // 받는 모든 피해 -2 (최소 1 보장)
    public override int ModifyIncomingDamage(int rawDamage) => Mathf.Max(1, rawDamage - DamageReduction);

    // 적 근접 공격이 이 카드만 노리게 한다 (도발)
    public override bool IsTaunter => true;
}
