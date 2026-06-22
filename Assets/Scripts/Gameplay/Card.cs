using UnityEngine;
using TMPro;
using DG.Tweening;

// 손패의 카드. 앞면이면 정보를 표시하고, 마우스 입력을 CardManager로 전달한다.
public class Card : MonoBehaviour
{
    [CenterHeader("< 뷰 참조 >")]
    [SerializeField] private SpriteRenderer _card;
    [SerializeField] private SpriteRenderer _character;
    [SerializeField] private TMP_Text       _typeTMP;
    [SerializeField] private TMP_Text       _abilityTMP;
    [SerializeField] private TMP_Text       _nameTMP;
    [SerializeField] private TMP_Text       _attackTMP;
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

        if (_isFront)
        {
            _character.sprite = item.sprite;
            _typeTMP.text     = $"타입 : {item.type.GetDisplayName()}";
            _abilityTMP.text  = $"능력 : {item.ability}"; // 능력 내용은 차후 입력
            _nameTMP.text     = item.name;
            _attackTMP.text   = item.attack.ToString();
            _healthTMP.text   = item.health.ToString();
        }
        else
        {
            _card.sprite     = _cardBack;
            _typeTMP.text    = "";
            _abilityTMP.text = "";
            _nameTMP.text    = "";
            _attackTMP.text  = "";
            _healthTMP.text  = "";
        }

        // 뒤집힌 상대 카드는 마우스 입력이 필요 없으므로 콜라이더를 끈다
        GetComponent<PolygonCollider2D>().enabled = isFront;
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
