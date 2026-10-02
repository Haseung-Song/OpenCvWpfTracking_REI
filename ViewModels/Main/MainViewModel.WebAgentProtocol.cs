using OpenCvWpfTracking.Common;
using OpenCvWpfTracking.Services.Communication;
using System;
using System.Text;
using System.Threading;
using System.Windows.Threading;

namespace OpenCvWpfTracking.ViewModels.Main
{
    /// <summary>WebAgent GUI-SBC additional protocol v1.8 response handling.</summary>
    public partial class MainViewModel
    {
        private const long LegacyEoLensAuthorityTicks =
            TimeSpan.TicksPerSecond * 3;

        private const long WebAgentStatusWarningIntervalTicks =
            TimeSpan.TicksPerSecond * 5;

        private long _lastLegacyEoLensStatusUtcTicks;

        private long _lastEoLensConflictWarningUtcTicks;

        private long _lastPanTiltMoveCommandUtcTicks;

        private long _lastPanTiltValueChangeUtcTicks;

        private long _lastPanTiltStallWarningUtcTicks;

        private int _lastWebAgentPanRaw = -1;

        private int _lastWebAgentTiltRaw = -1;

        private void InitializeWebAgentProtocolV18()
        {
            if (!IsEnvironmentStatusSelected) return;
            BeginWebAgentBuildCheck();
            _controlCommandService.RequestWebAgentIdentity();
            _controlCommandService.RequestLensCapabilityAndTelemetry(0x00);
            _controlCommandService.RequestLensCapabilityAndTelemetry(0x01);
            _controlCommandService.RequestCameraFeatureCapability(0x01);
            _controlCommandService.RequestImuTelemetry();
            RequestGpsAutoTxRateAfterConnection();
            ConsoleLogHelper.State("WEB AGENT V1.8", "Identity, EO/IR lens, IR feature, IMU and GPS rate queries sent");
        }

        private void ParseWebAgentV18Packet(LaResponsePacket packet)
        {
            byte[] payload = packet.Payload;
            try
            {
                switch (packet.Function)
                {
                    case 0x23: ParseWebAgentGps(payload); break;
                    case 0x24: ParseWebAgentImu(payload); break;
                    case 0x25: ParseWebAgentCapability(payload); break;
                    case 0x26: ParseWebAgentIdentity(payload); break;
                    case 0x27: ParseWebAgentFeatureState(payload); break;
                    case 0x28: ParseWebAgentLensTelemetry(payload); break;
                    case 0x29: ParseWebAgentHoming(payload); break;
                    case 0x2A: ParseWebAgentUnavailable(payload); break;
                    case 0x2B: ParseWebAgentRpyRateAck(payload); break;
                    case 0x2C: ParseWebAgentRpyOffsetAck(payload); break;
                    case 0x2D: ParseWebAgentSystemDateTime(payload); break;
                    case 0x2E: ParseStoredRpyOffsetResponse(payload); break;
                    case 0x2F: ParseStoredRpyRateResponse(payload); break;
                    case 0x30: ParseGpsAutoTxRateResponse(payload); break;
                }
            }
            catch (Exception ex)
            {
                ConsoleLogHelper.Warning("WEB AGENT V1.8", "Response parse failed / " + ex.Message);
            }
        }

        private void ParseWebAgentImu(byte[] p)
        {
            if (p.Length < 12) return;

            double roll = BitConverter.ToInt32(p, 0) / 10000.0;
            double pitch = BitConverter.ToInt32(p, 4) / 10000.0;
            double yaw = BitConverter.ToInt32(p, 8) / 10000.0;
            bool valid =
                Math.Abs(roll) <= 360.0 &&
                Math.Abs(pitch) <= 360.0 &&
                Math.Abs(yaw) <= 360.0;

            UpdatePositionImu(roll, pitch, yaw, valid);
            RecordRpyTelemetry(roll, pitch, yaw);
            UpdateGpsImuRawReference(yaw);
        }

        private void ParseWebAgentCapability(byte[] p)
        {
            if (p.Length < 13) return;
            ConsoleLogHelper.State("WEB AGENT CAPABILITY", string.Format(
                "SCHEMA={0} / TARGET=0x{1:X2} / LENS_FLAGS=0x{2:X4} / FEATURES={3}",
                p[0], p[1], BitConverter.ToUInt16(p, 2), p[12]));
            int entryCount = p[12];
            for (int index = 0; index < entryCount; index++)
            {
                int offset = 13 + index * 12;
                if (offset + 12 > p.Length) break;
                if (p[offset] == 0x05)
                {
                    _webAgentThermalPaletteService.ApplyPaletteCapability(BitConverter.ToUInt32(p, offset + 8));
                }
            }
        }

