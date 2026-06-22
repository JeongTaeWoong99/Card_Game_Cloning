using UnityEngine;
using DG.Tweening;

// 엔티티 간 공격 해석을 담당한다.
// 이동 연출 → 양측 데미지 적용 → 데미지 팝업 → 사망 정리/재정렬(EntityManager) → 승패 판정(GameManager) 순.
// 카드 종류별 효과는 추후 이 클래스에만 확장하면 되도록 전투 로직을 한곳에 모았다.
public class CombatSystem : MonoBehaviour
{
    public static CombatSystem Inst { get; private set; }

    [CenterHeader("< 참조 >")]
    [SerializeField] private GameObject _damagePrefab;

    // 싱글톤 등록 (Unity 메시지)
    private void Awake()
    {
        Inst = this;
    }

    // 턴 시작 시 해당 진영 엔티티들의 공격 가능 상태를 복구한다 (EntityManager.OnTurnStarted가 호출)
    public void AttackableReset(bool isMine)
    {
        var targetEntities = isMine ? EntityManager.Inst.MyEntities : EntityManager.Inst.OtherEntities;

        foreach (var entity in targetEntities)
        {
            entity.attackable = true;
        }
    }

    // attacker가 defender 위치로 이동했다가 돌아오며 서로 데미지를 주고받는다 (이동 중 order를 높임)
    // (EntityManager.EntityMouseUp · EnemyAI가 호출)
    public void Attack(Entity attacker, Entity defender)
    {
        attacker.attackable = false;
        attacker.GetComponent<Order>().SetMostFrontOrder(true);

        DOTween.Sequence()
            .Append(attacker.transform.DOMove(defender.originPos, 0.4f)).SetEase(Ease.InSine)
            .AppendCallback(() =>
            {
                attacker.Damaged(defender.attack);
                defender.Damaged(attacker.attack);
                SpawnDamage(defender.attack, attacker.transform);
                SpawnDamage(attacker.attack, defender.transform);
            })
            .Append(attacker.transform.DOMove(attacker.originPos, 0.4f)).SetEase(Ease.OutSine)
            .OnComplete(() => AttackCallback(attacker, defender));
    }

    // 공격 연출 종료 후 — order 원복 → 사망 정리/재정렬 → 승패 판정
    private void AttackCallback(Entity attacker, Entity defender)
    {
        attacker.GetComponent<Order>().SetMostFrontOrder(false);

        EntityManager.Inst.RemoveDeadAndRealign(attacker, defender);
        GameManager.Inst.CheckBattleResult();
    }

    // 데미지가 0 이하면 팝업을 만들지 않는다 (원거리 반격 0 등)
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
}
