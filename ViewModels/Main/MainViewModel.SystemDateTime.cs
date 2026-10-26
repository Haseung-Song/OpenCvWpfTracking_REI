using OpenCvWpfTracking.Common;
using OpenCvWpfTracking.Services.Communication;
using System;
using System.Globalization;
using System.Windows.Input;
using System.Windows.Threading;

namespace OpenCvWpfTracking.ViewModels.Main
{
    /// <summary>
    /// 2026-10-01: RTC 유지가 불가능한 WebAgent SBC를 위해 시스템 UTC 시간을
    /// 조회하고 PC 현재 시간을 UTC로 변환해 설정하는 GUI 기능을 제공한다.
    /// </summary>
    public partial class MainViewModel
    {
        private string _webAgentUtcDateTimeText = "장비 UTC: 조회 대기";

        private string _webAgentLocalDateTimeText = "장비 로컬 변환: 조회 대기";

        private string _pcLocalDateTimeText = "PC 로컬: 확인 대기";

        private string _pcUtcDateTimeText = "PC UTC: 확인 대기";

        private string _systemDateTimeStatusText = "Ready";

        private DispatcherTimer _systemTimePollTimer;

        private volatile bool _systemTimeQueryInFlight;

        private DateTime _systemTimeQuerySentAt;

        private bool _isAutomaticSystemTimeQuery;

        private bool _manualSystemTimeQueryPending;

        private double _systemTimeQueryMonotonicSentAt;

        private double _systemTimeLastAutomaticQueryAt;

        public ICommand RequestSystemDateTimeCommand { get; private set; }

        public ICommand SetSystemDateTimeFromPcCommand { get; private set; }

        public string WebAgentUtcDateTimeText
        {
            get => _webAgentUtcDateTimeText;
            private set
            {
                if (_webAgentUtcDateTimeText == value)
                    return;

                _webAgentUtcDateTimeText = value;
                OnPropertyChanged();
            }
        }

        public string WebAgentLocalDateTimeText
        {
            get => _webAgentLocalDateTimeText;
            private set
            {
                if (_webAgentLocalDateTimeText == value)
                    return;

                _webAgentLocalDateTimeText = value;
                OnPropertyChanged();
            }
        }

        public string PcLocalDateTimeText
        {
            get => _pcLocalDateTimeText;
            private set
            {
                if (_pcLocalDateTimeText == value)
                    return;

                _pcLocalDateTimeText = value;
                OnPropertyChanged();
            }
        }

        public string PcUtcDateTimeText
        {
            get => _pcUtcDateTimeText;
            private set
            {
                if (_pcUtcDateTimeText == value)
                    return;

                _pcUtcDateTimeText = value;
                OnPropertyChanged();
            }
        }

