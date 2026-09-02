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
        private bool _isPanoramaCancellationRequested;
        private bool _isPanoramaCancellationCompleted;
        private bool _isPanoramaCompleted;
        private readonly object _panoramaSourceFrameSync = new object();
        private BitmapSource _latestRawEoPanoramaFrame;
        // 2026-08-31: 파노라마 PTZ 이동/잔진동 구간은 탐지 기준 영상으로 사용하지 않는다.
        private int _panoramaCameraMotionActive;
        private long _panoramaDetectionResumeUtcTicks;

        private const double PanoramaCaptureStepDegrees = 10.0;
        private const int PanoramaCaptureFrameCount = 36;
        private const int PanoramaCaptureRowCount = 2;
        private const double PanoramaCaptureTiltOffsetDegrees = 12.0;
        private const double PanoramaPanTolerance = 0.05;
        private const int PanoramaPanStableSampleCount = 4;
        private const int PanoramaCapturePositionSpeed = 15;
        private const int PanoramaMaximumEoZoomPosition = 100;
        private const int PanoramaDetectionSettleMs = 400;

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
                OnPropertyChanged(nameof(IsOperationTabNavigationEnabled));
                OnPropertyChanged(nameof(IsEventAlertControlEnabled));
                OnPropertyChanged(nameof(IsOperationLockOverlayVisible));
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

        public bool IsPanoramaCancellationRequested =>
            _isPanoramaCancellationRequested;

        public bool IsPanoramaCancellationCompleted =>
            _isPanoramaCancellationCompleted;

        public bool IsPanoramaCompleted =>
            _isPanoramaCompleted;

        /// <summary>
        /// 2026-08-24: 파노라마 취소 요청 및 시작 위치 복귀 완료 상태를
        /// 우측 상단 작업 상태 표시와 동기화한다.
        /// </summary>
        public void SetPanoramaCancellationState(
            bool isRequested,
            bool isCompleted)
        {
            _isPanoramaCancellationRequested = isRequested;
            _isPanoramaCancellationCompleted = isCompleted;
            OnPropertyChanged(nameof(IsPanoramaCancellationRequested));
            OnPropertyChanged(nameof(IsPanoramaCancellationCompleted));
            OnPropertyChanged(nameof(ControlLockTitle));
            OnPropertyChanged(nameof(ControlLockMessage));
        }

        /// <summary>
        /// 2026-08-24: 정상 생성 완료 상태를 우측 상단 작업 표시와 동기화한다.
        /// 완료 알림을 확인할 때까지 촬영 중 문구가 남지 않도록 별도 상태로 관리한다.
        /// </summary>
        public void SetPanoramaCompletionState(
            bool isCompleted)
        {
            _isPanoramaCompleted = isCompleted;
            OnPropertyChanged(nameof(IsPanoramaCompleted));
            OnPropertyChanged(nameof(ControlLockTitle));
            OnPropertyChanged(nameof(ControlLockMessage));
        }

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
            OnPropertyChanged(nameof(IsOperationTabNavigationEnabled));
            OnPropertyChanged(nameof(IsEventAlertControlEnabled));
            OnPropertyChanged(nameof(IsOperationLockOverlayVisible));
            OnPropertyChanged(nameof(IsPanTiltSpeedControlEnabled));
            OnPropertyChanged(nameof(ControlLockTitle));
            OnPropertyChanged(nameof(ControlLockMessage));
        }

        /// <summary>
        /// 2026-08-28: ROOFTOP과 ENVIRONMENT 공통 EO 파노라마 촬영 전
        /// 필수 연결·영상·운용 상태를 검사한다.
        /// null이면 촬영 가능하며, 문자열이면 사용자에게 표시할 차단 사유다.
        /// </summary>
        public string GetPanoramaCaptureBlockReason()
        {
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

            ClearRawEoPanoramaFrame();

            double originalPan =
                _currentPan;

            double originalTilt =
                _currentTilt;

            /*
             * PANORAMA 전용 PT 속도는 수동 Slider와 분리한다.
             *
             * 고속 이동(예: 50)은 목표 위치 도착 직후 잔진동/오버슈트가 남아
             * 근거리 난간·건물에서 프레임별 광축 차이와 parallax가 커질 수 있다.
             * 파노라마 촬영 중에는 안정성을 우선하여 Speed 15를 고정 사용한다.
             * 수동 PT 조작의 PanTiltSpeedLevel 값 자체는 변경하지 않는다.
             */
            int capturePositionSpeed =
                PanoramaCapturePositionSpeed;

            int frameStabilizationMs =
                GetPanoramaFrameStabilizationMs(
                    capturePositionSpeed);

            List<IList<BitmapSource>> capturedRows =
                new List<IList<BitmapSource>>(PanoramaCaptureRowCount);

            ConsoleLogHelper.Info(
                "EO PANORAMA",
                "360-degree capture started / " +
                "ROWS=2 / COUNT_PER_ROW=36 / STEP=10deg / " +
                "TILT_OFFSET=+-12deg / MANUAL_SLIDER_SPEED=" + PanTiltSpeedLevel +
                " / CAPTURE_SPEED_FIXED=" + capturePositionSpeed + "deg/s" +
                " / STABLE=" + frameStabilizationMs + "ms / " +
                "START_PAN=" + originalPan.ToString("F2") +
                " / START_TILT=" + originalTilt.ToString("F2") +
                " / MODE=" + (IsRooftopStatusSelected ? "ROOFTOP" : "ENVIRONMENT"));

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

                        // 2026-08-31: 표시 영상에는 탐지 Overlay가 포함될 수 있으므로
                        // 파노라마는 분석·표시 처리 전 EO 원본 프레임만 사용한다.
                        BitmapSource frame =
                            GetRawEoPanoramaFrame();

                        if (frame == null)
                        {
                            throw new InvalidOperationException(
                                "Overlay가 없는 EO 원본 프레임을 가져오지 못했습니다.");
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
                            " / SOURCE=RAW_NO_OVERLAY" +
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
                    ConsoleLogHelper.Error(
                        "EO PANORAMA / RESTORE",
                        "Start position restore failed",
                        ex);

                    throw new InvalidOperationException(
                        "파노라마 촬영 시작 위치로 복귀하지 못했습니다.",
                        ex);
                }
                finally
                {
                    // 복귀 명령 자체가 실패해도 탐지 게이트와 UI 잠금은 반드시 해제한다.
                    IsPanoramaCaptureRunning =
                        false;
                    ResetPanoramaDetectionGate();
                }
            }

        }

        /// <summary>
        /// 2026-08-31: 탐지 Overlay 적용 전 EO 프레임을 파노라마 전용 버퍼에 보관한다.
        /// Frozen Bitmap만 공유하여 수신 Thread와 촬영 Thread 간 접근을 안전하게 한다.
        /// </summary>
        private void SetRawEoPanoramaFrame(BitmapSource frame)
        {
            if (frame == null)
            {
                return;
            }

            lock (_panoramaSourceFrameSync)
            {
                _latestRawEoPanoramaFrame = frame;
            }
        }

        private BitmapSource GetRawEoPanoramaFrame()
        {
            lock (_panoramaSourceFrameSync)
            {
                return _latestRawEoPanoramaFrame;
            }
        }

        private void ClearRawEoPanoramaFrame()
        {
            lock (_panoramaSourceFrameSync)
            {
                _latestRawEoPanoramaFrame = null;
            }

            ConsoleLogHelper.State(
                "EO PANORAMA / FRAME",
                "Raw panorama frame buffer reset");
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
        /// 파노라마 전용 고정 PT 속도에 따라 도착 판정 후 추가 안정화 시간을 선택한다.
        /// Speed 15에서는 상태 Packet 4회 연속(오차 0.05° 이하) 안정 판정 후
        /// 1초를 더 기다려 잔진동이 영상에 남는 것을 줄인다.
        /// </summary>
        private static int GetPanoramaFrameStabilizationMs(
            int positionSpeed)
        {
            if (positionSpeed <= 20)
            {
                // 이동 후 0.4초는 분석을 차단하고 이후 약 1.4초 동안
                // 새 기준 프레임과 36프레임 SMOKE 확인 시간을 확보한다.
                return 1800;
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

            BeginPanoramaCameraMotion("PAN", targetPan);

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
                    EndPanoramaCameraMotion("PAN", targetPan);
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

            BeginPanoramaCameraMotion("TILT", targetTilt);

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
                    EndPanoramaCameraMotion("TILT", targetTilt);
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

        /// <summary>
        /// 파노라마 이동 중과 도착 직후 잔진동 구간의 신규 탐지를 억제한다.
        /// </summary>
        private void BeginPanoramaCameraMotion(string axis, double target)
        {
            Volatile.Write(ref _panoramaCameraMotionActive, 1);
            Interlocked.Exchange(ref _panoramaDetectionResumeUtcTicks, long.MaxValue);
            ConsoleLogHelper.State(
                "PANORAMA DETECTION GATE",
                "Detection suspended / AXIS=" + axis +
                " / TARGET=" + target.ToString("F2"));
        }

        private void EndPanoramaCameraMotion(string axis, double target)
        {
            Interlocked.Exchange(
                ref _panoramaDetectionResumeUtcTicks,
                DateTime.UtcNow.AddMilliseconds(PanoramaDetectionSettleMs).Ticks);
            Volatile.Write(ref _panoramaCameraMotionActive, 0);
            ConsoleLogHelper.State(
                "PANORAMA DETECTION GATE",
                "Stable-frame analysis scheduled / AXIS=" + axis +
                " / TARGET=" + target.ToString("F2") +
                " / SETTLE_MS=" + PanoramaDetectionSettleMs);
        }

        private bool IsPanoramaMotionDetectionSuppressed()
        {
            if (!IsPanoramaCaptureRunning)
            {
                return false;
            }

            return Volatile.Read(ref _panoramaCameraMotionActive) != 0 ||
                   DateTime.UtcNow.Ticks <
                   Interlocked.Read(ref _panoramaDetectionResumeUtcTicks);
        }

        private void ResetPanoramaDetectionGate()
        {
            Volatile.Write(ref _panoramaCameraMotionActive, 0);
            Interlocked.Exchange(ref _panoramaDetectionResumeUtcTicks, 0L);
        }

    }

}
