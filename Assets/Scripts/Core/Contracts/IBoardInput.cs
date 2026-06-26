using UnityEngine;

// 보드 마우스 입력 — 공격자/타겟 선택·세팅 이동·스킬 타겟 피킹 (BoardInputController 구현)
public interface IBoardInput
{
    bool IsSelectingAttacker { get; }

    // 지정 엔티티가 현재 선택된 공격자 자신인지 (자기 자신 호버 미리보기 허용 판정용)
    bool IsSelectedAttacker(Entity entity);

    void EntityMouseDown(Entity entity);
    void EntityMouseUp();
    void EntityMouseDrag();

    bool   PickSkillTarget(Vector3 pos);
    Entity ConsumeSkillTarget();
}
