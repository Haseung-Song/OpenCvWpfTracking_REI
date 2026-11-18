using OpenCvWpfTracking.Common;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using static OpenCvWpfTracking.ViewModels.Main.MainViewModel;

namespace OpenCvWpfTracking.ViewModels.Main
{
    /// <summary>파노라마 촬영과 파일에 필요한 협력 기능만 받는 경계입니다. 화면 객체의 내부 구현을 알지 않습니다.</summary>
    internal interface IPanoramaFeatureContext
    {
        Services.Communication.IControlCommandServicePort _controlCommandService { get; }

        double _currentPan { get; set; }

        double _currentTilt { get; set; }

        bool _isEoFrameDisplayed { get; set; }

        bool _isHomePositionMoving { get; set; }

        bool _isLaPresetScanRunning { get; set; }

        bool _isPresetScanRunning { get; set; }

        ref double? _lastPanAbsoluteTarget { get; }

        Services.Communication.ITcpClientServicePort _laTcpService { get; }

        ref long _panTiltStatusVersion { get; }

        string ControlLockMessage { get; }

        string ControlLockTitle { get; }

        Tuple<double, double> ControlTuningForPanorama(bool ir);

        BitmapSource EOCameraImage { get; set; }

        string EoStatusText { get; set; }

        ushort GetCurrentPresetStandardZoom();

        System.Windows.Point GetOpticalCenter(bool ir);

        double GetTiltDifference(double currentTilt, double targetTilt);

        bool IsControlInputLocked { get; }

        bool IsEventAlertControlEnabled { get; }

        bool IsOperationCommandEnabled { get; }

        bool IsOperationLockOverlayVisible { get; }

        bool IsOperationTabNavigationEnabled { get; }

        bool IsPanTiltSpeedControlEnabled { get; }

        bool IsRooftopStatusSelected { get; }

        void OnPropertyChanged([global::System.Runtime.CompilerServices.CallerMemberName] string propertyName = null);

        string PanoramaPresetContextHash { get; }

        Services.Video.PanoramaReferenceState PanoramaReference { get; }

        byte PanTiltSpeedLevel { get; set; }
    }

    /// <summary>파노라마 촬영과 파일 담당 객체입니다. 상태를 보관하고 입력을 받아 결과를 알립니다.</summary>
    internal sealed class PanoramaFeature
    {

        #region [협력 객체와 초기화]

        private readonly IPanoramaFeatureContext _context;

        internal PanoramaFeature(IPanoramaFeatureContext context)
        {
            _context = context ?? throw new System.ArgumentNullException(nameof(context));
        }

        internal void InitializeState()
        {
            _selectedPanoramaCaptureOptionIndex = 1;
        }

        #endregion

        #region [상태 필드]

        internal bool _isPanoramaCaptureRunning;

        internal bool _isPanoramaProcessingRunning;

        internal bool _isPanoramaCancellationRequested;

        internal bool _isPanoramaCancellationCompleted;

        internal bool _isPanoramaCompleted;

        internal readonly object _panoramaSourceFrameSync = new object();

        internal BitmapSource _latestRawEoPanoramaFrame;

        internal int _panoramaCameraMotionActive;

        internal long _panoramaDetectionResumeUtcTicks;

        internal int _selectedPanoramaCaptureOptionIndex;

        #endregion

        #region [화면 속성과 이벤트]

        internal OpenCvWpfTracking.Services.Video.PanoramaCoordinateMetadata LastPanoramaCoordinates { get; set; }

        internal IList<string> PanoramaCaptureRangeOptions
        {
            get { return PanoramaCaptureRangeOptionItems; }
        }

        internal int SelectedPanoramaCaptureOptionIndex
        {
            get { return _selectedPanoramaCaptureOptionIndex; }

            set
            {
                int safeValue = Math.Max(0, Math.Min(4, value));

                if (_selectedPanoramaCaptureOptionIndex == safeValue)
                {
                    return;
                }

                _selectedPanoramaCaptureOptionIndex = safeValue;

                _context.OnPropertyChanged();

                _context.OnPropertyChanged(nameof(SelectedPanoramaCaptureSummary));

                _context.OnPropertyChanged(nameof(SelectedPanoramaTiltRangeDegrees));

                _context.OnPropertyChanged(nameof(SelectedPanoramaRowCount));

                _context.OnPropertyChanged(nameof(SelectedPanoramaTotalFrameCount));
            }
        }

        internal int SelectedPanoramaTiltRangeDegrees
        {
            get
            {
                return (_selectedPanoramaCaptureOptionIndex + 1) * 12;
            }
        }

