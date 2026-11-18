using OpenCvWpfTracking.Common;
using OpenCvWpfTracking.Models.Main;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using static OpenCvWpfTracking.ViewModels.Main.MainViewModel;

namespace OpenCvWpfTracking.ViewModels.Main
{
    /// <summary>장비 선택과 연결 설정에 필요한 협력 기능만 받는 경계입니다. 화면 객체의 내부 구현을 알지 않습니다.</summary>
    internal interface IRtspSettingsFeatureContext
    {
        bool _applyingPresetProfile { get; set; }

        System.Threading.CancellationTokenSource _controlAgentReconnectCts { get; set; }

        Services.Communication.IControlCommandServicePort _controlCommandService { get; }

        System.Threading.CancellationTokenSource _cts { get; set; }

        Services.Video.IFFmpegDecoderServicePort _eoDecoder { get; }

        Services.Video.IFFmpegDecoderServicePort _irDecoder { get; }

        bool _isEoFrameDisplayed { get; set; }

        bool _isIrFrameDisplayed { get; set; }

        bool _isVideoConnecting { get; set; }

        Services.Communication.ITcpClientServicePort _laTcpService { get; }

        RtspSourceOption _selectedAiEoRtspSource { get; set; }

        RtspSourceOption _selectedAiIrRtspSource { get; set; }

        RtspSourceOption _selectedEoRtspSource { get; set; }

        RtspSourceOption _selectedIrRtspSource { get; set; }

        string AiRtsp0Address { get; set; }

        string AiRtsp1Address { get; set; }

        string ControlAgentConnectionStatusText { get; set; }

        string ControlAgentIp { get; set; }

        string ControlAgentPortText { get; set; }

        string CurrentControlPowerText { get; }

        string CurrentEoPowerText { get; }

        string CurrentIrPowerText { get; }

        string DevicePowerFreshnessText { get; }

        int EoMinimumSpeed { get; set; }

        ObservableCollection<RtspSourceOption> EoRtspSourceOptions { get; }

        string EoSourceAddress { get; set; }

        double EoSpeedExponent { get; set; }

        string EoStatusText { get; set; }

        string EoZoomRatioOverlayText { get; }

        int IrMinimumSpeed { get; set; }

        ObservableCollection<RtspSourceOption> IrRtspSourceOptions { get; }

        string IrSourceAddress { get; set; }

        double IrSpeedExponent { get; set; }

        string IrStatusText { get; set; }

        string IrZoomRatioOverlayText { get; }

        bool IsAiEoRtspDirectInput { get; }

        bool IsAiIrRtspDirectInput { get; }

        bool IsAutoFocusPanelVisible { get; }

        bool IsContextPanelsVisible { get; }

        bool IsEnvironmentStatusSelected { get; }

        bool IsEoRtspDirectInput { get; }

        bool IsIrRtspDirectInput { get; }

        bool IsWebAgentFocusControlEnabled { get; set; }

        void OnPropertyChanged([global::System.Runtime.CompilerServices.CallerMemberName] string propertyName = null);

        byte PanTiltSpeedLevel { get; set; }

        string PanTiltZeroRouteText { get; }

        void ResetThermalPaletteButtonVisuals();

        void ResetWebAgentControlSession();

        RtspSourceOption SelectedAiEoRtspSource { get; set; }

        RtspSourceOption SelectedAiIrRtspSource { get; set; }

        RtspSourceOption SelectedEoRtspSource { get; set; }

        EquipmentStatusMode SelectedEquipmentStatusMode { get; set; }

        RtspSourceOption SelectedIrRtspSource { get; set; }

        void SwitchEquipmentPresets();

        string ThermalColorPaletteButtonText { get; }

        void UpdateSystemTimePollingState();

        string ZoomOpticalRangeText { get; }

        string ZoomOpticalStandardText { get; }
    }

