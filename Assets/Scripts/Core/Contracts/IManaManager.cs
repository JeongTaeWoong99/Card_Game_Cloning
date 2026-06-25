// 마나 상태·규칙 (ManaManager 구현)
public interface IManaManager
{
    int  GetMana(bool isMine);
    void GainMana(bool isMine);
    void FillMana(bool isMine);
    bool CanAfford(bool isMine, int cost);
    bool TrySpend(bool isMine, int cost);
}
