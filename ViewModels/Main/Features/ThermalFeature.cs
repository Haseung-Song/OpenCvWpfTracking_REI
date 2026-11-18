using OpenCvWpfTracking.Common;
using OpenCvWpfTracking.Services.Video;
using System;
using System.IO;
using System.Windows.Media;
using System.Windows.Input;

namespace OpenCvWpfTracking.ViewModels.Main
{
    /// <summary>열상 영상 표시 기능에 필요한 협력 기능만 받는 경계입니다. 화면 객체의 내부 구현을 알지 않습니다.</summary>
    internal interface IThermalFeatureContext
    {
        Services.Communication.IControlCommandServicePort _controlCommandService { get; }

        ref int _visionSettingsGeneration { get; }

        Services.Communication.WebAgent.IWebAgentThermalPaletteServicePort _webAgentThermalPaletteService { get; }

        bool CameraFeatureTransportReady { get; }

        void ClearDisabledVisionEvents(bool fire, bool writeAudit = true);

        string FireAlertStatusText { get; }

        bool IsControlInputLocked { get; }

        bool IsOperationCommandEnabled { get; }

        bool IsSelectedConnectionTarget { get; }

        bool IsSelectedControlAgentWebAgent { get; }

        void OnPropertyChanged([global::System.Runtime.CompilerServices.CallerMemberName] string propertyName = null);

        void ResetFireSmokeFrameAnalysis(string reason);

        void UpdateVisionBBoxEvents(string camera, string detectionType, System.Collections.Generic.IList<OpenCvSharp.Rect> candidates, System.Collections.Generic.IList<double> candidateScores, string detectionSource, Services.Position.ZoneFrameContext zoneFrame = null);
    }

    /// <summary>열상 영상 표시 기능 담당 객체입니다. 상태를 보관하고 입력을 받아 결과를 알립니다.</summary>
    internal sealed class ThermalFeature
    {

        #region [협력 객체와 초기화]

        private readonly IThermalFeatureContext _context;

        internal ThermalFeature(IThermalFeatureContext context)
        {
            _context = context ?? throw new System.ArgumentNullException(nameof(context));
        }

        internal void InitializeState()
        {
            _thermalHotThresholdRatio = 0.76;
            _thermalMinimumAreaRatio = 0.0015;
            _fireSensitivityIndex = 1;
            _fireDetectionSourceIndex = 2;
            _thermalFireBoxGroupingMode = 2;

            _thermalFireBoxMode1Background = new SolidColorBrush(Color.FromRgb(62, 81, 94));

            _thermalFireBoxMode2Background = new SolidColorBrush(Color.FromRgb(42, 111, 151));

            _thermalBlackHotButtonBackground = Brushes.WhiteSmoke;

            _thermalBlackHotButtonForeground = new SolidColorBrush(Color.FromRgb(32, 38, 45));

            _thermalWhiteHotButtonBackground = Brushes.WhiteSmoke;

            _thermalWhiteHotButtonForeground = new SolidColorBrush(Color.FromRgb(32, 38, 45));

            _thermalRainbowButtonBackground = Brushes.WhiteSmoke;

            _thermalRainbowButtonForeground = new SolidColorBrush(Color.FromRgb(32, 38, 45));

            _fireDiagnosticPathText = "OFF";
        }

        #endregion

        #region [상태 필드]

        internal readonly ThermalFireDetectionService _eoFireDetectionService = new ThermalFireDetectionService();

        internal readonly ThermalFireDetectionService _irFireDetectionService = new ThermalFireDetectionService();

        internal bool _isThermalFireDetectionEnabled;

        internal bool _isEoFireCandidateDetected;

        internal bool _isIrFireCandidateDetected;

        internal double _thermalHotThresholdRatio;

        internal double _thermalMinimumAreaRatio;

        internal int _fireSensitivityIndex;

        internal int _fireDetectionSourceIndex;

