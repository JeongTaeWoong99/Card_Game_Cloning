using System;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using Random = UnityEngine.Random;

// 손패(아직 전장에 놓이지 않은 카드)의 상태·입력·정렬을 담당한다.
// Setup 페이즈: 엔티티 카드 6장을 드래그해 앞줄/뒷줄에 배치한다(마우스 y로 행 판정).
// Battle 페이즈: 스킬 카드를 드로우·발동(마나 소모)하고, 대상 O 스킬의 대상 선택을 처리한다.
public class CardManager : MonoBehaviour
{
    public static CardManager Inst { get; private set; }

    private enum ECardState { Nothing, CanMouseOver, CanMouseDrag }

    private const float DragSkillCardScale = 1.2f; // 드래그 중 스킬 카드 축소 스케일 (손패 1.9보다 작게)

    [CenterHeader("< 참조 >")]
    [SerializeField] private ItemSO     _itemSO;
    [SerializeField] private SkillSO    _skillSO;
    [SerializeField] private GameObject _cardPrefab;
    [SerializeField] private GameObject _skillCardPrefab;

    [CenterHeader("< 손패 >")]
    [SerializeField] private List<Card>      _myCards;
    [SerializeField] private List<Card>      _otherCards;
    [SerializeField] private List<SkillCard> _mySkillCards;
    [SerializeField] private List<SkillCard> _otherSkillCards;

    [CenterHeader("< 스폰 / 정렬 기준점 >")]
    [SerializeField] private Transform _cardSpawnPoint;
    [SerializeField] private Transform _otherCardSpawnPoint;
    [SerializeField] private Transform _myCardLeft;
    [SerializeField] private Transform _myCardRight;
    [SerializeField] private Transform _otherCardLeft;
    [SerializeField] private Transform _otherCardRight;

    [CenterHeader("< 상태 >")]
    [SerializeField] private ECardState _cardState;

    private Deck<Item>  _deck;
    private Deck<Skill> _skillDeck;
    private Card        _selectCard;
    private bool        _isMyCardDrag;
    private bool        _onMyCardArea;

    private SkillCard _selectSkillCard;   // 드래그 중인 스킬 카드
    private bool      _isMySkillCardDrag; // 내 스킬 카드 드래그 중

    // 내 전투 턴 + 로딩 중이 아닐 때 스킬 발동 가능
    private bool CanPlaySkill => TurnManager.Inst.IsBattlePhase && TurnManager.Inst.myTurn
                                 && !TurnManager.Inst.isLoading;


    // 싱글톤 등록 (Unity 메시지)
    private void Awake()
    {
        Inst = this;
    }

