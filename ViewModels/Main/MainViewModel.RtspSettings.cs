using OpenCvWpfTracking.Models.Main;
using System.Collections.ObjectModel;

namespace OpenCvWpfTracking.ViewModels.Main
{
    public sealed class ControlAgentProfileOption
    {
        /// <summary>주소·표시 순번과 독립된 장비 설정의 고정 ID.</summary>
        public string DeviceId { get; }

        public string DisplayName { get; }

        public string IpAddress { get; }

        public string Port { get; }

        public string EoPresetName { get; }

        public string IrPresetName { get; }

        public bool IsDirectInput { get; }

        public bool PositionSensors { get; }

        public ControlAgentType AgentType { get; }

        public ControlAgentProfileOption(string displayName, string ipAddress, string port, string eoPresetName, string irPresetName, bool isDirectInput = false, ControlAgentType agentType = ControlAgentType.WebAgent, bool positionSensors = false, string deviceId = null)
        {
            DeviceId = deviceId;
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

        /// <summary>장비 CSV 목록과 직접 입력 항목을 화면에 제공한다.</summary>
        public ObservableCollection<ControlAgentProfileOption> ControlAgentProfiles
        {
            get { return _rtspSettingsFeature.ControlAgentProfiles; }
            private set { _rtspSettingsFeature.ControlAgentProfiles = value; }
        }

        public ControlAgentProfileOption SelectedControlAgentProfile
        {
            get { return _rtspSettingsFeature.SelectedControlAgentProfile; }
            set { _rtspSettingsFeature.SelectedControlAgentProfile = value; }
        }

        /// <summary>
        /// 저장된 연결 프로필과 EO/IR 주소를 복원하고 해당 장비 제어 모드를 맞춘다.
        /// </summary>
        private void LoadRtspCommunicationSettings()
        {
            _rtspSettingsFeature.LoadRtspCommunicationSettings();
        }

        public bool IsControlAgentDirectInput
        {
            get { return _rtspSettingsFeature.IsControlAgentDirectInput; }
        }

        /// <summary>
        /// 2026-10-06: 좌표계/화면 모드와 분리된 Control Agent 프로필 기준값.
        /// 전원 제어 명령은 WebAgent 프로필에 공통 적용한다.
        /// 상태 표시는 기동형만 Function 0x31을 사용하고 나머지는 기존 응답으로 폴백한다.
        /// </summary>
        /// <summary>LA 전용 프리셋 화면과 명령은 실제 제어 프로필을 기준으로 분리한다.</summary>
        public bool IsSelectedControlAgentLaAgent
        {
            get { return _rtspSettingsFeature.IsSelectedControlAgentLaAgent; }
        }

        public bool IsSelectedControlAgentWebAgent
        {
            get { return _rtspSettingsFeature.IsSelectedControlAgentWebAgent; }
        }

        /// <summary>
        /// 2026-10-01: IMU가 장착된 4층 LR1000(기동형) 프로필에서만
        /// IMU/RPY 탭을 표시한다. 직접 입력은 기동형 WebAgent IP인
        /// 192.168.0.163을 입력한 경우에만 동일하게 표시한다.
        /// 연결 여부와 Build 지원 여부는 탭 내부에서 별도로 안내한다.
        /// </summary>
        public bool IsImuRpyTabVisible
        {
            get { return _rtspSettingsFeature.IsImuRpyTabVisible; }
        }

        /// <summary>
        /// 2026-10-01: 시스템 시간 조회/설정은 WebAgent 프로필에서만 표시한다.
        /// LA 방식인 옥상 MR300(테스트)에는 해당 프로토콜이 없다.
        /// </summary>
        public bool IsSystemDateTimeTabVisible
        {
            get { return _rtspSettingsFeature.IsSystemDateTimeTabVisible; }
        }

        /// <summary>
        /// 2026-10-02: GPS는 IMU/RPY와 동일하게
        /// 4층 LR1000(기동형) 프로필에서만 표시한다.
        /// 직접 입력은 기동형 WebAgent IP 192.168.0.163인 경우에만 표시한다.
        /// </summary>
        public bool IsGpsTabVisible
        {
            get { return _rtspSettingsFeature.IsGpsTabVisible; }
        }

        public int SelectedCommunicationSettingsTabIndex
        {
            get { return _rtspSettingsFeature.SelectedCommunicationSettingsTabIndex; }
            set { _rtspSettingsFeature.SelectedCommunicationSettingsTabIndex = value; }
        }

    }
}
