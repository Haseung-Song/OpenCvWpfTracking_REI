using OpenCvWpfTracking.Common;
using OpenCvWpfTracking.Services.Video;
using System;
using System.IO;
using System.Windows.Media;
using System.Linq;
using System.Windows.Input;

namespace OpenCvWpfTracking.ViewModels.Main
{
    public partial class MainViewModel
    {
        // 2026-08-27: 실제 Viewer의 EO와 IR 프레임을 각각 독립 추적한다.
        // 한 Service를 공유하면 채널 전환 시 이전 프레임과 상태가 섞이므로 분리한다.
        private readonly ThermalFireDetectionService _eoFireDetectionService =
            new ThermalFireDetectionService();
        private readonly ThermalFireDetectionService _irFireDetectionService =
            new ThermalFireDetectionService();

        private bool _isThermalFireDetectionEnabled;
        private bool _isEoFireCandidateDetected;
        private bool _isIrFireCandidateDetected;
        // 2026-08-14: 컬러/흑백 IR 시험 영상 공통 초기값.
        private double _thermalHotThresholdRatio = 0.72;
        private double _thermalMinimumAreaRatio = 0.0015;
        // 2026-08-14: 1=전체 화염 단일 BBox, 2=분리 화염별 BBox(기본값).
        private int _thermalFireBoxGroupingMode = 2;
        private Brush _thermalFireBoxMode1Background = new SolidColorBrush(Color.FromRgb(62, 81, 94));
        private Brush _thermalFireBoxMode2Background = new SolidColorBrush(Color.FromRgb(42, 111, 151));

        // 2026-08-14: Direct palette buttons remain neutral until a command succeeds.
        private Brush _thermalBlackHotButtonBackground = Brushes.WhiteSmoke;
        private Brush _thermalBlackHotButtonForeground = new SolidColorBrush(Color.FromRgb(32, 38, 45));
        private Brush _thermalWhiteHotButtonBackground = Brushes.WhiteSmoke;
        private Brush _thermalWhiteHotButtonForeground = new SolidColorBrush(Color.FromRgb(32, 38, 45));
        private Brush _thermalRainbowButtonBackground = Brushes.WhiteSmoke;
        private Brush _thermalRainbowButtonForeground = new SolidColorBrush(Color.FromRgb(32, 38, 45));
        private bool _isFireDiagnosticEnabled;
        private string _fireDiagnosticPathText = "OFF";

        public Brush ThermalBlackHotButtonBackground { get => _thermalBlackHotButtonBackground; private set { _thermalBlackHotButtonBackground = value; OnPropertyChanged(); } }
        public Brush ThermalBlackHotButtonForeground { get => _thermalBlackHotButtonForeground; private set { _thermalBlackHotButtonForeground = value; OnPropertyChanged(); } }
        public Brush ThermalWhiteHotButtonBackground { get => _thermalWhiteHotButtonBackground; private set { _thermalWhiteHotButtonBackground = value; OnPropertyChanged(); } }
        public Brush ThermalWhiteHotButtonForeground { get => _thermalWhiteHotButtonForeground; private set { _thermalWhiteHotButtonForeground = value; OnPropertyChanged(); } }
        public Brush ThermalRainbowButtonBackground { get => _thermalRainbowButtonBackground; private set { _thermalRainbowButtonBackground = value; OnPropertyChanged(); } }
        public Brush ThermalRainbowButtonForeground { get => _thermalRainbowButtonForeground; private set { _thermalRainbowButtonForeground = value; OnPropertyChanged(); } }

        public ICommand PreviousThermalPaletteCommand { get; private set; }
        public ICommand NextThermalPaletteCommand { get; private set; }
        public ICommand RequestThermalNucCommand { get; private set; }
        public ICommand SelectThermalBlackHotCommand { get; private set; }
        public ICommand SelectThermalWhiteHotCommand { get; private set; }
        public ICommand SelectThermalRandomCommand { get; private set; }
        public ICommand SelectThermalFireBoxMode1Command { get; private set; }
        public ICommand SelectThermalFireBoxMode2Command { get; private set; }

