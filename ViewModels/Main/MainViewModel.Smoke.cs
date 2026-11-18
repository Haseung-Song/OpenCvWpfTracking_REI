using System.Windows.Input;
using System.Windows.Media;

namespace OpenCvWpfTracking.ViewModels.Main
{
    public partial class MainViewModel
    {

        public ICommand SelectSmokeBoxMode1Command
        {
            get { return _smokeFeature.SelectSmokeBoxMode1Command; }
            private set { _smokeFeature.SelectSmokeBoxMode1Command = value; }
        }

        public ICommand SelectSmokeBoxMode2Command
        {
            get { return _smokeFeature.SelectSmokeBoxMode2Command; }
            private set { _smokeFeature.SelectSmokeBoxMode2Command = value; }
        }

        public int SmokeBoxGroupingMode
        {
            get { return _smokeFeature.SmokeBoxGroupingMode; }
        }

        public Brush SmokeBoxMode1Background
        {
            get { return _smokeFeature.SmokeBoxMode1Background; }
            private set { _smokeFeature.SmokeBoxMode1Background = value; }
        }

        public Brush SmokeBoxMode2Background
        {
            get { return _smokeFeature.SmokeBoxMode2Background; }
            private set { _smokeFeature.SmokeBoxMode2Background = value; }
        }

        private void InitializeSmokeFeatures()
        {
            _smokeFeature.InitializeSmokeFeatures();
        }

        public bool IsSmokeDetectionEnabled
        {
            get { return _smokeFeature.IsSmokeDetectionEnabled; }
            set { _smokeFeature.IsSmokeDetectionEnabled = value; }
        }

        /// <summary>
        /// 2026-08-27: 연결 상태 영역에서 연기 영상처리 사용 여부를 FIRE와 분리해 표시한다.
        /// </summary>
        public string SmokePowerStatusText
        {
            get { return _smokeFeature.SmokePowerStatusText; }
        }

        /// <summary>
        /// 2026-09-03: 실시간 EO/IR의 후보 마스크와 탈락 단계를 TEST와 같은 형식으로 기록한다.
        /// 기본 OFF이며 켤 때마다 하나의 Viewer 세션 아래 EO/IR 폴더를 새로 만든다.
        /// </summary>
        public bool IsSmokeDiagnosticEnabled
        {
            get { return _smokeFeature.IsSmokeDiagnosticEnabled; }
            set { _smokeFeature.IsSmokeDiagnosticEnabled = value; }
        }

        public string SmokeDiagnosticPathText
        {
            get { return _smokeFeature.SmokeDiagnosticPathText; }
            private set { _smokeFeature.SmokeDiagnosticPathText = value; }
        }

        /// <summary>
        /// 2026-08-27: 0=AUTO, 1=EO, 2=IR 보조 후보로 제한한다.
        /// </summary>
        public int SmokeDetectionSourceIndex
        {
            get { return _smokeFeature.SmokeDetectionSourceIndex; }
            set { _smokeFeature.SmokeDetectionSourceIndex = value; }
        }

        /// <summary>
        /// 2026-08-27: 0=SENSITIVE, 1=BALANCED, 2=STRICT 감도 프리셋이다.
        /// </summary>
        public int SmokeSensitivityIndex
        {
            get { return _smokeFeature.SmokeSensitivityIndex; }
            set { _smokeFeature.SmokeSensitivityIndex = value; }
        }

        public double SmokeMinimumAreaRatio
        {
            get { return _smokeFeature.SmokeMinimumAreaRatio; }
            set { _smokeFeature.SmokeMinimumAreaRatio = value; }
        }

        public double SmokeChangeThresholdRatio
        {
            get { return _smokeFeature.SmokeChangeThresholdRatio; }
            set { _smokeFeature.SmokeChangeThresholdRatio = value; }
        }

    }
}
