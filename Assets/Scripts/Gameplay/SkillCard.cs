using UnityEngine;
using TMPro;
using DG.Tweening;

// 손패의 스킬 카드. 앞면이면 이름/설명/마나코스트/아이콘을 표시하고, 마우스 입력을 CardManager로 전달한다.
// 엔티티 카드(Card)와 표시·동작이 다르므로 별도 클래스로 분리한다 (SRP).
public class SkillCard : MonoBehaviour
{
    [CenterHeader("< 뷰 참조 >")]
    [SerializeField] private SpriteRenderer _card;
    [SerializeField] private SpriteRenderer _icon;
    [SerializeField] private TMP_Text       _nameTMP;
    [SerializeField] private TMP_Text       _descriptionTMP;
    [SerializeField] private TMP_Text       _manaCostTMP;

    [CenterHeader("< 스프라이트 >")]
    [SerializeField] private Sprite _cardBack;
    [SerializeField] private Sprite _cardFront;

    [CenterHeader("< 데이터 >")]
    public Skill skill;
    public PRS   originPRS;

    private bool       _isFront;
    private Renderer[] _renderers; // 드래그 중 숨김 토글용 (대상 O 스킬)


    // 렌더러 캐싱 (Unity 메시지)
    private void Awake()
    {
        _renderers = GetComponentsInChildren<Renderer>(true);
    }

    // 카드 표시/숨김 — 대상 O 스킬을 드래그할 때 카드를 가리고 타겟팅 UI로 안내하기 위함 (CardManager가 호출)
    public void SetVisible(bool visible)
    {
        foreach (Renderer r in _renderers)
        {
            r.enabled = visible;
        }
    }

    // 스킬 데이터 세팅 — isFront면 정보 노출, 아니면 뒷면(상대 보유 카드)으로 가린다 (CardManager가 호출)
    public void Setup(Skill skill, bool isFront)
    {
        this.skill = skill;
        _isFront   = isFront;

        _icon.enabled = isFront;

        if (_isFront)
        {
            _card.sprite         = _cardFront;
            _icon.sprite         = skill.sprite;
            _nameTMP.text        = skill.name;
            _descriptionTMP.text = skill.description;
            _manaCostTMP.text    = skill.manaCost.ToString();
        }
        else
        {
            _card.sprite         = _cardBack;
            _nameTMP.text        = "";
            _descriptionTMP.text = "";
            _manaCostTMP.text    = "";
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
        if (UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject()) return;

        if (_isFront)
        {
            CardManager.Inst.SkillCardMouseOver(this);
        }
    }

    // 마우스 벗어남 — 원위치 (앞면만, Unity 마우스 메시지)
    private void OnMouseExit()
    {
        if (_isFront)
        {
            CardManager.Inst.SkillCardMouseExit(this);
        }
    }

    // 마우스 누름 — 드래그 시작 (앞면만, Unity 마우스 메시지)
    private void OnMouseDown()
    {
        if (UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject()) return;

        if (_isFront)
        {
            CardManager.Inst.SkillCardMouseDown(this);
        }
    }

    // 마우스 놓음 — 스킬 발동 시도 (앞면만, Unity 마우스 메시지)
    private void OnMouseUp()
    {
        if (_isFront)
        {
            CardManager.Inst.SkillCardMouseUp(this);
        }
    }
}
