using UnityEngine;
using TMPro;
using DG.Tweening;

// 전장에 배치된 카드(엔티티). 스탯·상태를 보유하고 데미지/사망 표시를 처리한다.
public class Entity : MonoBehaviour
{
    [CenterHeader("< 데이터 >")]
    [SerializeField] private Item _item;

    [CenterHeader("< 뷰 참조 >")]
    [SerializeField] private SpriteRenderer _entity;
    [SerializeField] private SpriteRenderer _character;
    [SerializeField] private TMP_Text       _nameTMP;
    [SerializeField] private TMP_Text       _attackTMP;
    [SerializeField] private TMP_Text       _healthTMP;
    [SerializeField] private GameObject     _sleepParticle;

    [CenterHeader("< 스탯 >")]
    public int attack;
    public int health;

    [CenterHeader("< 상태 >")]
    public bool    isMine;
    public bool    isDie;
    public bool    isBossOrEmpty;
    public bool    attackable;
    public Vector3 originPos;

    private int _liveCount;


    private void Start()
    {
        TurnManager.OnTurnStarted += OnTurnStarted;
    }

    private void OnDestroy()
    {
        TurnManager.OnTurnStarted -= OnTurnStarted;
    }

    public void Setup(Item item)
    {
        _item  = item;
        attack = item.attack;
        health = item.health;

        _character.sprite = item.sprite;
        _nameTMP.text     = item.name;
        _attackTMP.text   = attack.ToString();
        _healthTMP.text   = health.ToString();
    }

    // 데미지를 적용하고 이번 피해로 사망했는지 반환한다
    public bool Damaged(int damage)
    {
        health -= damage;
        _healthTMP.text = health.ToString();

        if (health <= 0)
        {
            isDie = true;

            return true;
        }

        return false;
    }

    public void MoveTransform(Vector3 pos, bool useDotween, float dotweenTime = 0f)
    {
        if (useDotween)
        {
            transform.DOMove(pos, dotweenTime);
        }
        else
        {
            transform.position = pos;
        }
    }

    // 소환 후 자신의 턴을 한 번 맞이하기 전까지는 잠자는 파티클을 켜 행동 불가(소환 멀미)를 표시한다
    private void OnTurnStarted(bool myTurn)
    {
        if (isBossOrEmpty)
        {
            return;
        }

        if (isMine == myTurn)
        {
            _liveCount++;
        }

        _sleepParticle.SetActive(_liveCount < 1);
    }

    private void OnMouseDown()
    {
        if (isMine)
        {
            EntityManager.Inst.EntityMouseDown(this);
        }
    }

    private void OnMouseUp()
    {
        if (isMine)
        {
            EntityManager.Inst.EntityMouseUp();
        }
    }

    private void OnMouseDrag()
    {
        if (isMine)
        {
            EntityManager.Inst.EntityMouseDrag();
        }
    }
}
