using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

// 전장(보드) 엔티티의 상태를 소유한다: 진영 행 보유 / 정렬 / 사망 제거·승급 / 턴 시작·종료 처리.
// 세팅 단계 배치(스폰·빈 슬롯 미리보기)는 BoardPlacement, 마우스 입력은 BoardInputController,
// 전투 해석은 CombatSystem, 상대 턴 행동은 EnemyAI, 승패 판정은 GameManager가 담당한다.
public class EntityManager : MonoService<IBoardState>, IBoardState
{
    public  const int   MaxRow          = 3;    // 한 행(앞줄/뒷줄)의 최대 슬롯 수
    private const float BackEffectDelay = 0.3f; // 턴 시작 패시브를 하나씩 보여주기 위한 텀(초)

    [CenterHeader("< 진영 (앞줄=공개 / 뒷줄=대기) >")]
    [SerializeField] private Faction _mine;
    [SerializeField] private Faction _other;

    private readonly WaitForSeconds _backEffectDelay = new(BackEffectDelay); // 효과 사이 텀(캐싱)

    // 외부(CombatSystem/EnemyAI)가 타겟·공격 후보로 쓰는 앞줄(공개) 엔티티
    public IReadOnlyList<Entity> MyFront    => _mine.Front;
    public IReadOnlyList<Entity> OtherFront => _other.Front;

    // 승패 판정용 — 진영의 카드(앞줄+뒷줄)가 모두 제거되었는지
    public bool IsMyAllDead    => _mine.IsAllDead;
    public bool IsOtherAllDead => _other.IsAllDead;

    #region 정렬 / 사망 제거

    // 죽은 앞줄 엔티티를 제거하고, 뒷줄 대기 카드를 왼쪽부터 앞줄로 승격시킨 뒤 흔들기→축소 연출로 정리한다
    public void RemoveDeadAndRealign(params Entity[] entities)
    {
        foreach (var entity in entities)
        {
            if (!entity.isDie || entity.isEmpty)
            {
                continue;
            }

            bool    isMine  = entity.isMine;
            Faction faction = Of(isMine);

            faction.Front.Remove(entity);

            bool promoted = faction.PromoteFromBack(); // 뒷줄 → 앞줄 승격(왼쪽부터)
            EntityAlignment(isMine, true);
            if (promoted)
            {
                EntityAlignment(isMine, false);
            }

            DOTween.Sequence()
                .Append(entity.transform.DOShakePosition(1.3f))
                .Append(entity.transform.DOScale(Vector3.zero, 0.3f)).SetEase(Ease.OutCirc)
                .OnComplete(() => Destroy(entity.gameObject));
        }
    }

    // 한 행의 엔티티들을 BoardLayout 슬롯 위치로 이동시키고 정렬 순서를 갱신한다 (배치·입력 컨트롤러도 호출)
    public void EntityAlignment(bool isMine, bool isFront)
    {
        var targetRow = GetRow(isMine, isFront);

        for (int i = 0; i < targetRow.Count; i++)
        {
            var targetEntity = targetRow[i];
            targetEntity.originPos = BoardLayout.GetSlotPosition(isMine, isFront, i, targetRow.Count);
            targetEntity.MoveTransform(targetEntity.originPos, true, 0.5f);
            targetEntity.GetComponent<Order>()?.SetOriginOrder(i);
        }
    }

    #endregion

    #region 턴

    // 턴 시작 효과를 순차로 발동한다 — 공격권 갱신(즉시) → 후방 줄 패시브 → 전방 줄 패시브.
    // 각 패시브가 자기 코루틴 안에서 텀을 둬 겹쳐 보이지 않게 하고, 완료까지 TurnManager가 yield로 대기한다 (TurnManager.StartTurnCo가 호출)
    public IEnumerator ProcessTurnStartEffectsCo(bool isMine)
    {
        Of(isMine).RefreshTurnStart();

        yield return ProcessRowPassivesCo(isMine, isFront: false); // 후방 줄
        yield return ProcessRowPassivesCo(isMine, isFront: true);  // 전방 줄
    }

    // 한 줄을 순서대로 돌며 각 카드의 턴 시작 패시브(타입 행동)를 실행한다. 어떤 타입이 무엇을 하는지는 ICardBehaviour가 안다.
    // 발동 도중 리스트가 바뀔 수 있으므로(원거리 견제로 적 사망 정리 등) 사본을 순회한다
    private IEnumerator ProcessRowPassivesCo(bool isMine, bool isFront)
    {
        foreach (Entity card in new List<Entity>(GetRow(isMine, isFront)))
        {
            if (card.isEmpty)
            {
                continue;
            }

            yield return card.Behaviour.OnTurnStartPassive(new TurnPassiveContext(card, isMine, isFront, _backEffectDelay));
        }
    }

    // 해당 진영의 앞줄·뒷줄 엔티티에 한시 버프 만료를 적용한다 (TurnManager.EndTurn이 호출)
    public void TickBuffs(bool isMine)
    {
        Of(isMine).TickBuffs();
    }

    #endregion

    #region 헬퍼

    // 진영의 앞줄(공개) 리스트를 반환한다 (SkillSystem이 전방 전체 효과에 호출)
    public IReadOnlyList<Entity> GetFront(bool isMine)
    {
        return Of(isMine).Front;
    }

    // 해당 진영 앞줄에서 무작위 1체를 반환한다 (빈 슬롯·사망 제외, 없으면 null)
    public Entity GetRandomFront(bool isMine)
    {
        return Of(isMine).GetRandomFront();
    }

    // 시전자 기준 적 진영 앞줄에서 무작위 1체를 반환한다 (무작위 적 피해 스킬이 호출)
    public Entity GetRandomEnemyFront(bool isMine)
    {
        return GetRandomFront(!isMine);
    }

    // 대상과 같은 행에서 좌/우 인접 1체를 무작위로 반환한다 (무쌍 추가 피해가 호출, 없으면 null)
    public Entity GetRandomAdjacentFront(Entity target)
    {
        var row = Of(target.isMine).Front;
        int index = row.IndexOf(target);
        if (index < 0)
        {
            return null;
        }

        var neighbors = new List<Entity>();
        if (index - 1 >= 0)
        {
            neighbors.Add(row[index - 1]);
        }
        if (index + 1 < row.Count)
        {
            neighbors.Add(row[index + 1]);
        }

        return neighbors.Count == 0 ? null : neighbors[Random.Range(0, neighbors.Count)];
    }

    // 근접 공격자가 이 타겟을 칠 수 있는지 — 적 전방에 도발 유발자(생존)가 있으면 그 카드만 허용. 도발 무시 공격자(원거리)는 제한 없음
    // (BoardInputController 공격 확정·EnemyAI 타겟 선택이 호출)
    public bool CanMeleeTarget(Entity attacker, Entity target)
    {
        if (attacker.Behaviour.IgnoresTaunt)
        {
            return true;
        }

        var  enemyFront = Of(target.isMine).Front;
        bool hasTaunter = enemyFront.Exists(e => !e.isEmpty && !e.isDie && e.Behaviour.IsTaunter);

        return !hasTaunter || target.Behaviour.IsTaunter;
    }

    // 진영·행에 해당하는 리스트를 반환한다 (BoardPlacement·BoardInputController가 행을 조작·조회)
    public List<Entity> GetRow(bool isMine, bool isFront)
    {
        return Of(isMine).GetRow(isFront);
    }

    // 내/상대 진영 객체를 선택한다 — isMine 분기를 한곳에 모은다
    private Faction Of(bool isMine)
    {
        return isMine ? _mine : _other;
    }

    #endregion
}
