using System.Collections.Generic;
using UnityEngine;

// 세팅 단계의 카드 배치를 담당한다 — 엔티티 스폰, 드래그 중 빈 슬롯 미리보기, 배치 완료 판정.
// 보드 상태(진영 행·정렬)는 EntityManager가 소유하고, 이 클래스는 그 위에 "배치 행위"만 얹는다.
public class BoardPlacement : MonoService<IBoardPlacement>, IBoardPlacement
{
    [CenterHeader("< 프리팹 >")]
    [SerializeField] private GameObject _entityPrefab;

    [CenterHeader("< 빈 슬롯(배치 미리보기) >")]
    [SerializeField] private Entity _myEmptyEntity;

    // 내가 배치한 실제 카드 수(빈 슬롯 제외) / 6장 모두 배치 완료 여부
    public int  MyPlacedCount    => RealCount(Row(true, true)) + RealCount(Row(true, false));
    public bool IsMyPlaceDone    => RealCount(Row(true, true)) >= EntityManager.MaxRow && RealCount(Row(true, false)) >= EntityManager.MaxRow;
    public bool IsOtherPlaceDone => Row(false, true).Count     >= EntityManager.MaxRow && Row(false, false).Count     >= EntityManager.MaxRow;


    // 빈 슬롯 미리보기 숨김 (Unity 메시지)
    private void Start()
    {
        _myEmptyEntity.gameObject.SetActive(false); // 드래그 중에만 보이게 한다
    }

    // 지정한 진영·행에 엔티티를 생성한다. 내 카드는 미리보기 빈 슬롯 자리에, 상대는 행 끝에 채운다 (CardManager.TryPutCard가 호출)
    public bool SpawnEntity(bool isMine, Item item, bool isFrontRow, Vector3 spawnPos)
    {
        var rowList = Row(isMine, isFrontRow);

        if (isMine)
        {
            if (!ExistEmptyIn(rowList) || RealCount(rowList) >= EntityManager.MaxRow)
            {
                return false;
            }
        }
        else if (rowList.Count >= EntityManager.MaxRow)
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

        Services.Get<IBoardState>().EntityAlignment(isMine, isFrontRow);

        return true;
    }

    // 치트 — 빈 슬롯 미리보기 없이 내 카드 1장을 앞줄 우선(차면 뒷줄)으로 배치한다 (CardManager.CheatAutoPlaceMyCards가 호출)
    public bool CheatPlaceMyCard(Item item)
    {
        bool isFrontRow = RealCount(Row(true, true)) < EntityManager.MaxRow;
        var  rowList    = Row(true, isFrontRow);

        if (RealCount(rowList) >= EntityManager.MaxRow)
        {
            return false; // 앞줄·뒷줄 모두 가득
        }

        Vector3 spawnPos = BoardLayout.GetSlotPosition(true, isFrontRow, rowList.Count, EntityManager.MaxRow);
        var     entity   = Instantiate(_entityPrefab, spawnPos, Utils.QI).GetComponent<Entity>();

        entity.isMine = true;
        entity.Setup(item, true, !isFrontRow); // 내 카드는 항상 앞면, 뒷줄이면 대기 상태

        rowList.Add(entity);
        Services.Get<IBoardState>().EntityAlignment(true, isFrontRow);

        return true;
    }

    // 드래그한 카드의 위치(x, y)에 맞는 행을 골라 임시 빈 슬롯을 끼워 미리보기를 제공한다 (내 배치 전용, CardManager.CardDrag가 호출)
    public void InsertMyEmptyEntity(float xPos, float yPos)
    {
        bool isFront      = BoardLayout.IsMyFrontRow(yPos);
        var  targetRow    = Row(true, isFront);
        var  oppositeRow  = Row(true, !isFront);

        // 반대 행에 빈 슬롯이 남아 있으면 회수하고 그 행을 재정렬한다
        if (ExistEmptyIn(oppositeRow))
        {
            oppositeRow.Remove(_myEmptyEntity);
            Services.Get<IBoardState>().EntityAlignment(true, !isFront);
        }

        // 대상 행이 가득 찼으면 미리보기를 띄우지 않는다
        if (RealCount(targetRow) >= EntityManager.MaxRow)
        {
            if (ExistEmptyIn(targetRow))
            {
                targetRow.Remove(_myEmptyEntity);
                Services.Get<IBoardState>().EntityAlignment(true, isFront);
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
            Services.Get<IBoardState>().EntityAlignment(true, isFront);
        }
    }

    // 임시 빈 슬롯을 제거하고 재정렬한다 (배치 취소 시, CardManager가 호출)
    public void RemoveMyEmptyEntity()
    {
        var front = Row(true, true);
        var back  = Row(true, false);

        if (ExistEmptyIn(front))
        {
            front.Remove(_myEmptyEntity);
            Services.Get<IBoardState>().EntityAlignment(true, true);
        }

        if (ExistEmptyIn(back))
        {
            back.Remove(_myEmptyEntity);
            Services.Get<IBoardState>().EntityAlignment(true, false);
        }

        _myEmptyEntity.gameObject.SetActive(false);
    }

    // 진영·행의 실제 엔티티 수(빈 슬롯 제외) — 입력 컨트롤러가 세팅 이동 시 배치 한도 판정에 사용
    public int RowRealCount(bool isMine, bool isFront)
    {
        return RealCount(Row(isMine, isFront));
    }

    // 보드 상태가 소유한 진영·행 리스트를 가져온다
    private List<Entity> Row(bool isMine, bool isFront)
    {
        return Services.Get<IBoardState>().GetRow(isMine, isFront);
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
}
