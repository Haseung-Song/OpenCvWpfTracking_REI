using System;
using System.Globalization;
using System.Windows;
using System.Windows.Input;
using OpenCvWpfTracking.Common;
using OpenCvWpfTracking.Services.Communication.WebAgent;

namespace OpenCvWpfTracking.ViewModels.Main
{
    /// <summary>장비 특수 명령과 온도에 필요한 협력 기능만 받는 경계입니다. 화면 객체의 내부 구현을 알지 않습니다.</summary>
    internal interface IWebAgentEquipmentFeatureContext
    {
        string _connectionSessionId { get; set; }

        Services.Communication.IControlCommandServicePort _controlCommandService { get; }

        byte _currentScbPowerStatus { get; set; }

        bool _hasDevicePowerStatus { get; set; }

        bool _isDevicePowerStatusStale { get; set; }

        IWebAgentThermalPaletteServicePort _webAgentThermalPaletteService { get; }

        bool CameraFeatureTransportReady { get; }

        void DispatchSpecialFeaturePacket(byte[] p, Action<byte[]> apply);

        void EnsureSpecialFeatures();

        bool IsControlInputLocked { get; }

        bool IsOperationCommandEnabled { get; }

        bool IsSelectedConnectionTarget { get; }

        bool IsSelectedControlAgentWebAgent { get; }

        void OnPropertyChanged([global::System.Runtime.CompilerServices.CallerMemberName] string propertyName = null);

        void ResetThermalPaletteButtonVisuals();

        System.Collections.Generic.IEnumerable<Models.Main.CameraSpecialFeatureRow> SpecialFeatureRows { get; }

        bool TryReadTelemetryTime(ulong milliseconds, out DateTimeOffset value, bool requireFresh = true);

        Action QueryEquipmentTemperatureCallback { get; }
    }

    /// <summary>장비 특수 명령과 온도 담당 객체입니다. 상태를 보관하고 입력을 받아 결과를 알립니다.</summary>
    internal sealed class WebAgentEquipmentFeature
    {

        #region [협력 객체와 초기화]

        private readonly IWebAgentEquipmentFeatureContext _context;

        internal WebAgentEquipmentFeature(IWebAgentEquipmentFeatureContext context)
        {
            _context = context ?? throw new System.ArgumentNullException(nameof(context));
        }

        internal void InitializeState()
        {
            _temperatureText = "PT —   EO —   IR —   SWIR —";
            _wiperSecondsText = "8";
            _wiperRequestText = "와이퍼 요청 대기";
        }

        #endregion

        #region [상태 필드]

        internal TemperatureSnapshot _temperatureSnapshot;

        internal string _temperatureText;

        internal string _wiperSecondsText;

        internal string _wiperRequestText;

        internal string _lastWiperLogSignature;

        internal ICommand _wiperTimedCommand;

        internal ICommand _wiperContinuousCommand;

        internal ICommand _wiperStopCommand;

        internal ICommand _temperatureQueryCommand;

        #endregion

        #region [화면 속성과 이벤트]

        internal string ThermalColorPaletteButtonText
        {
            get
            {
                if (_context.IsSelectedControlAgentWebAgent)
                {
                    return "RAINBOW";
                }
                else
                {
                    return "RANDOM";
                }
            }
        }

        internal string EquipmentTemperatureText
        {
            get { return _temperatureText; }
        }

        internal string WiperRequestText
        {
            get { return _wiperRequestText; }
        }

        internal string WiperSecondsText
        {
            get { return _wiperSecondsText; }

            set
            {
                _wiperSecondsText = value;

                _context.OnPropertyChanged();
            }
        }

        internal bool IsWebAgentEquipmentControlReady
        {
            get
            {
                return _context.IsSelectedControlAgentWebAgent && _context.CameraFeatureTransportReady && _context.IsSelectedConnectionTarget;
            }
        }

        internal string WiperReportedText
        {
            get
            {
                if (IsWebAgentEquipmentControlReady && _context._hasDevicePowerStatus && !_context._isDevicePowerStatusStale)
                {
                    return ((_context._currentScbPowerStatus & 0x20) != 0 ? "장비 와이퍼 ON" : "장비 와이퍼 OFF");
                }
                else
                {
                    return "장비 와이퍼 미확인";
                }
            }
        }

        internal ICommand WiperTimedCommand
        {
            get
            {
                return _wiperTimedCommand ?? (_wiperTimedCommand = new RelayCommand(() => SendWebWiper(1), () => IsWebAgentEquipmentControlReady && _context.IsOperationCommandEnabled && !_context.IsControlInputLocked));
            }
        }

        internal ICommand WiperContinuousCommand
        {
            get
            {
                return _wiperContinuousCommand ?? (_wiperContinuousCommand = new RelayCommand(() => SendWebWiper(4), () => IsWebAgentEquipmentControlReady && _context.IsOperationCommandEnabled && !_context.IsControlInputLocked));
            }
        }

        internal ICommand WiperStopCommand
        {
            get
            {
                return _wiperStopCommand ?? (_wiperStopCommand = new RelayCommand(() => SendWebWiper(2), () => IsWebAgentEquipmentControlReady));
            }
        }

        internal ICommand TemperatureQueryCommand
        {
            get
            {
                return _temperatureQueryCommand ?? (_temperatureQueryCommand = new RelayCommand(_context.QueryEquipmentTemperatureCallback, () => IsWebAgentEquipmentControlReady));
            }
        }

        #endregion

        #region [동작 처리]

