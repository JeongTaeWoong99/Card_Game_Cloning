using System;
using UnityEngine;

// 엔티티 간 공격 해석·전투 연출 도구 (CombatSystem 구현)
public interface ICombatSystem
{
    void Attack(Entity attacker, Entity defender);
    (int dealt, int counter) PredictDamage(Entity attacker, Entity defender);
    void FireArrow(Vector3 from, Vector3 to, Action onHit);
    void FirePokeArrow(Vector3 from, Entity target, int damage);
    void FinishAttack(params Entity[] entities);
    void ShowDamagePopup(int damage, Transform target);
    void ShowHealPopup(int amount, Transform target);
}
