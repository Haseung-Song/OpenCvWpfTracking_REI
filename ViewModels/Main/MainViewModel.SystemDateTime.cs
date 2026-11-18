using System;
using System.Windows.Input;

namespace OpenCvWpfTracking.ViewModels.Main
{
    /// <summary>
    /// 2026-10-01: RTC 유지가 불가능한 WebAgent SBC를 위해 시스템 UTC 시간을
    /// 조회하고 PC 현재 시간을 UTC로 변환해 설정하는 GUI 기능을 제공한다.
    /// </summary>
    public partial class MainViewModel
    {

        public ICommand RequestSystemDateTimeCommand
        {
            get { return _systemDateTimeFeature.RequestSystemDateTimeCommand; }
            private set { _systemDateTimeFeature.RequestSystemDateTimeCommand = value; }
        }

        public ICommand SetSystemDateTimeFromPcCommand
        {
            get { return _systemDateTimeFeature.SetSystemDateTimeFromPcCommand; }
            private set { _systemDateTimeFeature.SetSystemDateTimeFromPcCommand = value; }
        }

        public string WebAgentUtcDateTimeText
        {
            get { return _systemDateTimeFeature.WebAgentUtcDateTimeText; }
            private set { _systemDateTimeFeature.WebAgentUtcDateTimeText = value; }
        }

        public string WebAgentLocalDateTimeText
        {
            get { return _systemDateTimeFeature.WebAgentLocalDateTimeText; }
            private set { _systemDateTimeFeature.WebAgentLocalDateTimeText = value; }
        }

        public string PcLocalDateTimeText
        {
            get { return _systemDateTimeFeature.PcLocalDateTimeText; }
            private set { _systemDateTimeFeature.PcLocalDateTimeText = value; }
        }

        public string PcUtcDateTimeText
        {
            get { return _systemDateTimeFeature.PcUtcDateTimeText; }
            private set { _systemDateTimeFeature.PcUtcDateTimeText = value; }
        }

        public string SystemDateTimeStatusText
        {
            get { return _systemDateTimeFeature.SystemDateTimeStatusText; }
            private set { _systemDateTimeFeature.SystemDateTimeStatusText = value; }
        }

        /// <summary>
        /// 장비 시각 조회·설정 명령과 자동 조회 타이머를 준비한다.
        /// </summary>
        private void InitializeSystemDateTimeSettings()
        {
            _systemDateTimeFeature.InitializeSystemDateTimeSettings();
        }

        /// <summary>
        /// 중복 조회를 방지하며 장비 UTC 시각을 요청하고 왕복 지연 측정 시점을 기록한다.
        /// </summary>
        private void RequestSystemDateTime()
        {
            _systemDateTimeFeature.RequestSystemDateTime();
        }

        /// <summary>
        /// 운용자의 명시적 요청으로만 PC UTC 시각을 장비에 설정한다.
        /// </summary>
        private void SetSystemDateTimeFromPc()
        {
            _systemDateTimeFeature.SetSystemDateTimeFromPc();
        }

        /// <summary>
        /// 조회 간격·응답 대기·시간 초과를 확인한 뒤 읽기 전용 시각 조회를 진행한다.
        /// </summary>
        private void SystemTimePollTimer_Tick(object sender, EventArgs e)
        {
            _systemDateTimeFeature.SystemTimePollTimer_Tick(sender, e);
        }

    }
}
