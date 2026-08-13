FIRE CANDIDATE OFFLINE VALIDATOR
작성일: 2026-08-11

1. 목적

- 실장비, AI 서버 및 Control Agent가 없어도 FIRE CANDIDATE 알고리즘을 시험한다.
- 이미지 또는 동영상을 불러와 검출 박스와 이진 마스크를 동시에 확인한다.
- 본 프로그램 결과는 확정 화재가 아니라 시험용 화재 후보이다.

2. 실행

- Visual Studio 2022에서 FireCandidateValidator.csproj를 연다.
- Debug / x64로 빌드 및 실행한다.
- 또는 bin\x64\Debug\FireCandidateValidator.exe를 실행한다.

3. 사용 순서

1) 이미지 열기 또는 동영상 열기를 누른다.
2) HOT PIXEL THRESHOLD를 조절한다.
3) MINIMUM CANDIDATE AREA RATIO를 조절한다.
4) 동영상은 VIDEO CONFIRM FRAMES 동안 후보가 연속 유지될 때 확정 표시한다.
5) 왼쪽 결과 영상과 오른쪽 마스크를 비교한다.
6) 결과 저장으로 현재 박스 영상을 저장한다.

4. 시험 패턴

- 시험 패턴 버튼은 적색/주황 고온 후보를 포함한 합성 영상을 생성한다.
- 장비 영상이 없어도 실행 환경과 박스 표시 기능을 즉시 확인할 수 있다.

5. 알고리즘 흐름

입력 영상
→ 밝기 및 적색/주황 팔레트 후보 분리
→ 주변 대비가 작은 균일 영역 제거
→ Morphology Open/Close 노이즈 제거
→ 최소/최대 면적 및 형상 필터
→ 동영상 연속 프레임 확인
→ FIRE CANDIDATE 표시

6. 주의

- 팔레트 영상의 RGB 픽셀은 실제 온도값이 아니다.
- 햇빛, 반사광, 고온 설비가 후보로 검출될 수 있다.
- 최종 화재 판정은 방사 온도 원시값 또는 AI 결과와 결합하여 검증해야 한다.
- 운영 Viewer와 독립된 Tools 프로젝트이므로 장비 제어 코드에는 영향을 주지 않는다.
