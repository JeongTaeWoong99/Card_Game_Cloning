using System;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using Random = UnityEngine.Random;

// 손패(아직 전장에 놓이지 않은 카드)의 상태·입력·정렬을 담당한다.
// Setup 페이즈: 6장을 드래그해 앞줄/뒷줄에 배치한다(마우스 y로 행 판정).
// Battle 페이즈: 스킬 카드 드로우(현재는 디버그)만 담당한다.
public class CardManager : MonoBehaviour
{
    public static CardManager Inst { get; private set; }

    private enum ECardState { Nothing, CanMouseOver, CanMouseDrag }

    [CenterHeader("< 참조 >")]
    [SerializeField] private ItemSO     _itemSO;
    [SerializeField] private GameObject _cardPrefab;

    [CenterHeader("< 손패 >")]
    [SerializeField] private List<Card> _myCards;
    [SerializeField] private List<Card> _otherCards;

    [CenterHeader("< 스폰 / 정렬 기준점 >")]
    [SerializeField] private Transform _cardSpawnPoint;
    [SerializeField] private Transform _otherCardSpawnPoint;
    [SerializeField] private Transform _myCardLeft;
    [SerializeField] private Transform _myCardRight;
    [SerializeField] private Transform _otherCardLeft;
    [SerializeField] private Transform _otherCardRight;

    [CenterHeader("< 상태 >")]
    [SerializeField] private ECardState _cardState;

    private Deck _deck;
    private Card _selectCard;
    private bool _isMyCardDrag;
    private bool _onMyCardArea;


    // 싱글톤 등록 (Unity 메시지)
    private void Awake()
    {
        Inst = this;
    }

    // 덱 생성 + 카드 분배 이벤트 구독 (Unity 메시지)
    private void Start()
    {
        _deck = new Deck(_itemSO.items);

        TurnManager.OnAddCard += AddCard;
    }

    // 이벤트 구독 해제 (Unity 메시지)
    private void OnDestroy()
    {
        TurnManager.OnAddCard -= AddCard;
    }

    // 드래그 처리 + 손패 영역 감지 + 입력 상태 갱신을 매 프레임 수행 (Unity 메시지)
    private void Update()
    {
        if (_isMyCardDrag)
        {
            CardDrag();
        }

        DetectCardArea();
        SetECardState();
    }

    #region 입력 (마우스 / 드래그)

    // 카드에 마우스 진입 — 선택 후 확대 (Card.OnMouseOver가 호출)
    public void CardMouseOver(Card card)
    {
        if (_cardState == ECardState.Nothing)
        {
            return;
        }

        _selectCard = card;
        EnlargeCard(true, card);
    }

    // 카드에서 마우스 이탈 — 원위치 (Card.OnMouseExit가 호출)
    public void CardMouseExit(Card card)
    {
        EnlargeCard(false, card);
    }

    // 카드 누름 — 드래그 시작 (Card.OnMouseDown이 호출)
    public void CardMouseDown()
    {
        if (_cardState != ECardState.CanMouseDrag)
        {
            return;
        }

        _isMyCardDrag = true;
    }

    // 카드 놓음 — 손패 영역이면 배치 취소, 밖이면 마우스 y로 행을 골라 배치 시도 (Card.OnMouseUp이 호출)
    public void CardMouseUp()
    {
        _isMyCardDrag = false;

        if (_cardState != ECardState.CanMouseDrag)
        {
            return;
        }

        if (_onMyCardArea)
        {
            EntityManager.Inst.RemoveMyEmptyEntity();
        }
        else
        {
            bool isFrontRow = BoardLayout.IsMyFrontRow(Utils.MousePos.y);
            TryPutCard(true, isFrontRow);
        }
    }

    // 드래그 중 카드를 마우스로 이동시키고, 마우스 위치에 맞는 행에 빈 슬롯 미리보기를 띄운다
    private void CardDrag()
    {
        if (_cardState != ECardState.CanMouseDrag)
        {
            return;
        }

        if (!_onMyCardArea)
        {
            _selectCard.MoveTransform(new PRS(Utils.MousePos, Utils.QI, _selectCard.originPRS.scale), false);
            EntityManager.Inst.InsertMyEmptyEntity(Utils.MousePos.x, Utils.MousePos.y);
        }
    }

    // 카드 확대(마우스 오버) 또는 원위치 + 최상단 표시 토글
    private void EnlargeCard(bool isEnlarge, Card card)
    {
        if (isEnlarge)
        {
            Vector3 enlargePos = new Vector3(card.originPRS.pos.x, card.originPRS.pos.y + 12.5f, -10f);
            card.MoveTransform(new PRS(enlargePos, Utils.QI, Vector3.one * 3.5f), false);
        }
        else
        {
            card.MoveTransform(card.originPRS, false);
        }

        card.GetComponent<Order>().SetMostFrontOrder(isEnlarge);
    }

