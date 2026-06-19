using UnityEngine;
using TMPro;
using DG.Tweening;

public class Card : MonoBehaviour
{
    [SerializeField] private SpriteRenderer _card;
    [SerializeField] private SpriteRenderer _character;
    [SerializeField] private TMP_Text       _nameTMP;
    [SerializeField] private TMP_Text       _attackTMP;
    [SerializeField] private TMP_Text       _healthTMP;
    [SerializeField] private Sprite         _cardFront;
    [SerializeField] private Sprite         _cardBack;

    public Item item;
    public PRS originPRS;

    private bool _isFront;


    public void Setup(Item item, bool isFront)
    {
        this.item = item;
        _isFront = isFront;

        if (_isFront)
        {
            _character.sprite = item.sprite;
            _nameTMP.text = item.name;
            _attackTMP.text = item.attack.ToString();
            _healthTMP.text = item.health.ToString();
        }
        else
        {
            _card.sprite = _cardBack;
            _nameTMP.text = "";
            _attackTMP.text = "";
            _healthTMP.text = "";
        }
    }

    private void OnMouseOver()
    {
        if (_isFront)
        {
            CardManager.Inst.CardMouseOver(this);
        }
    }

    private void OnMouseExit()
    {
        if (_isFront)
        {
            CardManager.Inst.CardMouseExit(this);
        }
    }

    private void OnMouseDown()
    {
        if (_isFront)
        {
            CardManager.Inst.CardMouseDown();
        }
    }

    private void OnMouseUp()
    {
        if (_isFront)
        {
            CardManager.Inst.CardMouseUp();
        }
    }

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
            transform.position = prs.pos;
            transform.rotation = prs.rot;
            transform.localScale = prs.scale;
        }
    }
}
