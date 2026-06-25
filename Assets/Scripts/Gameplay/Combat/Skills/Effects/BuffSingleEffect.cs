// 선택한 내 엔티티 1체 HP 버프. Duration으로 지속시간 조절.
public class BuffSingleEffect : ISkillEffect
{
    public void Execute(in SkillContext ctx)
    {
        if (ctx.Target != null)
        {
            ctx.Target.BuffHp(ctx.Value, ctx.Duration);
        }
    }
}
