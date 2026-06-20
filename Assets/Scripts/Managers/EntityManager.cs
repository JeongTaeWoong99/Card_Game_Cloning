using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

// 전장(보드) 엔티티의 상태를 소유한다: 스폰 / 빈 슬롯 / 선택·타겟 / 정렬 / 사망 제거.
// 전투 해석은 CombatSystem, 상대 턴 행동은 EnemyAI, 승패 판정은 GameManager가 담당한다.
public class EntityManager : MonoBehaviour
{
    public static EntityManager Inst { get; private set; }

    private const int MaxEntityCount = 6;

    [CenterHeader("< 프리팹 >")]
    [SerializeField] private GameObject _entityPrefab;

    [CenterHeader("< 진영 엔티티 >")]
    [SerializeField] private List<Entity> _myEntities;
    [SerializeField] private List<Entity> _otherEntities;

    [CenterHeader("< 보스 / 빈 슬롯 >")]
    [SerializeField] private Entity _myEmptyEntity;
    [SerializeField] private Entity _myBossEntity;
    [SerializeField] private Entity _otherBossEntity;

    [CenterHeader("< 타겟 표시 >")]
    [SerializeField] private GameObject _targetPicker;

    private Entity _selectEntity;
    private Entity _targetPickEntity;

    // 외부(CombatSystem/EnemyAI/GameManager)가 보드 상태를 질의하기 위한 읽기 전용 노출
    public IReadOnlyList<Entity> MyEntities    => _myEntities;
    public IReadOnlyList<Entity> OtherEntities => _otherEntities;
    public Entity                MyBoss        => _myBossEntity;
    public Entity                OtherBoss     => _otherBossEntity;

    public bool IsFullMyEntities => _myEntities.Count >= MaxEntityCount && !ExistMyEmptyEntity;

    private bool IsFullOtherEntities   => _otherEntities.Count >= MaxEntityCount;
    private bool ExistTargetPickEntity => _targetPickEntity != null;
    private bool ExistMyEmptyEntity    => _myEntities.Exists(x => x == _myEmptyEntity);
    private int  MyEmptyEntityIndex    => _myEntities.FindIndex(x => x == _myEmptyEntity);
    private bool CanMouseInput         => TurnManager.Inst.myTurn && !TurnManager.Inst.isLoading;


    private void Awake()
    {
        Inst = this;
    }

    private void Start()
    {
        TurnManager.OnTurnStarted += OnTurnStarted;
    }

    private void OnDestroy()
    {
        TurnManager.OnTurnStarted -= OnTurnStarted;
    }

    private void Update()
    {
        ShowTargetPicker(ExistTargetPickEntity);
    }

    // 빈 슬롯(아군) 또는 빈자리(상대)에 엔티티를 생성한다. 가득 찼으면 false
    public bool SpawnEntity(bool isMine, Item item, Vector3 spawnPos)
    {
        if (isMine)
        {
            if (IsFullMyEntities || !ExistMyEmptyEntity)
            {
                return false;
            }
        }
        else
        {
            if (IsFullOtherEntities)
            {
                return false;
            }
        }

        var entityObject = Instantiate(_entityPrefab, spawnPos, Utils.QI);
        var entity       = entityObject.GetComponent<Entity>();

        if (isMine)
        {
            _myEntities[MyEmptyEntityIndex] = entity;
        }
        else
        {
            _otherEntities.Insert(Random.Range(0, _otherEntities.Count), entity);
        }

        entity.isMine = isMine;
        entity.Setup(item);
        EntityAlignment(isMine);

        return true;
    }

    // 드래그한 카드의 x 위치에 임시 빈 슬롯을 끼워 넣고, 위치 순으로 정렬해 미리보기를 제공한다
    public void InsertMyEmptyEntity(float xPos)
    {
        if (IsFullMyEntities)
        {
            return;
        }

        if (!ExistMyEmptyEntity)
        {
            _myEntities.Add(_myEmptyEntity);
        }

        Vector3 emptyEntityPos = _myEmptyEntity.transform.position;
        emptyEntityPos.x = xPos;
        _myEmptyEntity.transform.position = emptyEntityPos;

        // 빈 슬롯의 인덱스가 바뀌었을 때만 재정렬한다
        int emptyEntityIndex = MyEmptyEntityIndex;
        _myEntities.Sort((entity1, entity2) => entity1.transform.position.x.CompareTo(entity2.transform.position.x));
        if (MyEmptyEntityIndex != emptyEntityIndex)
        {
            EntityAlignment(true);
        }
    }

