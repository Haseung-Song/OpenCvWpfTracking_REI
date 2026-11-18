using OpenCvWpfTracking.Common;
using OpenCvWpfTracking.Services.Communication;
using System;
using System.Text;
using System.Threading;
using static OpenCvWpfTracking.ViewModels.Main.MainViewModel;

namespace OpenCvWpfTracking.ViewModels.Main
{
    /// <summary>수신 패킷 분배에 필요한 협력 기능만 받는 경계입니다. 화면 객체의 내부 구현을 알지 않습니다.</summary>
    internal interface IWebAgentProtocolFeatureContext
    {
        ref double? _activePanAbsoluteTarget { get; }

        KeyboardPanTiltDirection _activePanTiltMoveDirection { get; set; }

        ref double? _activeTiltAbsoluteTarget { get; }

        string _connectionSessionId { get; set; }

        IControlCommandServicePort _controlCommandService { get; }

        short _currentEoFocus { get; set; }

        short _currentEoZoom { get; set; }

        ushort _currentIrFocus { get; set; }

        ushort _currentIrZoom { get; set; }

        double _currentPan { get; set; }

        double _currentTilt { get; set; }

        ref long _eoLensStatusVersion { get; }

        string _irFocusDirectUnavailableSession { get; set; }

        ref long _irLensStatusVersion { get; }

        ref double? _lastPanAbsoluteTarget { get; }

        ref long _panStartTxCount { get; }

        ref long _panTiltStatusVersion { get; }

        Services.Position.PositionSnapshotService _positionSnapshotService { get; }

        string _ptzPerformanceScenario { get; set; }

        ref long _tiltStartTxCount { get; }

        ref global::System.Tuple<double, double, long> _videoCenterPose { get; }

        void ApplyDevicePowerStatus(byte mcbStatus, byte scbStatus, DateTime? sourceTime = null, string sourceFunction = "0x31");

        void ApplyDigitalZoomCapability(byte[] p);

        void ApplyDigitalZoomState(byte[] p);

        void ApplyDigitalZoomUnavailable(int target);

        void ApplySpecialFeatureCapability(byte[] p);

        void ApplySpecialFeatureState(byte[] p);

        void ApplySpecialFeatureUnavailable(byte[] p);

        void BeginWebAgentBuildCheck();

        bool CameraFeatureTransportReady { get; }

        void DispatchSpecialFeaturePacket(byte[] p, Action<byte[]> apply);

        double GetTiltDifference(double currentTilt, double targetTilt);

        void HandleGpsUnavailable(byte[] payload);

        void HandlePrecisionUnavailable(byte[] payload);

        void HandleWebAgentEquipmentUnavailable(byte[] payload);

        bool IsEnvironmentStatusSelected { get; }

        void MarkLensEvidence(bool ir);

        void MarkPresetLensEvidence(bool ir, bool zoom, bool focus);

        void NotifyEoCurrentStatusChanged(bool panChanged = true, bool tiltChanged = true, bool zoomChanged = true, bool focusChanged = true, bool powerChanged = true);

        void NotifyIrCurrentStatusChanged();

        void ParseAutoFocusResponse(byte[] payload);

        void ParseEquipmentTemperature(byte[] payload);

        void ParseGpsAutoTxRateResponse(byte[] payload);

        void ParseStoredRpyOffsetResponse(byte[] payload);

        void ParseStoredRpyRateResponse(byte[] payload);

        void ParseWebAgentGps(byte[] payload);

        void ParseWebAgentRpyOffsetAck(byte[] payload);

        void ParseWebAgentRpyRateAck(byte[] payload);

        void ParseWebAgentSystemDateTime(byte[] payload);

        void QueryEquipmentTemperature();

        void QueuePositionStatusNotification(bool includeLastSnapshot = false);

        void RecordRpyTelemetry(double roll, double pitch, double yaw);

        void RequestGpsAutoTxRateAfterConnection();

        void ResetDigitalZoomSession();

        void ResetWebAgentControlSession();

        void ResetWebAgentEquipmentState();

        void UpdateGpsImuRawReference(double yaw);

        void UpdatePositionEoLens(int eoZoom, int eoFocus);

        void UpdatePositionImu(double roll, double pitch, double yaw, bool valid);

        void UpdatePositionIrLens(int irZoom, int irFocus);

