using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using DG.Tweening;
using Random = UnityEngine.Random;

// 손패(아직 전장에 놓이지 않은 카드)의 상태·입력·정렬을 담당한다.
// Setup 페이즈: 엔티티 카드 6장을 드래그해 앞줄/뒷줄에 배치한다(마우스 y로 행 판정).
// Battle 페이즈: 스킬 카드를 드로우·발동(마나 소모)하고, 대상 O 스킬의 대상 선택을 처리한다.
public class CardManager : MonoBehaviour
{
    public static CardManager Inst { get; private set; }

    private enum ECardState { Nothing, CanMouseOver, CanMouseDrag }

    private const float DragSkillCardScale = 1.2f;  // 드래그 중 스킬 카드 축소 스케일 (손패 1.9보다 작게)
    private const float SkillEnterOffset   = 20f;   // 스킬 연출 카드 등장 시작 x 오프셋 (왼쪽 화면 밖)
    private const float SkillEnterTime     = 0.3f;  // 스킬 연출 카드 등장 이동 시간
    private const float SkillExitTime      = 0.2f;  // 스킬 연출 카드 퇴장 축소 시간
    private const float WarningHoldTime    = 0.5f;  // 도발 등 경고 유지 시간
    private const float WarningFadeTime    = 1f;    // 경고 페이드 아웃 시간

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

    [CenterHeader("< 필드 미리보기 / 스킬 연출 위치 >")]
    [SerializeField] private Transform _myPreviewPoint;    // 상대 엔티티 정보 표시 위치 (내 필드 쪽 = 아래)
    [SerializeField] private Transform _otherPreviewPoint; // 내 엔티티 정보 표시 위치 (상대 필드 쪽 = 위)
    [SerializeField] private Transform _skillCastPoint;    // 스킬 발동 연출 위치 (좌중앙)

    [CenterHeader("< 배치 안내 (세팅) >")]
    [SerializeField] private GameObject _frontHighlight; // 전방 배치 영역 강조(포커스)
    [SerializeField] private GameObject _backHighlight;  // 후방 배치 영역 강조(포커스)
    [SerializeField] private TMP_Text   _placeGuideTMP;  // 배치 안내 문구

    [CenterHeader("< 딜 예측 (전투) >")]
    [SerializeField] private GameObject _damagePreview;        // 딜 예측 패널(묶음)
    [SerializeField] private Transform  _previewMyCardPoint;   // 내 카드 미리보기 위치
    [SerializeField] private Transform  _previewEnemyCardPoint;// 상대 카드 미리보기 위치
    [SerializeField] private TMP_Text   _dealtTMP;             // 주는 피해
    [SerializeField] private TMP_Text   _counterTMP;           // 받는 반격

    [CenterHeader("< 상태 >")]
    [SerializeField] private ECardState _cardState;

    private Deck<Skill> _skillDeck;
    private Card        _selectCard;
    private bool        _isMyCardDrag;
    private bool        _onMyCardArea;

    private Queue<ECardType> _myDealTypes;    // 내 분배에 남은 속성 (각 속성 1장씩 보장)
    private Queue<ECardType> _otherDealTypes; // 상대 분배에 남은 속성

    private System.Random _myDealRng;    // 내 분배 전용 난수 (상대와 독립된 시드)
    private System.Random _otherDealRng; // 상대 분배 전용 난수

    private SkillCard _selectSkillCard;   // 드래그 중인 스킬 카드
    private bool      _isMySkillCardDrag; // 내 스킬 카드 드래그 중

    private Card   _fieldPreviewCard;    // 재사용하는 필드 엔티티 미리보기 카드
    private Entity _previewSourceEntity; // 현재 미리보기 중인 필드 엔티티

    private Card _dealtPreviewCard; // 딜 예측 — 내 카드 미리보기
    private Card _dealtEnemyCard;   // 딜 예측 — 상대 카드 미리보기

    private readonly WaitForSeconds _skillShowDelay = new(0.8f);          // 스킬 연출 카드 노출 유지
    private readonly WaitForSeconds _skillExitDelay = new(SkillExitTime); // 스킬 연출 카드 퇴장 대기

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
        _skillDeck = new Deck<Skill>(_skillSO.skills);

