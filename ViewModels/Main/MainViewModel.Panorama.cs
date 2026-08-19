using OpenCvWpfTracking.Common;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace OpenCvWpfTracking.ViewModels.Main
{
    public partial class MainViewModel
    {
        private bool _isPanoramaCaptureRunning;
        private bool _isPanoramaProcessingRunning;

        private const double PanoramaCaptureStepDegrees = 10.0;
        private const int PanoramaCaptureFrameCount = 36;
        private const int PanoramaCaptureRowCount = 2;
        private const double PanoramaCaptureTiltOffsetDegrees = 12.0;
        private const double PanoramaPanTolerance = 0.12;
        private const int PanoramaPanStableSampleCount = 2;
        private const int PanoramaMaximumEoZoomPosition = 100;

        /// <summary>
        /// 2026-08-18: 자동 파노라마 촬영 중 수동 장비 제어 잠금 상태.
        /// 하단의 "촬영 중지" 버튼은 계속 사용할 수 있다.
        /// </summary>
        public bool IsPanoramaCaptureRunning
        {
            get => _isPanoramaCaptureRunning;
            private set
            {
                if (_isPanoramaCaptureRunning == value)
                {
                    return;
                }

                _isPanoramaCaptureRunning = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsControlInputLocked));
                OnPropertyChanged(nameof(IsOperationCommandEnabled));
                OnPropertyChanged(nameof(IsPanTiltSpeedControlEnabled));
                OnPropertyChanged(nameof(ControlLockTitle));
                OnPropertyChanged(nameof(ControlLockMessage));
            }

        }

        /// <summary>
        /// 2026-08-18: 촬영 시작부터 특징점 정합, 블렌딩 및 파일 저장이
        /// 끝날 때까지 전체 파노라마 작업의 장비 제어 잠금을 유지한다.
        /// </summary>
        public bool IsPanoramaProcessingRunning =>
            _isPanoramaProcessingRunning;

        /// <summary>
        /// SetPanoramaProcessingRunning 설정 함수.
        /// </summary>
        public void SetPanoramaProcessingRunning(
            bool isRunning)
        {
            if (_isPanoramaProcessingRunning == isRunning)
            {
                return;
            }

            _isPanoramaProcessingRunning = isRunning;
            OnPropertyChanged(nameof(IsPanoramaProcessingRunning));
            OnPropertyChanged(nameof(IsControlInputLocked));
            OnPropertyChanged(nameof(IsOperationCommandEnabled));
            OnPropertyChanged(nameof(IsPanTiltSpeedControlEnabled));
            OnPropertyChanged(nameof(ControlLockTitle));
            OnPropertyChanged(nameof(ControlLockMessage));
        }

        /// <summary>
        /// 2026-08-18: ROOFTOP EO 자동 파노라마 촬영 전 필수 상태를 검사한다.
        /// null이면 촬영 가능하며, 문자열이면 사용자에게 표시할 차단 사유다.
        /// </summary>
        public string GetPanoramaCaptureBlockReason()
        {
            if (!IsRooftopStatusSelected)
            {
                return "360° 파노라마 촬영은 ROOFTOP / LA AGENT 모드에서만 지원합니다.";
            }

            if (!_laTcpService.IsConnected)
            {
                return "Control Agent TCP 연결 후 파노라마 촬영을 시작하십시오.";
            }

            if (EoStatusText != "[EO] Connected" ||
                EOCameraImage == null ||
                !_isEoFrameDisplayed)
            {
                return "EO RTSP 영상이 실제로 표시된 후 파노라마 촬영을 시작하십시오.";
            }

            if (_isHomePositionMoving ||
                _isLaPresetScanRunning ||
                _isPresetScanRunning)
            {
                return "HOME 또는 PRESET/AUTO SCAN 작업을 먼저 종료하십시오.";
            }

            int eoZoom =
                GetCurrentPresetStandardZoom();

            if (eoZoom > PanoramaMaximumEoZoomPosition)
            {
                return "파노라마 중첩 확보를 위해 EO Zoom을 광각(0~100 / 1000)으로 낮추십시오. " +
                       "현재 값: " + eoZoom;
            }

            return null;
        }

        /// <summary>
        /// 2026-08-18: 기준 Tilt의 위·아래 12°에서 현재 Pan을 시작점으로
        /// 같은 방향으로 10°씩 순환하며 행별 EO 프레임 36장을 복사한다.
        /// 첫 실장비 결과 대비 중첩률과 RTSP 안정화 시간을 높였다.
        /// 고정 -180° 선이동을 제거하여 촬영 도중 반대 방향 전환을 방지한다.
        /// </summary>
        public async Task<IList<IList<BitmapSource>>> CaptureEoPanoramaFramesAsync(
            IProgress<string> progress,
            CancellationToken cancellationToken)
        {
            string blockReason =
                GetPanoramaCaptureBlockReason();

            if (blockReason != null)
            {
                ConsoleLogHelper.Warning(
                    "EO PANORAMA / CAPTURE",
                    "Capture blocked / REASON=" + blockReason);
                throw new InvalidOperationException(blockReason);
            }

            double originalPan =
                _currentPan;

            double originalTilt =
                _currentTilt;

            /*
             * 2026-08-18: 촬영 도중 Slider 변경이나 다른 UI 상태에 영향을 받지
             * 않도록 시작 순간의 PAN/TILT SPEED 값을 전체 촬영의 고정값으로 쓴다.
             */
            int capturePositionSpeed =
                Math.Max(
                    5,
                    Math.Min(
                        50,
                        (int)PanTiltSpeedLevel));

            int frameStabilizationMs =
                GetPanoramaFrameStabilizationMs(
                    capturePositionSpeed);

            List<IList<BitmapSource>> capturedRows =
                new List<IList<BitmapSource>>(PanoramaCaptureRowCount);

            ConsoleLogHelper.Info(
                "EO PANORAMA",
                "360-degree capture started / " +
                "ROWS=2 / COUNT_PER_ROW=36 / STEP=10deg / " +
                "TILT_OFFSET=+-12deg / SLIDER_SPEED=" + PanTiltSpeedLevel +
                " / CAPTURE_SPEED=" + capturePositionSpeed + "deg/s" +
                " / STABLE=" + frameStabilizationMs + "ms / " +
                "START_PAN=" + originalPan.ToString("F2") +
                " / START_TILT=" + originalTilt.ToString("F2"));

            IsPanoramaCaptureRunning =
                true;

            try
            {
                for (int rowIndex = 0;
                     rowIndex < PanoramaCaptureRowCount;
                     rowIndex++)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    double targetTilt =
                        ClampPanoramaTilt(
                            originalTilt +
                            (rowIndex == 0
                                ? PanoramaCaptureTiltOffsetDegrees
                                : -PanoramaCaptureTiltOffsetDegrees));

                    ConsoleLogHelper.Command(
                        "EO PANORAMA / CAPTURE",
                        "Row positioning started / ROW=" + (rowIndex + 1) +
                        " / TARGET_TILT=" + targetTilt.ToString("F2") +
                        " / START_PAN=" + originalPan.ToString("F2"));

                    progress?.Report(
                        string.Format(
                            "360° PANORAMA / 세로 촬영 {0}/{1} 준비 / TILT {2:F0}°",
                            rowIndex + 1,
                            PanoramaCaptureRowCount,
                            targetTilt));

                    if (!await MoveTiltForPanoramaAsync(
                            targetTilt,
                            capturePositionSpeed,
                            cancellationToken))
                    {
                        throw new InvalidOperationException(
                            "Tilt " + targetTilt.ToString("F0") +
                            "° 위치 도달을 확인하지 못했습니다.");
                    }

                    ConsoleLogHelper.State(
                        "EO PANORAMA / CAPTURE",
                        "Tilt arrived / ROW=" + (rowIndex + 1) +
                        " / TARGET=" + targetTilt.ToString("F2") +
                        " / ACTUAL=" + _currentTilt.ToString("F2"));

                    if (rowIndex > 0 &&
                        !await MovePanForPanoramaAsync(
                            originalPan,
                            capturePositionSpeed,
                            cancellationToken))
                    {
                        throw new InvalidOperationException(
                            "두 번째 세로 촬영 시작 Pan 위치로 복귀하지 못했습니다.");
                    }

                    List<BitmapSource> capturedFrames =
                        new List<BitmapSource>(PanoramaCaptureFrameCount);

                    for (int index = 0;
                         index < PanoramaCaptureFrameCount;
                         index++)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        double targetPan =
                            NormalizePanoramaPan(
                                originalPan +
                                index * PanoramaCaptureStepDegrees);

                        progress?.Report(
                            string.Format(
                                "360° PANORAMA / {0}행 이동 {1}/{2} / PAN {3:F0}° / TILT {4:F0}°",
                                rowIndex + 1,
                                index + 1,
                                PanoramaCaptureFrameCount,
                                targetPan,
                                targetTilt));

                        bool moved =
                            index == 0
                                ? true
                                : await MovePanForPanoramaAsync(
                                    targetPan,
                                    capturePositionSpeed,
                                    cancellationToken);

                        if (!moved)
                        {
                            throw new InvalidOperationException(
                                "Pan " + targetPan.ToString("F0") +
                                "° 위치 도달을 확인하지 못했습니다.");
                        }

                        ConsoleLogHelper.State(
                            "EO PANORAMA / MOVE",
                            "Pan arrived / ROW=" + (rowIndex + 1) +
                            " / FRAME=" + (index + 1) + "/" + PanoramaCaptureFrameCount +
                            " / TARGET=" + targetPan.ToString("F2") +
                            " / ACTUAL=" + _currentPan.ToString("F2"));

                        await Task.Delay(
                            frameStabilizationMs,
                            cancellationToken);

                        BitmapSource frame =
                            EOCameraImage;

                        if (frame == null)
                        {
                            throw new InvalidOperationException(
                                "EO 프레임을 가져오지 못했습니다.");
                        }

                        BitmapSource frozenFrame =
                            frame.Clone();

                        if (frozenFrame.CanFreeze &&
                            !frozenFrame.IsFrozen)
                        {
                            frozenFrame.Freeze();
                        }

                        capturedFrames.Add(frozenFrame);

                        ConsoleLogHelper.Info(
                            "EO PANORAMA / FRAME",
                            "Frame captured / ROW=" + (rowIndex + 1) +
                            " / FRAME=" + (index + 1) + "/" + PanoramaCaptureFrameCount +
                            " / PAN=" + _currentPan.ToString("F2") +
                            " / TILT=" + _currentTilt.ToString("F2") +
                            " / SPEED=" + capturePositionSpeed +
                            " / STABLE_MS=" + frameStabilizationMs +
                            " / SIZE=" + frozenFrame.PixelWidth + "x" + frozenFrame.PixelHeight);

                        progress?.Report(
                            string.Format(
                                "360° PANORAMA / {0}행 촬영 {1}/{2} / 전체 {3}/{4}",
                                rowIndex + 1,
                                index + 1,
                                PanoramaCaptureFrameCount,
                                rowIndex * PanoramaCaptureFrameCount + index + 1,
                                PanoramaCaptureRowCount * PanoramaCaptureFrameCount));
                    }

                    capturedRows.Add(capturedFrames);

                    ConsoleLogHelper.State(
                        "EO PANORAMA / CAPTURE",
                        "Row capture completed / ROW=" + (rowIndex + 1) +
                        " / FRAMES=" + capturedFrames.Count);
                }

                ConsoleLogHelper.State(
                    "EO PANORAMA",
                    "Frame capture completed / ROWS=" + capturedRows.Count +
                    " / TOTAL_COUNT=" +
                    (PanoramaCaptureRowCount * PanoramaCaptureFrameCount));

                return capturedRows;
            }
            catch (OperationCanceledException)
            {
                ConsoleLogHelper.Warning(
                    "EO PANORAMA / CAPTURE",
                    "Capture canceled / CAPTURED_ROWS=" + capturedRows.Count);
                throw;
            }
            catch (Exception ex)
            {
                ConsoleLogHelper.Error(
                    "EO PANORAMA / CAPTURE",
                    "Capture failed / CAPTURED_ROWS=" + capturedRows.Count,
                    ex);
                throw;
            }
            finally
            {
                _controlCommandService.StopPanTiltPositionMove();

                progress?.Report(
                    "360° PANORAMA / 시작 위치 복귀 중...");

                try
                {
                    await MovePanForPanoramaAsync(
                        originalPan,
                        capturePositionSpeed,
                        CancellationToken.None);

                    await MoveTiltForPanoramaAsync(
                        originalTilt,
                        capturePositionSpeed,
                        CancellationToken.None);

                    ConsoleLogHelper.State(
                        "EO PANORAMA / RESTORE",
                        "Start position restored / PAN=" + _currentPan.ToString("F2") +
                        " / TILT=" + _currentTilt.ToString("F2"));
                }
                catch (Exception ex)
                {
                    ConsoleLogHelper.Warning(
                        "EO PANORAMA",
                        "Start position restore failed / " + ex.Message);
                }

                IsPanoramaCaptureRunning =
                    false;
            }

        }

        /// <summary>
        /// 2026-08-18: 누적 Pan 목표값을 LA 절대 위치 범위 -180°~+180°로
        /// 정규화한다. +180/-180 경계에서도 다음 목표는 항상 10° 차이다.
        /// </summary>
        private static double NormalizePanoramaPan(
            double pan)
        {
            while (pan > 180.0)
            {
                pan -= 360.0;
            }

            while (pan < -180.0)
            {
                pan += 360.0;
            }

            return pan;
        }

        /// <summary>
        /// ClampPanoramaTilt 동작 수행 함수.
        /// </summary>
        private static double ClampPanoramaTilt(
            double tilt)
        {
            return Math.Max(
                -90.0,
                Math.Min(
                    90.0,
                    tilt));
        }

        /// <summary>
        /// 촬영 시작 시 고정한 Slider 속도에 따라 장비 정지 후 영상 안정화
        /// 시간을 선택한다. Slider는 5단위라 구간 사이 값은 발생하지 않지만
        /// 방어적으로 21~35는 1.2초, 36 이상은 1.5초로 처리한다.
        /// </summary>
        private static int GetPanoramaFrameStabilizationMs(
            int positionSpeed)
        {
            if (positionSpeed <= 20)
            {
                return 1000;
            }

            if (positionSpeed <= 35)
            {
                return 1200;
            }

            return 1500;
        }

        /// <summary>
        /// 2026-08-18: 파노라마 촬영 전용 Pan 이동 및 상태 Packet 기반 도착 판정.
        /// 기존 수동/프리셋 입력값을 변경하지 않고 명령만 독립 송신한다.
        /// </summary>
        private async Task<bool> MovePanForPanoramaAsync(
            double targetPan,
            int positionSpeed,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            bool commandResult =
                _controlCommandService.SetPanPositionSpeed(
                    positionSpeed) &&
                _controlCommandService.SetPanShortestPathMode() &&
                _controlCommandService.PanGoPosition(
                    targetPan);

            ConsoleLogHelper.Command(
                "EO PANORAMA / MOVE",
                "Pan absolute move sent / TARGET=" + targetPan.ToString("F2") +
                " / SPEED=" + positionSpeed +
                " / SEND_RESULT=" + commandResult);

            if (!commandResult)
            {
                return false;
            }

            _lastPanAbsoluteTarget =
                targetPan;

            long observedVersion =
                Interlocked.Read(
                    ref _panTiltStatusVersion);

            int stableCount = 0;
            Stopwatch stopwatch =
                Stopwatch.StartNew();

            double distance =
                Math.Abs(
                    GetShortestPanDifference(
                        _currentPan,
                        targetPan));

            int timeoutMs =
                Math.Max(
                    4000,
                    Math.Min(
                        30000,
                        (int)(distance /
                              positionSpeed *
                              1000.0) +
                        3500));

            while (stopwatch.ElapsedMilliseconds < timeoutMs)
            {
                await Task.Delay(
                    100,
                    cancellationToken);

                long currentVersion =
                    Interlocked.Read(
                        ref _panTiltStatusVersion);

                if (currentVersion == observedVersion)
                {
                    continue;
                }

                observedVersion =
                    currentVersion;

                double panDelta =
                    Math.Abs(
                        GetShortestPanDifference(
                            _currentPan,
                            targetPan));

                stableCount =
                    panDelta <= PanoramaPanTolerance
                        ? stableCount + 1
                        : 0;

                if (stableCount >= PanoramaPanStableSampleCount)
                {
                    ConsoleLogHelper.State(
                        "EO PANORAMA / MOVE",
                        "Pan target stable / TARGET=" + targetPan.ToString("F2") +
                        " / ACTUAL=" + _currentPan.ToString("F2") +
                        " / ELAPSED_MS=" + stopwatch.ElapsedMilliseconds);
                    return true;
                }

            }

            ConsoleLogHelper.Warning(
                "EO PANORAMA / MOVE",
                "Pan target timeout / TARGET=" + targetPan.ToString("F2") +
                " / ACTUAL=" + _currentPan.ToString("F2") +
                " / TIMEOUT_MS=" + timeoutMs);
            return false;
        }

        /// <summary>
        /// 2026-08-18: 휴대폰 파노라마에 가까운 세로 화각 확보를 위한
        /// 2행 촬영 전용 Tilt 절대 이동 및 상태 Packet 기반 도착 판정.
        /// </summary>
        private async Task<bool> MoveTiltForPanoramaAsync(
            double targetTilt,
            int positionSpeed,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            bool commandResult =
                _controlCommandService.SetTiltPositionSpeed(
                    positionSpeed) &&
                _controlCommandService.TiltGoPosition(
                    targetTilt);

            ConsoleLogHelper.Command(
                "EO PANORAMA / MOVE",
                "Tilt absolute move sent / TARGET=" + targetTilt.ToString("F2") +
                " / SPEED=" + positionSpeed +
                " / SEND_RESULT=" + commandResult);

            if (!commandResult)
            {
                return false;
            }

            long observedVersion =
                Interlocked.Read(
                    ref _panTiltStatusVersion);

            int stableCount = 0;
            Stopwatch stopwatch =
                Stopwatch.StartNew();

            double distance =
                Math.Abs(
                    _currentTilt - targetTilt);

            int timeoutMs =
                Math.Max(
                    4000,
                    Math.Min(
                        30000,
                        (int)(distance /
                              positionSpeed *
                              1000.0) +
                        3500));

            while (stopwatch.ElapsedMilliseconds < timeoutMs)
            {
                await Task.Delay(
                    100,
                    cancellationToken);

                long currentVersion =
                    Interlocked.Read(
                        ref _panTiltStatusVersion);

                if (currentVersion == observedVersion)
                {
                    continue;
                }

                observedVersion =
                    currentVersion;

                double tiltDelta =
                    Math.Abs(
                        _currentTilt - targetTilt);

                stableCount =
                    tiltDelta <= PanoramaPanTolerance
                        ? stableCount + 1
                        : 0;

                if (stableCount >= PanoramaPanStableSampleCount)
                {
                    ConsoleLogHelper.State(
                        "EO PANORAMA / MOVE",
                        "Tilt target stable / TARGET=" + targetTilt.ToString("F2") +
                        " / ACTUAL=" + _currentTilt.ToString("F2") +
                        " / ELAPSED_MS=" + stopwatch.ElapsedMilliseconds);
                    return true;
                }

            }

            ConsoleLogHelper.Warning(
                "EO PANORAMA / MOVE",
                "Tilt target timeout / TARGET=" + targetTilt.ToString("F2") +
                " / ACTUAL=" + _currentTilt.ToString("F2") +
                " / TIMEOUT_MS=" + timeoutMs);
            return false;
        }

    }

}