    public void RemoveMyEmptyEntity()
    {
        if (!ExistMyEmptyEntity)
        {
            return;
        }

        _myEntities.RemoveAt(MyEmptyEntityIndex);
        EntityAlignment(true);
    }

    public void EntityMouseDown(Entity entity)
    {
        if (!CanMouseInput)
        {
            return;
        }

        _selectEntity = entity;
    }

    public void EntityMouseUp()
    {
        if (!CanMouseInput)
        {
            return;
        }

        // selectEntity, targetPickEntity 둘 다 존재하고 공격 가능하면 공격한다
        if (_selectEntity && _targetPickEntity && _selectEntity.attackable)
        {
            CombatSystem.Inst.Attack(_selectEntity, _targetPickEntity);
        }

        _selectEntity     = null;
        _targetPickEntity = null;
    }

    public void EntityMouseDrag()
    {
        if (!CanMouseInput || _selectEntity == null)
        {
            return;
        }

        // 마우스 위치의 상대 엔티티를 타겟으로 찾는다
        bool existTarget = false;
        foreach (var hit in Physics2D.RaycastAll(Utils.MousePos, Vector3.forward))
        {
            Entity entity = hit.collider?.GetComponent<Entity>();
            if (entity != null && !entity.isMine && _selectEntity.attackable)
            {
                _targetPickEntity = entity;
                existTarget       = true;
                break;
            }
        }

        if (!existTarget)
        {
            _targetPickEntity = null;
        }
    }

    // 보스에게 직접 데미지 (치트용)
    public void DamageBoss(bool isMine, int damage)
    {
        var targetBossEntity = isMine ? _myBossEntity : _otherBossEntity;
        targetBossEntity.Damaged(damage);

        GameManager.Inst.CheckBattleResult();
    }

    // 죽은 엔티티(보스·빈칸 제외)를 흔들기→축소 연출 후 제거하고 진영을 재정렬한다
    public void RemoveDeadAndRealign(params Entity[] entities)
    {
        foreach (var entity in entities)
        {
            if (!entity.isDie || entity.isBossOrEmpty)
            {
                continue;
            }

            if (entity.isMine)
            {
                _myEntities.Remove(entity);
            }
            else
            {
                _otherEntities.Remove(entity);
            }

            DOTween.Sequence()
                .Append(entity.transform.DOShakePosition(1.3f))
                .Append(entity.transform.DOScale(Vector3.zero, 0.3f)).SetEase(Ease.OutCirc)
                .OnComplete(() =>
                {
                    EntityAlignment(entity.isMine);
                    Destroy(entity.gameObject);
                });
        }
    }

    private void OnTurnStarted(bool myTurn)
    {
        CombatSystem.Inst.AttackableReset(myTurn);

        if (!myTurn)
        {
            EnemyAI.Inst.Play();
        }
    }

    // 엔티티들을 BoardLayout이 계산한 슬롯 위치로 이동시키고 정렬 순서를 갱신한다
    private void EntityAlignment(bool isMine)
    {
        var targetEntities = isMine ? _myEntities : _otherEntities;

        for (int i = 0; i < targetEntities.Count; i++)
        {
            var targetEntity = targetEntities[i];
            targetEntity.originPos = BoardLayout.GetSlotPosition(isMine, i, targetEntities.Count);
            targetEntity.MoveTransform(targetEntity.originPos, true, 0.5f);
            targetEntity.GetComponent<Order>()?.SetOriginOrder(i);
        }
    }

    private void ShowTargetPicker(bool isShow)
    {
        _targetPicker.SetActive(isShow);

        if (ExistTargetPickEntity)
        {
            _targetPicker.transform.position = _targetPickEntity.transform.position;
        }
    }
}