    /// <summary>장비 선택과 연결 설정 담당 객체입니다. 상태를 보관하고 입력을 받아 결과를 알립니다.</summary>
    internal sealed class RtspSettingsFeature
    {

        #region [협력 객체와 초기화]

        private readonly IRtspSettingsFeatureContext _context;

        internal RtspSettingsFeature(IRtspSettingsFeatureContext context)
        {
            _context = context ?? throw new System.ArgumentNullException(nameof(context));
        }

        internal void InitializeState()
        {
            ControlAgentProfiles = new ObservableCollection<ControlAgentProfileOption>(OpenCvWpfTracking.Services.Configuration.DeviceCatalog.Entries.Select(e => new ControlAgentProfileOption(e.Label + " EO/IR", e.AgentIp, e.AgentPort, e.EoLabel, e.IrLabel, agentType: e.AgentType, positionSensors: e.PositionSensors, deviceId: e.Id)).Concat(new[] { new ControlAgentProfileOption("직접 입력", null, null, null, null, isDirectInput: true) }));
        }

        #endregion

        #region [상태 필드]

        internal bool _isLoadingRtspCommunicationSettings;

        internal ControlAgentProfileOption _selectedControlAgentProfile;

        internal int _selectedCommunicationSettingsTabIndex;

        internal int _deviceConnectRequested;

        internal bool _eoAlreadyConnectedNotice;

        internal bool _irAlreadyConnectedNotice;

        internal ControlAgentProfileOption _requestedControlProfile;

        internal string _requestedControlIp;

        internal string _requestedControlPort;

        internal string _requestedEoAddress;

        internal string _requestedIrAddress;

        #endregion

        #region [화면 속성과 이벤트]

        internal string SessionControlIp
        {
            get
            {
                return _requestedControlIp ?? _context.ControlAgentIp;
            }
        }

        internal string SessionControlPort
        {
            get
            {
                return _requestedControlPort ?? _context.ControlAgentPortText;
            }
        }

        internal string SessionEoAddress
        {
            get
            {
                return _requestedEoAddress ?? _context.EoSourceAddress;
            }
        }

        internal string SessionIrAddress
        {
            get
            {
                return _requestedIrAddress ?? _context.IrSourceAddress;
            }
        }

        internal bool IsSelectedConnectionTarget
        {
            get
            {
                return (_requestedControlProfile == null || _requestedControlProfile == SelectedControlAgentProfile) && SessionControlIp == _context.ControlAgentIp && SessionControlPort == _context.ControlAgentPortText && SessionEoAddress == _context.EoSourceAddress && SessionIrAddress == _context.IrSourceAddress;
            }
        }

        internal bool HasActiveDeviceSession
        {
            get
            {
                return _context._isVideoConnecting || _context._isEoFrameDisplayed || _context._isIrFrameDisplayed || _context._eoDecoder?.IsOpened == true || _context._irDecoder?.IsOpened == true || _context._laTcpService?.IsConnected == true || _context.ControlAgentConnectionStatusText == "Connected" || _context.ControlAgentConnectionStatusText == "Connecting" || (_context._controlAgentReconnectCts != null && !_context._controlAgentReconnectCts.IsCancellationRequested) || (_context._cts != null && !_context._cts.IsCancellationRequested);
            }
        }

        internal bool IsDeviceSettingsLocked
        {
            get
            {
                return !_isLoadingRtspCommunicationSettings && (HasActiveDeviceSession || System.Threading.Volatile.Read(ref _deviceConnectRequested) != 0);
            }
        }

        internal ObservableCollection<ControlAgentProfileOption> ControlAgentProfiles { get; set; }

