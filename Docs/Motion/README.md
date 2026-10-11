# 모션 수정·검증 기록

선택 기본값: 1번. 팔다리를 따로 생성하지 않고 제공된 전체 모델을 유지한다. 주인공은 무릎을 접었다 펴는 앞차기, 덩치는 팔꿈치를 접었다 펴는 펀치로 기존 판정 시간에 연결한다. 기존 공격력, 이동 속도, 공격 판정, 맵은 유지한다. 앉기 키는 C를 유지한다.

첫 방식: 신체 영역별 가중치 재분배. 실제 자세 렌더에서 팔의 경계가 찢어지고 메시 변형이 최대 60배 발생하여 폐기했다.

자동 대안: 18mm 복셀로 만든 폐쇄형 임시 메시에서 Blender bone heat 가중치를 계산하고 원본 UV 메시로 부드럽게 전달했다. 원본 텍스처와 외형은 유지하고 임시 메시는 내보내지 않는다. 19개 뼈, 최대 4개 가중치를 사용한다. 무릎·팔꿈치·발목·몸통을 별도로 구동하고, 앉을 때 메시를 축소하지 않는다. 2관절 다리 IK로 발 접지를 유지한다. 공격은 준비→관절 접기→타격→회수 순서이며 StrikeStarted 이벤트가 타격 시점을 결정한다. 모델의 루트 이동은 기존 게임 물리가 담당한다.

설명해: 오프닝 종료부터 무한 체력. 덩치: 기본공격 과제부터 마지막 궁극기 과제 완료 전까지 무한 체력. 무한 체력도 피격/가드/패링/콤보/궁극기 적중 이벤트는 발생한다. 오프닝 명치만과 튜토리얼 완료 이후 실전 적은 유한 체력이다. HUD는 보호 상태에서 ∞ / ∞를 표시한다.

검증 자료: `skin-audit.json`은 Blender 메시의 실제 변형과 바닥 높이를 기록한다. Player/Deongchi 자세 PNG는 Blender 검증 렌더이며 인게임 사진이 아니다. 실제 웹 실행 사진은 배포 사이트 `/verification/motion-walk.png`, `/verification/motion-crouch.png`, `/verification/motion-kick.png`, `/verification/production-hud.png`에 게시한다. Unity 테스트 장면은 출시 장면과 분리한다.

## 재현

Blender 4.3.2에서 `SourceArt/Characters/Player.blend`, `Deongchi.blend`를 열면 리그·UV·텍스처가 포함된 편집 가능한 모델을 볼 수 있다.

```sh
blender -b -t 2 -P Tools/Motion/verify_poses.py
blender -b -t 2 -P Tools/Motion/rebuild_rigs.py
```

첫 명령은 자세 렌더와 변형 검증, 둘째 명령은 가중치를 재계산하고 Unity Resources의 FBX와 텍스처를 생성한다. Blender 원본은 Assets 바깥에 두어 Unity/CI가 Blender 설치 없이 FBX로 빌드하도록 했다.

자가 진단 중 실제 공격 테스트가 뒤따르는 HUD 테스트의 체력을 감소시키는 테스트 격리 문제를 발견하여, 두 테스트 사이에 체력을 복원하도록 수정했다. 검증 사진과 실제 검사 수/소스 커밋은 verification 브랜치에도 보관한다.