        void UpdateWebAgentBuild(string buildText);

        Action<byte[]> ApplySpecialFeatureCapabilityCallback { get; }

        Action<byte[]> ApplySpecialFeatureStateCallback { get; }

        Action<byte[]> ApplySpecialFeatureUnavailableCallback { get; }
    }

    /// <summary>수신 패킷 분배 담당 객체입니다. 상태를 보관하고 입력을 받아 결과를 알립니다.</summary>
    internal sealed class WebAgentProtocolFeature
    {

        #region [협력 객체와 초기화]

        private readonly IWebAgentProtocolFeatureContext _context;

        internal WebAgentProtocolFeature(IWebAgentProtocolFeatureContext context)
        {
            _context = context ?? throw new System.ArgumentNullException(nameof(context));
        }

        internal void InitializeState()
        {
            _lastWebAgentPanRaw = -1;

            _lastWebAgentTiltRaw = -1;

            _webAgentTelemetryClockGeneration = -1;
        }

        #endregion

        #region [상태 필드]

        internal long _lastLegacyEoLensStatusUtcTicks;

        internal long _lastEoLensConflictWarningUtcTicks;

        internal long _lastPanTiltMoveCommandUtcTicks;

        internal long _lastPanTiltValueChangeUtcTicks;

        internal long _lastPanTiltStallWarningUtcTicks;

        internal int _lastWebAgentPanRaw;

        internal int _lastWebAgentTiltRaw;

        internal int _webAgentTelemetryNegotiationStarted;

        internal int _webAgentSeparatedTelemetryAckMask;

        internal readonly long[] _webAgentLensSampleMilliseconds = new long[2];

        internal long _webAgentPanSampleMilliseconds;

        internal long _webAgentTiltSampleMilliseconds;

        internal long _webAgentPowerSampleMilliseconds;

        internal long _webAgentSplitLogTicks;

        internal readonly TelemetryClock _webAgentTelemetryClock = new TelemetryClock();

        internal int _webAgentTelemetryClockGeneration;

        internal double _webAgentTelemetryRejectedLogAt;

        #endregion

        #region [동작 처리]

        internal void LogDeferredWebAgentTelemetry(string channel, ulong sample)
        {
            double now = TelemetryClock.MonotonicMilliseconds;

            if (now - _webAgentTelemetryRejectedLogAt < 5000)
            {
                return;
            }

            _webAgentTelemetryRejectedLogAt = now;

            ConsoleLogHelper.State("WEB AGENT TELEMETRY DEFERRED", "CHANNEL=" + channel + " / SOURCE_TIMESTAMP=" + sample + " / Clock reference pending, invalid time, or stale sample; packet is not used for control");
        }

        internal void ResetWebAgentTelemetrySession()
        {
            _context.ResetWebAgentControlSession();

            _context.ResetWebAgentEquipmentState();

            _webAgentTelemetryClock.Reset();

            _webAgentTelemetryClockGeneration = -1;

            Interlocked.Exchange(ref _webAgentTelemetryNegotiationStarted, 0);

            Interlocked.Exchange(ref _webAgentSeparatedTelemetryAckMask, 0);

            Interlocked.Exchange(ref _lastLegacyEoLensStatusUtcTicks, 0);

            Interlocked.Exchange(ref _webAgentPanSampleMilliseconds, 0);

            Interlocked.Exchange(ref _webAgentTiltSampleMilliseconds, 0);

            Interlocked.Exchange(ref _webAgentPowerSampleMilliseconds, 0);

            for (int index = 0; index < _webAgentLensSampleMilliseconds.Length; index++)
            {
                Interlocked.Exchange(ref _webAgentLensSampleMilliseconds[index], 0);
            }

            _context._irFocusDirectUnavailableSession = null;
        }

        internal bool TryReadTelemetryTime(ulong milliseconds, out DateTimeOffset value, bool requireFresh = true)
        {
            int generation;

            bool valid = _webAgentTelemetryClock.TryGetLocalSampleTime(milliseconds, TelemetryClock.MonotonicMilliseconds, DateTimeOffset.UtcNow, out value, out generation, requireFresh);

            if (generation != _webAgentTelemetryClockGeneration)
            {
                _webAgentTelemetryClockGeneration = generation;

                Interlocked.Exchange(ref _webAgentPanSampleMilliseconds, 0);

                Interlocked.Exchange(ref _webAgentTiltSampleMilliseconds, 0);

                Interlocked.Exchange(ref _webAgentPowerSampleMilliseconds, 0);

                for (int index = 0; index < _webAgentLensSampleMilliseconds.Length; index++)
                {
                    Interlocked.Exchange(ref _webAgentLensSampleMilliseconds[index], 0);
                }
            }

            return valid;
        }

