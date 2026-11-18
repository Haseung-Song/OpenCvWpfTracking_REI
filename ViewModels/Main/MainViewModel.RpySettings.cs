using System;
using System.Windows.Input;

namespace OpenCvWpfTracking.ViewModels.Main
{
    /// <summary>
    /// 2026-10-01: WebAgent Build 86 이상에서 제공하는 IMU RPY Offset과
    /// 자동 전송 주파수 설정을 관리한다. 2026-10-02 협의 기준 Function 0x24는
    /// DB Offset 미적용 IMU RAW 값이다. RAW 표시는 유지하며, GPS 탭의 진북 표시에만
    /// WebAgent API 의미와 동일하게 DB Yaw Offset을 GUI 계산으로 적용한다.
    /// </summary>
    public partial class MainViewModel
    {
        internal const int MinimumRpySettingsBuild = 86;

        internal const int MinimumRpyStoredSettingsQueryBuild = 93;

        public ICommand ApplyRpyOffsetCommand
        {
            get { return _rpySettingsFeature.ApplyRpyOffsetCommand; }
            private set { _rpySettingsFeature.ApplyRpyOffsetCommand = value; }
        }

        public ICommand ApplyRpyAutoTxRateCommand
        {
            get { return _rpySettingsFeature.ApplyRpyAutoTxRateCommand; }
            private set { _rpySettingsFeature.ApplyRpyAutoTxRateCommand = value; }
        }

        public ICommand RequestRpyTelemetryCommand
        {
            get { return _rpySettingsFeature.RequestRpyTelemetryCommand; }
            private set { _rpySettingsFeature.RequestRpyTelemetryCommand = value; }
        }

        public ICommand RequestStoredRpyOffsetCommand
        {
            get { return _rpySettingsFeature.RequestStoredRpyOffsetCommand; }
            private set { _rpySettingsFeature.RequestStoredRpyOffsetCommand = value; }
        }

        public ICommand RequestStoredRpyRateCommand
        {
            get { return _rpySettingsFeature.RequestStoredRpyRateCommand; }
            private set { _rpySettingsFeature.RequestStoredRpyRateCommand = value; }
        }

        public string RpyRollOffsetInput
        {
            get { return _rpySettingsFeature.RpyRollOffsetInput; }
            set { _rpySettingsFeature.RpyRollOffsetInput = value; }
        }

        public string RpyPitchOffsetInput
        {
            get { return _rpySettingsFeature.RpyPitchOffsetInput; }
            set { _rpySettingsFeature.RpyPitchOffsetInput = value; }
        }

        public string RpyYawOffsetInput
        {
            get { return _rpySettingsFeature.RpyYawOffsetInput; }
            set { _rpySettingsFeature.RpyYawOffsetInput = value; }
        }

        public string RpyAutoTxRateInput
        {
            get { return _rpySettingsFeature.RpyAutoTxRateInput; }
            set { _rpySettingsFeature.RpyAutoTxRateInput = value; }
        }

        public string CurrentRpyDisplay
        {
            get { return _rpySettingsFeature.CurrentRpyDisplay; }
            private set { _rpySettingsFeature.CurrentRpyDisplay = value; }
        }

        public string CurrentRpyOffsetDisplay
        {
            get { return _rpySettingsFeature.CurrentRpyOffsetDisplay; }
            private set { _rpySettingsFeature.CurrentRpyOffsetDisplay = value; }
        }

        public string CurrentRpyRateDisplay
        {
            get { return _rpySettingsFeature.CurrentRpyRateDisplay; }
            private set { _rpySettingsFeature.CurrentRpyRateDisplay = value; }
        }

        public string RpyOffsetStatusText
        {
            get { return _rpySettingsFeature.RpyOffsetStatusText; }
            private set { _rpySettingsFeature.RpyOffsetStatusText = value; }
        }

        public string RpyRateStatusText
        {
            get { return _rpySettingsFeature.RpyRateStatusText; }
            private set { _rpySettingsFeature.RpyRateStatusText = value; }
        }

        public string WebAgentBuildStatusText
        {
            get { return _rpySettingsFeature.WebAgentBuildStatusText; }
            private set { _rpySettingsFeature.WebAgentBuildStatusText = value; }
        }

        /// <summary>
        /// 자세 보정·전송 주기 설정 명령과 표시 상태를 준비한다.
        /// </summary>
        private void InitializeRpySettings()
        {
            _rpySettingsFeature.InitializeRpySettings();
        }

        /// <summary>
        /// 입력한 자세 보정값을 검증하고 장비에 설정을 요청한다.
        /// </summary>
        private void ApplyRpyOffset()
        {
            _rpySettingsFeature.ApplyRpyOffset();
        }

        /// <summary>
        /// 입력한 자세 자동 전송 주기를 검증하고 장비에 설정을 요청한다.
        /// </summary>
        private void ApplyRpyAutoTxRate()
        {
            _rpySettingsFeature.ApplyRpyAutoTxRate();
        }

        /// <summary>
        /// 장비의 현재 자세 센서 상태를 조회한다.
        /// </summary>
        private void RequestRpyTelemetry()
        {
            _rpySettingsFeature.RequestRpyTelemetry();
        }

        /// <summary>
        /// 장비에 저장된 자세 보정값을 조회한다.
        /// </summary>
        private void RequestStoredRpyOffset()
        {
            _rpySettingsFeature.RequestStoredRpyOffset();
        }

        /// <summary>
        /// 장비에 저장된 자세 전송 주기를 조회한다.
        /// </summary>
        private void RequestStoredRpyRate()
        {
            _rpySettingsFeature.RequestStoredRpyRate();
        }

        internal static string ToHex(byte[] packet)
        {
            return RpySettingsFeature.ToHex(packet);
        }

        internal static void RunOnUiThread(Action action)
        {
            RpySettingsFeature.RunOnUiThread(action);
        }

    }
}
