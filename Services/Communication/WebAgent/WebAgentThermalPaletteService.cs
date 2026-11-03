namespace OpenCvWpfTracking.Services.Communication.WebAgent
{
    /// <summary>직접 색상 선택과 기존 번호 기반 NEXT·PREV를 분리해 WEB에 전달한다.</summary>
    public sealed class WebAgentThermalPaletteService
    {
        private readonly ControlCommandService _controlCommandService;

        private readonly object _relativeSync = new object ();

        private int _currentPalette = 1;

        private uint _supportedPaletteMask = 0x3F;

        public WebAgentThermalPaletteService(ControlCommandService controlCommandService)
        {
            _controlCommandService = controlCommandService;
        }

        public bool SelectPrevious() => SelectRelative(-1);

        public bool SelectNext() => SelectRelative(1);

        public bool SelectBlackHot() => SendPalette(0xF4);

        public bool SelectWhiteHot() => SendPalette(0xF3);

        public bool SelectRainbow() => SendPalette(0xF5);

        /// <summary>WEB NUC는 공통 단발 요청만 사용하며 구형 명령으로 재시도하지 않는다.</summary>
        public bool RequestNuc() => _controlCommandService.SetCameraFeature(0x04, 0x01, 0x01);

        /// <summary>와 같은 지원 마스크를 사용하며 원시 번호로 색상 이름을 추정하지 않는다.</summary>
        public void ApplyPaletteCapability(uint optionMask)
        {
            lock (_relativeSync)
                _supportedPaletteMask = optionMask;
        }

        public void ApplyReportedPalette(int palette)
        {
            if (palette < 0 || palette >= 6)
                return;

            lock (_relativeSync)
                _currentPalette = palette;
        }

        /// <summary>장비·세션 전환 시 이전 장비의 순회 번호와 마스크를 재사용하지 않는다.</summary>
        public void ResetRelativeSelection()
        {
            lock (_relativeSync)
            {
                _currentPalette = 1;
                _supportedPaletteMask = 0x3F;
            }
        }

        /// <summary>정상 동작하던 의 E9 Feature 05 선택 후 E7 조회 순서를 유지한다.</summary>
        private bool SelectRelative(int offset)
        {
            lock (_relativeSync)
            {
                int palette = (_currentPalette + offset + 6) % 6;

                if ((_supportedPaletteMask & (1u << palette)) == 0)
                    return false;

                bool sent = _controlCommandService.SetCameraFeature(0x05, 0x01, (byte)palette);

                if (sent)
                {
                    _currentPalette = palette;
                    _controlCommandService.RequestCameraFeatureState(0x01, 0x05);
                }

                return sent;
            }
        }

        private bool SendPalette(byte operation)
        {
            return _controlCommandService.SendCommand(0x00, 0x31, operation, 0x00);
        }
    }
}
