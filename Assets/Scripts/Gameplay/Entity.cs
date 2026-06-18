using UnityEngine;
using UnityEngine.Serialization;
using TMPro;
using DG.Tweening;

public class Entity : MonoBehaviour
{
    [SerializeField, FormerlySerializedAs("item")]          private Item           _item;
    [SerializeField, FormerlySerializedAs("entity")]        private SpriteRenderer _entity;
    [SerializeField, FormerlySerializedAs("character")]     private SpriteRenderer _character;
    [SerializeField, FormerlySerializedAs("nameTMP")]       private TMP_Text       _nameTMP;
    [SerializeField, FormerlySerializedAs("attackTMP")]     private TMP_Text       _attackTMP;
    [SerializeField, FormerlySerializedAs("healthTMP")]     private TMP_Text       _healthTMP;
    [SerializeField, FormerlySerializedAs("sleepParticle")] private GameObject     _sleepParticle;

    public int attack;
    public int health;
    public bool isMine;
    public bool isDie;
    public bool isBossOrEmpty;
    public bool attackable;
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
        _item = item;
        attack = item.attack;
        health = item.health;

        _character.sprite = item.sprite;
        _nameTMP.text = item.name;
        _attackTMP.text = attack.ToString();
        _healthTMP.text = health.ToString();
    }

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
