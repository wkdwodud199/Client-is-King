# Manifest — task-102

> **Load**: 기본 로드 세트의 일부. 이 task 가 *실제로* 의존하는 것만 명시해 컨텍스트를 최소화한다.
> 여기에 없는 개념/파일은 기본적으로 열지 않는다.

- **task_id**: task-102
- **inputs**: kb/concepts/project-brief.md (Unity 버전·픽셀 표준·아키텍처 SSOT), kb/concepts/demo-scope.md (범위 가드), kb/tasks/task-101/design.md (.gitignore/규약 선행), Unity 에디터 6000.3.8f1 (로컬 설치)
- **concepts_needed**: kb/concepts/project-brief.md, kb/concepts/demo-scope.md
- **related_files**: game/ProjectSettings/, game/Packages/manifest.json, game/Assets/Scripts/{Runtime,Editor}/, game/Assets/Tests/EditMode/, game/Assets/Settings/Rendering/, game/Assets/{Data,Scenes,Art/Placeholders}/
- **notes**: Unity 배치 실행에는 활성 라이선스 필요 (Hub 로그인). 배치 게이트: -batchmode -quit -nographics -projectPath game (+ -executeMethod / -runTests).
- **generated_by**: design=codex gpt-5.5/xhigh @codex 0.143.0, 2026-07-09 (fallback=none)