        internal ControlAgentProfileOption SelectedControlAgentProfile
        {
            get
            {
                return _selectedControlAgentProfile ?? ControlAgentProfiles.First();
            }

            set
            {
                if (value == null || ReferenceEquals(_selectedControlAgentProfile, value))
                {
                    return;
                }

                _selectedControlAgentProfile = value;

                _context.OnPropertyChanged();

                _context.OnPropertyChanged(nameof(IsControlAgentDirectInput));

                _context.OnPropertyChanged(nameof(IsSelectedControlAgentWebAgent));

                _context.OnPropertyChanged(nameof(_context.ThermalColorPaletteButtonText));

                _context.ResetThermalPaletteButtonVisuals();

                _context.OnPropertyChanged(nameof(IsSelectedControlAgentLaAgent));

                _context.OnPropertyChanged(nameof(_context.IsContextPanelsVisible));

                _context.OnPropertyChanged(nameof(IsImuRpyTabVisible));

                _context.OnPropertyChanged(nameof(IsGpsTabVisible)); // 추가

                _context.OnPropertyChanged(nameof(_context.DevicePowerFreshnessText));

                _context.OnPropertyChanged(nameof(_context.CurrentControlPowerText));

                _context.OnPropertyChanged(nameof(_context.CurrentEoPowerText));

                _context.OnPropertyChanged(nameof(_context.CurrentIrPowerText));

                _context.OnPropertyChanged(nameof(IsSystemDateTimeTabVisible));

                _context.OnPropertyChanged(nameof(_context.PanTiltZeroRouteText));

                _context._applyingPresetProfile = true;

                try
                {
                    ApplyControlAgentProfile(value);
                }
                finally
                {
                    _context._applyingPresetProfile = false;
                }

                _context.SwitchEquipmentPresets();

                EnsureVisibleCommunicationSettingsTab();
            }
        }

        internal bool IsControlAgentDirectInput
        {
            get
            {
                return SelectedControlAgentProfile?.IsDirectInput == true;
            }
        }

        internal bool IsSelectedControlAgentLaAgent
        {
            get
            {
                return !IsSelectedControlAgentWebAgent;
            }
        }

        internal bool IsSelectedControlAgentWebAgent
        {
            get
            {
                return SelectedControlAgentProfile?.AgentType == ControlAgentType.WebAgent;
            }
        }

        internal bool IsImuRpyTabVisible
        {
            get
            {
                return SelectedControlAgentProfile.PositionSensors || (IsControlAgentDirectInput && OpenCvWpfTracking.Services.Configuration.DeviceCatalog.Entries.Any(e => e.PositionSensors && string.Equals(e.AgentIp, _context.ControlAgentIp?.Trim(), StringComparison.OrdinalIgnoreCase)));
            }
        }

        internal bool IsSystemDateTimeTabVisible
        {
            get { return _context.IsEnvironmentStatusSelected; }
        }

        internal bool IsGpsTabVisible
        {
            get { return IsImuRpyTabVisible; }
        }

        internal int SelectedCommunicationSettingsTabIndex
        {
            get { return _selectedCommunicationSettingsTabIndex; }

            set
            {
                if (_selectedCommunicationSettingsTabIndex == value)
                {
                    return;
                }

                _selectedCommunicationSettingsTabIndex = value;

                _context.OnPropertyChanged();

                _context.UpdateSystemTimePollingState();
            }
        }

        #endregion

        #region [동작 처리]

        internal void ShowAlreadyConnected()
        {
            // 중복 연결 안내만 표시하며 실제 연결 상태나 활성 세션을 변경하지 않는다.
            _eoAlreadyConnectedNotice = _context._isEoFrameDisplayed || _context._eoDecoder?.IsOpened == true;

            _irAlreadyConnectedNotice = _context._isIrFrameDisplayed || _context._irDecoder?.IsOpened == true;

            _context.OnPropertyChanged(nameof(_context.EoStatusText));

            _context.OnPropertyChanged(nameof(_context.IrStatusText));

            ConsoleLogHelper.State("DEVICE CONNECT", "Already Connected / request blocked");
        }