        internal int _thermalFireBoxGroupingMode;

        internal Brush _thermalFireBoxMode1Background;

        internal Brush _thermalFireBoxMode2Background;

        internal Brush _thermalBlackHotButtonBackground;

        internal Brush _thermalBlackHotButtonForeground;

        internal Brush _thermalWhiteHotButtonBackground;

        internal Brush _thermalWhiteHotButtonForeground;

        internal Brush _thermalRainbowButtonBackground;

        internal Brush _thermalRainbowButtonForeground;

        internal bool _isFireDiagnosticEnabled;

        internal string _fireDiagnosticPathText;

        #endregion

        #region [화면 속성과 이벤트]

        internal Brush ThermalBlackHotButtonBackground
        {
            get { return _thermalBlackHotButtonBackground; }

            set
            {
                _thermalBlackHotButtonBackground = value;

                _context.OnPropertyChanged();
            }
        }

        internal Brush ThermalBlackHotButtonForeground
        {
            get { return _thermalBlackHotButtonForeground; }

            set
            {
                _thermalBlackHotButtonForeground = value;

                _context.OnPropertyChanged();
            }
        }

        internal Brush ThermalWhiteHotButtonBackground
        {
            get { return _thermalWhiteHotButtonBackground; }

            set
            {
                _thermalWhiteHotButtonBackground = value;

                _context.OnPropertyChanged();
            }
        }

        internal Brush ThermalWhiteHotButtonForeground
        {
            get { return _thermalWhiteHotButtonForeground; }

            set
            {
                _thermalWhiteHotButtonForeground = value;

                _context.OnPropertyChanged();
            }
        }

        internal Brush ThermalRainbowButtonBackground
        {
            get { return _thermalRainbowButtonBackground; }

            set
            {
                _thermalRainbowButtonBackground = value;

                _context.OnPropertyChanged();
            }
        }

        internal Brush ThermalRainbowButtonForeground
        {
            get { return _thermalRainbowButtonForeground; }

            set
            {
                _thermalRainbowButtonForeground = value;

                _context.OnPropertyChanged();
            }
        }

        internal ICommand PreviousThermalPaletteCommand { get; set; }

        internal ICommand NextThermalPaletteCommand { get; set; }

        internal ICommand RequestThermalNucCommand { get; set; }

        internal ICommand SelectThermalBlackHotCommand { get; set; }

        internal ICommand SelectThermalWhiteHotCommand { get; set; }

        internal ICommand SelectThermalRandomCommand { get; set; }

        internal ICommand SelectThermalFireBoxMode1Command { get; set; }

        internal ICommand SelectThermalFireBoxMode2Command { get; set; }

        internal int ThermalFireBoxGroupingMode
        {
            get { return _thermalFireBoxGroupingMode; }
        }

        internal Brush ThermalFireBoxMode1Background
        {
            get { return _thermalFireBoxMode1Background; }

            set
            {
                _thermalFireBoxMode1Background = value;

                _context.OnPropertyChanged();
            }
        }

        internal Brush ThermalFireBoxMode2Background
        {
            get { return _thermalFireBoxMode2Background; }

            set
            {
                _thermalFireBoxMode2Background = value;

                _context.OnPropertyChanged();
            }
        }

        internal int FireDetectionSourceIndex
        {
            get { return _fireDetectionSourceIndex; }

            set
            {
                int normalized = value < 0 ? 0 : value > 2 ? 2 : value;

                if (_fireDetectionSourceIndex == normalized)
                {
                    return;
                }

                _fireDetectionSourceIndex = normalized;

                _eoFireDetectionService.Reset();

                _irFireDetectionService.Reset();

                _isEoFireCandidateDetected = false;
                _isIrFireCandidateDetected = false;

                _context.OnPropertyChanged();

                _context.OnPropertyChanged(nameof(_context.FireAlertStatusText));

                ConsoleLogHelper.State("FIRE DETECTOR", "Source mode changed / MODE=" + normalized);
            }
        }

