namespace OpenCvWpfTracking.Services.Communication.WebAgent
{
    /// <summary>
    /// ENVIRONMENT 장비용 WEB AGENT Pelco-D 열영상 팔레트 어댑터.
    /// 환경장비는 팔레트 ID 직접 적용을 사용하지 않고 단순 명령만 제공한다.
    /// </summary>
    public sealed class WebAgentThermalPaletteService
    {
        private readonly ControlCommandService _controlCommandService;

        public WebAgentThermalPaletteService(
            ControlCommandService controlCommandService)
        {
            _controlCommandService = controlCommandService;
        }

        public bool SelectPrevious() =>
            _controlCommandService.SelectPreviousIrPalette();

        public bool SelectNext() =>
            _controlCommandService.SelectNextIrPalette();

        public bool SelectBlackHot() =>
            _controlCommandService.SelectIrBlackHot();

        public bool SelectWhiteHot() =>
            _controlCommandService.SelectIrWhiteHot();

        public bool SelectRainbow() =>
            _controlCommandService.SelectIrRainbow();
    }
}
