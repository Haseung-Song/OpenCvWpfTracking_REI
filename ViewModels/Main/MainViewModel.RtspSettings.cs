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

        public ControlAgentProfileOption(string displayName, string ipAddress,
            string port, string eoPresetName, string irPresetName)
        {
            DisplayName = displayName;
            IpAddress = ipAddress;
            Port = port;
            EoPresetName = eoPresetName;
            IrPresetName = irPresetName;
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

        private bool _isLoadingRtspCommunicationSettings;
        private ControlAgentProfileOption _selectedControlAgentProfile;

        public ObservableCollection<ControlAgentProfileOption> ControlAgentProfiles { get; } =
            new ObservableCollection<ControlAgentProfileOption>
            {
                new ControlAgentProfileOption(
                    "옥상 GOP EO/IR - LA 방식",
                    "127.0.0.1", "5001",
                    "옥상 GOP 주간(EO)", "옥상 GOP 열상(IR)"),
                new ControlAgentProfileOption(
                    "옥상 MR300 EO/IR - O-droid 방식",
                    "192.168.20.164", "5005",
                    RooftopMr300EoDisplayName, RooftopMr300IrDisplayName)
            };

        public ControlAgentProfileOption SelectedControlAgentProfile
        {
            get => _selectedControlAgentProfile ?? ControlAgentProfiles.First();
            set
            {
                if (value == null || ReferenceEquals(_selectedControlAgentProfile, value)) return;
                _selectedControlAgentProfile = value;
                OnPropertyChanged();
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
                ControlAgentIp = _selectedControlAgentProfile.IpAddress;
                ControlAgentPortText = _selectedControlAgentProfile.Port;
                string eoAddress = settings.SavedEoRtspUrl?.Trim();
                string irAddress = settings.SavedIrRtspUrl?.Trim();

                if (!IsValidRtspAddress(eoAddress))
                {
                    eoAddress = GopEoRtspAddress;
                }

                if (!IsValidRtspAddress(irAddress))
                {
                    irAddress = GopIrRtspAddress;
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