        internal int SelectedPanoramaRowCount
        {
            get
            {
                return _selectedPanoramaCaptureOptionIndex + 1;
            }
        }

        internal int SelectedPanoramaTotalFrameCount
        {
            get
            {
                return SelectedPanoramaRowCount * PanoramaCaptureFrameCount;
            }
        }

        internal string SelectedPanoramaCaptureSummary
        {
            get
            {
                return "TILT " + SelectedPanoramaTiltRangeDegrees + "° / " + SelectedPanoramaTotalFrameCount + " FRAMES";
            }
        }

        internal bool IsPanoramaCaptureRunning
        {
            get { return _isPanoramaCaptureRunning; }

            set
            {
                if (_isPanoramaCaptureRunning == value)
                {
                    return;
                }

                _isPanoramaCaptureRunning = value;

                _context.OnPropertyChanged();

                _context.OnPropertyChanged(nameof(_context.IsControlInputLocked));

                _context.OnPropertyChanged(nameof(_context.IsOperationCommandEnabled));

                _context.OnPropertyChanged(nameof(_context.IsOperationTabNavigationEnabled));

                _context.OnPropertyChanged(nameof(_context.IsEventAlertControlEnabled));

                _context.OnPropertyChanged(nameof(_context.IsOperationLockOverlayVisible));

                _context.OnPropertyChanged(nameof(_context.IsPanTiltSpeedControlEnabled));

                _context.OnPropertyChanged(nameof(_context.ControlLockTitle));

                _context.OnPropertyChanged(nameof(_context.ControlLockMessage));
            }
        }

        internal bool IsPanoramaProcessingRunning
        {
            get { return _isPanoramaProcessingRunning; }
        }

        internal bool IsPanoramaCancellationRequested
        {
            get { return _isPanoramaCancellationRequested; }
        }

        internal bool IsPanoramaCancellationCompleted
        {
            get { return _isPanoramaCancellationCompleted; }
        }

        internal bool IsPanoramaCompleted
        {
            get { return _isPanoramaCompleted; }
        }

        #endregion

        #region [동작 처리]

        internal void SetPanoramaCancellationState(bool isRequested, bool isCompleted)
        {
            _isPanoramaCancellationRequested = isRequested;
            _isPanoramaCancellationCompleted = isCompleted;

            _context.OnPropertyChanged(nameof(IsPanoramaCancellationRequested));

            _context.OnPropertyChanged(nameof(IsPanoramaCancellationCompleted));

            _context.OnPropertyChanged(nameof(_context.ControlLockTitle));

            _context.OnPropertyChanged(nameof(_context.ControlLockMessage));
        }

        internal void SetPanoramaCompletionState(bool isCompleted)
        {
            _isPanoramaCompleted = isCompleted;

            _context.OnPropertyChanged(nameof(IsPanoramaCompleted));

            _context.OnPropertyChanged(nameof(_context.ControlLockTitle));

            _context.OnPropertyChanged(nameof(_context.ControlLockMessage));
        }

        internal void SetPanoramaProcessingRunning(bool isRunning)
        {
            if (_isPanoramaProcessingRunning == isRunning)
            {
                return;
            }

            _isPanoramaProcessingRunning = isRunning;

            _context.OnPropertyChanged(nameof(IsPanoramaProcessingRunning));

            _context.OnPropertyChanged(nameof(_context.IsControlInputLocked));

            _context.OnPropertyChanged(nameof(_context.IsOperationCommandEnabled));

            _context.OnPropertyChanged(nameof(_context.IsOperationTabNavigationEnabled));

            _context.OnPropertyChanged(nameof(_context.IsEventAlertControlEnabled));

            _context.OnPropertyChanged(nameof(_context.IsOperationLockOverlayVisible));

            _context.OnPropertyChanged(nameof(_context.IsPanTiltSpeedControlEnabled));

            _context.OnPropertyChanged(nameof(_context.ControlLockTitle));

            _context.OnPropertyChanged(nameof(_context.ControlLockMessage));
        }