        internal void LoadRtspCommunicationSettings()
        {
            _isLoadingRtspCommunicationSettings = true;

            try
            {
                Properties.Settings settings = Properties.Settings.Default;

                _selectedControlAgentProfile = ResolveControlAgentProfile(settings.SavedControlAgentProfile);

                // 2026-09-21: 저장된 CONNECT 프로필을 복원할 때도 화면의 활성 Agent와
                // 패킷 좌표계를 동시에 맞춘다. 기존에는 IP/Port와 unsigned 좌표만 복원되어
                // Web Agent 프로필에서도 CURRENT STATUS가 LA AGENT로 남을 수 있었다.
                _context.SelectedEquipmentStatusMode = _selectedControlAgentProfile.AgentType == ControlAgentType.LaAgent ? EquipmentStatusMode.Rooftop : EquipmentStatusMode.Environment;

                ApplyControlAgentPanCoordinateMode(_selectedControlAgentProfile);

                ApplyProfilePanTiltSpeedDefault(_selectedControlAgentProfile);

                if (!_selectedControlAgentProfile.IsDirectInput)
                {
                    _context.ControlAgentIp = _selectedControlAgentProfile.IpAddress;
                    _context.ControlAgentPortText = _selectedControlAgentProfile.Port;
                }

                RtspSourceOption profileEoSource = !_selectedControlAgentProfile.IsDirectInput ? _context.EoRtspSourceOptions.FirstOrDefault(item => string.Equals(item.DisplayName, _selectedControlAgentProfile.EoPresetName, StringComparison.OrdinalIgnoreCase)) : null;

                RtspSourceOption profileIrSource = !_selectedControlAgentProfile.IsDirectInput ? _context.IrRtspSourceOptions.FirstOrDefault(item => string.Equals(item.DisplayName, _selectedControlAgentProfile.IrPresetName, StringComparison.OrdinalIgnoreCase)) : null;

                string eoAddress = settings.SavedEoRtspUrl?.Trim();

                string irAddress = settings.SavedIrRtspUrl?.Trim();

                if (profileEoSource != null)
                {
                    eoAddress = profileEoSource.Address;
                }

                if (profileIrSource != null)
                {
                    irAddress = profileIrSource.Address;
                }

                MigrateLegacyRtspSettings(settings.SavedEoRtspPreset, settings.SavedIrRtspPreset, ref eoAddress, ref irAddress);

                if (!IsValidRtspAddress(eoAddress))
                {
                    eoAddress = profileEoSource?.Address ?? RooftopMr300EoRtspAddress;
                }

                if (!IsValidRtspAddress(irAddress))
                {
                    irAddress = profileIrSource?.Address ?? RooftopMr300IrRtspAddress;
                }

                _context.EoSourceAddress = eoAddress;
                _context.IrSourceAddress = irAddress;

                // 2026-10-02: AI 탭에서 마지막으로 고른 EO/IR 입력은
                // Viewer 선택과 독립적으로 복원한다.
                string aiEoAddress = settings.SavedAiEoRtspUrl?.Trim();

                string aiIrAddress = settings.SavedAiIrRtspUrl?.Trim();

                // 이름으로 선택한 AI 입력은 현재 CSV 주소를 따른다. 직접 입력 주소는 보존한다.
                aiEoAddress = RestoreNamedRtspAddress(_context.EoRtspSourceOptions, settings.SavedAiEoRtspPreset, aiEoAddress);

                aiIrAddress = RestoreNamedRtspAddress(_context.IrRtspSourceOptions, settings.SavedAiIrRtspPreset, aiIrAddress);

                if (!IsValidRtspAddress(aiEoAddress))
                {
                    aiEoAddress = eoAddress;
                }

                if (!IsValidRtspAddress(aiIrAddress))
                {
                    aiIrAddress = irAddress;
                }

                _context.AiRtsp0Address = aiEoAddress;
                _context.AiRtsp1Address = aiIrAddress;

                _context._selectedEoRtspSource = ResolveRtspSource(_context.EoRtspSourceOptions, settings.SavedEoRtspPreset, eoAddress);

                _context._selectedIrRtspSource = ResolveRtspSource(_context.IrRtspSourceOptions, settings.SavedIrRtspPreset, irAddress);

                _context._selectedAiEoRtspSource = ResolveRtspSource(_context.EoRtspSourceOptions, settings.SavedAiEoRtspPreset, aiEoAddress);

                _context._selectedAiIrRtspSource = ResolveRtspSource(_context.IrRtspSourceOptions, settings.SavedAiIrRtspPreset, aiIrAddress);

                _context.OnPropertyChanged(nameof(_context.SelectedEoRtspSource));

                _context.OnPropertyChanged(nameof(SelectedControlAgentProfile));

                _context.OnPropertyChanged(nameof(IsControlAgentDirectInput));

                _context.OnPropertyChanged(nameof(IsSelectedControlAgentWebAgent));

                _context.OnPropertyChanged(nameof(_context.ThermalColorPaletteButtonText));

                _context.ResetThermalPaletteButtonVisuals();

                _context.OnPropertyChanged(nameof(IsSelectedControlAgentLaAgent));

                _context.OnPropertyChanged(nameof(IsImuRpyTabVisible));

                _context.OnPropertyChanged(nameof(_context.IsContextPanelsVisible));

                _context.OnPropertyChanged(nameof(IsGpsTabVisible)); // 추가

                _context.OnPropertyChanged(nameof(_context.DevicePowerFreshnessText));

                _context.OnPropertyChanged(nameof(_context.CurrentControlPowerText));

                _context.OnPropertyChanged(nameof(_context.CurrentEoPowerText));

                _context.OnPropertyChanged(nameof(_context.CurrentIrPowerText));

                _context.OnPropertyChanged(nameof(IsSystemDateTimeTabVisible));

                _context.OnPropertyChanged(nameof(_context.SelectedIrRtspSource));

                _context.OnPropertyChanged(nameof(_context.SelectedAiEoRtspSource));

                _context.OnPropertyChanged(nameof(_context.SelectedAiIrRtspSource));

                _context.OnPropertyChanged(nameof(_context.IsEoRtspDirectInput));

                _context.OnPropertyChanged(nameof(_context.IsIrRtspDirectInput));

                _context.OnPropertyChanged(nameof(_context.IsAiEoRtspDirectInput));

                _context.OnPropertyChanged(nameof(_context.IsAiIrRtspDirectInput));

                ConsoleLogHelper.StateSection("RTSP CONFIG", "Saved camera settings loaded", string.Empty, "EO_PRESET=" + _context._selectedEoRtspSource?.DisplayName, "EO=" + ConsoleLogHelper.MaskRtspPassword(eoAddress), "IR_PRESET=" + _context._selectedIrRtspSource?.DisplayName, "IR=" + ConsoleLogHelper.MaskRtspPassword(irAddress), "AI_EO_PRESET=" + _context._selectedAiEoRtspSource?.DisplayName, "AI_IR_PRESET=" + _context._selectedAiIrRtspSource?.DisplayName, "CONTROL_PROFILE=" + _selectedControlAgentProfile?.DisplayName);
            }
            catch (Exception ex)
            {
                _context.EoSourceAddress = RooftopMr300EoRtspAddress;
                _context.IrSourceAddress = RooftopMr300IrRtspAddress;
                _context.AiRtsp0Address = _context.EoSourceAddress;
                _context.AiRtsp1Address = _context.IrSourceAddress;

                ConsoleLogHelper.Error("RTSP CONFIG", "Load failed; rooftop MR300 defaults retained", ex);
            }
            finally
            {
                _isLoadingRtspCommunicationSettings = false;
            }
        }

