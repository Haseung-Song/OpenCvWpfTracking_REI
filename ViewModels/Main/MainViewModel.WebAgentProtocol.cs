using System;

namespace OpenCvWpfTracking.ViewModels.Main
{
    /// <summary>WEB AGENT 추가 기능과 상태 응답을 기능 번호에 따라 처리한다.</summary>
    public partial class MainViewModel
    {
        internal const long LegacyEoLensAuthorityTicks = TimeSpan.TicksPerSecond * 3;

        internal const long WebAgentStatusWarningIntervalTicks = TimeSpan.TicksPerSecond * 5;

    }
}
