# 전투 밸런스 조사 근거

조사일: 2026-10-17 (한국 시간). 아래 공개 게임 소스 파일은 해당 고정 커밋의 raw URL에 실제 HTTP 요청을 보내 **모두 200 응답으로 본문을 받은 뒤** 관련 함수를 읽었다. 실행하거나 타 게임을 직접 플레이한 결과는 아니다. 원작의 피해량·프레임 수치를 KICK Back에 복사하지 않으며, 아래의 적용 방향은 우리 교실 액션에 맞춘 별도의 설계 판단이다.

## 실제로 읽은 개발사 공개 소스 3종

### 1. Overgrowth — Wolfire Games

공식 저장소 README는 “This is the official repository for Overgrowth's source code”라고 밝힌다. 현재 저장소 운영은 커뮤니티가 담당한다.

- 커밋: `245fe4828631c84c0023d29d1525f5716ccb6106`
- [방어·회피 코드 원문](https://raw.githubusercontent.com/WolfireGames/overgrowth/245fe4828631c84c0023d29d1525f5716ccb6106/Data/Scripts/aschar.as)
- [튜토리얼 원문](https://raw.githubusercontent.com/WolfireGames/overgrowth/245fe4828631c84c0023d29d1525f5716ccb6106/Data/Scripts/tutorial.as)
- [적 AI 원문](https://raw.githubusercontent.com/WolfireGames/overgrowth/245fe4828631c84c0023d29d1525f5716ccb6106/Data/Scripts/enemycontrol.as)

확인한 내용:

- `aschar.as` 4787행 부근 `UpdateActiveBlockMechanics`: `active_block_recharge <= 0.0f`일 때만 새로운 방어 판정을 시작한다. 입력을 반복하면 충전 지연이 생기며 판정 지속시간과 재사용 시간이 분리된다.
- 4813행 부근 `UpdateActiveDodgeMechanics`: 이동 상태·접지·공중 회전·구속 상태를 검사하고 회피 재사용 조건을 둔다.
- 6251행: `active_dodge_time = -1.0; // Avoid double dodge from one dodge input`로 한 회피 입력이 두 번 성공하지 않게 소비한다.
- `tutorial.as` 105~106행: “Vary your attacks to break through his guard!”, “When your attack is blocked or dodged, you are vulnerable to throws.” 다양하게 공격해야 하며 막히거나 회피당한 공격에는 위험이 있음을 실제 게임의 조작 안내가 설명한다.
- `enemycontrol.as` 1987행 부근: 반복해서 점프킥에 맞은 횟수(`got_hit_by_leg_cannon_count`)가 이후 회피 선택 확률에 반영된다. 무조건 회피하는 규칙은 아니다.
- 3295행 부근 `GetRepulsorForce`: 같은 편 캐릭터 사이의 근접 반발력을 계산해 적끼리 겹치는 현상을 줄인다.

KICK Back 적용 판단:

- L 패링 유효시간과 재시도 간격을 분리하고, L 연타로 유효시간을 계속 새로 만들지 않는다. 지속 가드는 별도로 허용한다.
- 회피 한 번에는 하나의 무적 구간만 부여하고, 회피 종료 즉시 무한 연속 무적으로 이어지지 않게 한다.
- J/I의 반복 입력과 K의 헛친 공격 뒤에 실제 후딜을 남긴다.
- 잔몹의 접근 방향을 분산한다. 도재학은 같은 행동 반복에 제한적으로 대응하되, 회피 확률 상한과 다음 공격까지의 간격을 둔다.
- 동시 공격 수 상한은 이 소스에서 확인한 수치가 아니다. 우리 게임의 카메라·키 구성에 맞춰 따로 정하는 정책이다.

### 2. Barony — Turning Wheel

개발사 공식 저장소의 README에 개발자 Sheridan의 “the open source release of Barony” 공개 편지가 포함되어 있다. RPG 전투이므로 격투게임의 콤보 프레임표로 사용하지 않는다.

- 커밋: `962a5ce36d10207beef7d8673876e0cebf8e76e4`
- [적 전투 코드 원문](https://raw.githubusercontent.com/TurningWheel/Barony/962a5ce36d10207beef7d8673876e0cebf8e76e4/src/actmonster.cpp)

확인한 내용:

- `actmonster.cpp` 10560행 부근: `monsterHitTime`이 무기·전역 속도에 따른 임계값을 넘는지 검사한 뒤 공격을 준비한다.
- 10570행 부근: 특수 행동을 처리한 프레임에는 일반 공격을 중복 실행하지 않고 반환한다.
- 10600행 이후: 쿨다운이 끝났어도 거리와 `lineTrace`로 실제 목표가 사정거리·시야 안에 있는지 재확인한다.
- 10693행 부근: `// skip attack, continue defending. offset the hit time to allow for timing variation.` 방어 유지를 선택한 경우 공격을 생략하고 재사용 시점을 조정한다.

KICK Back 적용 판단:

- 잔몹과 보스는 단순히 시간이 됐다는 이유만으로 허공에서 연계를 끝까지 재생하지 않는다. 연계 전환점마다 생존·거리·상태를 확인한다.
- 공격 시작 후 방향은 고정해 플레이어의 옆 이동이 의미 있게 한다. 다음 공격 사이에는 목표를 다시 보며, 해당 동작의 선행 자세를 보여준다.
- 공격/방어/거리 조절이 같은 프레임에 중복 시작되지 않게 한다. 회피 후에는 잠시 움직임을 안정시키고 공격을 다시 고른다.

### 3. DOOM — id Software

개발사 공개 원본 소스다. 무기 애니메이션과 재공격 상태의 구분, 공격과 재배치 교대만 참고한다. FPS의 피해량이나 공격속도를 태권도 액션의 기준 수치로 사용하지 않는다.

- 커밋: `a77dfb96cb91780ca334d0d4cfd86957558007e0`
- [무기 상태표 원문](https://raw.githubusercontent.com/id-Software/DOOM/a77dfb96cb91780ca334d0d4cfd86957558007e0/linuxdoom-1.10/info.c)
- [무기 제어 원문](https://raw.githubusercontent.com/id-Software/DOOM/a77dfb96cb91780ca334d0d4cfd86957558007e0/linuxdoom-1.10/p_pspr.c)
- [적 AI 원문](https://raw.githubusercontent.com/id-Software/DOOM/a77dfb96cb91780ca334d0d4cfd86957558007e0/linuxdoom-1.10/p_enemy.c)

확인한 내용:

- `info.c` 141~145행: 주먹 공격은 `S_PUNCH1`~`S_PUNCH5`의 서로 다른 지속시간을 거친다. `A_Punch`의 실제 타격과 마지막 단계의 `A_ReFire` 재공격 여부가 분리되어 있다.
- `p_pspr.c` `A_ReFire`: 입력 유지·무기 교체·생존 상태를 검사한 뒤 다음 공격을 시작한다. 입력은 애니메이션의 임의 프레임에서 공격을 새로 시작시키지 않는다.
- `p_enemy.c` `A_Chase`: `// do not attack twice in a row` 분기에서 직전 공격 플래그를 소비하고 일반 난이도에서 추적 방향을 다시 고른다. 모든 근접 공격에 적용되는 보편적인 쿨다운 규칙으로 해석하지 않는다.

KICK Back 적용 판단:

- 공격을 선행 자세 → 타격 → 회수로 구분하고, 입력을 연타해도 이미 시작한 타격의 회수 시간을 건너뛰지 않는다.
- 적은 공격 뒤 잠시 거리 조절을 하며 다음 차례를 정한다. 보스 역시 연계 종료 후 반격할 수 있는 간격을 남긴다.

## 추가로 읽은 격투게임 공개 구현

### OpenOMF — One Must Fall 2097의 커뮤니티 재구현

`omf2097/openomf` 프로젝트 자체의 공식 저장소다. **원작 Diversions Entertainment가 공개한 원본 소스라는 뜻은 아니다.** README는 “Open Source remake”임을 명시한다.

- 커밋: `2e705422088bb26c0189cef08cc8b971ab3cbc89`
- [격투 상태·판정 원문](https://raw.githubusercontent.com/omf2097/openomf/2e705422088bb26c0189cef08cc8b971ab3cbc89/src/game/objects/har.c)
- [AI 전술 원문](https://raw.githubusercontent.com/omf2097/openomf/2e705422088bb26c0189cef08cc8b971ab3cbc89/src/controller/ai_controller.c)

확인한 내용:

- `har.c` `har_is_blocking`: 다른 동작을 실행 중이거나 점프한 상태에서 일반적인 방어를 허용하지 않는다. 현재 서 있는/앉아 있는 방어 애니메이션 예외를 명시한다.
- 2352행의 주석: `executing_move = 1 prevents new moves while old one is running.` 이미 실행 중인 기술에 행동 제약을 둔다. 지정된 연계/취소 가능성까지 전부 금지한다는 뜻은 아니다.
- 2392행: 실행 중인 기술이 끝나기 전 새로운 일반 이동을 제한한다.
- `ai_controller.c`는 접근·회피·방어·반격 전술을 나누고, `ai_block_har`는 거리와 상대 공격 상태를 확인해 확률적으로 대응한다.

KICK Back 적용 판단:

- J/I 연계에는 허용된 다음 타격과 입력 예약 시점을 명시한다. 아무 키나 눌러 K의 후딜을 지우는 동작은 허용하지 않는다.
- K의 높은 단발 피해와 중단 성능에는 더 긴 준비·회수·재사용 시간을 대응시킨다.
- 적의 가드/회피는 플레이어의 키 입력을 읽고 즉시 무조건 성공하는 방식으로 구현하지 않는다. 제한된 거리·인지 지연·확률·재사용 간격을 함께 사용한다.

## 접근하지 못한 자료와 조사 범위

Sifu, Streets of Rage 4, Dead Cells, Hades의 공식 사이트/Steam/PlayStation Blog를 조회했으나 이 실행 환경의 프록시가 `Tunnel connection failed: 403 Forbidden`으로 거부했다. 검색 페이지도 실제 본문을 제공하지 않아 근거로 사용하지 않았다. 이 게임들의 플레이·공식 프레임표·개발자 인터뷰를 이번 작업에서 확인했다고 주장하지 않는다.

이번 조사는 위에서 실제로 읽은 공개 소스의 상태 전이·방어 재사용·AI 선택 구조에 한정한다. 우리 게임의 최종 피해량·공격간격·스폰 수치는 별도의 밸런스 정책과 실제 Unity 검증 결과로 판단한다.

## 기존 게임 코드 감사에서 확인한 문제

변경 전 기준:

- J는 3타 제한은 있었으나 전체가 같은 `.12 + .28`초 흐름이고, 종료 직후 다음 3타를 바로 시작할 수 있었다.
- I는 `punch || chain < 3` 조건으로 입력만 예약하면 횟수 제한 없이 이어졌다.
- `guardStarted`를 새 L 입력마다 갱신하므로 패링 창을 반복 갱신할 여지가 있었다.
- 가드·회피의 유효시간과 재사용 제한을 별도로 설계하지 않았다.
- `OnConfirmedHit`는 공격이 여러 적에게 맞으면 적마다 게이지를 가산했다. 군중에서 궁극기가 과도하게 빨리 충전될 수 있었다.
- 시각 모션의 재생시간만 조절하면 공격속도·판정·코루틴 복구가 불일치할 수 있다. 실제 타격 시점과 회수 정책을 단일 데이터로 관리해야 한다.

회귀 검사에서 함께 갱신해야 하는 기존 계약:

- `AuthoredMotionPlaytestRunner`: Q 피해 총합 30/48/60 고정값, 동일 기술을 재사용하기 전 대기시간, 각 공격의 회수 시간.
- `PunchPlaytestRunner`: 0.22초 뒤 시각 포즈 확인, 0.4초 뒤 재행동 확인, 펀치 번호 지속·3.1초 무입력 후 초기화.
- `DojaehakPlaytestRunner`: QuadPunch 4타 총피해 18/가드피해 4.5, 기존 5패턴의 정확한 순서, 고정된 패링·아머 시점, 121 피해 후 절반 체력 판정.
- `TutorialComboLesson`: 적중 간 1.3초 안에 3회 연속 성공해야 한다. 느려진 3타·실패 후 복귀 간격을 검토하고, 튜토리얼을 못 깨는 상태가 생기지 않게 한다.
- `TutorialDirector`: 궁극기 연습은 빗맞히면 게이지를 채워 재시도하게 한다. 새 궁극기 쿨다운을 넣는다면 안내와 재시도가 함께 동작해야 한다.
- `HUDPlaytestRunner`의 체력120/240은 별도 UI fixture 값이다. 실제 캠페인 밸런스가 달라져도 UI 바인딩 검사 의미는 유지한다.
