using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using OpenCvWpfTracking.Common;
using static OpenCvWpfTracking.ViewModels.Main.MainViewModel;

namespace OpenCvWpfTracking.ViewModels.Main
{
    /// <summary>카메라 특수 기능 제어에 필요한 협력 기능만 받는 경계입니다. 화면 객체의 내부 구현을 알지 않습니다.</summary>
    internal interface ICameraFeatureControlFeatureContext
    {
        ref double? _activePanAbsoluteTarget { get; }

        ref double? _activeTiltAbsoluteTarget { get; }

        Services.Communication.IControlCommandServicePort _controlCommandService { get; }

        System.Collections.Generic.Dictionary<string, Tuple<string, int, int>> _controlInputs { get; }

        int _inputEpoch { get; set; }

        ref long _irLensReceivedTicks { get; }

        bool _isDeviceConnectionRequested { get; set; }

        ref double? _lastPanAbsoluteTarget { get; }

        Services.Communication.ITcpClientServicePort _laTcpService { get; }

        ControlAgentProfileOption _requestedControlProfile { get; set; }

        Services.Configuration.LensControlTuning ControlTuningFor(bool ir);

        double CurrentMoveControlTiltMaximum { get; }

        double CurrentMoveControlTiltMinimum { get; }

        string EoStatusText { get; set; }

        int GetCurrentIrZoomStandardPosition();

        ushort GetCurrentPresetStandardZoom();

        Point GetOpticalCenter(bool ir);

        string IrStatusText { get; set; }

        bool IsControlInputLocked { get; }

        bool IsEnvironmentStatusSelected { get; }

        bool IsFovSpeedEnabled { get; set; }

        bool IsLensFresh(bool ir);

        bool IsOperationCommandEnabled { get; }

        void OnPropertyChanged([global::System.Runtime.CompilerServices.CallerMemberName] string propertyName = null);

        byte PanTiltSpeedLevel { get; set; }

        void ResetPresetLensEvidence();

        void ResetSpecialFeatureSession();

        void ResetWebAgentControlSession();

        ControlAgentProfileOption SelectedControlAgentProfile { get; set; }

        void SetActivePanTiltZoomSource(bool isInfrared);

        void StopAllPtzfMove();
    }

    /// <summary>카메라 특수 기능 제어 담당 객체입니다. 상태를 보관하고 입력을 받아 결과를 알립니다.</summary>
    internal sealed class CameraFeatureControlFeature
    {

        #region [협력 객체와 초기화]

        private readonly ICameraFeatureControlFeatureContext _context;

        internal CameraFeatureControlFeature(ICameraFeatureControlFeatureContext context)
        {
            _context = context ?? throw new System.ArgumentNullException(nameof(context));
        }

        internal void InitializeState()
        {
            _digitalEstimateTimestamp = System.Diagnostics.Stopwatch.GetTimestamp();
        }

        #endregion

        #region [상태 필드]

        internal readonly byte[] _digitalCaps = new byte[2];

        internal readonly Services.Configuration.DigitalZoomEstimate _eoDigitalEstimate = new Services.Configuration.DigitalZoomEstimate();

        internal readonly Services.Configuration.DigitalZoomEstimate _irDigitalEstimate = new Services.Configuration.DigitalZoomEstimate();

        internal long _digitalEstimateTimestamp;

        internal readonly bool[] _digitalCapsKnown = new bool[2];

        internal readonly int[] _digitalState =
        {
            -1,
            -1
        };

        internal readonly int[] _digitalRequestedState =
        {
            -1,
            -1
        };

        internal readonly bool[] _digitalTxFailed = new bool[2];

        internal readonly long[] _digitalStateTicks = new long[2];

        internal readonly long[] _digitalPendingTicks = new long[2];

        internal readonly long[] _digitalNextQueryTicks = new long[2];

        internal readonly long[] _videoCenterFrameTicks = new long[2];

        internal long _lastDigitalQueryTicks;

        internal Tuple<double, double, long> _videoCenterPose;

        internal bool _videoCenterBusy;

        internal long _lastCenterClickTicks;

        internal long _centerDeadlineTicks;

        internal int _centerMoveEpoch;

        internal string _lastDigitalUiSignature;

        #endregion

        #region [화면 속성과 이벤트]