        internal int FireSensitivityIndex
        {
            get { return _fireSensitivityIndex; }

            set
            {
                int normalized = value < 0 ? 0 : value > 2 ? 2 : value;

                if (_fireSensitivityIndex == normalized)
                {
                    return;
                }

                _fireSensitivityIndex = normalized;

                _context.OnPropertyChanged();

                if (normalized == 0)
                {
                    ThermalHotThresholdRatio = 0.68;
                    ThermalMinimumAreaRatio = 0.0010;
                }
                else if (normalized == 2)
                {
                    ThermalHotThresholdRatio = 0.82;
                    ThermalMinimumAreaRatio = 0.0030;
                }
                else
                {
                    ThermalHotThresholdRatio = 0.76;
                    ThermalMinimumAreaRatio = 0.0015;
                }

                // 프리셋 변경 시 이전 조건에서 누적된 Track을 재사용하지 않는다.
                _eoFireDetectionService.Reset();

                _irFireDetectionService.Reset();

                ConsoleLogHelper.State("FIRE DETECTOR", "Sensitivity changed / PRESET=" + (normalized == 0 ? "SENSITIVE" : normalized == 2 ? "STRICT" : "BALANCED") + " / SOURCE_MODE=" + FireDetectionSourceIndex + " / HOT_THRESHOLD=" + ThermalHotThresholdRatio.ToString("F2") + " / MIN_AREA=" + ThermalMinimumAreaRatio.ToString("F4"));
            }
        }

        internal bool IsFireDiagnosticEnabled
        {
            get { return _isFireDiagnosticEnabled; }

            set
            {
                if (_isFireDiagnosticEnabled == value)
                {
                    return;
                }

                if (value)
                {
                    string sessionDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "FireDiagnostics", "Viewer_" + DateTime.Now.ToString("yyyyMMdd_HHmmss"));

                    try
                    {
                        _eoFireDetectionService.StartDiagnostic(Path.Combine(sessionDirectory, "EO"), "EO");

                        _irFireDetectionService.StartDiagnostic(Path.Combine(sessionDirectory, "IR"), "IR");

                        _isFireDiagnosticEnabled = true;
                        FireDiagnosticPathText = sessionDirectory;
                    }
                    catch (Exception exception)
                    {
                        _eoFireDetectionService.StopDiagnostic();

                        _irFireDetectionService.StopDiagnostic();

                        _isFireDiagnosticEnabled = false;

                        FireDiagnosticPathText = "ERROR : " + exception.Message;

                        ConsoleLogHelper.Error("FIRE DIAGNOSTIC", "Live diagnostic start failed", exception);
                    }
                }
                else
                {
                    _eoFireDetectionService.StopDiagnostic();

                    _irFireDetectionService.StopDiagnostic();

                    _isFireDiagnosticEnabled = false;
                    FireDiagnosticPathText = "OFF";
                }

                _context.OnPropertyChanged();
            }
        }

        internal string FireDiagnosticPathText
        {
            get { return _fireDiagnosticPathText; }

            set
            {
                if (_fireDiagnosticPathText == value)
                {
                    return;
                }

                _fireDiagnosticPathText = value;

                _context.OnPropertyChanged();
            }
        }

        internal bool IsThermalFireDetectionEnabled
        {
            get { return _isThermalFireDetectionEnabled; }

            set
            {
                if (_isThermalFireDetectionEnabled == value)
                {
                    return;
                }

                _isThermalFireDetectionEnabled = value;

                System.Threading.Interlocked.Increment(ref _context._visionSettingsGeneration);

                if (!value)
                {
                    _context.ClearDisabledVisionEvents(true);
                }

                _context.OnPropertyChanged();

                _context.OnPropertyChanged(nameof(FirePowerStatusText));

                _context.OnPropertyChanged(nameof(_context.FireAlertStatusText));

                // Detector와 진단 수집을 동일한 운용 스위치로 유지한다.
                IsFireDiagnosticEnabled = value;

                ConsoleLogHelper.State("THERMAL FIRE", value ? "Experimental candidate detection enabled" : "Experimental candidate detection disabled");
            }
        }

