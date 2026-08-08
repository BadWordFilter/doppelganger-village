# AI 활용 로그 — 도플갱어 마을 탈출

> NAN 2026 사전과제 제출물 4번 "AI 활용 기술 문서"의 원천 자료.
> 매 세션 종료 시 append: 날짜 / 작업 내용 / 사용한 프롬프트 요지 / 결과와 수동 수정 내역.

---

## 2026-08-08 — 세션 1: 프로젝트 셋업 + 대화 데이터 이식

**도구**: Claude Code (Fable 5) + Python (pdfplumber, pypdf, kiwipiepy)

**작업 내용**
- 기획서 PDF(AI게임대회.pdf) 전문 분석 및 NAN 2026 제출 요건 확인 (마감 8/10, 웹 빌드 필수·exe 불가, 제출물 5종)
- git 저장소 초기화 + Unity용 .gitignore 작성, 초기 커밋
- MCP for Unity 패키지(com.coplaydev.unity-mcp)를 Packages/manifest.json에 추가 — 이후 세션부터 Claude가 Unity 에디터를 직접 조작
- **기획서 대화 테이블 1~67번(강아지 22·고양이 23·토끼 22)을 `Assets/Data/dialogue.json`으로 이식** (`tools/extract_dialogue.py`)

**프롬프트 요지**
- "기획서 PDF와 대회 사이트를 분석해 사전 과제 실현 가능성을 판단하고 Unity MCP로 개발을 시작할 준비를 해줘"

**AI 처리 방식과 수동 수정 내역**
- PDF 표 추출: pdfplumber 표 인식 → 페이지 경계에 걸려 누락된 2행(23, 61번)은 pypdf 원문 텍스트에서 수동 이식
- 줄바꿈으로 소실된 띄어쓰기: 한국어 형태소 분석기 Kiwi의 띄어쓰기 교정을 **줄바꿈 접합부에만** 적용해 원문 훼손 없이 복원
- 검증: ① 전 항목을 공백 제거 후 PDF 원문과 부분 문자열 대조(자동) ② 67개 전항목 눈검수 → 2건 수동 보정(47번 "때 피가", 57번 "경계해야 하니까요")
- 기획서 원문 텍스트를 임의 창작 없이 그대로 이식함 (CLAUDE.md 데이터 규칙 준수)
