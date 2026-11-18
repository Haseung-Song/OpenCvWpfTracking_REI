namespace OpenCvWpfTracking.Services.Communication.WebAgent
{
    /// <summary>기능 객체가 사용하는 서비스 계약입니다. 실제 통신·분석 구현을 교체할 수 있습니다.</summary>
    internal interface IWebAgentThermalPaletteServicePort
    {
        bool SelectPrevious();

        bool SelectNext();

        bool SelectBlackHot();

        bool SelectWhiteHot();

        bool SelectRainbow();

        bool RequestNuc();

        void ApplyPaletteCapability(uint optionMask);

        void ApplyReportedPalette(int palette);

        void ResetRelativeSelection();
    }
}
