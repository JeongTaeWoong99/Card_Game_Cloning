using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

// 전장(보드) 엔티티의 상태를 소유한다: 스폰 / 빈 슬롯 미리보기 / 선택·타겟 / 정렬 / 사망 제거·승격.
// 진영마다 앞줄(공개, 전투 참여)과 뒷줄(대기, 공격·피격 불가)을 둔다. (행당 최대 3)
// 전투 해석은 CombatSystem, 상대 턴 행동은 EnemyAI, 승패 판정은 GameManager가 담당한다.
public class EntityManager : MonoBehaviour
{
    public static EntityManager Inst { get; private set; }

    private const int   MaxRow            = 3;    // 한 행(앞줄/뒷줄)의 최대 슬롯 수
    private const int   HealerHealAmount  = 1;    // 힐러 1틱당 회복량
    private const int   FrontHealerTicks  = 1;    // 전방 힐러의 회복 횟수
    private const int   BackHealerTicks   = 3;    // 후방 힐러의 회복 횟수
    private const float MusouChargeChance = 0.5f; // 무쌍 후방 충전 발동 확률
    private const float BackEffectDelay   = 0.3f; // 후방·턴시작 효과를 하나씩 보여주기 위한 텀(초)

    [CenterHeader("< 프리팹 >")]
    [SerializeField] private GameObject _entityPrefab;

    [CenterHeader("< 진영 엔티티 (앞줄=공개 / 뒷줄=대기) >")]
    [SerializeField] private List<Entity> _myFront;
    [SerializeField] private List<Entity> _myBack;
    [SerializeField] private List<Entity> _otherFront;
    [SerializeField] private List<Entity> _otherBack;

    [CenterHeader("< 빈 슬롯(배치 미리보기) >")]
    [SerializeField] private Entity _myEmptyEntity;

    [CenterHeader("< 타겟 표시 >")]
    [SerializeField] private GameObject _targetPicker;

    private Entity _selectEntity;
    private Entity _targetPickEntity;

    private Entity _moveEntity;     // 세팅 단계에서 집어든(이동 중) 카드
    private bool   _moveFromFront;  // 집어들 당시의 행(앞줄 여부)
    private int    _moveFromIndex;  // 집어들 당시의 행 내 인덱스

    private readonly WaitForSeconds _backEffectDelay = new(BackEffectDelay); // 효과 사이 텀(캐싱)

    // 외부(CombatSystem/EnemyAI)가 타겟·공격 후보로 쓰는 앞줄(공개) 엔티티
    public IReadOnlyList<Entity> MyFront    => _myFront;
    public IReadOnlyList<Entity> OtherFront => _otherFront;

    // 공격자를 선택해 타겟팅 중인지 — 이때 호버 카드 미리보기를 막는다 (CardManager가 호출)
    public bool IsSelectingAttacker => _selectEntity != null;

    // 승패 판정용 — 진영의 카드(앞줄+뒷줄)가 모두 제거되었는지
    public bool IsMyAllDead    => _myFront.Count == 0 && _myBack.Count == 0;
    public bool IsOtherAllDead => _otherFront.Count == 0 && _otherBack.Count == 0;

    // 내가 배치한 실제 카드 수(빈 슬롯 제외) / 6장 모두 배치 완료 여부
    public int  MyPlacedCount   => RealCount(_myFront) + RealCount(_myBack);
    public bool IsMyPlaceDone    => RealCount(_myFront) >= MaxRow && RealCount(_myBack) >= MaxRow;
    public bool IsOtherPlaceDone => _otherFront.Count >= MaxRow && _otherBack.Count >= MaxRow;

    private bool ExistTargetPickEntity => _targetPickEntity != null;
    private bool CanMouseInput         => TurnManager.Inst.IsBattlePhase && TurnManager.Inst.myTurn && !TurnManager.Inst.isLoading;


    // 싱글톤 등록 (Unity 메시지)
    private void Awake()
    {
        Inst = this;
    }

    // 빈 슬롯 미리보기 숨김 (Unity 메시지)
    private void Start()
    {
        _myEmptyEntity.gameObject.SetActive(false); // 드래그 중에만 보이게 한다
    }

    // 타겟 피커 표시를 매 프레임 갱신 (Unity 메시지)
    private void Update()
    {
        ShowTargetPicker(ExistTargetPickEntity);
    }

    #region 스폰 / 슬롯 / 정렬

