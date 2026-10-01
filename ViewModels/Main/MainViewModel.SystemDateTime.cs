using OpenCvWpfTracking.Common;
using OpenCvWpfTracking.Services.Communication;
using System;
using System.Globalization;
using System.Windows.Input;

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

        public ICommand RequestSystemDateTimeCommand { get; private set; }

        public ICommand SetSystemDateTimeFromPcCommand { get; private set; }

        public string WebAgentUtcDateTimeText
        {
            get => _webAgentUtcDateTimeText;
            private set
            {
                if (_webAgentUtcDateTimeText == value) return;
                _webAgentUtcDateTimeText = value;
                OnPropertyChanged();
            }
        }

        public string WebAgentLocalDateTimeText
        {
            get => _webAgentLocalDateTimeText;
            private set
            {
                if (_webAgentLocalDateTimeText == value) return;
                _webAgentLocalDateTimeText = value;
                OnPropertyChanged();
            }
        }

        public string PcLocalDateTimeText
        {
            get => _pcLocalDateTimeText;
            private set
            {
                if (_pcLocalDateTimeText == value) return;
                _pcLocalDateTimeText = value;
                OnPropertyChanged();
            }
        }

        public string PcUtcDateTimeText
        {
            get => _pcUtcDateTimeText;
            private set
            {
                if (_pcUtcDateTimeText == value) return;
                _pcUtcDateTimeText = value;
                OnPropertyChanged();
            }
        }

        public string SystemDateTimeStatusText
        {
            get => _systemDateTimeStatusText;
            private set
            {
                if (_systemDateTimeStatusText == value) return;
                _systemDateTimeStatusText = value;
                OnPropertyChanged();
            }
        }

        private void InitializeSystemDateTimeSettings()
        {
            RequestSystemDateTimeCommand = new RelayCommand(RequestSystemDateTime);
            SetSystemDateTimeFromPcCommand = new RelayCommand(SetSystemDateTimeFromPc);
            UpdatePcDateTimePreview();
            VerifySystemDateTimePacketEncoding();
        }

        private void RequestSystemDateTime()
        {
            if (!CanUseSystemDateTime(out string reason))
            {
                SystemDateTimeStatusText = reason;
                ConsoleLogHelper.Warning("SYSTEM TIME QUERY", reason);
                return;
            }

            UpdatePcDateTimePreview();
            SystemDateTimeStatusText = "조회 응답 대기";
            bool sent = _controlCommandService.RequestSystemDateTime();

            ConsoleLogHelper.Command(
                "SYSTEM TIME QUERY TX",
                "CMD2=0xF1 / PACKET=FF 01 00 F1 00 00 F2 / SENT=" + sent);

            if (!sent)
            {
                SystemDateTimeStatusText = "Failed: 조회 명령 전송 실패";
            }
        }

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
                "PC_LOCAL=" + FormatDateTime(pcLocal) +
                " / UTC=" + FormatDateTime(pcUtc) +
                " / PACKET=" + ToHex(packet) +
                " / SENT=" + sent);

            if (!sent)
            {
                SystemDateTimeStatusText = "Failed: 설정 명령 전송 실패";
            }
        }

        private bool CanUseSystemDateTime(out string reason)
        {
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

        private void ParseWebAgentSystemDateTime(byte[] payload)
        {
            if (payload == null || payload.Length < 10)
            {
                SetSystemDateTimeFailure("Failed: 0x2D 응답 길이 오류");
                return;
            }

            byte version = payload[0];
            byte operation = payload[1];
            byte status = payload[2];
            int year = payload[3] | payload[4] << 8;
            string operationText = operation == 0x00 ? "QUERY" :
                operation == 0x01 ? "SET" : "UNKNOWN";

            DateTime utcDateTime;
            try
            {
                utcDateTime = new DateTime(
                    year,
                    payload[5],
                    payload[6],
                    payload[7],
                    payload[8],
                    payload[9],
                    DateTimeKind.Utc);
            }
            catch (ArgumentOutOfRangeException ex)
            {
                SetSystemDateTimeFailure("Failed: 0x2D 날짜 범위 오류");
                ConsoleLogHelper.Warning("SYSTEM TIME RX", ex.Message);
                return;
            }

            string statusText = GetSystemDateTimeStatusText(status);
            bool success = version == 0x01 &&
                           (operation == 0x00 || operation == 0x01) &&
                           status == 0x00;

            RunOnUiThread(() =>
            {
                WebAgentUtcDateTimeText =
                    "장비 UTC: " + FormatDateTime(utcDateTime);
                WebAgentLocalDateTimeText =
                    "장비 로컬 변환: " + FormatDateTime(utcDateTime.ToLocalTime());
                SystemDateTimeStatusText = success
                    ? operationText + " Success"
                    : "Failed: " + statusText;
            });

            ConsoleLogHelper.State(
                "SYSTEM TIME RX",
                "VERSION=" + version +
                " / OP=" + operationText +
                " / STATUS=0x" + status.ToString("X2") +
                "(" + statusText + ")" +
                " / UTC=" + FormatDateTime(utcDateTime) +
                " / LOCAL=" + FormatDateTime(utcDateTime.ToLocalTime()));
        }

        private void UpdatePcDateTimePreview()
        {
            DateTime local = DateTime.Now;
            UpdatePcDateTimePreview(local, local.ToUniversalTime());
        }

        private void UpdatePcDateTimePreview(DateTime local, DateTime utc)
        {
            PcLocalDateTimeText = "PC 로컬: " + FormatDateTime(local);
            PcUtcDateTimeText = "PC UTC: " + FormatDateTime(utc);
        }

        private void SetSystemDateTimeFailure(string message)
        {
            RunOnUiThread(() => SystemDateTimeStatusText = message);
            ConsoleLogHelper.Warning("SYSTEM TIME RX", message);
        }

        private static string GetSystemDateTimeStatusText(byte status)
        {
            switch (status)
            {
                case 0x00: return "SUCCESS";
                case 0x01: return "INVALID DATE OR RESERVED";
                case 0x02: return "OS SET FAILED OR PERMISSION DENIED";
                default: return "UNKNOWN STATUS 0x" + status.ToString("X2");
            }
        }

        private static string FormatDateTime(DateTime value)
        {
            return value.ToString(
                "yyyy-MM-dd HH:mm:ss",
                CultureInfo.InvariantCulture);
        }

        private static void VerifySystemDateTimePacketEncoding()
        {
            DateTime sample = new DateTime(
                2026,
                10,
                1,
                3,
                4,
                5,
                DateTimeKind.Utc);
            string actual = ToHex(ControlCommandService.BuildSystemDateTimePacket(sample));
            const string Expected = "FF 01 00 EF EA 07 0A 01 03 04 05 F8";

            if (actual != Expected)
            {
                ConsoleLogHelper.Error(
                    "SYSTEM TIME PACKET SELF TEST",
                    "EXPECTED=" + Expected + " / ACTUAL=" + actual);
            }
        }
    }
}