        public string SystemDateTimeStatusText
        {
            get => _systemDateTimeStatusText;
            private set
            {
                if (_systemDateTimeStatusText == value)
                    return;

                _systemDateTimeStatusText = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// 장비 시각 조회·설정 명령과 자동 조회 타이머를 준비한다.
        /// </summary>
        private void InitializeSystemDateTimeSettings()
        {
            RequestSystemDateTimeCommand = new RelayCommand(RequestSystemDateTime);
            SetSystemDateTimeFromPcCommand = new RelayCommand(SetSystemDateTimeFromPc);
            _systemTimePollTimer = new DispatcherTimer(DispatcherPriority.Background)
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _systemTimePollTimer.Tick += SystemTimePollTimer_Tick;
            UpdatePcDateTimePreview();
            VerifySystemDateTimePacketEncoding();
        }

        /// <summary>
        /// 중복 조회를 방지하며 장비 UTC 시각을 요청하고 왕복 지연 측정 시점을 기록한다.
        /// </summary>
        private void RequestSystemDateTime()
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
                    _manualSystemTimeQueryPending = true;

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
            bool sent = _controlCommandService.RequestSystemDateTime();

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

        /// <summary>
        /// 운용자의 명시적 요청으로만 PC UTC 시각을 장비에 설정한다.
        /// </summary>
        private void SetSystemDateTimeFromPc()
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
            bool sent = _controlCommandService.SetSystemDateTimeUtc(pcUtc);
            ConsoleLogHelper.Command(
                "SYSTEM TIME SET TX",
                "PC_LOCAL=" + FormatDateTime(pcLocal) + " / UTC=" + FormatDateTime(pcUtc) + " / PACKET=" + ToHex(packet) + " / SENT=" + sent
            );

            if (!sent)
            {
                SystemDateTimeStatusText = "Failed: 설정 명령 전송 실패";
            }
        }

        /// <summary>
        /// 현재 연결과 선택 프로필이 장비 시각 명령을 사용할 수 있는 상태인지 확인한다.
        /// </summary>
        private bool CanUseSystemDateTime(out string reason)
        {
            // 2026-10-06: 파노라마/AUTO SCAN/HOME/ZERO 수행 중에는
            // 조회와 설정 모두 막아 장비 명령 흐름을 단일화한다.
            if (!IsOperationCommandEnabled)
            {
                reason = "운용 작업 중 SYSTEM TIME 제어 잠금";
                return false;
            }

            if (!IsEnvironmentStatusSelected)
            {
                reason = "WebAgent 전용 기능";
                return false;
            }

            if (_laTcpService == null || !_laTcpService.IsConnected)
            {
                reason = "Failed: WebAgent 미연결";
                return false;
            }

            reason = string.Empty;
            return true;
        }

        /// <summary>
        /// 장비 UTC 응답을 읽어 화면과 상태 유효성 판정용 시각 기준을 갱신한다.
        /// </summary>
        private void ParseWebAgentSystemDateTime(byte[] payload)
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

                if (_webAgentTelemetryClock.Observe(
                    new DateTimeOffset(utcDateTime).ToUnixTimeMilliseconds(),
                    querySentAt,
                    receivedAt,
                    out clockChanged
                ) &&
                    clockChanged)
                {
                    ConsoleLogHelper.State(
                        "WEB AGENT CLOCK",
                        "F1 clock reference established/reset; no EF time write / PC_MINUS_SBC_SECONDS=" + (DateTime.UtcNow - utcDateTime).TotalSeconds.ToString(
                        "F1",
                        CultureInfo.InvariantCulture
                    )
                    );
                }
            }

            if (success && operation == 0x01)
                _webAgentTelemetryClock.Reset();

            RunOnUiThread(() =>
            {
                WebAgentUtcDateTimeText = "장비 UTC: " + FormatDateTime(utcDateTime);
                WebAgentLocalDateTimeText = "장비 로컬 변환: " + FormatDateTime(utcDateTime.ToLocalTime());
                SystemDateTimeStatusText = success ? operationText + " Success" : "Failed: " + statusText;
            });
            ConsoleLogHelper.State(
                "SYSTEM TIME RX",
                "VERSION=" + version + " / OP=" + operationText + " / STATUS=0x" + status.ToString("X2") + "(" + statusText + ")" + " / UTC=" + FormatDateTime(utcDateTime) + " / LOCAL=" + FormatDateTime(utcDateTime.ToLocalTime())
            );

            // 2026-10-06: 시간 설정 성공 직후 실제 적용값을 단건 재조회한다.
            if (success && operation == 0x01)
            {
                RunOnUiThread(RequestSystemDateTime);
            }
        }

        /// <summary>
        /// V32_5: 연결 직후와 다른 탭에서도 5초마다 읽기 전용 F1 조회.
        /// SYSTEM TIME 탭은 기존 1Hz이며 응답 대기 중에는 중복 송신하지 않는다.
        /// </summary>
        private void StartSystemTimePollingAfterConnection()
        {
            if (!IsSelectedControlAgentWebAgent)
                return;

            _systemTimePollTimer?.Start();
            _systemTimeQueryInFlight = false;
            _manualSystemTimeQueryPending = false;
            _systemTimeLastAutomaticQueryAt = TelemetryClock.MonotonicMilliseconds;
            ConsoleLogHelper.State("SYSTEM TIME AUTO POLL", "Connected / initial query");
            RequestSystemDateTimeAutomatically();
        }

        /// <summary>
        /// WEB AGENT 연결 상태에 맞춰 읽기 전용 시각 자동 조회를 시작하거나 중지한다.
        /// </summary>
        private void UpdateSystemTimePollingState()
        {
            if (_systemTimePollTimer == null)
                return;

            if (_laTcpService != null && _laTcpService.IsConnected && IsSelectedControlAgentWebAgent)
            {
                _systemTimePollTimer.Start();
            }
            else
            {
                StopSystemTimePolling("profile/tab state changed");
            }
        }

        /// <summary>
        /// 시각 자동 조회를 멈추고 진행 중 조회 표시를 초기화한다.
        /// </summary>
        private void StopSystemTimePolling(string reason)
        {
            if (_systemTimePollTimer?.IsEnabled == true)
            {
                _systemTimePollTimer.Stop();
                ConsoleLogHelper.State("SYSTEM TIME AUTO POLL", "Stopped / " + reason);
            }

            _systemTimeQueryInFlight = false;
            _manualSystemTimeQueryPending = false;
        }

