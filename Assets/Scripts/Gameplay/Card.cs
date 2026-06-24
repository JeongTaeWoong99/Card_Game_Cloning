using UnityEngine;
using UnityEngine.Serialization;
using TMPro;
using DG.Tweening;

// 손패의 카드. 앞면이면 정보를 표시하고, 마우스 입력을 CardManager로 전달한다.
public class Card : MonoBehaviour
{
    [CenterHeader("< 뷰 참조 >")]
    [SerializeField] private SpriteRenderer _card;
    [SerializeField] private SpriteRenderer _character;
    [SerializeField] private TMP_Text       _typeTMP;   // 타입명 (예: 일반)
    [SerializeField] private TMP_Text       _attackTMP; // 공격·능력 효과(합본)
    [SerializeField] private TMP_Text       _nameTMP;
    [SerializeField] private TMP_Text       _healthTMP;

    [CenterHeader("< 스프라이트 >")]
    [SerializeField] private Sprite _cardBack;
    [SerializeField] private Sprite _cardFront;

    [CenterHeader("< 데이터 >")]
    public Item item;
    public PRS  originPRS;

    private bool _isFront;


    // 카드 데이터 세팅 — isFront면 정보 노출, 아니면 뒷면(대기 카드)으로 가린다 (CardManager.AddCard가 호출)
    public void Setup(Item item, bool isFront)
    {
        this.item = item;
        _isFront  = isFront;

        // 뒷면(대기)에서는 캐릭터 렌더러를 꺼 프리팹 기본 스프라이트 노출을 막는다 (SkillCard·Entity와 동일 패턴)
        _character.enabled = isFront;

        if (_isFront)
        {
            // <공격>·<능력> 표기를 리치텍스트 태그로 해석하지 않고 그대로 보이게 한다
            _attackTMP.richText = false;

            _character.sprite = item.sprite;
            _typeTMP.text     = item.type.GetDisplayName(); // 타입명
            _attackTMP.text   = $"{item.type.GetAttackText()}\n{item.type.GetAbilityText()}"; // 공격+능력 합본
            _nameTMP.text     = item.name;
            _healthTMP.text   = item.health.ToString();
        }
        else
        {
            _card.sprite    = _cardBack;
            _typeTMP.text   = "";
            _attackTMP.text = "";
            _nameTMP.text   = "";
            _healthTMP.text = "";
        }

        // 뒤집힌 상대 카드는 마우스 입력이 필요 없으므로 콜라이더를 끈다
        GetComponent<PolygonCollider2D>().enabled = isFront;
    }

    // HP 표시를 현재 값으로 덮어쓴다 — 딜 예측 미리보기에서 원본(max)이 아닌 실제 HP를 보이기 위함 (CardManager가 호출)
    public void SetHealth(int currentHealth)
    {
        _healthTMP.text = currentHealth.ToString();
    }

    // PRS(위치/회전/스케일)로 이동 — 즉시 또는 DOTween 보간
    public void MoveTransform(PRS prs, bool useDotween, float dotweenTime = 0f)
    {
        if (useDotween)
        {
            transform.DOMove(prs.pos, dotweenTime);
            transform.DORotateQuaternion(prs.rot, dotweenTime);
            transform.DOScale(prs.scale, dotweenTime);
        }
        else
        {
            transform.position   = prs.pos;
            transform.rotation   = prs.rot;
            transform.localScale = prs.scale;
        }
    }

    // 마우스 올림 — 확대 (앞면만, Unity 마우스 메시지)
    private void OnMouseOver()
    {
        if (UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject()) return;

        if (_isFront)
        {
            CardManager.Inst.CardMouseOver(this);
        }
    }

    // 마우스 벗어남 — 원위치 (앞면만, Unity 마우스 메시지)
    private void OnMouseExit()
    {
        if (_isFront)
        {
            CardManager.Inst.CardMouseExit(this);
        }
    }

    // 마우스 누름 — 드래그 시작 (앞면만, Unity 마우스 메시지)
    private void OnMouseDown()
    {
        if (UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject()) return;

        if (_isFront)
        {
            CardManager.Inst.CardMouseDown();
        }
    }

    // 마우스 놓음 — 배치/취소 (앞면만, Unity 마우스 메시지)
    private void OnMouseUp()
    {
        if (_isFront)
        {
            CardManager.Inst.CardMouseUp();
        }
    }
}
