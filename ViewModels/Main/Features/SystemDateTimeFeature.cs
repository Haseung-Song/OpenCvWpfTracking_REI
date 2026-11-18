using OpenCvWpfTracking.Common;
using OpenCvWpfTracking.Services.Communication;
using System;
using System.Globalization;
using System.Windows.Input;
using System.Windows.Threading;
using static OpenCvWpfTracking.ViewModels.Main.MainViewModel;

namespace OpenCvWpfTracking.ViewModels.Main
{
    /// <summary>장비 시간 조회에 필요한 협력 기능만 받는 경계입니다. 화면 객체의 내부 구현을 알지 않습니다.</summary>
    internal interface ISystemDateTimeFeatureContext
    {
        IControlCommandServicePort _controlCommandService { get; }

        ITcpClientServicePort _laTcpService { get; }

        TelemetryClock _webAgentTelemetryClock { get; }

        bool IsEnvironmentStatusSelected { get; }

        bool IsOperationCommandEnabled { get; }

        bool IsSelectedControlAgentWebAgent { get; }

        void OnPropertyChanged([global::System.Runtime.CompilerServices.CallerMemberName] string propertyName = null);

        int SelectedCommunicationSettingsTabIndex { get; set; }

        Action RequestSystemDateTimeCommandCallback { get; }

        Action SetSystemDateTimeFromPcCallback { get; }

        EventHandler HandleSystemTimePollTickCallback { get; }

        Action RefreshSystemDateTimeAfterSetCallback { get; }
    }

    /// <summary>장비 시간 조회 담당 객체입니다. 상태를 보관하고 입력을 받아 결과를 알립니다.</summary>
    internal sealed class SystemDateTimeFeature
    {

        #region [협력 객체와 초기화]

        private readonly ISystemDateTimeFeatureContext _context;

        internal SystemDateTimeFeature(ISystemDateTimeFeatureContext context)
        {
            _context = context ?? throw new System.ArgumentNullException(nameof(context));
        }

        internal void InitializeState()
        {
            _webAgentUtcDateTimeText = "장비 UTC: 조회 대기";
            _webAgentLocalDateTimeText = "장비 로컬 변환: 조회 대기";
            _pcLocalDateTimeText = "PC 로컬: 확인 대기";
            _pcUtcDateTimeText = "PC UTC: 확인 대기";
            _systemDateTimeStatusText = "Ready";
        }

        #endregion

        #region [상태 필드]

        internal string _webAgentUtcDateTimeText;

        internal string _webAgentLocalDateTimeText;

        internal string _pcLocalDateTimeText;

        internal string _pcUtcDateTimeText;

        internal string _systemDateTimeStatusText;

        internal DispatcherTimer _systemTimePollTimer;

        internal volatile bool _systemTimeQueryInFlight;

        internal DateTime _systemTimeQuerySentAt;

        internal bool _isAutomaticSystemTimeQuery;

        internal bool _manualSystemTimeQueryPending;

        internal double _systemTimeQueryMonotonicSentAt;

        internal double _systemTimeLastAutomaticQueryAt;

        #endregion

        #region [화면 속성과 이벤트]

        internal ICommand RequestSystemDateTimeCommand { get; set; }

        internal ICommand SetSystemDateTimeFromPcCommand { get; set; }

        internal string WebAgentUtcDateTimeText
        {
            get { return _webAgentUtcDateTimeText; }

            set
            {
                if (_webAgentUtcDateTimeText == value)
                {
                    return;
                }

                _webAgentUtcDateTimeText = value;

                _context.OnPropertyChanged();
            }
        }

        internal string WebAgentLocalDateTimeText
        {
            get { return _webAgentLocalDateTimeText; }

            set
            {
                if (_webAgentLocalDateTimeText == value)
                {
                    return;
                }

                _webAgentLocalDateTimeText = value;

                _context.OnPropertyChanged();
            }
        }

        internal string PcLocalDateTimeText
        {
            get { return _pcLocalDateTimeText; }

            set
            {
                if (_pcLocalDateTimeText == value)
                {
                    return;
                }

                _pcLocalDateTimeText = value;

                _context.OnPropertyChanged();
            }
        }

        internal string PcUtcDateTimeText
        {
            get { return _pcUtcDateTimeText; }

            set
            {
                if (_pcUtcDateTimeText == value)
                {
                    return;
                }

                _pcUtcDateTimeText = value;

                _context.OnPropertyChanged();
            }
        }