        public int ThermalFireBoxGroupingMode => _thermalFireBoxGroupingMode;
        public Brush ThermalFireBoxMode1Background { get => _thermalFireBoxMode1Background; private set { _thermalFireBoxMode1Background = value; OnPropertyChanged(); } }
        public Brush ThermalFireBoxMode2Background { get => _thermalFireBoxMode2Background; private set { _thermalFireBoxMode2Background = value; OnPropertyChanged(); } }

        public bool IsFireDiagnosticEnabled
        {
            get => _isFireDiagnosticEnabled;
            set
            {
                if (_isFireDiagnosticEnabled == value)
                {
                    return;
                }

                if (value)
                {
                    string sessionDirectory = Path.Combine(
                        AppDomain.CurrentDomain.BaseDirectory,
                        "FireDiagnostics",
                        "Viewer_" + DateTime.Now.ToString("yyyyMMdd_HHmmss"));
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

                OnPropertyChanged();
            }
        }

        public string FireDiagnosticPathText
        {
            get => _fireDiagnosticPathText;
            private set
            {
                if (_fireDiagnosticPathText == value)
                {
                    return;
                }
                _fireDiagnosticPathText = value;
                OnPropertyChanged();
            }
        }

        public bool IsThermalFireDetectionEnabled
        {
            get => _isThermalFireDetectionEnabled;
            set
            {
                if (_isThermalFireDetectionEnabled == value)
                {
                    return;
                }

                _isThermalFireDetectionEnabled = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(FirePowerStatusText));
                OnPropertyChanged(nameof(FireAlertStatusText));

                // Detector와 진단 수집을 동일한 운용 스위치로 유지한다.
                IsFireDiagnosticEnabled = value;

                ConsoleLogHelper.State(
                    "THERMAL FIRE",
                    value
                        ? "Experimental candidate detection enabled"
                        : "Experimental candidate detection disabled");
            }

        }

        public double ThermalHotThresholdRatio
        {
            get => _thermalHotThresholdRatio;
            set
            {
                double normalized =
                    value < 0
                        ? 0
                        : value > 1
                            ? 1
                            : value;

                if (System.Math.Abs(_thermalHotThresholdRatio - normalized) < 0.001)
                {
                    return;
                }

                _thermalHotThresholdRatio = normalized;
                OnPropertyChanged();
            }

        }

        public double ThermalMinimumAreaRatio
        {
            get => _thermalMinimumAreaRatio;
            set
            {
                double normalized =
                    value < 0.001
                        ? 0.001
                        : value > 0.25
                            ? 0.25
                            : value;

                if (System.Math.Abs(_thermalMinimumAreaRatio - normalized) < 0.0001)
                {
                    return;
                }

                _thermalMinimumAreaRatio = normalized;
                OnPropertyChanged();
            }

        }

        public string FirePowerStatusText =>
            IsThermalFireDetectionEnabled
                ? "ON"
                : "OFF";

        /// <summary>
        /// InitializeThermalFeatures 초기화 함수.
        /// </summary>
        private void InitializeThermalFeatures()
        {
            PreviousThermalPaletteCommand =
                new RelayCommand(() =>
                    ChangeThermalPalette(
                        -1));

            NextThermalPaletteCommand =
                new RelayCommand(() =>
                    ChangeThermalPalette(
                        1));

            RequestThermalNucCommand =
                new RelayCommand(() =>
                    SendThermalNucCommand());

            SelectThermalBlackHotCommand =
                new RelayCommand(() =>
                    SendThermalPaletteDirectCommand("BLACK HOT", 0));

            SelectThermalWhiteHotCommand =
                new RelayCommand(() =>
                    SendThermalPaletteDirectCommand("WHITE HOT", 1));

            // 2026-08-14: RANDOM은 지원되지 않는 직접 RAINBOW 명령 대신 NEXT를 1회 전송한다.
            SelectThermalRandomCommand =
                new RelayCommand(() =>
                    ChangeThermalPalette(1, true));

            SelectThermalFireBoxMode1Command =
                new RelayCommand(() => SetThermalFireBoxGroupingMode(1));
            SelectThermalFireBoxMode2Command =
                new RelayCommand(() => SetThermalFireBoxGroupingMode(2));

        }

