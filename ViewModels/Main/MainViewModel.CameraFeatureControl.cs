using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using OpenCvWpfTracking.Common;

// 2026-10-16: 카메라 디지털 줌 지원·상태와 영상 클릭 중심 이동.
// 코드 정비: 한국어 설명과 함수 간격·region을 정리하며 기존 운용 흐름은 유지한다.
namespace OpenCvWpfTracking.ViewModels.Main
{
    public partial class MainViewModel
    {

        #region [필드 및 상태 정보]

        // 2026-10-12: 공통 DIGITAL_ZOOM은 켜짐/꺼짐 명령이며 단계별 배율 제어가 아니다.
        private readonly byte[] _digitalCaps = new byte[2];
        // EO/IR 추정은 전송 성공한 줌 요청을 따르며 ON 상태만으로 4배를 가정하지 않는다.
        private readonly Services.Configuration.DigitalZoomEstimate _eoDigitalEstimate = new Services.Configuration.DigitalZoomEstimate();
        private readonly Services.Configuration.DigitalZoomEstimate _irDigitalEstimate = new Services.Configuration.DigitalZoomEstimate();
        private long _digitalEstimateTimestamp = System.Diagnostics.Stopwatch.GetTimestamp();

        #endregion

        #region [기능 처리 함수]

        /// <summary>
        /// 단조 시계의 경과 시간을 계산해 EO/IR 디지털 추정 모델을 함께 갱신한다.
        /// </summary>
        private void TickDigitalEstimate()
        {
            long now = System.Diagnostics.Stopwatch.GetTimestamp();
            double elapsed = (now - _digitalEstimateTimestamp) / (double)System.Diagnostics.Stopwatch.Frequency;
            _digitalEstimateTimestamp = now;
            AdvanceDigitalEstimate(false, elapsed);
            AdvanceDigitalEstimate(true, elapsed);
        }

        /// <summary>
         
        /// 명시적으로 활성화한 추정값만 연결·상태 신선도·광학 끝점 조건으로 갱신한다.
         
        /// </summary>
        private void AdvanceDigitalEstimate(bool ir, double elapsed)
        {
            var estimate = ir ? _irDigitalEstimate : _eoDigitalEstimate;
            var tuning = ControlTuningFor(ir);
            bool fresh = CameraFeatureTransportReady && IsLensFresh(ir) && IsOperationCommandEnabled && !IsControlInputLocked;
            bool atMaximum = (ir ? GetCurrentIrZoomStandardPosition() : GetCurrentPresetStandardZoom()) >= 1000;
            // 2026-10-13: 광학 위치 1000만으로 디지털 줌 ON 상태를 판단하지 않는다.
            // ON 요청 전송 성공 또는 유효한 장비 ON 응답이 있을 때만 추정 모델을 켠다.
            estimate.Advance(elapsed, fresh, atMaximum, tuning.DigitalEstimateMaximum, tuning.DigitalEstimateSeconds);
            if (!CameraFeatureTransportReady)
                estimate.Enable(false);
        }

        /// <summary>
         
        /// 이전 방향의 경과 시간을 먼저 반영한 후 추정 배율의 증감 방향을 변경한다.
         
        /// </summary>
        private void SetDigitalEstimateDirection(int direction, bool ir = false)
        {
            TickDigitalEstimate();
            (ir ? _irDigitalEstimate : _eoDigitalEstimate).Direction = direction;
        }

        #endregion

        #region [속성 및 상태 표시]

        private double EoDisplayZoomRatio => ControlTuningFor(false).Ratio(GetCurrentPresetStandardZoom()) * (GetCurrentPresetStandardZoom() >= 1000 && _eoDigitalEstimate.Enabled ? _eoDigitalEstimate.Factor : 1);
        private double IrDisplayZoomRatio => ControlTuningFor(true).Ratio(GetCurrentIrZoomStandardPosition()) * (GetCurrentIrZoomStandardPosition() >= 1000 && _irDigitalEstimate.Enabled ? _irDigitalEstimate.Factor : 1);

        #endregion

        #region [필드 및 상태 정보]

        private readonly bool[] _digitalCapsKnown = new bool[2];
        private readonly int[] _digitalState =
        {
            -1,
            -1
        };
        private readonly int[] _digitalRequestedState =
        {
            -1,
            -1
        };
        private readonly bool[] _digitalTxFailed = new bool[2];
        private readonly long[] _digitalStateTicks = new long[2];
        private readonly long[] _digitalPendingTicks = new long[2];
        private readonly long[] _digitalNextQueryTicks = new long[2];
        private readonly long[] _videoCenterFrameTicks = new long[2];
        private long _lastDigitalQueryTicks;
        private Tuple<double, double, long> _videoCenterPose;
        private bool _videoCenterBusy;
        private long _lastCenterClickTicks;
        private long _centerDeadlineTicks;
        private int _centerMoveEpoch;

