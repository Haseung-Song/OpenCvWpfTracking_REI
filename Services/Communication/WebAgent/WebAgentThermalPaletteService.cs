namespace OpenCvWpfTracking.Services.Communication.WebAgent
{
    /// <summary>
    /// ENVIRONMENT 장비용 WEB AGENT Pelco-D 열영상 팔레트 어댑터.
    /// 환경장비는 팔레트 ID 직접 적용을 사용하지 않고 단순 명령만 제공한다.
    /// </summary>
    public sealed class WebAgentThermalPaletteService
    {
        private readonly ControlCommandService _controlCommandService;

        /// <summary>
        /// WebAgentThermalPaletteService 동작 수행 함수.
        /// </summary>
        public WebAgentThermalPaletteService(
            ControlCommandService controlCommandService)
        {
            _controlCommandService = controlCommandService;
        }

        /// <summary>
        /// SelectPrevious 동작 수행 함수.
        /// </summary>
        public bool SelectPrevious() =>
            _controlCommandService.SelectPreviousIrPalette();

        /// <summary>
        /// SelectNext 동작 수행 함수.
        /// </summary>
        public bool SelectNext() =>
            _controlCommandService.SelectNextIrPalette();

        /// <summary>
        /// SelectBlackHot 동작 수행 함수.
        /// </summary>
        public bool SelectBlackHot() =>
            _controlCommandService.SelectIrBlackHot();

        /// <summary>
        /// SelectWhiteHot 동작 수행 함수.
        /// </summary>
        public bool SelectWhiteHot() =>
            _controlCommandService.SelectIrWhiteHot();

        /// <summary>
        /// SelectRainbow 동작 수행 함수.
        /// </summary>
        public bool SelectRainbow() =>
            _controlCommandService.SelectIrRainbow();

        // 2026-08-14: ENVIRONMENT Web Agent도 규격서 NUC 명령을 같은 경로로 송신한다.
        /// <summary>
        /// RequestNuc 동작 수행 함수.
        /// </summary>
        public bool RequestNuc() =>
            _controlCommandService.RequestIrNuc();
    }

}
