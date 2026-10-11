# 브라우저 플레이 배포

main 브랜치는 Unity 소스이며 webgl 브랜치는 사용자가 빌드한 게임 실행 파일입니다.
2026-10-11 업로드된 WebTest.data/wasm/framework.js/loader.js를 webgl에 배치했습니다.
누락된 TemplateData/style.css는 웹페이지 스타일로 보완했습니다.

최초 활성화는 저장소 소유자가 Settings → Pages에서 다음을 저장해야 합니다.
Source: Deploy from a branch / Branch: webgl / Folder: / (root).
GitHub Actions를 통한 자동 최초 활성화는 Resource not accessible by integration으로 거부됐습니다.
이후 webgl 브랜치 업데이트는 GitHub Pages의 기본 배포 기능이 처리합니다.
사이트 주소는 활성화와 배포가 완료된 뒤 Pages 설정 화면에서 확인합니다.

Unity 소스 변경 후에는 Unity가 있는 PC에서 WebGL 빌드를 새로 만들어야 합니다.
School Rule Zero → Build WebGL 또는 Unity Build Profiles → Web → Build를 사용합니다.
index.html과 Build의 data/wasm/framework.js/loader.js가 모두 있어야 합니다.
이 실행 파일들을 전달하면 같은 webgl 브랜치와 사이트 주소를 업데이트할 수 있습니다.
index.html을 파일로 직접 여는 file:// 방식 대신 HTTP(S) 서버로 실행합니다.

빌드 파일 헤더, Node WebAssembly.validate, 내부 HTTP 파일 응답을 확인했습니다.
실제 브라우저 검증은 Chromium 다운로드 도메인 접근 제한으로 미실행입니다.
공개 Pages 사이트 직접 접근도 이 클라우드의 네트워크 제한으로 확인하지 못했습니다.
현재 업로드 사실을 브라우저 플레이 성공으로 간주하지 않습니다.