        internal void InitializeWebAgentSession()
        {
            if (!_context.IsEnvironmentStatusSelected)
            {
                return;
            }

            _context.ResetDigitalZoomSession();

            _context.BeginWebAgentBuildCheck();

            _context._controlCommandService.RequestWebAgentIdentity();

            _context.QueryEquipmentTemperature();

            _context._controlCommandService.RequestLensCapabilityAndTelemetry(0x00);

            _context._controlCommandService.RequestLensCapabilityAndTelemetry(0x01);

            _context._controlCommandService.RequestCameraFeatureCapability(0x01);

            _context._controlCommandService.RequestCameraFeatureCapability(0x00);

            _context._controlCommandService.RequestImuTelemetry();

            _context.RequestGpsAutoTxRateAfterConnection();

            for (byte category = 0; category <= 3; category++)
            {
                _context._controlCommandService.RequestWebAgentTelemetrySetting(category);
            }

            ConsoleLogHelper.State("WEB AGENT V2.9", "Identity, EO/IR lens, feature, IMU, GPS rate and separated telemetry capability queries sent");
        }

        internal void ParseWebAgentTelemetryPacket(LaResponsePacket packet)
        {
            byte[] payload = packet.Payload;

            try
            {
                switch (packet.Function)
                {
                    case 0x23:
                        _context.ParseWebAgentGps(payload);
                        break;
                    case 0x24:
                        ParseWebAgentImu(payload);
                        break;
                    case 0x25:
                        ParseWebAgentCapability(payload);
                        break;
                    case 0x26:
                        ParseWebAgentIdentity(payload);
                        break;
                    case 0x27:
                        ParseWebAgentFeatureState(payload);
                        break;
                    case 0x28:
                        ParseWebAgentLensTelemetry(payload);
                        break;
                    case 0x29:
                        ParseWebAgentHoming(payload);
                        break;
                    case 0x2A:
                        ParseWebAgentUnavailable(payload);
                        break;
                    case 0x2B:
                        _context.ParseWebAgentRpyRateAck(payload);
                        break;
                    case 0x2C:
                        _context.ParseWebAgentRpyOffsetAck(payload);
                        break;
                    case 0x2D:
                        _context.ParseWebAgentSystemDateTime(payload);
                        break;
                    case 0x2E:
                        _context.ParseStoredRpyOffsetResponse(payload);
                        break;
                    case 0x2F:
                        _context.ParseStoredRpyRateResponse(payload);
                        break;
                    case 0x30:
                        _context.ParseGpsAutoTxRateResponse(payload);
                        break;
                    case 0x32:
                        ParseWebAgentPanTiltTelemetry(payload);
                        break;
                    case 0x33:
                        ParseWebAgentPowerTelemetry(payload);
                        break;
                    case 0x34:
                        ParseWebAgentTelemetrySetting(payload);
                        break;
                    case 0x35:
                        _context.ParseAutoFocusResponse(payload);
                        break;
                    case 0x36:
                        _context.ParseEquipmentTemperature(payload);
                        break;
                }
            }
            catch (Exception ex)
            {
                ConsoleLogHelper.Warning("WEB AGENT V1.8", "Response parse failed / " + ex.Message);
            }
        }