        private void ParseWebAgentIdentity(byte[] p)
        {
            if (p.Length < 2) return;
            int versionLength = p[0];
            if (versionLength > 31 || p.Length < versionLength + 2) return;
            int buildLength = p[versionLength + 1];
            if (p.Length < versionLength + buildLength + 2) return;
            string version = Encoding.ASCII.GetString(p, 1, versionLength);
            string build = Encoding.ASCII.GetString(p, versionLength + 2, buildLength);
            UpdateWebAgentBuild(build);
            ConsoleLogHelper.State("WEB AGENT IDENTITY", "VERSION=" + version + " / BUILD=" + build);
        }

        private void ParseWebAgentLensTelemetry(byte[] p)
        {
            if (p.Length < 16 || p[0] != 1) return;
            byte target = p[1];
            byte flags = p[2];
            ushort zoom = BitConverter.ToUInt16(p, 4);
            ushort focus = BitConverter.ToUInt16(p, 6);
            bool zoomValid = (flags & 0x01) != 0 && zoom <= 1000;
            bool focusValid = (flags & 0x02) != 0 && focus <= 1000;
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
                    if (zoomValid) _currentEoZoom = (short)zoom;
                    if (focusValid) _currentEoFocus = (short)focus;
                    if (zoomValid && focusValid)
                    {
                        UpdatePositionEoLens(_currentEoZoom, _currentEoFocus);
                    }
                    NotifyEoCurrentStatusChanged();
                }
                else if ((zoomValid && zoom != (ushort)_currentEoZoom) ||
                         (focusValid && focus != (ushort)_currentEoFocus))
                {
                    LogWebAgentEoLensConflict(zoom, focus, flags);
                }
            }
            else if (target == 0x01)
            {
                if (zoomValid) _currentIrZoom = zoom;
                if (focusValid) _currentIrFocus = focus;
                if (zoomValid && focusValid)
                {
                    UpdatePositionIrLens(_currentIrZoom, _currentIrFocus);
                }
                Interlocked.Increment(ref _irLensStatusVersion);
                NotifyIrCurrentStatusChanged();
            }
            ConsoleLogHelper.State("WEB AGENT LENS", string.Format(
                "TARGET=0x{0:X2} / VALID=0x{1:X2} / ZOOM={2} / FOCUS={3}", target, flags, zoom, focus));
        }

        private void MarkLegacyWebAgentStatusReceived(
            ushort panRaw,
            ushort tiltRaw)
        {
            if (!IsEnvironmentStatusSelected)
            {
                return;
            }

            long nowTicks = DateTime.UtcNow.Ticks;

            Interlocked.Exchange(
                ref _lastLegacyEoLensStatusUtcTicks,
                nowTicks);

            bool valueChanged =
                _lastWebAgentPanRaw != panRaw ||
                _lastWebAgentTiltRaw != tiltRaw;

            _lastWebAgentPanRaw = panRaw;
            _lastWebAgentTiltRaw = tiltRaw;

            if (valueChanged ||
                Interlocked.Read(ref _lastPanTiltValueChangeUtcTicks) == 0)
            {
                Interlocked.Exchange(
                    ref _lastPanTiltValueChangeUtcTicks,
                    nowTicks);
                return;
            }

            long lastCommandTicks =
                Interlocked.Read(ref _lastPanTiltMoveCommandUtcTicks);

            long lastChangeTicks =
                Interlocked.Read(ref _lastPanTiltValueChangeUtcTicks);

            if (lastCommandTicks == 0 ||
                nowTicks - lastCommandTicks > TimeSpan.TicksPerSecond * 3 ||
                nowTicks - lastChangeTicks < TimeSpan.TicksPerSecond)
            {
                return;
            }

            long lastWarningTicks =
                Interlocked.Read(ref _lastPanTiltStallWarningUtcTicks);

            if (nowTicks - lastWarningTicks <
                WebAgentStatusWarningIntervalTicks)
            {
                return;
            }

            Interlocked.Exchange(
                ref _lastPanTiltStallWarningUtcTicks,
                nowTicks);

            ConsoleLogHelper.Warning(
                "WEB AGENT PAN/TILT",
                "Movement command was sent, but Function 0x01 position is unchanged" +
                " / PAN_RAW=" + panRaw +
                " / TILT_RAW=" + tiltRaw +
                " / CHECK=WebAgent PT status publisher");
        }

        private void MarkPanTiltMoveCommandIssued()
        {
            switch (_activePanTiltMoveDirection)
            {
                case KeyboardPanTiltDirection.PanLeft: _ptzPerformanceScenario = "PAN_LEFT"; Interlocked.Increment(ref _panStartTxCount); break;
                case KeyboardPanTiltDirection.PanRight: _ptzPerformanceScenario = "PAN_RIGHT"; Interlocked.Increment(ref _panStartTxCount); break;
                case KeyboardPanTiltDirection.TiltUp: _ptzPerformanceScenario = "TILT_UP"; Interlocked.Increment(ref _tiltStartTxCount); break;
                case KeyboardPanTiltDirection.TiltDown: _ptzPerformanceScenario = "TILT_DOWN"; Interlocked.Increment(ref _tiltStartTxCount); break;
                default: _ptzPerformanceScenario = "PAN_TILT_DIAGONAL"; Interlocked.Increment(ref _panStartTxCount); Interlocked.Increment(ref _tiltStartTxCount); break;
            }
            if (!IsEnvironmentStatusSelected)
            {
                return;
            }

            Interlocked.Exchange(
                ref _lastPanTiltMoveCommandUtcTicks,
                DateTime.UtcNow.Ticks);
        }

        private bool HasRecentLegacyEoLensStatus()
        {
            long receivedTicks =
                Interlocked.Read(ref _lastLegacyEoLensStatusUtcTicks);

            return receivedTicks > 0 &&
                   DateTime.UtcNow.Ticks - receivedTicks <=
                   LegacyEoLensAuthorityTicks;
        }

        private void LogWebAgentEoLensConflict(
            ushort zoom,
            ushort focus,
            byte flags)
        {
            long nowTicks = DateTime.UtcNow.Ticks;
            long lastWarningTicks =
                Interlocked.Read(ref _lastEoLensConflictWarningUtcTicks);

            if (nowTicks - lastWarningTicks <
                WebAgentStatusWarningIntervalTicks)
            {
                return;
            }

            Interlocked.Exchange(
                ref _lastEoLensConflictWarningUtcTicks,
                nowTicks);

            ConsoleLogHelper.Warning(
                "WEB AGENT EO LENS",
                "Conflicting Function 0x28 value ignored; recent Function 0x01 retained" +
                " / FLAGS=0x" + flags.ToString("X2") +
                " / 0x28_ZOOM=" + zoom +
                " / 0x28_FOCUS=" + focus +
                " / ACTIVE_ZOOM=" + _currentEoZoom +
                " / ACTIVE_FOCUS=" + _currentEoFocus);
        }

        private void ParseWebAgentFeatureState(byte[] p)
        {
            if (p.Length < 18) return;
            byte target = p[1];
            byte feature = p[2];
            byte flags = p[3];
            short value = BitConverter.ToInt16(p, 4);
            ConsoleLogHelper.State("WEB AGENT FEATURE", string.Format(
                "TARGET=0x{0:X2} / FEATURE=0x{1:X2} / FLAGS=0x{2:X2} / VALUE={3}", target, feature, flags, value));
            if (target != 0x01 || feature != 0x05 || (flags & 0x03) != 0x03) return;
            _webAgentThermalPaletteService.ApplyReportedPalette(value);
            Dispatcher dispatcher = System.Windows.Application.Current?.Dispatcher;
            if (dispatcher == null) return;
            dispatcher.BeginInvoke(new Action(() =>
            {
                if (value == 1) UpdateThermalPaletteButtonVisual(0);
                else if (value == 0) UpdateThermalPaletteButtonVisual(1);
                else if (value == 3) UpdateThermalPaletteButtonVisual(2);
                else ResetThermalPaletteButtonVisuals();
            }));
        }

        private static void ParseWebAgentHoming(byte[] p)
        {
            if (p.Length < 7) return;
            ConsoleLogHelper.State("WEB AGENT HOMING", string.Format(
                "TYPE={0} / AXIS={1} / STATE=0x{2:X2} / FLAGS=0x{3:X2} / STATUS=0x{4:X4}",
                p[1], p[2], p[3], p[4], BitConverter.ToUInt16(p, 5)));
        }

        private void ParseWebAgentUnavailable(byte[] p)
        {
            if (p.Length < 6) return;
            HandleGpsUnavailable(p);
            ConsoleLogHelper.Warning("WEB AGENT V1.8", string.Format(
                "Request unavailable / STATUS={0} / REQUEST={1:X2}{2:X2}{3:X2}{4:X2}",
                p[1], p[2], p[3], p[4], p[5]));
        }
    }
}