    // 지정한 진영·행에 엔티티를 생성한다. 내 카드는 미리보기 빈 슬롯 자리에, 상대는 행 끝에 채운다
    public bool SpawnEntity(bool isMine, Item item, bool isFrontRow, Vector3 spawnPos)
    {
        var rowList = GetRow(isMine, isFrontRow);

        if (isMine)
        {
            if (!ExistEmptyIn(rowList) || RealCount(rowList) >= MaxRow)
            {
                return false;
            }
        }
        else if (rowList.Count >= MaxRow)
        {
            return false;
        }

        var entityObject = Instantiate(_entityPrefab, spawnPos, Utils.QI);
        var entity       = entityObject.GetComponent<Entity>();

        entity.isMine = isMine;
        // 상대 대기 카드도 내 카드처럼 공개(앞면)로 보여준다
        entity.Setup(item, true, !isFrontRow);

        if (isMine)
        {
            rowList[rowList.IndexOf(_myEmptyEntity)] = entity; // 빈 슬롯 자리를 실제 엔티티로 교체
            _myEmptyEntity.gameObject.SetActive(false);
        }
        else
        {
            rowList.Add(entity);
        }

        EntityAlignment(isMine, isFrontRow);

        return true;
    }

    // 치트 — 빈 슬롯 미리보기 없이 내 카드 1장을 앞줄 우선(차면 뒷줄)으로 배치한다 (CardManager.CheatAutoPlaceMyCards가 호출)
    public bool CheatPlaceMyCard(Item item)
    {
        bool isFrontRow = RealCount(_myFront) < MaxRow;
        var  rowList    = GetRow(true, isFrontRow);

        if (RealCount(rowList) >= MaxRow)
        {
            return false; // 앞줄·뒷줄 모두 가득
        }

        Vector3 spawnPos = BoardLayout.GetSlotPosition(true, isFrontRow, rowList.Count, MaxRow);
        var     entity   = Instantiate(_entityPrefab, spawnPos, Utils.QI).GetComponent<Entity>();

        entity.isMine = true;
        entity.Setup(item, true, !isFrontRow); // 내 카드는 항상 앞면, 뒷줄이면 대기 상태

        rowList.Add(entity);
        EntityAlignment(true, isFrontRow);

        return true;
    }

    // 드래그한 카드의 위치(x, y)에 맞는 행을 골라 임시 빈 슬롯을 끼워 미리보기를 제공한다 (내 배치 전용)
    public void InsertMyEmptyEntity(float xPos, float yPos)
    {
        bool isFront      = BoardLayout.IsMyFrontRow(yPos);
        var  targetRow    = GetRow(true, isFront);
        var  oppositeRow  = GetRow(true, !isFront);

        // 반대 행에 빈 슬롯이 남아 있으면 회수하고 그 행을 재정렬한다
        if (ExistEmptyIn(oppositeRow))
        {
            oppositeRow.Remove(_myEmptyEntity);
            EntityAlignment(true, !isFront);
        }

        // 대상 행이 가득 찼으면 미리보기를 띄우지 않는다
        if (RealCount(targetRow) >= MaxRow)
        {
            if (ExistEmptyIn(targetRow))
            {
                targetRow.Remove(_myEmptyEntity);
                EntityAlignment(true, isFront);
            }

            _myEmptyEntity.gameObject.SetActive(false);

            return;
        }

        if (!ExistEmptyIn(targetRow))
        {
            targetRow.Add(_myEmptyEntity);
        }

        _myEmptyEntity.gameObject.SetActive(true);

        // x 위치로 슬롯 순서를 잡고, 순서가 바뀌면 재정렬한다
        Vector3 emptyPos = _myEmptyEntity.transform.position;
        emptyPos.x = xPos;
        _myEmptyEntity.transform.position = emptyPos;

        int beforeIndex = targetRow.IndexOf(_myEmptyEntity);
        targetRow.Sort((a, b) => a.transform.position.x.CompareTo(b.transform.position.x));
        if (targetRow.IndexOf(_myEmptyEntity) != beforeIndex)
        {
            EntityAlignment(true, isFront);
        }
    }

    // 임시 빈 슬롯을 제거하고 재정렬한다 (배치 취소 시)
    public void RemoveMyEmptyEntity()
    {
        if (ExistEmptyIn(_myFront))
        {
            _myFront.Remove(_myEmptyEntity);
            EntityAlignment(true, true);
        }

        if (ExistEmptyIn(_myBack))
        {
            _myBack.Remove(_myEmptyEntity);
            EntityAlignment(true, false);
        }

        _myEmptyEntity.gameObject.SetActive(false);
    }