        internal string GetPanoramaCaptureBlockReason()
        {
            if (!_context._laTcpService.IsConnected)
            {
                return "Control Agent TCP 연결 후 파노라마 촬영을 시작하십시오.";
            }

            if (_context.EoStatusText != "[EO] Connected" || _context.EOCameraImage == null || !_context._isEoFrameDisplayed)
            {
                return "EO RTSP 영상이 실제로 표시된 후 파노라마 촬영을 시작하십시오.";
            }

            if (_context._isHomePositionMoving || _context._isLaPresetScanRunning || _context._isPresetScanRunning)
            {
                return "HOME 또는 PRESET/AUTO SCAN 작업을 먼저 종료하십시오.";
            }

            int eoZoom = _context.GetCurrentPresetStandardZoom();

            if (eoZoom > PanoramaMaximumEoZoomPosition)
            {
                return "파노라마 중첩 확보를 위해 EO Zoom을 광각(0~100 / 1000)으로 낮추십시오. " + "현재 값: " + eoZoom;
            }

            try
            {
                BuildPanoramaTiltTargets(_selectedPanoramaCaptureOptionIndex, _context._currentTilt);
            }
            catch (InvalidOperationException ex)
            {
                return ex.Message;
            }

            return null;
        }

        internal static IList<double> BuildPanoramaTiltTargets(int optionIndex, double startTilt)
        {
            int safeOptionIndex = Math.Max(0, Math.Min(4, optionIndex));

            int rowCount = safeOptionIndex + 1;

            double tiltRange = rowCount * 12.0;

            List<double> targets = new List<double>(rowCount);

            if (rowCount == 1)
            {
                targets.Add(startTilt);
            }
            else
            {
                double upperOffset = tiltRange / 2.0;

                double step = tiltRange / (rowCount - 1);

                for (int rowIndex = 0; rowIndex < rowCount; rowIndex++)
                {
                    targets.Add(startTilt + upperOffset - rowIndex * step);
                }
            }

            if (targets.Exists(target => target < -90.0 || target > 90.0))
            {
                throw new InvalidOperationException("선택한 파노라마 Tilt 범위가 장비 허용범위(-90°~90°)를 초과합니다. " + "시작 Tilt를 중앙 쪽으로 이동하거나 더 작은 촬영 범위를 선택하십시오. " + "START=" + startTilt.ToString("F2") + "°, " + "TARGET=" + string.Join(", ", targets.ConvertAll(target => target.ToString("F2") + "°")));
            }

            return targets.AsReadOnly();
        }

