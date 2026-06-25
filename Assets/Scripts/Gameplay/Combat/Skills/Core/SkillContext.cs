using UnityEngine;

// 스킬 효과 실행에 필요한 정보 묶음. 효과별로 필요한 것만 골라 쓴다 (case마다 다른 인자 배선 제거).
public readonly struct SkillContext
{
    public readonly bool    IsMine;       // 시전 진영
    public readonly Entity  Target;       // 대상 O 스킬(BuffSingle)만 사용
    public readonly int     Value;        // 효과 수치 (피해/회복/버프량)
    public readonly int     Duration;     // 버프 지속(버프 효과만 사용)
    public readonly Vector3 CastFrom;     // 투사체 시작 위치(포크포인트)
    public readonly bool    HasCastPoint; // 포크포인트 미할당 시 대상 위치로 폴백하기 위한 플래그

    public SkillContext(bool isMine, Entity target, int value, int duration, Vector3 castFrom, bool hasCastPoint)
    {
        IsMine       = isMine;
        Target       = target;
        Value        = value;
        Duration     = duration;
        CastFrom     = castFrom;
        HasCastPoint = hasCastPoint;
    }
}