        internal void ParseWebAgentPanTiltTelemetry(byte[] p)
        {
            if (p == null || p.Length != 22 || p[0] != 0x01)
            {
                ConsoleLogHelper.Warning("WEB AGENT TELEMETRY", "FUNCTION=0x32 / INVALID_PAYLOAD");

                return;
            }

            byte validFlags = p[1];

            bool panValid = (validFlags & 0x01) != 0;

            bool tiltValid = (validFlags & 0x02) != 0;

            ushort panRaw = BitConverter.ToUInt16(p, 2);

            ushort tiltRaw = BitConverter.ToUInt16(p, 4);

            ulong panUpdatedAt = BitConverter.ToUInt64(p, 6);

            ulong tiltUpdatedAt = BitConverter.ToUInt64(p, 14);

            DateTimeOffset panTime;

            DateTimeOffset tiltTime;

            panValid &= TryReadTelemetryTime(panUpdatedAt, out panTime);

            tiltValid &= TryReadTelemetryTime(tiltUpdatedAt, out tiltTime);

            panValid &= DateTimeOffset.UtcNow - panTime <= TimeSpan.FromSeconds(3);

            tiltValid &= DateTimeOffset.UtcNow - tiltTime <= TimeSpan.FromSeconds(3);

            if (!panValid && !tiltValid)
            {
                LogDeferredWebAgentTelemetry("PT", panUpdatedAt);
            }

            if ((panValid && panRaw > 35999) || (tiltValid && tiltRaw > 35999))
            {
                ConsoleLogHelper.Warning("WEB AGENT TELEMETRY", $"FUNCTION=0x32 / OUT_OF_RANGE / PAN={panRaw} / TILT={tiltRaw}");

                return;
            }

            double previousPan = _context._currentPan;

            double previousTilt = _context._currentTilt;

            bool panNew = panValid && (long)panUpdatedAt > Interlocked.Read(ref _webAgentPanSampleMilliseconds);

            bool tiltNew = tiltValid && (long)tiltUpdatedAt > Interlocked.Read(ref _webAgentTiltSampleMilliseconds);

            if (panNew)
            {
                Interlocked.Exchange(ref _webAgentPanSampleMilliseconds, (long)panUpdatedAt);

                _context._currentPan = ConvertWebAgentPanToGuiAngle(panRaw / 100.0);

                if (Math.Abs(Math.Abs(_context._currentPan) - 180.0) <= 0.05 && _context._lastPanAbsoluteTarget.HasValue && Math.Abs(Math.Abs(_context._lastPanAbsoluteTarget.Value) - 180.0) <= 0.05)
                {
                    _context._currentPan = _context._lastPanAbsoluteTarget.Value < 0 ? -180.0 : 180.0;
                }
            }

            if (tiltNew)
            {
                Interlocked.Exchange(ref _webAgentTiltSampleMilliseconds, (long)tiltUpdatedAt);

                _context._currentTilt = NormalizeUnsignedWebAgentTilt(tiltRaw / 100.0, _context._currentTilt);
            }

            if (!panNew && !tiltNew)
            {
                return;
            }

            if (panNew && _context._activePanAbsoluteTarget.HasValue && Math.Abs(_context._currentPan - _context._activePanAbsoluteTarget.Value) <= 0.03)
            {
                _context._activePanAbsoluteTarget = null;
            }

            if (tiltNew && _context._activeTiltAbsoluteTarget.HasValue && Math.Abs(_context.GetTiltDifference(_context._currentTilt, _context._activeTiltAbsoluteTarget.Value)) <= 0.03)
            {
                _context._activeTiltAbsoluteTarget = null;
            }

            if (panValid && tiltValid)
            {
                DateTimeOffset poseTime = panTime < tiltTime ? panTime : tiltTime;

                Interlocked.Exchange(ref _context._videoCenterPose, Tuple.Create(_context._currentPan, _context._currentTilt, poseTime.UtcDateTime.Ticks));

                Interlocked.Increment(ref _context._panTiltStatusVersion);

                _context._positionSnapshotService.UpdatePtz(_context._currentPan, _context._currentTilt, poseTime);

                _context.QueuePositionStatusNotification();
            }

            _context.NotifyEoCurrentStatusChanged(panValid && Math.Abs(previousPan - _context._currentPan) > 0.0001, tiltValid && Math.Abs(previousTilt - _context._currentTilt) > 0.0001, false, false, false);

            long nowTicks = DateTime.UtcNow.Ticks;

            if (nowTicks - Interlocked.Read(ref _webAgentSplitLogTicks) >= TimeSpan.TicksPerSecond)
            {
                Interlocked.Exchange(ref _webAgentSplitLogTicks, nowTicks);

                ConsoleLogHelper.State("WEB AGENT TELEMETRY", $"FUNCTION=0x32 / VALID=0x{validFlags:X2} / PAN={_context._currentPan:F2} / TILT={_context._currentTilt:F2} / PAN_AT={panUpdatedAt} / TILT_AT={tiltUpdatedAt}");
            }
        }

