using System;
using System.IO;
using System.Threading;
using System.Windows;
using OpenCvWpfTracking.Services.Control;
using static OpenCvWpfTracking.ViewModels.Main.MainViewModel;

namespace OpenCvWpfTracking.ViewModels.Main
{
    /// <summary>광학 중심 보정에 필요한 협력 기능만 받는 경계입니다. 화면 객체의 내부 구현을 알지 않습니다.</summary>
    internal interface IOpticalCenterFeatureContext
    {
        int ActivePanTiltZoomSource { get; set; }

        double CurrentPan { get; set; }

        double CurrentTilt { get; set; }

        byte[] DigitalCaps { get; }

        bool[] DigitalCapsKnown { get; }

        int[] DigitalState { get; }

        Services.Configuration.DigitalZoomEstimate EoDigitalEstimate { get; }

        Services.Configuration.DigitalZoomEstimate IrDigitalEstimate { get; }

        ControlAgentProfileOption RequestedControlProfile { get; set; }

        long[] VideoCenterFrameTicks { get; }

        ref global::System.Tuple<double, double, long> VideoCenterPose { get; }

        string ControlAgentIp { get; set; }

        string ControlAgentPortText { get; set; }

        /// <summary>
        /// 연결 요청 중인 프로파일을 우선하여 해당 채널의 조정값을 가져온다.
        /// </summary>
        Services.Configuration.LensControlTuning ControlTuningFor(bool ir);

        string EoConnectionStatusText { get; }

        string EoSourceAddress { get; set; }

        /// <summary>
        /// 현재 장비 구성에 맞춰 IR Zoom 상태값을 0~1000 위치로 반환한다. ROOFTOP / LA AGENT: Raw 1000(Wide)
        /// ->; Standard 0(Wide) Raw 0(Tele) ->; Standard 1000(Tele) ENVIRONMENT / WEB
        /// AGENT: 장비 상태값과 화면 Position 방향이 같으므로 역변환하지 않는다.
        /// </summary>
        int GetCurrentIrZoomStandardPosition();

        /// <summary>
        /// 현재 EO Zoom 값을 프리셋 공통 범위 0~1000으로 변환한다.
        /// </summary>
        ushort GetCurrentPresetStandardZoom();

        string IrConnectionStatusText { get; }

        string IrSourceAddress { get; set; }

        /// <summary>
        /// 연결 요청과 마지막 렌즈 응답 시간을 기준으로 유효성을 확인한다.
        /// </summary>
        bool IsLensFresh(bool ir);

        ControlAgentProfileOption SelectedControlAgentProfile { get; set; }
    }

    /// <summary>광학 중심 보정 담당 객체입니다. 상태를 보관하고 입력을 받아 결과를 알립니다.</summary>
    internal sealed class OpticalCenterFeature
    {

        #region [협력 객체와 초기화]

        private readonly IOpticalCenterFeatureContext _context;

        /// <summary>
        /// 광학 중심 보정 기능에서 사용할 협력 객체를 연결하고 초기 상태를 준비한다.
        /// </summary>
        internal OpticalCenterFeature(IOpticalCenterFeatureContext context)
        {
            _context = context ?? throw new System.ArgumentNullException(nameof(context));
        }

        #endregion

        #region [상태 필드]

        internal OpticalCenterCalibration opticalCenters;

        internal readonly string[] _opticalIdentityKeys = new string[2];

        internal readonly string[] _opticalResolvedKeys = new string[2];

        #endregion

        #region [화면 속성과 이벤트]

