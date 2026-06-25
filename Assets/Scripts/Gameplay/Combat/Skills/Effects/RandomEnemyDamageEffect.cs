using UnityEngine;

// 무작위 적 전방 1체에게 포크포인트에서 화살을 쏜다 (도착 시 피해·정리·승패는 CombatSystem이 처리).
public class RandomEnemyDamageEffect : ISkillEffect
{
    public void Execute(in SkillContext ctx)
    {
        Entity target = Services.Get<IBoardState>().GetRandomEnemyFront(ctx.IsMine);
        if (target == null)
        {
            return;
        }

        // 포크포인트가 있으면 거기서, 없으면 대상 위치에서 발사 (기존 폴백 유지)
        Vector3 from = ctx.HasCastPoint ? ctx.CastFrom : target.transform.position;
        Services.Get<ICombatSystem>().FirePokeArrow(from, target, ctx.Value);
    }
}