        internal void ParseWebAgentPowerTelemetry(byte[] p)
        {
            if (p == null || p.Length != 12 || p[0] != 0x01)
            {
                ConsoleLogHelper.Warning("WEB AGENT TELEMETRY", "FUNCTION=0x33 / INVALID_PAYLOAD");

                return;
            }

            byte validFlags = p[1];

            byte mcbStatus = p[2];

            byte scbStatus = p[3];

            ulong updatedAt = BitConverter.ToUInt64(p, 4);

            DateTimeOffset powerTime;

            if ((validFlags & 0x01) != 0 && TryReadTelemetryTime(updatedAt, out powerTime, false) && (long)updatedAt > Interlocked.Read(ref _webAgentPowerSampleMilliseconds))
            {
                Interlocked.Exchange(ref _webAgentPowerSampleMilliseconds, (long)updatedAt);

                _context.ApplyDevicePowerStatus(mcbStatus, scbStatus, powerTime.LocalDateTime, "0x33");
            }

            ConsoleLogHelper.State("WEB AGENT TELEMETRY", $"FUNCTION=0x33 / VALID=0x{validFlags:X2} / MCB=0x{mcbStatus:X2} / SCB=0x{scbStatus:X2} / UPDATED_AT={updatedAt}");
        }

        internal void ParseWebAgentTelemetrySetting(byte[] p)
        {
            if (p == null || p.Length != 6 || p[0] != 0x01 || p[1] > 3)
            {
                ConsoleLogHelper.Warning("WEB AGENT TELEMETRY", "FUNCTION=0x34 / INVALID_PAYLOAD");

                return;
            }

            byte category = p[1];

            byte mode = p[2];

            byte frequencyHz = p[3];

            byte maximumHz = p[4];

            byte flags = p[5];

            if (mode > 1 || frequencyHz > 30 || maximumHz > 30 || (category == 3 && (frequencyHz != 0 || maximumHz != 0)))
            {
                ConsoleLogHelper.Warning("WEB AGENT TELEMETRY", "FUNCTION=0x34 / INVALID_SETTING");

                return;
            }

            ConsoleLogHelper.State("WEB AGENT TELEMETRY", $"FUNCTION=0x34 / CATEGORY={category} / MODE={mode} / HZ={frequencyHz} / MAX={maximumHz} / FLAGS=0x{flags:X2}");

            if (_context.IsEnvironmentStatusSelected && category == 0 && maximumHz >= 10 && (flags & 0x01) != 0 && Interlocked.CompareExchange(ref _webAgentTelemetryNegotiationStarted, 1, 0) == 0)
            {
                bool ptSent = _context._controlCommandService.SetWebAgentTelemetryRate(0, 10);

                bool eoSent = _context._controlCommandService.SetWebAgentTelemetryRate(1, 2);

                bool irSent = _context._controlCommandService.SetWebAgentTelemetryRate(2, 2);

                ConsoleLogHelper.Command("WEB AGENT TELEMETRY", $"SEPARATED_MODE_REQUEST / PT_10HZ={ptSent} / EO_2HZ={eoSent} / IR_2HZ={irSent}");
            }

            if (mode == 0x01 && category <= 2)
            {
                int mask = 1 << category;

                int currentMask;

                int acknowledged;

                do
                {
                    currentMask = Volatile.Read(ref _webAgentSeparatedTelemetryAckMask);

                    acknowledged = currentMask | mask;
                }
                while (Interlocked.CompareExchange(ref _webAgentSeparatedTelemetryAckMask, acknowledged, currentMask) != currentMask);

                if ((acknowledged & 0x07) == 0x07 && (currentMask & 0x07) != 0x07)
                {
                    _context._controlCommandService.RequestWebAgentTelemetrySnapshot(3);
                }
            }
        }

        internal void ParseWebAgentImu(byte[] p)
        {
            if (p.Length < 12)
            {
                return;
            }

            double roll = BitConverter.ToInt32(p, 0) / 10000.0;

            double pitch = BitConverter.ToInt32(p, 4) / 10000.0;

            double yaw = BitConverter.ToInt32(p, 8) / 10000.0;

            bool valid = Math.Abs(roll) <= 360.0 && Math.Abs(pitch) <= 360.0 && Math.Abs(yaw) <= 360.0;

            _context.UpdatePositionImu(roll, pitch, yaw, valid);

            _context.RecordRpyTelemetry(roll, pitch, yaw);

            _context.UpdateGpsImuRawReference(yaw);
        }

