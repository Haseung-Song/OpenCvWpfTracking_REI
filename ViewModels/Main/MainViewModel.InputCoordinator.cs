using System.Windows.Input;

// 2026-10-16: 입력 소유자별 PTZF 제어와 공통 속도·상태 갱신.
// 코드 정비: 한국어 설명과 함수 간격·region을 정리하며 기존 운용 흐름은 유지한다.
namespace OpenCvWpfTracking.ViewModels.Main
{
    public partial class MainViewModel
    {

        internal static readonly string[] ZoomStatusPropertyNames =
        {
            nameof(EoZoomRatioOverlayText),
            nameof(IrZoomRatioOverlayText),
            nameof(RooftopEoZoomStatusText),
            nameof(RooftopIrZoomStatusText),
            nameof(EnvironmentEoZoomStatusText),
            nameof(EnvironmentIrZoomStatusText)
        };

        internal static readonly string[] PowerStatusPropertyNames =
        {
            nameof(EoPowerControlStatusText),
            nameof(IrPowerControlStatusText),
            nameof(CurrentEoPowerStatusColor),
            nameof(CurrentIrPowerStatusColor),
            nameof(IsEoPowerOnEnabled),
            nameof(IsEoPowerOffEnabled),
            nameof(IsIrPowerOnEnabled),
            nameof(IsIrPowerOffEnabled)
        };

        public bool IsFovSpeedEnabled
        {
            get { return _inputCoordinatorFeature.IsFovSpeedEnabled; }
            set { _inputCoordinatorFeature.IsFovSpeedEnabled = value; }
        }

        public bool IsGlobalControlEnabled
        {
            get { return _inputCoordinatorFeature.IsGlobalControlEnabled; }
            set { _inputCoordinatorFeature.IsGlobalControlEnabled = value; }
        }

        public bool IsVideoClickCenterEnabled
        {
            get { return _inputCoordinatorFeature.IsVideoClickCenterEnabled; }
            set { _inputCoordinatorFeature.IsVideoClickCenterEnabled = value; }
        }

        /// <summary>
        /// 2026-10-14 모든 EO/IR 메인·서브·분리 영상의 배율 배지만 표시/숨긴다. 제어값은 바꾸지 않는다.
        /// </summary>
        public bool IsZoomRatioOverlayVisible
        {
            get { return _inputCoordinatorFeature.IsZoomRatioOverlayVisible; }
            set { _inputCoordinatorFeature.IsZoomRatioOverlayVisible = value; }
        }

        public string GlobalControlStatusText
        {
            get { return _inputCoordinatorFeature.GlobalControlStatusText; }
        }

        public double EoOpticalRatioMinimum
        {
            get { return _inputCoordinatorFeature.EoOpticalRatioMinimum; }
        }

        public double EoOpticalRatioMaximum
        {
            get { return _inputCoordinatorFeature.EoOpticalRatioMaximum; }
        }

        public string ZoomOpticalStandardText
        {
            get { return _inputCoordinatorFeature.ZoomOpticalStandardText; }
        }

        public string ZoomOpticalRangeText
        {
            get { return _inputCoordinatorFeature.ZoomOpticalRangeText; }
        }

        public double EoSpeedExponent
        {
            get { return _inputCoordinatorFeature.EoSpeedExponent; }
            set { _inputCoordinatorFeature.EoSpeedExponent = value; }
        }

        public double IrSpeedExponent
        {
            get { return _inputCoordinatorFeature.IrSpeedExponent; }
            set { _inputCoordinatorFeature.IrSpeedExponent = value; }
        }

        public int EoMinimumSpeed
        {
            get { return _inputCoordinatorFeature.EoMinimumSpeed; }
            set { _inputCoordinatorFeature.EoMinimumSpeed = value; }
        }

        public int IrMinimumSpeed
        {
            get { return _inputCoordinatorFeature.IrMinimumSpeed; }
            set { _inputCoordinatorFeature.IrMinimumSpeed = value; }
        }

        /// <summary>
        /// Z/C 반복 입력을 포함하여 공통 설정 속도를 1~60으로 제한해 증감한다.
        /// </summary>
        public void AdjustControlSpeed(int delta)
        {
            _inputCoordinatorFeature.AdjustControlSpeed(delta);
        }

        public string EoZoomRatioOverlayText
        {
            get { return _inputCoordinatorFeature.EoZoomRatioOverlayText; }
        }

        public string IrZoomRatioOverlayText
        {
            get { return _inputCoordinatorFeature.IrZoomRatioOverlayText; }
        }

        /// <summary>
        /// 250ms 상태 표시 감시를 한 번만 시작한다. 디지털 추정으로 PT를 재송신하지 않는다.
        /// </summary>
        public void InitializeControlStatusMonitor()
        {
            _inputCoordinatorFeature.InitializeControlStatusMonitor();
        }

        /// <summary>
        /// 상태 타이머를 중지하고 종료 시 남은 제어 입력을 해제한다.
        /// </summary>
        public void ShutdownControlInputMonitor()
        {
            _inputCoordinatorFeature.ShutdownControlInputMonitor();
        }

        /// <summary>
        /// 입력 소유자/채널별 키를 축 명령으로 변환하고 키 해제 및 반복을 처리한다.
        /// </summary>
        public bool HandleControlKey(string owner, Key key, bool down, bool ir, bool repeat = false)
        {
            return _inputCoordinatorFeature.HandleControlKey(owner, key, down, ir, repeat);
        }

        /// <summary>
        /// 연결/잠금 검증 후 누름 입력을 등록하며 수동 입력은 절대 위치 이동보다 우선한다.
        /// </summary>
        public void PressControlInput(string token, string axis, int x, int y, bool ir)
        {
            _inputCoordinatorFeature.PressControlInput(token, axis, x, y, ir);
        }

        /// <summary>
        /// 해당 토큰만 해제하고 다른 소유자가 누르고 있는 축은 유지한다.
        /// </summary>
        public void ReleaseControlInput(string token)
        {
            _inputCoordinatorFeature.ReleaseControlInput(token);
        }

        /// <summary>
        /// 지정 창/입력 소유자의 토큰만 일괄 해제한다.
        /// </summary>
        public void ReleaseControlOwner(string owner)
        {
            _inputCoordinatorFeature.ReleaseControlOwner(owner);
        }

        /// <summary>
        /// 입력 세대를 갱신해 대기 명령을 무효화하고 필요할 때 전체 PTZF를 정지한다.
        /// </summary>
        public void ReleaseAllControlInputs(string reason)
        {
            _inputCoordinatorFeature.ReleaseAllControlInputs(reason);
        }

    }
}
