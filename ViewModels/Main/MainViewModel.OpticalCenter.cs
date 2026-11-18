using System;
using System.Windows;

// 2026-10-16: 장비·채널별 십자선 보정 상태 연결.
// 코드 정비: 한국어 설명과 함수 간격·region을 정리하며 기존 운용 흐름은 유지한다.
namespace OpenCvWpfTracking.ViewModels.Main
{
    public partial class MainViewModel
    {

        /// <summary>
        /// 현재 채널의 보정 저장 대상 키를 반환한다.
        /// </summary>
        public string OpticalDeviceKey(bool ir)
        {
            return _opticalCenterFeature.OpticalDeviceKey(ir);
        }

        /// <summary>
        /// 현재 채널의 표준 광학 줌 위치를 반환한다.
        /// </summary>
        public int OpticalZoom(bool ir)
        {
            return _opticalCenterFeature.OpticalZoom(ir);
        }

        /// <summary>
        /// 성공한 D-ZOOM ON 요청 또는 장비 ON 응답이 있는 채널의 4점 보정을 막는다.
        /// 미수신을 ON으로 취급하지 않으며 D-ZOOM 미지원 IR의 보정은 허용한다.
        /// </summary>
        public bool IsOpticalCalibrationBlockedByDigitalZoom(bool ir)
        {
            return _opticalCenterFeature.IsOpticalCalibrationBlockedByDigitalZoom(ir);
        }

        /// <summary>
        /// 현재 장비와 줌 위치에 대응하는 십자선 중심을 반환한다.
        /// </summary>
        public Point GetOpticalCenter(bool ir)
        {
            return _opticalCenterFeature.GetOpticalCenter(ir);
        }

        /// <summary>
        /// 보정에 필요한 렌즈와 위치 응답의 신선도를 검사한다.
        /// </summary>
        public bool OpticalFeedbackFresh(bool ir)
        {
            return _opticalCenterFeature.OpticalFeedbackFresh(ir);
        }

        /// <summary>
        /// 영상 저장에 사용할 실제 프레임의 연결 상태와 수신 시간을 확인한다.
        /// </summary>
        public bool SnapshotFrameFresh(bool ir)
        {
            return _opticalCenterFeature.SnapshotFrameFresh(ir);
        }

        public Point OpticalPose
        {
            get { return _opticalCenterFeature.OpticalPose; }
        }

        public double CurrentPanoramaPan
        {
            get { return _opticalCenterFeature.CurrentPanoramaPan; }
        }

        public double CurrentPanoramaTilt
        {
            get { return _opticalCenterFeature.CurrentPanoramaTilt; }
        }

        // 2026-10-15: 원자적으로 읽은 복사본을 사용하여 연결 해제 중 중복 읽기의 null 참조를 방지한다.
        public bool PanoramaPoseFresh
        {
            get { return _opticalCenterFeature.PanoramaPoseFresh; }
        }

        public string PanoramaDeviceKey
        {
            get { return _opticalCenterFeature.PanoramaDeviceKey; }
        }

        public bool IsPanoramaIrSource
        {
            get { return _opticalCenterFeature.IsPanoramaIrSource; }
        }

        public event Action PositionReferenceChanged
        {
            add
            {
                _opticalCenterFeature.PositionReferenceChanged += value;
            }

            remove
            {
                _opticalCenterFeature.PositionReferenceChanged -= value;
            }
        }

        /// <summary>
        /// 현재 광학 줌과 채널 비율에 따른 파노라마 화각 정보를 반환한다.
        /// </summary>
        public Tuple<double, double> ControlTuningForPanorama(bool ir)
        {
            return _opticalCenterFeature.ControlTuningForPanorama(ir);
        }

        /// <summary>
        /// 최신 위치와 렌즈 증거를 확인한 후 채널 보정값을 저장한다.
        /// </summary>
        public void SaveOpticalCenter(bool ir, Point center)
        {
            _opticalCenterFeature.SaveOpticalCenter(ir, center);
        }

        /// <summary>
        /// 선택 채널의 보정값을 기본 중심으로 복원한다.
        /// </summary>
        public void ResetOpticalCenter(bool ir)
        {
            _opticalCenterFeature.ResetOpticalCenter(ir);
        }

    }
}