        internal OpticalCenterCalibration OpticalCenters
        {
            get
            {
                return opticalCenters ?? (opticalCenters = new OpticalCenterCalibration(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config", "OpticalCenters.xml")));
            }
        }

        internal Point OpticalPose
        {
            get
            {
                return new Point(_context.CurrentPan, _context.CurrentTilt);
            }
        }

        internal double CurrentPanoramaPan
        {
            get
            {
                return NormalizeToSignedAngle(_context.CurrentPan);
            }
        }

        internal double CurrentPanoramaTilt
        {
            get { return _context.CurrentTilt; }
        }

        internal bool PanoramaPoseFresh
        {
            get
            {
                var pose = Volatile.Read(ref _context.VideoCenterPose);

                if (pose == null)
                {
                    return false;
                }

                long age = DateTime.UtcNow.Ticks - pose.Item3;

                return age >= 0 && age < TimeSpan.FromSeconds(2).Ticks;
            }
        }

        internal string PanoramaDeviceKey
        {
            get
            {
                return OpenCvWpfTracking.Services.Configuration.DeviceCatalog.StorageDisplayName(_context.SelectedControlAgentProfile?.DisplayName ?? "DIRECT") + "|" + _context.ControlAgentIp + ":" + _context.ControlAgentPortText;
            }
        }

        internal bool IsPanoramaIrSource
        {
            get
            {
                return _context.ActivePanTiltZoomSource == 1;
            }
        }

        internal event Action PositionReferenceChanged;

        #endregion

        #region [동작 처리]

        /// <summary>
        /// 인증정보를 제외한 카메라 주소로 보정 저장 키를 구성한다.
        /// </summary>
        internal static string CameraIdentity(string address)
        {
            Uri uri;

            if (Uri.TryCreate(address, UriKind.Absolute, out uri))
            {
                return uri.Scheme + "://" + uri.Host + ":" + uri.Port + uri.AbsolutePath;
            }
            else
            {
                return address;
            }
        }

        /// <summary>
        /// 이전 보정값 이전에 사용할 주소 기준 키를 유지한다.
        /// </summary>
        internal string LegacyOpticalCenterKey(bool ir)
        {
            return OpenCvWpfTracking.Services.Configuration.DeviceCatalog.StorageDisplayName((_context.RequestedControlProfile ?? _context.SelectedControlAgentProfile)?.DisplayName ?? "DIRECT") + "|" + _context.ControlAgentIp + ":" + _context.ControlAgentPortText + "|" + CameraIdentity(ir ? _context.IrSourceAddress : _context.EoSourceAddress) + "|" + (ir ? "IR" : "EO");
        }

        /// <summary>
        /// 현재 채널의 보정 저장 대상 키를 반환한다.
        /// </summary>
        internal string OpticalDeviceKey(bool ir)
        {
            return OpticalCenterKey(ir);
        }

        /// <summary>
        /// 등록 장비는 IP 변경과 무관하게 보정값을 유지하고 EO·IR 및 LA·WEB은 분리한다.
        /// </summary>
        internal string OpticalCenterKey(bool ir)
        {
            var profile = _context.RequestedControlProfile ?? _context.SelectedControlAgentProfile;

            string stable = OpticalDeviceIdentity.Key(profile?.DeviceId, profile?.AgentType.ToString(), ir, null);

            if (stable == null)
            {
                return LegacyOpticalCenterKey(ir);
            }

            int channel = ir ? 1 : 0;

            if (_opticalIdentityKeys[channel] != stable)
            {
                _opticalResolvedKeys[channel] = OpticalCenters.ResolveIdentity(stable, LegacyOpticalCenterKey(ir), profile?.DisplayName, ir);

                _opticalIdentityKeys[channel] = stable;
            }

            return _opticalResolvedKeys[channel];
        }

        /// <summary>
        /// 현재 채널의 표준 광학 줌 위치를 반환한다.
        /// </summary>
        internal int OpticalZoom(bool ir)
        {
            return ir ? _context.GetCurrentIrZoomStandardPosition() : _context.GetCurrentPresetStandardZoom();
        }

        /// <summary>
        /// 성공한 D-ZOOM ON 요청 또는 장비 ON 응답이 있는 채널의 4점 보정을 막는다. 미수신을 ON으로 취급하지 않으며 D-ZOOM 미지원
        /// IR의 보정은 허용한다.
        /// </summary>
        internal bool IsOpticalCalibrationBlockedByDigitalZoom(bool ir)
        {
            int channel = ir ? 1 : 0;

            if (ir && _context.DigitalCapsKnown[channel] && (_context.DigitalCaps[channel] & 5) != 5)
            {
                return false;
            }

            return (ir ? _context.IrDigitalEstimate : _context.EoDigitalEstimate).Enabled || _context.DigitalState[channel] == 1;
        }

        /// <summary>
        /// 현재 장비와 줌 위치에 대응하는 십자선 중심을 반환한다.
        /// </summary>
        internal Point GetOpticalCenter(bool ir)
        {
            return OpticalCenters.Get(OpticalCenterKey(ir), OpticalZoom(ir));
        }

        /// <summary>
        /// 보정에 필요한 렌즈와 위치 응답의 신선도를 검사한다.
        /// </summary>
        internal bool OpticalFeedbackFresh(bool ir)
        {
            var pose = Volatile.Read(ref _context.VideoCenterPose);

            long age = pose == null ? long.MaxValue : DateTime.UtcNow.Ticks - pose.Item3;

            return _context.IsLensFresh(ir) && age >= 0 && age < TimeSpan.FromSeconds(2).Ticks;
        }

        /// <summary>
        /// 영상 저장에 사용할 실제 프레임의 연결 상태와 수신 시간을 확인한다.
        /// </summary>
        internal bool SnapshotFrameFresh(bool ir)
        {
            long age = DateTime.UtcNow.Ticks - Interlocked.Read(ref _context.VideoCenterFrameTicks[ir ? 1 : 0]);

            return age >= 0 && age < TimeSpan.FromSeconds(1).Ticks && (ir ? _context.IrConnectionStatusText : _context.EoConnectionStatusText) == "Connected";
        }

        /// <summary>
        /// 현재 광학 줌과 채널 비율에 따른 파노라마 화각 정보를 반환한다.
        /// </summary>
        internal Tuple<double, double> ControlTuningForPanorama(bool ir)
        {
            var tuning = _context.ControlTuningFor(ir);

            double ratio = tuning.Ratio(OpticalZoom(ir));

            double hfov = 2 * Math.Atan(Math.Tan(tuning.WideHorizontalFov * Math.PI / 360) / Math.Max(1, ratio)) * 180 / Math.PI;

            return Tuple.Create(hfov, ir ? 5.0 / 4 : 16.0 / 9);
        }

        /// <summary>
        /// 최신 위치와 렌즈 증거를 확인한 후 채널 보정값을 저장한다.
        /// </summary>
        internal void SaveOpticalCenter(bool ir, Point center)
        {
            if (!OpticalFeedbackFresh(ir))
            {
                throw new InvalidOperationException("새 줌/PAN/TILT 수신값이 필요합니다.");
            }

            OpticalCenters.Save(OpticalCenterKey(ir), OpticalZoom(ir), center);

            InvalidatePanoramaReference();

            Common.ConsoleLogHelper.State("OPTICAL CENTER", (ir ? "IR" : "EO") + " / ZOOM=" + OpticalZoom(ir) + " / NORMALIZED=" + center);
        }

        /// <summary>
        /// 선택 채널의 보정값을 기본 중심으로 복원한다.
        /// </summary>
        internal void ResetOpticalCenter(bool ir)
        {
            OpticalCenters.Reset(OpticalCenterKey(ir));

            InvalidatePanoramaReference();
        }

        /// <summary>
        /// 위치 기준 변경을 등록된 화면에 알린다.
        /// </summary>
        internal void InvalidatePanoramaReference()
        {
            PositionReferenceChanged?.Invoke();
        }

        #endregion

    }
}
