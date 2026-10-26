using OpenCvWpfTracking.Common;
using OpenCvWpfTracking.Services.Communication;
using System;
using System.Text;
using System.Threading;
using System.Windows.Threading;

namespace OpenCvWpfTracking.ViewModels.Main
{
    /// <summary>WEB AGENT 추가 기능과 상태 응답을 기능 번호에 따라 처리한다.</summary>
    public partial class MainViewModel
    {
        private const long LegacyEoLensAuthorityTicks = TimeSpan.TicksPerSecond * 3;

        private const long WebAgentStatusWarningIntervalTicks = TimeSpan.TicksPerSecond * 5;

        private long _lastLegacyEoLensStatusUtcTicks;

        private long _lastEoLensConflictWarningUtcTicks;

        private long _lastPanTiltMoveCommandUtcTicks;

        private long _lastPanTiltValueChangeUtcTicks;

        private long _lastPanTiltStallWarningUtcTicks;

        private int _lastWebAgentPanRaw = -1;

        private int _lastWebAgentTiltRaw = -1;

        private int _webAgentTelemetryNegotiationStarted;

        private int _webAgentSeparatedTelemetryAckMask;

        private readonly long[] _webAgentLensSampleMilliseconds = new long[2];

        private long _webAgentPanSampleMilliseconds;

        private long _webAgentTiltSampleMilliseconds;

        private long _webAgentPowerSampleMilliseconds;

        private long _webAgentSplitLogTicks;

        private readonly TelemetryClock _webAgentTelemetryClock = new TelemetryClock();

        private int _webAgentTelemetryClockGeneration = -1;

        private double _webAgentTelemetryRejectedLogAt;

        private void LogDeferredWebAgentTelemetry(string channel, ulong sample)
        {
            double now = TelemetryClock.MonotonicMilliseconds;

            if (now - _webAgentTelemetryRejectedLogAt < 5000)
                return;

            _webAgentTelemetryRejectedLogAt = now;
            ConsoleLogHelper.State(
                "WEB AGENT TELEMETRY DEFERRED",
                "CHANNEL=" + channel + " / SOURCE_TIMESTAMP=" + sample + " / Clock reference pending, invalid time, or stale sample; packet is not used for control"
            );
        }

        /// <summary>연결마다 이전 캐시 시각과 기능 협상 결과를 비운다.</summary>
        private void ResetWebAgentTelemetrySession()
        {
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

            _irFocusDirectUnavailableSession = null;
        }

        /// <summary>F1 장비 시간과 단조 시계를 사용하여 로컬 기준 샘플 시각으로 보정한다.</summary>
        private bool TryReadTelemetryTime(ulong milliseconds, out DateTimeOffset value, bool requireFresh = true)
        {
            int generation;
            bool valid = _webAgentTelemetryClock.TryGetLocalSampleTime(
                milliseconds,
                TelemetryClock.MonotonicMilliseconds,
                DateTimeOffset.UtcNow,
                out value,
                out generation,
                requireFresh
            );

            if (generation != _webAgentTelemetryClockGeneration)
            {
                _webAgentTelemetryClockGeneration = generation;
                Interlocked.Exchange(ref _webAgentPanSampleMilliseconds, 0);
                Interlocked.Exchange(ref _webAgentTiltSampleMilliseconds, 0);
                Interlocked.Exchange(ref _webAgentPowerSampleMilliseconds, 0);

                for (int index = 0; index < _webAgentLensSampleMilliseconds.Length; index++)
                    Interlocked.Exchange(ref _webAgentLensSampleMilliseconds[index], 0);
            }

            return valid;
        }

        /// <summary>
        /// WEB AGENT 연결 세션의 응답 상태를 초기화하고 필요한 기능·장비 정보를 조회한다.
        /// </summary>
        private void InitializeWebAgentProtocolV18()
        {
            if (!IsEnvironmentStatusSelected)
                return;

            ResetDigitalZoomSession();
            BeginWebAgentBuildCheck();
            _controlCommandService.RequestWebAgentIdentity();
            _controlCommandService.RequestLensCapabilityAndTelemetry(0x00);
            _controlCommandService.RequestLensCapabilityAndTelemetry(0x01);
            _controlCommandService.RequestCameraFeatureCapability(0x01);
            _controlCommandService.RequestCameraFeatureCapability(0x00);
            _controlCommandService.RequestImuTelemetry();
            RequestGpsAutoTxRateAfterConnection();

            for (byte category = 0; category <= 3; category++)
            {
                _controlCommandService.RequestWebAgentTelemetrySetting(category);
            }

            ConsoleLogHelper.State(
                "WEB AGENT V2.9",
                "Identity, EO/IR lens, feature, IMU, GPS rate and separated telemetry capability queries sent"
            );
        }

