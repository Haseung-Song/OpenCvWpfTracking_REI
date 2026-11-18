using OpenCvSharp;
using OpenCvWpfTracking.Common;
using OpenCvWpfTracking.Models.AI;
using OpenCvWpfTracking.Services.Video;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Input;
using System.Windows.Media;
using static OpenCvWpfTracking.ViewModels.Main.MainViewModel;

namespace OpenCvWpfTracking.ViewModels.Main
{
    /// <summary>연기 분석 설정에 필요한 협력 기능만 받는 경계입니다. 화면 객체의 내부 구현을 알지 않습니다.</summary>
    internal interface ISmokeFeatureContext
    {
        ref double? _activePanAbsoluteTarget { get; }

        ref double? _activeTiltAbsoluteTarget { get; }

        ContinuousMoveType _currentMoveType { get; set; }

        ThermalFireDetectionService _eoFireDetectionService { get; }

        ThermalFireDetectionService _irFireDetectionService { get; }

        ref int _visionSettingsGeneration { get; }

        double AiDisplayConfidenceThreshold { get; set; }

        string AiPowerStatusText { get; set; }

        void ClearDisabledVisionEvents(bool fire, bool writeAudit = true);

        string FireAlertStatusText { get; }

        int GetCurrentIrZoomStandardPosition();

        ushort GetCurrentPresetStandardZoom();

        bool IsAutoTrackingEnabled { get; set; }

        bool IsControlInputLocked { get; }

        bool IsLaPresetScanRunning { get; set; }

        bool IsPanoramaCaptureRunning { get; set; }

        bool IsPanoramaMotionDetectionSuppressed();

        bool IsPanoramaProcessingRunning { get; }

        bool IsPresetScanRunning { get; set; }

        bool IsThermalFireDetectionEnabled { get; set; }

        void OnPropertyChanged([global::System.Runtime.CompilerServices.CallerMemberName] string propertyName = null);

        bool TryRunAiUiAction(Action action, string operationName);
    }

    /// <summary>연기 분석 설정 담당 객체입니다. 상태를 보관하고 입력을 받아 결과를 알립니다.</summary>
    internal sealed class SmokeFeature
    {

        #region [협력 객체와 초기화]

        private readonly ISmokeFeatureContext _context;

        internal SmokeFeature(ISmokeFeatureContext context)
        {
            _context = context ?? throw new System.ArgumentNullException(nameof(context));
        }

        internal void InitializeState()
        {
            _latestEoAiSmokeCandidateTime = DateTime.MinValue;
            _latestIrAiSmokeCandidateTime = DateTime.MinValue;
            _latestEoAiFireCandidateTime = DateTime.MinValue;
            _latestIrAiFireCandidateTime = DateTime.MinValue;
            _latestEoAiVehicleCandidateTime = DateTime.MinValue;
            _latestIrAiVehicleCandidateTime = DateTime.MinValue;
            _smokeDetectionSourceIndex = 1;
            _smokeSensitivityIndex = 1;
            _smokeMinimumAreaRatio = 0.0015;
            _smokeChangeThresholdRatio = 0.035;
            _smokeDiagnosticPathText = "OFF";
            _smokeBoxGroupingMode = 2;

            _smokeBoxMode1Background = new SolidColorBrush(Color.FromRgb(62, 81, 94));

            _smokeBoxMode2Background = new SolidColorBrush(Color.FromRgb(42, 111, 151));
        }

        #endregion

        #region [상태 필드]

        internal readonly SmokeDetectionService _eoSmokeDetectionService = new SmokeDetectionService();

        internal readonly SmokeDetectionService _irSmokeDetectionService = new SmokeDetectionService();

        internal bool _wasMotionCompensatedSmokeAnalysis;

        internal readonly object _aiSmokeCandidateSync = new object();

        internal readonly List<Rect> _latestEoAiSmokeCandidates = new List<Rect>();

        internal readonly List<Rect> _latestIrAiSmokeCandidates = new List<Rect>();

        internal readonly List<Rect> _latestEoAiFireCandidates = new List<Rect>();

        internal readonly List<Rect> _latestIrAiFireCandidates = new List<Rect>();

        internal readonly List<Rect> _latestEoAiVehicleCandidates = new List<Rect>();

        internal readonly List<Rect> _latestIrAiVehicleCandidates = new List<Rect>();

        internal DateTime _latestEoAiSmokeCandidateTime;

        internal DateTime _latestIrAiSmokeCandidateTime;

        internal DateTime _latestEoAiFireCandidateTime;

        internal DateTime _latestIrAiFireCandidateTime;