        /// <summary>
        /// 조회 간격·응답 대기·시간 초과를 확인한 뒤 읽기 전용 시각 조회를 진행한다.
        /// </summary>
        private void SystemTimePollTimer_Tick(object sender, EventArgs e)
        {
            UpdatePcDateTimePreview();

            if (_manualSystemTimeQueryPending && TelemetryClock.MonotonicMilliseconds - _systemTimeQueryMonotonicSentAt >= 2000)
            {
                _manualSystemTimeQueryPending = false;
                _systemTimeQueryInFlight = false;
                SystemDateTimeStatusText = "Timeout: 장비 시간 응답 없음";
                ConsoleLogHelper.Warning(
                    "SYSTEM TIME QUERY",
                    "Manual query timeout after 2 seconds; automatic polling remains available"
                );
                return;
            }

            // XAML 기본 순서: CTRL=0, GPS=1, IMU/RPY=2, SYSTEM TIME=3.
            // 다른 탭을 열어도 읽기 전용 장비 시각 기준을 유지한다.
            // 장비 시각 설정은 자동 송신하지 않아 공유 SBC 시각을 덮어쓰지 않는다.
            double nowMonotonic = TelemetryClock.MonotonicMilliseconds;
            double interval = SelectedCommunicationSettingsTabIndex == 3 ? 1000 : 5000;

            if (nowMonotonic - _systemTimeLastAutomaticQueryAt < interval)
                return;

            if (_systemTimeQueryInFlight)
            {
                if (nowMonotonic - _systemTimeQueryMonotonicSentAt < 2000)
                    return;

                _systemTimeQueryInFlight = false;
                ConsoleLogHelper.Warning("SYSTEM TIME AUTO POLL", "Query timeout; retry enabled");
            }

            _systemTimeLastAutomaticQueryAt = nowMonotonic;
            RequestSystemDateTimeAutomatically();
        }

        /// <summary>
        /// 수동 조회 응답을 기다리는 동안 자동 요청이 끼어들지 않게 한다.
        /// </summary>
        private void RequestSystemDateTimeAutomatically()
        {
            // 수동 조회 응답을 기다리는 동안에는 자동 요청을 끼워 넣지 않는다.
            if (_manualSystemTimeQueryPending)
                return;

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

        /// <summary>
        /// PC 로컬·UTC 시각의 화면 미리보기를 갱신한다.
        /// </summary>
        private void UpdatePcDateTimePreview()
        {
            DateTime local = DateTime.Now;
            UpdatePcDateTimePreview(local, local.ToUniversalTime());
        }

        /// <summary>
        /// PC 로컬·UTC 시각의 화면 미리보기를 갱신한다.
        /// </summary>
        private void UpdatePcDateTimePreview(DateTime local, DateTime utc)
        {
            PcLocalDateTimeText = "PC 로컬: " + FormatDateTime(local);
            PcUtcDateTimeText = "PC UTC: " + FormatDateTime(utc);
        }

        /// <summary>
        /// 장비 시각 명령 실패 내용을 화면 상태에 반영한다.
        /// </summary>
        private void SetSystemDateTimeFailure(string message)
        {
            RunOnUiThread(() => SystemDateTimeStatusText = message);
            ConsoleLogHelper.Warning("SYSTEM TIME RX", message);
        }

        /// <summary>
        /// 장비 시각 처리 결과 번호를 표시 문구로 변환한다.
        /// </summary>
        private static string GetSystemDateTimeStatusText(byte status)
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

        private static string FormatDateTime(DateTime value)
        {
            return value.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// 장비 시각 명령의 필드 구성과 체크섬을 오프라인으로 확인한다.
        /// </summary>
        private static void VerifySystemDateTimePacketEncoding()
        {
            DateTime sample = new DateTime(2026, 10, 1, 3, 4, 5, DateTimeKind.Utc);
            string actual = ToHex(ControlCommandService.BuildSystemDateTimePacket(sample));
            const string Expected = "FF 01 00 EF EA 07 0A 01 03 04 05 F8";

            if (actual != Expected)
            {
                ConsoleLogHelper.Error("SYSTEM TIME PACKET SELF TEST", "EXPECTED=" + Expected + " / ACTUAL=" + actual);
            }
        }
    }
}
