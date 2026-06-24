using UnityEngine;

// 전장(보드) 슬롯 위치를 계산한다.
// 앞줄(공개) / 뒷줄(대기) 2행과 내 / 상대 진영을 지원한다. (행당 최대 3슬롯)
// 위치 상수는 기준값이며, 세부 좌표는 에디터에서 화면에 맞게 튜닝한다.
public static class BoardLayout
{
    private const float MyFrontY    = -6f;     // 내 앞줄(공개) y
    private const float MyBackY     = -15f;    // 내 뒷줄(대기) y
    private const float OtherFrontY =  6f;     // 상대 앞줄(공개) y
    private const float OtherBackY  =  15f;    // 상대 뒷줄(대기) y
    
    private const float MyRowSplitY = -10.5f;  // 내 앞줄/뒷줄 구분 기준 y (MyFrontY와 MyBackY의 중간)
    
    private const float SlotSpacing =  7f;     // 슬롯 간 가로 간격
    
    // 내 진영에서 드롭 y가 앞줄(공개)인지 뒷줄(대기)인지 판정한다 (배치 입력용)
    public static bool IsMyFrontRow(float y)
    {
        return y > MyRowSplitY;
    }

    // 진영·행에 맞춰 count개를 가로 중앙 정렬했을 때 index번째 슬롯의 월드 위치
    public static Vector3 GetSlotPosition(bool isMine, bool isFront, int index, int count)
    {
        float y = (isMine, isFront) switch
        {
            (true,  true)  => MyFrontY,
            (true,  false) => MyBackY,
            (false, true)  => OtherFrontY,
            _              => OtherBackY, // (false, false)
        };
        float x = (count - 1) * -(SlotSpacing * 0.5f) + index * SlotSpacing;

        return new Vector3(x, y, 0f);
    }
}
