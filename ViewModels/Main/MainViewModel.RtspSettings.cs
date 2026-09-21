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
        public ControlAgentType AgentType { get; }

        public ControlAgentProfileOption(string displayName, string ipAddress,
            string port, string eoPresetName, string irPresetName,
            bool isDirectInput = false,
            ControlAgentType agentType = ControlAgentType.WebAgent)
        {
            DisplayName = displayName;
            IpAddress = ipAddress;
            Port = port;
            EoPresetName = eoPresetName;
            IrPresetName = irPresetName;
            IsDirectInput = isDirectInput;
            AgentType = agentType;
        }

    }

    public partial class MainViewModel
    {
        // 2026-09-08: 신규 옥상 MR300 EO/IR Camera Preset.
        private const string RooftopMr300EoRtspAddress =
            "rtsp://root:rmffhqjf1!@192.168.1.25:554/AVStream1_1";

        private const string RooftopMr300IrRtspAddress =
            "rtsp://admin:Cg600ip100m@192.168.2.101:554/stream1";

        private const string RooftopMr300EoDisplayName =
            "옥상 MR300 - 주간(EO)";

        private const string RooftopMr300IrDisplayName =
            "옥상 MR300 - 열상(IR)";

        // 2026-09-16: 기동형 LR1000 장비 리스트 ver3 기준 4층 장비 설정.
        private const string Lr1000EoRtspAddress =
            "rtsp://admin:rmffhqjf1!@192.168.2.155:554/trackID=1";

        private const string Lr1000IrRtspAddress =
            "rtsp://root:rmffhqjf1!@192.168.1.41/cam0_0";

        private const string Lr1000EoDisplayName =
            "4층 LR1000 - 주간(EO)";

        private const string Lr1000IrDisplayName =
            "4층 LR1000 - 열상(IR)";

        private bool _isLoadingRtspCommunicationSettings;
        private ControlAgentProfileOption _selectedControlAgentProfile;

        public ObservableCollection<ControlAgentProfileOption> ControlAgentProfiles { get; } =
            new ObservableCollection<ControlAgentProfileOption>
            {
                new ControlAgentProfileOption(
                    "옥상 GOP EO/IR - LA 방식",
                    "127.0.0.1", "5001",
                    "옥상 GOP 주간(EO)", "옥상 GOP 열상(IR)",
                    agentType: ControlAgentType.LaAgent),
                new ControlAgentProfileOption(
                    "옥상 MR300 EO/IR - O-droid 방식",
                    "192.168.20.164", "5005",
                    RooftopMr300EoDisplayName, RooftopMr300IrDisplayName,
                    agentType: ControlAgentType.WebAgent),
                new ControlAgentProfileOption(
                    "4층 LR1000 EO/IR - Web Agent 방식",
                    "192.168.20.163", "5005",
                    Lr1000EoDisplayName, Lr1000IrDisplayName),
                new ControlAgentProfileOption(
                    "환경부(MOE) PTZ EO/IR - Web Agent 방식",
                    "192.168.20.161", "5005",
                    "환경부(MOE) PTZ 주간(EO)",
                    "환경부(MOE) PTZ 열상(IR)"),
                new ControlAgentProfileOption(
                    "직접 입력",
                    null, null, null, null,
                    isDirectInput: true)
            };

        public ControlAgentProfileOption SelectedControlAgentProfile
        {
            get => _selectedControlAgentProfile ?? ControlAgentProfiles.First();
            set
            {
                if (value == null || ReferenceEquals(_selectedControlAgentProfile, value))
                {
                    return;
                }

                _selectedControlAgentProfile = value;

                OnPropertyChanged();
                OnPropertyChanged(nameof(IsControlAgentDirectInput));
                ApplyControlAgentProfile(value);
            }

        }

        private void LoadRtspCommunicationSettings()
        {
            _isLoadingRtspCommunicationSettings = true;

            try
            {
                Properties.Settings settings = Properties.Settings.Default;
                _selectedControlAgentProfile = ControlAgentProfiles.FirstOrDefault(item =>
                    string.Equals(item.DisplayName, settings.SavedControlAgentProfile,
                        StringComparison.OrdinalIgnoreCase)) ?? ControlAgentProfiles.First();

                // 2026-09-21: 저장된 CONNECT 프로필을 복원할 때도 화면의 활성 Agent와
                // 패킷 좌표계를 동시에 맞춘다. 기존에는 IP/Port와 unsigned 좌표만 복원되어
                // Web Agent 프로필에서도 CURRENT STATUS가 LA AGENT로 남을 수 있었다.
                SelectedEquipmentStatusMode =
                    _selectedControlAgentProfile.AgentType == ControlAgentType.LaAgent
                        ? EquipmentStatusMode.Rooftop
                        : EquipmentStatusMode.Environment;
                ApplyControlAgentPanCoordinateMode(_selectedControlAgentProfile);
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

                if (!IsValidRtspAddress(eoAddress))
                {
                    eoAddress = profileEoSource?.Address ?? GopEoRtspAddress;
                }

                if (!IsValidRtspAddress(irAddress))
                {
                    irAddress = profileIrSource?.Address ?? GopIrRtspAddress;
                }

                EoSourceAddress = eoAddress;
                IrSourceAddress = irAddress;
                AiRtsp0Address = eoAddress;
                AiRtsp1Address = irAddress;

                _selectedEoRtspSource = ResolveRtspSource(
                    EoRtspSourceOptions, settings.SavedEoRtspPreset, eoAddress);
                _selectedIrRtspSource = ResolveRtspSource(
                    IrRtspSourceOptions, settings.SavedIrRtspPreset, irAddress);
                _selectedAiEoRtspSource = _selectedEoRtspSource;
                _selectedAiIrRtspSource = _selectedIrRtspSource;

                OnPropertyChanged(nameof(SelectedEoRtspSource));
                OnPropertyChanged(nameof(SelectedControlAgentProfile));
                OnPropertyChanged(nameof(IsControlAgentDirectInput));
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
                    "CONTROL_PROFILE=" + _selectedControlAgentProfile?.DisplayName);
            }
            catch (Exception ex)
            {
                EoSourceAddress = GopEoRtspAddress;
                IrSourceAddress = GopIrRtspAddress;
                AiRtsp0Address = EoSourceAddress;
                AiRtsp1Address = IrSourceAddress;
                ConsoleLogHelper.Error(
                    "RTSP CONFIG",
                    "Load failed; rooftop GOP defaults retained",
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
                settings.Save();

                ConsoleLogHelper.StateSection(
                    "RTSP CONFIG",
                    "Camera settings saved",
                    string.Empty,
                    "EO_PRESET=" + settings.SavedEoRtspPreset,
                    "EO=" + ConsoleLogHelper.MaskRtspPassword(settings.SavedEoRtspUrl),
                    "IR_PRESET=" + settings.SavedIrRtspPreset,
                    "IR=" + ConsoleLogHelper.MaskRtspPassword(settings.SavedIrRtspUrl),
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

        private void ApplyControlAgentProfile(ControlAgentProfileOption profile)
        {
            // 2026-09-18: CONNECT 프로필이 LA/WEB 활성 모드의 유일한 결정점이다.
            SelectedEquipmentStatusMode = profile.AgentType == ControlAgentType.LaAgent
                ? EquipmentStatusMode.Rooftop
                : EquipmentStatusMode.Environment;
            ApplyControlAgentPanCoordinateMode(profile);

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

            SelectedEoRtspSource = eo;
            SelectedIrRtspSource = ir;
            SelectedAiEoRtspSource = eo;
            SelectedAiIrRtspSource = ir;
            SaveRtspCommunicationSettings();
        }

        public bool IsControlAgentDirectInput =>
            SelectedControlAgentProfile?.IsDirectInput == true;

        /// <summary>
        /// 2026-09-15: REI 통합본에서는 Web Agent 프로필에만 unsigned Pan 좌표를 적용한다.
        /// </summary>
        private void ApplyControlAgentPanCoordinateMode(ControlAgentProfileOption profile)
        {
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

    }

}
