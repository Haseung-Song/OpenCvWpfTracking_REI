using System;

namespace OpenCvWpfTracking.ViewModels.Main
{
    /// <summary>
    /// 장비 상태를 PRESET과 Event가 공통으로 사용하는 위치 스냅샷으로 변환한다.
    /// RTSP 프레임 처리와 분리하여 영상 수신·렌더링 부하에 영향을 주지 않는다.
    /// </summary>
    public partial class MainViewModel
    {
        internal static readonly TimeSpan PositionStateMaximumAge = TimeSpan.FromSeconds(3);

        internal static readonly TimeSpan PositionTelemetryMaximumAge = TimeSpan.FromSeconds(30);

        /// <summary>
        /// CURRENT STATUS 하단에서 최근 상태 패킷의 유효성을 간단히 확인한다.
        /// 실제 값은 기존 PTZF 행을 유지하고 여기서는 유효성만 표시한다.
        /// </summary>
        public string PositionStatusText
        {
            get { return _positionFeature.PositionStatusText; }
        }

        public string LastPositionSnapshotText
        {
            get { return _positionFeature.LastPositionSnapshotText; }
        }

    }
}
