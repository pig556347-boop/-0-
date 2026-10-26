# 0.12 검증

공개 고정 주소에서 실제 외부 Chromium **81개 검사 통과**, 실행·콘솔 오류0. 게시된 파일이 로컬 검증 빌드와 SHA-256까지 일치합니다.

정적82,546 / 회귀81(64시드·5,076경로) / 별관 연결38 / 실제 로컬 Chromium80 검사 통과, 실행·콘솔 오류0. 전개도71개 화면과PNG6개 정상 표시. `Evidence/annex-*` 보고서에 결과를 기록했다.

브라우저에서 E 입퇴실, 키보드 계단1↔2층 자동 왕복, 생성위치·바운스 방지, 추적자의 목격한 문 추적,3/4고음 피아노·괴물 음향 출력·정지,3열쇠 수집/교문 성공을 확인했다. 공개 주소 결과는 별도 `annex-public-runtime.json`에 저장한다.

Unity 실제 컴파일 성공, `No valid Unity Editor license found.`로 Play 실패. Unity 음향 이식과 WebGL 검증은 남아 있다.

---

# 0.11 검증 결과

공개 고정 주소에서 실제 외부 Chromium **55개 검사 통과**, 실행·콘솔 오류0. 게시된 HTML이 로컬 검증 파일과 SHA-256까지 일치함을 확인했습니다.

- 정적 지도/충돌 간격: **79,945개 통과**.
- 실제 런타임 규칙: **81개 통과**, 64개 시드, 12개 시드에서 총4,332개 실내 탐방 지점 경로.
- 로컬 실제 Chromium: **54개 통과**, 콘솔·실행 오류0. 난이도4종/실제 잡힘/재도전/드롭 회수/가이드북/사물함8칸/직접 클릭 수집/새판 재배치/실내외 피아노 출력/교문 성공 확인.
- 전개도: **58개 화면·PNG5개** 실제 브라우저 표시 정상.
- Unity C# 컴파일 성공. 실제 Editor Play 시 `No valid Unity Editor license found.`로 실패. Unity Play/WebGL과 새로운 웹 시스템의 네이티브 동등성은 미검증.

실행물 SHA-256: `6195c966e832c72f62d9118d1a003a157a37c61233d4b032d7037941f50f8192`. 상세 기록은 Evidence/upgrade-*.json 및 upgrade-unity.log.

## 0.11 업데이트 — 2026-10-26

운동장 야외 소품과 본관·서관·별관 외관, 실내/실외 합성 피아노, 난이도 4종과 영구 가이드북, 개별 사물함 칸/클릭 수집, 같은 층 안 교실·교무실 재배치, 교실 상태·책상 변형, 화장실 축소를 반영했습니다. [세부 규칙과 기본값](UPGRADE_011.md).

| 난이도 | 목숨 | 스태미나 | 열쇠 단서 | 잡혔을 때 |
|---|---:|---:|---|---|
| 이지 | 5 | 200% | 방·조사물·칸 | 소지품 보존 |
| 노말 | 5 | 150% | 분위기·용도 | 사망 지점에 드롭 |
| 하드 | 3 | 100% | 없음 | 같은 공간 8m 안 무작위 드롭 |
| 익스트림 | 1 | 100% | 없음 | 게임 종료 |

가이드북은 가방 전용 버튼으로 항상 보존하며 9칸을 소비하지 않습니다. E/F는 수납창을 열고 아이템을 클릭해야 획득합니다. 새 판 시작 시 배치를 새로 생성합니다. 남은 목숨으로 계속하기는 같은 판을 유지합니다.

이번 새 시스템은 웹 버전에 적용했습니다. Unity에는 공통 지도와 야외 조사 종류를 반영했지만 가이드북·난이도·클릭 수집·매판 변형·피아노의 네이티브 이식은 남아 있습니다. Unity Play/WebGL은 라이선스 오류로 미검증입니다.

# 검증 기록

정적 데이터, 독립 브라우저 검토판, 실제 Unity Play, Unity WebGL은 서로 다른 검증 대상입니다.

## 현재0.10 — 독립 운동장·실제 교문 탈출

기록일:2026-10-25(한국시간). 본관53실을 유지하고69×55셀(내부 약83.75×66.25m)의 `playground` 독립 공간과 본관1층 현관의 양방향 E문을 추가했습니다. A/B/C 최종 사용 지점은 운동장 남쪽 교문입니다. 매판 열쇠는 본관 최소1개·운동장 최소1개·나머지 무작위이며 초기13개 아이템과9+1칸 수납은 유지합니다.

