using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

// 엔티티 간 공격 해석을 담당한다. 모든 전투 피해는 현재 HP의 절반(소수점 버림, 최소 1).
// 분기: 근접(일반·힐러·방패, 반격 O) / 흡혈(반격 O + 생존 시 회복) / 무쌍(광역, 반격 O) / 원거리(화살, 반격 X).
// 공통 후처리(이동 정렬 원복 → 사망 정리/재정렬 → 승패 판정)를 한곳에 모았다.
public class CombatSystem : MonoBehaviour
{
    public static CombatSystem Inst { get; private set; }

    private const float MoveTime          = 0.4f; // 근접 공격자 이동(왕복) 한 구간 시간
    private const float DamageRatio       = 0.5f; // 공격 피해 = 현재 HP의 절반
    private const float CounterRatio      = 0.5f; // 반격 피해 = 상대 현재 HP의 절반
    private const float MusouSplashRatio  = 0.5f; // 무쌍 인접 추가 피해 비율(본체 피해의 절반 = 원래 HP의 25%)
    private const int   VampireHealAmount = 3;    // 흡혈형이 공격 시 회복하는 고정량

    [CenterHeader("< 참조 >")]
    [SerializeField] private GameObject _damagePrefab; // 피해 -N 팝업
    [SerializeField] private GameObject _healPrefab;   // 회복·버프 +N 팝업 (색·크기 별도)
    [SerializeField] private GameObject _arrowPrefab;

    // 싱글톤 등록 (Unity 메시지)
    private void Awake()
    {
        Inst = this;
    }

    #region 공격

    // 공격 진입점 — 공격자 속성에 따라 근접/무쌍/원거리로 분기한다 (EntityManager.EntityMouseUp·EnemyAI가 호출)
    public void Attack(Entity attacker, Entity defender)
    {
        attacker.attackable = false;
        attacker.RefreshSleepParticle(); // 공격을 마쳐 더는 공격할 수 없음을 zzz로 표시

        switch (attacker.CardType)
        {
            case ECardType.Ranged:
                RangedAttack(attacker, defender);
                break;

            case ECardType.Musou:
                MusouAttack(attacker, defender);
                break;

            case ECardType.Vampire:
                VampireAttack(attacker, defender);
                break;

            default: // Normal, Healer, Shield
                MeleeAttack(attacker, defender);
                break;
        }
    }

    // 현재 HP에 비율을 적용한 피해(소수점 버림, 최소 1)
    private static int CalcDamage(int hp, float ratio)
    {
        return Mathf.Max(1, Mathf.FloorToInt(hp * ratio));
    }

    // (주는 피해, 받는 반격) 예측 — 실제 적용 X. 원거리는 반격 0, 방패 경감 반영 (딜 예측 UI가 호출)
    public (int dealt, int counter) PredictDamage(Entity attacker, Entity defender)
    {
        int dealt   = defender.ApplyDefense(CalcDamage(attacker.health, DamageRatio));
        int counter = attacker.CardType == ECardType.Ranged
            ? 0
            : attacker.ApplyDefense(CalcDamage(defender.health, CounterRatio));

        return (dealt, counter);
    }

    // 근접(일반·힐러·방패) — 대상 위치로 이동 후 양측 현재 HP 절반씩 동시 피해(반격 O)
    private void MeleeAttack(Entity attacker, Entity defender)
    {
        attacker.GetComponent<Order>().SetMostFrontOrder(true);

        DOTween.Sequence()
            .Append(attacker.transform.DOMove(defender.originPos, MoveTime)).SetEase(Ease.InSine)
            .AppendCallback(() =>
            {
                // 순서 의존을 없애기 위해 양측 피해를 먼저 계산(현재 HP 절반·방패 경감)한 뒤 동시 적용한다
                int toDefender = defender.ApplyDefense(CalcDamage(attacker.health, DamageRatio));
                int toAttacker = attacker.ApplyDefense(CalcDamage(defender.health, CounterRatio));

                defender.Damaged(toDefender);
                attacker.Damaged(toAttacker);
                SpawnDamage(toDefender, defender.transform);
                SpawnDamage(toAttacker, attacker.transform);
            })
            .Append(attacker.transform.DOMove(attacker.originPos, MoveTime)).SetEase(Ease.OutSine)
            .OnComplete(() => AttackCallback(attacker, defender));
    }

    // 흡혈 — 근접 이동 후 대상에 피해 → 반격을 받고 → 제자리로 돌아온 뒤 살아남았으면 회복(+VampireHealAmount)
    private void VampireAttack(Entity attacker, Entity defender)
    {
        attacker.GetComponent<Order>().SetMostFrontOrder(true);

        bool attackerDied = false;

        DOTween.Sequence()
            .Append(attacker.transform.DOMove(defender.originPos, MoveTime)).SetEase(Ease.InSine)
            .AppendCallback(() =>
            {
                int defenderHp = defender.health; // 반격용 — 공격 전 HP 캡처
                int toDefender = defender.ApplyDefense(CalcDamage(attacker.health, DamageRatio));

                defender.Damaged(toDefender);
                SpawnDamage(toDefender, defender.transform);

                int toAttacker = attacker.ApplyDefense(CalcDamage(defenderHp, CounterRatio));
                attackerDied   = attacker.Damaged(toAttacker);
                SpawnDamage(toAttacker, attacker.transform);
            })
            .Append(attacker.transform.DOMove(attacker.originPos, MoveTime)).SetEase(Ease.OutSine)
            .AppendCallback(() =>
            {
                if (!attackerDied) // 제자리로 돌아온 뒤 생존 시 회복
                {
                    attacker.Heal(VampireHealAmount);
                }
            })
            .OnComplete(() => AttackCallback(attacker, defender));
    }