        internal double EoDisplayZoomRatio
        {
            get
            {
                return _context.ControlTuningFor(false).Ratio(_context.GetCurrentPresetStandardZoom()) * (_context.GetCurrentPresetStandardZoom() >= 1000 && _eoDigitalEstimate.Enabled ? _eoDigitalEstimate.Factor : 1);
            }
        }

        internal double IrDisplayZoomRatio
        {
            get
            {
                return _context.ControlTuningFor(true).Ratio(_context.GetCurrentIrZoomStandardPosition()) * (_context.GetCurrentIrZoomStandardPosition() >= 1000 && _irDigitalEstimate.Enabled ? _irDigitalEstimate.Factor : 1);
            }
        }

        internal bool CameraFeatureTransportReady
        {
            get
            {
                return _context._isDeviceConnectionRequested && _context._laTcpService.IsConnected;
            }
        }

        internal bool CanSetEoDigitalZoom
        {
            get
            {
                return CanSetDigitalZoom(0);
            }
        }

        internal bool CanSetIrDigitalZoom
        {
            get
            {
                return CanSetDigitalZoom(1);
            }
        }

        internal string EoDigitalZoomStatusText
        {
            get
            {
                return DigitalStatus(0);
            }
        }

        internal string IrDigitalZoomStatusText
        {
            get
            {
                return DigitalStatus(1);
            }
        }

        internal bool IsVideoCenterMoveActive
        {
            get
            {
                return _videoCenterBusy || _context._activePanAbsoluteTarget.HasValue || _context._activeTiltAbsoluteTarget.HasValue;
            }
        }

        #endregion

        #region [동작 처리]

        internal void TickDigitalEstimate()
        {
            long now = System.Diagnostics.Stopwatch.GetTimestamp();

            double elapsed = (now - _digitalEstimateTimestamp) / (double)System.Diagnostics.Stopwatch.Frequency;

            _digitalEstimateTimestamp = now;

            AdvanceDigitalEstimate(false, elapsed);

            AdvanceDigitalEstimate(true, elapsed);
        }

        internal void AdvanceDigitalEstimate(bool ir, double elapsed)
        {
            var estimate = ir ? _irDigitalEstimate : _eoDigitalEstimate;

            var tuning = _context.ControlTuningFor(ir);

            bool fresh = CameraFeatureTransportReady && _context.IsLensFresh(ir) && _context.IsOperationCommandEnabled && !_context.IsControlInputLocked;

            bool atMaximum = (ir ? _context.GetCurrentIrZoomStandardPosition() : _context.GetCurrentPresetStandardZoom()) >= 1000;

            // 2026-10-13: 광학 위치 1000만으로 디지털 줌 ON 상태를 판단하지 않는다.
            // ON 요청 전송 성공 또는 유효한 장비 ON 응답이 있을 때만 추정 모델을 켠다.
            estimate.Advance(elapsed, fresh, atMaximum, tuning.DigitalEstimateMaximum, tuning.DigitalEstimateSeconds);

            if (!CameraFeatureTransportReady)
            {
                estimate.Enable(false);
            }
        }

        internal void SetDigitalEstimateDirection(int direction, bool ir = false)
        {
            TickDigitalEstimate();

            (ir ? _irDigitalEstimate : _eoDigitalEstimate).Direction = direction;
        }

        internal bool CanSetDigitalZoom(int i)
        {
            return CameraFeatureTransportReady && _context.IsOperationCommandEnabled && !_context.IsControlInputLocked && (i == 1 || (_digitalCapsKnown[i] && (_digitalCaps[i] & 5) == 5)) && _digitalPendingTicks[i] == 0;
        }

        internal string DigitalStatus(int i)
        {
            if (!CameraFeatureTransportReady)
            {
                return "D-ZOOM · 미연결";
            }

            if (i == 1 && _digitalRequestedState[i] < 0 && _digitalCapsKnown[i] && (_digitalCaps[i] & 5) != 5)
            {
                return "D-ZOOM · IR 설정 미지원";
            }

            if (i == 1 && _digitalRequestedState[i] < 0 && !_digitalCapsKnown[i])
            {
                return "D-ZOOM · IR 지원 조회 중";
            }

            if (_digitalTxFailed[i])
            {
                return "D-ZOOM · 요청 전송 실패";
            }

            if (_digitalState[i] >= 0 && DateTime.UtcNow.Ticks - _digitalStateTicks[i] <= TimeSpan.FromSeconds(10).Ticks)
            {
                return "장비 " + (_digitalState[i] == 0 ? "OFF" : "ON") + " 확인" + (_digitalRequestedState[i] >= 0 && _digitalRequestedState[i] != _digitalState[i] ? " · 요청 불일치" : "");
            }

            if (_digitalRequestedState[i] >= 0)
            {
                return (_digitalRequestedState[i] == 0 ? "OFF" : "ON") + " 요청 전송 · " + (_digitalPendingTicks[i] != 0 ? "응답 대기" : "적용 미확인");
            }

            if (_digitalCapsKnown[i] && (_digitalCaps[i] & 2) == 0)
            {
                return "D-ZOOM · 상태 조회 미지원";
            }

            return "D-ZOOM · 장비 상태 미수신";
        }

