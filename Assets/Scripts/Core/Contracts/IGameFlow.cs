// 게임 최상위 흐름 — 알림·승패 판정·게임 시작 (GameManager 구현)
public interface IGameFlow
{
    void Notification(string message);
    void CheckBattleResult();
    void StartGame();
}
