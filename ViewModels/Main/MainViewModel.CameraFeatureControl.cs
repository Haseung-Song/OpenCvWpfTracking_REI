using System;
using System.Threading.Tasks;

// 2026-10-16: 카메라 디지털 줌 지원·상태와 영상 클릭 중심 이동.
// 코드 정비: 한국어 설명과 함수 간격·region을 정리하며 기존 운용 흐름은 유지한다.
namespace OpenCvWpfTracking.ViewModels.Main
{
    public partial class MainViewModel
    {

        public bool CanSetEoDigitalZoom
        {
            get { return _cameraFeatureControlFeature.CanSetEoDigitalZoom; }
        }

        public bool CanSetIrDigitalZoom
        {
            get { return _cameraFeatureControlFeature.CanSetIrDigitalZoom; }
        }

        public string EoDigitalZoomStatusText
        {
            get { return _cameraFeatureControlFeature.EoDigitalZoomStatusText; }
        }

        public string IrDigitalZoomStatusText
        {
            get { return _cameraFeatureControlFeature.IrDigitalZoomStatusText; }
        }

        /// <summary>
        /// 영상 클릭으로 요청한 Pan/Tilt 절대 위치 이동이 송신 중이거나
        /// 장비의 목표 도착 응답을 기다리고 있는지 여부입니다.
        /// 마우스 hover에 따른 EO/IR 채널 전환이 진행 중인 클릭 이동을
        /// 중단하지 않도록 MainWindow에서 사용합니다.
        /// </summary>
        public bool IsVideoCenterMoveActive
        {
            get { return _cameraFeatureControlFeature.IsVideoCenterMoveActive; }
        }

        internal static readonly string[] DigitalZoomPropertyNames =
        {
            nameof(CanSetEoDigitalZoom),
            nameof(CanSetIrDigitalZoom),
            nameof(EoDigitalZoomStatusText),
            nameof(IrDigitalZoomStatusText)
        };

        /// <summary>
        /// EO/IR 기능 지원 조회를 연결 중 최대 초당 한 번 요청한다.
        /// </summary>
        public void QueryDigitalZoomSupport()
        {
            _cameraFeatureControlFeature.QueryDigitalZoomSupport();
        }

        /// <summary>
        /// 운용 잠금 검증 후 BOOLEAN 명령을 송신한다. 송신 성공을 장비 적용 확인으로 취급하지 않는다.
        /// </summary>
        public bool SetDigitalZoom(bool ir, bool on)
        {
            return _cameraFeatureControlFeature.SetDigitalZoom(ir, on);
        }

        // 2026-10-12: 시간 기반 이동이 아닌 핀홀 광선 투영을 사용한다. 임시 화각은 현장 보정이 필요하다.
        /// <summary>
        /// 정규화된 영상 클릭 좌표를 핀홀 광선 모델로 PT 절대 목표 각도에 환산한다.
        /// </summary>
        public static Tuple<double, double> VideoCenterTarget(double pan, double tilt, double nx, double ny, double hfov, double aspect)
        {
            return CameraFeatureControlFeature.VideoCenterTarget(pan, tilt, nx, ny, hfov, aspect);
        }

        /// <summary>
        /// 영상·렌즈·자세의 신선도와 제어 잠금을 검증한 뒤 줌 보정 PT 절대 이동을 요청한다.
        /// </summary>
        public Task<bool> CenterVideoPointAsync(bool ir, double nx, double ny, double aspect, int sourceWidth = 0, int sourceHeight = 0)
        {
            return _cameraFeatureControlFeature.CenterVideoPointAsync(ir, nx, ny, aspect, sourceWidth, sourceHeight);
        }
    }

    internal static class CameraNumberValidation
    {
        /// <summary>
        /// 좌표 계산에 사용할 값에 유효하지 않은 수가 포함되었는지 확인한다.
        /// </summary>
        internal static bool AnyInvalid(this double[] numbers)
        {
            foreach (double x in numbers)
            {
                if (double.IsNaN(x) || double.IsInfinity(x))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