        internal void ParseWebAgentCapability(byte[] p)
        {
            if (p.Length < 13)
            {
                return;
            }

            _context.DispatchSpecialFeaturePacket(p, _context.ApplySpecialFeatureCapabilityCallback);

            var digitalPayload = (byte[])p.Clone();

            System.Windows.Application.Current?.Dispatcher.BeginInvoke(new Action(() =>
            {
                if (_context.CameraFeatureTransportReady)
                {
                    _context.ApplyDigitalZoomCapability(digitalPayload);
                }
            }));

            ConsoleLogHelper.State("WEB AGENT CAPABILITY", string.Format("SCHEMA={0} / TARGET=0x{1:X2} / LENS_FLAGS=0x{2:X4} / FEATURES={3}", p[0], p[1], BitConverter.ToUInt16(p, 2), p[12]));
        }

        internal void ParseWebAgentIdentity(byte[] p)
        {
            if (p.Length < 2)
            {
                return;
            }

            int versionLength = p[0];

            if (versionLength > 31 || p.Length < versionLength + 2)
            {
                return;
            }

            int buildLength = p[versionLength + 1];

            if (p.Length < versionLength + buildLength + 2)
            {
                return;
            }

            string version = Encoding.ASCII.GetString(p, 1, versionLength);

            string build = Encoding.ASCII.GetString(p, versionLength + 2, buildLength);

            _context.UpdateWebAgentBuild(build);

            ConsoleLogHelper.State("WEB AGENT IDENTITY", "VERSION=" + version + " / BUILD=" + build);
        }

        internal void ParseWebAgentLensTelemetry(byte[] p)
        {
            if (p.Length != 16 || p[0] != 1 || p[1] > 1)
            {
                return;
            }

            byte target = p[1];

            byte flags = p[2];

            ushort zoom = BitConverter.ToUInt16(p, 4);

            ushort focus = BitConverter.ToUInt16(p, 6);

            bool zoomValid = (flags & 0x01) != 0 && zoom <= 1000;

            bool focusValid = (flags & 0x02) != 0 && focus <= 1000;

            DateTimeOffset sampleTime;

            ulong sampleMilliseconds = BitConverter.ToUInt64(p, 8);

            if (!TryReadTelemetryTime(sampleMilliseconds, out sampleTime) || DateTimeOffset.UtcNow - sampleTime > TimeSpan.FromSeconds(3))
            {
                LogDeferredWebAgentTelemetry(target == 0 ? "EO" : "IR", sampleMilliseconds);

                return;
            }

            if ((long)sampleMilliseconds <= Interlocked.Read(ref _webAgentLensSampleMilliseconds[target]))
            {
                return;
            }

            Interlocked.Exchange(ref _webAgentLensSampleMilliseconds[target], (long)sampleMilliseconds);

            if (target == 0x00)
            {
                Interlocked.Increment(ref _context._eoLensStatusVersion);

                // 2026-09-15: Web Agent가 기존 Function 0x01에서는 EO Zoom/Focus를
                // 정상값으로 보내면서 Function 0x28에서는 valid=1인 0을 반복하는
                // 실장비 현상을 확인했다. 최근 0x01 상태가 있으면 해당 값을
                // 권위값으로 유지하여 CURRENT STATUS가 0과 정상값 사이에서
                // 깜빡이지 않도록 한다. 0x01이 끊긴 경우에만 0x28을 fallback한다.
                if (!HasRecentLegacyEoLensStatus())
                {
                    if (zoomValid)
                    {
                        _context._currentEoZoom = (short)zoom;

                        _context.MarkLensEvidence(false);
                    }

                    if (focusValid)
                    {
                        _context._currentEoFocus = (short)focus;
                    }

                    _context.MarkPresetLensEvidence(false, zoomValid, focusValid);

                    if (zoomValid && focusValid)
                    {
                        _context.UpdatePositionEoLens(_context._currentEoZoom, _context._currentEoFocus);
                    }

                    _context.NotifyEoCurrentStatusChanged();
                }
                else if ((zoomValid && zoom != (ushort)_context._currentEoZoom) || (focusValid && focus != (ushort)_context._currentEoFocus))
                {
                    LogWebAgentEoLensConflict(zoom, focus, flags);
                }
            }
            else if (target == 0x01)
            {
                if (zoomValid)
                {
                    _context._currentIrZoom = zoom;

                    _context.MarkLensEvidence(true);
                }

                if (focusValid)
                {
                    _context._currentIrFocus = focus;
                }

                _context.MarkPresetLensEvidence(true, zoomValid, focusValid);

                if (zoomValid && focusValid)
                {
                    _context.UpdatePositionIrLens(_context._currentIrZoom, _context._currentIrFocus);
                }

                Interlocked.Increment(ref _context._irLensStatusVersion);

                _context.NotifyIrCurrentStatusChanged();
            }

            ConsoleLogHelper.State("WEB AGENT LENS", string.Format("TARGET=0x{0:X2} / VALID=0x{1:X2} / ZOOM={2} / FOCUS={3}", target, flags, zoom, focus));
        }

