using System.Collections.Generic;
using UnityEngine;

// 엔티티 간 공격 해석을 담당한다. 모든 전투 피해는 현재 HP의 절반(소수점 버림, 최소 1).
// 분기: 근접(일반·힐러·방패, 반격 O) / 흡혈(반격 O + 생존 시 회복) / 무쌍(광역, 반격 O) / 원거리(화살, 반격 X).
// 공통 후처리(이동 정렬 원복 → 사망 정리/재정렬 → 승패 판정)를 한곳에 모았다.
public class CombatSystem : MonoService<ICombatSystem>, ICombatSystem
{
    // behaviour의 공격 연출이 동일 수치로 계산하도록 공개한다
    public const float MoveTime     = 0.4f; // 근접 공격자 이동(왕복) 한 구간 시간
    public const float DamageRatio  = 0.5f; // 공격 피해 = 현재 HP의 절반
    public const float CounterRatio = 0.5f; // 반격 피해 = 상대 현재 HP의 절반

    [CenterHeader("< 참조 >")]
    [SerializeField] private GameObject _damagePrefab; // 피해 -N 팝업
    [SerializeField] private GameObject _healPrefab;   // 회복·버프 +N 팝업 (색·크기 별도)
    [SerializeField] private GameObject _arrowPrefab;

    #region 공격

    // 공격 진입점 — 공통 선처리 후, 공격 연출/해석은 타입별 behaviour에 위임한다 (EntityManager.EntityMouseUp·EnemyAI가 호출)
    public void Attack(Entity attacker, Entity defender)
    {
        attacker.attackable = false;
        attacker.RefreshSleepParticle(); // 공격을 마쳐 더는 공격할 수 없음을 zzz로 표시

        attacker.Behaviour.Attack(attacker, defender);
    }

    // 현재 HP에 비율을 적용한 피해(소수점 버림, 최소 1) — behaviour의 공격 연출이 공유한다
    public static int CalcDamage(int hp, float ratio)
    {
        return Mathf.Max(1, Mathf.FloorToInt(hp * ratio));
    }

    // (주는 피해, 받는 반격) 예측 — 실제 적용 X. 원거리는 반격 0, 방패 경감 반영 (딜 예측 UI가 호출)
    public (int dealt, int counter) PredictDamage(Entity attacker, Entity defender)
    {
        int dealt   = defender.ApplyDefense(CalcDamage(attacker.health, DamageRatio));
        int counter = attacker.Behaviour.ReceivesCounter
            ? attacker.ApplyDefense(CalcDamage(defender.health, CounterRatio))
            : 0;

        return (dealt, counter);
    }

    // 화살 발사 primitive — 원거리 공격 연출·후방 견제·투사체 스킬이 공유한다 (도착 시 onHit 실행)
    public void FireArrow(Vector3 from, Vector3 to, System.Action onHit)
    {
        var arrow = Instantiate(_arrowPrefab).GetComponent<Arrow>();
        arrow.Fire(from, to, onHit);
    }

    // 지정 위치에서 적 전방 1체로 화살을 쏘고, 도착 시 피해·정리·승패 판정 (후방 원거리 견제·투사체 스킬이 호출)
    public void FirePokeArrow(Vector3 from, Entity target, int damage)
    {
        FireArrow(from, target.transform.position, () =>
        {
            int dealt = target.ApplyDefense(damage);
            target.Damaged(dealt);
            SpawnDamage(dealt, target.transform);

            Services.Get<IBoardState>().RemoveDeadAndRealign(target);
            Services.Get<IGameFlow>().CheckBattleResult();
        });
    }

    // 공격 연출 종료 후 — 끌어올린 order 원복 → 사망 정리/재정렬 → 승패 판정 (behaviour의 공격 연출이 호출)
    public void FinishAttack(params Entity[] entities)
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

        Services.Get<IBoardState>().RemoveDeadAndRealign(involved.ToArray());
        Services.Get<IGameFlow>().CheckBattleResult();
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