        internal void SaveRtspCommunicationSettings()
        {
            if (_isLoadingRtspCommunicationSettings)
            {
                return;
            }

            try
            {
                Properties.Settings settings = Properties.Settings.Default;

                settings.SavedEoRtspUrl = OpenCvWpfTracking.Services.Video.RtspUriNormalization.Connect(_context.EoSourceAddress?.Trim() ?? string.Empty);

                settings.SavedIrRtspUrl = OpenCvWpfTracking.Services.Video.RtspUriNormalization.Connect(_context.IrSourceAddress?.Trim() ?? string.Empty);

                settings.SavedEoRtspPreset = _context.SelectedEoRtspSource?.DisplayName ?? "직접 입력";

                settings.SavedIrRtspPreset = _context.SelectedIrRtspSource?.DisplayName ?? "직접 입력";

                settings.SavedControlAgentProfile = SelectedControlAgentProfile.DisplayName;

                settings.SavedAiEoRtspUrl = _context.AiRtsp0Address?.Trim() ?? string.Empty;

                settings.SavedAiIrRtspUrl = _context.AiRtsp1Address?.Trim() ?? string.Empty;

                settings.SavedAiEoRtspPreset = _context.SelectedAiEoRtspSource?.DisplayName ?? "직접 입력";

                settings.SavedAiIrRtspPreset = _context.SelectedAiIrRtspSource?.DisplayName ?? "직접 입력";

                settings.Save();

                ConsoleLogHelper.StateSection("RTSP CONFIG", "Camera settings saved", string.Empty, "EO_PRESET=" + settings.SavedEoRtspPreset, "EO=" + ConsoleLogHelper.MaskRtspPassword(settings.SavedEoRtspUrl), "IR_PRESET=" + settings.SavedIrRtspPreset, "IR=" + ConsoleLogHelper.MaskRtspPassword(settings.SavedIrRtspUrl), "AI_EO_PRESET=" + settings.SavedAiEoRtspPreset, "AI_IR_PRESET=" + settings.SavedAiIrRtspPreset, "CONTROL_PROFILE=" + settings.SavedControlAgentProfile);
            }
            catch (Exception ex)
            {
                ConsoleLogHelper.Error("RTSP CONFIG", "Save failed; current runtime values retained", ex);
            }
        }

