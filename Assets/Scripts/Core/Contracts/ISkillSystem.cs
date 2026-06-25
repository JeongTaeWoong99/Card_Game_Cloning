// 스킬 효과 실행 진입점 (SkillSystem 구현)
public interface ISkillSystem
{
    void Cast(Skill skill, bool isMine, Entity target = null);
}
