using UnityEngine;

// 전장(보드)에 배치된 엔티티들의 슬롯 위치를 계산한다.
// 위치 상수는 가로형 기준이며, 세로형 레이아웃 재조정은 콘텐츠 단계에서 진행한다.
public static class BoardLayout
{
    private const float MyRowY      = -4.35f;  // 아군 행 y
    private const float OtherRowY   =  4.15f;  // 상대 행 y
    private const float SlotSpacing =  6.8f;   // 슬롯 간 가로 간격

    // count개를 가로로 가운데 정렬했을 때 index번째 슬롯의 월드 위치
    public static Vector3 GetSlotPosition(bool isMine, int index, int count)
    {
        float y = isMine ? MyRowY : OtherRowY;
        float x = (count - 1) * -(SlotSpacing * 0.5f) + index * SlotSpacing;

        return new Vector3(x, y, 0f);
    }
}