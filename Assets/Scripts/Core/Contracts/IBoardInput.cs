using UnityEngine;

// 보드 마우스 입력 — 공격자/타겟 선택·세팅 이동·스킬 타겟 피킹 (BoardInputController 구현)
public interface IBoardInput
{
    bool IsSelectingAttacker { get; }

    void EntityMouseDown(Entity entity);
    void EntityMouseUp();
    void EntityMouseDrag();

    bool   PickSkillTarget(Vector3 pos);
    Entity ConsumeSkillTarget();
}