        internal void MarkLegacyWebAgentStatusReceived(ushort panRaw, ushort tiltRaw)
        {
            if (!_context.IsEnvironmentStatusSelected)
            {
                return;
            }

            long nowTicks = DateTime.UtcNow.Ticks;

            Interlocked.Exchange(ref _lastLegacyEoLensStatusUtcTicks, nowTicks);

            bool valueChanged = _lastWebAgentPanRaw != panRaw || _lastWebAgentTiltRaw != tiltRaw;

            _lastWebAgentPanRaw = panRaw;
            _lastWebAgentTiltRaw = tiltRaw;

            if (valueChanged || Interlocked.Read(ref _lastPanTiltValueChangeUtcTicks) == 0)
            {
                Interlocked.Exchange(ref _lastPanTiltValueChangeUtcTicks, nowTicks);

                return;
            }

            long lastCommandTicks = Interlocked.Read(ref _lastPanTiltMoveCommandUtcTicks);

            long lastChangeTicks = Interlocked.Read(ref _lastPanTiltValueChangeUtcTicks);

            if (lastCommandTicks == 0 || _context._activePanTiltMoveDirection == KeyboardPanTiltDirection.None || nowTicks - lastCommandTicks < TimeSpan.TicksPerSecond || nowTicks - lastCommandTicks > TimeSpan.TicksPerSecond * 3 || nowTicks - lastChangeTicks < TimeSpan.TicksPerSecond)
            {
                return;
            }

            long lastWarningTicks = Interlocked.Read(ref _lastPanTiltStallWarningUtcTicks);

            if (nowTicks - lastWarningTicks < WebAgentStatusWarningIntervalTicks)
            {
                return;
            }

            Interlocked.Exchange(ref _lastPanTiltStallWarningUtcTicks, nowTicks);

            ConsoleLogHelper.Warning("WEB AGENT PAN/TILT", "Movement command was sent, but Function 0x01 position is unchanged" + " / PAN_RAW=" + panRaw + " / TILT_RAW=" + tiltRaw + " / CHECK=WebAgent PT status publisher");
        }

        internal void MarkPanTiltMoveCommandIssued()
        {
            switch (_context._activePanTiltMoveDirection)
            {
                case KeyboardPanTiltDirection.PanLeft:
                    _context._ptzPerformanceScenario = "PAN_LEFT";
                    Interlocked.Increment(ref _context._panStartTxCount);
                    break;
                case KeyboardPanTiltDirection.PanRight:
                    _context._ptzPerformanceScenario = "PAN_RIGHT";
                    Interlocked.Increment(ref _context._panStartTxCount);
                    break;
                case KeyboardPanTiltDirection.TiltUp:
                    _context._ptzPerformanceScenario = "TILT_UP";
                    Interlocked.Increment(ref _context._tiltStartTxCount);
                    break;
                case KeyboardPanTiltDirection.TiltDown:
                    _context._ptzPerformanceScenario = "TILT_DOWN";
                    Interlocked.Increment(ref _context._tiltStartTxCount);
                    break;
                default:
                    _context._ptzPerformanceScenario = "PAN_TILT_DIAGONAL";
                    Interlocked.Increment(ref _context._panStartTxCount);

                    Interlocked.Increment(ref _context._tiltStartTxCount);
                    break;
            }

            if (!_context.IsEnvironmentStatusSelected)
            {
                return;
            }

            Interlocked.Exchange(ref _lastPanTiltMoveCommandUtcTicks, DateTime.UtcNow.Ticks);
        }