        internal void ApplyProfilePanTiltSpeedDefault(ControlAgentProfileOption profile)
        {
            // 프로필 기본값만 적용하며 수신 상태로 사용자 속도를 초기화하지 않는다.
            byte speed = 30;

            _context.PanTiltSpeedLevel = speed;

            ConsoleLogHelper.State("PTZ SPEED", "Profile default=" + speed);
        }

        internal void ApplyControlAgentProfile(ControlAgentProfileOption profile)
        {
            // 2026-09-18: CONNECT 프로필이 LA/WEB 활성 모드의 유일한 결정점이다.
            _context.SelectedEquipmentStatusMode = profile.AgentType == ControlAgentType.LaAgent ? EquipmentStatusMode.Rooftop : EquipmentStatusMode.Environment;

            ApplyControlAgentPanCoordinateMode(profile);

            _context.ResetWebAgentControlSession();

            _context.OnPropertyChanged(nameof(_context.IsAutoFocusPanelVisible));

            _context.OnPropertyChanged(nameof(_context.IsWebAgentFocusControlEnabled));

            ApplyProfilePanTiltSpeedDefault(profile);

            foreach (var name in new[]
            {
                nameof(_context.EoSpeedExponent),
                nameof(_context.IrSpeedExponent),
                nameof(_context.EoMinimumSpeed),
                nameof(_context.IrMinimumSpeed),
                nameof(_context.ZoomOpticalStandardText),
                nameof(_context.ZoomOpticalRangeText),
                nameof(_context.EoZoomRatioOverlayText),
                nameof(_context.IrZoomRatioOverlayText)
            }

            )
            {
                _context.OnPropertyChanged(name);
            }

            // 2026-09-16: 직접 입력은 현재 IP/Port와 카메라 선택을 유지하고
            // Control Agent 입력란만 편집 가능하게 전환한다.
            if (profile.IsDirectInput)
            {
                _context.OnPropertyChanged(nameof(IsControlAgentDirectInput));

                SaveRtspCommunicationSettings();

                return;
            }

            _context.ControlAgentIp = profile.IpAddress;
            _context.ControlAgentPortText = profile.Port;

            RtspSourceOption eo = _context.EoRtspSourceOptions.First(item => string.Equals(item.DisplayName, profile.EoPresetName, StringComparison.OrdinalIgnoreCase));

            RtspSourceOption ir = _context.IrRtspSourceOptions.First(item => string.Equals(item.DisplayName, profile.IrPresetName, StringComparison.OrdinalIgnoreCase));

            bool wasLoading = _isLoadingRtspCommunicationSettings;

            _isLoadingRtspCommunicationSettings = true;

            try
            {
                _context.SelectedEoRtspSource = eo;
                _context.SelectedIrRtspSource = ir;
                _context.SelectedAiEoRtspSource = eo;
                _context.SelectedAiIrRtspSource = ir;
            }
            finally
            {
                _isLoadingRtspCommunicationSettings = wasLoading;
            }

            SaveRtspCommunicationSettings();
        }