        /// <summary>
        /// 응답 기능 번호에 따라 WEB AGENT 상태 데이터를 각 해석 경로로 전달한다.
        /// </summary>
        private void ParseWebAgentV18Packet(LaResponsePacket packet)
        {
            byte[] payload = packet.Payload;

            try
            {
                switch (packet.Function)
                {
                    case 0x23:
                        ParseWebAgentGps(payload);
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
                        ParseWebAgentRpyRateAck(payload);
                        break;
                    case 0x2C:
                        ParseWebAgentRpyOffsetAck(payload);
                        break;
                    case 0x2D:
                        ParseWebAgentSystemDateTime(payload);
                        break;
                    case 0x2E:
                        ParseStoredRpyOffsetResponse(payload);
                        break;
                    case 0x2F:
                        ParseStoredRpyRateResponse(payload);
                        break;
                    case 0x30:
                        ParseGpsAutoTxRateResponse(payload);
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
                }
            }
            catch (Exception ex)
            {
                ConsoleLogHelper.Warning("WEB AGENT V1.8", "Response parse failed / " + ex.Message);
            }
        }

        /// <summary>
        /// v2.9 Function 0x32 PAN/TILT 분리 텔레메트리를 기존 상태에 반영한다.
        /// Payload의 각도와 시각은 Little Endian이다.
        /// </summary>
        private void ParseWebAgentPanTiltTelemetry(byte[] p)
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
                LogDeferredWebAgentTelemetry("PT", panUpdatedAt);

            if ((panValid && panRaw > 35999) || (tiltValid && tiltRaw > 35999))
            {
                ConsoleLogHelper.Warning("WEB AGENT TELEMETRY", $"FUNCTION=0x32 / OUT_OF_RANGE / PAN={panRaw} / TILT={tiltRaw}");
                return;
            }

            double previousPan = _currentPan;
            double previousTilt = _currentTilt;
            bool panNew = panValid && (long)panUpdatedAt > Interlocked.Read(ref _webAgentPanSampleMilliseconds);
            bool tiltNew = tiltValid && (long)tiltUpdatedAt > Interlocked.Read(ref _webAgentTiltSampleMilliseconds);

            if (panNew)
            {
                Interlocked.Exchange(ref _webAgentPanSampleMilliseconds, (long)panUpdatedAt);
                _currentPan = ConvertWebAgentPanToGuiAngle(panRaw / 100.0);

                if (Math.Abs(Math.Abs(_currentPan) - 180.0) <= 0.05 &&
                    _lastPanAbsoluteTarget.HasValue &&
                    Math.Abs(Math.Abs(_lastPanAbsoluteTarget.Value) - 180.0) <= 0.05)
                {
                    _currentPan = _lastPanAbsoluteTarget.Value < 0 ? -180.0 : 180.0;
                }
            }

            if (tiltNew)
            {
                Interlocked.Exchange(ref _webAgentTiltSampleMilliseconds, (long)tiltUpdatedAt);
                _currentTilt = NormalizeUnsignedWebAgentTilt(tiltRaw / 100.0, _currentTilt);
            }

            if (!panNew && !tiltNew)
            {
                return;
            }

            if (panNew && _activePanAbsoluteTarget.HasValue && Math.Abs(_currentPan - _activePanAbsoluteTarget.Value) <= 0.03)
                _activePanAbsoluteTarget = null;

            if (tiltNew &&
                _activeTiltAbsoluteTarget.HasValue &&
                Math.Abs(GetTiltDifference(
                _currentTilt,
                _activeTiltAbsoluteTarget.Value
            )) <= 0.03)
                _activeTiltAbsoluteTarget = null;

            if (panValid && tiltValid)
            {
                DateTimeOffset poseTime = panTime < tiltTime ? panTime : tiltTime;
                Interlocked.Exchange(ref _videoCenterPose, Tuple.Create(_currentPan, _currentTilt, poseTime.UtcDateTime.Ticks));
                Interlocked.Increment(ref _panTiltStatusVersion);
                _positionSnapshotService.UpdatePtz(_currentPan, _currentTilt, poseTime);
                QueuePositionStatusNotification();
            }

