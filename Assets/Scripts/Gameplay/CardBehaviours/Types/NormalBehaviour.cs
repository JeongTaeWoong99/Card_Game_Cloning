// 일반 — 특별한 후방 효과·대기 없음.
public class NormalBehaviour : CardBehaviour
{
    public override int    WaitTurn    => 0;
    public override string DisplayName => "일반";

    protected override string AttackDescription  => "현재 HP 절반만큼 피해. 상대도 현재 HP 절반만큼 반격.";
    protected override string AbilityDescription => "없음.";
}
