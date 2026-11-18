namespace OpenCvWpfTracking.Services.Communication.WebAgent
{
    /// <summary>직접 색상 선택과 기존 번호 기반 NEXT·PREV를 분리해 WEB에 전달한다.</summary>
    public sealed class WebAgentThermalPaletteService : IWebAgentThermalPaletteServicePort
    {
        private readonly ControlCommandService _controlCommandService;

        private readonly object _relativeSync = new object();

        private int _currentPalette = 1;

        private uint _supportedPaletteMask = 0x3F;

        public WebAgentThermalPaletteService(ControlCommandService controlCommandService)
        {
            _controlCommandService = controlCommandService;
        }

        public bool SelectPrevious()
        {
            return SelectRelative(-1);
        }

        public bool SelectNext()
        {
            return SelectRelative(1);
        }

        public bool SelectBlackHot()
        {
            return SendPalette(PolarityOperation(false));
        }

        public bool SelectWhiteHot()
        {
            return SendPalette(PolarityOperation(true));
        }

        /// <summary>
        /// 2026-11-06: 문서의 흑백 팔레트·역상 선택 기준으로 WEB 공통 매핑을 복원한다.
        /// WHITE HOT은 F3, BLACK HOT은 F4를 사용하며 실제 반영은 영상으로 확인한다.
        /// 별도 LA 제어 경로와 색상 번호 기반 NEXT·PREV는 유지한다.
        /// </summary>
        internal static byte PolarityOperation(bool whiteHot)
        {
            if (whiteHot)
            {
                return (byte)0xF3;
            }
            else
            {
                return (byte)0xF4;
            }
        }

        public bool SelectRainbow()
        {
            return SendPalette(0xF5);
        }

        /// <summary>WEB NUC는 공통 단발 요청만 사용하며 구형 명령으로 재시도하지 않는다.</summary>
        public bool RequestNuc()
        {
            return _controlCommandService.SetCameraFeature(0x04, 0x01, 0x01);
        }

        /// <summary>카메라가 응답한 지원 마스크를 사용하며 원시 번호로 색상 이름을 추정하지 않는다.</summary>
        public void ApplyPaletteCapability(uint optionMask)
        {
            lock (_relativeSync)
            {
                _supportedPaletteMask = optionMask;
            }
        }

        public void ApplyReportedPalette(int palette)
        {
            if (palette < 0 || palette >= 6)
            {
                return;
            }

            lock (_relativeSync)
            {
                _currentPalette = palette;
            }
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

        /// <summary>기존 NEXT·PREV의 E9 Feature 05 선택 후 E7 조회 순서를 유지한다.</summary>
        private bool SelectRelative(int offset)
        {
            lock (_relativeSync)
            {
                int palette = (_currentPalette + offset + 6) % 6;

                if ((_supportedPaletteMask & (1u << palette)) == 0)
                {
                    return false;
                }

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