        internal void NotifyDigitalZoom()
        {
            foreach (string n in DigitalZoomPropertyNames)
            {
                _context.OnPropertyChanged(n);
            }
        }

        internal void ResetDigitalZoomSession()
        {
            _context.ResetWebAgentControlSession();

            _context.ResetSpecialFeatureSession();

            _context.ResetPresetLensEvidence();

            _eoDigitalEstimate.Enable(false);

            _irDigitalEstimate.Enable(false);

            for (int i = 0; i < 2; i++)
            {
                _digitalCaps[i] = 0;

                _digitalCapsKnown[i] = false;

                _digitalState[i] = -1;

                _digitalRequestedState[i] = -1;

                _digitalTxFailed[i] = false;

                _digitalStateTicks[i] = 0;

                _digitalPendingTicks[i] = 0;

                _digitalNextQueryTicks[i] = 0;

                _videoCenterFrameTicks[i] = 0;
            }

            Interlocked.Exchange(ref _videoCenterPose, null);

            NotifyDigitalZoom();
        }

        internal void QueryDigitalZoomSupport()
        {
            if (!CameraFeatureTransportReady || DateTime.UtcNow.Ticks - _lastDigitalQueryTicks < TimeSpan.FromSeconds(1).Ticks)
            {
                return;
            }

            _lastDigitalQueryTicks = DateTime.UtcNow.Ticks;

            for (byte i = 0; i < 2; i++)
            {
                _context._controlCommandService.RequestCameraFeatureCapability(i);
            }

            ConsoleLogHelper.Command("DIGITAL ZOOM", "EO/IR support query / E7");
        }

        internal void ApplyDigitalZoomCapability(byte[] p)
        {
            if (p.Length < 13 || p[0] != 2 || p[1] > 1 || p.Length < 13 + 12 * p[12])
            {
                return;
            }

            int i = p[1];

            byte flags = 0;

            for (int o = 13; o + 12 <= 13 + 12 * p[12]; o += 12)
            {
                if (p[o] == 0x1D)
                {
                    // 켜짐/꺼짐 값 0~1만 허용하며 알 수 없는 새 값 형식은 제어를 활성화하지 않는다.
                    if (p[o + 2] == 0 && BitConverter.ToInt16(p, o + 4) == 0 && BitConverter.ToInt16(p, o + 6) == 1)
                    {
                        flags = p[o + 1];
                    }

                    break;
                }
            }

            _digitalCaps[i] = flags;

            _digitalCapsKnown[i] = true;

            _digitalState[i] = -1;

            // 디지털 줌 설정을 지원하지 않는 IR 카메라에서는 추정값도 활성화하지 않는다.
            if (i == 1 && (flags & 5) != 5)
            {
                _irDigitalEstimate.Enable(false);

                _digitalRequestedState[i] = -1;

                _digitalPendingTicks[i] = 0;
            }

            // 2026-10-12: 설정만 지원하는 카메라와 상태 응답 누락을 구분한다.
            ConsoleLogHelper.State("DIGITAL ZOOM", "CAPABILITY / TARGET=" + i + " / FLAGS=0x" + flags.ToString("X2") + " / SUPPORTED=" + ((flags & 1) != 0) + " / READABLE=" + ((flags & 2) != 0) + " / SETTABLE=" + ((flags & 4) != 0));

            if ((flags & 3) == 3)
            {
                _context._controlCommandService.RequestCameraFeatureState((byte)i, 0x1D);
            }

            _digitalNextQueryTicks[i] = DateTime.UtcNow.AddSeconds(5).Ticks;

            NotifyDigitalZoom();
        }

