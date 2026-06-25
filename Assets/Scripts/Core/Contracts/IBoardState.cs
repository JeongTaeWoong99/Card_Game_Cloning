using System.Collections;
using System.Collections.Generic;

// 전장 진영 상태·정렬·사망 제거·턴 처리·쿼리 (EntityManager 구현)
public interface IBoardState
{
    IReadOnlyList<Entity> MyFront    { get; }
    IReadOnlyList<Entity> OtherFront { get; }
    bool IsMyAllDead    { get; }
    bool IsOtherAllDead { get; }

    List<Entity> GetRow(bool isMine, bool isFront);
    void EntityAlignment(bool isMine, bool isFront);
    void RemoveDeadAndRealign(params Entity[] entities);

    IEnumerator ProcessTurnStartEffectsCo(bool isMine);
    void TickBuffs(bool isMine);

    IReadOnlyList<Entity> GetFront(bool isMine);
    Entity GetRandomFront(bool isMine);
    Entity GetRandomEnemyFront(bool isMine);
    Entity GetRandomAdjacentFront(Entity target);
    bool   CanMeleeTarget(Entity attacker, Entity target);
}