    // 덱 생성 + 카드 분배 이벤트 구독 (Unity 메시지)
    private void Start()
    {
        _deck      = new Deck<Item>(_itemSO.items);
        _skillDeck = new Deck<Skill>(_skillSO.skills);

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

        if (_isMySkillCardDrag)
        {
            SkillCardDrag();
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

    // 스킬 덱에서 count장을 뽑아 손패에 추가한다. 내 카드는 앞면, 상대 카드는 뒷면 (TurnManager가 호출)
    public void DrawSkillCards(bool isMine, int count)
    {
        var  spawnPoint = isMine ? _cardSpawnPoint : _otherCardSpawnPoint;
        var  targetList = isMine ? _mySkillCards   : _otherSkillCards;

        for (int i = 0; i < count; i++)
        {
            var cardObject = Instantiate(_skillCardPrefab, spawnPoint.position, Utils.QI);
            var skillCard  = cardObject.GetComponent<SkillCard>();
            skillCard.Setup(_skillDeck.Pop(), isMine);
            targetList.Add(skillCard);
        }

        SetSkillOriginOrder(isMine);
        SkillCardAlignment(isMine);
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

    #region 스킬 (전투)

    // 스킬 카드에 마우스 진입 — 드래그 중이 아니면 확대 (SkillCard.OnMouseOver가 호출)
    public void SkillCardMouseOver(SkillCard card)
    {
        if (!CanPlaySkill || _isMySkillCardDrag)
        {
            return;
        }

        EnlargeSkillCard(true, card);
    }

    // 스킬 카드에서 마우스 이탈 — 드래그 중이 아니면 원위치 (SkillCard.OnMouseExit가 호출)
    public void SkillCardMouseExit(SkillCard card)
    {
        if (_isMySkillCardDrag)
        {
            return;
        }

        EnlargeSkillCard(false, card);
    }

    // 스킬 카드 누름 — 마나가 충분하면 드래그를 시작한다 (SkillCard.OnMouseDown이 호출)
    public void SkillCardMouseDown(SkillCard card)
    {
        if (!CanPlaySkill)
        {
            return;
        }

        if (!ManaManager.Inst.CanAfford(true, card.skill.manaCost))
        {
            GameManager.Inst.Notification("마나가 부족합니다");

            return;
        }

        _selectSkillCard   = card;
        _isMySkillCardDrag = true;

        EnlargeSkillCard(false, card);                      // 호버 확대 해제
        card.GetComponent<Order>().SetMostFrontOrder(true); // 드래그 동안 위로
    }

    // 드래그 중 — 손패 영역 밖이면 논타겟은 축소 이동, 대상 O는 카드를 숨기고 내 앞줄을 타겟팅 (Update가 호출)
    private void SkillCardDrag()
    {
        bool isTargeting = _selectSkillCard.skill.targeting != ESkillTargeting.None;

        // 손패 영역 안으로 되돌아오면 원래 손패 모습으로 복귀(놓으면 취소)
        if (_onMyCardArea)
        {
            _selectSkillCard.SetVisible(true);
            _selectSkillCard.MoveTransform(_selectSkillCard.originPRS, false);

            if (isTargeting)
            {
                EntityManager.Inst.ConsumeSkillTarget(); // 타겟 피커 해제
            }

            return;
        }

        // 대상 O: 유효 타겟 위에서만 카드를 숨기고 타겟팅 UI로 안내. 그 전까지는 논타겟처럼 축소 상태로 따라온다
        bool hideForTarget = isTargeting && EntityManager.Inst.PickSkillTarget(Utils.MousePos);

        _selectSkillCard.SetVisible(!hideForTarget);

        if (!hideForTarget)
        {
            // 논타겟이거나, 아직 유효 타겟이 없으면 카드를 마우스로 축소 이동
            _selectSkillCard.MoveTransform(new PRS(Utils.MousePos, Utils.QI, Vector3.one * DragSkillCardScale), false);
        }
    }

    // 스킬 카드 놓음 — 손패 영역 밖이면 발동 시도, 안이면 취소 (SkillCard.OnMouseUp이 호출)
    public void SkillCardMouseUp(SkillCard card)
    {
        if (!_isMySkillCardDrag)
        {
            return;
        }

        _isMySkillCardDrag = false;
        card.GetComponent<Order>().SetMostFrontOrder(false);

        Skill skill = card.skill;

        // 손패 영역 안에서 놓으면 발동하지 않고 취소
        if (_onMyCardArea)
        {
            CancelSkillDrag();
        }
        else if (skill.targeting == ESkillTargeting.None)
        {
            // 논타겟: 영역 밖에서 놓으면 즉시 발동 (마나 재확인)
            if (ManaManager.Inst.TrySpend(true, skill.manaCost))
            {
                SkillSystem.Inst.Cast(skill, true);
                DiscardSkillCard(true, card);
            }
            else
            {
                CancelSkillDrag();
            }
        }
        else
        {
            // 대상 O: 유효한 내 앞줄 엔티티 위에서 놓으면 발동
            Entity target = EntityManager.Inst.ConsumeSkillTarget();
            if (target != null && ManaManager.Inst.TrySpend(true, skill.manaCost))
            {
                SkillSystem.Inst.Cast(skill, true, target);
                DiscardSkillCard(true, card);
            }
            else
            {
                CancelSkillDrag();
            }
        }

        _selectSkillCard = null;
    }

    // 드래그 취소 — 숨김을 해제하고 손패 정렬 위치로 되돌린다
    private void CancelSkillDrag()
    {
        _selectSkillCard.SetVisible(true);
        SkillCardAlignment(true);
    }

    // 상대가 마나가 되는 보유 스킬 중 무작위 1개를 시전한다. 시전했으면 true (EnemyAI가 호출)
    public bool TryCastOtherSkill()
    {
        // 마나 충분 + (대상 O면 시전할 아군 전방이 존재)하는 카드만 후보
        var castable = _otherSkillCards.FindAll(card =>
            ManaManager.Inst.CanAfford(false, card.skill.manaCost) &&
            (card.skill.targeting == ESkillTargeting.None || EntityManager.Inst.GetRandomFront(false) != null));

        if (castable.Count == 0)
        {
            return false;
        }

        var   card  = castable[Random.Range(0, castable.Count)];
        Skill skill = card.skill;

        Entity target = skill.targeting == ESkillTargeting.MyEntity
            ? EntityManager.Inst.GetRandomFront(false) // 상대 입장의 아군 전방
            : null;

        ManaManager.Inst.TrySpend(false, skill.manaCost);
        SkillSystem.Inst.Cast(skill, false, target);
        DiscardSkillCard(false, card);

        return true;
    }

    // 스킬 카드를 손패에서 제거·파괴하고 재정렬한다
    private void DiscardSkillCard(bool isMine, SkillCard card)
    {
        (isMine ? _mySkillCards : _otherSkillCards).Remove(card);
        card.transform.DOKill();
        Destroy(card.gameObject);

        SkillCardAlignment(isMine);
    }

    // 스킬 카드 확대(마우스 오버) 또는 원위치 + 최상단 표시 토글
    private void EnlargeSkillCard(bool isEnlarge, SkillCard card)
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

    // 스킬 손패를 인덱스 순서대로 정렬 order를 부여한다
    private void SetSkillOriginOrder(bool isMine)
    {
        var targetCards = isMine ? _mySkillCards : _otherSkillCards;
        for (int i = 0; i < targetCards.Count; i++)
        {
            targetCards[i]?.GetComponent<Order>().SetOriginOrder(i);
        }
    }

    // 스킬 손패를 부채꼴(CardLayout)로 배치한다. 엔티티 손패와 동일 기준점을 재사용
    private void SkillCardAlignment(bool isMine)
    {
        List<PRS> originCardPRSs;
        if (isMine)
        {
            originCardPRSs = CardLayout.GetHandPRS(_myCardLeft, _myCardRight, _mySkillCards.Count, 0.5f, Vector3.one * 1.9f);
        }
        else
        {
            originCardPRSs = CardLayout.GetHandPRS(_otherCardLeft, _otherCardRight, _otherSkillCards.Count, -0.5f, Vector3.one * 1.9f);
        }

        var targetCards = isMine ? _mySkillCards : _otherSkillCards;
        for (int i = 0; i < targetCards.Count; i++)
        {
            var targetCard = targetCards[i];
            targetCard.originPRS = originCardPRSs[i];
            targetCard.MoveTransform(targetCard.originPRS, true, 0.7f);
        }
    }

    #endregion
}
