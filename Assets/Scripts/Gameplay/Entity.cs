using UnityEngine;
using TMPro;
using DG.Tweening;

// 전장에 배치된 카드(엔티티). 스탯·상태를 보유하고 데미지/사망/앞뒷면 표시를 처리한다.
// 앞줄(공개)은 전투에 참여하고, 뒷줄(대기)은 공격·피격이 불가하다.
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

    [CenterHeader("< 스프라이트 >")]
    [SerializeField] private Sprite _cardFront; // 앞면(공개) 테두리
    [SerializeField] private Sprite _cardBack;  // 뒷면(상대 대기 카드)

    [CenterHeader("< 스탯 >")]
    public int attack;
    public int health;

    [CenterHeader("< 상태 >")]
    public bool    isMine;
    public bool    isDie;
    public bool    isEmpty;    // 배치 미리보기용 빈 슬롯 (제거·전투 제외)
    public bool    isWaiting;  // 뒷줄 대기 카드 (공격·피격 불가)
    public bool    attackable;
    public Vector3 originPos;

    private int _waitCount; // 공격 가능까지 남은 자기 턴 수 (속성별 대기시간)


    // 스탯·뷰·대기시간을 초기화한다 (스폰 시 EntityManager.SpawnEntity가 호출)
    // showFront: 앞면(스탯 노출) 여부, waiting: 뒷줄 대기 여부
    public void Setup(Item item, bool showFront, bool waiting)
    {
        _item      = item;
        attack     = item.attack;
        health     = item.health;
        isWaiting  = waiting;
        _waitCount = item.type.GetWaitTurn();

        SetFront(showFront);
        _sleepParticle.SetActive(!isWaiting && _waitCount > 0);
    }

    // 앞면(공개)/뒷면(가림) 표시를 전환한다 (Card.cs의 뒷면 처리 패턴과 동일)
    public void SetFront(bool isFront)
    {
        _entity.sprite     = isFront ? _cardFront : _cardBack;
        _character.enabled = isFront;

        if (isFront)
        {
            _character.sprite = _item.sprite;
            _nameTMP.text     = _item.name;
            _attackTMP.text   = attack.ToString();
            _healthTMP.text   = health.ToString();
        }
        else
        {
            _nameTMP.text   = "";
            _attackTMP.text = "";
            _healthTMP.text = "";
        }
    }

    // 뒷줄 대기 → 앞줄 공개로 승격한다. 공개로 전환하고 대기시간을 다시 건다 (EntityManager가 호출)
    public void Promote()
    {
        isWaiting  = false;
        attackable = false;
        _waitCount = _item.type.GetWaitTurn();

        SetFront(true);
        _sleepParticle.SetActive(_waitCount > 0);
    }

    // 세팅 단계에서 행을 옮길 때 대기 상태·표시를 갱신한다 (내 카드는 항상 앞면)
    public void SetRowState(bool isFrontRow)
    {
        isWaiting = !isFrontRow;

        SetFront(true);
        _sleepParticle.SetActive(!isWaiting && _waitCount > 0);
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

    // 목표 위치로 이동 — 즉시 또는 DOTween 보간
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

    // 자기 턴 시작 시 호출 — 대기시간을 1 줄이고 공격 가능 여부·잠자기 파티클을 갱신한다
    // (EntityManager.OnTurnStarted가 진영 순서를 보장하며 호출)
    public void OnMyTurnStart()
    {
        if (isEmpty || isWaiting)
        {
            return;
        }

        if (_waitCount > 0)
        {
            attackable = false; // 이번 턴은 아직 대기
            _waitCount--;
        }
        else
        {
            attackable = true;
        }

        // 공격이 불가능한 동안에는 잠자기(소환 멀미) 파티클을 계속 띄운다
        _sleepParticle.SetActive(!attackable);
    }

    // 내 앞줄 엔티티 누름 — 공격자 선택 (Unity 마우스 메시지)
    private void OnMouseDown()
    {
        if (isMine && !isEmpty)
        {
            EntityManager.Inst.EntityMouseDown(this);
        }
    }

    // 손 뗌 — 공격 실행 판정 (Unity 마우스 메시지)
    private void OnMouseUp()
    {
        if (isMine && !isEmpty)
        {
            EntityManager.Inst.EntityMouseUp();
        }
    }

    // 드래그 — 타겟 지정 (Unity 마우스 메시지)
    private void OnMouseDrag()
    {
        if (isMine && !isEmpty)
        {
            EntityManager.Inst.EntityMouseDrag();
        }
    }
}