            NotifyEoCurrentStatusChanged(
                panValid &&
                Math.Abs(previousPan - _currentPan) > 0.0001,
                tiltValid &&
                Math.Abs(previousTilt - _currentTilt) > 0.0001,
                false,
                false,
                false
            );
            long nowTicks = DateTime.UtcNow.Ticks;

            if (nowTicks - Interlocked.Read(ref _webAgentSplitLogTicks) >= TimeSpan.TicksPerSecond)
            {
                Interlocked.Exchange(ref _webAgentSplitLogTicks, nowTicks);
                ConsoleLogHelper.State(
                    "WEB AGENT TELEMETRY",
                    $"FUNCTION=0x32 / VALID=0x{validFlags:X2} / PAN={_currentPan:F2} / TILT={_currentTilt:F2} / PAN_AT={panUpdatedAt} / TILT_AT={tiltUpdatedAt}"
                );
            }
        }

        /// <summary>
        /// v2.9 Function 0x33 MCB/SCB 전원 상태 이벤트를 반영한다.
        /// 미수신 Valid=0은 실제 OFF로 해석하지 않는다.
        /// </summary>
        private void ParseWebAgentPowerTelemetry(byte[] p)
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

            if ((validFlags & 0x01) != 0 &&
                TryReadTelemetryTime(
                updatedAt,
                out powerTime,
                false
            ) &&
                (long)updatedAt > Interlocked.Read(ref _webAgentPowerSampleMilliseconds))
            {
                Interlocked.Exchange(ref _webAgentPowerSampleMilliseconds, (long)updatedAt);
                ApplyDevicePowerStatus(mcbStatus, scbStatus, powerTime.LocalDateTime, "0x33");
            }

