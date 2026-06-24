using System;
using UnityEngine;

// 양 진영의 마나 상태와 규칙을 소유한다. 턴 시작 시 +1, 최대 3.
// 스킬 시전 시 마나를 차감하며, 변동은 이벤트로 ManaUI에 알린다 (Observer).
public class ManaManager : MonoBehaviour
{
    public static ManaManager Inst { get; private set; }

    public const int MaxMana = 3; // 마나 상한 (UI 슬롯 수와 동일)

    private const int ManaPerTurn = 1; // 턴 시작 시 회복량

    // 마나 변동 알림 (isMine, 현재 마나) — ManaUI가 구독 (외부 Invoke 차단)
    public static event Action<bool, int> ManaChanged;

    public int MyMana    { get; private set; }
    public int OtherMana { get; private set; }


    // 싱글톤 등록 (Unity 메시지)
    private void Awake()
    {
        Inst = this;
    }

    // 진영의 현재 마나를 반환한다 (ManaUI 초기 동기화·시전 판정이 호출)
    public int GetMana(bool isMine)
    {
        return isMine ? MyMana : OtherMana;
    }

    // 턴 시작 시 해당 진영 마나를 +1 회복한다 (상한 MaxMana) (TurnManager.StartTurnCo가 호출)
    public void GainMana(bool isMine)
    {
        if (isMine)
        {
            MyMana = Mathf.Min(MyMana + ManaPerTurn, MaxMana);
        }
        else
        {
            OtherMana = Mathf.Min(OtherMana + ManaPerTurn, MaxMana);
        }

        ManaChanged?.Invoke(isMine, GetMana(isMine));
    }

    // 해당 진영 마나를 최대치로 채운다 (치트용)
    public void FillMana(bool isMine)
    {
        if (isMine)
        {
            MyMana = MaxMana;
        }
        else
        {
            OtherMana = MaxMana;
        }

        ManaChanged?.Invoke(isMine, GetMana(isMine));
    }

    // 마나가 충분한지 검사한다 (스킬 시전 가능 판정이 호출)
    public bool CanAfford(bool isMine, int cost)
    {
        return GetMana(isMine) >= cost;
    }

    // 마나가 충분하면 차감하고 true를 반환한다 (스킬 시전 시 CardManager·EnemyAI가 호출)
    public bool TrySpend(bool isMine, int cost)
    {
        if (!CanAfford(isMine, cost))
        {
            return false;
        }

        if (isMine)
        {
            MyMana -= cost;
        }
        else
        {
            OtherMana -= cost;
        }

        ManaChanged?.Invoke(isMine, GetMana(isMine));

        return true;
    }
}
