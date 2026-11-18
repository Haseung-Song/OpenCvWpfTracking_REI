using System.Windows.Input;

namespace OpenCvWpfTracking.ViewModels.Main
{
    /// <summary>2026-11-03 정비: WEB 자동 초점 세션과 보관된 정밀 이동의 안전 처리를 관리한다.</summary>
    public partial class MainViewModel
    {

        public string PrecisionPanSpeed
        {
            get { return _webAgentMotionFocusFeature.PrecisionPanSpeed; }
            set { _webAgentMotionFocusFeature.PrecisionPanSpeed = value; }
        }

        public string PrecisionTiltSpeed
        {
            get { return _webAgentMotionFocusFeature.PrecisionTiltSpeed; }
            set { _webAgentMotionFocusFeature.PrecisionTiltSpeed = value; }
        }

        internal const double PrecisionMaximumSpeed = 60.0;

        /// <summary>AF 화면과 명령의 지원 대상은 선택한 WEB AGENT 프로필로 결정한다.</summary>
        public bool IsAutoFocusPanelVisible
        {
            get { return _webAgentMotionFocusFeature.IsAutoFocusPanelVisible; }
        }

        public bool IsWebAgentFocusControlEnabled
        {
            get { return _webAgentMotionFocusFeature.IsWebAgentFocusControlEnabled; }
            set { _webAgentMotionFocusFeature.IsWebAgentFocusControlEnabled = value; }
        }

        public string PrecisionStatusText
        {
            get { return _webAgentMotionFocusFeature.PrecisionStatusText; }
        }

        public string EoAutoFocusStatusText
        {
            get { return _webAgentMotionFocusFeature.EoAutoFocusStatusText; }
        }

        public string IrAutoFocusStatusText
        {
            get { return _webAgentMotionFocusFeature.IrAutoFocusStatusText; }
        }

        public bool CanUsePrecisionJogButton
        {
            get { return _webAgentMotionFocusFeature.CanUsePrecisionJogButton; }
        }

        // 정밀 이동은 실장비 검증 보류로 프로토콜만 유지하고 실행 경로는 잠근다.
        public bool CanStartPrecisionMotion
        {
            get { return _webAgentMotionFocusFeature.CanStartPrecisionMotion; }
        }

        public ICommand PrecisionAbsoluteCommand
        {
            get { return _webAgentMotionFocusFeature.PrecisionAbsoluteCommand; }
        }

        public ICommand PrecisionStopCommand
        {
            get { return _webAgentMotionFocusFeature.PrecisionStopCommand; }
        }

        public ICommand AutoFocusCommand
        {
            get { return _webAgentMotionFocusFeature.AutoFocusCommand; }
        }

        /// <summary>
        /// 정밀 조그의 보관 경로이며 현재는 상위 실행 허용값이 false여서 시작하지 않는다.
        /// </summary>
        public bool StartPrecisionJog(byte direction)
        {
            return _webAgentMotionFocusFeature.StartPrecisionJog(direction);
        }

        /// <summary>
        /// 진행 중 정밀 동작 상태를 정리하고 필요할 때 양 축 정지를 요청한다.
        /// </summary>
        public void StopPrecisionMotion(string reason, bool force = false)
        {
            _webAgentMotionFocusFeature.StopPrecisionMotion(reason, force);
        }

        /// <summary>
        /// 연결·지원 기능·응답 대기·제어 잠금을 확인해 대상별 AF 명령 실행 가능 여부를 판단한다.
        /// </summary>
        private bool CanExecuteAutoFocus(object parameter)
        {
            return _webAgentMotionFocusFeature.CanExecuteAutoFocus(parameter);
        }

        /// <summary>
        /// 선택한 EO/IR AF 버튼 인수를 조회·설정·단발 초점 요청으로 변환한다.
        /// </summary>
        private void ExecuteAutoFocus(object parameter)
        {
            _webAgentMotionFocusFeature.ExecuteAutoFocus(parameter);
        }

    }
}