        #endregion

        #region [속성 및 상태 표시]

        private bool CameraFeatureTransportReady => _isDeviceConnectionRequested && _laTcpService.IsConnected;
        public bool CanSetEoDigitalZoom => CanSetDigitalZoom(0);
        public bool CanSetIrDigitalZoom => CanSetDigitalZoom(1);
        public string EoDigitalZoomStatusText => DigitalStatus(0);
        public string IrDigitalZoomStatusText => DigitalStatus(1);

        #endregion

        #region [기능 처리 함수]

        /// <summary>
        /// 채널의 연결과 지원 플래그 및 요청 대기를 확인한다.
        /// </summary>
        private bool CanSetDigitalZoom(int i) => CameraFeatureTransportReady && IsOperationCommandEnabled && !IsControlInputLocked && _digitalCapsKnown[i] && (_digitalCaps[i] & 5) == 5 && _digitalPendingTicks[i] == 0;
        /// <summary>
        /// 요청 송신과 유효한 장비 응답을 구분해 D-ZOOM 적용 확인 문구를 만든다.
        /// </summary>
        private string DigitalStatus(int i)
        {
            if (!CameraFeatureTransportReady)
                return "D-ZOOM · 미연결";
            if (i == 1 && _digitalCapsKnown[i] && (_digitalCaps[i] & 5) != 5)
                return "D-ZOOM · IR 설정 미지원";
            if (i == 1 && !_digitalCapsKnown[i])
                return "D-ZOOM · IR 지원 조회 중";
            if (_digitalTxFailed[i])
                return "D-ZOOM · 요청 전송 실패";
            if (_digitalState[i] >= 0 && DateTime.UtcNow.Ticks - _digitalStateTicks[i] <= TimeSpan.FromSeconds(10).Ticks)
                return "장비 " + (_digitalState[i] == 0 ? "OFF" : "ON") + " 확인" + (_digitalRequestedState[i] >= 0 && _digitalRequestedState[i] != _digitalState[i] ? " · 요청 불일치" : "");
            if (_digitalRequestedState[i] >= 0)
                return (_digitalRequestedState[i] == 0 ? "OFF" : "ON") + " 요청 전송 · " + (_digitalPendingTicks[i] != 0 ? "응답 대기" : "적용 미확인");
            if (_digitalCapsKnown[i] && (_digitalCaps[i] & 2) == 0)
                return "D-ZOOM · 상태 조회 미지원";
            return "D-ZOOM · 장비 상태 미수신";
        }

        #endregion

        #region [필드 및 상태 정보]

        // 2026-10-13: 알림 순서를 바꾸지 않고 속성명 목록을 재사용한다.
        private static readonly string[] DigitalZoomPropertyNames =
        {
            nameof(CanSetEoDigitalZoom),
            nameof(CanSetIrDigitalZoom),
            nameof(EoDigitalZoomStatusText),
            nameof(IrDigitalZoomStatusText)
        };

        #endregion

        #region [기능 처리 함수]

        /// <summary>
        /// D-ZOOM 가능 여부 및 상태 표시의 바인딩을 갱신한다.
        /// </summary>
        private void NotifyDigitalZoom()
        {
            foreach (string n in DigitalZoomPropertyNames)
                OnPropertyChanged(n);
        }

        /// <summary>
         
        /// 연결 세션의 capability·응답·요청·추정 및 클릭 증거를 초기화한다.
         