        /// <summary>
        /// SetThermalFireBoxGroupingMode 설정 함수.
        /// </summary>
        private void SetThermalFireBoxGroupingMode(int mode)
        {
            _thermalFireBoxGroupingMode = mode == 1 ? 1 : 2;
            ThermalFireBoxMode1Background = new SolidColorBrush(
                _thermalFireBoxGroupingMode == 1 ? Color.FromRgb(42, 111, 151) : Color.FromRgb(62, 81, 94));
            ThermalFireBoxMode2Background = new SolidColorBrush(
                _thermalFireBoxGroupingMode == 2 ? Color.FromRgb(42, 111, 151) : Color.FromRgb(62, 81, 94));
            OnPropertyChanged(nameof(ThermalFireBoxGroupingMode));
            ConsoleLogHelper.State("THERMAL FIRE", "BBox grouping mode=" + _thermalFireBoxGroupingMode);
        }

        /// <summary>
        /// SendThermalPaletteDirectCommand 송신 함수.
        /// </summary>
        private void SendThermalPaletteDirectCommand(string paletteName, int paletteType)
        {
            // 2026-08-14: REI는 ROOFTOP과 ENVIRONMENT가 각각의 제어 경로를 사용한다.
            bool result = IsEnvironmentStatusSelected
                ? (paletteType == 0 ? _webAgentThermalPaletteService.SelectBlackHot()
                    : paletteType == 1 ? _webAgentThermalPaletteService.SelectWhiteHot()
                    : _webAgentThermalPaletteService.SelectRainbow())
                : (paletteType == 0 ? _controlCommandService.SelectIrBlackHot()
                    : paletteType == 1 ? _controlCommandService.SelectIrWhiteHot()
                    : _controlCommandService.SelectIrRainbow());
            LogThermalControlCommandResult(
                "PALETTE " + paletteName,
                result);

            if (result)
            {
                UpdateThermalPaletteButtonVisual(paletteType);
                ResetFireSmokeFrameAnalysis("PALETTE " + paletteName);
            }

        }

        /// <summary>
        /// SendThermalNucCommand 송신 함수.
        /// </summary>
        private void SendThermalNucCommand()
        {
            // 2026-08-14: C# 7.3에서는 조건식에 메서드 그룹을 직접 사용할 수 없다.
            System.Func<bool> sendCommand;

            if (IsEnvironmentStatusSelected)
            {
                sendCommand = _webAgentThermalPaletteService.RequestNuc;
            }
            else
            {
                sendCommand = _controlCommandService.RequestIrNuc;
            }

            SendThermalCommand(
                "NUC",
                sendCommand);

            ResetFireSmokeFrameAnalysis("NUC");
        }

        /// <summary>
        /// InitializeThermalBlackHotAfterDeviceConnected 초기화 함수.
        /// </summary>
        private void InitializeThermalBlackHotAfterDeviceConnected()
        {
            // 2026-08-14: Do not reuse the palette retained by the IR camera from
            // the previous run. Every new device connection starts in BLACK HOT.
            SendThermalPaletteDirectCommand("INITIAL BLACK HOT", 0);
        }

        /// <summary>
        /// 장비의 현재 Palette를 추정하지 않고 PREV / NEXT 상대 명령을
        /// 정확히 한 번만 전송한다.
        /// </summary>
        private void ChangeThermalPalette(
            int offset,
            bool highlightRandomButton = false)
        {
            if (IsEnvironmentStatusSelected)
            {
                bool environmentResult =
                    offset < 0
                        ? _webAgentThermalPaletteService.SelectPrevious()
                        : _webAgentThermalPaletteService.SelectNext();

                LogThermalControlCommandResult(
                    offset < 0
                        ? "ENVIRONMENT PALETTE PREV"
                        : "ENVIRONMENT PALETTE NEXT",
                    environmentResult);

                if (environmentResult)
                {
                    if (highlightRandomButton)
                    {
                        UpdateThermalPaletteButtonVisual(2);
                    }
                    else
                    {
                        ResetThermalPaletteButtonVisuals();
                    }

                    ResetFireSmokeFrameAnalysis("ENVIRONMENT PALETTE CHANGE");

                }

                return;
            }

            bool result =
                offset < 0
                    ? _controlCommandService.SelectPreviousIrPalette()
                    : _controlCommandService.SelectNextIrPalette();

            LogThermalControlCommandResult(
                offset < 0
                    ? "PALETTE PREV"
                    : "PALETTE NEXT",
                result);

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

                ResetFireSmokeFrameAnalysis("ROOFTOP PALETTE CHANGE");

            }

        }