        internal double ThermalHotThresholdRatio
        {
            get { return _thermalHotThresholdRatio; }

            set
            {
                double normalized = value < 0 ? 0 : value > 1 ? 1 : value;

                if (System.Math.Abs(_thermalHotThresholdRatio - normalized) < 0.001)
                {
                    return;
                }

                _thermalHotThresholdRatio = normalized;

                _context.OnPropertyChanged();
            }
        }

        internal double ThermalMinimumAreaRatio
        {
            get { return _thermalMinimumAreaRatio; }

            set
            {
                double normalized = value < 0.001 ? 0.001 : value > 0.25 ? 0.25 : value;

                if (System.Math.Abs(_thermalMinimumAreaRatio - normalized) < 0.0001)
                {
                    return;
                }

                _thermalMinimumAreaRatio = normalized;

                _context.OnPropertyChanged();
            }
        }

        internal string FirePowerStatusText
        {
            get
            {
                if (IsThermalFireDetectionEnabled)
                {
                    return "ON";
                }
                else
                {
                    return "OFF";
                }
            }
        }

        #endregion

        #region [동작 처리]

        internal bool IsFireSourceEnabled(bool isInfrared)
        {
            return FireDetectionSourceIndex == 0 || (FireDetectionSourceIndex == 1 && !isInfrared) || (FireDetectionSourceIndex == 2 && isInfrared);
        }

        internal void InitializeThermalFeatures()
        {
            PreviousThermalPaletteCommand = new RelayCommand(() => ChangeThermalPalette(-1));

            NextThermalPaletteCommand = new RelayCommand(() => ChangeThermalPalette(1));

            RequestThermalNucCommand = new RelayCommand(() => SendThermalNucCommand());

            SelectThermalBlackHotCommand = new RelayCommand(() => SendThermalPaletteDirectCommand("BLACK HOT", 0));

            SelectThermalWhiteHotCommand = new RelayCommand(() => SendThermalPaletteDirectCommand("WHITE HOT", 1));

            // 2026-08-14: RANDOM은 지원되지 않는 직접 RAINBOW 명령 대신 NEXT를 1회 전송한다.
            SelectThermalRandomCommand = new RelayCommand(() => ChangeThermalPalette(1, true));

            SelectThermalFireBoxMode1Command = new RelayCommand(() => SetThermalFireBoxGroupingMode(1));

            SelectThermalFireBoxMode2Command = new RelayCommand(() => SetThermalFireBoxGroupingMode(2));
        }

        internal void SetThermalFireBoxGroupingMode(int mode)
        {
            _thermalFireBoxGroupingMode = mode == 1 ? 1 : 2;

            ThermalFireBoxMode1Background = new SolidColorBrush(_thermalFireBoxGroupingMode == 1 ? Color.FromRgb(42, 111, 151) : Color.FromRgb(62, 81, 94));

            ThermalFireBoxMode2Background = new SolidColorBrush(_thermalFireBoxGroupingMode == 2 ? Color.FromRgb(42, 111, 151) : Color.FromRgb(62, 81, 94));

            _context.OnPropertyChanged(nameof(ThermalFireBoxGroupingMode));

            ConsoleLogHelper.State("THERMAL FIRE", "BBox grouping mode=" + _thermalFireBoxGroupingMode);
        }

