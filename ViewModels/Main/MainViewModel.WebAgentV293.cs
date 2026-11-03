using System;
using System.Globalization;
using System.Windows;
using System.Windows.Input;
using OpenCvWpfTracking.Common;
using OpenCvWpfTracking.Services.Communication.WebAgent;

namespace OpenCvWpfTracking.ViewModels.Main
{
    public partial class MainViewModel
    {
        private TemperatureSnapshot _temperatureSnapshot;

        private string _temperatureText = "PT —   EO —   IR —   SWIR —";

        private string _wiperSecondsText = "8";

        private string _wiperRequestText = "와이퍼 요청 대기";

        private string _lastWiperLogSignature;

        private ICommand _wiperTimedCommand, _wiperContinuousCommand, _wiperStopCommand, _temperatureQueryCommand;

        public string ThermalColorPaletteButtonText => IsSelectedControlAgentWebAgent ? "RAINBOW" : "RANDOM";

        public string EquipmentTemperatureText => _temperatureText;

        public string WiperRequestText => _wiperRequestText;

        public string WiperSecondsText
        {
            get => _wiperSecondsText;
            set
            {
                _wiperSecondsText = value;
                OnPropertyChanged();
            }
        }

        private bool IsV293ControlReady => IsSelectedControlAgentWebAgent &&
            CameraFeatureTransportReady && IsSelectedConnectionTarget;

        public string WiperReportedText => IsV293ControlReady && _hasDevicePowerStatus && !_isDevicePowerStatusStale ?
            ((_currentScbPowerStatus & 0x20) != 0 ? "장비 와이퍼 ON" : "장비 와이퍼 OFF") : "장비 와이퍼 미확인";

        public ICommand WiperTimedCommand => _wiperTimedCommand ?? (_wiperTimedCommand =
            new RelayCommand(() => SendWebWiper(1), () => IsV293ControlReady && IsOperationCommandEnabled && !IsControlInputLocked));

        public ICommand WiperContinuousCommand => _wiperContinuousCommand ?? (_wiperContinuousCommand =
            new RelayCommand(() => SendWebWiper(4), () => IsV293ControlReady && IsOperationCommandEnabled && !IsControlInputLocked));

        // 정지 요청은 일반 운용 잠금 중에도 현재 연결 장비로 전송할 수 있다.
        public ICommand WiperStopCommand => _wiperStopCommand ?? (_wiperStopCommand =
            new RelayCommand(() => SendWebWiper(2), () => IsV293ControlReady));

        public ICommand TemperatureQueryCommand => _temperatureQueryCommand ?? (_temperatureQueryCommand =
            new RelayCommand(QueryEquipmentTemperature, () => IsV293ControlReady));

        /// <summary>GUI 타이머로 중복 정지하지 않고 구동 시간 관리를 WebAgent에 맡긴다.</summary>
        private void SendWebWiper(int mode)
        {
            if (!IsV293ControlReady || (mode != 2 && (!IsOperationCommandEnabled || IsControlInputLocked)))
                return;

            int seconds = 0;
            if (mode == 1 && !int.TryParse(WiperSecondsText, NumberStyles.Integer, CultureInfo.InvariantCulture, out seconds))
                seconds = -1;

            if (!WebAgentV293Protocol.IsWiperRequestValid(mode, seconds))
            {
                _wiperRequestText = "구동 시간은 1~255초로 입력하세요.";
                OnPropertyChanged(nameof(WiperRequestText));
                return;
            }

            bool sent = _controlCommandService.SendCommand(0x00, 0x3F, (byte)mode, (byte)seconds);
            string action = mode == 1 ? seconds + "초 구동" : mode == 2 ? "정지" : "연속 구동";
            _wiperRequestText = action + (sent ? " 요청 전송" : " 요청 실패");
            OnPropertyChanged(nameof(WiperRequestText));
            ConsoleLogHelper.Command("WEB 와이퍼", _wiperRequestText + Environment.NewLine +
                "CMD2=0x3F / MODE=" + mode + " / SECONDS=" + seconds + " / SESSION=" + _connectionSessionId);
        }

        /// <summary>최신 온도 캐시 조회는 새 센서 측정이나 장비 시간 설정이 아니다.</summary>
        private void QueryEquipmentTemperature()
        {
            if (!IsV293ControlReady)
                return;

            bool sent = _controlCommandService.SendCommand(0x00, 0xFB, 0x00, 0x00);
            ConsoleLogHelper.Command("WEB 온도", sent ? "최신 온도 캐시 조회 전송" : "온도 조회 전송 실패");
        }

