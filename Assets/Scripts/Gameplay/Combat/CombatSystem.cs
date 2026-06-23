using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

// 엔티티 간 공격 해석을 담당한다. 공격 데미지 = 공격자의 "현재 HP".
// 공격자 속성에 따라 분기한다: 근접(일반·힐러, 반격 O) / 무쌍(광역, 반격 X) / 원거리(화살, 반격 X).
// 공통 후처리(이동 정렬 원복 → 사망 정리/재정렬 → 승패 판정)를 한곳에 모았다.
public class CombatSystem : MonoBehaviour
{
    public static CombatSystem Inst { get; private set; }

    private const float MoveTime         = 0.4f; // 근접 공격자 이동(왕복) 한 구간 시간
    private const float MusouSplashRatio = 0.5f; // 무쌍 인접 추가 피해 비율

    [CenterHeader("< 참조 >")]
    [SerializeField] private GameObject _damagePrefab;
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

            default: // Normal, Healer
                MeleeAttack(attacker, defender);
                break;
        }
    }

    // 근접(일반·힐러) — 대상 위치로 이동 후 양측 현재 HP만큼 동시 피해(반격 O)
    private void MeleeAttack(Entity attacker, Entity defender)
    {
        attacker.GetComponent<Order>().SetMostFrontOrder(true);

        DOTween.Sequence()
            .Append(attacker.transform.DOMove(defender.originPos, MoveTime)).SetEase(Ease.InSine)
            .AppendCallback(() =>
            {
                // 순서 의존을 없애기 위해 양측 현재 HP를 먼저 캡처한 뒤 동시 적용한다
                int attackerHp = attacker.health;
                int defenderHp = defender.health;

                defender.Damaged(attackerHp);
                attacker.Damaged(defenderHp);
                SpawnDamage(attackerHp, defender.transform);
                SpawnDamage(defenderHp, attacker.transform);
            })
            .Append(attacker.transform.DOMove(attacker.originPos, MoveTime)).SetEase(Ease.OutSine)
            .OnComplete(() => AttackCallback(attacker, defender));
    }

    // 무쌍 — 이동 후 대상에 현재 HP 100%, 인접 적 1체(랜덤)에 50% 피해(반격 X)
    private void MusouAttack(Entity attacker, Entity defender)
    {
        attacker.GetComponent<Order>().SetMostFrontOrder(true);

        Entity splashTarget = EntityManager.Inst.GetRandomAdjacentFront(defender);

        DOTween.Sequence()
            .Append(attacker.transform.DOMove(defender.originPos, MoveTime)).SetEase(Ease.InSine)
            .AppendCallback(() =>
            {
                int damage = attacker.health;

                defender.Damaged(damage);
                SpawnDamage(damage, defender.transform);

                if (splashTarget != null)
                {
                    int splashDamage = Mathf.Max(1, Mathf.RoundToInt(damage * MusouSplashRatio));
                    splashTarget.Damaged(splashDamage);
                    SpawnDamage(splashDamage, splashTarget.transform);
                }
            })
            .Append(attacker.transform.DOMove(attacker.originPos, MoveTime)).SetEase(Ease.OutSine)
            .OnComplete(() => AttackCallback(attacker, defender, splashTarget));
    }

    // 원거리 — 제자리에서 화살을 발사하고, 화살이 도착했을 때만 대상에 현재 HP 피해(반격 X)
    private void RangedAttack(Entity attacker, Entity defender)
    {
        int damage = attacker.health;

        var arrow = Instantiate(_arrowPrefab).GetComponent<Arrow>();
        arrow.Fire(attacker.transform.position, defender.transform.position, () =>
        {
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
            target.Damaged(damage);
            SpawnDamage(damage, target.transform);

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

    // 데미지가 0 이하면 팝업을 만들지 않는다 (반격 없는 공격 등)
    private void SpawnDamage(int damage, Transform target)
    {
        if (damage <= 0)
        {
            return;
        }

        var damageComponent = Instantiate(_damagePrefab).GetComponent<Damage>();
        damageComponent.SetupTransform(target);
        damageComponent.Damaged(damage);
    }

    #endregion
}