    // 마우스가 손패 영역(MyCardArea 레이어) 위에 있는지 검사한다
    private void DetectCardArea()
    {
        RaycastHit2D[] hits  = Physics2D.RaycastAll(Utils.MousePos, Vector3.forward);
        int            layer = LayerMask.NameToLayer("MyCardArea");
        _onMyCardArea = Array.Exists(hits, x => x.collider.gameObject.layer == layer);
    }

    // 로딩 중엔 입력 차단, Setup 페이즈에 손패가 남아 있으면 드래그 배치 허용, 그 외엔 확대만
    private void SetECardState()
    {
        if (TurnManager.Inst.isLoading)
        {
            _cardState = ECardState.Nothing;
        }
        else if (TurnManager.Inst.phase == TurnManager.EGamePhase.Setup && _myCards.Count > 0)
        {
            _cardState = ECardState.CanMouseDrag;
        }
        else
        {
            _cardState = ECardState.CanMouseOver;
        }
    }

    #endregion

    #region 손패 / 배치

    // 선택한 카드(아군) 또는 무작위 카드(상대)를 지정 행에 배치 시도한다. 성공 시 손패에서 제거
    // (CardMouseUp · EnemyAI가 호출)
    public bool TryPutCard(bool isMine, bool isFrontRow)
    {
        if (!isMine && _otherCards.Count <= 0)
        {
            return false;
        }

        Card card = isMine ? _selectCard : _otherCards[Random.Range(0, _otherCards.Count)];
        if (card == null)
        {
            return false;
        }

        var spawnPos    = isMine ? Utils.MousePos : _otherCardSpawnPoint.position;
        var targetCards = isMine ? _myCards       : _otherCards;

        if (EntityManager.Inst.SpawnEntity(isMine, card.item, isFrontRow, spawnPos))
        {
            targetCards.Remove(card);
            card.transform.DOKill();

            Destroy(card.gameObject);

            if (isMine)
            {
                _selectCard = null;
            }

            CardAlignment(isMine);

            return true;
        }

        // 배치 실패 시 끌어올렸던 order를 되돌리고 정렬 복구한다
        targetCards.ForEach(x => x.GetComponent<Order>().SetMostFrontOrder(false));
        CardAlignment(isMine);

        return false;
    }

    // 카드 한 장을 뽑아 생성·세팅 후 손패에 추가하고 재정렬한다 (시작 6장 분배 시 OnAddCard 구독)
    private void AddCard(bool isMine)
    {
        var spawnPoint = isMine ? _cardSpawnPoint : _otherCardSpawnPoint;
        var cardObject = Instantiate(_cardPrefab, spawnPoint.position, Utils.QI);
        var card       = cardObject.GetComponent<Card>();
        card.Setup(_deck.Pop(), isMine); // 내 카드는 앞면, 상대 카드는 뒷면
        (isMine ? _myCards : _otherCards).Add(card);

        SetOriginOrder(isMine);
        CardAlignment(isMine);
    }

    // 스킬 카드 드로우 시도 — 스킬 카드 미제작이라 현재는 디버그만 출력한다 (TurnManager가 호출)
    public void DrawSkillCards(bool isMine, int count)
    {
        string who = isMine ? "나" : "상대";

        for (int i = 0; i < count; i++)
        {
            Debug.Log($"[{who}] 스킬 카드 드로우 시도 — 남은 스킬 카드가 없습니다.");
        }
    }

    // 손패를 인덱스 순서대로 정렬 order를 부여한다 (왼→오 겹침 순서)
    private void SetOriginOrder(bool isMine)
    {
        int count = isMine ? _myCards.Count : _otherCards.Count;
        for (int i = 0; i < count; i++)
        {
            var targetCard = isMine ? _myCards[i] : _otherCards[i];
            targetCard?.GetComponent<Order>().SetOriginOrder(i);
        }
    }

    // 손패를 부채꼴(CardLayout)로 배치한다. 아군은 위로 볼록(+height), 상대는 아래로(-height)
    private void CardAlignment(bool isMine)
    {
        List<PRS> originCardPRSs;
        if (isMine)
        {
            originCardPRSs = CardLayout.GetHandPRS(_myCardLeft, _myCardRight, _myCards.Count, 0.5f, Vector3.one * 1.9f);
        }
        else
        {
            originCardPRSs = CardLayout.GetHandPRS(_otherCardLeft, _otherCardRight, _otherCards.Count, -0.5f, Vector3.one * 1.9f);
        }

        var targetCards = isMine ? _myCards : _otherCards;
        for (int i = 0; i < targetCards.Count; i++)
        {
            var targetCard = targetCards[i];
            targetCard.originPRS = originCardPRSs[i];
            targetCard.MoveTransform(targetCard.originPRS, true, 0.7f);
        }
    }

    #endregion
}
