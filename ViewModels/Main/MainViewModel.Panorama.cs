using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace OpenCvWpfTracking.ViewModels.Main
{
    public partial class MainViewModel
    {
        public OpenCvWpfTracking.Services.Video.PanoramaCoordinateMetadata LastPanoramaCoordinates
        {
            get { return _panoramaFeature.LastPanoramaCoordinates; }
            private set { _panoramaFeature.LastPanoramaCoordinates = value; }
        }

        internal const double PanoramaCaptureStepDegrees = 10.0;

        internal const int PanoramaCaptureFrameCount = 36;

        internal const double PanoramaPositionTolerance = 0.5;

        internal const int PanoramaPanStableSampleCount = 3;

        internal const int PanoramaCapturePositionSpeed = 15;

        internal const int PanoramaMaximumEoZoomPosition = 100;

        internal const int PanoramaDetectionSettleMs = 400;

        internal static readonly IList<string> PanoramaCaptureRangeOptionItems = new List<string>
        {
            "12° / 36 FRAMES",
            "24° / 72 FRAMES",
            "36° / 108 FRAMES",
            "48° / 144 FRAMES",
            "60° / 180 FRAMES"
        }.AsReadOnly();

        public IList<string> PanoramaCaptureRangeOptions
        {
            get { return _panoramaFeature.PanoramaCaptureRangeOptions; }
        }

        public int SelectedPanoramaCaptureOptionIndex
        {
            get { return _panoramaFeature.SelectedPanoramaCaptureOptionIndex; }
            set { _panoramaFeature.SelectedPanoramaCaptureOptionIndex = value; }
        }

        public int SelectedPanoramaTiltRangeDegrees
        {
            get { return _panoramaFeature.SelectedPanoramaTiltRangeDegrees; }
        }

        public int SelectedPanoramaRowCount
        {
            get { return _panoramaFeature.SelectedPanoramaRowCount; }
        }

        public int SelectedPanoramaTotalFrameCount
        {
            get { return _panoramaFeature.SelectedPanoramaTotalFrameCount; }
        }

        public string SelectedPanoramaCaptureSummary
        {
            get { return _panoramaFeature.SelectedPanoramaCaptureSummary; }
        }

        /// <summary>
        /// 2026-08-18: 자동 파노라마 촬영 중 수동 장비 제어 잠금 상태.
        /// 하단의 "촬영 중지" 버튼은 계속 사용할 수 있다.
        /// </summary>
        public bool IsPanoramaCaptureRunning
        {
            get { return _panoramaFeature.IsPanoramaCaptureRunning; }
            private set { _panoramaFeature.IsPanoramaCaptureRunning = value; }
        }

        /// <summary>
        /// 2026-08-18: 촬영 시작부터 특징점 정합, 블렌딩 및 파일 저장이
        /// 끝날 때까지 전체 파노라마 작업의 장비 제어 잠금을 유지한다.
        /// </summary>
        public bool IsPanoramaProcessingRunning
        {
            get { return _panoramaFeature.IsPanoramaProcessingRunning; }
        }

        public bool IsPanoramaCancellationRequested
        {
            get { return _panoramaFeature.IsPanoramaCancellationRequested; }
        }

        public bool IsPanoramaCancellationCompleted
        {
            get { return _panoramaFeature.IsPanoramaCancellationCompleted; }
        }

        public bool IsPanoramaCompleted
        {
            get { return _panoramaFeature.IsPanoramaCompleted; }
        }

        /// <summary>
        /// 2026-08-24: 파노라마 취소 요청 및 시작 위치 복귀 완료 상태를
        /// 우측 상단 작업 상태 표시와 동기화한다.
        /// </summary>
        public void SetPanoramaCancellationState(bool isRequested, bool isCompleted)
        {
            _panoramaFeature.SetPanoramaCancellationState(isRequested, isCompleted);
        }

        /// <summary>
        /// 2026-08-24: 정상 생성 완료 상태를 우측 상단 작업 표시와 동기화한다.
        /// 완료 알림을 확인할 때까지 촬영 중 문구가 남지 않도록 별도 상태로 관리한다.
        /// </summary>
        public void SetPanoramaCompletionState(bool isCompleted)
        {
            _panoramaFeature.SetPanoramaCompletionState(isCompleted);
        }

        /// <summary>
        /// SetPanoramaProcessingRunning 설정 함수.
        /// </summary>
        public void SetPanoramaProcessingRunning(bool isRunning)
        {
            _panoramaFeature.SetPanoramaProcessingRunning(isRunning);
        }

        /// <summary>
        /// 2026-08-28: ROOFTOP과 ENVIRONMENT 공통 EO 파노라마 촬영 전
        /// 필수 연결·영상·운용 상태를 검사한다.
        /// null이면 촬영 가능하며, 문자열이면 사용자에게 표시할 차단 사유다.
        /// </summary>
        public string GetPanoramaCaptureBlockReason()
        {
            return _panoramaFeature.GetPanoramaCaptureBlockReason();
        }

        /// <summary>
        /// 2026-08-18: 기준 Tilt의 위·아래 12°에서 현재 Pan을 시작점으로
        /// 같은 방향으로 10°씩 순환하며 행별 EO 프레임 36장을 복사한다.
        /// 첫 실장비 결과 대비 중첩률과 RTSP 안정화 시간을 높였다.
        /// 고정 -180° 선이동을 제거하여 촬영 도중 반대 방향 전환을 방지한다.
        /// </summary>
        public Task<IList<IList<BitmapSource>>> CaptureEoPanoramaFramesAsync(IProgress<string> progress, CancellationToken cancellationToken)
        {
            return _panoramaFeature.CaptureEoPanoramaFramesAsync(progress, cancellationToken);
        }

    }
}
