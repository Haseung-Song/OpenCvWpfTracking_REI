using System.Windows.Input;

namespace OpenCvWpfTracking.ViewModels.Main
{
    /// <summary>2026-11-03 정비: WEB 장비의 와이퍼 요청과 수신 온도·동작 상태를 관리한다.</summary>
    public partial class MainViewModel
    {

        public string ThermalColorPaletteButtonText
        {
            get { return _webAgentEquipmentFeature.ThermalColorPaletteButtonText; }
        }

        public string EquipmentTemperatureText
        {
            get { return _webAgentEquipmentFeature.EquipmentTemperatureText; }
        }

        public string WiperRequestText
        {
            get { return _webAgentEquipmentFeature.WiperRequestText; }
        }

        public string WiperSecondsText
        {
            get { return _webAgentEquipmentFeature.WiperSecondsText; }
            set { _webAgentEquipmentFeature.WiperSecondsText = value; }
        }

        public string WiperReportedText
        {
            get { return _webAgentEquipmentFeature.WiperReportedText; }
        }

        public ICommand WiperTimedCommand
        {
            get { return _webAgentEquipmentFeature.WiperTimedCommand; }
        }

        public ICommand WiperContinuousCommand
        {
            get { return _webAgentEquipmentFeature.WiperContinuousCommand; }
        }

        // 정지 요청은 일반 운용 잠금 중에도 현재 연결 장비로 전송할 수 있다.
        public ICommand WiperStopCommand
        {
            get { return _webAgentEquipmentFeature.WiperStopCommand; }
        }

        public ICommand TemperatureQueryCommand
        {
            get { return _webAgentEquipmentFeature.TemperatureQueryCommand; }
        }

        /// <summary>최신 온도 캐시 조회는 새 센서 측정이나 장비 시간 설정이 아니다.</summary>
        private void QueryEquipmentTemperature()
        {
            _webAgentEquipmentFeature.QueryEquipmentTemperature();
        }

    }
}