최종 HTML은6,659,374bytes, SHA-256 `a07f0080b02cbabceb58d4c87550434701fcc69f23983ff173bdcfc2c2b179bd`입니다. 새 공개 주소: [웹 플레이0.10](https://rawcdn.githack.com/pig556347-boop/-0-/6e40868697e73f470d0163ce00a14889ef00cb67/index.html). 아래 실제 결과와 진행 중인 검사를 구분하며0.9 결과를0.10의 새 검사로 세지 않습니다.

| 대상 | 이번 결과 | 증거·범위 |
|---|---|---|
| Astra 코어 검토 / Node 실행 |47개 통과·실패0. 실제 작성된 운동장 기하와 생산 커널/추적/아이템 코드를 사용 |[검토 JSON](Evidence/playground-astra-core-review.json). 해당 보고서의 소스 해시 기준이며 이후 울타리·시각 수정의 최종 빌드 결과와 별도 |
| 최종 정적 지도·아이템 검사 |정적79,795개 및 아이템 커널29개 통과·실패0 |[지도](Evidence/playground-map-check.json), [아이템](Evidence/playground-items-kernel.json).128시드/1,664배치/36경로 |
| 실제 로컬 Chromium 전체 회귀 |2,913개 통과·실패/실행오류0 |[전체 실행](Evidence/playground-full-browser-runtime.json), [실행물 해시](Evidence/playground-full-artifact.json).53방·자동계단1→2→3→2→1·거울·기존 동선 |
| 실제 로컬 Chromium 운동장 |65개 통과·실패/실행오류0 |[운동장 실행](Evidence/playground-browser-runtime.json).실제E왕복·보행/충돌·조사/드롭·나침반·양방향추적·남쪽교문탈출·녹음 신호 |
| 공개 검증기의 로컬 사전 실행 |91개 통과·실행/콘솔오류0 |[로컬 검사](Evidence/playground-public-local-harness.json).공개주소 결과와 별도 |
| 새 공개 주소 외부 Chromium |91개 통과·실행/콘솔오류0 |[외부 보고서](Evidence/public-web-playground.json).HTTP200/text-html·최종해시일치·실제E왕복/수집/교문성공/재시작 |
| 실제 Unity Editor C# |컴파일 성공,6항목 갱신. `Tundra build success` |[이번 Unity 실행 JSON](Evidence/playground-unity-attempt.json), `TestResults/unity-playground-release.log` |
| Unity Play/물리/화면/네이티브 음향/Unity WebGL |미실행·미검증. Play 진입은 라이선스 오류exit198 |실제 오류: `No valid Unity Editor license found. Please activate your license.` |

이번 Unity 실행 증거 UTC는 `2026-10-24T23:53:25.455717+00:00`입니다. 에디터 컴파일 성공을 실제 Unity 플레이나 Unity WebGL 실행 성공으로 확대하지 않습니다. Node 검토 역시 브라우저 입력·렌더링·오디오 검증을 대신하지 않습니다.

네이티브 러너에는 운동장 흙 텍스처 임포트,13개 아이템/9칸 가방·A/B/C 구역 배분, 닫힌 현관 E왕복·정확한 도착 좌표·단일 활성 공간, 운동장 경계/탐방 지점, 옛 현관 탈출 불가·열쇠 부족·남쪽 교문 범위 검사를 추가했습니다. 이번 Play 중단으로 해당 Unity 런타임 검사는 실행 완료되지 않았습니다.

### 0.10 브라우저 검사 재실행

프로젝트 루트 `SchoolEscape/`에서 최종 HTML을 생성하고 별도 터미널로 HTTP 서버를 실행합니다. 아래 명령은 재실행 방법이며 통과 보고가 아닙니다. `ESCAPE_EXPECT_SHA256`에는 실제 검사 파일의 최종 해시를 지정합니다.

```bash
python3 Tools/check_maps.py
node Tools/items_check.mjs
node Tools/build_web.cjs /출력/SchoolEscape-play.html
python3 -m http.server 8793 --bind 127.0.0.1 --directory /출력
```

```bash
ESCAPE_URL='http://127.0.0.1:8793/SchoolEscape-play.html?qa' ESCAPE_EXPECT_SELF_CONTAINED=1 node Tools/browser_check.cjs
ESCAPE_URL='http://127.0.0.1:8793/SchoolEscape-play.html?qa' node Tools/playground_browser_check.cjs
```

`ESCAPE_RESULTS_DIR`로 증거 출력 위치를 분리합니다. 실제 실행 결과 파일·UTC·HTML 해시가 있는 검사만 완료로 기록합니다.

## 이전0.9 — 본관 나머지12개 방·운동장 기획

이하0.9 표와 해시는 과거 본관 확장판의 기록입니다. 당시 운동장 기획만 존재했고1층 임시 교문에서 탈출했습니다. 현재0.10 상태는 위 표를 기준으로 봅니다.

지도 revision `remaining-main-rooms-2026-10-24`, featureRevision `remaining-main-rooms-playground-plan-2026-10-24`. 새 방12개/85구역을 추가해53개 방이 되었으며 운동장은 기획 문서/SVG만 작성했습니다. 실외 실행 맵이나 교문 이전을 검증했다고 기록하지 않습니다.

| 대상 | 결과 | 증거 |
|---|---|---|
| 정적 지도/가구/연결 |78,583개 통과/실패0. 모든53개 방의 문 연결, 구역 경로·충돌 여유 검사 |[정적JSON](Evidence/rooms-map-check.json) |
| 기존 지도 보존 |기존41개 roomMap 깊은 비교 불변, 층 격자·계단·봉쇄부 불변 |[비교JSON](Evidence/rooms-map-delta.json) |
| 아이템 커널 |25개 통과/실패0.128시드·1,664배치·36실제 경로 |[커널JSON](Evidence/rooms-items-kernel.json) |
| 새 방 조사/추적자 경로 |12실의 접근 가능한 조사점102개, 추적자 반지름 경로85개와 문 진입·퇴실 안전지점 확인 |[통합JSON](Evidence/rooms-search-and-pursuer-paths.json) |
| 실제 전체 Chromium |2,914개 통과/실패·실행 오류0.53방 전체 탐방·충돌·139공간분리 검사, 신규12실 실제 E→W→E, 계단1→2→3→2→1,12거울/책 안정성. UTC2026-10-24T11:57:42.475Z(한국20:57) |[실행JSON](Evidence/rooms-browser-runtime.json) |
| 새 공개 주소 외부 Chromium |79개 전부 통과/실행·콘솔 오류 및 경고0. 실제 CDN HTTP200/text-html/최종해시 일치, Open the page 확인, 신규4실 실제 E→W→E와 기존 아이템·음향·탈출 검증. UTC2026-10-24T12:02:10.110Z(한국21:02) |[배포 기록](DEPLOYMENT.md) |
| 실제 Unity Editor |변경 C#3items 컴파일 성공. 첫 config 생성 오류는 XDG_CONFIG_HOME/CACHE_HOME을 workspace에 지정해 복구 |[실제 발췌](Evidence/rooms-unity-attempt.txt) |
| Unity Play/물리/화면/네이티브음향/UnityWebGL |미실행. Play는 유효한 에디터 라이선스가 없어exit198 |브라우저 결과로 대체하지 않음 |

검사 대상 HTML **6,481,052bytes**, SHA-256 `3971f6feb8b178a41eea8efdc3a684a073352140696afbff6b526fca780bd0a6`. 정적 검사 수는 기하 부품 필드 검사를 포함하며 같은 횟수의 실제 플레이를 뜻하지 않습니다.

Unity 첫 실행의 실제 오류는 `CreateDirectory '/home/agent/.config/unity3d' failed`였습니다. `XDG_CONFIG_HOME`과 `XDG_CACHE_HOME`을 `/workspace/escape-tools/unity-*`에 지정한 재실행에서 설정과 컴파일이 진행됐고, 최종 Play만 라이선스 오류로 중단됐습니다. 브라우저 첫 시도는 기존8797서버의 `ERR_EMPTY_RESPONSE`로0개 검사 후 종료돼 `pre-server-restart`로 보존했습니다. 새8801서버의 HTTP200을 확인한 뒤 같은 빌드를 다시 실행했으며 초기 실패를 통과로 계산하지 않습니다.

## 이전0.8 — 랜덤 아이템·조사·탈출·배회

2026-10-24 사용자 승인 기본안에 따라9낱개 가방+착용1칸, 시계 세 부품 자동 합성, 초코바100% 회복, 열쇠 A/B/C 전부 사용을 검사했습니다. 브라우저 단일HTML과 실제 Unity 실행은 구분합니다.

| 대상 | 결과 | 증거 |
|---|---|---|
| 실제 로컬 Chromium 기능 |54개 통과/실패·실행 오류0. 실제E/F 조사·줍기, I가방·사용/착용/드롭, 합성·나침반 소멸, 개발ON/OFF·새 판, 교문 성공 포함 |[기능JSON](Evidence/items-browser-runtime.json) |
| 아이템/경로 커널 |25개 통과/실패0.128시드·1,664개 배치·36개 실제 경로, 중복/가득찬 가방/드롭UID/탈출 조건 검사 |[커널JSON](Evidence/items-kernel.json) |
| 실제 로컬 전체맵 회귀 |2,267개 통과/실패·오류0.41방·294구역·103공간분리·12거울·자동계단 왕복·책 안정성 |[전체JSON](Evidence/items-full-browser-runtime.json) |
| 실제 오디오 |기능검사에서 Start로AudioContext해제, 음소거, 착용 워치 원근BPM·실제 신호 검사 포함 |위 기능JSON |
| Unity Editor C# |실제 에디터의7items 컴파일 성공, C#오류·TagManager파싱오류 없음 |[실제 발췌](Evidence/items-unity-attempt.txt) |
| Unity Play/물리/화면/네이티브음향/UnityWebGL |미실행. Play는 라이선스 오류exit198, 헤드리스FMOD는nosound |브라우저 통과로 대체하지 않음 |
| 공개 검증 도구의 로컬 실행 |57개 통과/런타임·콘솔오류0. 최종해시에서실제조작·오디오·아이템·탈출 검증. 외부CDN실행과구분 |[배포 기록](DEPLOYMENT.md) |
| 새 공개 주소 외부 Chromium |57개 전부 통과/실행·콘솔 오류 및 경고0. 실제 CDN HTTP200/text-html/최종해시 일치, 최초 Open the page 확인, 실제 입력으로 아이템 수집·사용·착용·드롭/회수·자동합성·교문 탈출/새 판 확인. UTC2026-10-24T00:10:51.681Z(한국09:10) |[배포 기록](DEPLOYMENT.md) |

최종 기능검사 HTML은 **5,833,749bytes**, SHA-256 `52c7529b3083d400c820f3d4a7d0eab51b7daa063a797922d5856693728eff01`, 실행 UTC `2026-10-24T00:03:59.855Z`입니다. 전체맵2,267회귀는 바로 이전 HTML **5,833,463bytes**, SHA-256 `de73c8951272b6b6094e14727811bbfd1859cec67ceb31a854f17ec673dde3c8`에서 완료했습니다. 이후 변경은 추적자 버튼의 aria/text와 탈출 성공 모달을 클릭 시 즉시 동기화한 UI수정입니다. 전체맵 회귀를 최종 해시에서 다시 실행했다고 기록하지 않습니다.

첫 기능 실행은51통과/2실패/예외0이었습니다. 추적자 OFF와 탈출 성공의 내부 상태는 바뀌었지만 표시가 다음 프레임을 기다려 검사와 즉시 피드백이 어긋났습니다. 두 표시를 이벤트 처리 중 동기화한 후 최종54검사를 통과했습니다. 커널 첫 실행의24통과/1실패는 검사에 남아 있던 옛 교문좌표(24,5) 때문이었으며 실제 `escapeGate`좌표(24,2)를 사용하도록 검사만 고쳐25개가 통과했습니다. 실패 원본은 `TestResults/ItemsRelease/pre-*`에 보존하고 통과로 세지 않았습니다.

## 이전0.7 — 당직자/스태미나/소리

| 대상 | 결과 | 증거 |
|---|---|---|
| 실제 로컬 Chromium 전체맵 회귀 |2,267개 통과/실패·오류0,41방/294구역·103공간분리·12거울·1→2→3→2→1자동계단/책안정성 |[전체JSON](Evidence/chase-full-browser-runtime.json) |
| 실제 로컬 Chromium 추격 |55개 통과/실패·실행 오류0, HTML외자산요청0 |[실행JSON](Evidence/chase-browser-runtime.json) |
| 원본 모델·조작 |104메시/중립높이2.405m, 기본시점·확대확인, 실제W/대각/Shift 2.6/2.6/4.7m/s |위JSON·[실제추격](Evidence/chase-live-corridor.png) |
| 스태미나 |5초소모/지침후25%해제/1.5초지연회복/벽·제자리미소모/실제HUD |위JSON·[고갈화면](Evidence/chase-stamina-exhausted.png) |
| 실제 문·층 추적 |E입퇴실후문추격/지연안전도착/1→2→3→2→1계단따라옴/루프없음 |위JSON·[실내추격](Evidence/chase-room-arrival.png) |
| 오디오 |실제Start로AudioContext unlock/비영신호/RMS·음소거·문·계단·추격·재시작·정지 |위JSON |
| 독립 음향 실행 |19항목 통과/오류0, 단발음성수제한·이벤트중복방지·원격문음향조건 |[음향JSON](Evidence/chase-audio-isolated.json) |
| 커널/경로 |16항목 통과/실패0,44공간7,644이동프레임·41문123접근/착지·정확모서리회귀·시야차단/추적종료 |[커널JSON](Evidence/chase-kernel.json) |
| 추격자 실제 거울 반사 |추가9개통과/오류0, 당직자만표시차이847pixels/max244·동일장면복구0pixels·인물/카메라정지중적이동518pixels/캐시4→5 |[반사JSON](Evidence/chase-chaser-mirror.json) |
| 실제 공개 Chromium |43항목 전부 통과/실행·콘솔오류0, 최신해시/실제조작·오디오출력·문추격·잡힘/재시작·거울 |[외부JSON](Evidence/public-web-chase.json) |
| Unity Editor |새C#7items빌드성공. 빈레이어파싱오류수정후재실행/파싱오류해소. Play는라이선스오류exit198 |[실제발췌](Evidence/chase-unity-attempt.txt) |
| Unity Play/물리/네이티브음향/UnityWebGL |미실행. 헤드리스FMOD는nosound장치로초기화 |브라우저결과로대체하지않음 |

최종HTML5,764,285bytes/SHA256 `8d10d62e4103f7087426e791cae0b4c097d98fe701bb5ab78d52345588250664`. 로컬추격 한국22:50(UTC13:50:39.486Z), 공개 한국22:55(UTC13:55:42.073Z), 전체회귀는 한국22:58(UTC13:58:53.435Z). 모두2026-10-23. 지도필드검사수와실제프레임수/항목수를구분합니다.

추격검사의초기숨겨진모달클릭오류와100ms고정대기로재개프레임을일찍읽은실패는TestResults/ChaseRelease/pre-*에보존했습니다. 실제Esc/재개버튼과관찰가능한프레임을기다려동일조건으로통과했으며제품일시정지를비활성화하지않았습니다. 기존맵전체검사는전용QA에서만당직자를명시적으로끄고경로를격리하며추격의기본활성상태는별도55개검사에서확인합니다.

## 이전0.6 — 복도/행정18석/빈교실전환/방송실/화장실과거울

Revision `facilities-and-corridor-2026-10-23`,총41방/294구역입니다.

| 대상 | 결과 | 증거 |
|---|---|---|
| 정적지도/가구/복도폭/연속경로 |69,252개통과,실패0 |[정적JSON](Evidence/facilities-map-check.json) |
| 실제Chromium전체실행 |2,267개통과,실패/예외/리소스오류0 |[실행JSON](Evidence/facilities-browser-runtime.json) |
| 41방/신규13시설 |E입퇴실·정확한복귀·전294구역왕복/가구벽문충돌 |위JSON passed/facilityRoomVisits |
| 실제키입력 |보행/대각2.6m/s·달리기4.7m/s,기존4종+방송/학생/교직원화장실E/W |위JSON keyboard/supportKeyboard |
| 손그림L복도 |폭4셀/연속반경여유,A→남→여→북연결부→복귀,문가시성 |위JSON corridorRoutes와정적검사 |
| 실시간큰거울12개 |모든화장실현재공간반사/512²RT/캐릭터표면안투영 |위JSON mirrorChecks |
| 실제반사픽셀 |직원523pixels/max232,학생553pixels/max233. 캐릭터메시만비교,광원유지 |위JSON mirrorPixelChecks |
| 카메라변경 |투영행렬변경때거울갱신검사통과 |위JSON passed |
| 공간분리 |현재공간루트/가구/안내도103회,입장전내부미노출 |위JSON spaceChecks |
| 기존동선 |1→2→3→2→1자동계단·안전도착/반복방지,음악구역/긴통로왕복,2F책정지프레임안정성 |위JSON route/passed |
| 실제Unity |C#빌드성공7items,새거울셰이더임포트. Play는라이선스오류exit198 |[발췌](Evidence/facilities-unity-attempt.txt) |
| Unity Play/물리/반사화면/UnityWebGL |미실행 |독립웹실행으로대체하지않음 |

최종실행UTC `2026-10-23T11:31:42.261Z`,HTML5,585,542bytes/SHA-256`10c227a82c1f11b9316d301d24447aef48d3396a7f07d499a22809cb5416c887`. HTML문서1건외자산요청0. 재생성바이트일치확인. 정적검사수에는22,422기하부품필드검사도포함됩니다.

첫검사에서는동기walkTo중추적카메라프레임갱신이없어화면밖거울검사26개가실패했습니다. 실패를보존하고정상추적목표에카메라를정착시켜같은반사/픽셀검사를다시실행했습니다. 실제플레이접근화면과최종12개거울검사가모두통과했습니다.

[실제배치](FACILITIES.md),[학생거울근접화면](Evidence/facilities-browser-interior-studentRestroom.png),[교직원거울근접화면](Evidence/facilities-browser-interior-staffRestroom.png). 공개주소의외부실행27항목도통과했습니다: HTTP/content-type/해시/41방구조,실제게임시작·W걷기·방송실/학생화장실 E왕복,거울실제547pixels/max235. UTC2026-10-23T11:35:19.244Z. [외부보고서](Evidence/public-web-facilities.json),[배포기록](DEPLOYMENT.md).

## 이전 결과 — 보건실·행정실·교무실·컴퓨터실

Revision `support-room-interiors-2026-10-23`, 총28개 독립 방/142개 구역입니다. 사용자 승인 시안01의4종을 기존 문에 연결했으며 교무실은2층과3층 각각 별도 맵입니다.

| 대상 | 결과 | 증거 |
|---|---|---|
| 정적 지도·가구·연속 구간 경로 |64,776개 통과, 실패0 |[정적 JSON](Evidence/support-rooms-map-check.json) |
| 실제 Chromium 런타임 |1,447개 통과, 실패/예외/리소스 오류0 |[실행 JSON](Evidence/support-rooms-browser-runtime.json) |
| 신규5방 |문앞E→독립방→전체구역왕복→원래층문앞복귀,2/3층교무실분리 |위 JSON supportRoomVisits/passed |
| 실제 키 입력 |신규4종에서 DOM E입장·W보행·E퇴실,걷기/대각2.6m/s·달리기4.7m/s |위 JSON supportKeyboard/keyboard |
| 가구/통로 |4병상·행정6석·교무각16석·학생PC24석,PC중앙통로연속왕복·가구/벽/닫힌문충돌 |정적/실행 JSON |
| 공간 분리 |현재공간 루트/가구/안내도64회 확인. 입장전 내부 미노출 |위 JSON spaceChecks |
| 기존 동선 |1→2→3→2→1자동계단·안전도착·재전환방지,음악실구역·서관/별관통로왕복·책장정지프레임 안정성 통과 |위 JSON route/passed |
| 요청 |HTML문서1건만,외부자산요청0 |위 JSON requests |
| 실제 Unity |최종지도/글꼴임포트성공,빌드시스템확인(업데이트C#항목0),Play는라이선스오류exit198 |[에디터 발췌](Evidence/support-rooms-unity-attempt.txt) |
| Unity Play/시각/Unity WebGL |미실행 |독립웹결과를Unity실행으로간주하지않음 |

실행 UTC: `2026-10-23T10:30:29.235Z`. 단일HTML `SchoolEscape-support-rooms.html`,5,286,483bytes,SHA-256 `c59b69a762640f63d9dfb081b228f1aa3cccc05efdeb11a753028b97d904c7ca`. 결과JSON에도 빌드해시를 보존했습니다. 정적검사 수에는21,080개기하부품의필드검사가포함되어 실제21,080회플레이했다는뜻이아닙니다.

새4종의 정상 플레이 화면을 직접 확인했습니다: [보건실](Evidence/support-rooms-browser-interior-infirmary.png), [행정실](Evidence/support-rooms-browser-interior-administration.png), [교무실](Evidence/support-rooms-browser-interior-staff.png), [컴퓨터실](Evidence/support-rooms-browser-interior-computer.png). 창문·가구와 인물을 식별할 수 있으며 구조물은 임시모델입니다. 전체배치 검토용 화면은 QA카메라를 멀리 둔 별도 캡처이며 실제 게임 카메라 설정은 변경하지 않습니다.

## 이전 결과 — 큰 사물함·음악실 구조·실내 보강·긴 연결 통로

이전 revision은 `dense-interiors-and-links-2026-10-23`입니다. 기존 독립 방/E입퇴실/자동계단에 더해, 큰4열×2행 사물함 모듈4개, 비정형 음악실 연결 통로, 실내 구조물, 서관/별관 방향의 긴 통로를 구현했습니다.

| 대상 | 결과 | 증거 |
|---|---|---|
| 지도·가구·연결 정적 검사 | 56,368개 통과, 실패0 | [정적 JSON](Evidence/dense-map-check.json) |
| 실제 Chromium 런타임 | 1,169개 통과, 실패/예외/리소스 오류0 | [실행 JSON](Evidence/dense-browser-runtime.json) |
| 실제 키보드 | 걷기·대각선2.6m/s, 달리기4.7m/s, 실제E입퇴실/반복 방지 통과 | 위 JSON keyboard/passed |
| 사물함 |4열×2행×4모듈, 칸0.60×0.46m, 후면 정렬/중앙 출구 동선 검사 통과 | 정적 검사와 [후면 화면](Evidence/dense-browser-rear-lockers.png) |
| 음악실 | 메인→꺾인 통로→보컬룸, 통로 중간 악기창고. 전 구역 실제 이동/복귀, 같은 공간 유지 | [통로 화면](Evidence/dense-browser-music-suite.png) |
| 서관/별관 연결 | 각각37.5m/38.75m 실제 걷기·복귀, 끝 봉쇄 충돌 통과 | 실행 JSON의wingPassages와passed |
| 공간 분리 | 49회 현재 공간의루트·가구·안내도만 표시 확인 | 위 JSON spaceChecks |
| 계단 |1→2→3→2→1 자동 왕복·연속 전환 방지 통과 | 위 JSON route/passed |
| 책 안정성 | 가구 추가 후 책/책장 면 겹침 검사와 정지WebGL화면 연속 렌더 바이트 일치 | 위 JSON passed 및정적 검사 |
| 실제 Unity | 새 지도/글꼴 임포트 및C#빌드 성공. Play는라이선스 오류exit198로 미실행 | [에디터 발췌](Evidence/dense-unity-attempt.txt) |
| Unity 화면/Unity WebGL | 미실행 | 독립 웹 검증으로 대체하지 않음 |

정적 검사에는 18,322개 가구 구성 부품의 필드 검사도 포함되어 있으며 그 횟수만큼 실제 플레이 동선을 실행했다는 뜻이 아닙니다. 실제 이동 검사는 브라우저 결과로 구분합니다. 새 모듈은 정적가구이며조사/아이템/괴이/전투 기능을 추가하지 않았습니다.

## 직접 확인한 화면

[교실](Evidence/dense-browser-interior-classroom.png), [사물함](Evidence/dense-browser-rear-lockers.png), [음악실](Evidence/dense-browser-interior-music.png), [음악실 통로](Evidence/dense-browser-music-suite.png), [미술실](Evidence/dense-browser-interior-art.png), [도서실](Evidence/dense-browser-interior-library.png), [서관 통로](Evidence/dense-browser-passage-west-wing-link.png), [별관 통로](Evidence/dense-browser-passage-annex-link.png)를 확인했습니다. 중앙의 빈 공간도 전시/작업 가구로 보강한 뒤 재검사했습니다. 캐릭터·통로·출입구는 식별 가능하고, 방 밖에서는 내부가 보이지 않습니다. 아트는 임시 기하 모델입니다.

음악실은 확인 가능한 텍스트 백업의 관계와 기본1안 방향을 적용했습니다. 이미지 포함DOCX는 재시도해도32MiB전송제한으로 미확인이라 원본이미지의 정확한 배치·치수 복제를 주장하지 않습니다. 연결 통로는 현재 채팅의 손그림을 참고했습니다.

## 이전0.9 검사의 재실행 기록

프로젝트 루트 `SchoolEscape/`에서:

```bash
python3 Tools/author_maps.py
python3 Tools/check_maps.py
node Tools/build_web.cjs /출력/SchoolEscape-play.html
python3 -m http.server 8793 --bind 127.0.0.1 --directory /출력
# 별도 터미널에서
node Tools/items_check.mjs
ESCAPE_URL='http://127.0.0.1:8793/SchoolEscape-play.html?qa' ESCAPE_EXPECT_SHA256=3971f6feb8b178a41eea8efdc3a684a073352140696afbff6b526fca780bd0a6 node Tools/items_browser_check.cjs
# 기존 전체 지도 회귀는 별도로 실행
ESCAPE_URL='http://127.0.0.1:8793/SchoolEscape-play.html?qa' ESCAPE_EXPECT_SELF_CONTAINED=1 node Tools/browser_check.cjs
```

Playwright 기본 설치 경로는 `/workspace/escape-tools/node_modules/playwright`이며 다른 머신에서는 `PLAYWRIGHT_MODULE`로 지정합니다. `ESCAPE_RESULTS_DIR`로 결과 경로를 정할 수 있습니다. 파일의 해시와 실제 실행 UTC는 증거 JSON에 보존했습니다. 현재 HTTP 실행은 HTML 문서 1건 외 추가 요청이 없습니다.

관리형 Chromium에서 `file://` 직접 탐색은 이전 시도에서 `ERR_BLOCKED_BY_ADMINISTRATOR`로 차단됐습니다. 정책을 우회하지 않았으며, 동일 단일 파일의 HTTP 실행 검증과 직접 파일 열기 미실행을 구분합니다. 이후 공개 실행 주소와 외부 브라우저 검증을 추가했습니다. [배포 기록](DEPLOYMENT.md)을 참조하세요.

## Unity 실행 방법과 이전 실행 기록

실제 Unity 6000.0.62f1 에디터로 이번 변경의 임포트와 C# 컴파일까지 수행했습니다. `Tundra build success` 후 `No valid Unity Editor license found. Please activate your license.`로 exit 198 종료했습니다. Unity 런타임 결과는 생성되지 않았습니다. 라이선스 값을 채팅으로 전달하지 말고 활성화된 Unity Hub 환경에서 이 프로젝트만 여세요.

```bash
"$UNITY_EDITOR" -batchmode -projectPath "$PWD" \
  -executeMethod SchoolEscape.EditorTools.Validate -escapeValidate \
  -logFile "$PWD/TestResults/unity-editor.log"
```

검증 명령에 `-quit`를 추가하지 않습니다. 러너가 실제 CharacterController로 이동·방 출입·구역 왕복·가구/벽/문·자동 계단을 검사하고 완료 후 종료합니다. `TestResults/unity-runtime.json`의 현재 UTC, 실제 검사 수, 실패/예외를 확인해야 합니다. 파일 없음이나 0개는 통과가 아닙니다.

Unity 화면의 조명/보행/가구 가독성도 확인해야 합니다. 낮게 표시한 벽은 물리 높이를 유지하고, 가구 충돌은 바닥부터 확보해 낮은 이젤 발판을 밟고 통과하는 일을 방지했습니다. 이 물리 동작의 실제 Unity 검증은 아직 미실행입니다.

Unity WebGL Build Support와 활성화된 에디터를 확보한 뒤:

```bash
"$UNITY_EDITOR" -batchmode -quit -projectPath "$PWD" \
  -executeMethod SchoolEscape.EditorTools.BuildWebGL \
  -logFile "$PWD/TestResults/unity-webgl.log"
python3 -m http.server 8792 --directory Builds/WebGL
```

생성된 wasm/data와 실제 브라우저 조작을 별도로 확인해야 합니다. 독립 브라우저 검토판을 Unity WebGL 결과로 간주하지 않습니다. 기존 배포를 변경하지 않습니다.

## 이전 단계 증거

외곽 전용 단계의 `backup-*`, `interiors-*`, `single-html-runtime.json`, 초기 `browser-runtime.json`은 과거 기록입니다. 0.9 결과는 `rooms-*` 증거에 보존하며 `dense-*`와 `room-portals-*`는 이전 단계입니다. 미열람 DOCX의 이미지와 실제 평면도 일치 여부, 최종 아트 교체는 남은 작업입니다.


## 이전0.5~0.7 공개 웹 주소의 실제 외부 실행

[실행 보고서](Evidence/public-web-play.json):2026-10-23T10:51:55.190Z,14개항목통과/실행·콘솔오류0. 공개CDN HTTP200/text-html/해시일치,처음확인화면정상버튼,최신28방로드,실제시작/W보행/E보건실입퇴실및공간분리확인. 로컬전체1,447개검사를외부14개로대체하거나UnityWebGL검증으로간주하지않습니다.

최종같은소스에서재생성한HTML이공개/검사파일과바이트단위일치함을확인했습니다. 첫전체검사의실제W계단대기timeout은진행실패로보존했고, 재실행에서는실제입력전에포커스를복귀시키고정지화면이있으면공개재개버튼으로재개했습니다. 계단검사조건은유지했으며전체2,267개가통과했습니다. 최초timeout상태가보존되지않아구체적인당시원인을단정하지않습니다.

2026-10-24 한국01:50(UTC2026-10-23T16:50:37.337Z) 재개후동일최종HTML로추격자실제반사추가검사9개를통과했습니다. 게임수정이나시각확대없이실제512²타깃을읽었으며플레이어/카메라는정지하고당직자만이동해캐시갱신을확인했습니다.
