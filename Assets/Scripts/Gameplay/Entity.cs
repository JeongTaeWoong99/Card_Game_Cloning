using System.Collections.Generic;
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
    [SerializeField] private TMP_Text       _healthTMP;
    [SerializeField] private GameObject     _sleepParticle;

    [CenterHeader("< 스프라이트 >")]
    [SerializeField] private Sprite _cardFront; // 앞면(공개) 테두리
    [SerializeField] private Sprite _cardBack;  // 뒷면(상대 대기 카드)

    [CenterHeader("< 스탯 >")]
    public int health; // 공격 데미지로도 쓰인다 (HP가 곧 공격력)

    [CenterHeader("< 상태 >")]
    public bool    isMine;
    public bool    isDie;
    public bool    isEmpty;    // 배치 미리보기용 빈 슬롯 (제거·전투 제외)
    public bool    isWaiting;  // 뒷줄 대기 카드 (공격·피격 불가)
    public bool    attackable;
    public Vector3 originPos;

    private int   _maxHealth;          // 회복 상한 (버프는 이 값을 초과할 수 있다)
    private int   _waitCount;          // 공격 가능까지 남은 자기 턴 수 (속성별 대기시간)
    private Color _defaultHealthColor; // HP 텍스트 기본색 (버프 시 빨강으로 교체)
    private bool  _isFront;            // 현재 앞면(정보 공개) 여부 — 뒷면이면 HP 등을 숨긴다

    // 한시 버프 목록 (영구 버프는 만료가 없으므로 등록하지 않는다)
    private readonly List<ActiveBuff> _activeBuffs = new();

    private static readonly Color BuffHealthColor = Color.red; // 버프(health > maxHealth) 상태 표시

    // 만료를 추적할 한시 버프 1건 — 적용한 수치와 남은 자기 턴 수를 기억한다
    private struct ActiveBuff
    {
        public int amount;
        public int remainingTurns;
    }

    // 카드 속성 — CombatSystem이 공격 방식을 분기하는 데 사용 (다른 클래스가 호출)
    public ECardType CardType => _item.type;

    // 카드 원본 데이터 — 필드 엔티티 호버 시 카드 형태 미리보기에 사용 (CardManager가 호출)
    public Item Item => _item;

    // 회복 가능 여부 — 힐러 패시브/힐 스킬이 대상 선별에 사용 (다른 클래스가 호출)
    public bool CanHeal => !isEmpty && !isDie && health < _maxHealth;
    
    // HP 텍스트 기본색 캐싱 (Unity 메시지)
    private void Awake()
    {
        // 정렬용 빈 엔티티는 _healthTMP가 없으므로 패스
        if (_healthTMP == null) return;

        _defaultHealthColor = _healthTMP.color;
    }

    // 스탯·뷰·대기시간을 초기화한다 (스폰 시 EntityManager.SpawnEntity가 호출)
    // showFront: 앞면(스탯 노출) 여부, waiting: 뒷줄 대기 여부
    public void Setup(Item item, bool showFront, bool waiting)
    {
        _item      = item;
        health     = item.health;
        _maxHealth = item.health;
        isWaiting  = waiting;
        _waitCount = item.type.GetWaitTurn();

        SetFront(showFront);
        _sleepParticle.SetActive(!isWaiting && _waitCount > 0);
    }

    // 앞면(공개)/뒷면(가림) 표시를 전환한다 (Card.cs의 뒷면 처리 패턴과 동일)
    public void SetFront(bool isFront)
    {
        _isFront           = isFront;
        _entity.sprite     = isFront ? _cardFront : _cardBack;
        _character.enabled = isFront;

        if (isFront)
        {
            _character.sprite = _item.sprite;
            _nameTMP.text     = _item.name;
            RefreshHealthText();
        }
        else
        {
            _nameTMP.text   = "";
            _healthTMP.text = "";
        }
    }

    // HP 텍스트를 갱신한다. 버프(health > maxHealth)면 빨강, 아니면 기본색 (HP 변동 공통 경로)
    private void RefreshHealthText()
    {
        // 뒷면(대기·상대 비공개)은 후방 효과·피해로 갱신돼도 정보를 노출하지 않는다
        if (!_isFront)
        {
            _healthTMP.text = "";

            return;
        }

        _healthTMP.text  = health.ToString();
        _healthTMP.color = health > _maxHealth ? BuffHealthColor : _defaultHealthColor;
    }

    // 공격 불가 상태를 zzz(소환 멀미) 파티클로 표시한다 — 전방 공개 카드 한정. attackable 변경 후 호출
    public void RefreshSleepParticle()
    {
        _sleepParticle.SetActive(!isEmpty && !isWaiting && !attackable);
    }

    // 뒷줄 대기 → 앞줄 공개로 승격한다. 공개로 전환하고 대기시간을 다시 건다 (EntityManager가 호출)
    public void Promote()
    {
        isWaiting  = false;
        attackable = false;
        _waitCount = _item.type.GetWaitTurn();

        SetFront(true);
        RefreshSleepParticle(); // 승격한 턴은 공격 불가이므로 zzz 표시
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
        RefreshHealthText();

        if (health <= 0)
        {
            isDie = true;

            return true;
        }

        return false;
    }

    // HP를 회복한다 — 최대치(_maxHealth)를 넘지 않는다 (힐러 패시브·치유 스킬이 호출)
    public void Heal(int amount)
    {
        // 버프(충전 등)로 이미 최대치 이상이면 회복이 오히려 HP를 깎지 않도록 막는다
        if (health >= _maxHealth)
        {
            return;
        }

        health = Mathf.Min(health + amount, _maxHealth);
        RefreshHealthText();
    }

    // HP를 버프한다 — 최대치를 초과할 수 있다(= 공격력 증가, 빨간 텍스트) (버프 스킬이 호출)
    // duration: 0=영구(만료 없음), N=자기 턴 N회 동안 유지 후 되돌림
    public void BuffHp(int amount, int duration)
    {
        health += amount;

        // 한시 버프만 만료 추적 대상에 등록한다 (영구 버프는 되돌리지 않으므로 제외)
        if (duration > 0)
        {
            _activeBuffs.Add(new ActiveBuff { amount = amount, remainingTurns = duration });
        }

        RefreshHealthText();
    }

    // 자기 턴 종료 시 호출 — 한시 버프의 남은 턴을 1 줄이고, 0이 된 버프는 HP에서 되돌린다
    // (EntityManager.TickBuffs가 진영 단위로 호출)
    public void TickBuffs()
    {
        for (int i = _activeBuffs.Count - 1; i >= 0; i--)
        {
            ActiveBuff buff = _activeBuffs[i];
            buff.remainingTurns--;

            if (buff.remainingTurns <= 0)
            {
                health -= buff.amount;
                _activeBuffs.RemoveAt(i);
            }
            else
            {
                _activeBuffs[i] = buff;
            }
        }

        if (health < 1)
        {
            health = 1; // 버프 만료만으로는 사망하지 않는다
        }

        RefreshHealthText();
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
    // (EntityManager.RefreshTurnStart가 진영 순서를 보장하며 호출)
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
        RefreshSleepParticle();
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

    // 마우스 올림 — 필드 엔티티의 카드 형태 정보 미리보기 (Unity 마우스 메시지)
    private void OnMouseOver()
    {
        if (!isEmpty)
        {
            CardManager.Inst.ShowFieldPreview(this);
        }
    }

    // 마우스 벗어남 — 미리보기 숨김 (Unity 마우스 메시지)
    private void OnMouseExit()
    {
        CardManager.Inst.HideFieldPreview(this);
    }
}