        internal void ApplyDigitalZoomState(byte[] p)
        {
            if (p.Length < 18 || p[1] > 1 || p[2] != 0x1D)
            {
                return;
            }

            int i = p[1], value = BitConverter.ToInt16(p, 4);

            _digitalState[i] = (p[3] & 3) == 3 && (value == 0 || value == 1) ? value : -1;

            if (_digitalState[i] >= 0)
            {
                TickDigitalEstimate();

                (i == 0 ? _eoDigitalEstimate : _irDigitalEstimate).Enable(_digitalState[i] == 1 && (i == 0 || (_digitalCapsKnown[i] && (_digitalCaps[i] & 5) == 5)));
            }

            _digitalStateTicks[i] = DateTime.UtcNow.Ticks;

            _digitalPendingTicks[i] = 0;

            NotifyDigitalZoom();
        }

        internal bool SetDigitalZoom(bool ir, bool on)
        {
            int i = ir ? 1 : 0;

            // EO의 기존 설정 전용 흐름을 유지하며 IR도 지원 조회만으로 차단하지 않는다.
            // 실제 전송 결과와 장비 적용 확인은 별도로 유지한다.
            if (_context.IsControlInputLocked || !_context.IsOperationCommandEnabled)
            {
                ConsoleLogHelper.Warning("DIGITAL ZOOM", "Rejected / TARGET=" + i + " / operation locked");

                return false;
            }

            // IR은 미연결·운용 잠금·중복 요청만 차단한다.
            if (ir && !CanSetIrDigitalZoom)
            {
                ConsoleLogHelper.Warning("DIGITAL ZOOM", "IR 요청 거절 / 연결·운용 잠금·응답 대기 확인 필요");

                return false;
            }

            bool sent = _context._controlCommandService.SetCameraFeature(0x1D, (byte)i, (byte)(on ? 1 : 0));

            _digitalTxFailed[i] = !sent;

            _digitalRequestedState[i] = on ? 1 : 0;

            if (sent)
            {
                _digitalState[i] = -1;

                _digitalPendingTicks[i] = DateTime.UtcNow.Ticks;

                TickDigitalEstimate();

                // 지원 미확인 IR에는 요청만 보내며 배율 추정을 실제 상태로 가정하지 않는다.
                (i == 0 ? _eoDigitalEstimate : _irDigitalEstimate).Enable(on && (i == 0 || (_digitalCapsKnown[i] && (_digitalCaps[i] & 5) == 5)));

                ConsoleLogHelper.State("D-ZOOM ESTIMATE", "CAMERA=" + (ir ? "IR" : "EO") + " / LOCAL REQUEST / ENABLED=" + on + " / MAX=" + _context.ControlTuningFor(ir).DigitalEstimateMaximum + " / SECONDS=" + _context.ControlTuningFor(ir).DigitalEstimateSeconds + " / ESTIMATED / NOT TELEMETRY");
            }

            ConsoleLogHelper.Command("DIGITAL ZOOM", "TARGET=" + i + " / REQUEST=" + (on ? "ON" : "OFF") + " / TX=" + sent + " / not a device ACK");

            NotifyDigitalZoom();

            return sent;
        }

        internal void ApplyDigitalZoomUnavailable(int target)
        {
            TickDigitalEstimate();

            _digitalPendingTicks[target] = 0;

            _digitalState[target] = -1;

            _digitalRequestedState[target] = -1;

            _digitalCaps[target] = 0;

            _digitalCapsKnown[target] = true;

            (target == 0 ? _eoDigitalEstimate : _irDigitalEstimate).Enable(false);

            ConsoleLogHelper.Warning("D-ZOOM ESTIMATE", "CAMERA=" + (target == 0 ? "EO" : "IR") + " / DEVICE REJECTED / ESTIMATE RESET");

            NotifyDigitalZoom();
        }