        internal string SystemDateTimeStatusText
        {
            get { return _systemDateTimeStatusText; }

            set
            {
                if (_systemDateTimeStatusText == value)
                {
                    return;
                }

                _systemDateTimeStatusText = value;

                _context.OnPropertyChanged();
            }
        }

        #endregion

        #region [동작 처리]

        internal void InitializeSystemDateTimeSettings()
        {
            RequestSystemDateTimeCommand = new RelayCommand(_context.RequestSystemDateTimeCommandCallback);

            SetSystemDateTimeFromPcCommand = new RelayCommand(_context.SetSystemDateTimeFromPcCallback);

            _systemTimePollTimer = new DispatcherTimer(DispatcherPriority.Background)
            {
                Interval = TimeSpan.FromSeconds(1)
            };

            _systemTimePollTimer.Tick += _context.HandleSystemTimePollTickCallback;

            UpdatePcDateTimePreview();

            VerifySystemDateTimePacketEncoding();
        }

        internal void RequestSystemDateTime()
        {
            if (!CanUseSystemDateTime(out string reason))
            {
                SystemDateTimeStatusText = reason;

                ConsoleLogHelper.Warning("SYSTEM TIME QUERY", reason);

                return;
            }

            bool isAutomatic = _isAutomaticSystemTimeQuery;

            UpdatePcDateTimePreview();

            // F1에는 요청 번호가 없어 왕복 지연 보정용 조회를 중복 실행하지 않는다.
            if (_systemTimeQueryInFlight && TelemetryClock.MonotonicMilliseconds - _systemTimeQueryMonotonicSentAt < 2000)
            {
                if (!isAutomatic)
                {
                    _manualSystemTimeQueryPending = true;
                }

                return;
            }

            // 2026-10-06: 사용자가 누른 단건 조회는 자동 폴링의 응답 대기 상태보다
            // 우선한다. 자동 조회가 진행 중이어도 즉시 새 요청을 송신한다.
            if (!isAutomatic)
            {
                _systemTimeQueryInFlight = false;
                _manualSystemTimeQueryPending = true;
                SystemDateTimeStatusText = "조회 응답 대기";
            }

            _systemTimeQueryMonotonicSentAt = TelemetryClock.MonotonicMilliseconds;
            _systemTimeQuerySentAt = DateTime.Now;
            _systemTimeQueryInFlight = true;

            bool sent = _context._controlCommandService.RequestSystemDateTime();

            if (!isAutomatic)
            {
                ConsoleLogHelper.Command("SYSTEM TIME QUERY TX", "CMD2=0xF1 / PACKET=FF 01 00 F1 00 00 F2 / SENT=" + sent);
            }

            if (!sent)
            {
                _systemTimeQueryInFlight = false;
                _manualSystemTimeQueryPending = false;
                SystemDateTimeStatusText = "Failed: 조회 명령 전송 실패";
            }
        }

        internal void SetSystemDateTimeFromPc()
        {
            if (!CanUseSystemDateTime(out string reason))
            {
                SystemDateTimeStatusText = reason;

                ConsoleLogHelper.Warning("SYSTEM TIME SET", reason);

                return;
            }

            DateTime pcLocal = DateTime.Now;

            DateTime pcUtc = pcLocal.ToUniversalTime();

            byte[] packet = ControlCommandService.BuildSystemDateTimePacket(pcUtc);

            UpdatePcDateTimePreview(pcLocal, pcUtc);

            SystemDateTimeStatusText = "설정 응답 대기";

            bool sent = _context._controlCommandService.SetSystemDateTimeUtc(pcUtc);

            ConsoleLogHelper.Command("SYSTEM TIME SET TX", "PC_LOCAL=" + FormatDateTime(pcLocal) + " / UTC=" + FormatDateTime(pcUtc) + " / PACKET=" + ToHex(packet) + " / SENT=" + sent);

            if (!sent)
            {
                SystemDateTimeStatusText = "Failed: 설정 명령 전송 실패";
            }
        }

