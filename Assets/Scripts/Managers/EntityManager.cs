using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class EntityManager : MonoBehaviour
{
    public static EntityManager Inst { get; private set; }

    [SerializeField] private GameObject   _entityPrefab;
    [SerializeField] private GameObject   _damagePrefab;
    [SerializeField] private List<Entity> _myEntities;
    [SerializeField] private List<Entity> _otherEntities;
    [SerializeField] private GameObject   _targetPicker;
    [SerializeField] private Entity       _myEmptyEntity;
    [SerializeField] private Entity       _myBossEntity;
    [SerializeField] private Entity       _otherBossEntity;

    private Entity _selectEntity;
    private Entity _targetPickEntity;
    private readonly WaitForSeconds _delay1 = new WaitForSeconds(1f);
    private readonly WaitForSeconds _delay2 = new WaitForSeconds(2f);

    private const int MaxEntityCount = 6;

    public bool IsFullMyEntities => _myEntities.Count >= MaxEntityCount && !ExistMyEmptyEntity;
    private bool IsFullOtherEntities => _otherEntities.Count >= MaxEntityCount;
    private bool ExistTargetPickEntity => _targetPickEntity != null;
    private bool ExistMyEmptyEntity => _myEntities.Exists(x => x == _myEmptyEntity);
    private int MyEmptyEntityIndex => _myEntities.FindIndex(x => x == _myEmptyEntity);
    private bool CanMouseInput => TurnManager.Inst.myTurn && !TurnManager.Inst.isLoading;


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
        var entity = entityObject.GetComponent<Entity>();

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
            Attack(_selectEntity, _targetPickEntity);
        }

        _selectEntity = null;
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
                existTarget = true;
                break;
            }
        }
        if (!existTarget)
        {
            _targetPickEntity = null;
        }
    }

    public void DamageBoss(bool isMine, int damage)
    {
        var targetBossEntity = isMine ? _myBossEntity : _otherBossEntity;
        targetBossEntity.Damaged(damage);
        StartCoroutine(CheckBossDie());
    }

    public void AttackableReset(bool isMine)
    {
        var targetEntities = isMine ? _myEntities : _otherEntities;
        targetEntities.ForEach(x => x.attackable = true);
    }

    private void OnTurnStarted(bool myTurn)
    {
        AttackableReset(myTurn);

        if (!myTurn)
        {
            StartCoroutine(AICo());
        }
    }

    private IEnumerator AICo()
    {
        CardManager.Inst.TryPutCard(false);
        yield return _delay1;

        // attackable 한 otherEntities를 모아 순서를 섞는다
        var attackers = new List<Entity>(_otherEntities.FindAll(x => x.attackable));
        Utils.Shuffle(attackers);

        // 보스를 포함한 myEntities를 랜덤하게 시간차로 공격한다
        foreach (var attacker in attackers)
        {
            var defenders = new List<Entity>(_myEntities) { _myBossEntity };
            int rand = Random.Range(0, defenders.Count);
            Attack(attacker, defenders[rand]);

            if (TurnManager.Inst.isLoading)
            {
                yield break;
            }

            yield return _delay2;
        }
        TurnManager.Inst.EndTurn();
    }

    private void EntityAlignment(bool isMine)
    {
        float targetY = isMine ? -4.35f : 4.15f;
        var targetEntities = isMine ? _myEntities : _otherEntities;

        for (int i = 0; i < targetEntities.Count; i++)
        {
            float targetX = (targetEntities.Count - 1) * -3.4f + i * 6.8f;

            var targetEntity = targetEntities[i];
            targetEntity.originPos = new Vector3(targetX, targetY, 0f);
            targetEntity.MoveTransform(targetEntity.originPos, true, 0.5f);
            targetEntity.GetComponent<Order>()?.SetOriginOrder(i);
        }
    }

    private void Attack(Entity attacker, Entity defender)
    {
        // attacker가 defender 위치로 이동했다가 원래 위치로 돌아온다 (이때 order를 높인다)
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

    private void AttackCallback(params Entity[] entities)
    {
        // 죽은 엔티티(보스·빈칸 제외)를 제거 처리한다
        entities[0].GetComponent<Order>().SetMostFrontOrder(false);

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
        StartCoroutine(CheckBossDie());
    }

    private IEnumerator CheckBossDie()
    {
        yield return _delay2;

        if (_myBossEntity.isDie)
        {
            StartCoroutine(GameManager.Inst.GameOver(false));
        }

        if (_otherBossEntity.isDie)
        {
            StartCoroutine(GameManager.Inst.GameOver(true));
        }
    }

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

    private void ShowTargetPicker(bool isShow)
    {
        _targetPicker.SetActive(isShow);
        if (ExistTargetPickEntity)
        {
            _targetPicker.transform.position = _targetPickEntity.transform.position;
        }
    }
}