        internal void TickDigitalZoom()
        {
            if (_centerDeadlineTicks != 0)
            {
                if (_centerMoveEpoch != _context._inputEpoch || (!_context._activePanAbsoluteTarget.HasValue && !_context._activeTiltAbsoluteTarget.HasValue))
                {
                    _centerDeadlineTicks = 0;
                }
                else if (DateTime.UtcNow.Ticks >= _centerDeadlineTicks)
                {
                    _context.StopAllPtzfMove();

                    ConsoleLogHelper.Warning("VIDEO CENTER", "Arrival not confirmed within deadline; stop requested");
                }
            }

            if (!CameraFeatureTransportReady)
            {
                if (_digitalCapsKnown[0] || _digitalCapsKnown[1])
                {
                    ResetDigitalZoomSession();
                }

                return;
            }

            for (byte i = 0; i < 2; i++)
            {
                if (_digitalPendingTicks[i] != 0 && DateTime.UtcNow.Ticks - _digitalPendingTicks[i] > TimeSpan.FromSeconds(2).Ticks)
                {
                    _digitalPendingTicks[i] = 0;

                    if ((_digitalCaps[i] & 2) != 0)
                    {
                        _context._controlCommandService.RequestCameraFeatureState(i, 0x1D);
                    }

                    NotifyDigitalZoom();
                }
            }

            for (byte i = 0; i < 2; i++)
            {
                if ((_digitalCaps[i] & 3) == 3 && _digitalPendingTicks[i] == 0 && DateTime.UtcNow.Ticks >= _digitalNextQueryTicks[i])
                {
                    _digitalNextQueryTicks[i] = DateTime.UtcNow.AddSeconds(5).Ticks;

                    _context._controlCommandService.RequestCameraFeatureState(i, 0x1D);
                }
            }

            string signature = EoDigitalZoomStatusText + IrDigitalZoomStatusText + CanSetEoDigitalZoom + CanSetIrDigitalZoom;

            if (signature != _lastDigitalUiSignature)
            {
                _lastDigitalUiSignature = signature;

                NotifyDigitalZoom();
            }
        }

        internal static Tuple<double, double> VideoCenterTarget(double pan, double tilt, double nx, double ny, double hfov, double aspect)
        {
            if (new[]
            {
                pan,
                tilt,
                nx,
                ny,
                hfov,
                aspect
            }.AnyInvalid() || Math.Abs(nx) > 1 || Math.Abs(ny) > 1 || hfov <= 0 || hfov >= 170 || aspect <= 0 || Math.Abs(tilt) > 85)
            {
                return null;
            }

            double r = Math.PI / 180, x = nx * Math.Tan(hfov * r / 2), y = -ny * Math.Tan(hfov * r / 2) / aspect;

            double forward = Math.Cos(tilt * r) - y * Math.Sin(tilt * r), up = Math.Sin(tilt * r) + y * Math.Cos(tilt * r);

            double dp = Math.Atan2(x, forward) / r, dt = Math.Atan2(up, Math.Sqrt(x * x + forward * forward)) / r;

            double targetPan = (pan + dp + 540) % 360 - 180;

            return Tuple.Create(targetPan, dt);
        }

