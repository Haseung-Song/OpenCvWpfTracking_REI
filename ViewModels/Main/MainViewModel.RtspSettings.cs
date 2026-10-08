using OpenCvWpfTracking.Common;
using OpenCvWpfTracking.Models.Main;
using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace OpenCvWpfTracking.ViewModels.Main
{
    public sealed class ControlAgentProfileOption
    {
        public string DisplayName { get; }
        public string IpAddress { get; }
        public string Port { get; }
        public string EoPresetName { get; }
        public string IrPresetName { get; }
        public bool IsDirectInput { get; }
        public bool PositionSensors { get; }
        public ControlAgentType AgentType { get; }

        public ControlAgentProfileOption(string displayName, string ipAddress,
            string port, string eoPresetName, string irPresetName,
            bool isDirectInput = false,
            ControlAgentType agentType = ControlAgentType.WebAgent, bool positionSensors = false)
        {
            DisplayName = displayName;
            IpAddress = ipAddress;
            Port = port;
            EoPresetName = eoPresetName;
            IrPresetName = irPresetName;
            IsDirectInput = isDirectInput;
            AgentType = agentType;
            PositionSensors = positionSensors;
        }

    }

    public partial class MainViewModel
    {
        // 2026-10-01: 운용 장비 명칭은 RTSP 주소 모음 기준으로 통일한다.
        private static string RooftopMr300EoDisplayName => OpenCvWpfTracking.Services.Configuration.DeviceCatalog.Get("MR300_TEST").EoLabel;

        private static string RooftopMr300IrDisplayName => OpenCvWpfTracking.Services.Configuration.DeviceCatalog.Get("MR300_TEST").IrLabel;

        private static string RooftopMr500EoDisplayName => OpenCvWpfTracking.Services.Configuration.DeviceCatalog.Get("MR500_MOE").EoLabel;

        private static string RooftopMr500IrDisplayName => OpenCvWpfTracking.Services.Configuration.DeviceCatalog.Get("MR500_MOE").IrLabel;

        // 2026-09-16: 기동형 LR1000 장비 리스트 ver3 기준 4층 장비 설정.
        private static string Lr1000EoRtspAddress => OpenCvWpfTracking.Services.Configuration.DeviceCatalog.Get("LR1000_MOBILE").EoAddress;

        private static string Lr1000IrRtspAddress => OpenCvWpfTracking.Services.Configuration.DeviceCatalog.Get("LR1000_MOBILE").IrAddress;

        private static string Lr1000EoDisplayName => OpenCvWpfTracking.Services.Configuration.DeviceCatalog.Get("LR1000_MOBILE").EoLabel;

        private static string Lr1000IrDisplayName => OpenCvWpfTracking.Services.Configuration.DeviceCatalog.Get("LR1000_MOBILE").IrLabel;

        private static string ErWatcherEoRtspAddress => OpenCvWpfTracking.Services.Configuration.DeviceCatalog.Get("MR300_ERWATCHER").EoAddress;

        private static string ErWatcherIrRtspAddress => OpenCvWpfTracking.Services.Configuration.DeviceCatalog.Get("MR300_ERWATCHER").IrAddress;

        private static string ErWatcherEoDisplayName => OpenCvWpfTracking.Services.Configuration.DeviceCatalog.Get("MR300_ERWATCHER").EoLabel;

        private static string ErWatcherIrDisplayName => OpenCvWpfTracking.Services.Configuration.DeviceCatalog.Get("MR300_ERWATCHER").IrLabel;

        private bool _isLoadingRtspCommunicationSettings;

        private ControlAgentProfileOption _selectedControlAgentProfile;

        private int _selectedCommunicationSettingsTabIndex;
        private int _deviceConnectRequested;
        private bool _eoAlreadyConnectedNotice, _irAlreadyConnectedNotice;
        private ControlAgentProfileOption _requestedControlProfile;
        private string _requestedControlIp, _requestedControlPort, _requestedEoAddress, _requestedIrAddress;
        private string SessionControlIp => _requestedControlIp ?? ControlAgentIp;
        private string SessionControlPort => _requestedControlPort ?? ControlAgentPortText;
        private string SessionEoAddress => _requestedEoAddress ?? EoSourceAddress;
        private string SessionIrAddress => _requestedIrAddress ?? IrSourceAddress;
        private bool IsSelectedConnectionTarget =>
            (_requestedControlProfile == null || _requestedControlProfile == SelectedControlAgentProfile) &&
            SessionControlIp == ControlAgentIp && SessionControlPort == ControlAgentPortText &&
            SessionEoAddress == EoSourceAddress && SessionIrAddress == IrSourceAddress;
        private bool HasActiveDeviceSession => _isVideoConnecting || _isEoFrameDisplayed || _isIrFrameDisplayed ||
            _eoDecoder?.IsOpened==true || _irDecoder?.IsOpened==true || _laTcpService?.IsConnected==true ||
            ControlAgentConnectionStatusText=="Connected" || ControlAgentConnectionStatusText=="Connecting" ||
            (_controlAgentReconnectCts!=null && !_controlAgentReconnectCts.IsCancellationRequested) ||
            (_cts!=null && !_cts.IsCancellationRequested);
        private bool IsDeviceSettingsLocked => !_isLoadingRtspCommunicationSettings &&
            (HasActiveDeviceSession || System.Threading.Volatile.Read(ref _deviceConnectRequested)!=0);
        private void ShowAlreadyConnected()
        {
            // Overlay notice only: do not alter the real connection state or invalidate a live session.
            _eoAlreadyConnectedNotice=_isEoFrameDisplayed || _eoDecoder?.IsOpened==true;
            _irAlreadyConnectedNotice=_isIrFrameDisplayed || _irDecoder?.IsOpened==true;
            OnPropertyChanged(nameof(EoStatusText));OnPropertyChanged(nameof(IrStatusText));
            ConsoleLogHelper.State("DEVICE CONNECT","Already Connected / request blocked");
        }
        private bool BlockConnectedSettingChange(string property)
        {
            // V29_10: selection edits configure the NEXT connection, even during retries.
            // ConnectCoreAsync still rejects duplicate connections and preserves live images.
            return false;
        }

        public ObservableCollection<ControlAgentProfileOption> ControlAgentProfiles { get; } =
            new ObservableCollection<ControlAgentProfileOption>(
                OpenCvWpfTracking.Services.Configuration.DeviceCatalog.Entries.Select(e =>
                    new ControlAgentProfileOption(e.Label + " EO/IR",e.AgentIp,e.AgentPort,e.EoLabel,e.IrLabel,
                        agentType:e.AgentType,positionSensors:e.PositionSensors)).Concat(new[] {
                    new ControlAgentProfileOption("직접 입력",null,null,null,null,isDirectInput:true) }));

        public ControlAgentProfileOption SelectedControlAgentProfile
        {
            get => _selectedControlAgentProfile ?? ControlAgentProfiles.First();
            set
            {
                if (value == null || ReferenceEquals(_selectedControlAgentProfile, value))
                {
                    return;
                }

                if(BlockConnectedSettingChange(nameof(SelectedControlAgentProfile)))return;
                _selectedControlAgentProfile = value;

                OnPropertyChanged();
                OnPropertyChanged(nameof(IsControlAgentDirectInput));
                OnPropertyChanged(nameof(IsSelectedControlAgentWebAgent));
                OnPropertyChanged(nameof(IsContextPanelsVisible));
                OnPropertyChanged(nameof(IsImuRpyTabVisible));
                OnPropertyChanged(nameof(IsGpsTabVisible));        // 추가
                OnPropertyChanged(nameof(DevicePowerFreshnessText));
                OnPropertyChanged(nameof(CurrentControlPowerText));
                OnPropertyChanged(nameof(CurrentEoPowerText));
                OnPropertyChanged(nameof(CurrentIrPowerText));
                OnPropertyChanged(nameof(IsSystemDateTimeTabVisible));
                OnPropertyChanged(nameof(PanTiltZeroRouteText));
                ApplyControlAgentProfile(value);
                EnsureVisibleCommunicationSettingsTab();
            }

        }

        private void LoadRtspCommunicationSettings()
        {
            _isLoadingRtspCommunicationSettings = true;

            try
            {
                Properties.Settings settings = Properties.Settings.Default;
                _selectedControlAgentProfile = ResolveControlAgentProfile(
                    settings.SavedControlAgentProfile);

                // 2026-09-21: 저장된 CONNECT 프로필을 복원할 때도 화면의 활성 Agent와
                // 패킷 좌표계를 동시에 맞춘다. 기존에는 IP/Port와 unsigned 좌표만 복원되어
                // Web Agent 프로필에서도 CURRENT STATUS가 LA AGENT로 남을 수 있었다.
                SelectedEquipmentStatusMode =
                    _selectedControlAgentProfile.AgentType == ControlAgentType.LaAgent
                        ? EquipmentStatusMode.Rooftop
                        : EquipmentStatusMode.Environment;
                ApplyControlAgentPanCoordinateMode(_selectedControlAgentProfile);
                ApplyProfilePanTiltSpeedDefault(_selectedControlAgentProfile);
                if (!_selectedControlAgentProfile.IsDirectInput)
                {
                    ControlAgentIp = _selectedControlAgentProfile.IpAddress;
                    ControlAgentPortText = _selectedControlAgentProfile.Port;
                }

                RtspSourceOption profileEoSource =
                    !_selectedControlAgentProfile.IsDirectInput
                        ? EoRtspSourceOptions.FirstOrDefault(item =>
                            string.Equals(item.DisplayName,
                                _selectedControlAgentProfile.EoPresetName,
                                StringComparison.OrdinalIgnoreCase))
                        : null;

                RtspSourceOption profileIrSource =
                    !_selectedControlAgentProfile.IsDirectInput
                        ? IrRtspSourceOptions.FirstOrDefault(item =>
                            string.Equals(item.DisplayName,
                                _selectedControlAgentProfile.IrPresetName,
                                StringComparison.OrdinalIgnoreCase))
                        : null;

                string eoAddress = settings.SavedEoRtspUrl?.Trim();
                string irAddress = settings.SavedIrRtspUrl?.Trim();
                if (profileEoSource != null) eoAddress = profileEoSource.Address;
                if (profileIrSource != null) irAddress = profileIrSource.Address;

                MigrateLegacyRtspSettings(
                    settings.SavedEoRtspPreset,
                    settings.SavedIrRtspPreset,
                    ref eoAddress,
                    ref irAddress);

                if (!IsValidRtspAddress(eoAddress))
                {
                    eoAddress = profileEoSource?.Address ?? RooftopMr300EoRtspAddress;
                }

                if (!IsValidRtspAddress(irAddress))
                {
                    irAddress = profileIrSource?.Address ?? RooftopMr300IrRtspAddress;
                }

                EoSourceAddress = eoAddress;
                IrSourceAddress = irAddress;
                // 2026-10-02: AI 탭에서 마지막으로 고른 EO/IR 입력은
                // Viewer 선택과 독립적으로 복원한다.
                string aiEoAddress = settings.SavedAiEoRtspUrl?.Trim();
                string aiIrAddress = settings.SavedAiIrRtspUrl?.Trim();
                if (!IsValidRtspAddress(aiEoAddress)) aiEoAddress = eoAddress;
                if (!IsValidRtspAddress(aiIrAddress)) aiIrAddress = irAddress;

                AiRtsp0Address = aiEoAddress;
                AiRtsp1Address = aiIrAddress;

                _selectedEoRtspSource = ResolveRtspSource(
                    EoRtspSourceOptions, settings.SavedEoRtspPreset, eoAddress);
                _selectedIrRtspSource = ResolveRtspSource(
                    IrRtspSourceOptions, settings.SavedIrRtspPreset, irAddress);
                _selectedAiEoRtspSource = ResolveRtspSource(
                    EoRtspSourceOptions, settings.SavedAiEoRtspPreset, aiEoAddress);
                _selectedAiIrRtspSource = ResolveRtspSource(
                    IrRtspSourceOptions, settings.SavedAiIrRtspPreset, aiIrAddress);

                OnPropertyChanged(nameof(SelectedEoRtspSource));
                OnPropertyChanged(nameof(SelectedControlAgentProfile));
                OnPropertyChanged(nameof(IsControlAgentDirectInput));
                OnPropertyChanged(nameof(IsSelectedControlAgentWebAgent));
                OnPropertyChanged(nameof(IsImuRpyTabVisible));
                OnPropertyChanged(nameof(IsContextPanelsVisible));
                OnPropertyChanged(nameof(IsGpsTabVisible));        // 추가
                OnPropertyChanged(nameof(DevicePowerFreshnessText));
                OnPropertyChanged(nameof(CurrentControlPowerText));
                OnPropertyChanged(nameof(CurrentEoPowerText));
                OnPropertyChanged(nameof(CurrentIrPowerText));
                OnPropertyChanged(nameof(IsSystemDateTimeTabVisible));
                OnPropertyChanged(nameof(SelectedIrRtspSource));
                OnPropertyChanged(nameof(SelectedAiEoRtspSource));
                OnPropertyChanged(nameof(SelectedAiIrRtspSource));
                OnPropertyChanged(nameof(IsEoRtspDirectInput));
                OnPropertyChanged(nameof(IsIrRtspDirectInput));
                OnPropertyChanged(nameof(IsAiEoRtspDirectInput));
                OnPropertyChanged(nameof(IsAiIrRtspDirectInput));

                ConsoleLogHelper.StateSection(
                    "RTSP CONFIG",
                    "Saved camera settings loaded",
                    string.Empty,
                    "EO_PRESET=" + _selectedEoRtspSource?.DisplayName,
                    "EO=" + ConsoleLogHelper.MaskRtspPassword(eoAddress),
                    "IR_PRESET=" + _selectedIrRtspSource?.DisplayName,
                    "IR=" + ConsoleLogHelper.MaskRtspPassword(irAddress),
                    "AI_EO_PRESET=" + _selectedAiEoRtspSource?.DisplayName,
                    "AI_IR_PRESET=" + _selectedAiIrRtspSource?.DisplayName,
                    "CONTROL_PROFILE=" + _selectedControlAgentProfile?.DisplayName);
            }
            catch (Exception ex)
            {
                EoSourceAddress = RooftopMr300EoRtspAddress;
                IrSourceAddress = RooftopMr300IrRtspAddress;
                AiRtsp0Address = EoSourceAddress;
                AiRtsp1Address = IrSourceAddress;
                ConsoleLogHelper.Error(
                    "RTSP CONFIG",
                    "Load failed; rooftop MR300 defaults retained",
                    ex);
            }
            finally
            {
                _isLoadingRtspCommunicationSettings = false;
            }

        }

        private void SaveRtspCommunicationSettings()
        {
            if (_isLoadingRtspCommunicationSettings)
            {
                return;
            }

            try
            {
                Properties.Settings settings = Properties.Settings.Default;
                settings.SavedEoRtspUrl = EoSourceAddress?.Trim() ?? string.Empty;
                settings.SavedIrRtspUrl = IrSourceAddress?.Trim() ?? string.Empty;
                settings.SavedEoRtspPreset =
                    SelectedEoRtspSource?.DisplayName ?? "직접 입력";
                settings.SavedIrRtspPreset =
                    SelectedIrRtspSource?.DisplayName ?? "직접 입력";
                settings.SavedControlAgentProfile =
                    SelectedControlAgentProfile.DisplayName;
                settings.SavedAiEoRtspUrl = AiRtsp0Address?.Trim() ?? string.Empty;
                settings.SavedAiIrRtspUrl = AiRtsp1Address?.Trim() ?? string.Empty;
                settings.SavedAiEoRtspPreset =
                    SelectedAiEoRtspSource?.DisplayName ?? "직접 입력";
                settings.SavedAiIrRtspPreset =
                    SelectedAiIrRtspSource?.DisplayName ?? "직접 입력";
                settings.Save();

                ConsoleLogHelper.StateSection(
                    "RTSP CONFIG",
                    "Camera settings saved",
                    string.Empty,
                    "EO_PRESET=" + settings.SavedEoRtspPreset,
                    "EO=" + ConsoleLogHelper.MaskRtspPassword(settings.SavedEoRtspUrl),
                    "IR_PRESET=" + settings.SavedIrRtspPreset,
                    "IR=" + ConsoleLogHelper.MaskRtspPassword(settings.SavedIrRtspUrl),
                    "AI_EO_PRESET=" + settings.SavedAiEoRtspPreset,
                    "AI_IR_PRESET=" + settings.SavedAiIrRtspPreset,
                    "CONTROL_PROFILE=" + settings.SavedControlAgentProfile);
            }
            catch (Exception ex)
            {
                ConsoleLogHelper.Error(
                    "RTSP CONFIG",
                    "Save failed; current runtime values retained",
                    ex);
            }

        }

        private void ApplyProfilePanTiltSpeedDefault(ControlAgentProfileOption profile)
        {
            // MR500 환경부 프로필만 40. 기동/LA 장비와 줌 연동 감쇄는 유지한다.
            var environmentDevice = OpenCvWpfTracking.Services.Configuration.DeviceCatalog.Get("MR500_MOE");
            byte speed = environmentDevice.Id == "MR500_MOE" && profile != null && !profile.IsDirectInput &&
                string.Equals(profile.EoPresetName, environmentDevice.EoLabel, StringComparison.Ordinal)
                ? (byte)40 : (byte)30;
            PanTiltSpeedLevel = speed;
            ConsoleLogHelper.State("PTZ SPEED", "Profile default=" + speed);
        }

        private void ApplyControlAgentProfile(ControlAgentProfileOption profile)
        {
            // 2026-09-18: CONNECT 프로필이 LA/WEB 활성 모드의 유일한 결정점이다.
            SelectedEquipmentStatusMode = profile.AgentType == ControlAgentType.LaAgent
                ? EquipmentStatusMode.Rooftop
                : EquipmentStatusMode.Environment;
            ApplyControlAgentPanCoordinateMode(profile);
            ApplyProfilePanTiltSpeedDefault(profile);

            // 2026-09-16: 직접 입력은 현재 IP/Port와 카메라 선택을 유지하고
            // Control Agent 입력란만 편집 가능하게 전환한다.
            if (profile.IsDirectInput)
            {
                OnPropertyChanged(nameof(IsControlAgentDirectInput));
                SaveRtspCommunicationSettings();
                return;
            }

            ControlAgentIp = profile.IpAddress;
            ControlAgentPortText = profile.Port;

            RtspSourceOption eo = EoRtspSourceOptions.First(item =>
                string.Equals(item.DisplayName, profile.EoPresetName,
                    StringComparison.OrdinalIgnoreCase));
            RtspSourceOption ir = IrRtspSourceOptions.First(item =>
                string.Equals(item.DisplayName, profile.IrPresetName,
                    StringComparison.OrdinalIgnoreCase));

            bool wasLoading = _isLoadingRtspCommunicationSettings;
            _isLoadingRtspCommunicationSettings = true;
            try
            {
                SelectedEoRtspSource = eo;
                SelectedIrRtspSource = ir;
                SelectedAiEoRtspSource = eo;
                SelectedAiIrRtspSource = ir;
            }
            finally { _isLoadingRtspCommunicationSettings = wasLoading; }
            SaveRtspCommunicationSettings();
        }

        public bool IsControlAgentDirectInput =>
            SelectedControlAgentProfile?.IsDirectInput == true;

        /// <summary>
        /// 2026-10-06: 좌표계/화면 모드와 분리된 Control Agent 프로필 기준값.
        /// 전원 제어 명령은 WebAgent 프로필에 공통 적용한다.
        /// 상태 표시는 기동형만 Function 0x31을 사용하고 나머지는 기존 응답으로 폴백한다.
        /// </summary>
        public bool IsSelectedControlAgentWebAgent =>
            SelectedControlAgentProfile?.AgentType == ControlAgentType.WebAgent;

        /// <summary>
        /// 2026-10-01: IMU가 장착된 4층 LR1000(기동형) 프로필에서만
        /// IMU/RPY 탭을 표시한다. 직접 입력은 기동형 WebAgent IP인
        /// 192.168.20.163을 입력한 경우에만 동일하게 표시한다.
        /// 연결 여부와 Build 지원 여부는 탭 내부에서 별도로 안내한다.
        /// </summary>
        public bool IsImuRpyTabVisible =>
            SelectedControlAgentProfile.PositionSensors ||
            (IsControlAgentDirectInput && OpenCvWpfTracking.Services.Configuration.DeviceCatalog.Entries.Any(e =>
                e.PositionSensors && string.Equals(e.AgentIp,ControlAgentIp?.Trim(),StringComparison.OrdinalIgnoreCase)));

        /// <summary>
        /// 2026-10-01: 시스템 시간 조회/설정은 WebAgent 프로필에서만 표시한다.
        /// LA 방식인 옥상 MR300(테스트)에는 해당 프로토콜이 없다.
        /// </summary>
        public bool IsSystemDateTimeTabVisible => IsEnvironmentStatusSelected;

        /// <summary>
        /// 2026-10-02: GPS는 IMU/RPY와 동일하게
        /// 4층 LR1000(기동형) 프로필에서만 표시한다.
        /// 직접 입력은 기동형 WebAgent IP 192.168.20.163인 경우에만 표시한다.
        /// </summary>
        public bool IsGpsTabVisible => IsImuRpyTabVisible;

        public int SelectedCommunicationSettingsTabIndex
        {
            get => _selectedCommunicationSettingsTabIndex;
            set
            {
                if (_selectedCommunicationSettingsTabIndex == value)
                {
                    return;
                }

                _selectedCommunicationSettingsTabIndex = value;
                OnPropertyChanged();
                UpdateSystemTimePollingState();
            }
        }

        private void EnsureVisibleCommunicationSettingsTab()
        {
            // IMU/RPY 또는 SYSTEM TIME 탭을 보고 있던 중 지원하지 않는
            // 장비로 전환하면 숨겨진 탭이 남지 않도록 CONTROL/RTSP로 돌아간다.
            // 사용자가 탭을 Drag해 순서를 바꿀 수 있으므로 고정 Index로
            // 숨김 탭을 판별하지 않는다. 지원 탭 구성이 줄어드는 장비 전환 시
            // 항상 안전한 CTRL/RTSP 탭으로 복귀한다.
            if (!IsImuRpyTabVisible || !IsSystemDateTimeTabVisible || !IsGpsTabVisible)
            {
                SelectedCommunicationSettingsTabIndex = 0;
            }
        }

        /// <summary>
        /// 2026-09-15: REI 통합본에서는 Web Agent 프로필에만 unsigned Pan 좌표를 적용한다.
        /// </summary>
        private void ApplyControlAgentPanCoordinateMode(ControlAgentProfileOption profile)
        {
            profile = _requestedControlProfile ?? profile;
            // 프로토콜 변환만 Agent 좌표계에 맞추고 GUI는 항상 signed 좌표로 표시한다.
            _controlCommandService.UseUnsignedWebAgentPanCoordinates =
                profile?.AgentType == ControlAgentType.WebAgent;

            _controlCommandService.UseUnsignedWebAgentTiltCoordinates =
                _controlCommandService.UseUnsignedWebAgentPanCoordinates;

        }

        private static RtspSourceOption ResolveRtspSource(
            System.Collections.Generic.IEnumerable<RtspSourceOption> options,
            string savedDisplayName,
            string address)
        {
            RtspSourceOption byName = options.FirstOrDefault(option =>
                string.Equals(option.DisplayName, savedDisplayName,
                    StringComparison.OrdinalIgnoreCase));

            if (byName != null &&
                (byName.IsDirectInput || string.Equals(
                    byName.Address, address, StringComparison.OrdinalIgnoreCase)))
            {
                return byName;
            }

            return options.FirstOrDefault(option =>
                       !option.IsDirectInput && string.Equals(
                           option.Address, address, StringComparison.OrdinalIgnoreCase))
                   ?? options.First(option => option.IsDirectInput);
        }

        /// <summary>
        /// 2026-10-01: 이전 버전에 저장된 장비명도 현재 장비 프로필로 안전하게 이관한다.
        /// </summary>
        private ControlAgentProfileOption ResolveControlAgentProfile(
            string savedDisplayName)
        {
            ControlAgentProfileOption exact = ControlAgentProfiles.FirstOrDefault(item =>
                string.Equals(item.DisplayName, savedDisplayName,
                    StringComparison.OrdinalIgnoreCase));

            if (exact != null)
            {
                return exact;
            }

            if (!string.IsNullOrWhiteSpace(savedDisplayName) &&
                (savedDisplayName.IndexOf("MR500",
                     StringComparison.OrdinalIgnoreCase) >= 0 ||
                 savedDisplayName.IndexOf("환경부",
                     StringComparison.OrdinalIgnoreCase) >= 0))
            {
                return ControlAgentProfiles.FirstOrDefault(p=>p.DisplayName.IndexOf("MR500",StringComparison.OrdinalIgnoreCase)>=0) ?? ControlAgentProfiles.First();
            }

            if (!string.IsNullOrWhiteSpace(savedDisplayName) &&
                savedDisplayName.IndexOf("LR1000",
                    StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return ControlAgentProfiles.FirstOrDefault(p=>p.DisplayName.IndexOf("LR1000",StringComparison.OrdinalIgnoreCase)>=0) ?? ControlAgentProfiles.First();
            }

            if (!string.IsNullOrWhiteSpace(savedDisplayName) &&
                savedDisplayName.IndexOf("ER-WATCHER",
                    StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return ControlAgentProfiles.FirstOrDefault(p=>p.DisplayName.IndexOf("ER-WATCHER",StringComparison.OrdinalIgnoreCase)>=0) ?? ControlAgentProfiles.First();
            }

            if (string.Equals(savedDisplayName, "직접 입력",
                    StringComparison.OrdinalIgnoreCase))
            {
                return ControlAgentProfiles.Last();
            }

            // 폐기된 옥상 GOP 및 기존 MR300 명칭은 옥상 MR300(테스트, LA)로 통합한다.
            return ControlAgentProfiles[0];
        }

        /// <summary>
        /// 2026-10-01: 폐기된 임시 주소와 구 환경부 프리셋을 현재 장비 주소로 변환한다.
        /// 사용자가 직접 입력한 다른 정상 RTSP 주소는 변경하지 않는다.
        /// </summary>
        private static void MigrateLegacyRtspSettings(
            string savedEoPreset,
            string savedIrPreset,
            ref string eoAddress,
            ref string irAddress)
        {
            if (string.Equals(eoAddress,
                    "rtsp://192.168.1.25:554/AVStream1_1",
                    StringComparison.OrdinalIgnoreCase))
            {
                eoAddress = RooftopMr300EoRtspAddress;
            }

            if (string.Equals(irAddress,
                    "rtsp://192.168.2.101:554/stream1",
                    StringComparison.OrdinalIgnoreCase))
            {
                irAddress = RooftopMr300IrRtspAddress;
            }

            if (string.Equals(savedEoPreset,
                    "환경부(MOE) PTZ 주간(EO)",
                    StringComparison.OrdinalIgnoreCase))
            {
                eoAddress = RooftopMr500EoRtspAddress;
            }

            if (string.Equals(savedIrPreset,
                    "환경부(MOE) PTZ 열상(IR)",
                    StringComparison.OrdinalIgnoreCase))
            {
                irAddress = RooftopMr500IrRtspAddress;
            }
        }

    }

}