        /// </summary>
        private void ResetDigitalZoomSession()
        {
            ResetSpecialFeatureSession();
            ResetPresetLensEvidence();
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

        /// <summary>
         
        /// EO/IR 기능 지원 조회를 연결 중 최대 초당 한 번 요청한다.
         
        /// </summary>
        public void QueryDigitalZoomSupport()
        {
            if (!CameraFeatureTransportReady || DateTime.UtcNow.Ticks - _lastDigitalQueryTicks < TimeSpan.FromSeconds(1).Ticks)
                return;
            _lastDigitalQueryTicks = DateTime.UtcNow.Ticks;
            for (byte i = 0; i < 2; i++)
                _controlCommandService.RequestCameraFeatureCapability(i);
            ConsoleLogHelper.Command("DIGITAL ZOOM", "EO/IR support query / E7");
        }

        /// <summary>
         
        /// 길이와 BOOLEAN 범위를 확인한 capability 응답으로 조회/설정 지원을 갱신한다.
         
        /// </summary>
        private void ApplyDigitalZoomCapability(byte[] p)
        {
            if (p.Length < 13 || p[0] != 2 || p[1] > 1 || p.Length < 13 + 12 * p[12])
                return;
            int i = p[1];
            byte flags = 0;
            for (int o = 13; o + 12 <= 13 + 12 * p[12]; o += 12)
                if (p[o] == 0x1D)
                {
                    // 켜짐/꺼짐 값 0~1만 허용하며 알 수 없는 새 값 형식은 제어를 활성화하지 않는다.
                    if (p[o + 2] == 0 && BitConverter.ToInt16(p, o + 4) == 0 && BitConverter.ToInt16(p, o + 6) == 1)
                        flags = p[o + 1];
                    break;
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
                _controlCommandService.RequestCameraFeatureState((byte)i, 0x1D);
            _digitalNextQueryTicks[i] = DateTime.UtcNow.AddSeconds(5).Ticks;
            NotifyDigitalZoom();
        }

        /// <summary>
         
        /// 유효한 장비 ON/OFF 응답을 적용하며 요청 확인과 추정 억제 상태를 갱신한다.
         
        /// </summary>
        private void ApplyDigitalZoomState(byte[] p)
        {
            if (p.Length < 18 || p[1] > 1 || p[2] != 0x1D)
                return;
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

        /// <summary>
         
        /// 운용 잠금 검증 후 BOOLEAN 명령을 송신한다. 송신 성공을 장비 적용 확인으로 취급하지 않는다.
         
        /// </summary>
        public bool SetDigitalZoom(bool ir, bool on)
        {
            int i = ir ? 1 : 0;
            // EO의 기존 설정 전용 흐름은 유지하며 IR은 아래 지원 응답도 필요하다.
            // 실제 전송 결과와 장비 적용 확인은 별도로 유지한다.
            if (IsControlInputLocked || !IsOperationCommandEnabled)
            {
                ConsoleLogHelper.Warning("DIGITAL ZOOM", "Rejected / TARGET=" + i + " / operation locked");
                return false;
            }

            // EO의 설정 전용 흐름을 유지하고 IR에는 명시적인 설정 지원을 요구한다.
            if (ir && !CanSetIrDigitalZoom)
            {
                ConsoleLogHelper.Warning("DIGITAL ZOOM", "Rejected / TARGET=1 / IR capability, connection or pending request");
                return false;
            }

            bool sent = _controlCommandService.SetCameraFeature(0x1D, (byte)i, (byte)(on ? 1 : 0));
            _digitalTxFailed[i] = !sent;
            _digitalRequestedState[i] = on ? 1 : 0;
            if (sent)
            {
                _digitalState[i] = -1;
                _digitalPendingTicks[i] = DateTime.UtcNow.Ticks;
                TickDigitalEstimate();
                (i == 0 ? _eoDigitalEstimate : _irDigitalEstimate).Enable(on);
                ConsoleLogHelper.State("D-ZOOM ESTIMATE", "CAMERA=" + (ir ? "IR" : "EO") + " / LOCAL REQUEST / ENABLED=" + on + " / MAX=" + ControlTuningFor(ir).DigitalEstimateMaximum + " / SECONDS=" + ControlTuningFor(ir).DigitalEstimateSeconds + " / ESTIMATED / NOT TELEMETRY");
            }

            ConsoleLogHelper.Command("DIGITAL ZOOM", "TARGET=" + i + " / REQUEST=" + (on ? "ON" : "OFF") + " / TX=" + sent + " / not a device ACK");
            NotifyDigitalZoom();
            return sent;
        }

        /// <summary>
         
        /// DIGITAL_ZOOM 거절 응답 시 요청·추정을 해제하고 IR 설정 버튼을 비활성화한다.
         
        /// </summary>
        private void ApplyDigitalZoomUnavailable(int target)
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

        /// <summary>
         
        /// 클릭 이동 기한, D-ZOOM 응답 대기 및 주기 조회를 기존 간격으로 감시한다.
         
        /// </summary>
        private void TickDigitalZoom()
        {
            if (_centerDeadlineTicks != 0)
            {
                if (_centerMoveEpoch != _inputEpoch || (!_activePanAbsoluteTarget.HasValue && !_activeTiltAbsoluteTarget.HasValue))
                    _centerDeadlineTicks = 0;
                else if (DateTime.UtcNow.Ticks >= _centerDeadlineTicks)
                {
                    StopAllPtzfMove();
                    ConsoleLogHelper.Warning("VIDEO CENTER", "Arrival not confirmed within deadline; stop requested");
                }
            }

            if (!CameraFeatureTransportReady)
            {
                if (_digitalCapsKnown[0] || _digitalCapsKnown[1])
                    ResetDigitalZoomSession();
                return;
            }

            for (byte i = 0; i < 2; i++)
                if (_digitalPendingTicks[i] != 0 && DateTime.UtcNow.Ticks - _digitalPendingTicks[i] > TimeSpan.FromSeconds(2).Ticks)
                {
                    _digitalPendingTicks[i] = 0;
                    if ((_digitalCaps[i] & 2) != 0)
                        _controlCommandService.RequestCameraFeatureState(i, 0x1D);
                    NotifyDigitalZoom();
                }

            for (byte i = 0; i < 2; i++)
                if ((_digitalCaps[i] & 3) == 3 && _digitalPendingTicks[i] == 0 && DateTime.UtcNow.Ticks >= _digitalNextQueryTicks[i])
                {
                    _digitalNextQueryTicks[i] = DateTime.UtcNow.AddSeconds(5).Ticks;
                    _controlCommandService.RequestCameraFeatureState(i, 0x1D);
                }

            string signature = EoDigitalZoomStatusText + IrDigitalZoomStatusText + CanSetEoDigitalZoom + CanSetIrDigitalZoom;
            if (signature != _lastDigitalUiSignature)
            {
                _lastDigitalUiSignature = signature;
                NotifyDigitalZoom();
            }
        }

        #endregion

        #region [필드 및 상태 정보]

        private string _lastDigitalUiSignature;

        #endregion

        #region [기능 처리 함수]

        // 2026-10-12: 시간 기반 이동이 아닌 핀홀 광선 투영을 사용한다. 임시 화각은 현장 보정이 필요하다.
        /// <summary>
        /// 정규화된 영상 클릭 좌표를 핀홀 광선 모델로 PT 절대 목표 각도에 환산한다.
        /// </summary>
        public static Tuple<double, double> VideoCenterTarget(double pan, double tilt, double nx, double ny, double hfov, double aspect)
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
                return null;
            double r = Math.PI / 180, x = nx * Math.Tan(hfov * r / 2), y = -ny * Math.Tan(hfov * r / 2) / aspect;
            double forward = Math.Cos(tilt * r) - y * Math.Sin(tilt * r), up = Math.Sin(tilt * r) + y * Math.Cos(tilt * r);
            double dp = Math.Atan2(x, forward) / r, dt = Math.Atan2(up, Math.Sqrt(x * x + forward * forward)) / r;
            double targetPan = (pan + dp + 540) % 360 - 180;
            return Tuple.Create(targetPan, dt);
        }

        /// <summary>
         
        /// 영상·렌즈·자세의 신선도와 제어 잠금을 검증한 뒤 줌 보정 PT 절대 이동을 요청한다.
         
        /// </summary>
        public async Task<bool> CenterVideoPointAsync(bool ir, double nx, double ny, double aspect)
        {
            var pose = Volatile.Read(ref _videoCenterPose);
            long now = DateTime.UtcNow.Ticks;
            string status = ir ? IrStatusText : EoStatusText;
            if (_videoCenterBusy || _activePanAbsoluteTarget.HasValue || _activeTiltAbsoluteTarget.HasValue || !CameraFeatureTransportReady || IsControlInputLocked || !IsOperationCommandEnabled || _controlInputs.Count > 0 || pose == null || now - pose.Item3 > TimeSpan.FromSeconds(2).Ticks || !IsLensFresh(ir) || now - Interlocked.Read(ref _videoCenterFrameTicks[ir ? 1 : 0]) > TimeSpan.FromSeconds(1).Ticks || status == null || !status.EndsWith("] Connected", StringComparison.Ordinal) || now - _lastCenterClickTicks < TimeSpan.FromMilliseconds(300).Ticks)
            {
                ConsoleLogHelper.Warning("VIDEO CENTER", "Rejected / stale video, pose/lens, lock, connection or active input / CAMERA=" + (ir ? "IR" : "EO"));
                return false;
            }

            var tuning = ControlTuningFor(ir);
            int zoom = ir ? GetCurrentIrZoomStandardPosition() : GetCurrentPresetStandardZoom();
            TickDigitalEstimate();
            double opticalRatio = tuning.Ratio(zoom);
            var estimate = ir ? _irDigitalEstimate : _eoDigitalEstimate;
            double digitalFactor = zoom >= 1000 && estimate.Enabled ? estimate.Factor : 1;
            double effectiveRatio = opticalRatio * digitalFactor;
            // 확인된 광각 기준을 유지하고 각도나 PT 이동 시간이 아닌
            // 탄젠트 공간에서 비율을 조정한다. 디지털 배율은 추정값임을 유지한다.
            double hfov = 2 * Math.Atan(Math.Tan(tuning.WideHorizontalFov * Math.PI / 360) / effectiveRatio) * 180 / Math.PI;
            var opticalCenter = GetOpticalCenter(ir);
            nx -= 2 * (opticalCenter.X - .5);
            ny -= 2 * (opticalCenter.Y - .5);
            // 보정 중심이 영상 중앙에서 벗어나 가장자리 클릭 좌표가
            // 기존 [-1,1] 범위를 넘어가더라도 같은 투영 광선을 유지한다.
            double rayScale = Math.Max(1, Math.Max(Math.Abs(nx), Math.Abs(ny)));
            double projectionFov = 2 * Math.Atan(Math.Tan(hfov * Math.PI / 360) * rayScale) * 180 / Math.PI;
            var target = VideoCenterTarget(pose.Item1, pose.Item2, nx / rayScale, ny / rayScale, projectionFov, aspect);
            if (target == null)
                return false;
            double tilt = Clamp(target.Item2, CurrentMoveControlTiltMinimum, CurrentMoveControlTiltMaximum);
            if (Math.Abs(nx) < .005 && Math.Abs(ny) < .005)
                return true;
            _videoCenterBusy = true;
            _lastCenterClickTicks = now;
            int epoch = _inputEpoch;
            try
            {
                SetActivePanTiltZoomSource(ir);
                double speed = tuning.Speed(PanTiltSpeedLevel, zoom, IsFovSpeedEnabled);
                if (!_controlCommandService.SetPanPositionSpeed(speed) || !_controlCommandService.SetTiltPositionSpeed(speed) || !_controlCommandService.SetPanShortestPathMode())
                    return false;
                await Task.Delay(IsEnvironmentStatusSelected ? 100 : 80);
                if (epoch != _inputEpoch || !CameraFeatureTransportReady || IsControlInputLocked || !IsOperationCommandEnabled || _controlInputs.Count > 0)
                    return false;
                bool panSent = _controlCommandService.PanGoPosition(target.Item1);
                await Task.Delay(IsEnvironmentStatusSelected ? 60 : 0);
                if (epoch != _inputEpoch || !CameraFeatureTransportReady || IsControlInputLocked || !IsOperationCommandEnabled || _controlInputs.Count > 0)
                {
                    _controlCommandService.StopPanTiltPositionMove();
                    return false;
                }

                bool tiltSent = panSent && _controlCommandService.TiltGoPosition(tilt);
                if (!tiltSent)
                {
                    _controlCommandService.StopPanTiltPositionMove();
                    return false;
                }

                _activePanAbsoluteTarget = target.Item1;
                _activeTiltAbsoluteTarget = tilt;
                _lastPanAbsoluteTarget = target.Item1;
                double panDistance = Math.Abs((target.Item1 - pose.Item1 + 540) % 360 - 180), tiltDistance = Math.Abs(tilt - pose.Item2);
                _centerMoveEpoch = epoch;
                _centerDeadlineTicks = DateTime.UtcNow.AddSeconds(Math.Min(120, Math.Max(5, 2 * Math.Max(panDistance, tiltDistance) / Math.Max(1, speed) + 3))).Ticks;
                ConsoleLogHelper.Command("VIDEO CENTER", "SOURCE=VIDEO_CLICK / CAMERA=" + (ir ? "IR" : "EO") + " / MODEL=" + (tuning.UseMeasuredSyncAnchors ? "SYNC_ANCHOR_OPTICAL" : "LINEAR_OPTICAL") + " / RAW_ZOOM=" + zoom + " / OPTICAL_RATIO=" + opticalRatio + " / DIGITAL_ESTIMATE=" + digitalFactor + " / EFFECTIVE_RATIO=" + effectiveRatio + " / XY=" + nx + "," + ny + " / ESTIMATED_HFOV=" + hfov + " / TARGET=" + target.Item1 + "," + tilt + " / TX_ONLY, arrival not confirmed");
                return true;
            }
            catch (Exception ex)
            {
                _controlCommandService.StopPanTiltPositionMove();
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

    internal static class CameraNumberValidation
    {

        #region [기능 처리 함수]

        /// <summary>
        /// 좌표 계산에 사용할 값에 유효하지 않은 수가 포함되었는지 확인한다.
        /// </summary>
        internal static bool AnyInvalid(this double[] numbers)
        {
            foreach (double x in numbers)
                if (double.IsNaN(x) || double.IsInfinity(x))
                    return true;
            return false;
        }

        #endregion

    }
}
