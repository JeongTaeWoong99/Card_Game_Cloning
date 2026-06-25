using System.Collections;

// 카드 타입 하나의 행동/표시 계약. 표시 텍스트·대기턴·전투 규칙·턴 시작 패시브를 담는다.
// 새 타입 = 이 인터페이스 구현(베이스 상속) 1개 추가 (Strategy + OCP).
public interface ICardBehaviour
{
    int    WaitTurn    { get; }
    string DisplayName { get; }
    string AttackText  { get; }
    string AbilityText { get; }

    int  ModifyIncomingDamage(int rawDamage); // 받는 피해 가공 (방패 경감 등, 기본=그대로)
    bool IsTaunter    { get; }                // 도발 유발자 — 적 근접이 이 카드만 노림 (기본=false)
    bool IgnoresTaunt { get; }                // 도발을 무시하는 공격자 — 원거리 (기본=false)

    void Attack(Entity attacker, Entity defender); // 타입별 공격 연출/해석 (기본=근접)
    bool ReceivesCounter { get; }                  // 반격을 받는가 — 딜 예측용 (기본=true, 원거리=false)

    IEnumerator OnTurnStartPassive(TurnPassiveContext ctx); // 자기 턴 시작 패시브 (기본=없음)
}