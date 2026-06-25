using System.Collections.Generic;
using UnityEngine;

// 한 진영(나/상대)의 앞줄(공개·전투)·뒷줄(대기) 엔티티를 묶어 소유한다.
// "내 진영/상대 진영" 분기를 데이터로 묶고, 진영 단위 행동(턴 시작·버프 만료·승급·무작위 선택)을 스스로 처리한다.
[System.Serializable]
public class Faction
{
    [SerializeField] private List<Entity> _front = new();
    [SerializeField] private List<Entity> _back  = new();

    public List<Entity> Front => _front;
    public List<Entity> Back  => _back;

    // 이 진영의 카드(앞줄+뒷줄)가 모두 제거되었는지 (승패 판정용)
    public bool IsAllDead => _front.Count == 0 && _back.Count == 0;

    // 앞줄/뒷줄 리스트를 반환한다
    public List<Entity> GetRow(bool isFront) => isFront ? _front : _back;

    // 앞줄에서 무작위 1체를 반환한다 (빈 슬롯·사망 제외, 없으면 null)
    public Entity GetRandomFront()
    {
        var alive = _front.FindAll(e => !e.isEmpty && !e.isDie);

        return alive.Count == 0 ? null : alive[Random.Range(0, alive.Count)];
    }

    // 자기 턴 시작 처리(대기시간·공격권 갱신)를 앞줄→뒷줄 순서로 적용한다
    public void RefreshTurnStart()
    {
        foreach (var entity in _front)
        {
            entity.OnMyTurnStart();
        }

        foreach (var entity in _back)
        {
            entity.OnMyTurnStart(); // 대기 카드는 내부에서 무시된다
        }
    }

    // 앞줄·뒷줄 전체에 한시 버프 만료를 적용한다
    public void TickBuffs()
    {
        foreach (var entity in _front)
        {
            entity.TickBuffs();
        }

        foreach (var entity in _back)
        {
            entity.TickBuffs();
        }
    }

    // 뒷줄의 가장 왼쪽 카드를 앞줄 빈 자리로 승격한다 (성공 시 true)
    public bool PromoteFromBack()
    {
        if (_front.Count >= EntityManager.MaxRow || _back.Count == 0)
        {
            return false;
        }

        var promoted = _back[0]; // 왼쪽부터
        _back.RemoveAt(0);
        _front.Add(promoted);
        promoted.Promote(); // 공개 전환 + 대기시간 재설정

        return true;
    }
}
