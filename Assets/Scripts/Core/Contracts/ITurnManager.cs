using System.Collections;

// 게임 흐름 — 페이즈·턴·배치 완료·로딩 상태 (TurnManager 구현)
public interface ITurnManager
{
    bool isLoading { get; set; }
    bool myTurn    { get; }

    bool IsBattlePhase { get; }
    bool IsSetupPhase  { get; }
    bool IsFastMode    { get; }

    IEnumerator StartGameCo();
    void OnSetupDone();
    void EndTurn();
}