    // 죽은 앞줄 엔티티를 제거하고, 뒷줄 대기 카드를 왼쪽부터 앞줄로 승격시킨 뒤 흔들기→축소 연출로 정리한다
    public void RemoveDeadAndRealign(params Entity[] entities)
    {
        foreach (var entity in entities)
        {
            if (!entity.isDie || entity.isEmpty)
            {
                continue;
            }

            bool isMine    = entity.isMine;
            var  frontList = isMine ? _myFront : _otherFront;

            frontList.Remove(entity);

            bool promoted = PromoteFromBack(isMine); // 뒷줄 → 앞줄 승격(왼쪽부터)
            EntityAlignment(isMine, true);
            if (promoted)
            {
                EntityAlignment(isMine, false);
            }

            DOTween.Sequence()
                .Append(entity.transform.DOShakePosition(1.3f))
                .Append(entity.transform.DOScale(Vector3.zero, 0.3f)).SetEase(Ease.OutCirc)
                .OnComplete(() => Destroy(entity.gameObject));
        }
    }

    // 해당 진영 뒷줄의 가장 왼쪽 카드를 앞줄 빈 자리로 승격한다 (성공 시 true)
    private bool PromoteFromBack(bool isMine)
    {
        var frontList = isMine ? _myFront : _otherFront;
        var backList  = isMine ? _myBack  : _otherBack;

        if (frontList.Count >= MaxRow || backList.Count == 0)
        {
            return false;
        }

        var promoted = backList[0]; // 왼쪽부터
        backList.RemoveAt(0);
        frontList.Add(promoted);
        promoted.Promote(); // 공개 전환 + 대기시간 재설정

        return true;
    }

    // 한 행의 엔티티들을 BoardLayout 슬롯 위치로 이동시키고 정렬 순서를 갱신한다
    private void EntityAlignment(bool isMine, bool isFront)
    {
        var targetRow = GetRow(isMine, isFront);

        for (int i = 0; i < targetRow.Count; i++)
        {
            var targetEntity = targetRow[i];
            targetEntity.originPos = BoardLayout.GetSlotPosition(isMine, isFront, i, targetRow.Count);
            targetEntity.MoveTransform(targetEntity.originPos, true, 0.5f);
            targetEntity.GetComponent<Order>()?.SetOriginOrder(i);
        }
    }

    #endregion

    #region 입력 / 타겟 선택

    // 엔티티 누름 — 세팅 단계면 이동 시작, 전투 단계면 공격자 선택 (Entity.OnMouseDown이 호출)
    public void EntityMouseDown(Entity entity)
    {
        if (TurnManager.Inst.phase == TurnManager.EGamePhase.Setup)
        {
            if (!TurnManager.Inst.isLoading)
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
            CardManager.Inst.HideDamagePreview();
            return;
        }

        if (TurnManager.Inst.phase == TurnManager.EGamePhase.Setup)
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
            if (CanMeleeTarget(_selectEntity, _targetPickEntity))
            {
                CombatSystem.Inst.Attack(_selectEntity, _targetPickEntity);
            }
            else
            {
                CardManager.Inst.ShowWarning("전방에 도발(방패) 타입이 있으면 먼저 처치해야 다른 적을 공격할 수 있습니다.\n(원거리 타입은 도발을 무시하고 모든 적을 공격할 수 있습니다.)");
            }
        }

