using System.Collections.ObjectModel;
using System.Windows.Input;
using OpenCvWpfTracking.Models.Main;

// 2026-10-16: 선택한 LA/WEB 장비의 특수 기능 조회와 요청 연결.
// 코드 정비: 한국어 설명과 함수 간격·region을 정리하며 기존 운용 흐름은 유지한다.
namespace OpenCvWpfTracking.ViewModels.Main
{
    public partial class MainViewModel
    {

        public ObservableCollection<CameraSpecialFeatureRow> EoSpecialFeatures
        {
            get { return _specialFeaturesFeature.EoSpecialFeatures; }
        }

        public ObservableCollection<CameraSpecialFeatureRow> IrSpecialFeatures
        {
            get { return _specialFeaturesFeature.IrSpecialFeatures; }
        }

        public bool CanRefreshSpecialFeatures
        {
            get { return _specialFeaturesFeature.CanRefreshSpecialFeatures; }
        }

        public string SpecialFeatureSummary
        {
            get { return _specialFeaturesFeature.SpecialFeatureSummary; }
        }

        public ICommand RefreshSpecialFeaturesCommand
        {
            get { return _specialFeaturesFeature.RefreshSpecialFeaturesCommand; }
        }

        /// <summary>
        /// EO와 IR 기능 지원 조회를 요청하고 응답 기한을 기록한다.
        /// </summary>
        public void QuerySpecialFeatureSupport()
        {
            _specialFeaturesFeature.QuerySpecialFeatureSupport();
        }

        /// <summary>
        /// 현재 연결 세션의 지원 응답을 반영하고 필요한 상태 조회를 예약한다.
        /// </summary>
        private void ApplySpecialFeatureCapability(byte[] p)
        {
            _specialFeaturesFeature.ApplySpecialFeatureCapability(p);
        }

        /// <summary>
        /// 검증한 기능 상태 응답을 해당 채널의 행에 반영한다.
        /// </summary>
        private void ApplySpecialFeatureState(byte[] p)
        {
            _specialFeaturesFeature.ApplySpecialFeatureState(p);
        }

        /// <summary>
        /// 기능 미지원 또는 실행 거절 응답을 해당 행에 반영한다.
        /// </summary>
        private void ApplySpecialFeatureUnavailable(byte[] p)
        {
            _specialFeaturesFeature.ApplySpecialFeatureUnavailable(p);
        }

        /// <summary>
        /// supported/settable·지원값·운용 잠금을 재검증한 단발 E9만 전송한다.
        /// </summary>
        private void SendSpecialFeature(CameraSpecialFeatureRow row, int value)
        {
            _specialFeaturesFeature.SendSpecialFeature(row, value);
        }

        /// <summary>
        /// 해당 행의 조회 가능 조건을 확인한 후 상태를 요청한다.
        /// </summary>
        private void QuerySpecialFeatureState(CameraSpecialFeatureRow row)
        {
            _specialFeaturesFeature.QuerySpecialFeatureState(row);
        }

    }
}