        internal async Task<IList<IList<BitmapSource>>> CaptureEoPanoramaFramesAsync(IProgress<string> progress, CancellationToken cancellationToken)
        {
            string blockReason = GetPanoramaCaptureBlockReason();

            if (blockReason != null)
            {
                ConsoleLogHelper.Warning("EO PANORAMA / CAPTURE", "Capture blocked / REASON=" + blockReason);

                throw new InvalidOperationException(blockReason);
            }

            LastPanoramaCoordinates = null;

            ClearRawEoPanoramaFrame();

            double originalPan = _context._currentPan;

            double originalTilt = _context._currentTilt;

            int selectedOptionIndex = _selectedPanoramaCaptureOptionIndex;

            IList<double> tiltTargets = BuildPanoramaTiltTargets(selectedOptionIndex, originalTilt);

            int selectedTiltRangeDegrees = (selectedOptionIndex + 1) * 12;

            int totalFrameCount = tiltTargets.Count * PanoramaCaptureFrameCount;

            var tuning = _context.ControlTuningForPanorama(false);

            var center = _context.GetOpticalCenter(false);

            var coordinates = new OpenCvWpfTracking.Services.Video.PanoramaCoordinateMetadata
            {
                CapturePanOffset = _context.PanoramaReference.PanOffset,
                CaptureTiltOffset = _context.PanoramaReference.TiltOffset,
                ReferencePan = originalPan,
                ReferenceTilt = originalTilt,
                CaptureRange = selectedTiltRangeDegrees,
                CaptureDateTime = DateTimeOffset.Now.ToString("o"),
                ContextHash = _context.PanoramaPresetContextHash,
                OpticalCenterX = center.X,
                OpticalCenterY = center.Y,
                VerticalFov = 2 * Math.Atan(Math.Tan(tuning.Item1 * Math.PI / 360) / tuning.Item2) * 180 / Math.PI
            };

            // 2026-09-08: UI 범위와 실제 장비 Tilt Target의 연결을 명시한다.
            ConsoleLogHelper.StateSection("PANORAMA CONFIG", "Capture range resolved", string.Empty, "RANGE=" + selectedTiltRangeDegrees, "ROW_COUNT=" + tiltTargets.Count, "TOTAL_FRAME=" + totalFrameCount, "TILT_TARGETS=[" + string.Join(", ", new List<double>(tiltTargets).ConvertAll(target => target.ToString("F2"))) + "]");

            /*
             * PANORAMA 전용 PT 속도는 수동 Slider와 분리한다.
             *
             * 고속 이동(예: 50)은 목표 위치 도착 직후 잔진동/오버슈트가 남아
             * 근거리 난간·건물에서 프레임별 광축 차이와 parallax가 커질 수 있다.
             * 파노라마 촬영 중에는 안정성을 우선하여 Speed 15를 고정 사용한다.
             * 수동 PT 조작의 PanTiltSpeedLevel 값 자체는 변경하지 않는다.
             */
            int capturePositionSpeed = PanoramaCapturePositionSpeed;

            int frameStabilizationMs = GetPanoramaFrameStabilizationMs(capturePositionSpeed);

            List<IList<BitmapSource>> capturedRows = new List<IList<BitmapSource>>(tiltTargets.Count);

            ConsoleLogHelper.Info("EO PANORAMA", "360-degree capture started / " + "OPTION=" + (selectedOptionIndex + 1) + " / TILT_RANGE=" + selectedTiltRangeDegrees + "deg" + " / ROWS=" + tiltTargets.Count + " / COUNT_PER_ROW=" + PanoramaCaptureFrameCount + " / TOTAL_COUNT=" + totalFrameCount + " / STEP=10deg / TILT_TARGETS=" + string.Join(",", new List<double>(tiltTargets).ConvertAll(target => target.ToString("F2"))) + " / MANUAL_SLIDER_SPEED=" + _context.PanTiltSpeedLevel + " / CAPTURE_SPEED_FIXED=" + capturePositionSpeed + "deg/s" + " / STABLE=" + frameStabilizationMs + "ms / " + "START_PAN=" + originalPan.ToString("F2") + " / START_TILT=" + originalTilt.ToString("F2") + " / MODE=" + (_context.IsRooftopStatusSelected ? "ROOFTOP" : "ENVIRONMENT"));

            IsPanoramaCaptureRunning = true;

            try
            {
                for (int rowIndex = 0; rowIndex < tiltTargets.Count; rowIndex++)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    double targetTilt = tiltTargets[rowIndex];

                    // 2026-09-08: Row마다 진행 방향을 교대하여 Pan 축 누적 회전을 방지한다.
                    bool reversePanDirection = rowIndex % 2 == 1;

                    double rowStartPan = NormalizePanoramaPan(originalPan + (reversePanDirection ? (PanoramaCaptureFrameCount - 1) * PanoramaCaptureStepDegrees : 0.0));

                    ConsoleLogHelper.Command("EO PANORAMA / CAPTURE", "Row positioning started / ROW=" + (rowIndex + 1) + " / TARGET_TILT=" + targetTilt.ToString("F2") + " / START_PAN=" + rowStartPan.ToString("F2") + " / DIRECTION=" + (reversePanDirection ? "REVERSE" : "FORWARD"));

                    progress?.Report(string.Format("OPTION {0} / TILT RANGE {1}° / ROW {2}/{3} 준비 / TILT {4:F0}°", selectedOptionIndex + 1, selectedTiltRangeDegrees, rowIndex + 1, tiltTargets.Count, targetTilt));

                    if (!await MoveTiltForPanoramaAsync(targetTilt, capturePositionSpeed, cancellationToken))
                    {
                        throw new InvalidOperationException("Tilt " + targetTilt.ToString("F0") + "° 위치 도달을 확인하지 못했습니다.");
                    }

                    ConsoleLogHelper.State("EO PANORAMA / CAPTURE", "Tilt arrived / ROW=" + (rowIndex + 1) + " / TARGET=" + targetTilt.ToString("F2") + " / ACTUAL=" + _context._currentTilt.ToString("F2"));

                    if (rowIndex > 0 && !await MovePanForPanoramaAsync(rowStartPan, capturePositionSpeed, cancellationToken))
                    {
                        throw new InvalidOperationException((rowIndex + 1) + "번째 Row 촬영 시작 Pan 위치로 복귀하지 못했습니다.");
                    }

                    List<BitmapSource> capturedFrames = new List<BitmapSource>(PanoramaCaptureFrameCount);

                    var coordinateRow = new OpenCvWpfTracking.Services.Video.PanoramaCoordinateRow
                    {
                        TargetTilt = targetTilt
                    };

                    double tiltSum = 0;

                    for (int index = 0; index < PanoramaCaptureFrameCount; index++)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        int logicalPanIndex = reversePanDirection ? PanoramaCaptureFrameCount - 1 - index : index;

                        double targetPan = NormalizePanoramaPan(originalPan + logicalPanIndex * PanoramaCaptureStepDegrees);

                        progress?.Report(string.Format("OPTION {0} / TILT RANGE {1}° / ROW {2}/{3} / FRAME {4}/{5} 이동 / PAN {6:F0}° / TILT {7:F0}°", selectedOptionIndex + 1, selectedTiltRangeDegrees, rowIndex + 1, tiltTargets.Count, index + 1, PanoramaCaptureFrameCount, targetPan, targetTilt));

                        bool moved = index == 0 ? true : await MovePanForPanoramaAsync(targetPan, capturePositionSpeed, cancellationToken);

                        if (!moved)
                        {
                            throw new InvalidOperationException("Pan " + targetPan.ToString("F0") + "° 위치 도달을 확인하지 못했습니다.");
                        }

                        ConsoleLogHelper.State("EO PANORAMA / MOVE", "Pan arrived / ROW=" + (rowIndex + 1) + " / FRAME=" + (index + 1) + "/" + PanoramaCaptureFrameCount + " / CANONICAL_FRAME=" + (logicalPanIndex + 1) + " / DIRECTION=" + (reversePanDirection ? "REVERSE" : "FORWARD") + " / TARGET=" + targetPan.ToString("F2") + " / ACTUAL=" + _context._currentPan.ToString("F2"));

                        await Task.Delay(frameStabilizationMs, cancellationToken);

                        // 2026-08-31: 표시 영상에는 탐지 Overlay가 포함될 수 있으므로
                        // 파노라마는 분석·표시 처리 전 EO 원본 프레임만 사용한다.
                        BitmapSource frame = GetRawEoPanoramaFrame();

                        if (frame == null)
                        {
                            throw new InvalidOperationException("Overlay가 없는 EO 원본 프레임을 가져오지 못했습니다.");
                        }

                        BitmapSource frozenFrame = frame.Clone();

                        if (frozenFrame.CanFreeze && !frozenFrame.IsFrozen)
                        {
                            frozenFrame.Freeze();
                        }

                        capturedFrames.Add(frozenFrame);

                        coordinateRow.FramePans.Add(_context._currentPan);

                        tiltSum += _context._currentTilt;

                        ConsoleLogHelper.Info("EO PANORAMA / FRAME", "Frame captured / ROW=" + (rowIndex + 1) + " / FRAME=" + (index + 1) + "/" + PanoramaCaptureFrameCount + " / PAN=" + _context._currentPan.ToString("F2") + " / TILT=" + _context._currentTilt.ToString("F2") + " / SPEED=" + capturePositionSpeed + " / STABLE_MS=" + frameStabilizationMs + " / SOURCE=RAW_NO_OVERLAY" + " / SIZE=" + frozenFrame.PixelWidth + "x" + frozenFrame.PixelHeight);

                        progress?.Report(string.Format("OPTION {0} / TILT RANGE {1}° / ROW {2}/{3} / FRAME {4}/{5}", selectedOptionIndex + 1, selectedTiltRangeDegrees, rowIndex + 1, tiltTargets.Count, rowIndex * PanoramaCaptureFrameCount + index + 1, totalFrameCount));
                    }

                    // 역방향 촬영 Row도 합성 입력은 정방향 Pan 순서로 통일한다.
                    if (reversePanDirection)
                    {
                        capturedFrames.Reverse();

                        coordinateRow.FramePans.Reverse();
                    }

                    coordinateRow.ActualTilt = tiltSum / capturedFrames.Count;

                    coordinates.Rows.Add(coordinateRow);

                    capturedRows.Add(capturedFrames);

                    ConsoleLogHelper.State("EO PANORAMA / CAPTURE", "Row capture completed / ROW=" + (rowIndex + 1) + " / FRAMES=" + capturedFrames.Count + " / DIRECTION=" + (reversePanDirection ? "REVERSE" : "FORWARD") + " / STORED_ORDER=CANONICAL_FORWARD");
                }

