# CLAUDE.md

---

## 환경

| 항목             | 내용                  |
|------------------|-----------------------|
| Unity 버전       | 6000.5.0f1            |
| IDE              | Rider 2026.1.0.1      |
| 렌더 파이프라인  | 2D URP                |

---

## Skills 참조

필요한 작업 시 `/` 슬래시 커맨드로 호출하거나, 관련 작업 시 자동 로드된다.

| 슬래시 커맨드         | 경로                                              | 내용                |
|-----------------------|---------------------------------------------------|---------------------|
| `/commit-convention`  | [`.claude/skills/commit-convention/SKILL.md`](.claude/skills/commit-convention/SKILL.md) | Git 커밋 메시지 규칙 |
| `/clean-code-style`   | [`.claude/skills/clean-code-style/SKILL.md`](.claude/skills/clean-code-style/SKILL.md) | Unity/C# 클린 코드 스타일 규칙 |
| `/feature-design`     | [`.claude/skills/feature-design/SKILL.md`](.claude/skills/feature-design/SKILL.md) | OOP·SOLID·디자인 패턴 기반 기능 설계 |
| `/optimization`       | [`.claude/skills/optimization/SKILL.md`](.claude/skills/optimization/SKILL.md) | 성능 최적화 판단 및 적용 가이드 |
| `/unity-handoff`      | [`.claude/skills/unity-handoff/SKILL.md`](.claude/skills/unity-handoff/SKILL.md) | 유니티 에디터 작업 핸드오프 프롬프트 생성 |

---

## 주요 규칙

- 커밋 시 반드시 `/commit-convention` 규칙을 따른다.
- 코드 작성 시 `/clean-code-style` 규칙을 따른다.
- 기능 구현 시 `/feature-design` 규칙을 따른다.
- 최적화 작업 시 `/optimization` 규칙을 따른다.
- 유니티 에디터에서 해야 할 후속 작업이 있으면 `/unity-handoff` 규칙에 따라 프롬프트를 만들어 제공한다.
- 에셋 원본 폴더는 `.gitignore`에 등록하여 GitHub 업로드에서 제외한다.
- 참고 자료는 `.claude/references/` 폴더에 보관하며, git에서 제외된다.
- 코드는 한글 주석 사용.
