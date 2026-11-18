using OpenCvWpfTracking.Services.Video;

namespace OpenCvWpfTracking.ViewModels.Main
{
    public partial class MainViewModel
    {

        internal sealed class PanoramaZeroTicket
        {
            public string Context;

            public bool PanAxis;

            public double? BeforeValue;

            public long PoseTicks;

            public double OtherBeforeValue;
        }

        /// <summary>같은 장비 재연결은 같은 변환 기록을 사용하며 이전 사진과 연동 번호를 지우지 않는다.</summary>
        public PanoramaReferenceState PanoramaReference
        {
            get { return _panoramaReferenceFeature.PanoramaReference; }
        }

        internal static double SignedPan(double pan)
        {
            return PanoramaReferenceFeature.SignedPan(pan);
        }

        /// <summary>Config의 변경 기록을 먼저 확정하므로 CSV 저장 중 종료되어도 같은 보정을 한 번만 적용한다.</summary>
        public void RecoverPanoramaPresetReference()
        {
            _panoramaReferenceFeature.RecoverPanoramaPresetReference();
        }

    }
}
