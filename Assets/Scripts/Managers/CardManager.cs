using System;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using Random = UnityEngine.Random;

// 손패(아직 전장에 놓이지 않은 카드)의 상태·입력·정렬을 담당한다.
// 뽑기 버퍼는 Deck, 부채꼴 배치 수학은 CardLayout, 전장 배치는 EntityManager에 위임한다.
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
    private int  _myPutCount;


    private void Awake()
    {
        Inst = this;
    }

    private void Start()
    {
        _deck = new Deck(_itemSO.items);

        TurnManager.OnAddCard     += AddCard;
        TurnManager.OnTurnStarted += OnTurnStarted;
    }

    private void OnDestroy()
    {
        TurnManager.OnAddCard     -= AddCard;
        TurnManager.OnTurnStarted -= OnTurnStarted;
    }

    private void Update()
    {
        if (_isMyCardDrag)
        {
            CardDrag();
        }

        DetectCardArea();
        SetECardState();
    }

    // 선택한 카드(아군) 또는 무작위 카드(상대)를 전장에 배치 시도한다. 성공 시 손패에서 제거
    public bool TryPutCard(bool isMine)
    {
        if (isMine && _myPutCount >= 1)
        {
            return false;
        }

        if (!isMine && _otherCards.Count <= 0)
        {
            return false;
        }

        Card card        = isMine ? _selectCard    : _otherCards[Random.Range(0, _otherCards.Count)];
        var  spawnPos    = isMine ? Utils.MousePos : _otherCardSpawnPoint.position;
        var  targetCards = isMine ? _myCards       : _otherCards;

        if (EntityManager.Inst.SpawnEntity(isMine, card.item, spawnPos))
        {
            targetCards.Remove(card);
            card.transform.DOKill();

            Destroy(card.gameObject);

            if (isMine)
            {
                _selectCard = null;
                _myPutCount++;
            }

            CardAlignment(isMine);

            return true;
        }

        // 배치 실패 시 끌어올렸던 order를 되돌리고 정렬 복구한다
        targetCards.ForEach(x => x.GetComponent<Order>().SetMostFrontOrder(false));
        CardAlignment(isMine);

        return false;
    }

    public void CardMouseOver(Card card)
    {
        if (_cardState == ECardState.Nothing)
        {
            return;
        }

        _selectCard = card;
        EnlargeCard(true, card);
    }

    public void CardMouseExit(Card card)
    {
        EnlargeCard(false, card);
    }

    public void CardMouseDown()
    {
        if (_cardState != ECardState.CanMouseDrag)
        {
            return;
        }

        _isMyCardDrag = true;
    }

    public void CardMouseUp()
    {
        _isMyCardDrag = false;

        if (_cardState != ECardState.CanMouseDrag)
        {
            return;
        }

        // 카드 영역 위에서 놓으면 배치 취소(빈 슬롯 제거), 밖에서 놓으면 배치 시도
        if (_onMyCardArea)
        {
            EntityManager.Inst.RemoveMyEmptyEntity();
        }
        else
        {
            TryPutCard(true);
        }
    }

    private void OnTurnStarted(bool myTurn)
    {
        if (myTurn)
        {
            _myPutCount = 0;
        }
    }

    private void AddCard(bool isMine)
    {
        var spawnPoint = isMine ? _cardSpawnPoint : _otherCardSpawnPoint;
        var cardObject = Instantiate(_cardPrefab, spawnPoint.position, Utils.QI);
        var card       = cardObject.GetComponent<Card>();
        card.Setup(_deck.Pop(), isMine);
        (isMine ? _myCards : _otherCards).Add(card);

        SetOriginOrder(isMine);
        CardAlignment(isMine);
    }

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

    private void CardDrag()
    {
        if (_cardState != ECardState.CanMouseDrag)
        {
            return;
        }

        if (!_onMyCardArea)
        {
            _selectCard.MoveTransform(new PRS(Utils.MousePos, Utils.QI, _selectCard.originPRS.scale), false);
            EntityManager.Inst.InsertMyEmptyEntity(Utils.MousePos.x);
        }
    }

    // 마우스가 손패 영역(MyCardArea 레이어) 위에 있는지 검사한다
    private void DetectCardArea()
    {
        RaycastHit2D[] hits  = Physics2D.RaycastAll(Utils.MousePos, Vector3.forward);
        int            layer = LayerMask.NameToLayer("MyCardArea");
        _onMyCardArea = Array.Exists(hits, x => x.collider.gameObject.layer == layer);
    }

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

    // 로딩 중엔 입력 차단, 내 턴이 아니거나 이미 배치했거나 전장이 가득 차면 확대만, 그 외엔 드래그 허용
    private void SetECardState()
    {
        if (TurnManager.Inst.isLoading)
        {
            _cardState = ECardState.Nothing;
        }
        else if (!TurnManager.Inst.myTurn || _myPutCount == 1 || EntityManager.Inst.IsFullMyEntities)
        {
            _cardState = ECardState.CanMouseOver;
        }
        else if (TurnManager.Inst.myTurn && _myPutCount == 0)
        {
            _cardState = ECardState.CanMouseDrag;
        }
    }
}
