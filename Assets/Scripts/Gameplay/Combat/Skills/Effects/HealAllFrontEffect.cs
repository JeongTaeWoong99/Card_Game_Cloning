// 내 전방 전체 HP 회복 (최대치 한도).
public class HealAllFrontEffect : ISkillEffect
{
    public void Execute(in SkillContext ctx)
    {
        foreach (Entity entity in Services.Get<IBoardState>().GetFront(ctx.IsMine))
        {
            entity.Heal(ctx.Value);
        }
    }
}