        internal bool CanUseSystemDateTime(out string reason)
        {
            // 2026-10-06: 파노라마/AUTO SCAN/HOME/ZERO 수행 중에는
            // 조회와 설정 모두 막아 장비 명령 흐름을 단일화한다.
            if (!_context.IsOperationCommandEnabled)
            {
                reason = "운용 작업 중 SYSTEM TIME 제어 잠금";

                return false;
            }

            if (!_context.IsEnvironmentStatusSelected)
            {
                reason = "WebAgent 전용 기능";

                return false;
            }

            if (_context._laTcpService == null || !_context._laTcpService.IsConnected)
            {
                reason = "Failed: WebAgent 미연결";

                return false;
            }

            reason = string.Empty;

            return true;
        }

        internal void ParseWebAgentSystemDateTime(byte[] payload)
        {
            double receivedAt = TelemetryClock.MonotonicMilliseconds;

            bool queryWasPending = _systemTimeQueryInFlight;

            double querySentAt = _systemTimeQueryMonotonicSentAt;

            _systemTimeQueryInFlight = false;
            _manualSystemTimeQueryPending = false;

            if (payload == null || payload.Length < 10)
            {
                SetSystemDateTimeFailure("Failed: 0x2D 응답 길이 오류");

                return;
            }

            byte version = payload[0];

            byte operation = payload[1];

            byte status = payload[2];

            int year = payload[3] | payload[4] << 8;

            string operationText = operation == 0x00 ? "QUERY" : operation == 0x01 ? "SET" : "UNKNOWN";

            DateTime utcDateTime;

            try
            {
                utcDateTime = new DateTime(year, payload[5], payload[6], payload[7], payload[8], payload[9], DateTimeKind.Utc);
            }
            catch (ArgumentOutOfRangeException ex)
            {
                SetSystemDateTimeFailure("Failed: 0x2D 날짜 범위 오류");

                ConsoleLogHelper.Warning("SYSTEM TIME RX", ex.Message);

                return;
            }

            string statusText = GetSystemDateTimeStatusText(status);

            bool success = version == 0x01 && (operation == 0x00 || operation == 0x01) && status == 0x00;

            if (success && operation == 0x00 && queryWasPending)
            {
                bool clockChanged;

                if (_context._webAgentTelemetryClock.Observe(new DateTimeOffset(utcDateTime).ToUnixTimeMilliseconds(), querySentAt, receivedAt, out clockChanged) && clockChanged)
                {
                    ConsoleLogHelper.State("WEB AGENT CLOCK", "F1 clock reference established/reset; no EF time write / PC_MINUS_SBC_SECONDS=" + (DateTime.UtcNow - utcDateTime).TotalSeconds.ToString("F1", CultureInfo.InvariantCulture));
                }
            }

            if (success && operation == 0x01)
            {
                _context._webAgentTelemetryClock.Reset();
            }

            RunOnUiThread(() =>
            {
                WebAgentUtcDateTimeText = "장비 UTC: " + FormatDateTime(utcDateTime);

                WebAgentLocalDateTimeText = "장비 로컬 변환: " + FormatDateTime(utcDateTime.ToLocalTime());

                SystemDateTimeStatusText = success ? operationText + " Success" : "Failed: " + statusText;
            });

            ConsoleLogHelper.State("SYSTEM TIME RX", "VERSION=" + version + " / OP=" + operationText + " / STATUS=0x" + status.ToString("X2") + "(" + statusText + ")" + " / UTC=" + FormatDateTime(utcDateTime) + " / LOCAL=" + FormatDateTime(utcDateTime.ToLocalTime()));

            // 2026-10-06: 시간 설정 성공 직후 실제 적용값을 단건 재조회한다.
            if (success && operation == 0x01)
            {
                RunOnUiThread(_context.RefreshSystemDateTimeAfterSetCallback);
            }
        }

        internal void StartSystemTimePollingAfterConnection()
        {
            if (!_context.IsSelectedControlAgentWebAgent)
            {
                return;
            }

            _systemTimePollTimer?.Start();

            _systemTimeQueryInFlight = false;
            _manualSystemTimeQueryPending = false;
            _systemTimeLastAutomaticQueryAt = TelemetryClock.MonotonicMilliseconds;

            ConsoleLogHelper.State("SYSTEM TIME AUTO POLL", "Connected / initial query");

            RequestSystemDateTimeAutomatically();
        }

        internal void UpdateSystemTimePollingState()
        {
            if (_systemTimePollTimer == null)
            {
                return;
            }

            if (_context._laTcpService != null && _context._laTcpService.IsConnected && _context.IsSelectedControlAgentWebAgent)
            {
                _systemTimePollTimer.Start();
            }
            else
            {
                StopSystemTimePolling("profile/tab state changed");
            }
        }