        internal void EnsureVisibleCommunicationSettingsTab()
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

        internal void ApplyControlAgentPanCoordinateMode(ControlAgentProfileOption profile)
        {
            profile = _requestedControlProfile ?? profile;

            bool previousPanMode = _context._controlCommandService.UseUnsignedWebAgentPanCoordinates;

            bool previousTiltMode = _context._controlCommandService.UseUnsignedWebAgentTiltCoordinates;

            // 프로토콜 변환만 Agent 좌표계에 맞추고 GUI는 항상 signed 좌표로 표시한다.
            _context._controlCommandService.UseUnsignedWebAgentPanCoordinates = profile?.AgentType == ControlAgentType.WebAgent;

            _context._controlCommandService.UseUnsignedWebAgentTiltCoordinates = _context._controlCommandService.UseUnsignedWebAgentPanCoordinates;

            if (previousPanMode != _context._controlCommandService.UseUnsignedWebAgentPanCoordinates || previousTiltMode != _context._controlCommandService.UseUnsignedWebAgentTiltCoordinates)
            {
                ConsoleLogHelper.State("CONTROL COORDINATE MODE", "PROFILE=" + (profile?.DisplayName ?? "UNKNOWN") + " / MODE=" + (_context._controlCommandService.UseUnsignedWebAgentPanCoordinates ? "WEB_AGENT_UNSIGNED" : "LA_SIGNED") + " / SOURCE=CONTROL_AGENT_PROFILE");
            }
        }

        internal static string RestoreNamedRtspAddress(System.Collections.Generic.IEnumerable<RtspSourceOption> options, string savedDisplayName, string address)
        {
            var named = options.FirstOrDefault(option => !option.IsDirectInput && string.Equals(OpenCvWpfTracking.Services.Configuration.DeviceCatalog.DisplayIdentity(option.DisplayName), OpenCvWpfTracking.Services.Configuration.DeviceCatalog.DisplayIdentity(savedDisplayName), StringComparison.OrdinalIgnoreCase));

            return named?.Address ?? address;
        }

        internal static RtspSourceOption ResolveRtspSource(System.Collections.Generic.IEnumerable<RtspSourceOption> options, string savedDisplayName, string address)
        {
            RtspSourceOption byName = options.FirstOrDefault(option => string.Equals(OpenCvWpfTracking.Services.Configuration.DeviceCatalog.DisplayIdentity(option.DisplayName), OpenCvWpfTracking.Services.Configuration.DeviceCatalog.DisplayIdentity(savedDisplayName), StringComparison.OrdinalIgnoreCase));

            if (byName != null && (byName.IsDirectInput || string.Equals(OpenCvWpfTracking.Services.Video.RtspUriNormalization.Connect(byName.Address), OpenCvWpfTracking.Services.Video.RtspUriNormalization.Connect(address), StringComparison.OrdinalIgnoreCase)))
            {
                return byName;
            }

            return options.FirstOrDefault(option => !option.IsDirectInput && string.Equals(OpenCvWpfTracking.Services.Video.RtspUriNormalization.Connect(option.Address), OpenCvWpfTracking.Services.Video.RtspUriNormalization.Connect(address), StringComparison.OrdinalIgnoreCase)) ?? options.First(option => option.IsDirectInput);
        }

