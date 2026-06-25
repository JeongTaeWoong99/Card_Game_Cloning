// 스킬 효과 하나의 실행 단위(Command). ESkillEffect 종류마다 구현을 둔다.
// 새 효과 = 이 인터페이스 구현 클래스 1개 추가 + SkillSystem 등록 1줄 (기존 효과는 무수정 = OCP).
public interface ISkillEffect
{
    void Execute(in SkillContext ctx);
}
