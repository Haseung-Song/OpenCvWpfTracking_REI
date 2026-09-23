namespace OpenCvWpfTracking.Services.Communication.WebAgent
{
    /// <summary>WebAgent GUI-SBC additional protocol v1.8 thermal adapter.</summary>
    public sealed class WebAgentThermalPaletteService
    {
        private const byte IrTarget = 0x01;

        private const byte NucFeature = 0x04;

        private const byte PaletteFeature = 0x05;

        private const byte PolarityFeature = 0x06;

        private const int PaletteCount = 6;

        private readonly ControlCommandService _controlCommandService;
        private readonly object _sync = new object();
        private int _currentPalette = 1;

        private uint _supportedPaletteMask = 0x3F;

        public WebAgentThermalPaletteService(ControlCommandService controlCommandService)
        {
            _controlCommandService = controlCommandService;
        }

        public bool SelectPrevious() => SelectRelative(-1);
        public bool SelectNext() => SelectRelative(1);
        public bool SelectBlackHot() => SelectPalette(1);
        public bool SelectWhiteHot() => SelectPalette(0);
        public bool SelectRainbow() => SelectPalette(3);
        public bool RequestNuc() => _controlCommandService.SetCameraFeature(NucFeature, IrTarget, 0x01);
        public bool RequestCapability() => _controlCommandService.RequestCameraFeatureCapability(IrTarget);
        public bool RequestPaletteState() => _controlCommandService.RequestCameraFeatureState(IrTarget, PaletteFeature);
        public bool RequestPolarityState() => _controlCommandService.RequestCameraFeatureState(IrTarget, PolarityFeature);

        public void ApplyPaletteCapability(uint optionMask)
        {
            lock (_sync) _supportedPaletteMask = optionMask;
        }

        public void ApplyReportedPalette(int palette)
        {
            if (palette < 0 || palette >= PaletteCount) return;
            lock (_sync) _currentPalette = palette;
        }

        private bool SelectRelative(int offset)
        {
            lock (_sync)
            {
                return SelectPalette((_currentPalette + offset + PaletteCount) % PaletteCount);
            }
        }

        private bool SelectPalette(int palette)
        {
            lock (_sync)
            {
                if ((_supportedPaletteMask & (1u << palette)) == 0) return false;
                bool sent = _controlCommandService.SetCameraFeature(PaletteFeature, IrTarget, (byte)palette);
                if (sent)
                {
                    _currentPalette = palette;
                    _controlCommandService.RequestCameraFeatureState(IrTarget, PaletteFeature);
                }
                return sent;
            }
        }
    }
}