                ConsoleLogHelper.State("EO PANORAMA", "Frame capture completed / ROWS=" + capturedRows.Count + " / TOTAL_COUNT=" + totalFrameCount);

                LastPanoramaCoordinates = coordinates;

                return capturedRows;
            }
            catch (OperationCanceledException)
            {
                ConsoleLogHelper.Warning("EO PANORAMA / CAPTURE", "Capture canceled / CAPTURED_ROWS=" + capturedRows.Count);

                throw;
            }
            catch (Exception ex)
            {
                ConsoleLogHelper.Error("EO PANORAMA / CAPTURE", "Capture failed / CAPTURED_ROWS=" + capturedRows.Count, ex);

                throw;
            }
            finally
            {
                _context._controlCommandService.StopPanTiltPositionMove();

                progress?.Report("360° PANORAMA / 시작 위치 복귀 중...");

                try
                {
                    bool panRestored = await MovePanForPanoramaAsync(originalPan, capturePositionSpeed, CancellationToken.None);

                    bool tiltRestored = await MoveTiltForPanoramaAsync(originalTilt, capturePositionSpeed, CancellationToken.None);

                    string restoreResult = panRestored && tiltRestored ? "Start position restored" : "Start position restore incomplete";

                    ConsoleLogHelper.State("EO PANORAMA / RESTORE", restoreResult + " / EXPECTED_PAN=" + originalPan.ToString("F2") + " / ACTUAL_PAN=" + _context._currentPan.ToString("F2") + " / EXPECTED_TILT=" + originalTilt.ToString("F2") + " / ACTUAL_TILT=" + _context._currentTilt.ToString("F2") + " / PAN_RESTORED=" + panRestored + " / TILT_RESTORED=" + tiltRestored);

                    if (!panRestored || !tiltRestored)
                    {
                        throw new InvalidOperationException("파노라마 촬영 시작 위치 복귀가 완료되지 않았습니다.");
                    }
                }
                catch (Exception ex)
                {
                    ConsoleLogHelper.Error("EO PANORAMA / RESTORE", "Start position restore failed", ex);

                    throw new InvalidOperationException("파노라마 촬영 시작 위치로 복귀하지 못했습니다.", ex);
                }
                finally
                {
                    // 복귀 명령 자체가 실패해도 탐지 게이트와 UI 잠금은 반드시 해제한다.
                    IsPanoramaCaptureRunning = false;

                    ResetPanoramaDetectionGate();
                }
            }
        }

        internal void SetRawEoPanoramaFrame(BitmapSource frame)
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

        internal BitmapSource GetRawEoPanoramaFrame()
        {
            lock (_panoramaSourceFrameSync)
            {
                return _latestRawEoPanoramaFrame;
            }
        }

        internal void ClearRawEoPanoramaFrame()
        {
            lock (_panoramaSourceFrameSync)
            {
                _latestRawEoPanoramaFrame = null;
            }

            ConsoleLogHelper.State("EO PANORAMA / FRAME", "Raw panorama frame buffer reset");
        }

        internal static double NormalizePanoramaPan(double pan)
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

        internal static int GetPanoramaFrameStabilizationMs(int positionSpeed)
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

        internal async Task<bool> MovePanForPanoramaAsync(double targetPan, int positionSpeed, CancellationToken cancellationToken, bool allowRetry = true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            BeginPanoramaCameraMotion("PAN", targetPan);

            double startPosition = _context._currentPan;

            long startStatusVersion = Interlocked.Read(ref _context._panTiltStatusVersion);

            // 명령 직전 버전을 보관해야 빠른 상태 응답도 도착 판정에 포함된다.
            long observedVersion = Interlocked.Read(ref _context._panTiltStatusVersion);

            bool speedResult = _context._controlCommandService.SetPanPositionSpeed(positionSpeed);

            await Task.Delay(60, cancellationToken);

            bool modeResult = speedResult && _context._controlCommandService.SetPanShortestPathMode();

            await Task.Delay(60, cancellationToken);

            bool commandResult = modeResult && _context._controlCommandService.PanGoPosition(targetPan);

            ConsoleLogHelper.Command("EO PANORAMA / MOVE", "Pan absolute move sent / TARGET=" + targetPan.ToString("F2") + " / SPEED=" + positionSpeed + " / START_POSITION=" + startPosition.ToString("F2") + " / START_STATUS_VERSION=" + startStatusVersion + " / COMMAND_SENT=" + commandResult + " / CONTROL_CONNECTED=" + _context._laTcpService.IsConnected);

            if (!commandResult)
            {
                if (allowRetry)
                {
                    ConsoleLogHelper.Warning("EO PANORAMA / MOVE", "Pan command send failed; retrying once / TARGET=" + targetPan.ToString("F2"));

                    await Task.Delay(250, cancellationToken);

                    return await MovePanForPanoramaAsync(targetPan, positionSpeed, cancellationToken, false);
                }

                return false;
            }

            _context._lastPanAbsoluteTarget = targetPan;

            double distance = Math.Abs(GetShortestPanDifference(_context._currentPan, targetPan));

            if (distance <= PanoramaPositionTolerance)
            {
                EndPanoramaCameraMotion("PAN", targetPan);

                ConsoleLogHelper.State("EO PANORAMA / MOVE", "Pan target already within tolerance / TARGET=" + targetPan.ToString("F2") + " / ACTUAL=" + _context._currentPan.ToString("F2"));

                return true;
            }

            int stableCount = 0;

            bool movementStarted = false;

            bool noMotion = false;

            Stopwatch stopwatch = Stopwatch.StartNew();

            int timeoutMs = Math.Max(8000, Math.Min(45000, (int)(distance / positionSpeed * 1000.0) + 3500));

            while (stopwatch.ElapsedMilliseconds < timeoutMs)
            {
                await Task.Delay(100, cancellationToken);

                long currentVersion = Interlocked.Read(ref _context._panTiltStatusVersion);

                if (currentVersion == observedVersion)
                {
                    continue;
                }

                observedVersion = currentVersion;

                double movedDistance = Math.Abs(GetShortestPanDifference(startPosition, _context._currentPan));

                if (movedDistance >= 0.15)
                {
                    movementStarted = true;
                }

                // 2026-09-17: SEND_RESULT=True와 실제 장비 이동을 분리한다.
                // 상태 패킷은 오지만 1.2초 동안 위치가 그대로면 8초 전체 Timeout을
                // 기다리지 않고 STOP/재설정/재시도 Recovery로 전환한다.
                if (!movementStarted && stopwatch.ElapsedMilliseconds >= 1200)
                {
                    noMotion = true;

                    ConsoleLogHelper.Warning("EO PANORAMA / MOVE", "RESULT=COMMAND_SENT_BUT_NO_MOTION / TARGET=" + targetPan.ToString("F2") + " / START_POSITION=" + startPosition.ToString("F2") + " / CURRENT_POSITION=" + _context._currentPan.ToString("F2") + " / STATUS_VERSION=" + currentVersion + " / ELAPSED_MS=" + stopwatch.ElapsedMilliseconds);

                    break;
                }

                double panDelta = Math.Abs(GetShortestPanDifference(_context._currentPan, targetPan));

                stableCount = panDelta <= PanoramaPositionTolerance ? stableCount + 1 : 0;

                if (stableCount >= PanoramaPanStableSampleCount)
                {
                    EndPanoramaCameraMotion("PAN", targetPan);

                    ConsoleLogHelper.State("EO PANORAMA / MOVE", "Pan target stable / TARGET=" + targetPan.ToString("F2") + " / ACTUAL=" + _context._currentPan.ToString("F2") + " / MOVEMENT_STARTED=" + movementStarted + " / TARGET_REACHED=True" + " / ELAPSED_MS=" + stopwatch.ElapsedMilliseconds);

                    return true;
                }
            }

            ConsoleLogHelper.Warning("EO PANORAMA / MOVE", (noMotion ? "Pan no-motion" : "Pan target timeout") + " / TARGET=" + targetPan.ToString("F2") + " / ACTUAL=" + _context._currentPan.ToString("F2") + " / MOVEMENT_STARTED=" + movementStarted + " / TARGET_REACHED=False" + " / TIMEOUT_MS=" + timeoutMs);

            if (allowRetry)
            {
                bool stopResult = _context._controlCommandService.StopPanPositionMove();

                ConsoleLogHelper.Warning("EO PANORAMA / MOVE", "Pan recovery; retrying once / TARGET=" + targetPan.ToString("F2") + " / STOP_RESULT=" + stopResult + " / CONTROL_CONNECTED=" + _context._laTcpService.IsConnected + " / RETRY_COUNT=1");

                await Task.Delay(250, cancellationToken);

                return await MovePanForPanoramaAsync(targetPan, positionSpeed, cancellationToken, false);
            }

            return false;
        }

        internal async Task<bool> MoveTiltForPanoramaAsync(double targetTilt, int positionSpeed, CancellationToken cancellationToken, bool allowRetry = true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            BeginPanoramaCameraMotion("TILT", targetTilt);

            // 명령 직전 버전을 보관해 목표 전송 직후 수신된 상태도 놓치지 않는다.
            long observedVersion = Interlocked.Read(ref _context._panTiltStatusVersion);

            bool speedResult = _context._controlCommandService.SetTiltPositionSpeed(positionSpeed);

            await Task.Delay(60, cancellationToken);

            bool commandResult = speedResult && _context._controlCommandService.TiltGoPosition(targetTilt);

            ConsoleLogHelper.Command("EO PANORAMA / MOVE", "Tilt absolute move sent / TARGET=" + targetTilt.ToString("F2") + " / SPEED=" + positionSpeed + " / SEND_RESULT=" + commandResult);

            if (!commandResult)
            {
                if (allowRetry)
                {
                    ConsoleLogHelper.Warning("EO PANORAMA / MOVE", "Tilt command send failed; retrying once / TARGET=" + targetTilt.ToString("F2"));

                    await Task.Delay(250, cancellationToken);

                    return await MoveTiltForPanoramaAsync(targetTilt, positionSpeed, cancellationToken, false);
                }

                return false;
            }

            double distance = Math.Abs(_context.GetTiltDifference(_context._currentTilt, targetTilt));

            if (distance <= PanoramaPositionTolerance)
            {
                EndPanoramaCameraMotion("TILT", targetTilt);

                ConsoleLogHelper.State("EO PANORAMA / MOVE", "Tilt target already within tolerance / TARGET=" + targetTilt.ToString("F2") + " / ACTUAL=" + _context._currentTilt.ToString("F2"));

                return true;
            }

            int stableCount = 0;

            Stopwatch stopwatch = Stopwatch.StartNew();

            int timeoutMs = Math.Max(4000, Math.Min(30000, (int)(distance / positionSpeed * 1000.0) + 3500));

            while (stopwatch.ElapsedMilliseconds < timeoutMs)
            {
                await Task.Delay(100, cancellationToken);

                long currentVersion = Interlocked.Read(ref _context._panTiltStatusVersion);

                if (currentVersion == observedVersion)
                {
                    continue;
                }

                observedVersion = currentVersion;

                double tiltDelta = Math.Abs(_context.GetTiltDifference(_context._currentTilt, targetTilt));

                stableCount = tiltDelta <= PanoramaPositionTolerance ? stableCount + 1 : 0;

                if (stableCount >= PanoramaPanStableSampleCount)
                {
                    EndPanoramaCameraMotion("TILT", targetTilt);

                    ConsoleLogHelper.State("EO PANORAMA / MOVE", "Tilt target stable / TARGET=" + targetTilt.ToString("F2") + " / ACTUAL=" + _context._currentTilt.ToString("F2") + " / ELAPSED_MS=" + stopwatch.ElapsedMilliseconds);

                    return true;
                }
            }

            ConsoleLogHelper.Warning("EO PANORAMA / MOVE", "Tilt target timeout / TARGET=" + targetTilt.ToString("F2") + " / ACTUAL=" + _context._currentTilt.ToString("F2") + " / TIMEOUT_MS=" + timeoutMs);

            if (allowRetry)
            {
                ConsoleLogHelper.Warning("EO PANORAMA / MOVE", "Tilt target timeout; retrying once / TARGET=" + targetTilt.ToString("F2"));

                await Task.Delay(250, cancellationToken);

                return await MoveTiltForPanoramaAsync(targetTilt, positionSpeed, cancellationToken, false);
            }

            return false;
        }

        internal void BeginPanoramaCameraMotion(string axis, double target)
        {
            Volatile.Write(ref _panoramaCameraMotionActive, 1);

            Interlocked.Exchange(ref _panoramaDetectionResumeUtcTicks, long.MaxValue);

            ConsoleLogHelper.State("PANORAMA DETECTION GATE", "Detection suspended / AXIS=" + axis + " / TARGET=" + target.ToString("F2"));
        }

        internal void EndPanoramaCameraMotion(string axis, double target)
        {
            Interlocked.Exchange(ref _panoramaDetectionResumeUtcTicks, DateTime.UtcNow.AddMilliseconds(PanoramaDetectionSettleMs).Ticks);

            Volatile.Write(ref _panoramaCameraMotionActive, 0);

            ConsoleLogHelper.State("PANORAMA DETECTION GATE", "Stable-frame analysis scheduled / AXIS=" + axis + " / TARGET=" + target.ToString("F2") + " / SETTLE_MS=" + PanoramaDetectionSettleMs);
        }

        internal bool IsPanoramaMotionDetectionSuppressed()
        {
            if (!IsPanoramaCaptureRunning)
            {
                return false;
            }

            return Volatile.Read(ref _panoramaCameraMotionActive) != 0 || DateTime.UtcNow.Ticks < Interlocked.Read(ref _panoramaDetectionResumeUtcTicks);
        }

        internal void ResetPanoramaDetectionGate()
        {
            Volatile.Write(ref _panoramaCameraMotionActive, 0);

            Interlocked.Exchange(ref _panoramaDetectionResumeUtcTicks, 0L);
        }

        #endregion

    }
}
