using System;
using System.IO;
using System.Threading;
using System.Windows;
using OpenCvWpfTracking.Services.Control;

// 2026-10-16: 장비·채널별 십자선 보정 상태 연결.
// 코드 정비: 한국어 설명과 함수 간격·region을 정리하며 기존 운용 흐름은 유지한다.
namespace OpenCvWpfTracking.ViewModels.Main
{
    public partial class MainViewModel
    {

        #region [필드 및 상태 정보]

        private OpticalCenterCalibration opticalCenters;

        #endregion

        #region [속성 및 상태 표시]

        private OpticalCenterCalibration OpticalCenters => opticalCenters ?? (opticalCenters = new OpticalCenterCalibration(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config", "OpticalCenters.xml")));

        #endregion

        #region [기능 처리 함수]

        /// <summary>
        /// 인증정보를 제외한 카메라 주소로 보정 저장 키를 구성한다.
        /// </summary>
        private static string CameraIdentity(string address)
        {
            Uri uri;
            return Uri.TryCreate(address, UriKind.Absolute, out uri) ? uri.Scheme + "://" + uri.Host + ":" + uri.Port + uri.AbsolutePath : address;
        }

        /// <summary>
        /// 장비와 제어 주소 및 카메라 채널별 보정 키를 만든다.
        /// </summary>
        private string OpticalCenterKey(bool ir) => (SelectedControlAgentProfile?.DisplayName ?? "DIRECT") + "|" + ControlAgentIp + ":" + ControlAgentPortText + "|" + CameraIdentity(ir ? IrSourceAddress : EoSourceAddress) + "|" + (ir ? "IR" : "EO");
        /// <summary>
        /// 현재 채널의 보정 저장 대상 키를 반환한다.
        /// </summary>
        public string OpticalDeviceKey(bool ir) => OpticalCenterKey(ir);
        /// <summary>
        /// 현재 채널의 표준 광학 줌 위치를 반환한다.
        /// </summary>
        public int OpticalZoom(bool ir) => ir ? GetCurrentIrZoomStandardPosition() : GetCurrentPresetStandardZoom();
        /// <summary>
        /// 현재 장비와 줌 위치에 대응하는 십자선 중심을 반환한다.
        /// </summary>
        public Point GetOpticalCenter(bool ir) => OpticalCenters.Get(OpticalCenterKey(ir), OpticalZoom(ir));
        /// <summary>
        /// 보정에 필요한 렌즈와 위치 응답의 신선도를 검사한다.
        /// </summary>
        public bool OpticalFeedbackFresh(bool ir)
        {
            var pose = Volatile.Read(ref _videoCenterPose);
            long age = pose == null ? long.MaxValue : DateTime.UtcNow.Ticks - pose.Item3;
            return IsLensFresh(ir) && age >= 0 && age < TimeSpan.FromSeconds(2).Ticks;
        }

        /// <summary>
        /// 영상 저장에 사용할 실제 프레임의 연결 상태와 수신 시간을 확인한다.
        /// </summary>
        public bool SnapshotFrameFresh(bool ir)
        {
            long age = DateTime.UtcNow.Ticks - Interlocked.Read(ref _videoCenterFrameTicks[ir ? 1 : 0]);
            return age >= 0 && age < TimeSpan.FromSeconds(1).Ticks && (ir ? IrConnectionStatusText : EoConnectionStatusText) == "Connected";
        }

        #endregion

        #region [속성 및 상태 표시]

        public Point OpticalPose => new Point(_currentPan, _currentTilt);
        public double CurrentPanoramaPan => NormalizeToSignedAngle(_currentPan);
        public double CurrentPanoramaTilt => _currentTilt;

        // 2026-10-15: 원자적으로 읽은 복사본을 사용하여 연결 해제 중 중복 읽기의 null 참조를 방지한다.
        public bool PanoramaPoseFresh
        {
            get
            {
                var pose = Volatile.Read(ref _videoCenterPose);
                if (pose == null)
                    return false;
                long age = DateTime.UtcNow.Ticks - pose.Item3;
                return age >= 0 && age < TimeSpan.FromSeconds(2).Ticks;
            }
        }

        public string PanoramaDeviceKey => (SelectedControlAgentProfile?.DisplayName ?? "DIRECT") + "|" + ControlAgentIp + ":" + ControlAgentPortText;
        public bool IsPanoramaIrSource => _activePanTiltZoomSource == 1;

        #endregion

        #region [필드 및 상태 정보]

        public event Action PositionReferenceChanged;

        #endregion

        #region [기능 처리 함수]

        /// <summary>
        /// 현재 광학 줌과 채널 비율에 따른 파노라마 화각 정보를 반환한다.
        /// </summary>
        public Tuple<double, double> ControlTuningForPanorama(bool ir)
        {
            var tuning = ControlTuningFor(ir);
            double ratio = tuning.Ratio(OpticalZoom(ir));
            double hfov = 2 * Math.Atan(Math.Tan(tuning.WideHorizontalFov * Math.PI / 360) / Math.Max(1, ratio)) * 180 / Math.PI;
            return Tuple.Create(hfov, ir ? 5.0 / 4 : 16.0 / 9);
        }

        /// <summary>
        /// 최신 위치와 렌즈 증거를 확인한 후 채널 보정값을 저장한다.
        /// </summary>
        public void SaveOpticalCenter(bool ir, Point center)
        {
            if (!OpticalFeedbackFresh(ir))
                throw new InvalidOperationException("새 줌/PAN/TILT 수신값이 필요합니다.");
            OpticalCenters.Save(OpticalCenterKey(ir), OpticalZoom(ir), center);
            Common.ConsoleLogHelper.State("OPTICAL CENTER", (ir ? "IR" : "EO") + " / ZOOM=" + OpticalZoom(ir) + " / NORMALIZED=" + center);
        }

        /// <summary>
        /// 선택 채널의 보정값을 기본 중심으로 복원한다.
        /// </summary>
        public void ResetOpticalCenter(bool ir) => OpticalCenters.Reset(OpticalCenterKey(ir));
        /// <summary>
        /// 위치 기준 변경을 등록된 화면에 알린다.
        /// </summary>
        private void InvalidatePanoramaReference() => PositionReferenceChanged?.Invoke();

        #endregion

    }
}
