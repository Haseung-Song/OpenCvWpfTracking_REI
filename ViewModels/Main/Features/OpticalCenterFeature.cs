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
        int _activePanTiltZoomSource { get; set; }

        double _currentPan { get; set; }

        double _currentTilt { get; set; }

        byte[] _digitalCaps { get; }

        bool[] _digitalCapsKnown { get; }

        int[] _digitalState { get; }

        Services.Configuration.DigitalZoomEstimate _eoDigitalEstimate { get; }

        Services.Configuration.DigitalZoomEstimate _irDigitalEstimate { get; }

        ControlAgentProfileOption _requestedControlProfile { get; set; }

        long[] _videoCenterFrameTicks { get; }

        ref global::System.Tuple<double, double, long> _videoCenterPose { get; }

        string ControlAgentIp { get; set; }

        string ControlAgentPortText { get; set; }

        Services.Configuration.LensControlTuning ControlTuningFor(bool ir);

        string EoConnectionStatusText { get; }

        string EoSourceAddress { get; set; }

        int GetCurrentIrZoomStandardPosition();

        ushort GetCurrentPresetStandardZoom();

        string IrConnectionStatusText { get; }

        string IrSourceAddress { get; set; }

        bool IsLensFresh(bool ir);

        ControlAgentProfileOption SelectedControlAgentProfile { get; set; }
    }

    /// <summary>광학 중심 보정 담당 객체입니다. 상태를 보관하고 입력을 받아 결과를 알립니다.</summary>
    internal sealed class OpticalCenterFeature
    {

        #region [협력 객체와 초기화]

        private readonly IOpticalCenterFeatureContext _context;

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
                return new Point(_context._currentPan, _context._currentTilt);
            }
        }

        internal double CurrentPanoramaPan
        {
            get
            {
                return NormalizeToSignedAngle(_context._currentPan);
            }
        }

        internal double CurrentPanoramaTilt
        {
            get { return _context._currentTilt; }
        }

        internal bool PanoramaPoseFresh
        {
            get
            {
                var pose = Volatile.Read(ref _context._videoCenterPose);

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
                return _context._activePanTiltZoomSource == 1;
            }
        }

        internal event Action PositionReferenceChanged;

        #endregion

        #region [동작 처리]

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

        internal string LegacyOpticalCenterKey(bool ir)
        {
            return OpenCvWpfTracking.Services.Configuration.DeviceCatalog.StorageDisplayName((_context._requestedControlProfile ?? _context.SelectedControlAgentProfile)?.DisplayName ?? "DIRECT") + "|" + _context.ControlAgentIp + ":" + _context.ControlAgentPortText + "|" + CameraIdentity(ir ? _context.IrSourceAddress : _context.EoSourceAddress) + "|" + (ir ? "IR" : "EO");
        }

        internal string OpticalDeviceKey(bool ir)
        {
            return OpticalCenterKey(ir);
        }

        internal string OpticalCenterKey(bool ir)
        {
            var profile = _context._requestedControlProfile ?? _context.SelectedControlAgentProfile;

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

        internal int OpticalZoom(bool ir)
        {
            return ir ? _context.GetCurrentIrZoomStandardPosition() : _context.GetCurrentPresetStandardZoom();
        }

        internal bool IsOpticalCalibrationBlockedByDigitalZoom(bool ir)
        {
            int channel = ir ? 1 : 0;

            if (ir && _context._digitalCapsKnown[channel] && (_context._digitalCaps[channel] & 5) != 5)
            {
                return false;
            }

            return (ir ? _context._irDigitalEstimate : _context._eoDigitalEstimate).Enabled || _context._digitalState[channel] == 1;
        }

        internal Point GetOpticalCenter(bool ir)
        {
            return OpticalCenters.Get(OpticalCenterKey(ir), OpticalZoom(ir));
        }

        internal bool OpticalFeedbackFresh(bool ir)
        {
            var pose = Volatile.Read(ref _context._videoCenterPose);

            long age = pose == null ? long.MaxValue : DateTime.UtcNow.Ticks - pose.Item3;

            return _context.IsLensFresh(ir) && age >= 0 && age < TimeSpan.FromSeconds(2).Ticks;
        }

        internal bool SnapshotFrameFresh(bool ir)
        {
            long age = DateTime.UtcNow.Ticks - Interlocked.Read(ref _context._videoCenterFrameTicks[ir ? 1 : 0]);

            return age >= 0 && age < TimeSpan.FromSeconds(1).Ticks && (ir ? _context.IrConnectionStatusText : _context.EoConnectionStatusText) == "Connected";
        }

        internal Tuple<double, double> ControlTuningForPanorama(bool ir)
        {
            var tuning = _context.ControlTuningFor(ir);

            double ratio = tuning.Ratio(OpticalZoom(ir));

            double hfov = 2 * Math.Atan(Math.Tan(tuning.WideHorizontalFov * Math.PI / 360) / Math.Max(1, ratio)) * 180 / Math.PI;

            return Tuple.Create(hfov, ir ? 5.0 / 4 : 16.0 / 9);
        }

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

        internal void ResetOpticalCenter(bool ir)
        {
            OpticalCenters.Reset(OpticalCenterKey(ir));

            InvalidatePanoramaReference();
        }

        internal void InvalidatePanoramaReference()
        {
            PositionReferenceChanged?.Invoke();
        }

        #endregion

    }
}