    // 무쌍 — 이동 후 대상에 현재 HP 50%, 인접 적 1체(랜덤)에 25% 피해 + 대상의 반격(현재 HP 절반)을 받음
    private void MusouAttack(Entity attacker, Entity defender)
    {
        attacker.GetComponent<Order>().SetMostFrontOrder(true);

        Entity splashTarget = EntityManager.Inst.GetRandomAdjacentFront(defender);

        DOTween.Sequence()
            .Append(attacker.transform.DOMove(defender.originPos, MoveTime)).SetEase(Ease.InSine)
            .AppendCallback(() =>
            {
                int defenderHp = defender.health; // 반격용 — 공격 전 HP 캡처
                int mainDamage = defender.ApplyDefense(CalcDamage(attacker.health, DamageRatio));

                defender.Damaged(mainDamage);
                SpawnDamage(mainDamage, defender.transform);

                if (splashTarget != null)
                {
                    int splashDamage = splashTarget.ApplyDefense(CalcDamage(attacker.health, DamageRatio * MusouSplashRatio));
                    splashTarget.Damaged(splashDamage);
                    SpawnDamage(splashDamage, splashTarget.transform);
                }

                int toAttacker = attacker.ApplyDefense(CalcDamage(defenderHp, CounterRatio));
                attacker.Damaged(toAttacker);
                SpawnDamage(toAttacker, attacker.transform);
            })
            .Append(attacker.transform.DOMove(attacker.originPos, MoveTime)).SetEase(Ease.OutSine)
            .OnComplete(() => AttackCallback(attacker, defender, splashTarget));
    }

    // 원거리 — 제자리에서 화살을 발사하고, 화살이 도착했을 때만 대상에 현재 HP 절반 피해(반격 X)
    private void RangedAttack(Entity attacker, Entity defender)
    {
        int rawDamage = CalcDamage(attacker.health, DamageRatio);

        var arrow = Instantiate(_arrowPrefab).GetComponent<Arrow>();
        arrow.Fire(attacker.transform.position, defender.transform.position, () =>
        {
            int damage = defender.ApplyDefense(rawDamage);
            defender.Damaged(damage);
            SpawnDamage(damage, defender.transform);
            AttackCallback(attacker, defender);
        });
    }

    // 지정 위치에서 적 전방 1체로 화살을 쏘고, 도착 시 피해·정리·승패 판정 (후방 원거리 견제·투사체 스킬이 호출)
    public void FirePokeArrow(Vector3 from, Entity target, int damage)
    {
        var arrow = Instantiate(_arrowPrefab).GetComponent<Arrow>();
        arrow.Fire(from, target.transform.position, () =>
        {
            int dealt = target.ApplyDefense(damage);
            target.Damaged(dealt);
            SpawnDamage(dealt, target.transform);

            EntityManager.Inst.RemoveDeadAndRealign(target);
            GameManager.Inst.CheckBattleResult();
        });
    }

    // 공격 연출 종료 후 — 끌어올린 order 원복 → 사망 정리/재정렬 → 승패 판정 (각 공격 메서드가 호출)
    private void AttackCallback(params Entity[] entities)
    {
        var involved = new List<Entity>();

        foreach (Entity entity in entities)
        {
            if (entity == null)
            {
                continue;
            }

            entity.GetComponent<Order>().SetMostFrontOrder(false);
            involved.Add(entity);
        }

        EntityManager.Inst.RemoveDeadAndRealign(involved.ToArray());
        GameManager.Inst.CheckBattleResult();
    }

    #endregion

    #region 데미지 팝업

    // 외부(SkillSystem)에서도 데미지 팝업을 띄울 수 있게 공개한다 (무작위 적 피해 스킬 등)
    public void ShowDamagePopup(int damage, Transform target)
    {
        SpawnDamage(damage, target);
    }

    // HP 증가 팝업(+N) — 힐 전용 프리팹으로 띄운다(색·크기는 프리팹이 결정) (Entity.Heal/BuffHp가 호출)
    public void ShowHealPopup(int amount, Transform target)
    {
        if (_healPrefab == null || amount <= 0)
        {
            return;
        }

        var popup = Instantiate(_healPrefab).GetComponent<NumberPopup>();
        popup.SetupTransform(target);
        popup.Healed(amount);
    }

    // 데미지가 0 이하면 팝업을 만들지 않는다 (반격 없는 공격 등)
    private void SpawnDamage(int damage, Transform target)
    {
        if (damage <= 0)
        {
            return;
        }

        var popup = Instantiate(_damagePrefab).GetComponent<NumberPopup>();
        popup.SetupTransform(target);
        popup.Damaged(damage);
    }

    #endregion
}