        // 2026-08-14: Highlight a direct palette only after the device write succeeds.
        /// <summary>
        /// UpdateThermalPaletteButtonVisual 갱신 함수.
        /// </summary>
        private void UpdateThermalPaletteButtonVisual(int paletteType)
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
                LinearGradientBrush rainbow = new LinearGradientBrush { StartPoint = new System.Windows.Point(0, 0), EndPoint = new System.Windows.Point(1, 0) };
                rainbow.GradientStops.Add(new GradientStop(Colors.Red, 0));
                rainbow.GradientStops.Add(new GradientStop(Colors.Yellow, 0.35));
                rainbow.GradientStops.Add(new GradientStop(Colors.LimeGreen, 0.60));
                rainbow.GradientStops.Add(new GradientStop(Colors.DodgerBlue, 0.82));
                rainbow.GradientStops.Add(new GradientStop(Colors.MediumPurple, 1));
                ThermalRainbowButtonBackground = rainbow;
                ThermalRainbowButtonForeground = Brushes.White;
            }

        }

        /// <summary>
        /// ResetThermalPaletteButtonVisuals 동작 수행 함수.
        /// </summary>
        private void ResetThermalPaletteButtonVisuals()
        {
            Brush neutralForeground = new SolidColorBrush(Color.FromRgb(32, 38, 45));
            ThermalBlackHotButtonBackground = Brushes.WhiteSmoke;
            ThermalBlackHotButtonForeground = neutralForeground;
            ThermalWhiteHotButtonBackground = Brushes.WhiteSmoke;
            ThermalWhiteHotButtonForeground = neutralForeground;
            ThermalRainbowButtonBackground = Brushes.WhiteSmoke;
            ThermalRainbowButtonForeground = neutralForeground;
        }

        /// <summary>
        /// LogThermalControlCommandResult 동작 수행 함수.
        /// </summary>
        private static void LogThermalControlCommandResult(
            string commandName,
            bool result)
        {
            if (result)
            {
                ConsoleLogHelper.Info(
                    "THERMAL CONTROL",
                    "WRITE SUCCESS / COMMAND=" +
                    commandName);
            }
            else
            {
                ConsoleLogHelper.Warning(
                    "THERMAL CONTROL",
                    "WRITE FAILED / COMMAND=" +
                    commandName);
            }

        }

        /// <summary>
        /// SendThermalCommand 송신 함수.
        /// </summary>
        private static void SendThermalCommand(
            string commandName,
            System.Func<bool> sendCommand)
        {
            bool result = sendCommand();

            if (result)
            {
                ConsoleLogHelper.Info(
                    "THERMAL CONTROL",
                    "CONTROL AGENT TCP WRITE SUCCESS / " +
                    "DEVICE APPLY UNCONFIRMED / COMMAND=" +
                    commandName +
                    " / VERIFY=CONTROL AGENT AND IR CAMERA");
            }
            else
            {
                ConsoleLogHelper.Warning(
                    "THERMAL CONTROL",
                    "CONTROL AGENT TCP WRITE FAILED / COMMAND=" +
                    commandName);
            }

        }

        /// <summary>
        /// UpdateThermalFireCandidateState 갱신 함수.
        /// </summary>
        private void UpdateThermalFireCandidateState(
            string camera,
            ThermalFireDetectionResult result)
        {
            bool isInfrared =
                string.Equals(camera, "IR", System.StringComparison.OrdinalIgnoreCase);
            bool previousState = isInfrared
                ? _isIrFireCandidateDetected
                : _isEoFireCandidateDetected;

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
                OnPropertyChanged(nameof(FirePowerStatusText));
                OnPropertyChanged(nameof(FireAlertStatusText));
            }

            UpdateVisionBBoxEvents(
                camera, "FIRE", result.CandidateRects,
                result.CandidateScores,
                "IMAGE PROCESSING");
        }

    }

}
