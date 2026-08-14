using OpenCvWpfTracking.Common;
using OpenCvWpfTracking.Services.Video;
using System.Windows.Input;

namespace OpenCvWpfTracking.ViewModels.Main
{
    public partial class MainViewModel
    {
        private readonly ThermalFireDetectionService
            _thermalFireDetectionService =
                new ThermalFireDetectionService();

        private bool _isThermalFireDetectionEnabled;
        private bool _isThermalFireCandidateDetected;
        private double _thermalHotThresholdRatio = 0.82;
        private double _thermalMinimumAreaRatio = 0.01;

        public ICommand PreviousThermalPaletteCommand { get; private set; }
        public ICommand NextThermalPaletteCommand { get; private set; }
        public ICommand RequestThermalNucCommand { get; private set; }
        public ICommand SelectThermalBlackHotCommand { get; private set; }
        public ICommand SelectThermalWhiteHotCommand { get; private set; }
        public ICommand SelectThermalRainbowCommand { get; private set; }

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
                    SendThermalCommand(
                        "NUC",
                        _controlCommandService.RequestIrNuc));

            SelectThermalBlackHotCommand =
                new RelayCommand(() =>
                    SendThermalPaletteDirectCommand(
                        "BLACK HOT",
                        _controlCommandService.SelectIrBlackHot));

            SelectThermalWhiteHotCommand =
                new RelayCommand(() =>
                    SendThermalPaletteDirectCommand(
                        "WHITE HOT",
                        _controlCommandService.SelectIrWhiteHot));

            SelectThermalRainbowCommand =
                new RelayCommand(() =>
                    SendThermalPaletteDirectCommand(
                        "RAINBOW",
                        _controlCommandService.SelectIrRainbow));

        }

        private static void SendThermalPaletteDirectCommand(
            string paletteName,
            System.Func<bool> sendCommand)
        {
            LogThermalControlCommandResult(
                "PALETTE " + paletteName,
                sendCommand());
        }

        /// <summary>
        /// 장비의 현재 Palette를 추정하지 않고 PREV / NEXT 상대 명령을
        /// 정확히 한 번만 전송한다.
        /// </summary>
        private void ChangeThermalPalette(
            int offset)
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
        }

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

        private void UpdateThermalFireCandidateState(
            bool isDetected)
        {
            if (_isThermalFireCandidateDetected == isDetected)
            {
                return;
            }

            _isThermalFireCandidateDetected = isDetected;
            OnPropertyChanged(nameof(FirePowerStatusText));
        }
    }
}
