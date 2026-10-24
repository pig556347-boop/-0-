# 폐교탈출 전용 웹 배포

## 최신 0.9 — 본관53방·운동장 기획

[폐교탈출 전용 소스 백업](https://github.com/pig556347-boop/-0-/tree/school-escape-source/SchoolEscape): `SchoolEscape/`만 담는 독립 스냅샷 브랜치이며 루트 자동 배포 워크플로가 없다. 원본 작업 브랜치의 최종 소스 커밋은 백업의 `SOURCE_CHECKPOINT.json`에 기록한다.

**[폐교탈출 웹 플레이](https://rawcdn.githack.com/pig556347-boop/-0-/7f267cbd0903ab6aeb2e0a9c469026c0c2df469b/index.html)**

고정 공개 커밋 `7f267cbd0903ab6aeb2e0a9c469026c0c2df469b`, 빌드 소스 `569fb02a8181b780c3f99a0a116dcb9f94cb6ba8`, 버전0.9/지도 revision `remaining-main-rooms-2026-10-24`. 폐교탈출 전용 `school-escape-web`만 갱신한다. 기존41방과 층 바닥/복도/계단은 바꾸지 않고 미제작12방을 꾸며 본관53개 방문에 모두 실내를 연결했다. 기존main·다른게임·Pages설정은 변경하지 않았다.

실제 로컬 Chromium2,914개/오류0, 정적78,583개/실패0, 아이템 커널25개/실패0. 최종 HTML6,481,052bytes/SHA256 `3971f6feb8b178a41eea8efdc3a684a073352140696afbff6b526fca780bd0a6`. 같은 최종 해시의 게시 전 Chromium79개도 통과했고 실제외부공개결과와 구분한다.

실제 공개 CDN/Chromium: 79개 전부 통과/실행·콘솔 오류 및 경고0. 실제 CDN HTTP200/text-html/최종해시 일치, Open the page 확인, 신규4실 실제 E→W→E와 기존 아이템·음향·탈출 검증. UTC2026-10-24T12:02:10.110Z(한국21:02). [외부 보고서](Evidence/public-web-rooms.json).

**[운동장 기획도 PNG](https://raw.githubusercontent.com/pig556347-boop/-0-/7f267cbd0903ab6aeb2e0a9c469026c0c2df469b/concepts/playground-plan-09.png)**:84×66m, 북쪽본관/남쪽교문, 중앙+좌우길/차폐2곳. 이미지와SVG/문서만 작성했으며 운동장런타임·탈출위치이전은 미구현이다. 현재1층임시교문A/B/C탈출은 유지한다.

처음 **Open the page → 폐교에 들어가기**. 독립브라우저구현이며UnityWebGL빌드가아니다. UnityC#컴파일은성공했지만Play는유효라이선스가없어exit198로미검증이다.

---

## 이전 0.8 — 배회·아이템·열쇠 탈출

**[폐교탈출 웹 플레이](https://rawcdn.githack.com/pig556347-boop/-0-/abb593029282eadabd8aee550107e371253b2fd9/index.html)**

폐교탈출 전용 `school-escape-web` 브랜치만 갱신했다. 공개 게임/워크플로 커밋 `abb593029282eadabd8aee550107e371253b2fd9`, 빌드 소스 `3c30d63b88c060150921d8328c774dd70c19f3ec`, 기능 revision `items-search-roaming-2026-10-24`. 지도 revision은 기존 `facilities-and-corridor-2026-10-23` 그대로다. 기존 main/다른 게임/Pages 설정은 변경하지 않았다.

개발 추적자 ON/OFF, 지정 랜덤 스폰·독립 배회·감각과 유한 기억, 9칸 가방+착용 1칸, E 조사·랜덤 열쇠 A/B/C·소모품·시계 자동 합성 나침반·심박 워치·1층 교문 탈출을 포함한다. 최초 외부 확인에서는 **Open the page**, 게임에서는 **폐교에 들어가기**를 누른다. `chatgpt.com/local/…` 파일 미리보기 실패는 이 고정 공개 주소로 우회한다.

최종 HTML **5,833,749 bytes**, SHA-256 `52c7529b3083d400c820f3d4a7d0eab51b7daa063a797922d5856693728eff01`. 로컬 실제 기능 검사 54개, 커널 검사 25개, 같은 최종 빌드의 게시 전 Chromium 시나리오 57개 통과. 전체 지도 2,267개는 바로 전 빌드 해시 `de73c8951272b6b6094e14727811bbfd1859cec67ceb31a854f17ec673dde3c8`에서 통과했으며, 이후 변경은 추적자 버튼과 성공 화면의 즉시 DOM 갱신뿐이다.

실제 공개 CDN/Chromium 결과: 57개 전부 통과/실행·콘솔 오류 및 경고0. 실제 CDN HTTP200/text-html/최종해시 일치, 최초 Open the page 확인, 실제 입력으로 아이템 수집·사용·착용·드롭/회수·자동합성·교문 탈출/새 판 확인. UTC2026-10-24T00:10:51.681Z(한국09:10). [외부 보고서](Evidence/public-web-items.json). 원본 HTTP200/정확한 바이트 해시와 이미지2개 HTTP200은 [별도 원본 검사](Evidence/items-public-origin.json)에 기록했다. 원본 HTML의 `text/plain` 주소는 플레이 링크로 사용하지 않는다. 외부 브라우저 검증은 `Deploy/verify-web.mjs`와 전용 브랜치의 기존 워크플로를 통해 실행하며 이전 보고서를 새 성공으로 간주하지 않는다.

이 실행물은 독립 브라우저 구현이다. Unity C# 컴파일은 성공했으나 실제 Unity Play/Unity WebGL은 라이선스 오류로 미검증이다.

---

## 이전0.7 — 당직자·스태미나·소리

**[폐교탈출 웹 플레이](https://rawcdn.githack.com/pig556347-boop/-0-/d99cee6532d9947d72a5c78a49a0e8052b5a7f67/index.html)**

게임/검증 스크립트 커밋 `d99cee6532d9947d72a5c78a49a0e8052b5a7f67`, 기능revision `caretaker-stamina-audio-2026-10-23`, 지도는 기존41방 revision을 유지합니다. 본관3층/41방과시설/거울에 추격·스태미나·독자 합성 음향을 추가했습니다. 시작은 **폐교에 들어가기**이며 첫외부확인화면은 **Open the page**입니다.

실제 외부 Chromium **43항목 전부 통과/실행·콘솔 오류0**, 한국시간2026-10-23 22:55(UTC13:55:42.073Z). HTTP200/text-html/정확한HTML해시, 실제Start/오디오RMS/음소거/Shift달리기/스태미나고갈·회복/실제E문추격과안전도착/추격음향/잡힘·재시작, 기존방송·화장실입퇴실과거울픽셀까지 확인했습니다. [외부원문](Evidence/public-web-chase.json).

HTML5,764,285bytes/SHA256 `8d10d62e4103f7087426e791cae0b4c097d98fe701bb5ab78d52345588250664`. 원본GitHub의HTML은text/plain이므로 실행링크로쓰지않고위CDN주소를사용합니다. 실제게임화면PNG와시안PNG는직접이미지주소로제공합니다. 전용브랜치만갱신했고 main은 `1268055a35266ca70e1c1302c2d5852ba537a2ad` 그대로입니다.

## 이전0.6

**[폐교탈출 웹 플레이](https://rawcdn.githack.com/pig556347-boop/-0-/45f4916f5687386b50e310149af4f5daa69c7d88/index.html)**

버전0.6:41방,손그림L복도·행정18석·1층빈교실전환·방송실·12화장실/큰실시간거울. 게임/워크플로커밋`45f4916f5687386b50e310149af4f5daa69c7d88`. 기존사이트/메인브랜치/Pages설정은유지하고동일한폐교탈출전용웹브랜치만갱신했습니다. 최초확인화면은일반`Open the page`버튼으로진행합니다.

실제 외부 Chromium 검증 완료: `2026-10-23T11:35:19.244Z`, **27항목 통과/실행·콘솔 오류0**. [실행 보고서](Evidence/public-web-facilities.json)에서 HTTP200/text-html/정확한 HTML해시,41방 구조,실제W/E 방송실·학생화장실 왕복 및 거울 반사547pixels/max235를 확인합니다. 첫 Open the page 확인도 정상 클릭했습니다.

아래는최초0.5배포의이전기록입니다.

사용자의 “웹플레이 링크를 줘야지” 요청에 따라 다운로드 파일과 별개인 공개 HTML 실행 경로를 만들었습니다. 기존 게임의 사이트/Pages 설정/배포 브랜치를 교체하지 않고 같은 저장소의 새 독립 브랜치 `school-escape-web`을 사용합니다. 이 브랜치는 부모 없는 별도 커밋으로 시작하며 폐교탈출의 정적 파일만 포함합니다.

첫 공개 커밋: `66e335440c9d20e2eeff62af8c8626629e9ebd3d`.

- 게임 파일: `index.html`, 모든 지도·글꼴·표면 이미지·게임 코드 포함.
- 빌드 정보: `build.json`, 버전/원본 커밋/HTML 해시/기존 실행 검사 수.
- 문서: `README.md`와 정적 사이트 마커 `.nojekyll`.
- 원본 공개 파일을 실제 내려받아 HTTP200, 5,286,483bytes, SHA-256 `c59b69a762640f63d9dfb081b228f1aa3cccc05efdeb11a753028b97d904c7ca` 일치를 확인했습니다.

HTML 실행은 raw.githack/rawcdn.githack의 GitHub 정적 파일 제공 서비스를 사용합니다. 처음 브라우저로 열 때 외부 콘텐츠 확인 화면이 나타나면 정상적인 `Open the page` 링크로 진행합니다. GitHub의 원본 파일 주소는 `text/plain`이므로 플레이 링크로 안내하지 않습니다.

현재 클라우드 환경의 CDN/Pages/API 직접 요청은 `CONNECT tunnel failed, response 403`이며, Cloudflare 임시 터널도 API DNS 조회 `connection refused`로 실패했습니다. 이 오류로 토큰의 실제 유효성이나 전세계 서비스 상태를 단정하지 않습니다. GitHub git 원격 읽기/새 브랜치 쓰기와 github.com/raw.githubusercontent.com 요청은 실제 성공했습니다.

기존 로컬 HTTP 실제 브라우저 검사는1,447개 통과했습니다. 공개 CDN의 외부 브라우저 검증은 별도 CI 결과를 보존하여 구분합니다. Unity WebGL 빌드나 활성화된 Unity Play의 검증 완료를 의미하지 않습니다.


## 실행 주소와 외부 검증

[폐교탈출 웹 플레이](https://rawcdn.githack.com/pig556347-boop/-0-/66e335440c9d20e2eeff62af8c8626629e9ebd3d/index.html)

전용브랜치에 검증 워크플로 `669ddf174b6929635ccfeb4e3ac9d89398f3de96`을 추가했습니다. 외부 GitHub Actions에서 위 고정 주소의 HTTP/content-type/해시와 실제 Chromium 시작·걷기·보건실 E 입퇴실을 확인합니다. CDN의 정상 확인 화면은 일반 버튼 클릭으로 진행하며 확인 쿠키를 임의 주입하지 않습니다. 결과는 전용브랜치의 `published-status.json`에 기록합니다. 자동검사 자체가 성공했다고 가정하지 않고 실제 보고서를 확인합니다.

CI원본은 `Deploy/verify-web-play.yml`, `Deploy/verify-web.mjs`, 주소/커밋은 `Deploy/publication.json`에 보존합니다. 이 프로젝트의 일반 소스 브랜치에서 자동 배포하도록 루트워크플로를 추가하지 않았습니다.


## 외부 실행 결과

실제 외부 브라우저 검증 완료: `2026-10-23T10:51:55.190Z`. GitHub Actions 실행 [39941972255](https://github.com/pig556347-boop/-0-/actions/runs/39941972255), [보존 보고서](Evidence/public-web-play.json).

- HTTP200,`text/html; charset=utf-8`,최신빌드해시일치.
- 첫방문`Open the page`정상버튼을실제로눌러진행. 최신28방로드·탐방시작·실제W걷기2.6m/s확인.
- 실제E보건실입장·현재방만표시·원래1층문앞퇴실통과.
-14개확인항목통과,실행/콘솔오류0. 로컬1,447개전체검사와구분한외부공개주소smoke검사입니다.

기존 사이트/Pages 설정과 main 브랜치는 변경하지 않았습니다. 공개게임은고정커밋주소이므로클라우드세션의로컬서버유지와무관합니다.