        internal bool HasRecentLegacyEoLensStatus()
        {
            long receivedTicks = Interlocked.Read(ref _lastLegacyEoLensStatusUtcTicks);

            return receivedTicks > 0 && DateTime.UtcNow.Ticks - receivedTicks <= LegacyEoLensAuthorityTicks;
        }

        internal void LogWebAgentEoLensConflict(ushort zoom, ushort focus, byte flags)
        {
            long nowTicks = DateTime.UtcNow.Ticks;

            long lastWarningTicks = Interlocked.Read(ref _lastEoLensConflictWarningUtcTicks);

            if (nowTicks - lastWarningTicks < WebAgentStatusWarningIntervalTicks)
            {
                return;
            }

            Interlocked.Exchange(ref _lastEoLensConflictWarningUtcTicks, nowTicks);

            ConsoleLogHelper.Warning("WEB AGENT EO LENS", "Conflicting Function 0x28 value ignored; recent Function 0x01 retained" + " / FLAGS=0x" + flags.ToString("X2") + " / 0x28_ZOOM=" + zoom + " / 0x28_FOCUS=" + focus + " / ACTIVE_ZOOM=" + _context._currentEoZoom + " / ACTIVE_FOCUS=" + _context._currentEoFocus);
        }

        internal void ParseWebAgentFeatureState(byte[] p)
        {
            if (p.Length < 18)
            {
                return;
            }

            _context.DispatchSpecialFeaturePacket(p, _context.ApplySpecialFeatureStateCallback);

            byte target = p[1];

            byte feature = p[2];

            byte flags = p[3];

            short value = BitConverter.ToInt16(p, 4);

            if (feature == 0x1D)
            {
                var digitalPayload = (byte[])p.Clone();

                System.Windows.Application.Current?.Dispatcher.BeginInvoke(new Action(() =>
                {
                    if (_context.CameraFeatureTransportReady)
                    {
                        _context.ApplyDigitalZoomState(digitalPayload);
                    }
                }));
            }

            ConsoleLogHelper.State("WEB AGENT FEATURE", string.Format("TARGET=0x{0:X2} / FEATURE=0x{1:X2} / FLAGS=0x{2:X2} / VALUE={3}", target, feature, flags, value));

            // 카메라별 원시 번호는 색 이름이나 직접 버튼 선택 상태로 해석하지 않는다.
        }

        internal static void ParseWebAgentHoming(byte[] p)
        {
            if (p.Length < 7)
            {
                return;
            }

            ConsoleLogHelper.State("WEB AGENT HOMING", string.Format("TYPE={0} / AXIS={1} / STATE=0x{2:X2} / FLAGS=0x{3:X2} / STATUS=0x{4:X4}", p[1], p[2], p[3], p[4], BitConverter.ToUInt16(p, 5)));
        }

        internal void ParseWebAgentUnavailable(byte[] p)
        {
            if (p.Length < 6)
            {
                return;
            }

            _context.HandlePrecisionUnavailable(p);

            _context.HandleWebAgentEquipmentUnavailable(p);

            if (p[0] == 1 && p[1] == 0 && p[2] == 0 && p[3] == 0x2B)
            {
                _context._irFocusDirectUnavailableSession = _context._connectionSessionId;
            }

            _context.DispatchSpecialFeaturePacket(p, _context.ApplySpecialFeatureUnavailableCallback);

            _context.HandleGpsUnavailable(p);

            if (p[2] == 0x1D && p[3] == 0xE9 && p[4] < 2)
            {
                byte target = p[4];

                System.Windows.Application.Current?.Dispatcher.BeginInvoke(new Action(() =>
                {
                    if (_context.CameraFeatureTransportReady)
                    {
                        _context.ApplyDigitalZoomUnavailable(target);
                    }
                }));
            }

            ConsoleLogHelper.Warning("WEB AGENT V1.8", string.Format("Request unavailable / STATUS={0} / REQUEST={1:X2}{2:X2}{3:X2}{4:X2}", p[1], p[2], p[3], p[4], p[5]));
        }

        #endregion

    }
}