            ConsoleLogHelper.State(
                "WEB AGENT TELEMETRY",
                $"FUNCTION=0x33 / VALID=0x{validFlags:X2} / MCB=0x{mcbStatus:X2} / SCB=0x{scbStatus:X2} / UPDATED_AT={updatedAt}"
            );
        }

        /// <summary>
        /// v2.9 Function 0x34 설정/지원 응답을 처리한다.
        /// 구형 서버는 BB에 응답하지 않으므로 기존 호환 모드를 그대로 유지한다.
        /// </summary>
        private void ParseWebAgentTelemetrySetting(byte[] p)
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

            ConsoleLogHelper.State(
                "WEB AGENT TELEMETRY",
                $"FUNCTION=0x34 / CATEGORY={category} / MODE={mode} / HZ={frequencyHz} / MAX={maximumHz} / FLAGS=0x{flags:X2}"
            );

            if (IsEnvironmentStatusSelected &&
                category == 0 &&
                maximumHz >= 10 &&
                (flags & 0x01) != 0 &&
                Interlocked.CompareExchange(
                ref _webAgentTelemetryNegotiationStarted,
                1,
                0
            ) == 0)
            {
                bool ptSent = _controlCommandService.SetWebAgentTelemetryRate(0, 10);
                bool eoSent = _controlCommandService.SetWebAgentTelemetryRate(1, 2);
                bool irSent = _controlCommandService.SetWebAgentTelemetryRate(2, 2);
                ConsoleLogHelper.Command(
                    "WEB AGENT TELEMETRY",
                    $"SEPARATED_MODE_REQUEST / PT_10HZ={ptSent} / EO_2HZ={eoSent} / IR_2HZ={irSent}"
                );
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
                    _controlCommandService.RequestWebAgentTelemetrySnapshot(3);
                }
            }
        }

        /// <summary>
        /// WEB AGENT의 관성 센서 응답을 해석해 현재 센서 상태를 갱신한다.
        /// </summary>
        private void ParseWebAgentImu(byte[] p)
        {
            if (p.Length < 12)
                return;

            double roll = BitConverter.ToInt32(p, 0) / 10000.0;
            double pitch = BitConverter.ToInt32(p, 4) / 10000.0;
            double yaw = BitConverter.ToInt32(p, 8) / 10000.0;
            bool valid = Math.Abs(roll) <= 360.0 && Math.Abs(pitch) <= 360.0 && Math.Abs(yaw) <= 360.0;
            UpdatePositionImu(roll, pitch, yaw, valid);
            RecordRpyTelemetry(roll, pitch, yaw);
            UpdateGpsImuRawReference(yaw);
        }

        /// <summary>
        /// 장비가 응답한 지원 기능을 반영해 사용 가능한 제어 기능을 구분한다.
        /// </summary>
        private void ParseWebAgentCapability(byte[] p)
        {
            if (p.Length < 13)
                return;

            DispatchSpecialFeaturePacket(p, ApplySpecialFeatureCapability);
            var digitalPayload = (byte[])p.Clone();
            System.Windows.Application.Current?.Dispatcher.BeginInvoke(new Action(() =>
            {
                if (CameraFeatureTransportReady)
                    ApplyDigitalZoomCapability(digitalPayload);
            }));
            ConsoleLogHelper.State(
                "WEB AGENT CAPABILITY",
                string.Format(
                "SCHEMA={0} / TARGET=0x{1:X2} / LENS_FLAGS=0x{2:X4} / FEATURES={3}",
                p[0],
                p[1],
                BitConverter.ToUInt16(
                p,
                2
            ),
                p[12]
            )
            );
            int entryCount = p[12];

            for (int index = 0; index < entryCount; index++)
            {
                int offset = 13 + index * 12;

                if (offset + 12 > p.Length)
                    break;

                if (p[offset] == 0x05)
                {
                    _webAgentThermalPaletteService.ApplyPaletteCapability(BitConverter.ToUInt32(p, offset + 8));
                }
            }
        }

        /// <summary>
        /// 장비 식별 응답을 읽어 연결 장비 정보를 갱신한다.
        /// </summary>
        private void ParseWebAgentIdentity(byte[] p)
        {
            if (p.Length < 2)
                return;

            int versionLength = p[0];

            if (versionLength > 31 || p.Length < versionLength + 2)
                return;

            int buildLength = p[versionLength + 1];

            if (p.Length < versionLength + buildLength + 2)
                return;

            string version = Encoding.ASCII.GetString(p, 1, versionLength);
            string build = Encoding.ASCII.GetString(p, versionLength + 2, buildLength);
            UpdateWebAgentBuild(build);
            ConsoleLogHelper.State("WEB AGENT IDENTITY", "VERSION=" + version + " / BUILD=" + build);
        }

        /// <summary>
        /// 채널별 렌즈 위치와 유효 시각을 확인해 수신한 줌·초점 상태를 반영한다.
        /// </summary>
        private void ParseWebAgentLensTelemetry(byte[] p)
        {
            if (p.Length != 16 || p[0] != 1 || p[1] > 1)
                return;

            byte target = p[1];
            byte flags = p[2];
            ushort zoom = BitConverter.ToUInt16(p, 4);
            ushort focus = BitConverter.ToUInt16(p, 6);
            bool zoomValid = (flags & 0x01) != 0 && zoom <= 1000;
            bool focusValid = (flags & 0x02) != 0 && focus <= 1000;
            DateTimeOffset sampleTime;
            ulong sampleMilliseconds = BitConverter.ToUInt64(p, 8);

            if (!TryReadTelemetryTime(
                sampleMilliseconds,
                out sampleTime
            ) ||
                DateTimeOffset.UtcNow - sampleTime > TimeSpan.FromSeconds(3))
            {
                LogDeferredWebAgentTelemetry(target == 0 ? "EO" : "IR", sampleMilliseconds);
                return;
            }

            if ((long)sampleMilliseconds <= Interlocked.Read(ref _webAgentLensSampleMilliseconds[target]))
                return;

            Interlocked.Exchange(ref _webAgentLensSampleMilliseconds[target], (long)sampleMilliseconds);

            if (target == 0x00)
            {
                Interlocked.Increment(ref _eoLensStatusVersion);

                // 2026-09-15: Web Agent가 기존 Function 0x01에서는 EO Zoom/Focus를
                // 정상값으로 보내면서 Function 0x28에서는 valid=1인 0을 반복하는
                // 실장비 현상을 확인했다. 최근 0x01 상태가 있으면 해당 값을
                // 권위값으로 유지하여 CURRENT STATUS가 0과 정상값 사이에서
                // 깜빡이지 않도록 한다. 0x01이 끊긴 경우에만 0x28을 fallback한다.
                if (!HasRecentLegacyEoLensStatus())
                {
                    if (zoomValid)
                    {
                        _currentEoZoom = (short)zoom;
                        MarkLensEvidence(false);
                    }

                    if (focusValid)
                        _currentEoFocus = (short)focus;

                    MarkPresetLensEvidence(false, zoomValid, focusValid);

                    if (zoomValid && focusValid)
                    {
                        UpdatePositionEoLens(_currentEoZoom, _currentEoFocus);
                    }

                    NotifyEoCurrentStatusChanged();
                }
                else if ((zoomValid && zoom != (ushort)_currentEoZoom) || (focusValid && focus != (ushort)_currentEoFocus))
                {
                    LogWebAgentEoLensConflict(zoom, focus, flags);
                }
            }
            else if (target == 0x01)
            {
                if (zoomValid)
                {
                    _currentIrZoom = zoom;
                    MarkLensEvidence(true);
                }

                if (focusValid)
                    _currentIrFocus = focus;

                MarkPresetLensEvidence(true, zoomValid, focusValid);

                if (zoomValid && focusValid)
                {
                    UpdatePositionIrLens(_currentIrZoom, _currentIrFocus);
                }

                Interlocked.Increment(ref _irLensStatusVersion);
                NotifyIrCurrentStatusChanged();
            }

            ConsoleLogHelper.State(
                "WEB AGENT LENS",
                string.Format(
                "TARGET=0x{0:X2} / VALID=0x{1:X2} / ZOOM={2} / FOCUS={3}",
                target,
                flags,
                zoom,
                focus
            )
            );
        }

        /// <summary>
        /// 기존 형식의 상태 수신 시각을 기록해 오래된 상태와 구분한다.
        /// </summary>
        private void MarkLegacyWebAgentStatusReceived(ushort panRaw, ushort tiltRaw)
        {
            if (!IsEnvironmentStatusSelected)
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

            if (lastCommandTicks == 0 ||
                _activePanTiltMoveDirection == KeyboardPanTiltDirection.None ||
                nowTicks - lastCommandTicks < TimeSpan.TicksPerSecond ||
                nowTicks - lastCommandTicks > TimeSpan.TicksPerSecond * 3 ||
                nowTicks - lastChangeTicks < TimeSpan.TicksPerSecond)
            {
                return;
            }

            long lastWarningTicks = Interlocked.Read(ref _lastPanTiltStallWarningUtcTicks);

            if (nowTicks - lastWarningTicks < WebAgentStatusWarningIntervalTicks)
            {
                return;
            }

            Interlocked.Exchange(ref _lastPanTiltStallWarningUtcTicks, nowTicks);
            ConsoleLogHelper.Warning(
                "WEB AGENT PAN/TILT",
                "Movement command was sent, but Function 0x01 position is unchanged" + " / PAN_RAW=" + panRaw + " / TILT_RAW=" + tiltRaw + " / CHECK=WebAgent PT status publisher"
            );
        }

        /// <summary>
        /// 팬·틸트 이동 명령 시점을 기록해 이전 수신 위치를 새 이동의 완료로 오인하지 않는다.
        /// </summary>
        private void MarkPanTiltMoveCommandIssued()
        {
            switch (_activePanTiltMoveDirection)
            {
                case KeyboardPanTiltDirection.PanLeft:
                    _ptzPerformanceScenario = "PAN_LEFT";
                    Interlocked.Increment(ref _panStartTxCount);
                    break;
                case KeyboardPanTiltDirection.PanRight:
                    _ptzPerformanceScenario = "PAN_RIGHT";
                    Interlocked.Increment(ref _panStartTxCount);
                    break;
                case KeyboardPanTiltDirection.TiltUp:
                    _ptzPerformanceScenario = "TILT_UP";
                    Interlocked.Increment(ref _tiltStartTxCount);
                    break;
                case KeyboardPanTiltDirection.TiltDown:
                    _ptzPerformanceScenario = "TILT_DOWN";
                    Interlocked.Increment(ref _tiltStartTxCount);
                    break;
                default:
                    _ptzPerformanceScenario = "PAN_TILT_DIAGONAL";
                    Interlocked.Increment(ref _panStartTxCount);
                    Interlocked.Increment(ref _tiltStartTxCount);
                    break;
            }

            if (!IsEnvironmentStatusSelected)
            {
                return;
            }

            Interlocked.Exchange(ref _lastPanTiltMoveCommandUtcTicks, DateTime.UtcNow.Ticks);
        }

        /// <summary>
        /// 기존 형식 EO 렌즈 상태가 최근 수신되어 비교에 사용할 수 있는지 확인한다.
        /// </summary>
        private bool HasRecentLegacyEoLensStatus()
        {
            long receivedTicks = Interlocked.Read(ref _lastLegacyEoLensStatusUtcTicks);
            return receivedTicks > 0 && DateTime.UtcNow.Ticks - receivedTicks <= LegacyEoLensAuthorityTicks;
        }

        /// <summary>
        /// 신규·기존 EO 렌즈 상태가 충돌할 때 진단 로그를 남긴다.
        /// </summary>
        private void LogWebAgentEoLensConflict(ushort zoom, ushort focus, byte flags)
        {
            long nowTicks = DateTime.UtcNow.Ticks;
            long lastWarningTicks = Interlocked.Read(ref _lastEoLensConflictWarningUtcTicks);

            if (nowTicks - lastWarningTicks < WebAgentStatusWarningIntervalTicks)
            {
                return;
            }

            Interlocked.Exchange(ref _lastEoLensConflictWarningUtcTicks, nowTicks);
            ConsoleLogHelper.Warning(
                "WEB AGENT EO LENS",
                "Conflicting Function 0x28 value ignored; recent Function 0x01 retained" + " / FLAGS=0x" + flags.ToString("X2") + " / 0x28_ZOOM=" + zoom + " / 0x28_FOCUS=" + focus + " / ACTIVE_ZOOM=" + _currentEoZoom + " / ACTIVE_FOCUS=" + _currentEoFocus
            );
        }

        /// <summary>
        /// 장비 기능 상태 응답을 해석해 현재 기능 상태를 갱신한다.
        /// </summary>
        private void ParseWebAgentFeatureState(byte[] p)
        {
            if (p.Length < 18)
                return;

            DispatchSpecialFeaturePacket(p, ApplySpecialFeatureState);
            byte target = p[1];
            byte feature = p[2];
            byte flags = p[3];
            short value = BitConverter.ToInt16(p, 4);

            if (feature == 0x1D)
            {
                var digitalPayload = (byte[])p.Clone();
                System.Windows.Application.Current?.Dispatcher.BeginInvoke(new Action(() =>
                {
                    if (CameraFeatureTransportReady)
                        ApplyDigitalZoomState(digitalPayload);
                }));
            }

            ConsoleLogHelper.State(
                "WEB AGENT FEATURE",
                string.Format(
                "TARGET=0x{0:X2} / FEATURE=0x{1:X2} / FLAGS=0x{2:X2} / VALUE={3}",
                target,
                feature,
                flags,
                value
            )
            );

            if (target != 0x01 || feature != 0x05 || (flags & 0x03) != 0x03)
                return;

            _webAgentThermalPaletteService.ApplyReportedPalette(value);
            Dispatcher dispatcher = System.Windows.Application.Current?.Dispatcher;

            if (dispatcher == null)
                return;

            dispatcher.BeginInvoke(new Action(() =>
            {
                if (value == 1)
                    UpdateThermalPaletteButtonVisual(0);
                else if (value == 0)
                    UpdateThermalPaletteButtonVisual(1);
                else if (value == 3)
                    UpdateThermalPaletteButtonVisual(2);
                else
                    ResetThermalPaletteButtonVisuals();
            }));
        }

        /// <summary>
        /// 홈 동작 응답을 해석해 진행 상태를 반영한다.
        /// </summary>
        private static void ParseWebAgentHoming(byte[] p)
        {
            if (p.Length < 7)
                return;

            ConsoleLogHelper.State(
                "WEB AGENT HOMING",
                string.Format(
                "TYPE={0} / AXIS={1} / STATE=0x{2:X2} / FLAGS=0x{3:X2} / STATUS=0x{4:X4}",
                p[1],
                p[2],
                p[3],
                p[4],
                BitConverter.ToUInt16(
                p,
                5
            )
            )
            );
        }

        /// <summary>
        /// 장비의 명령 미지원·실행 불가 응답을 처리한다.
        /// </summary>
        private void ParseWebAgentUnavailable(byte[] p)
        {
            if (p.Length < 6)
                return;

            if (p[0] == 1 && p[2] == 0 && p[3] == 0x2B)
            {
                _irFocusDirectUnavailableSession = _connectionSessionId;
            }

            DispatchSpecialFeaturePacket(p, ApplySpecialFeatureUnavailable);
            HandleGpsUnavailable(p);

            if (p[2] == 0x1D && p[3] == 0xE9 && p[4] < 2)
            {
                byte target = p[4];
                System.Windows.Application.Current?.Dispatcher.BeginInvoke(new Action(() =>
                {
                    if (CameraFeatureTransportReady)
                        ApplyDigitalZoomUnavailable(target);
                }));
            }

            ConsoleLogHelper.Warning(
                "WEB AGENT V1.8",
                string.Format(
                "Request unavailable / STATUS={0} / REQUEST={1:X2}{2:X2}{3:X2}{4:X2}",
                p[1],
                p[2],
                p[3],
                p[4],
                p[5]
            )
            );
        }
    }
}
