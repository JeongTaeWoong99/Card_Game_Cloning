using System;
using UnityEngine;
using DG.Tweening;

// 원거리 공격용 화살. 시작점에서 대상까지 날아가 도착하면 콜백을 호출하고 스스로 사라진다.
// 피해 적용은 콜백을 받은 CombatSystem이 담당한다 (Arrow는 비행 연출만 책임).
public class Arrow : MonoBehaviour
{
    private const int   FlightOrder = 900;  // 비행 중 다른 엔티티 위로 보이게 하는 정렬 순서
    private const float FlightTime  = 0.35f; // 시작→대상 비행 시간

    // 시작점에서 대상 위치로 발사한다 — 진행 방향으로 회전, 도착 시 onArrive 호출 후 자기 파괴
    // (CombatSystem.RangedAttack이 호출)
    public void Fire(Vector3 start, Vector3 target, Action onArrive)
    {
        transform.position = start;

        Vector3 direction = target - start;
        float   angle     = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);

        GetComponent<Order>()?.SetOrder(FlightOrder);

        transform.DOMove(target, FlightTime).SetEase(Ease.InQuad)
            .OnComplete(() =>
            {
                onArrive?.Invoke();
                Destroy(gameObject);
            });
    }
}
