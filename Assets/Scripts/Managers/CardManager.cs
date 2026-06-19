using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using DG.Tweening;
using Random = UnityEngine.Random;

public class CardManager : MonoBehaviour
{
    public static CardManager Inst { get; private set; }

    private enum ECardState { Nothing, CanMouseOver, CanMouseDrag }

    [SerializeField] private ItemSO     _itemSO;
    [SerializeField] private GameObject _cardPrefab;
    [SerializeField] private List<Card> _myCards;
    [SerializeField] private List<Card> _otherCards;
    [SerializeField] private Transform  _cardSpawnPoint;
    [SerializeField] private Transform  _otherCardSpawnPoint;
    [SerializeField] private Transform  _myCardLeft;
    [SerializeField] private Transform  _myCardRight;
    [SerializeField] private Transform  _otherCardLeft;
    [SerializeField] private Transform  _otherCardRight;
    [SerializeField] private ECardState _cardState;

    private List<Item> _itemBuffer;
    private Card       _selectCard;
    private bool       _isMyCardDrag;
    private bool       _onMyCardArea;
    private int        _myPutCount;


    private void Awake()
    {
        Inst = this;
    }

    private void Start()
    {
        SetupItemBuffer();
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

    public Item PopItem()
    {
        if (_itemBuffer.Count == 0)
        {
            SetupItemBuffer();
        }

        Item item = _itemBuffer[0];
        _itemBuffer.RemoveAt(0);
        return item;
    }

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

    // percent 비율로 아이템을 채운 뒤 셔플하여 뽑기 버퍼를 만든다
    private void SetupItemBuffer()
    {
        _itemBuffer = new List<Item>(100);
        for (int i = 0; i < _itemSO.items.Length; i++)
        {
            Item item = _itemSO.items[i];
            for (int j = 0; j < item.percent; j++)
            {
                _itemBuffer.Add(item);
            }
        }

        Utils.Shuffle(_itemBuffer);
    }

    private void AddCard(bool isMine)
    {
        var cardObject = Instantiate(_cardPrefab, _cardSpawnPoint.position, Utils.QI);
        var card = cardObject.GetComponent<Card>();
        card.Setup(PopItem(), isMine);
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

    private void CardAlignment(bool isMine)
    {
        List<PRS> originCardPRSs;
        if (isMine)
        {
            originCardPRSs = RoundAlignment(_myCardLeft, _myCardRight, _myCards.Count, 0.5f, Vector3.one * 1.9f);
        }
        else
        {
            originCardPRSs = RoundAlignment(_otherCardLeft, _otherCardRight, _otherCards.Count, -0.5f, Vector3.one * 1.9f);
        }

        var targetCards = isMine ? _myCards : _otherCards;
        for (int i = 0; i < targetCards.Count; i++)
        {
            var targetCard = targetCards[i];
            targetCard.originPRS = originCardPRSs[i];
            targetCard.MoveTransform(targetCard.originPRS, true, 0.7f);
        }
    }

    private List<PRS> RoundAlignment(Transform leftTr, Transform rightTr, int objCount, float height, Vector3 scale)
    {
        float[] objLerps = new float[objCount];
        List<PRS> results = new List<PRS>(objCount);

        switch (objCount)
        {
            case 1: objLerps = new float[] { 0.5f }; break;
            case 2: objLerps = new float[] { 0.27f, 0.73f }; break;
            case 3: objLerps = new float[] { 0.1f, 0.5f, 0.9f }; break;
            default:
                float interval = 1f / (objCount - 1);
                for (int i = 0; i < objCount; i++)
                {
                    objLerps[i] = interval * i;
                }
                break;
        }

        for (int i = 0; i < objCount; i++)
        {
            var targetPos = Vector3.Lerp(leftTr.position, rightTr.position, objLerps[i]);
            var targetRot = Utils.QI;
            if (objCount >= 4)
            {
                float curve = Mathf.Sqrt(Mathf.Pow(height, 2) - Mathf.Pow(objLerps[i] - 0.5f, 2));
                curve = height >= 0 ? curve : -curve;
                targetPos.y += curve;
                targetRot = Quaternion.Slerp(leftTr.rotation, rightTr.rotation, objLerps[i]);
            }
            results.Add(new PRS(targetPos, targetRot, scale));
        }
        return results;
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

    private void DetectCardArea()
    {
        RaycastHit2D[] hits = Physics2D.RaycastAll(Utils.MousePos, Vector3.forward);
        int layer = LayerMask.NameToLayer("MyCardArea");
        _onMyCardArea = Array.Exists(hits, x => x.collider.gameObject.layer == layer);
    }

    private void EnlargeCard(bool isEnlarge, Card card)
    {
        if (isEnlarge)
        {
            Vector3 enlargePos = new Vector3(card.originPRS.pos.x, -4.8f, -10f);
            card.MoveTransform(new PRS(enlargePos, Utils.QI, Vector3.one * 3.5f), false);
        }
        else
        {
            card.MoveTransform(card.originPRS, false);
        }

        card.GetComponent<Order>().SetMostFrontOrder(isEnlarge);
    }

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