        internal ControlAgentProfileOption ResolveControlAgentProfile(string savedDisplayName)
        {
            ControlAgentProfileOption exact = ControlAgentProfiles.FirstOrDefault(item => string.Equals(item.DisplayName, savedDisplayName, StringComparison.OrdinalIgnoreCase));

            if (exact != null)
            {
                return exact;
            }

            // ER-WATCHER 2의 과거 이름을 다른 WATCHER의 첫 항목으로 잘못 복원하지 않는다.
            var renamed = ControlAgentProfiles.FirstOrDefault(item => !item.IsDirectInput && string.Equals(OpenCvWpfTracking.Services.Configuration.DeviceCatalog.DisplayIdentity(item.DisplayName), OpenCvWpfTracking.Services.Configuration.DeviceCatalog.DisplayIdentity(savedDisplayName), StringComparison.OrdinalIgnoreCase));

            if (renamed != null)
            {
                return renamed;
            }

            if (!string.IsNullOrWhiteSpace(savedDisplayName) && (savedDisplayName.IndexOf("MR500", StringComparison.OrdinalIgnoreCase) >= 0 || savedDisplayName.IndexOf("환경부", StringComparison.OrdinalIgnoreCase) >= 0))
            {
                return ControlAgentProfiles.FirstOrDefault(p => p.DisplayName.IndexOf("MR500", StringComparison.OrdinalIgnoreCase) >= 0) ?? ControlAgentProfiles.First();
            }

            if (!string.IsNullOrWhiteSpace(savedDisplayName) && savedDisplayName.IndexOf("LR1000", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return ControlAgentProfiles.FirstOrDefault(p => p.DisplayName.IndexOf("LR1000", StringComparison.OrdinalIgnoreCase) >= 0) ?? ControlAgentProfiles.First();
            }

            if (!string.IsNullOrWhiteSpace(savedDisplayName) && savedDisplayName.IndexOf("ER-WATCHER", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return ControlAgentProfiles.FirstOrDefault(p => p.DisplayName.IndexOf("ER-WATCHER", StringComparison.OrdinalIgnoreCase) >= 0) ?? ControlAgentProfiles.First();
            }

            if (string.Equals(savedDisplayName, "직접 입력", StringComparison.OrdinalIgnoreCase))
            {
                return ControlAgentProfiles.Last();
            }

            // 폐기된 옥상 GOP 및 기존 MR300 명칭은 옥상 MR300(테스트, LA)로 통합한다.
            return ControlAgentProfiles[0];
        }

        internal static void MigrateLegacyRtspSettings(string savedEoPreset, string savedIrPreset, ref string eoAddress, ref string irAddress)
        {
            if (string.Equals(eoAddress, "rtsp://192.168.1.25:554/AVStream1_1", StringComparison.OrdinalIgnoreCase))
            {
                eoAddress = RooftopMr300EoRtspAddress;
            }

            if (string.Equals(irAddress, "rtsp://192.168.2.101:554/stream1", StringComparison.OrdinalIgnoreCase))
            {
                irAddress = RooftopMr300IrRtspAddress;
            }

            if (string.Equals(savedEoPreset, "환경부(MOE) PTZ 주간(EO)", StringComparison.OrdinalIgnoreCase))
            {
                eoAddress = RooftopMr500EoRtspAddress;
            }

            if (string.Equals(savedIrPreset, "환경부(MOE) PTZ 열상(IR)", StringComparison.OrdinalIgnoreCase))
            {
                irAddress = RooftopMr500IrRtspAddress;
            }
        }

        #endregion

    }
}