        internal void SendWebWiper(int mode)
        {
            if (!IsWebAgentEquipmentControlReady || (mode != 2 && (!_context.IsOperationCommandEnabled || _context.IsControlInputLocked)))
            {
                return;
            }

            int seconds = 0;

            if (mode == 1 && !int.TryParse(WiperSecondsText, NumberStyles.Integer, CultureInfo.InvariantCulture, out seconds))
            {
                seconds = -1;
            }

            if (!WebAgentEquipmentProtocol.IsWiperRequestValid(mode, seconds))
            {
                _wiperRequestText = "구동 시간은 1~255초로 입력하세요.";

                _context.OnPropertyChanged(nameof(WiperRequestText));

                return;
            }

            bool sent = _context._controlCommandService.SendCommand(0x00, 0x3F, (byte)mode, (byte)seconds);

            string action = mode == 1 ? seconds + "초 구동" : mode == 2 ? "정지" : "연속 구동";

            _wiperRequestText = action + (sent ? " 요청 전송" : " 요청 실패");

            _context.OnPropertyChanged(nameof(WiperRequestText));

            ConsoleLogHelper.Command("WEB 와이퍼", _wiperRequestText + Environment.NewLine + "CMD2=0x3F / MODE=" + mode + " / SECONDS=" + seconds + " / SESSION=" + _context._connectionSessionId);
        }

        internal void QueryEquipmentTemperature()
        {
            if (!IsWebAgentEquipmentControlReady)
            {
                return;
            }

            bool sent = _context._controlCommandService.SendCommand(0x00, 0xFB, 0x00, 0x00);

            ConsoleLogHelper.Command("WEB 온도", sent ? "최신 온도 캐시 조회 전송" : "온도 조회 전송 실패");
        }

        internal void ParseEquipmentTemperature(byte[] payload)
        {
            TemperatureSnapshot value;

            if (!WebAgentEquipmentProtocol.TryTemperature(payload, out value))
            {
                ConsoleLogHelper.Warning("WEB 온도", "온도 응답의 길이·스키마·유효성 오류로 적용하지 않았습니다.");

                return;
            }

            string session = _context._connectionSessionId;

            Application.Current?.Dispatcher.BeginInvoke(new Action(() =>
            {
                if (session != _context._connectionSessionId || !IsWebAgentEquipmentControlReady)
                {
                    return;
                }

                _temperatureSnapshot = value;

                RefreshWebAgentEquipmentState();
            }));
        }

        internal void ResetWebAgentEquipmentState()
        {
            _context._webAgentThermalPaletteService?.ResetRelativeSelection();

            _temperatureSnapshot = null;
            _wiperRequestText = "와이퍼 요청 대기";
            _lastWiperLogSignature = null;

            RefreshWebAgentEquipmentState();

            _context.OnPropertyChanged(nameof(WiperRequestText));
        }

        internal void RefreshWebAgentEquipmentState()
        {
            string text = "PT —   EO —   IR —   SWIR —";

            var snapshot = _temperatureSnapshot;

            if (IsWebAgentEquipmentControlReady && snapshot != null && snapshot.SampleTime != 0)
            {
                DateTimeOffset sampled;

                bool clockReady = _context.TryReadTelemetryTime(snapshot.SampleTime, out sampled, false);

                bool stale = clockReady && (DateTimeOffset.UtcNow - sampled).TotalSeconds >= 15;

                var labels = new[]
                {
                    "PT",
                    "EO",
                    "IR",
                    "SWIR"
                };

                var values = new string[4];

                for (int index = 0; index < values.Length; index++)
                {
                    values[index] = labels[index] + " " + (snapshot.Value(index).HasValue ? snapshot.Value(index).Value.ToString("0.00", CultureInfo.InvariantCulture) + "℃" : "—");
                }

                text = string.Join("   ", values) + (!clockReady ? " (시각 미확인)" : stale ? " (과거값)" : "");
            }

            if (_temperatureText != text)
            {
                _temperatureText = text;

                _context.OnPropertyChanged(nameof(EquipmentTemperatureText));

                ConsoleLogHelper.State("WEB 온도", text);
            }

            string wiperState = WiperReportedText;

            if (_lastWiperLogSignature != wiperState)
            {
                _lastWiperLogSignature = wiperState;

                _context.OnPropertyChanged(nameof(WiperReportedText));
            }
        }

        internal void HandleWebAgentEquipmentUnavailable(byte[] payload)
        {
            if (payload == null || payload.Length != 6 || payload[0] != 1 || payload[1] > 1)
            {
                return;
            }

            if (payload[2] == 0 && payload[3] == 0x3F)
            {
                _context.DispatchSpecialFeaturePacket(payload, p =>
                {
                    _wiperRequestText = p[1] == 0 ? "와이퍼 미지원" : "와이퍼 실행 불가";

                    _context.OnPropertyChanged(nameof(WiperRequestText));
                });
            }

            if (payload[2] == 0 && payload[3] == 0x31 && (payload[4] == 0xF3 || payload[4] == 0xF4 || payload[4] == 0xF5 || payload[4] == 0x0D || payload[4] == 0x0E))
            {
                _context.DispatchSpecialFeaturePacket(payload, p =>
                {
                    _context.ResetThermalPaletteButtonVisuals();

                    _context.EnsureSpecialFeatures();

                    foreach (var row in _context.SpecialFeatureRows)
                    {
                        if (row.IsSemanticPalette)
                        {
                            row.Warn(p[1] == 0 ? "선택 팔레트 미지원 · 다른 색상은 영상 확인" : "팔레트 실행 불가 · 조건 확인");
                        }
                    }
                });
            }
        }

        #endregion

    }
}
