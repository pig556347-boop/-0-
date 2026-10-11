# 브라우저 플레이 배포

이 저장소의 `main`은 Unity 프로젝트입니다. 아직 실행 가능한 WebGL 빌드는 없습니다.
클라우드에는 Unity Editor와 라이선스가 없어 Windows PC에서 빌드해야 합니다.

1. Unity Hub의 Installs에서 Unity 6.4 (6000.4.10f1)의 Manage → Add modules로
   WebGL Build Support를 설치합니다. 이미 설치되어 있다면 생략합니다.
2. 최신 프로젝트를 Unity에서 엽니다. Play Mode는 종료합니다.
3. School Rule Zero → Build WebGL을 선택하고 빈 출력 폴더를 지정합니다.
4. 성공하면 출력 폴더 옆에 ZIP이 만들어지고 탐색기가 열립니다.
   이 ZIP은 프로젝트 소스 ZIP과 다릅니다. index.html과 Build 폴더를 포함합니다.
5. ZIP을 대화에 첨부하면 배포 작업을 이어갈 수 있습니다.

빌드는 GitHub Pages에서 호환되도록 압축과 멀티스레드를 비활성화합니다.
로드 용량은 커질 수 있으며 실제 브라우저 검증은 빌드 후 수행해야 합니다.

배포 파일은 `webgl` 브랜치 루트에 배치하고 main의 WebGL 배포 workflow를
해당 브랜치에도 포함합니다. 브랜치 push가 GitHub Actions 배포를 실행합니다.
첫 게시에는 GitHub Pages 활성화가 필요하며 자동 활성화가 권한 오류로 실패하면
저장소 Settings → Pages → Source에서 GitHub Actions를 선택해야 합니다.
비공개 저장소의 Pages 사용 가능 여부는 GitHub 요금제에 따라 다릅니다.

빌드 업로드, Actions 성공 및 웹 응답을 확인한 이후 플레이 URL을 안내합니다.
이후 새 빌드를 같은 브랜치에 게시하면 같은 URL에서 업데이트됩니다.