        /// <summary>파서가 검증한 표본을 현재 연결의 UI에만 반영한다.</summary>
        private void ParseEquipmentTemperature(byte[] payload)
        {
            TemperatureSnapshot value;
            if (!WebAgentV293Protocol.TryTemperature(payload, out value))
            {
                ConsoleLogHelper.Warning("WEB 온도", "온도 응답의 길이·스키마·유효성 오류로 적용하지 않았습니다.");
                return;
            }

            string session = _connectionSessionId;
            Application.Current?.Dispatcher.BeginInvoke(new Action(() =>
            {
                if (session != _connectionSessionId || !IsV293ControlReady)
                    return;

                _temperatureSnapshot = value;
                RefreshWebAgentV293();
            }));
        }

        /// <summary>장비 변경·재연결 시 이전 온도와 요청 상태를 남기지 않는다.</summary>
        private void ResetWebAgentV293()
        {
            _webAgentThermalPaletteService?.ResetRelativeSelection();
            _temperatureSnapshot = null;
            _wiperRequestText = "와이퍼 요청 대기";
            _lastWiperLogSignature = null;
            RefreshWebAgentV293();
            OnPropertyChanged(nameof(WiperRequestText));
        }

        /// <summary>원래 표본 시각으로 경과 시간을 계산하여 캐시를 최신값으로 위장하지 않는다.</summary>
        private void RefreshWebAgentV293()
        {
            string text = "PT —   EO —   IR —   SWIR —";
            var snapshot = _temperatureSnapshot;
            if (IsV293ControlReady && snapshot != null && snapshot.SampleTime != 0)
            {
                DateTimeOffset sampled;
                bool clockReady = TryReadTelemetryTime(snapshot.SampleTime, out sampled, false);
                bool stale = clockReady && (DateTimeOffset.UtcNow - sampled).TotalSeconds >= 15;
                var labels = new[] { "PT", "EO", "IR", "SWIR" };
                var values = new string[4];

                for (int index = 0; index < values.Length; index++)
                    values[index] = labels[index] + " " + (snapshot.Value(index).HasValue ?
                        snapshot.Value(index).Value.ToString("0.00", CultureInfo.InvariantCulture) + "℃" : "—");

                text = string.Join("   ", values) + (!clockReady ? " (시각 미확인)" : stale ? " (과거값)" : "");
            }

            if (_temperatureText != text)
            {
                _temperatureText = text;
                OnPropertyChanged(nameof(EquipmentTemperatureText));
                ConsoleLogHelper.State("WEB 온도", text);
            }

            string wiperState = WiperReportedText;
            if (_lastWiperLogSignature != wiperState)
            {
                _lastWiperLogSignature = wiperState;
                OnPropertyChanged(nameof(WiperReportedText));
            }
        }

        /// <summary>실패 응답의 원 명령을 확인해 해당 기능의 요청 상태만 갱신한다.</summary>
        private void HandleWebAgentV293Unavailable(byte[] payload)
        {
            if (payload == null || payload.Length != 6 || payload[0] != 1 || payload[1] > 1)
                return;

            if (payload[2] == 0 && payload[3] == 0x3F)
            {
                DispatchSpecialFeaturePacket(payload, p =>
                {
                    _wiperRequestText = p[1] == 0 ? "와이퍼 미지원" : "와이퍼 실행 불가";
                    OnPropertyChanged(nameof(WiperRequestText));
                });
            }

            if (payload[2] == 0 && payload[3] == 0x31 &&
                (payload[4] == 0xF3 || payload[4] == 0xF4 || payload[4] == 0xF5 || payload[4] == 0x0D || payload[4] == 0x0E))
            {
                DispatchSpecialFeaturePacket(payload, p =>
                {
                    ResetThermalPaletteButtonVisuals();
                    EnsureSpecialFeatures();
                    foreach (var row in SpecialFeatureRows)
                        if (row.IsSemanticPalette)
                            row.Warn(p[1] == 0 ? "선택 팔레트 미지원 · 다른 색상은 영상 확인" : "팔레트 실행 불가 · 조건 확인");
                });
            }
        }
    }
}