        _myDealRng    = new System.Random(System.Guid.NewGuid().GetHashCode());
        _otherDealRng = new System.Random(System.Guid.NewGuid().GetHashCode());

        _myDealTypes    = BuildDealTypeQueue(_myDealRng);
        _otherDealTypes = BuildDealTypeQueue(_otherDealRng);

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

        HidePlaceGuide(); // 드롭하면 안내 숨김
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
            ShowPlaceGuide(BoardLayout.IsMyFrontRow(Utils.MousePos.y)); // 배치 영역 포커스 + 역할 안내
        }
        else
        {
            HidePlaceGuide(); // 손패 영역으로 돌아오면 안내 숨김
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

        // 각 속성을 1장씩 보장: 진영별 독립 난수로 큐에서 속성을 꺼내 그 속성의 카드 중 무작위 1종을 준다
        System.Random rng  = isMine ? _myDealRng : _otherDealRng;
        ECardType     type = (isMine ? _myDealTypes : _otherDealTypes).Dequeue();
        card.Setup(PickRandomItemOfType(type, rng), isMine); // 내 카드는 앞면, 상대 카드는 뒷면

        (isMine ? _myCards : _otherCards).Add(card);

        SetOriginOrder(isMine);
        CardAlignment(isMine);
    }

    // 모든 속성을 무작위 순서로 담은 분배 큐를 만든다 (속성 종 수 = 시작 손패 수). 진영별 난수로 셔플
    private static Queue<ECardType> BuildDealTypeQueue(System.Random rng)
    {
        var types = new List<ECardType>((ECardType[])Enum.GetValues(typeof(ECardType)));

        for (int i = types.Count - 1; i > 0; i--) // Fisher-Yates
        {
            int j = rng.Next(i + 1);
            (types[i], types[j]) = (types[j], types[i]);
        }

        return new Queue<ECardType>(types);
    }

    // 지정 속성의 카드 중 무작위 1종을 반환한다 (진영별 난수)
    private Item PickRandomItemOfType(ECardType type, System.Random rng)
    {
        var pool = Array.FindAll(_itemSO.items, item => item.type == type);

        return pool[rng.Next(pool.Length)];
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

    // 치트 — 세팅 단계에서 내 손패 전부를 앞줄 우선으로 자동 배치한다 (GameManager 치트키가 호출)
    public void CheatAutoPlaceMyCards()
    {
        if (TurnManager.Inst.phase != TurnManager.EGamePhase.Setup)
        {
            return;
        }

        EntityManager.Inst.RemoveMyEmptyEntity(); // 드래그 미리보기 정리

        foreach (Card card in new List<Card>(_myCards))
        {
            if (!EntityManager.Inst.CheatPlaceMyCard(card.item))
            {
                continue;
            }

            _myCards.Remove(card);
            card.transform.DOKill();
            Destroy(card.gameObject);
        }

        _selectCard = null;
        CardAlignment(true);
    }

    #endregion

    #region 필드 미리보기

    // 필드 엔티티에 마우스 오버 — 내 엔티티는 상대 필드 쪽, 상대 엔티티는 내 필드 쪽에 카드 형태로 정보를 띄운다
    // (Entity.OnMouseOver가 호출)
    public void ShowFieldPreview(Entity entity)
    {
        // 드래그 중·빈 슬롯·공격 타겟팅 중이면 호버 미리보기를 띄우지 않는다(딜 예측과 겹침 방지)
        if (_isMyCardDrag || _isMySkillCardDrag || entity.isEmpty || EntityManager.Inst.IsSelectingAttacker)
        {
            return;
        }

        if (_previewSourceEntity == entity) // 같은 엔티티 위에서는 매 프레임 재생성하지 않는다
        {
            return;
        }

        Transform point = entity.isMine ? _otherPreviewPoint : _myPreviewPoint;
        if (point == null)
        {
            return;
        }

        _previewSourceEntity = entity;

        if (_fieldPreviewCard == null)
        {
            _fieldPreviewCard = Instantiate(_cardPrefab).GetComponent<Card>();
        }

        _fieldPreviewCard.gameObject.SetActive(true);
        _fieldPreviewCard.transform.position   = point.position;
        _fieldPreviewCard.transform.localScale = point.localScale;
        _fieldPreviewCard.Setup(entity.Item, true);                      // 앞면(정보 공개)으로 표시
        _fieldPreviewCard.GetComponent<Collider2D>().enabled = false;    // Setup이 콜라이더를 켜므로 다시 끈다(입력 차단)
        _fieldPreviewCard.GetComponent<Order>().SetMostFrontOrder(true); // 필드 엔티티 위로
    }

    // 필드 엔티티에서 마우스 이탈 — 해당 엔티티의 미리보기였으면 숨긴다 (Entity.OnMouseExit가 호출)
    public void HideFieldPreview(Entity entity)
    {
        if (_previewSourceEntity != entity)
        {
            return;
        }

        _previewSourceEntity = null;

        if (_fieldPreviewCard != null)
        {
            _fieldPreviewCard.gameObject.SetActive(false);
        }
    }

    #endregion

    #region 배치 안내 / 딜 예측

    // 세팅 드래그 중 — 마우스가 가리키는 행(전방/후방)을 강조하고 역할 안내를 띄운다 (CardDrag가 호출)
    private void ShowPlaceGuide(bool isFront)
    {
        if (_frontHighlight != null)
        {
            _frontHighlight.SetActive(isFront);
        }
        if (_backHighlight != null)
        {
            _backHighlight.SetActive(!isFront);
        }

        if (_placeGuideTMP == null)
        {
            return;
        }

        DOTween.Kill(_placeGuideTMP);
        _placeGuideTMP.alpha = 1f; // 경고 페이드로 낮아진 알파 복원
        _placeGuideTMP.gameObject.SetActive(true);
        _placeGuideTMP.text = isFront
            ? "전방에 배치할 카드 3장을 놓아주세요.\n전방 카드는 직접 공격이 가능합니다."
            : "후방에 배치할 카드 3장을 놓아주세요.\n후방 카드는 전방 카드 사망 시 왼쪽부터 순서대로 전방으로 투입됩니다.";
    }

    // 배치 안내·영역 강조를 숨긴다 (드롭/손패 복귀 시)
    private void HidePlaceGuide()
    {
        if (_frontHighlight != null)
        {
            _frontHighlight.SetActive(false);
        }
        if (_backHighlight != null)
        {
            _backHighlight.SetActive(false);
        }
        if (_placeGuideTMP != null)
        {
            DOTween.Kill(_placeGuideTMP);
            _placeGuideTMP.gameObject.SetActive(false);
        }
    }

    // 전투 중 경고 안내를 잠깐 띄웠다가 서서히 사라지게 한다 (도발 위반 등, EntityManager가 호출)
    public void ShowWarning(string message)
    {
        if (_placeGuideTMP == null)
        {
            return;
        }

        DOTween.Kill(_placeGuideTMP);
        _placeGuideTMP.gameObject.SetActive(true);
        _placeGuideTMP.text  = message;
        _placeGuideTMP.alpha = 1f;

        DOTween.To(() => _placeGuideTMP.alpha, a => _placeGuideTMP.alpha = a, 0f, WarningFadeTime)
            .SetTarget(_placeGuideTMP)
            .SetDelay(WarningHoldTime)
            .OnComplete(() => _placeGuideTMP.gameObject.SetActive(false));
    }

    // 공격 드래그 중 — 내 카드·상대 카드 미리보기와 예상 피해/반격을 표시한다 (EntityManager가 호출)
    public void ShowDamagePreview(Entity attacker, Entity defender)
    {
        if (_damagePreview == null)
        {
            return;
        }

        // 이미 떠 있던 호버 카드 미리보기를 끈다(겹침 방지)
        if (_fieldPreviewCard != null)
        {
            _fieldPreviewCard.gameObject.SetActive(false);
        }
        _previewSourceEntity = null;

        var (dealt, counter) = CombatSystem.Inst.PredictDamage(attacker, defender);

        _damagePreview.SetActive(true);
        SetPreviewCard(ref _dealtPreviewCard, _previewMyCardPoint,    attacker.Item, attacker.health);
        SetPreviewCard(ref _dealtEnemyCard,   _previewEnemyCardPoint, defender.Item, defender.health);

        if (_dealtTMP != null)
        {
            _dealtTMP.text = $"주는 피해 {dealt}";
        }
        if (_counterTMP != null)
        {
            _counterTMP.text = $"받는 반격 {counter}";
        }
    }

    // 딜 예측을 숨긴다 (타겟 해제/공격 실행 시)
    public void HideDamagePreview()
    {
        if (_damagePreview != null)
        {
            _damagePreview.SetActive(false);
        }
        if (_dealtPreviewCard != null)
        {
            _dealtPreviewCard.gameObject.SetActive(false);
        }
        if (_dealtEnemyCard != null)
        {
            _dealtEnemyCard.gameObject.SetActive(false);
        }
    }

    // 미리보기 카드를 지정 위치에 앞면으로 세팅한다(없으면 생성, 입력 차단). HP는 현재 값으로 표시
    private void SetPreviewCard(ref Card card, Transform point, Item item, int currentHealth)
    {
        if (point == null)
        {
            return;
        }

        if (card == null)
        {
            card = Instantiate(_cardPrefab).GetComponent<Card>();
        }

        card.gameObject.SetActive(true);
        card.transform.position   = point.position;
        card.transform.localScale = point.localScale;
        card.Setup(item, true);
        card.SetHealth(currentHealth);                         // 원본(max)이 아닌 현재 HP로 표시
        card.GetComponent<Collider2D>().enabled = false;       // 미리보기는 입력을 받지 않음
        card.GetComponent<Order>().SetMostFrontOrder(true);    // 다른 요소 위로
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
            ShowWarning("마나가 부족합니다");

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
            // 논타겟: 영역 밖에서 놓으면 발동 (마나 재확인). 카드를 치우고 발동 연출 후 효과 적용
            if (ManaManager.Inst.TrySpend(true, skill.manaCost))
            {
                DiscardSkillCard(true, card);
                StartCoroutine(PlaySkillCo(skill, true, null));
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
                DiscardSkillCard(true, card);
                StartCoroutine(PlaySkillCo(skill, true, target));
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
        DiscardSkillCard(false, card);
        StartCoroutine(PlaySkillCo(skill, false, target)); // 손패에서 치우되, 연출로 어떤 스킬인지 보여준 뒤 발동

        return true;
    }

    // 스킬 발동 연출 — 좌중앙에 스킬 카드를 띄워 어떤 스킬인지 보여준 뒤, 카드가 사라지면 실제 효과를 적용한다 (내/상대 공통)
    private IEnumerator PlaySkillCo(Skill skill, bool isMine, Entity target)
    {
        if (_skillCastPoint == null) // 연출 위치 미할당 시 곧바로 발동
        {
            SkillSystem.Inst.Cast(skill, isMine, target);

            yield break;
        }

        Vector3 showPos  = _skillCastPoint.position;
        Vector3 startPos = showPos + Vector3.left * SkillEnterOffset;

        var presented = Instantiate(_skillCardPrefab, startPos, Utils.QI).GetComponent<SkillCard>();
        presented.Setup(skill, true);
        presented.GetComponent<Collider2D>().enabled = false; // 연출 카드는 입력을 받지 않는다
        presented.transform.localScale = _skillCastPoint.localScale;
        presented.GetComponent<Order>().SetMostFrontOrder(true);

        presented.transform.DOMove(showPos, SkillEnterTime); // 왼쪽에서 스르륵 등장
        yield return _skillShowDelay;

        presented.transform.DOScale(Vector3.zero, SkillExitTime); // 사라짐
        yield return _skillExitDelay;
        Destroy(presented.gameObject);

        SkillSystem.Inst.Cast(skill, isMine, target); // 카드가 사라진 뒤 효과 적용
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
