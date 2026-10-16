# 교칙 제0조 — Unity 2.5D 프로토타입

태권도를 사용하는 전학생의 학원 액션 프로토타입입니다.
첨부 소스의 주인공 표시 이름 **권민준**을 유지합니다.
현재 캠페인은 **1-1~1-11**이며 상세 규칙은 [CAMPAIGN_1_11.md](CAMPAIGN_1_11.md)를 참조하세요.
XZ 바닥 이동과 Y축 점프를 사용하는 Unity 3D 물리 구조입니다.

## 현재 상태

20단계 로드맵의 시스템 소스를 순서대로 확장했습니다. 캐릭터는 자체 제작
교복 임시 모델이며, 최종 일러스트 기반 에셋은 아직 교체하지 않았습니다.
**현재 클라우드 머신에는 Unity가 없어 Unity 컴파일·Play Mode·그래픽 검증은 미실행입니다.**
20단계의 최종 검증 완료를 의미하지 않습니다.

## 실행

1. Unity Hub에서 이 저장소 폴더를 프로젝트로 추가합니다.
2. **Unity 6.4 (6000.4.10f1) / Built-in Render Pipeline**을 기준으로 엽니다.
   Unity가 패키지와 에셋을 가져오는 동안 기다립니다.
3. `Assets/Scenes/Prototype.unity`를 열거나
   `School Rule Zero > Open Prototype` 메뉴를 사용합니다.
4. Play를 누르고 Game 뷰를 클릭합니다. 약 4초 오프닝 후 조작 튜토리얼이 시작됩니다.
   플레이어·적·교실·카메라·HUD는 씬의 Bootstrap이 런타임에 생성합니다.
5. 다시 연습하려면 Play를 중지한 뒤 다시 시작합니다.

`School Rule Zero > Play Prototype` 메뉴는 씬을 열고 Play를 시작합니다.
브라우저용 출력은 `School Rule Zero > Build WebGL` 메뉴로 생성합니다.
Unity Hub의 해당 에디터에 WebGL Build Support가 설치되어 있어야 합니다.
이 출력 폴더를 HTTP(S) 서버에 게시해야 브라우저 플레이 URL을 얻을 수 있습니다.
현재 저장소에는 WebGL 빌드 결과나 게시된 플레이 URL이 없고, 해당 빌드는 미실행입니다.

기존 Input Manager를 기본으로 사용합니다. Input System 패키지가 설치되어
활성화된 프로젝트에서는 새 키보드 입력 분기를 사용하며 Input Actions 파일은 필요 없습니다.
Unity 6 전용 속도/기본 글꼴 API를 사용합니다. Unity 6.4에서 가져온 결과는 미검증입니다.
기존 Input Manager 설정을 프로젝트에 명시했습니다. 버전 변경 기록은 UNITY6_MIGRATION.md를 참조하세요.

| 키 | 동작 |
| --- | --- |
| WASD | 상하좌우 이동 |
| Space | 점프; 상승 중 놓으면 짧은 점프 |
| C 유지 | 앉기 |
| Shift + 방향 | 방향 회피 |
| J | 기본공격; 공격 중 다시 눌러 최대 3회 입력 연계 |
| K | 강공격 |
| L 유지 | 정면 가드 |
| L을 피격 직전에 누르기 | 타이밍 방어 |
| I | 펀치: 왼손 → 오른손 → 3타부터 좌우 무작위 |
| Q | 현재 게이지 단계의 궁극기 |
| Esc | 일시정지·재개 |

궁극기: **33% 연속차기 / 66% 반격연계 / 100% 천파일격**.
사용 시 게이지를 모두 소모합니다. 반격연계는 발동 직후 0.65초 이내에
일반 공격을 받아야 연계가 이어집니다. 수치는 임시 밸런스이며 확정 기획이 아닙니다.

튜토리얼은 이동 → 점프 → 앉기 → 기본공격 3회 연속 적중 → 강공격 → 가드 → 타이밍 방어 →
회피 → 잡기 → 3단계 충전 → 천파일격 적중 순서입니다.
공격·가드·잡기는 실제 판정 이벤트로 진행하며, Q를 누르기만 해서는 완료되지 않습니다.
훈련 중에는 단계 전환/사망 시 연습 상대 HP를 회복하고, 방어 과제에서 적이 공격합니다.

## 검증

- 실제 순수 C# 소스를 컴파일한 자동 테스트: **58개 통과**, 경고·오류 0.
- Unity 소스의 C# 구문 검사: Unity 버전·입력 시스템·에디터/플레이어 8개 분기, 192개 검사 통과.
- 실제 Unity 컴파일, 물리, 렌더링, 프레임 속도 비교: **미실행**.

Unity에서 `School Rule Zero > Run Smoke Checks`를 선택하면 별도 검증 플레이가 시작됩니다.
실제 히트박스 HP 감소, 중복 타격 방지, 가드, 타이밍 방어, 회피 무적,
잡기·밀어내기, 천파일격·게이지 소모의 **7개 검사**를 실행하도록 작성했습니다.
이 러너 자체도 현재 환경에서는 미실행입니다.
결과는 Console과 `Application.persistentDataPath/prototype-smoke-results.json`에 기록합니다.
오프닝/튜토리얼의 수동 검증은 [개발 기록](DEVELOPMENT.md)의 체크리스트를 따릅니다.

순수 C# 테스트 재실행 (.NET 8 SDK):

```bash
dotnet build Tests/MovementFsm/MovementFsm.Tests.csproj \
  -p:BaseIntermediateOutputPath=/tmp/school-rule-zero-fsm/obj/ \
  -p:BaseOutputPath=/tmp/school-rule-zero-fsm/bin/
dotnet /tmp/school-rule-zero-fsm/bin/Debug/net8.0/MovementFsm.Tests.dll
```

한글 글꼴은 Noto Sans CJK KR이며 SIL Open Font License를 함께 포함했습니다.
캐릭터·교실·효과·셰이더는 이 프로토타입을 위해 작성한 임시 표현입니다.

## 추가 반영

좌측 하단 콤보 팝업은 `N HIT`를 표시하고 6회부터 `GOOD!`를 덧붙입니다.
Q 조작 과제 이후 우측 상단의 `불량 학생 제압 0/4` 목표가 시작됩니다.
상대는 한 명씩 총 네 명이며, 연습 중 제압이나 중복 판정은 세지 않습니다.
서랍장과 노을빛 창가 표현을 추가했습니다. 자세한 비교 기록은 ROADMAP_ADDITIONS.md를 참조하세요.

펀치와 Esc 음악 정지·재개, 튜토리얼 뒤 덩치전의 최신 검증: [실행 기록](Docs/Punches/README.md).

잔몹1 모델·네 공격·이동 연결과 1-5 이후 시체 정리의 최신 검증: [실행 기록](Docs/Minion1/README.md).

잔몹2와 전투 줌인 적용의 최신 검증: [실행 기록](Docs/Minion2/README.md).