        internal DateTime _latestEoAiVehicleCandidateTime;

        internal DateTime _latestIrAiVehicleCandidateTime;

        internal bool _isSmokeDetectionEnabled;

        internal int _smokeDetectionSourceIndex;

        internal int _smokeSensitivityIndex;

        internal double _smokeMinimumAreaRatio;

        internal double _smokeChangeThresholdRatio;

        internal bool _isSmokeDiagnosticEnabled;

        internal string _smokeDiagnosticPathText;

        internal int _smokeBoxGroupingMode;

        internal Brush _smokeBoxMode1Background;

        internal Brush _smokeBoxMode2Background;

        #endregion

        #region [화면 속성과 이벤트]

        internal ICommand SelectSmokeBoxMode1Command { get; set; }

        internal ICommand SelectSmokeBoxMode2Command { get; set; }

        internal int SmokeBoxGroupingMode
        {
            get { return _smokeBoxGroupingMode; }
        }

        internal Brush SmokeBoxMode1Background
        {
            get { return _smokeBoxMode1Background; }

            set
            {
                _smokeBoxMode1Background = value;

                _context.OnPropertyChanged();
            }
        }

        internal Brush SmokeBoxMode2Background
        {
            get { return _smokeBoxMode2Background; }

            set
            {
                _smokeBoxMode2Background = value;

                _context.OnPropertyChanged();
            }
        }

        internal bool IsSmokeDetectionEnabled
        {
            get { return _isSmokeDetectionEnabled; }

            set
            {
                if (_isSmokeDetectionEnabled == value)
                {
                    return;
                }

                _isSmokeDetectionEnabled = value;

                System.Threading.Interlocked.Increment(ref _context._visionSettingsGeneration);

                if (!value)
                {
                    _context.ClearDisabledVisionEvents(false);
                }

                _context.OnPropertyChanged();

                _context.OnPropertyChanged(nameof(SmokePowerStatusText));

                _context.OnPropertyChanged(nameof(_context.FireAlertStatusText));

                // Detector와 진단 수집을 동일한 운용 스위치로 유지한다.
                IsSmokeDiagnosticEnabled = value;

                ConsoleLogHelper.State("SMOKE DETECTOR", value ? "Smoke candidate detection enabled" : "Smoke candidate detection disabled");
            }
        }

        internal string SmokePowerStatusText
        {
            get
            {
                if (IsSmokeDetectionEnabled)
                {
                    return "ON";
                }
                else
                {
                    return "OFF";
                }
            }
        }

        internal bool IsSmokeDiagnosticEnabled
        {
            get { return _isSmokeDiagnosticEnabled; }

            set
            {
                if (_isSmokeDiagnosticEnabled == value)
                {
                    return;
                }

                if (value)
                {
                    string sessionDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "SmokeDiagnostics", "Viewer_" + DateTime.Now.ToString("yyyyMMdd_HHmmss"));

                    try
                    {
                        _eoSmokeDetectionService.StartDiagnostic(Path.Combine(sessionDirectory, "EO"), "EO");

                        _irSmokeDetectionService.StartDiagnostic(Path.Combine(sessionDirectory, "IR"), "IR");

                        _isSmokeDiagnosticEnabled = true;
                        SmokeDiagnosticPathText = sessionDirectory;
                    }
                    catch (Exception exception)
                    {
                        _eoSmokeDetectionService.StopDiagnostic();

                        _irSmokeDetectionService.StopDiagnostic();

                        _isSmokeDiagnosticEnabled = false;

                        SmokeDiagnosticPathText = "ERROR : " + exception.Message;

                        ConsoleLogHelper.Error("SMOKE DIAGNOSTIC", "Live diagnostic start failed", exception);
                    }
                }
                else
                {
                    _eoSmokeDetectionService.StopDiagnostic();

                    _irSmokeDetectionService.StopDiagnostic();

                    _isSmokeDiagnosticEnabled = false;
                    SmokeDiagnosticPathText = "OFF";
                }

                _context.OnPropertyChanged();
            }
        }

        internal string SmokeDiagnosticPathText
        {
            get { return _smokeDiagnosticPathText; }

            set
            {
                if (string.Equals(_smokeDiagnosticPathText, value, StringComparison.Ordinal))
                {
                    return;
                }

                _smokeDiagnosticPathText = value;

                _context.OnPropertyChanged();
            }
        }

        internal int SmokeDetectionSourceIndex
        {
            get { return _smokeDetectionSourceIndex; }

            set
            {
                int normalized = value < 0 ? 0 : value > 2 ? 2 : value;

                if (_smokeDetectionSourceIndex == normalized)
                {
                    return;
                }

                _smokeDetectionSourceIndex = normalized;

                _context.OnPropertyChanged();

                ConsoleLogHelper.State("SMOKE DETECTOR", "Source mode changed / MODE=" + normalized);
            }
        }

