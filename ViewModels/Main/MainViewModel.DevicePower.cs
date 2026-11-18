using System;
using System.Windows.Input;

namespace OpenCvWpfTracking.ViewModels.Main
{
    /// <summary>
    /// WebAgent EO/IR 전원 제어와 Function 0x31 MCB/SCB 상태 동기화.
    /// </summary>
    public partial class MainViewModel
    {
        internal const byte EoPowerDeviceCode = 0x00;

        internal const byte IrPowerDeviceCode = 0x01;

        internal static readonly TimeSpan DevicePowerCommandTimeout = TimeSpan.FromSeconds(90);

        public string CameraPowerStateHelpText
        {
            get { return _devicePowerFeature.CameraPowerStateHelpText; }
        }

        public ICommand EoPowerOnCommand
        {
            get { return _devicePowerFeature.EoPowerOnCommand; }
            private set { _devicePowerFeature.EoPowerOnCommand = value; }
        }

        public ICommand EoPowerOffCommand
        {
            get { return _devicePowerFeature.EoPowerOffCommand; }
            private set { _devicePowerFeature.EoPowerOffCommand = value; }
        }

        public ICommand IrPowerOnCommand
        {
            get { return _devicePowerFeature.IrPowerOnCommand; }
            private set { _devicePowerFeature.IrPowerOnCommand = value; }
        }

        public ICommand IrPowerOffCommand
        {
            get { return _devicePowerFeature.IrPowerOffCommand; }
            private set { _devicePowerFeature.IrPowerOffCommand = value; }
        }

        public string DevicePowerStatusText
        {
            get { return _devicePowerFeature.DevicePowerStatusText; }
        }

        public string DevicePowerFreshnessText
        {
            get { return _devicePowerFeature.DevicePowerFreshnessText; }
        }

        public string DeviceControlConnectionStatusText
        {
            get { return _devicePowerFeature.DeviceControlConnectionStatusText; }
        }

        public string CurrentGpsPowerText
        {
            get { return _devicePowerFeature.CurrentGpsPowerText; }
        }

        public string EoPowerControlStatusText
        {
            get { return _devicePowerFeature.EoPowerControlStatusText; }
        }

        public string IrPowerControlStatusText
        {
            get { return _devicePowerFeature.IrPowerControlStatusText; }
        }

        public string CurrentEoPowerStatusColor
        {
            get { return _devicePowerFeature.CurrentEoPowerStatusColor; }
        }

        public string CurrentIrPowerStatusColor
        {
            get { return _devicePowerFeature.CurrentIrPowerStatusColor; }
        }

        // 2026-11-03 정비: 영상 재연결/전원 응답 WAIT는 표시만 하고 전원 버튼을 잠그지 않는다.
        public bool IsEoPowerControlEnabled
        {
            get { return _devicePowerFeature.IsEoPowerControlEnabled; }
        }

        public bool IsIrPowerControlEnabled
        {
            get { return _devicePowerFeature.IsIrPowerControlEnabled; }
        }

        public bool IsEoPowerOnEnabled
        {
            get { return _devicePowerFeature.IsEoPowerOnEnabled; }
        }

        public bool IsEoPowerOffEnabled
        {
            get { return _devicePowerFeature.IsEoPowerOffEnabled; }
        }

        public bool IsIrPowerOnEnabled
        {
            get { return _devicePowerFeature.IsIrPowerOnEnabled; }
        }

        public bool IsIrPowerOffEnabled
        {
            get { return _devicePowerFeature.IsIrPowerOffEnabled; }
        }

        internal static string GetLegacyCameraPowerText(string status, string camera)
        {
            return DevicePowerFeature.GetLegacyCameraPowerText(status, camera);
        }

        public string DevicePowerDetailText
        {
            get { return _devicePowerFeature.DevicePowerDetailText; }
        }

        /// <summary>
        /// 장비 전원 제어 명령과 수신 근거의 유효성 확인을 준비한다.
        /// </summary>
        private void InitializeDevicePowerSettings()
        {
            _devicePowerFeature.InitializeDevicePowerSettings();
        }

        /// <summary>
        /// 마지막 전원 응답의 경과 시간을 확인해 오래된 근거의 표시를 갱신한다.
        /// </summary>
        private void DevicePowerFreshnessTimer_Tick(object sender, EventArgs e)
        {
            _devicePowerFeature.DevicePowerFreshnessTimer_Tick(sender, e);
        }
    }
}
