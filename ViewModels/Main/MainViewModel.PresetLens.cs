// 2026-10-16: 프리셋 렌즈 복원과 수신 증거·지연 시간 관리.
// 코드 정비: 한국어 설명과 함수 간격·region을 정리하며 기존 운용 흐름은 유지한다.
namespace OpenCvWpfTracking.ViewModels.Main
{
    public partial class MainViewModel
    {

        /// <summary>자동 초점 또는 상태 미확인으로 저장 초점을 적용하지 않은 채널을 구분한다.</summary>
        public string PresetFocusSkipText
        {
            get { return _presetLensFeature.PresetFocusSkipText; }
        }

        internal const int PresetEoLensTolerance = 25;

        internal const int PresetIrLensTolerance = 35;

        internal const int PresetZoomRestoreTimeoutMs = 45000;

        internal const int PresetFocusRestoreTimeoutMs = 15000;

        internal static bool TryPresetLensValue(string text, out int value)
        {
            return PresetLensFeature.TryPresetLensValue(text, out value);
        }
    }
}
