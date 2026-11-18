using System.Threading.Tasks;

namespace OpenCvWpfTracking.ViewModels.Main
{
    /// <summary>
    /// EO/IR Zoom 및 Focus 동기화 작업을 관리한다.
    ///
    /// MainViewModel을 기능 영역별로 나눈 partial class이다.
    /// 모든 partial 파일은 실행 시 하나의 MainViewModel 타입으로 합쳐진다.
    /// </summary>
    public partial class MainViewModel
    {
        /// <summary>
        /// SelectPreviousZoomSyncLevel 동작 수행 함수.
        /// </summary>
        private void SelectPreviousZoomSyncLevel()
        {
            _syncFeature.SelectPreviousZoomSyncLevel();
        }

        /// <summary>
        /// SelectNextZoomSyncLevel 동작 수행 함수.
        /// </summary>
        private void SelectNextZoomSyncLevel()
        {
            _syncFeature.SelectNextZoomSyncLevel();
        }

        /// <summary>
        /// WEB AGENT에서 선택한 11단계(LEVEL 0~10)의 실측 화각 대응 위치를 적용한다.
        ///
        /// EO/IR 렌즈별 목표 위치는 각각 계산하며 동일한 값을 단순 복사하지 않는다.
        /// LA 구성은 동기화 탭과 실행 경로를 차단한다.
        /// </summary>
        private Task ApplySelectedZoomSyncLevelAsync()
        {
            return _syncFeature.ApplySelectedZoomSyncLevelAsync();
        }

        /// <summary>
        /// StopZoomSyncAsync 중지 함수.
        /// </summary>
        private Task StopZoomSyncAsync()
        {
            return _syncFeature.StopZoomSyncAsync();
        }

        internal static int ConvertStandardZoomToCtecRaw(int standardPosition)
        {
            return SyncFeature.ConvertStandardZoomToCtecRaw(standardPosition);
        }

        internal static int ConvertStandardFocusToCtecRaw(int standardPosition)
        {
            return SyncFeature.ConvertStandardFocusToCtecRaw(standardPosition);
        }
    }
}