        _selectEntity     = null;
        _targetPickEntity = null;
        CardManager.Inst.HideDamagePreview(); // 공격 실행/취소 시 딜 예측 숨김
    }

    // 드래그 — 세팅 단계면 집어든 카드를 마우스로 이동, 전투 단계면 상대 앞줄을 타겟 지정 (Entity.OnMouseDrag이 호출)
    public void EntityMouseDrag()
    {
        if (UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
        {
            _targetPickEntity = null;
            CardManager.Inst.HideDamagePreview();
            
            if (TurnManager.Inst.phase == TurnManager.EGamePhase.Setup && _moveEntity != null)
            {
                _moveEntity.MoveTransform(Utils.MousePos, false);
            }
            return;
        }

        if (TurnManager.Inst.phase == TurnManager.EGamePhase.Setup)
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
            CardManager.Inst.ShowDamagePreview(_selectEntity, _targetPickEntity);
        }
        else
        {
            _targetPickEntity = null;
            CardManager.Inst.HideDamagePreview();
        }
    }

    // 세팅 단계 — 내 카드를 행에서 떼어내 이동을 시작한다
    private void BeginSetupMove(Entity entity)
    {
        _moveEntity    = entity;
        _moveFromFront = _myFront.Contains(entity);
        _moveFromIndex = GetRow(true, _moveFromFront).IndexOf(entity);

        GetRow(true, _moveFromFront).Remove(entity);
        EntityAlignment(true, _moveFromFront);
        entity.GetComponent<Order>()?.SetMostFrontOrder(true); // 드래그 동안 위로
    }

    // 세팅 단계 — 드롭한 행에 카드를 안착시킨다. 행이 가득 차 있으면 가장 가까운 카드와 자리를 맞바꾼다
    private void EndSetupMove()
    {
        if (_moveEntity == null)
        {
            return;
        }

        bool tFront = BoardLayout.IsMyFrontRow(Utils.MousePos.y);
        var  tRow   = GetRow(true, tFront);

        if (RealCount(tRow) < MaxRow)
        {
            InsertEntitySorted(tRow, _moveEntity, tFront); // 빈 자리에 삽입(같은 행 재배치 포함)
        }
        else
        {
            SwapIntoFullRow(_moveEntity, tFront);          // 가득 찬 행이면 스왑
        }

        EntityAlignment(true, true);
        EntityAlignment(true, false);
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
        var tRow = GetRow(true, tFront);

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

        var origRow = GetRow(true, _moveFromFront);
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

    #endregion

    #region 턴

    // 턴 시작 효과를 순차로 발동한다 — 공격권 갱신(즉시) → 후방 효과(엔티티별) → 전방 힐러(여러 틱).
    // 각 발동 사이 텀을 둬 효과가 겹쳐 보이지 않게 하고, 완료까지 TurnManager가 yield로 대기한다 (TurnManager.StartTurnCo가 호출)
    public IEnumerator ProcessTurnStartEffectsCo(bool isMine)
    {
        RefreshTurnStart(isMine);

        yield return ProcessBackRowCo(isMine);
        yield return ProcessFrontHealersCo(isMine);
    }

    // 후방 대기 카드의 타입별 효과를 한 장씩 발동한다 (자기 턴 시작)
    private IEnumerator ProcessBackRowCo(bool isMine)
    {
        var back = isMine ? _myBack : _otherBack;

        // 발동 도중 리스트가 바뀔 수 있으므로(원거리 견제로 적 사망 정리 등) 사본을 순회한다
        foreach (Entity card in new List<Entity>(back))
        {
            if (card.isEmpty)
            {
                continue;
            }

            if (card.CardType == ECardType.Healer) // 힐러는 틱마다 텀을 둬 +N 팝업이 겹치지 않게 한다
            {
                yield return HealTicksCo(isMine, card, BackHealerTicks);
            }
            else if (TriggerBackEffect(isMine, card))
            {
                yield return _backEffectDelay;
            }
        }
    }

    // 후방 카드 1장의 타입별 효과를 발동한다. 실제로 발동했으면 true (텀을 둘지 판단용)
    private bool TriggerBackEffect(bool isMine, Entity card)
    {
        switch (card.CardType)
        {
            case ECardType.Ranged: // 약한 견제 — 적 전방 무작위 1체에 공격력의 1/3(버림, 최소 1)만큼 화살 피해
                Entity target = GetRandomEnemyFront(isMine);
                if (target == null)
                {
                    return false;
                }

                int damage = Mathf.Max(1, card.health / 3);
                CombatSystem.Inst.FirePokeArrow(card.transform.position, target, damage); // 후방 카드 자기 위치에서 발사

                return true;

            case ECardType.Musou: // 충전 — 50% 확률로 자신 HP +1 영구 버프(승격 시 강해짐)
                if (Random.value >= MusouChargeChance)
                {
                    return false;
                }
                card.BuffHp(1, 0);

                return true;

            // Healer — 다중 틱이라 텀이 필요해 ProcessBackRowCo에서 HealTicksCo로 따로 처리한다

            default: // Normal — 후방 효과 없음
                return false;
        }
    }

    // 전방 힐러마다 회복 가능한 다른 전방 아군을 1씩 여러 틱(FrontHealerTicks) 회복한다. 한 틱씩 보이도록 텀을 둔다
    private IEnumerator ProcessFrontHealersCo(bool isMine)
    {
        var front = isMine ? _myFront : _otherFront;

        foreach (Entity healer in new List<Entity>(front))
        {
            if (healer.isEmpty || healer.isWaiting || healer.CardType != ECardType.Healer)
            {
                continue;
            }

            yield return HealTicksCo(isMine, healer, FrontHealerTicks);
        }
    }

    // 힐러가 회복 가능한 다른 전방 아군을 1씩 ticks회 회복한다. 한 틱마다 텀을 둬 +N 팝업이 겹치지 않게 한다 (전·후방 공통)
    private IEnumerator HealTicksCo(bool isMine, Entity healer, int ticks)
    {
        for (int tick = 0; tick < ticks; tick++)
        {
            if (!TryHealFront(isMine, healer)) // 회복할 대상이 없으면 조기 종료
            {
                yield break;
            }

            yield return _backEffectDelay;
        }
    }

    // 자신 제외, 회복 가능한 전방 아군 무작위 1명을 1 회복한다. 회복했으면 true (전방·후방 힐러 공통)
    private bool TryHealFront(bool isMine, Entity healer)
    {
        var front      = isMine ? _myFront : _otherFront;
        var candidates = front.FindAll(target => target != healer && target.CanHeal);
        if (candidates.Count == 0)
        {
            return false;
        }

        candidates[Random.Range(0, candidates.Count)].Heal(HealerHealAmount);

        return true;
    }

    // 이번 턴 진영의 앞줄·뒷줄 엔티티에 자기 턴 시작 처리를 순서대로 적용한다 (EnemyAI보다 먼저)
    private void RefreshTurnStart(bool myTurn)
    {
        var frontList = myTurn ? _myFront : _otherFront;
        var backList  = myTurn ? _myBack  : _otherBack;

        foreach (var entity in frontList)
        {
            entity.OnMyTurnStart();
        }

        foreach (var entity in backList)
        {
            entity.OnMyTurnStart(); // 대기 카드는 내부에서 무시된다
        }
    }

    // 해당 진영의 앞줄·뒷줄 엔티티에 한시 버프 만료를 적용한다 (TurnManager.EndTurn이 호출)
    public void TickBuffs(bool isMine)
    {
        foreach (var entity in (isMine ? _myFront : _otherFront))
        {
            entity.TickBuffs();
        }

        foreach (var entity in (isMine ? _myBack : _otherBack))
        {
            entity.TickBuffs();
        }
    }

    #endregion

    #region 헬퍼

    // 진영의 앞줄(공개) 리스트를 반환한다 (SkillSystem이 전방 전체 효과에 호출)
    public IReadOnlyList<Entity> GetFront(bool isMine)
    {
        return isMine ? _myFront : _otherFront;
    }

    // 해당 진영 앞줄에서 무작위 1체를 반환한다 (빈 슬롯·사망 제외, 없으면 null)
    public Entity GetRandomFront(bool isMine)
    {
        var front = (isMine ? _myFront : _otherFront).FindAll(e => !e.isEmpty && !e.isDie);

        return front.Count == 0 ? null : front[Random.Range(0, front.Count)];
    }

    // 시전자 기준 적 진영 앞줄에서 무작위 1체를 반환한다 (무작위 적 피해 스킬이 호출)
    public Entity GetRandomEnemyFront(bool isMine)
    {
        return GetRandomFront(!isMine);
    }

    // 대상과 같은 행에서 좌/우 인접 1체를 무작위로 반환한다 (무쌍 추가 피해가 호출, 없으면 null)
    public Entity GetRandomAdjacentFront(Entity target)
    {
        var row = target.isMine ? _myFront : _otherFront;
        int index = row.IndexOf(target);
        if (index < 0)
        {
            return null;
        }

        var neighbors = new List<Entity>();
        if (index - 1 >= 0)
        {
            neighbors.Add(row[index - 1]);
        }
        if (index + 1 < row.Count)
        {
            neighbors.Add(row[index + 1]);
        }

        return neighbors.Count == 0 ? null : neighbors[Random.Range(0, neighbors.Count)];
    }

    // 근접 공격자가 이 타겟을 칠 수 있는지 — 적 전방에 방패형(생존)이 있으면 방패형만 허용. 원거리는 제한 없음
    // (EntityManager 공격 확정·EnemyAI 타겟 선택이 호출)
    public bool CanMeleeTarget(Entity attacker, Entity target)
    {
        if (attacker.CardType == ECardType.Ranged)
        {
            return true;
        }

        var  enemyFront = target.isMine ? _myFront : _otherFront;
        bool hasShield  = enemyFront.Exists(e => !e.isEmpty && !e.isDie && e.CardType == ECardType.Shield);

        return !hasShield || target.CardType == ECardType.Shield;
    }

    // 진영·행에 해당하는 리스트를 반환한다
    private List<Entity> GetRow(bool isMine, bool isFront)
    {
        return isMine ? (isFront ? _myFront : _myBack)
                      : (isFront ? _otherFront : _otherBack);
    }

    // 리스트에 미리보기 빈 슬롯이 포함되어 있는지
    private bool ExistEmptyIn(List<Entity> list)
    {
        return list.Contains(_myEmptyEntity);
    }

    // 빈 슬롯을 제외한 실제 엔티티 수
    private int RealCount(List<Entity> list)
    {
        return list.Count - (ExistEmptyIn(list) ? 1 : 0);
    }

    #endregion
}
