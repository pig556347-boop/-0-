# Unity 6.4 기준 변경

대상 버전은 사용자의 설치 화면에 표시된 **6000.4.10f1**이다.

- ProjectVersion을 해당 에디터 버전으로 변경했다.
- uGUI를 2.0.0으로 변경했다.
- 이동·적 AI·전투 속도 처리를 Unity 6의 Rigidbody.linearVelocity로 통일했다.
- 한글 글꼴이 없는 경우 Unity 6의 LegacyRuntime.ttf를 사용한다.
- Player Settings에 기존 Input Manager를 명시했다(activeInputHandler 0).
  기존 WASD/Space/C/Shift/J/K/L/I/Q/Esc 입력을 그대로 사용한다.
- Built-in Render Pipeline 및 자체 카툰/VFX 셰이더를 유지한다.
  에디터 버전 변경 때문에 URP로 전환할 필요는 없다.

## 열기

새 ZIP을 별도 폴더에 풀고 Unity Hub의 Projects에서 해당 폴더를 추가한다.
프로젝트 폴더는 Assets, Packages, ProjectSettings가 들어 있는 SchoolRuleZero 폴더다.
Unity 6.4 (6000.4.10f1)로 열고 에셋/패키지 import가 완료되기를 기다린다.
`School Rule Zero > Play Prototype`으로 실행한다.
`School Rule Zero > Run Smoke Checks`는 실제 전투 판정 검사다.
웹 빌드는 이 버전의 WebGL Build Support 설치 후 Build WebGL 메뉴를 사용한다.

## 확인 범위

Unity 에디터가 없는 클라우드 환경에서 소스와 설정을 수정했다.
순수 C# 테스트, 구문 검사, ZIP 구성 검사 결과는 별도로 기록한다.
Unity 6.4의 실제 패키지 import/컴파일/물리/셰이더/WebGL 검증은 아직 미실행이다.
이 ZIP은 Unity 프로젝트이며, 브라우저에서 즉시 실행되는 WebGL 빌드가 아니다.
