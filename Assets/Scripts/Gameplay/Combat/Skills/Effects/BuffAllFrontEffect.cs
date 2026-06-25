// 내 전방 전체 HP 버프 (최대치 초과 허용 = 공격력 증가). Duration으로 지속시간 조절.
public class BuffAllFrontEffect : ISkillEffect
{
    public void Execute(in SkillContext ctx)
    {
        foreach (Entity entity in Services.Get<IBoardState>().GetFront(ctx.IsMine))
        {
            entity.BuffHp(ctx.Value, ctx.Duration);
        }
    }
}
