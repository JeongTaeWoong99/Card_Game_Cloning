using UnityEngine;

// 세팅 단계 배치 — 스폰·빈 슬롯 미리보기·배치 완료 판정 (BoardPlacement 구현)
public interface IBoardPlacement
{
    bool IsMyPlaceDone    { get; }
    bool IsOtherPlaceDone { get; }

    bool SpawnEntity(bool isMine, Item item, bool isFrontRow, Vector3 spawnPos);
    bool CheatPlaceMyCard(Item item);
    void InsertMyEmptyEntity(float xPos, float yPos);
    void RemoveMyEmptyEntity();
    int  RowRealCount(bool isMine, bool isFront);
}
