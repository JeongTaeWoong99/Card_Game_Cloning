using System.Collections.Generic;
using UnityEngine;

// 보드(전장) 위의 마우스 입력을 해석한다 — 세팅 단계의 카드 이동/배치, 전투 단계의 공격자·타겟 선택, 스킬 타겟 피킹.
// 보드 상태(행 리스트·정렬·승급)는 EntityManager가 소유하고, 이 컨트롤러는 입력 → 보드 조작 오케스트레이션만 담당한다.
public class BoardInputController : MonoService<IBoardInput>, IBoardInput
{
    [CenterHeader("< 타겟 표시 >")]
    [SerializeField] private GameObject _targetPicker;

    private Entity _selectEntity;     // 전투: 선택한 공격자
    private Entity _targetPickEntity; // 전투/스킬: 현재 가리키는 타겟

    private Entity _moveEntity;     // 세팅 단계에서 집어든(이동 중) 카드
    private bool   _moveFromFront;  // 집어들 당시의 행(앞줄 여부)
    private int    _moveFromIndex;  // 집어들 당시의 행 내 인덱스

    // 공격자를 선택해 타겟팅 중인지 — 이때 호버 카드 미리보기를 막는다 (CardManager가 호출)
    public bool IsSelectingAttacker => _selectEntity != null;

    // 지정 엔티티가 현재 선택된 공격자 자신인지 — 자기 자신을 누른 동안엔 미리보기를 허용하기 위함 (CardManager가 호출)
    public bool IsSelectedAttacker(Entity entity) => _selectEntity == entity;

    private bool ExistTargetPickEntity => _targetPickEntity != null;

    // 내 전투 턴 + 로딩 중이 아닐 때만 보드 입력을 받는다
    private bool CanMouseInput
    {
        get
        {
            var turn = Services.Get<ITurnManager>();
            return turn.IsBattlePhase && turn.myTurn && !turn.isLoading;
        }
    }

    // 타겟 피커 표시를 매 프레임 갱신 (Unity 메시지)
    private void Update()
    {
        ShowTargetPicker(ExistTargetPickEntity);
    }

    // 엔티티 누름 — 세팅 단계면 이동 시작, 전투 단계면 공격자 선택 (Entity.OnMouseDown이 호출)
    public void EntityMouseDown(Entity entity)
    {
        if (Services.Get<ITurnManager>().IsSetupPhase)
        {
            if (!Services.Get<ITurnManager>().isLoading)
            {
                BeginSetupMove(entity);
            }

            return;
        }

        if (!CanMouseInput)
        {
            return;
        }

        if (entity.isWaiting) // 전투: 앞줄만 공격자 선택
        {
            return;
        }

        _selectEntity = entity;
    }