        internal int SmokeSensitivityIndex
        {
            get { return _smokeSensitivityIndex; }

            set
            {
                int normalized = value < 0 ? 0 : value > 2 ? 2 : value;

                if (_smokeSensitivityIndex == normalized)
                {
                    return;
                }

                _smokeSensitivityIndex = normalized;

                _context.OnPropertyChanged();

                if (normalized == 0)
                {
                    SmokeMinimumAreaRatio = 0.0005;
                    SmokeChangeThresholdRatio = 0.015;
                }
                else if (normalized == 2)
                {
                    SmokeMinimumAreaRatio = 0.0030;
                    SmokeChangeThresholdRatio = 0.055;
                }
                else
                {
                    SmokeMinimumAreaRatio = 0.0015;
                    SmokeChangeThresholdRatio = 0.035;
                }

                ConsoleLogHelper.State("SMOKE DETECTOR", "Sensitivity changed / PRESET=" + normalized);
            }
        }

        internal double SmokeMinimumAreaRatio
        {
            get { return _smokeMinimumAreaRatio; }

            set
            {
                double normalized = Math.Max(0.0005, Math.Min(0.05, value));

                if (Math.Abs(_smokeMinimumAreaRatio - normalized) < 0.0001)
                {
                    return;
                }

                _smokeMinimumAreaRatio = normalized;

                _context.OnPropertyChanged();
            }
        }

        internal double SmokeChangeThresholdRatio
        {
            get { return _smokeChangeThresholdRatio; }

            set
            {
                double normalized = Math.Max(0.015, Math.Min(0.15, value));

                if (Math.Abs(_smokeChangeThresholdRatio - normalized) < 0.001)
                {
                    return;
                }

                _smokeChangeThresholdRatio = normalized;

                _context.OnPropertyChanged();
            }
        }

        #endregion

        #region [동작 처리]

        internal void InitializeSmokeFeatures()
        {
            SelectSmokeBoxMode1Command = new RelayCommand(() => SetSmokeBoxGroupingMode(1));

            SelectSmokeBoxMode2Command = new RelayCommand(() => SetSmokeBoxGroupingMode(2));
        }

        internal void SetSmokeBoxGroupingMode(int mode)
        {
            _smokeBoxGroupingMode = mode == 1 ? 1 : 2;

            SmokeBoxMode1Background = new SolidColorBrush(_smokeBoxGroupingMode == 1 ? Color.FromRgb(42, 111, 151) : Color.FromRgb(62, 81, 94));

            SmokeBoxMode2Background = new SolidColorBrush(_smokeBoxGroupingMode == 2 ? Color.FromRgb(42, 111, 151) : Color.FromRgb(62, 81, 94));

            _context.OnPropertyChanged(nameof(SmokeBoxGroupingMode));

            ConsoleLogHelper.State("SMOKE DETECTOR", "BBox grouping mode changed / MODE=" + _smokeBoxGroupingMode);
        }

