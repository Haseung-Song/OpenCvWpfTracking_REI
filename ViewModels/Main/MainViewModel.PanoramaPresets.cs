using OpenCvWpfTracking.Models.Main;
using System.Threading.Tasks;

namespace OpenCvWpfTracking.ViewModels.Main
{
    public partial class MainViewModel
    {

        public bool IsPanoramaPresetRunning
        {
            get { return _panoramaPresetsFeature.IsPanoramaPresetRunning; }
        }

        /// <summary>연동 중 장비 구분은 주소가 아닌 고정 장비 ID를 사용한다.</summary>
        public string PanoramaPresetContext
        {
            get { return _panoramaPresetsFeature.PanoramaPresetContext; }
        }

        /// <summary>과거 사진·ZERO 기록과 호환되는 저장 해시를 같은 장비에 계속 연결한다.</summary>
        public string PanoramaPresetContextHash
        {
            get { return _panoramaPresetsFeature.PanoramaPresetContextHash; }
        }

        /// <summary>
        /// 현재 연결 장비에 해당하는 기존 프리셋만 번호 순서로 복사한다.
        /// </summary>
        public PresetPointOption[] GetPanoramaPresets()
        {
            return _panoramaPresetsFeature.GetPanoramaPresets();
        }

        public bool CanStartPanoramaPresets
        {
            get { return _panoramaPresetsFeature.CanStartPanoramaPresets; }
        }

        /// <summary>
        /// 번호 클릭은 기존 프리셋의 단일 PTZF 이동 경로를 사용한다.
        /// 이미지 좌표로 새로운 이동 각도를 계산하거나 별도 패킷을 만들지 않는다.
        /// </summary>
        public Task<bool> MovePanoramaPresetAsync(PresetPointOption point)
        {
            return _panoramaPresetsFeature.MovePanoramaPresetAsync(point);
        }

        /// <summary>
        /// 연동 버튼과 공통 정지는 기존 단일 프리셋 이동의 취소 경로를 그대로 사용한다.
        /// </summary>
        public void CancelPanoramaPresets()
        {
            _panoramaPresetsFeature.CancelPanoramaPresets();
        }
    }
}