    // 대상 O 스킬 드래그 중 — 마우스 위치의 내 앞줄(공개) 엔티티를 타겟 피커로 지정한다. 유효 타겟이 잡히면 true
    // (CardManager가 매 프레임 호출)
    public bool PickSkillTarget(Vector3 pos)
    {
        _targetPickEntity = null;

        if (UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
        {
            return false;
        }

        foreach (var hit in Physics2D.RaycastAll(pos, Vector3.forward))
        {
            Entity entity = hit.collider?.GetComponent<Entity>();
            if (entity != null && entity.isMine && !entity.isWaiting && !entity.isEmpty)
            {
                _targetPickEntity = entity;
                break;
            }
        }

        return _targetPickEntity != null;
    }

    // 현재 지정된 스킬 타겟을 반환하고 피커를 해제한다 (CardManager가 발동/취소 시 호출)
    public Entity ConsumeSkillTarget()
    {
        Entity target = _targetPickEntity;
        _targetPickEntity = null;

        return target;
    }

    // 손을 뗌 — 세팅 단계면 이동 확정, 전투 단계면 공격 실행 (Entity.OnMouseUp이 호출)
    public void EntityMouseUp()
    {
        if (UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
        {
            _selectEntity = null;
            _targetPickEntity = null;
            Services.Get<ICardManager>().HideDamagePreview();
            return;
        }

        if (Services.Get<ITurnManager>().IsSetupPhase)
        {
            EndSetupMove();

            return;
        }

        if (!CanMouseInput)
        {
            return;
        }

        if (_selectEntity && _targetPickEntity && _selectEntity.attackable)
        {
            // 적 전방에 방패형(도발)이 있으면 근접 타입은 방패형만 공격할 수 있다 (원거리는 예외)
            if (Services.Get<IBoardState>().CanMeleeTarget(_selectEntity, _targetPickEntity))
            {
                Services.Get<ICombatSystem>().Attack(_selectEntity, _targetPickEntity);
            }
            else
            {
                Services.Get<ICardManager>().ShowWarning("전방에 도발(방패) 타입이 있으면 먼저 처치해야 다른 적을 공격할 수 있습니다.\n(원거리 타입은 도발을 무시하고 모든 적을 공격할 수 있습니다.)");
            }
        }

        _selectEntity     = null;
        _targetPickEntity = null;
        Services.Get<ICardManager>().HideDamagePreview(); // 공격 실행/취소 시 딜 예측 숨김
    }

    // 드래그 — 세팅 단계면 집어든 카드를 마우스로 이동, 전투 단계면 상대 앞줄을 타겟 지정 (Entity.OnMouseDrag이 호출)
    public void EntityMouseDrag()
    {
        if (UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
        {
            _targetPickEntity = null;
            Services.Get<ICardManager>().HideDamagePreview();

            if (Services.Get<ITurnManager>().IsSetupPhase && _moveEntity != null)
            {
                _moveEntity.MoveTransform(Utils.MousePos, false);
            }
            return;
        }

        if (Services.Get<ITurnManager>().IsSetupPhase)
        {
            if (_moveEntity != null)
            {
                _moveEntity.MoveTransform(Utils.MousePos, false);
            }

            return;
        }

        if (!CanMouseInput || _selectEntity == null)
        {
            return;
        }

        // 마우스 위치의 상대 앞줄 엔티티만 타겟으로 찾는다 (뒷줄 대기·빈 슬롯 제외)
        bool existTarget = false;
        foreach (var hit in Physics2D.RaycastAll(Utils.MousePos, Vector3.forward))
        {
            Entity entity = hit.collider?.GetComponent<Entity>();
            if (entity != null && !entity.isMine && !entity.isWaiting && !entity.isEmpty && _selectEntity.attackable)
            {
                _targetPickEntity = entity;
                existTarget       = true;
                break;
            }
        }

        // 타겟이 잡히면 딜 교환 예측을 표시하고, 없으면 숨긴다
        if (existTarget)
        {
            Services.Get<ICardManager>().ShowDamagePreview(_selectEntity, _targetPickEntity);
        }
        else
        {
            _targetPickEntity = null;
            Services.Get<ICardManager>().HideDamagePreview();
        }
    }

    // 세팅 단계 — 내 카드를 행에서 떼어내 이동을 시작한다
    private void BeginSetupMove(Entity entity)
    {
        _moveEntity    = entity;
        _moveFromFront = Services.Get<IBoardState>().GetRow(true, true).Contains(entity);
        _moveFromIndex = Services.Get<IBoardState>().GetRow(true, _moveFromFront).IndexOf(entity);

        Services.Get<IBoardState>().GetRow(true, _moveFromFront).Remove(entity);
        Services.Get<IBoardState>().EntityAlignment(true, _moveFromFront);
        entity.GetComponent<Order>()?.SetMostFrontOrder(true); // 드래그 동안 위로
    }

    // 세팅 단계 — 드롭한 행에 카드를 안착시킨다. 행이 가득 차 있으면 가장 가까운 카드와 자리를 맞바꾼다
    private void EndSetupMove()
    {
        if (_moveEntity == null)
        {
            return;
        }

        bool         tFront = BoardLayout.IsMyFrontRow(Utils.MousePos.y);
        List<Entity> tRow   = Services.Get<IBoardState>().GetRow(true, tFront);

        if (Services.Get<IBoardPlacement>().RowRealCount(true, tFront) < EntityManager.MaxRow)
        {
            InsertEntitySorted(tRow, _moveEntity, tFront); // 빈 자리에 삽입(같은 행 재배치 포함)
        }
        else
        {
            SwapIntoFullRow(_moveEntity, tFront);          // 가득 찬 행이면 스왑
        }

        Services.Get<IBoardState>().EntityAlignment(true, true);
        Services.Get<IBoardState>().EntityAlignment(true, false);
        _moveEntity = null;
    }

    // 카드를 x 위치 순서에 맞게 행에 삽입하고 행 상태를 갱신한다
    private void InsertEntitySorted(List<Entity> row, Entity entity, bool isFront)
    {
        row.Add(entity);
        row.Sort((a, b) => a.transform.position.x.CompareTo(b.transform.position.x));
        entity.SetRowState(isFront);
    }

    // 가득 찬 대상 행에서 마우스와 가장 가까운 카드를 골라, 그 자리를 이동 카드와 맞바꾼다
    private void SwapIntoFullRow(Entity entity, bool tFront)
    {
        var tRow = Services.Get<IBoardState>().GetRow(true, tFront);

        Entity nearest = tRow[0];
        float  best    = Mathf.Abs(nearest.transform.position.x - Utils.MousePos.x);
        foreach (var candidate in tRow)
        {
            float distance = Mathf.Abs(candidate.transform.position.x - Utils.MousePos.x);
            if (distance < best)
            {
                best    = distance;
                nearest = candidate;
            }
        }

        int nearestIndex = tRow.IndexOf(nearest);
        tRow[nearestIndex] = entity; // 대상 슬롯에 이동 카드
        entity.SetRowState(tFront);

        var origRow = Services.Get<IBoardState>().GetRow(true, _moveFromFront);
        origRow.Insert(Mathf.Clamp(_moveFromIndex, 0, origRow.Count), nearest); // 원래 자리에 교환 카드
        nearest.SetRowState(_moveFromFront);
    }

    // 타겟 피커를 표시하고 타겟 위치로 따라붙인다
    private void ShowTargetPicker(bool isShow)
    {
        _targetPicker.SetActive(isShow);

        if (ExistTargetPickEntity)
        {
            _targetPicker.transform.position = _targetPickEntity.transform.position;
        }
    }
}