        internal void SendThermalPaletteDirectCommand(string paletteName, int paletteType)
        {
            // 표시 중인 상태 탭이 아니라 실제 선택·연결된 제어 장비로 경로를 정한다.
            if (!_context.CameraFeatureTransportReady || !_context.IsSelectedConnectionTarget || !_context.IsOperationCommandEnabled || _context.IsControlInputLocked)
            {
                return;
            }

            bool result = _context.IsSelectedControlAgentWebAgent ? (paletteType == 0 ? _context._webAgentThermalPaletteService.SelectBlackHot() : paletteType == 1 ? _context._webAgentThermalPaletteService.SelectWhiteHot() : _context._webAgentThermalPaletteService.SelectRainbow()) : (paletteType == 0 ? _context._controlCommandService.SelectIrBlackHot() : paletteType == 1 ? _context._controlCommandService.SelectIrWhiteHot() : _context._controlCommandService.SelectIrRainbow());

            LogThermalControlCommandResult("PALETTE " + paletteName, result);

            if (result)
            {
                UpdateThermalPaletteButtonVisual(paletteType);

                _context.ResetFireSmokeFrameAnalysis("PALETTE " + paletteName);
            }
        }

        internal void SendThermalNucCommand()
        {
            if (!_context.CameraFeatureTransportReady || !_context.IsSelectedConnectionTarget || !_context.IsOperationCommandEnabled || _context.IsControlInputLocked)
            {
                return;
            }

            // 선택 장비의 단발 NUC 요청만 보내며 구형 요청으로 자동 대체하지 않는다.
            System.Func<bool> sendCommand;

            if (_context.IsSelectedControlAgentWebAgent)
            {
                sendCommand = _context._webAgentThermalPaletteService.RequestNuc;
            }
            else
            {
                sendCommand = _context._controlCommandService.RequestIrNuc;
            }

            SendThermalCommand("NUC", sendCommand);

            _context.ResetFireSmokeFrameAnalysis("NUC");
        }

        internal void InitializeThermalBlackHotAfterDeviceConnected()
        {
            // IR 카메라에 남아 있는 이전 실행의 팔레트를 재사용하지 않는다.
            // 새 장비 연결마다 검정 고온 팔레트로 시작한다.
            SendThermalPaletteDirectCommand("INITIAL BLACK HOT", 0);
        }

        internal void ChangeThermalPalette(int offset, bool highlightRandomButton = false)
        {
            if (!_context.CameraFeatureTransportReady || !_context.IsSelectedConnectionTarget || !_context.IsOperationCommandEnabled || _context.IsControlInputLocked)
            {
                return;
            }

            if (_context.IsSelectedControlAgentWebAgent)
            {
                if (highlightRandomButton)
                {
                    SendThermalPaletteDirectCommand("RAINBOW", 2);

                    return;
                }

                bool environmentResult = offset < 0 ? _context._webAgentThermalPaletteService.SelectPrevious() : _context._webAgentThermalPaletteService.SelectNext();

                LogThermalControlCommandResult(offset < 0 ? "ENVIRONMENT PALETTE PREV" : "ENVIRONMENT PALETTE NEXT", environmentResult);

                if (environmentResult)
                {
                    ResetThermalPaletteButtonVisuals();

                    _context.ResetFireSmokeFrameAnalysis("ENVIRONMENT PALETTE CHANGE");
                }

                return;
            }

            bool result = offset < 0 ? _context._controlCommandService.SelectPreviousIrPalette() : _context._controlCommandService.SelectNextIrPalette();

            LogThermalControlCommandResult(offset < 0 ? "PALETTE PREV" : "PALETTE NEXT", result);

            if (result)
            {
                if (highlightRandomButton)
                {
                    UpdateThermalPaletteButtonVisual(2);
                }
                else
                {
                    ResetThermalPaletteButtonVisuals();
                }

                _context.ResetFireSmokeFrameAnalysis("ROOFTOP PALETTE CHANGE");
            }
        }