        internal async Task<bool> CenterVideoPointAsync(bool ir, double nx, double ny, double aspect, int sourceWidth = 0, int sourceHeight = 0)
        {
            var pose = Volatile.Read(ref _videoCenterPose);

            long now = DateTime.UtcNow.Ticks;

            string status = ir ? _context.IrStatusText : _context.EoStatusText;

            bool approximateIr = ir && !_context.IsLensFresh(true);

            if (_videoCenterBusy || _context._activePanAbsoluteTarget.HasValue || _context._activeTiltAbsoluteTarget.HasValue || !CameraFeatureTransportReady || _context.IsControlInputLocked || !_context.IsOperationCommandEnabled || _context._controlInputs.Count > 0 || pose == null || now - pose.Item3 > TimeSpan.FromSeconds(2).Ticks || (!ir && !_context.IsLensFresh(false)) || now - Interlocked.Read(ref _videoCenterFrameTicks[ir ? 1 : 0]) > TimeSpan.FromSeconds(1).Ticks || status == null || !status.EndsWith("] Connected", StringComparison.Ordinal) || now - _lastCenterClickTicks < TimeSpan.FromMilliseconds(300).Ticks)
            {
                ConsoleLogHelper.Warning("VIDEO CENTER", "Rejected / stale video, pose/lens, lock, connection or active input / CAMERA=" + (ir ? "IR" : "EO"));

                return false;
            }

            var profile = _context._requestedControlProfile ?? _context.SelectedControlAgentProfile;

            string deviceId = Services.Configuration.DeviceCatalog.CanonicalDeviceId(profile?.DeviceId);

            var geometry = Services.Configuration.ClickProjectionSettings.Get(deviceId, ir);

            double sourceX = nx, sourceY = ny, sourceAspect = aspect;

            double mappedX, mappedY, mappedAspect;

            if (!geometry.TryMap(nx, ny, aspect, out mappedX, out mappedY, out mappedAspect))
            {
                ConsoleLogHelper.Warning("VIDEO CENTER", "클릭 영상 영역 밖 / DEVICE_ID=" + deviceId + " / CAMERA=" + (ir ? "IR" : "EO") + " / 명령 미송신");

                return false;
            }

            nx = mappedX;
            ny = mappedY;
            aspect = mappedAspect;

            var tuning = _context.ControlTuningFor(ir);

            int zoom = ir ? _context.GetCurrentIrZoomStandardPosition() : _context.GetCurrentPresetStandardZoom();

            if (approximateIr && Interlocked.Read(ref _context._irLensReceivedTicks) == 0)
            {
                zoom = 0; // 이번 세션에 줌 응답이 없으면 초기 Raw 값을 실제 Tele 배율로 해석하지 않는다.
            }

            TickDigitalEstimate();

            double opticalRatio = tuning.Ratio(zoom);

            var estimate = ir ? _irDigitalEstimate : _eoDigitalEstimate;

            double digitalFactor = zoom >= 1000 && estimate.Enabled ? estimate.Factor : 1;

            double effectiveRatio = opticalRatio * digitalFactor;

            // 확인된 광각 기준을 유지하고 각도나 PT 이동 시간이 아닌
            // 탄젠트 공간에서 비율을 조정한다. 디지털 배율은 추정값임을 유지한다.
            double wideFov = geometry.ResolveWideHorizontalFov(tuning.WideHorizontalFov);

            double hfov = 2 * Math.Atan(Math.Tan(wideFov * Math.PI / 360) / effectiveRatio) * 180 / Math.PI;

            var opticalCenter = _context.GetOpticalCenter(ir);

            nx -= 2 * (geometry.CenterX(opticalCenter.X) - .5);

            ny -= 2 * (geometry.CenterY(opticalCenter.Y) - .5);

            // 보정 중심이 영상 중앙에서 벗어나 가장자리 클릭 좌표가
            // 기존 [-1,1] 범위를 넘어가더라도 같은 투영 광선을 유지한다.
            double rayScale = Math.Max(1, Math.Max(Math.Abs(nx), Math.Abs(ny)));

            double projectionFov = 2 * Math.Atan(Math.Tan(hfov * Math.PI / 360) * rayScale) * 180 / Math.PI;

            var target = VideoCenterTarget(pose.Item1, pose.Item2, nx / rayScale, ny / rayScale, projectionFov, aspect);

            if (target == null)
            {
                return false;
            }

            if (approximateIr)
            {
                // 배율이 미확인인 IR도 방향 선택은 허용한다. 정확한 중심 도달로 가장하지 않으며
                // 한 번에 각 축 최대 3°만 요청하여 오래된 배율로 큰 이동이 발생하지 않게 한다.
                double deltaPan = (target.Item1 - pose.Item1 + 540) % 360 - 180;

                target = Tuple.Create((pose.Item1 + Clamp(deltaPan, -3.0, 3.0) + 540) % 360 - 180, pose.Item2 + Clamp(target.Item2 - pose.Item2, -3.0, 3.0));
            }

            double tilt = Clamp(target.Item2, _context.CurrentMoveControlTiltMinimum, _context.CurrentMoveControlTiltMaximum);

            if (Math.Abs(nx) < .005 && Math.Abs(ny) < .005)
            {
                return true;
            }

            _videoCenterBusy = true;
            _lastCenterClickTicks = now;

            int epoch = _context._inputEpoch;

            try
            {
                _context.SetActivePanTiltZoomSource(ir);

                double speed = tuning.Speed(_context.PanTiltSpeedLevel, zoom, _context.IsFovSpeedEnabled);

                if (approximateIr)
                {
                    speed = Math.Min(speed, 10);
                }

                if (!_context._controlCommandService.SetPanPositionSpeed(speed) || !_context._controlCommandService.SetTiltPositionSpeed(speed) || !_context._controlCommandService.SetPanShortestPathMode())
                {
                    return false;
                }

                await Task.Delay(_context.IsEnvironmentStatusSelected ? 100 : 80);

                if (epoch != _context._inputEpoch || !CameraFeatureTransportReady || _context.IsControlInputLocked || !_context.IsOperationCommandEnabled || _context._controlInputs.Count > 0)
                {
                    return false;
                }

                bool panSent = _context._controlCommandService.PanGoPosition(target.Item1);

                await Task.Delay(_context.IsEnvironmentStatusSelected ? 60 : 0);

                if (epoch != _context._inputEpoch || !CameraFeatureTransportReady || _context.IsControlInputLocked || !_context.IsOperationCommandEnabled || _context._controlInputs.Count > 0)
                {
                    _context._controlCommandService.StopPanTiltPositionMove();

                    return false;
                }

                bool tiltSent = panSent && _context._controlCommandService.TiltGoPosition(tilt);

                if (!tiltSent)
                {
                    _context._controlCommandService.StopPanTiltPositionMove();

                    return false;
                }

                _context._activePanAbsoluteTarget = target.Item1;
                _context._activeTiltAbsoluteTarget = tilt;
                _context._lastPanAbsoluteTarget = target.Item1;

                double panDistance = Math.Abs((target.Item1 - pose.Item1 + 540) % 360 - 180), tiltDistance = Math.Abs(tilt - pose.Item2);

                _centerMoveEpoch = epoch;
                _centerDeadlineTicks = DateTime.UtcNow.AddSeconds(Math.Min(120, Math.Max(5, 2 * Math.Max(panDistance, tiltDistance) / Math.Max(1, speed) + 3))).Ticks;

                ConsoleLogHelper.Command("VIDEO CENTER", "클릭 이동 요청 송신 / 실제 영상 중앙 도달은 별도 확인\n" + "DEVICE_ID=" + deviceId + " / DEVICE=" + profile?.DisplayName + " / AGENT=" + profile?.AgentType + " / CAMERA=" + (ir ? "IR" : "EO") + " / SOURCE=VIDEO_CLICK" + " / APPROXIMATE_IR=" + approximateIr + " / MODEL=" + (tuning.Calibration != null ? "SHARED_MEASURED_TABLE" : tuning.UseMeasuredSyncAnchors ? "SYNC_ANCHOR_OPTICAL" : "LINEAR_OPTICAL") + " / RAW_ZOOM=" + zoom + " / OPTICAL_RATIO=" + opticalRatio + " / DIGITAL_ESTIMATE=" + digitalFactor + " / EFFECTIVE_RATIO=" + effectiveRatio + " / SOURCE_XY=" + sourceX.ToString("F6", CultureInfo.InvariantCulture) + "," + sourceY.ToString("F6", CultureInfo.InvariantCulture) + " / SOURCE_SIZE=" + sourceWidth + "x" + sourceHeight + " / SOURCE_ASPECT=" + sourceAspect.ToString("F6", CultureInfo.InvariantCulture) + " / ACTIVE_RECT=" + string.Join(",", new[] { geometry.Left, geometry.Top, geometry.Width, geometry.Height }) + " / ACTIVE_ASPECT=" + aspect.ToString("F6", CultureInfo.InvariantCulture) + " / OPTICAL_CENTER=" + opticalCenter.X.ToString("F6", CultureInfo.InvariantCulture) + "," + opticalCenter.Y.ToString("F6", CultureInfo.InvariantCulture) + " / CORRECTED_XY=" + nx.ToString("F6", CultureInfo.InvariantCulture) + "," + ny.ToString("F6", CultureInfo.InvariantCulture) + " / GEOMETRY_BASIS=" + geometry.Basis + " / WIDE_HFOV=" + wideFov + " / HFOV_SOURCE=" + (geometry.WideHorizontalFov.HasValue ? "DEVICE_CLICK_CONFIG" : "CONTROL_TUNING") + " / ESTIMATED_HFOV=" + hfov + " / CURRENT_PT=" + pose.Item1 + "," + pose.Item2 + " / SPEED=" + speed + " / TARGET=" + target.Item1 + "," + tilt + " / TX_ONLY, arrival not confirmed");

                return true;
            }
            catch (Exception ex)
            {
                _context._controlCommandService.StopPanTiltPositionMove();

                ConsoleLogHelper.Warning("VIDEO CENTER", ex.GetType().Name);

                return false;
            }
            finally
            {
                _videoCenterBusy = false;
            }
        }

        #endregion

    }
}
