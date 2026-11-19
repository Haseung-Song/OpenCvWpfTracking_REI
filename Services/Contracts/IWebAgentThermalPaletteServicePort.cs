namespace OpenCvWpfTracking.Services.Communication.WebAgent
{
    /// <summary>기능 객체가 사용하는 서비스 계약입니다. 실제 통신·분석 구현을 교체할 수 있습니다.</summary>
    internal interface IWebAgentThermalPaletteServicePort
    {
        /// <summary>
        /// IR 팔레트를 이전 항목으로 전환한다.
        /// </summary>
        bool SelectPrevious();

        /// <summary>
        /// IR 팔레트를 다음 항목으로 전환한다.
        /// </summary>
        bool SelectNext();

        /// <summary>
        /// IR 영상의 팔레트를 Black Hot으로 설정한다.
        /// </summary>
        bool SelectBlackHot();

        /// <summary>
        /// IR 영상의 팔레트를 White Hot으로 설정한다.
        /// </summary>
        bool SelectWhiteHot();

        /// <summary>
        /// IR 영상의 팔레트를 Rainbow로 설정한다.
        /// </summary>
        bool SelectRainbow();

        /// <summary>
        /// WEB NUC는 공통 단발 요청만 사용하며 구형 명령으로 재시도하지 않는다.
        /// </summary>
        bool RequestNuc();

        /// <summary>
        /// 카메라가 응답한 지원 마스크를 사용하며 원시 번호로 색상 이름을 추정하지 않는다.
        /// </summary>
        void ApplyPaletteCapability(uint optionMask);

        /// <summary>
        /// 장비가 보고한 팔레트 값을 화면의 선택 상태에 반영한다.
        /// </summary>
        void ApplyReportedPalette(int palette);

        /// <summary>
        /// 장비·세션 전환 시 이전 장비의 순회 번호와 마스크를 재사용하지 않는다.
        /// </summary>
        void ResetRelativeSelection();
    }
}
