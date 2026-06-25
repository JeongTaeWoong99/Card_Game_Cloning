using UnityEngine;

// 턴 시작 패시브 실행에 필요한 정보 묶음.
public readonly struct TurnPassiveContext
{
    public readonly Entity         Self;    // 패시브 주체
    public readonly bool           IsMine;  // 시전 진영
    public readonly bool           IsFront; // 현재 앞줄 여부(앞/뒤로 동작이 갈리는 타입용)
    public readonly WaitForSeconds Delay;   // 효과 사이 텀(겹쳐 보이지 않게)

    public TurnPassiveContext(Entity self, bool isMine, bool isFront, WaitForSeconds delay)
    {
        Self    = self;
        IsMine  = isMine;
        IsFront = isFront;
        Delay   = delay;
    }
}
