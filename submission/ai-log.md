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
- 멀티(PUN 2) 유지 확정 후, Photon 무관 코어 스크립트 작성: `DialogueEntry`/`DialogueDatabase`(TextAsset 기반 JSON 로더, WebGL 호환) + `GameConfig`(승리 수치·확률 테이블·스태미나 상수)

---

## 2026-08-08~09 — 세션 2: MCP 연결 + PUN 검증 + WebGL 배포 검증

**도구**: Claude Code (Fable 5) + MCP for Unity (에디터 직접 조작)

**작업 내용**
- Unity MCP 연결 진단·복구: 클라이언트 등록 누락을 발견하고 `.mcp.json`(HTTP 127.0.0.1:8080) 작성, JSON-RPC 핸드셰이크로 서버 실동작 검증
- Photon Voice 2 임포트 검수(API 업데이터 안내) 후 Claude가 MCP `manage_scriptable_object`로 PhotonServerSettings에 PUN/Voice AppId 입력
- `ConnectionManager`(PUN 접속, 4자리 룸 코드 생성·입장, 최대 4인, 마스터 권한) + 스모크 테스트 작성 → **에디터 플레이 모드에서 실서버 접속 검증** (MCP로 플레이 진입·콘솔 확인까지 자동)
- WebGL 빌드 설정(압축 비활성, 제품명)을 MCP `execute_code`로 적용 → **WebGL 빌드(19분, docs/ 출력) → 로컬 서버 + 브라우저 자동화로 Photon WSS 접속 검증** (룸 5930, asia 리전)
- GitHub 리포 `doppelganger-village` 퍼블리시(사람) + Pages 활성화(사람), docs/ 커밋·푸시로 배포

**프롬프트 요지**
- "MCP 연결 확인" / "임포트 경고창 어떻게 하지" / "멀티 몇 명까지 가능?" — Claude가 진단·구현·검증 사이클을 주도

**결과와 수동 수정 내역**
- 컴파일 에러 0. 사람 개입: Photon 계정·AppId 발급, Asset Store 임포트 클릭, GitHub 리포 생성·Pages 설정, GitHub Desktop 푸시
- 배포 트러블슈팅: 첫 배포에서 게임 404 → Claude가 네트워크 로그로 `.gitignore`의 `Build/` 패턴이 `docs/Build/`까지 무시한 것을 진단, 루트 앵커링(`/Build/`)으로 수정 후 재배포
- **최종 검증: https://badwordfilter.github.io/doppelganger-village/ 에서 게임 로드 + Photon WSS 접속(룸 6883, 마스터 권한) 확인** — 심사 동선(링크 클릭→플레이) 전체 작동

---

## 2026-08-09 — 세션 3: 게임플레이 전체 구현 (Claude가 MCP로 에디터 직접 조작)

**도구**: Claude Code (Fable 5) + MCP for Unity — 스크립트 작성부터 씬 구축, 플레이 모드 검증까지 AI가 주도

**작업 내용 (구현 순서 CLAUDE.md 준수, 단계마다 플레이 모드 자동 검증 후 커밋)**
1. **플레이어/로비**: `PlayerController`(WASD·Shift 달리기·스태미나 소모/회복/탈진), `ThirdPersonCameraRig`(마우스 궤도·포인터락), PUN 동기화 프리팹(PhotonTransformView), 런타임 생성 uGUI 로비(방 만들기/4자리 코드 입장)·HP/기력 HUD. 한글 폰트는 Pretendard(OFL) 도입
2. **마을**: `execute_code`로 그레이박스 씬 자동 구축 — 파스텔 집 6채·트레일러·안전구역·동물 7마리(강아지3/고양이2/토끼2, 프리미티브 조형). 도플갱어 2~3마리를 마스터가 룸 CustomProperties로 배정(`VillageDirector`) — 늦게 합류해도 동일 결과
3. **대화 시스템**: E키 상호작용 → 질문 선택지 3개(일상/핵심 혼합, 핵심 최소 1개 보장), 마리당 질문 3회 전 플레이어 공유, **핵심 질문에서만 차수별 10/30/60% 확률로 이상 답변**(마스터가 굴려 RPC 브로드캐스트 — 전 클라이언트 동일), 연출 지문(괄호) 스타일 구분, **4번째 질문 = 과잉 심문**(HP -34, 동물 돌변·변색)
4. **판정·정산**: 트레일러로 보내기/거울 비추기 → 구출(드랍: 부품 65%/구급상자 10%/식량 10%/없음 15% — 무드랍을 둬서 도플갱어가 통계로 안 들키게), 퇴치(붉은 플래시), 진짜에게 거울=도주. **도플갱어를 보내도 겉보기 동일(잠입) → 정산 때 공개·구출 수 차감**. 목표(주민 3+부품 3) 도달 시 해질녘 정산 → 승리/계속/패배 분기, 전원 감염 시 게임 오버

**검증 방식 (AI 자동화)**
- 각 단계마다 Claude가 플레이 모드 진입 → Photon 실서버 룸 생성 → 리플렉션으로 UI 버튼 클릭/판정 시뮬레이션 → 콘솔·상태 조회로 검증 → 종료 후 커밋
- 승리 엔딩 시나리오 통합 테스트: 퇴치→도주→잠입→구출×4 → 정산 차감(5-1=3) → "탈출 성공!" 확인

**트러블슈팅**
- `Player` 네임스페이스 vs Photon `Player` 타입 충돌 → using 별칭
- 씬 PhotonView ViewID 0 (스크립트 저장 경로에서 PUN 에디터 훅 미작동) → sceneViewId 수동 할당
- MCP 스크린샷의 오버레이 UI 합성 불가(플레이 모드) → UI 검증을 리플렉션 상태 조회로 전환

**프롬프트 요지**
- "기획서 PDF와 대회 사이트를 분석해 사전 과제 실현 가능성을 판단하고 Unity MCP로 개발을 시작할 준비를 해줘"

**AI 처리 방식과 수동 수정 내역**
- PDF 표 추출: pdfplumber 표 인식 → 페이지 경계에 걸려 누락된 2행(23, 61번)은 pypdf 원문 텍스트에서 수동 이식
- 줄바꿈으로 소실된 띄어쓰기: 한국어 형태소 분석기 Kiwi의 띄어쓰기 교정을 **줄바꿈 접합부에만** 적용해 원문 훼손 없이 복원
- 검증: ① 전 항목을 공백 제거 후 PDF 원문과 부분 문자열 대조(자동) ② 67개 전항목 눈검수 → 2건 수동 보정(47번 "때 피가", 57번 "경계해야 하니까요")
- 기획서 원문 텍스트를 임의 창작 없이 그대로 이식함 (CLAUDE.md 데이터 규칙 준수)