        internal void StopSystemTimePolling(string reason)
        {
            if (_systemTimePollTimer?.IsEnabled == true)
            {
                _systemTimePollTimer.Stop();

                ConsoleLogHelper.State("SYSTEM TIME AUTO POLL", "Stopped / " + reason);
            }

            _systemTimeQueryInFlight = false;
            _manualSystemTimeQueryPending = false;
        }

        internal void SystemTimePollTimer_Tick(object sender, EventArgs e)
        {
            UpdatePcDateTimePreview();

            if (_manualSystemTimeQueryPending && TelemetryClock.MonotonicMilliseconds - _systemTimeQueryMonotonicSentAt >= 2000)
            {
                _manualSystemTimeQueryPending = false;
                _systemTimeQueryInFlight = false;
                SystemDateTimeStatusText = "Timeout: 장비 시간 응답 없음";

                ConsoleLogHelper.Warning("SYSTEM TIME QUERY", "Manual query timeout after 2 seconds; automatic polling remains available");

                return;
            }

            // XAML 기본 순서: CTRL=0, GPS=1, IMU/RPY=2, SYSTEM TIME=3.
            // 다른 탭을 열어도 읽기 전용 장비 시각 기준을 유지한다.
            // 장비 시각 설정은 자동 송신하지 않아 공유 SBC 시각을 덮어쓰지 않는다.
            double nowMonotonic = TelemetryClock.MonotonicMilliseconds;

            double interval = _context.SelectedCommunicationSettingsTabIndex == 3 ? 1000 : 5000;

            if (nowMonotonic - _systemTimeLastAutomaticQueryAt < interval)
            {
                return;
            }

            if (_systemTimeQueryInFlight)
            {
                if (nowMonotonic - _systemTimeQueryMonotonicSentAt < 2000)
                {
                    return;
                }

                _systemTimeQueryInFlight = false;

                ConsoleLogHelper.Warning("SYSTEM TIME AUTO POLL", "Query timeout; retry enabled");
            }

            _systemTimeLastAutomaticQueryAt = nowMonotonic;

            RequestSystemDateTimeAutomatically();
        }

        internal void RequestSystemDateTimeAutomatically()
        {
            // 수동 조회 응답을 기다리는 동안에는 자동 요청을 끼워 넣지 않는다.
            if (_manualSystemTimeQueryPending)
            {
                return;
            }

            _isAutomaticSystemTimeQuery = true;

            try
            {
                RequestSystemDateTime();
            }
            finally
            {
                _isAutomaticSystemTimeQuery = false;
            }
        }

        internal void UpdatePcDateTimePreview()
        {
            DateTime local = DateTime.Now;

            UpdatePcDateTimePreview(local, local.ToUniversalTime());
        }

        internal void UpdatePcDateTimePreview(DateTime local, DateTime utc)
        {
            PcLocalDateTimeText = "PC 로컬: " + FormatDateTime(local);

            PcUtcDateTimeText = "PC UTC: " + FormatDateTime(utc);
        }

        internal void SetSystemDateTimeFailure(string message)
        {
            RunOnUiThread(() => SystemDateTimeStatusText = message);

            ConsoleLogHelper.Warning("SYSTEM TIME RX", message);
        }

        internal static string GetSystemDateTimeStatusText(byte status)
        {
            switch (status)
            {
                case 0x00:
                    return "SUCCESS";
                case 0x01:
                    return "INVALID DATE OR RESERVED";
                case 0x02:
                    return "OS SET FAILED OR PERMISSION DENIED";
                default:
                    return "UNKNOWN STATUS 0x" + status.ToString("X2");
            }
        }

        internal static string FormatDateTime(DateTime value)
        {
            return value.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
        }

        internal static void VerifySystemDateTimePacketEncoding()
        {
            DateTime sample = new DateTime(2026, 10, 1, 3, 4, 5, DateTimeKind.Utc);

            string actual = ToHex(ControlCommandService.BuildSystemDateTimePacket(sample));

            const string Expected = "FF 01 00 EF EA 07 0A 01 03 04 05 F8";

            if (actual != Expected)
            {
                ConsoleLogHelper.Error("SYSTEM TIME PACKET SELF TEST", "EXPECTED=" + Expected + " / ACTUAL=" + actual);
            }
        }

        #endregion

    }
}
