# Unity 6.4 수정본 재검토 (2026-10-11)

## 확인된 문제와 수정

사용자 Unity Console에서 PrototypeSmokeTest의 JsonUtility를 찾을 수 없는
CS0103 오류를 확인했습니다. Packages/manifest.json에
com.unity.modules.jsonserialize 1.0.0을 추가했습니다.
재검토에서 추가했던 inputlegacy와 textrendering 1.0.0은 사용자 Unity 6.4의
Package Manager가 찾지 못하는 것으로 확인되어 제거했습니다.
이 두 이름을 패키지 의존성으로 요구하지 않습니다.

전체 재검토에서 추가로 수정한 사항:

- 낮은 교실 경계벽의 충돌체를 바닥부터 높이 6까지 늘렸습니다.
  기본 점프로 경계벽을 넘어 맵 밖으로 떨어지는 경로를 막습니다.
  보이는 벽 높이는 그대로입니다.
- 제압한 적의 콜라이더를 끈 후 Rigidbody를 고정합니다.
  콜라이더가 없는 시체가 계속 바닥 아래로 떨어지지 않게 합니다.
- WebGL 출력은 빈 폴더만 허용하며 ZIP 이름에 시각을 붙입니다.
  기존 파일이 빌드 ZIP에 섞이거나 이전 ZIP이 덮어써지는 일을 방지합니다.
  ZIP 생성 실패 시 성공한 빌드 폴더 위치와 실제 실패 내용을 표시합니다.
- 회피 안내의 '방향키' 표현을 실제 지원 키인 WASD로 수정했습니다.

## 이번 수정본에서 실행한 확인

- 실제 순수 C# 로직을 컴파일하고 자동 테스트 54개 통과.
- 에디터/플레이어 전처리 분기 각각 84개, 총 168개 C# 구문 검사 통과.
- JSON 문법, Unity 6000.4.10f1 버전 설정 확인.
  패키지 이름 존재 여부를 클라우드 정적 검사로 검증하지 못했고,
  사용자 임포트에서 발견된 두 잘못된 선언을 제거했습니다.
- 모든 Assets 메타데이터 존재, GUID 중복 없음 확인.
- Prototype 씬의 Bootstrap 스크립트 GUID 연결 확인.
- Resources의 한글 글꼴, 카툰/타격 셰이더 파일 존재 확인.
- 기존 키보드 입력 활성화 설정 확인.
- Unity 공식 UnityCsReference 6000.4 소스에서 Rigidbody.linearVelocity,
  JsonUtility.ToJson, Font, Input.GetKey, ParticleSystem 모듈과
  PlayerSettings.WebGL.compressionFormat/threadsSupport 선언을 대조했습니다.
  참조 소스 확인은 실행 가능한 Unity 어셈블리로 컴파일한 검증이 아닙니다.
- 초기화 순서, 공격/가드/회피/잡기 취소, 궁극기, 튜토리얼 진행,
  적 생성과 HUD 대상 교체 흐름을 코드로 검토했습니다. 실제 물리 테스트는 미실행입니다.

## 검증 한계

클라우드에 Unity Editor가 없으므로 전체 Unity 컴파일, 에셋 임포트,
물리/그래픽 실행, WebGL 빌드 및 브라우저 실행은 수행하지 못했습니다.
구문 검사는 Unity API 타입 검사를 대체하지 않습니다.
따라서 추가 오류가 전혀 없다고 보증하는 자료는 아닙니다.

## PC에서 열기

Unity를 닫고 새 ZIP을 기존 폴더와 다른 위치에 압축 해제합니다.
Unity Hub의 Add project from disk로 Assets, Packages, ProjectSettings가
함께 있는 폴더를 추가한 다음 6000.4.10f1로 엽니다.
이전 프로젝트의 Library나 Packages 파일을 섞지 않습니다.
임포트 완료 후 School Rule Zero → Play Prototype으로 실행합니다.
