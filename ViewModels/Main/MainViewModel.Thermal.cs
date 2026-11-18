using System.Windows.Media;
using System.Windows.Input;

namespace OpenCvWpfTracking.ViewModels.Main
{
    public partial class MainViewModel
    {

        public Brush ThermalBlackHotButtonBackground
        {
            get { return _thermalFeature.ThermalBlackHotButtonBackground; }
            private set { _thermalFeature.ThermalBlackHotButtonBackground = value; }
        }

        public Brush ThermalBlackHotButtonForeground
        {
            get { return _thermalFeature.ThermalBlackHotButtonForeground; }
            private set { _thermalFeature.ThermalBlackHotButtonForeground = value; }
        }

        public Brush ThermalWhiteHotButtonBackground
        {
            get { return _thermalFeature.ThermalWhiteHotButtonBackground; }
            private set { _thermalFeature.ThermalWhiteHotButtonBackground = value; }
        }

        public Brush ThermalWhiteHotButtonForeground
        {
            get { return _thermalFeature.ThermalWhiteHotButtonForeground; }
            private set { _thermalFeature.ThermalWhiteHotButtonForeground = value; }
        }

        public Brush ThermalRainbowButtonBackground
        {
            get { return _thermalFeature.ThermalRainbowButtonBackground; }
            private set { _thermalFeature.ThermalRainbowButtonBackground = value; }
        }

        public Brush ThermalRainbowButtonForeground
        {
            get { return _thermalFeature.ThermalRainbowButtonForeground; }
            private set { _thermalFeature.ThermalRainbowButtonForeground = value; }
        }

        public ICommand PreviousThermalPaletteCommand
        {
            get { return _thermalFeature.PreviousThermalPaletteCommand; }
            private set { _thermalFeature.PreviousThermalPaletteCommand = value; }
        }

        public ICommand NextThermalPaletteCommand
        {
            get { return _thermalFeature.NextThermalPaletteCommand; }
            private set { _thermalFeature.NextThermalPaletteCommand = value; }
        }

        public ICommand RequestThermalNucCommand
        {
            get { return _thermalFeature.RequestThermalNucCommand; }
            private set { _thermalFeature.RequestThermalNucCommand = value; }
        }

        public ICommand SelectThermalBlackHotCommand
        {
            get { return _thermalFeature.SelectThermalBlackHotCommand; }
            private set { _thermalFeature.SelectThermalBlackHotCommand = value; }
        }

        public ICommand SelectThermalWhiteHotCommand
        {
            get { return _thermalFeature.SelectThermalWhiteHotCommand; }
            private set { _thermalFeature.SelectThermalWhiteHotCommand = value; }
        }

        public ICommand SelectThermalRandomCommand
        {
            get { return _thermalFeature.SelectThermalRandomCommand; }
            private set { _thermalFeature.SelectThermalRandomCommand = value; }
        }

        public ICommand SelectThermalFireBoxMode1Command
        {
            get { return _thermalFeature.SelectThermalFireBoxMode1Command; }
            private set { _thermalFeature.SelectThermalFireBoxMode1Command = value; }
        }

        public ICommand SelectThermalFireBoxMode2Command
        {
            get { return _thermalFeature.SelectThermalFireBoxMode2Command; }
            private set { _thermalFeature.SelectThermalFireBoxMode2Command = value; }
        }

        public int ThermalFireBoxGroupingMode
        {
            get { return _thermalFeature.ThermalFireBoxGroupingMode; }
        }

        public Brush ThermalFireBoxMode1Background
        {
            get { return _thermalFeature.ThermalFireBoxMode1Background; }
            private set { _thermalFeature.ThermalFireBoxMode1Background = value; }
        }

        public Brush ThermalFireBoxMode2Background
        {
            get { return _thermalFeature.ThermalFireBoxMode2Background; }
            private set { _thermalFeature.ThermalFireBoxMode2Background = value; }
        }

        /// <summary>
        /// 0=AUTO(EO+IR), 1=EO ONLY, 2=IR ONLY. 기본은 두 채널 분석.
        /// </summary>
        public int FireDetectionSourceIndex
        {
            get { return _thermalFeature.FireDetectionSourceIndex; }
            set { _thermalFeature.FireDetectionSourceIndex = value; }
        }

        public int FireSensitivityIndex
        {
            get { return _thermalFeature.FireSensitivityIndex; }
            set { _thermalFeature.FireSensitivityIndex = value; }
        }

        public bool IsFireDiagnosticEnabled
        {
            get { return _thermalFeature.IsFireDiagnosticEnabled; }
            set { _thermalFeature.IsFireDiagnosticEnabled = value; }
        }

        public string FireDiagnosticPathText
        {
            get { return _thermalFeature.FireDiagnosticPathText; }
            private set { _thermalFeature.FireDiagnosticPathText = value; }
        }

        public bool IsThermalFireDetectionEnabled
        {
            get { return _thermalFeature.IsThermalFireDetectionEnabled; }
            set { _thermalFeature.IsThermalFireDetectionEnabled = value; }
        }

        public double ThermalHotThresholdRatio
        {
            get { return _thermalFeature.ThermalHotThresholdRatio; }
            set { _thermalFeature.ThermalHotThresholdRatio = value; }
        }

        public double ThermalMinimumAreaRatio
        {
            get { return _thermalFeature.ThermalMinimumAreaRatio; }
            set { _thermalFeature.ThermalMinimumAreaRatio = value; }
        }

        public string FirePowerStatusText
        {
            get { return _thermalFeature.FirePowerStatusText; }
        }

        /// <summary>
        /// InitializeThermalFeatures 초기화 함수.
        /// </summary>
        private void InitializeThermalFeatures()
        {
            _thermalFeature.InitializeThermalFeatures();
        }

    }
}