        internal void UpdateThermalPaletteButtonVisual(int paletteType)
        {
            ResetThermalPaletteButtonVisuals();

            if (paletteType == 0)
            {
                ThermalBlackHotButtonBackground = Brushes.Black;
                ThermalBlackHotButtonForeground = Brushes.White;
            }
            else if (paletteType == 1)
            {
                ThermalWhiteHotButtonBackground = Brushes.White;
            }
            else
            {
                LinearGradientBrush rainbow = new LinearGradientBrush
                {
                    StartPoint = new System.Windows.Point(0, 0),
                    EndPoint = new System.Windows.Point(1, 0)
                };

                rainbow.GradientStops.Add(new GradientStop(Colors.Red, 0));

                rainbow.GradientStops.Add(new GradientStop(Colors.Yellow, 0.35));

                rainbow.GradientStops.Add(new GradientStop(Colors.LimeGreen, 0.60));

                rainbow.GradientStops.Add(new GradientStop(Colors.DodgerBlue, 0.82));

                rainbow.GradientStops.Add(new GradientStop(Colors.MediumPurple, 1));

                ThermalRainbowButtonBackground = rainbow;
                ThermalRainbowButtonForeground = Brushes.White;
            }
        }

        internal void ResetThermalPaletteButtonVisuals()
        {
            Brush neutralForeground = new SolidColorBrush(Color.FromRgb(32, 38, 45));

            ThermalBlackHotButtonBackground = Brushes.WhiteSmoke;
            ThermalBlackHotButtonForeground = neutralForeground;
            ThermalWhiteHotButtonBackground = Brushes.WhiteSmoke;
            ThermalWhiteHotButtonForeground = neutralForeground;
            ThermalRainbowButtonBackground = Brushes.WhiteSmoke;
            ThermalRainbowButtonForeground = neutralForeground;
        }

        internal static void LogThermalControlCommandResult(string commandName, bool result)
        {
            if (result)
            {
                ConsoleLogHelper.Info("THERMAL CONTROL", "WRITE SUCCESS / COMMAND=" + commandName);
            }
            else
            {
                ConsoleLogHelper.Warning("THERMAL CONTROL", "WRITE FAILED / COMMAND=" + commandName);
            }
        }

        internal static void SendThermalCommand(string commandName, System.Func<bool> sendCommand)
        {
            bool result = sendCommand();

            if (result)
            {
                ConsoleLogHelper.Info("THERMAL CONTROL", "CONTROL AGENT TCP WRITE SUCCESS / " + "DEVICE APPLY UNCONFIRMED / COMMAND=" + commandName + " / VERIFY=CONTROL AGENT AND IR CAMERA");
            }
            else
            {
                ConsoleLogHelper.Warning("THERMAL CONTROL", "CONTROL AGENT TCP WRITE FAILED / COMMAND=" + commandName);
            }
        }

        internal void UpdateThermalFireCandidateState(string camera, ThermalFireDetectionResult result, OpenCvWpfTracking.Services.Position.ZoneFrameContext zoneFrame = null)
        {
            if (!IsThermalFireDetectionEnabled)
            {
                result = default(ThermalFireDetectionResult);
            }

            bool isInfrared = string.Equals(camera, "IR", System.StringComparison.OrdinalIgnoreCase);

            bool previousState = isInfrared ? _isIrFireCandidateDetected : _isEoFireCandidateDetected;

            if (previousState != result.IsDetected && isInfrared)
            {
                _isIrFireCandidateDetected = result.IsDetected;
            }
            else if (previousState != result.IsDetected)
            {
                _isEoFireCandidateDetected = result.IsDetected;
            }

            if (previousState != result.IsDetected)
            {
                _context.OnPropertyChanged(nameof(FirePowerStatusText));

                _context.OnPropertyChanged(nameof(_context.FireAlertStatusText));
            }

            _context.UpdateVisionBBoxEvents(camera, "FIRE", result.CandidateRects, result.CandidateScores, "IMAGE PROCESSING", zoneFrame);
        }

        #endregion

    }
}