        internal void UpdateAiSmokeCandidateSnapshot(AiDetectionResult result, DateTime receiveTime)
        {
            if (result == null || result.Boxes == null || (result.RtspIndex != 0 && result.RtspIndex != 1))
            {
                return;
            }

            List<Rect> snapshot = new List<Rect>();

            List<Rect> fireSnapshot = new List<Rect>();

            List<Rect> vehicleSnapshot = new List<Rect>();

            foreach (AiDetectionBox box in result.Boxes)
            {
                if (box.NormalizedConfidence < _context.AiDisplayConfidenceThreshold || box.Width <= 0 || box.Height <= 0)
                {
                    continue;
                }

                Rect rectangle = new Rect(box.Left, box.Top, box.Width, box.Height);

                if (box.ClassName.IndexOf("smoke", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    snapshot.Add(rectangle);
                }

                if (box.ClassName.IndexOf("fire", StringComparison.OrdinalIgnoreCase) >= 0 || box.ClassName.IndexOf("flame", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    fireSnapshot.Add(rectangle);
                }

                if (IsAiVehicleClass(box.ClassName))
                {
                    vehicleSnapshot.Add(rectangle);
                }
            }

            lock (_aiSmokeCandidateSync)
            {
                List<Rect> target = result.RtspIndex == 0 ? _latestEoAiSmokeCandidates : _latestIrAiSmokeCandidates;

                target.Clear();

                target.AddRange(snapshot);

                List<Rect> fireTarget = result.RtspIndex == 0 ? _latestEoAiFireCandidates : _latestIrAiFireCandidates;

                fireTarget.Clear();

                fireTarget.AddRange(fireSnapshot);

                if (result.RtspIndex == 0)
                {
                    _latestEoAiSmokeCandidateTime = receiveTime;
                    _latestEoAiFireCandidateTime = receiveTime;

                    if (vehicleSnapshot.Count > 0)
                    {
                        _latestEoAiVehicleCandidates.Clear();

                        _latestEoAiVehicleCandidates.AddRange(vehicleSnapshot);

                        _latestEoAiVehicleCandidateTime = receiveTime;
                    }
                }
                else
                {
                    _latestIrAiSmokeCandidateTime = receiveTime;
                    _latestIrAiFireCandidateTime = receiveTime;

                    if (vehicleSnapshot.Count > 0)
                    {
                        _latestIrAiVehicleCandidates.Clear();

                        _latestIrAiVehicleCandidates.AddRange(vehicleSnapshot);

                        _latestIrAiVehicleCandidateTime = receiveTime;
                    }
                }
            }
        }

        internal static bool IsAiVehicleClass(string className)
        {
            if (string.IsNullOrWhiteSpace(className))
            {
                return false;
            }

            string normalized = className.Trim().ToLowerInvariant();

            return normalized == "car" || normalized == "truck" || normalized == "bus" || normalized == "van" || normalized == "vehicle" || normalized == "motorcycle" || normalized == "motorbike" || normalized == "automobile";
        }

        internal IList<Rect> GetRecentAiSmokeCandidates(bool isInfrared)
        {
            lock (_aiSmokeCandidateSync)
            {
                DateTime smokeTimestamp = isInfrared ? _latestIrAiSmokeCandidateTime : _latestEoAiSmokeCandidateTime;

                DateTime fireTimestamp = isInfrared ? _latestIrAiFireCandidateTime : _latestEoAiFireCandidateTime;

                List<Rect> hybridExclusions = new List<Rect>();

                if ((DateTime.Now - smokeTimestamp).TotalSeconds <= 2.0)
                {
                    hybridExclusions.AddRange(isInfrared ? _latestIrAiSmokeCandidates : _latestEoAiSmokeCandidates);
                }

                // 2026-09-02: AI가 FIRE로 분류한 동일 현상 주변에도 자체 SMOKE를
                // 중복 표시하지 않는다. AI가 놓친 떨어진 플룸만 영상처리가 보완한다.
                if ((DateTime.Now - fireTimestamp).TotalSeconds <= 2.0)
                {
                    hybridExclusions.AddRange(isInfrared ? _latestIrAiFireCandidates : _latestEoAiFireCandidates);
                }

                return hybridExclusions;
            }
        }

        internal IList<Rect> GetRecentAiVehicleCandidates(bool isInfrared)
        {
            lock (_aiSmokeCandidateSync)
            {
                DateTime timestamp = isInfrared ? _latestIrAiVehicleCandidateTime : _latestEoAiVehicleCandidateTime;

                if ((DateTime.Now - timestamp).TotalSeconds > 2.0)
                {
                    return new List<Rect>();
                }

                return new List<Rect>(isInfrared ? _latestIrAiVehicleCandidates : _latestEoAiVehicleCandidates);
            }
        }

        internal IList<Rect> GetRecentAiFireCandidates(bool isInfrared)
        {
            lock (_aiSmokeCandidateSync)
            {
                DateTime timestamp = isInfrared ? _latestIrAiFireCandidateTime : _latestEoAiFireCandidateTime;

                if ((DateTime.Now - timestamp).TotalSeconds > 2.0)
                {
                    return new List<Rect>();
                }

                return new List<Rect>(isInfrared ? _latestIrAiFireCandidates : _latestEoAiFireCandidates);
            }
        }

        internal void ClearAiSmokeCandidateSnapshots()
        {
            lock (_aiSmokeCandidateSync)
            {
                _latestEoAiSmokeCandidates.Clear();

                _latestIrAiSmokeCandidates.Clear();

                _latestEoAiFireCandidates.Clear();

                _latestIrAiFireCandidates.Clear();

                _latestEoAiVehicleCandidates.Clear();

                _latestIrAiVehicleCandidates.Clear();

                _latestEoAiSmokeCandidateTime = DateTime.MinValue;
                _latestIrAiSmokeCandidateTime = DateTime.MinValue;
                _latestEoAiFireCandidateTime = DateTime.MinValue;
                _latestIrAiFireCandidateTime = DateTime.MinValue;
                _latestEoAiVehicleCandidateTime = DateTime.MinValue;
                _latestIrAiVehicleCandidateTime = DateTime.MinValue;
            }
        }

        internal void SetFireSmokeDetectorsForAiConnection(bool enabled, string reason)
        {
            _context.TryRunAiUiAction(() =>
            {
                // AI 통신 상태가 사용자의 검출 확인란을 변경하지 않는다.
                if (!enabled)
                {
                    _context.IsAutoTrackingEnabled = false;

                    ClearAiSmokeCandidateSnapshots();
                }
            }, "FIRE/SMOKE detector connection sync");

            ConsoleLogHelper.State("FIRE / SMOKE", "AI connection mode changed / AI=" + (enabled ? "ON" : "OFF") + " / VIDEO_FIRE=" + _context.IsThermalFireDetectionEnabled + " / VIDEO_SMOKE=" + IsSmokeDetectionEnabled + " / REASON=" + reason);
        }

        internal void ResetFireSmokeFrameAnalysis(string reason)
        {
            _eoSmokeDetectionService.Reset();

            _irSmokeDetectionService.Reset();

            _context._eoFireDetectionService.Reset();

            _context._irFireDetectionService.Reset();

            ConsoleLogHelper.State("FIRE / SMOKE", "Frame analysis reset / REASON=" + reason);
        }

        internal bool IsFireSmokeFrameAnalysisAllowed()
        {
            if (_context.IsPanoramaCaptureRunning)
            {
                // 실제 PTZ 이동 및 도착 직후에는 FIRE/SMOKE 상태를 초기화하고,
                // 정지 영상이 확보된 구간에서만 다시 분석한다.
                return !_context.IsPanoramaMotionDetectionSuppressed();
            }

            if (_context.IsLaPresetScanRunning || _context.IsPresetScanRunning || _context.IsPanoramaProcessingRunning)
            {
                return true;
            }

            return !_context.IsControlInputLocked && _context._currentMoveType == ContinuousMoveType.None && !_context._activePanAbsoluteTarget.HasValue && !_context._activeTiltAbsoluteTarget.HasValue;
        }

        internal bool IsAutomaticCameraMotionActive()
        {
            return _context.IsLaPresetScanRunning || _context.IsPresetScanRunning;
        }

        internal SmokeDetectionResult ProcessSmokeFrame(Mat frame, string streamName, Rect fireCandidateRect)
        {
            bool isInfrared = string.Equals(streamName, "IR", StringComparison.OrdinalIgnoreCase);

            bool sourceEnabled = SmokeDetectionSourceIndex == 0 || (SmokeDetectionSourceIndex == 1 && !isInfrared) || (SmokeDetectionSourceIndex == 2 && isInfrared);

            SmokeDetectionService service = isInfrared ? _irSmokeDetectionService : _eoSmokeDetectionService;

            bool compensateCameraMotion = IsAutomaticCameraMotionActive();

            if (_wasMotionCompensatedSmokeAnalysis != compensateCameraMotion)
            {
                _wasMotionCompensatedSmokeAnalysis = compensateCameraMotion;

                ConsoleLogHelper.State("SMOKE DETECTOR", "Camera motion compensation " + (compensateCameraMotion ? "enabled" : "disabled") + " / AUTO_SCAN_L=" + _context.IsLaPresetScanRunning + " / AUTO_SCAN_W=" + _context.IsPresetScanRunning + " / PANORAMA=" + _context.IsPanoramaCaptureRunning);
            }

            if (!IsFireSmokeFrameAnalysisAllowed())
            {
                SmokeDetectionResult disabledResult = service.Process(frame, false, isInfrared, SmokeMinimumAreaRatio, SmokeChangeThresholdRatio, Rect.Empty, new List<Rect>(), new List<Rect>(), SmokeBoxGroupingMode, false, _context.AiPowerStatusText == "ON", isInfrared ? _context.GetCurrentIrZoomStandardPosition() : _context.GetCurrentPresetStandardZoom());

                return disabledResult;
            }

            SmokeDetectionResult result = service.Process(frame, IsSmokeDetectionEnabled && sourceEnabled, isInfrared, SmokeMinimumAreaRatio, SmokeChangeThresholdRatio, fireCandidateRect, GetRecentAiSmokeCandidates(isInfrared), GetRecentAiVehicleCandidates(isInfrared), SmokeBoxGroupingMode, compensateCameraMotion, _context.AiPowerStatusText == "ON", isInfrared ? _context.GetCurrentIrZoomStandardPosition() : _context.GetCurrentPresetStandardZoom());

            return result;
        }

        #endregion

    }
}
