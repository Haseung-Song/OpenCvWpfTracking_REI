using OpenCvWpfTracking.Services.Position;
using System;
using System.Collections.ObjectModel;
using System.Windows.Media.Imaging;
using OpenCvWpfTracking.Models.Main;
using OpenCvWpfTracking.Services.Video;
using static OpenCvWpfTracking.ViewModels.Main.MainViewModel;

namespace OpenCvWpfTracking.ViewModels.Main
{
    /// <summary>
    /// 담당 기능이 요청한 상태와 동작을 실제 소유 기능에 연결한다.
    /// get/set은 원래 상태를 읽고 쓰는 전달 창구이며 별도 상태 사본을 만들지 않는다.
    /// ref 반환은 값의 복사가 아니라 같은 저장 위치를 공유한다. 변경 알림은 원래 담당 기능의 규칙을 따른다.
    /// </summary>
    internal sealed class ZonesFeatureContext : IZonesFeatureContext
    {
        private readonly FeatureComposition _features;

        // 협력 대상만 보관한다. 생성자에서 통신·타이머 시작이나 다른 기능 실행은 하지 않는다.
        internal ZonesFeatureContext(FeatureComposition features)
        {
            _features = features ?? throw new ArgumentNullException(nameof(features));
        }

        RtspSourceOption IZonesFeatureContext.ConnectedEoCtecSource
        {
            get { return _features.ScreenStateFeature._connectedEoCtecSource; }
            set { _features.ScreenStateFeature._connectedEoCtecSource = value; }
        }

        PositionSnapshotService IZonesFeatureContext.PositionSnapshotService
        {
            get { return _features.PositionFeature._positionSnapshotService; }
        }

        ObservableCollection<FireEventRecord> IZonesFeatureContext.AiDetectionEvents
        {
            get { return _features.EventAlertsFeature.AiDetectionEvents; }
        }

        string IZonesFeatureContext.AiRtsp0Address
        {
            get { return _features.PropertiesFeature.AiRtsp0Address; }
            set { _features.PropertiesFeature.AiRtsp0Address = value; }
        }

        string IZonesFeatureContext.AiRtsp1Address
        {
            get { return _features.PropertiesFeature.AiRtsp1Address; }
            set { _features.PropertiesFeature.AiRtsp1Address = value; }
        }

        string IZonesFeatureContext.ControlAgentIp
        {
            get { return _features.PropertiesFeature.ControlAgentIp; }
            set { _features.PropertiesFeature.ControlAgentIp = value; }
        }

        string IZonesFeatureContext.ControlAgentPortText
        {
            get { return _features.PropertiesFeature.ControlAgentPortText; }
            set { _features.PropertiesFeature.ControlAgentPortText = value; }
        }

        /// <summary>
        /// 일반 WEB 줌은 수신·송신 위치를 그대로 사용한다. LA의 기존 방향 반전만 유지한다. 실제 처리는 연결된 기능 객체에 위임한다.
        /// </summary>
        int IZonesFeatureContext.ConvertIrZoomStandardToStatusPosition(int standardPosition)
        {
            return _features.SyncFeature.ConvertIrZoomStandardToStatusPosition(standardPosition);
        }

        BitmapSource IZonesFeatureContext.EOCameraImage
        {
            get { return _features.PropertiesFeature.EOCameraImage; }
            set { _features.PropertiesFeature.EOCameraImage = value; }
        }

        string IZonesFeatureContext.EoConnectionStatusText
        {
            get { return _features.PropertiesFeature.EoConnectionStatusText; }
        }

        string IZonesFeatureContext.EoSourceAddress
        {
            get { return _features.PropertiesFeature.EoSourceAddress; }
            set { _features.PropertiesFeature.EoSourceAddress = value; }
        }

        int IZonesFeatureContext.EoVideoHeight
        {
            get { return _features.PropertiesFeature.EoVideoHeight; }
            set { _features.PropertiesFeature.EoVideoHeight = value; }
        }

        int IZonesFeatureContext.EoVideoWidth
        {
            get { return _features.PropertiesFeature.EoVideoWidth; }
            set { _features.PropertiesFeature.EoVideoWidth = value; }
        }

        ObservableCollection<FireEventRecord> IZonesFeatureContext.FireEvents
        {
            get { return _features.FireEventsFeature.FireEvents; }
        }

        /// <summary>
        /// 현재 EO Zoom 값을 프리셋 공통 범위 0~1000으로 변환한다. 실제 처리는 연결된 기능 객체에 위임한다.
        /// </summary>
        ushort IZonesFeatureContext.GetCurrentPresetStandardZoom()
        {
            return _features.MoveControlFeature.GetCurrentPresetStandardZoom();
        }

        /// <summary>
        /// EO/IR 채널별 수신·표시 성능 측정 상태를 선택한다. 실제 처리는 연결된 기능 객체에 위임한다.
        /// </summary>
        RtspChannelPerformance IZonesFeatureContext.GetRtspPerformance(string streamName)
        {
            return _features.VideoFeature.GetRtspPerformance(streamName);
        }

        BitmapSource IZonesFeatureContext.IRCameraImage
        {
            get { return _features.PropertiesFeature.IRCameraImage; }
            set { _features.PropertiesFeature.IRCameraImage = value; }
        }

        string IZonesFeatureContext.IrConnectionStatusText
        {
            get { return _features.PropertiesFeature.IrConnectionStatusText; }
        }

        string IZonesFeatureContext.IrSourceAddress
        {
            get { return _features.PropertiesFeature.IrSourceAddress; }
            set { _features.PropertiesFeature.IrSourceAddress = value; }
        }

        int IZonesFeatureContext.IrVideoHeight
        {
            get { return _features.PropertiesFeature.IrVideoHeight; }
            set { _features.PropertiesFeature.IrVideoHeight = value; }
        }

        int IZonesFeatureContext.IrVideoWidth
        {
            get { return _features.PropertiesFeature.IrVideoWidth; }
            set { _features.PropertiesFeature.IrVideoWidth = value; }
        }

        bool IZonesFeatureContext.IsOperationCommandEnabled
        {
            get { return _features.PropertiesFeature.IsOperationCommandEnabled; }
        }

        bool IZonesFeatureContext.IsPresetScanControlLocked
        {
            get { return _features.PropertiesFeature.IsPresetScanControlLocked; }
        }

        bool IZonesFeatureContext.IsSelectedControlAgentLaAgent
        {
            get { return _features.RtspSettingsFeature.IsSelectedControlAgentLaAgent; }
        }

        bool IZonesFeatureContext.IsSelectedControlAgentWebAgent
        {
            get { return _features.RtspSettingsFeature.IsSelectedControlAgentWebAgent; }
        }

        ObservableCollection<PresetPointOption> IZonesFeatureContext.LaPresetPoints
        {
            get { return _features.PropertiesFeature.LaPresetPoints; }
        }

        System.Windows.Input.ICommand IZonesFeatureContext.MoveToLaPresetCommand
        {
            get { return _features.PropertiesFeature.MoveToLaPresetCommand; }
        }

        System.Windows.Input.ICommand IZonesFeatureContext.MoveToPresetCommand
        {
            get { return _features.PropertiesFeature.MoveToPresetCommand; }
        }

        /// <summary>
        /// 바인딩 속성 변경 알림 실제 처리는 연결된 기능 객체에 위임한다.
        /// </summary>
        void IZonesFeatureContext.OnPropertyChanged(string propertyName)
        {
            _features.Callbacks.Notify(propertyName);
        }

        PanoramaReferenceState IZonesFeatureContext.PanoramaReference
        {
            get { return _features.PanoramaReferenceFeature.PanoramaReference; }
        }

        /// <summary>
        /// 과거 WEB 줌 저장값은 호환 변환만 적용하고 새 프리셋은 장비 위치를 그대로 사용한다. 실제 처리는 연결된 기능 객체에 위임한다.
        /// </summary>
        int IZonesFeatureContext.PresetIrZoomPosition(PresetPointOption preset)
        {
            return _features.PresetLensFeature.PresetIrZoomPosition(preset);
        }

        ObservableCollection<PresetPointOption> IZonesFeatureContext.PresetPoints
        {
            get { return _features.PropertiesFeature.PresetPoints; }
        }

        /// <summary>
        /// [CTEC Position Inquiry] CGI 송신 후 실제 TCP 9000 Position 응답까지 기다린다. HTTP 200
        /// OK만으로 조회 완료 처리하지 않는다. Zoom은 0x47, Focus는 0x48 응답이 도착해야 한 번의 Inquiry가 끝난다. 실제
        /// 처리는 연결된 기능 객체에 위임한다.
        /// </summary>
        System.Threading.Tasks.Task<int?> IZonesFeatureContext.RequestAndWaitCtecEoPositionAsync(ContinuousMoveType moveType, RtspSourceOption sourceOption, System.Threading.CancellationToken cancellationToken)
        {
            return _features.ContinuousControlFeature.RequestAndWaitCtecEoPositionAsync(moveType, sourceOption, cancellationToken);
        }

        ControlAgentProfileOption IZonesFeatureContext.SelectedControlAgentProfile
        {
            get { return _features.RtspSettingsFeature.SelectedControlAgentProfile; }
            set { _features.RtspSettingsFeature.SelectedControlAgentProfile = value; }
        }

        PresetPointOption IZonesFeatureContext.SelectedLaPresetPoint
        {
            get { return _features.PropertiesFeature.SelectedLaPresetPoint; }
            set { _features.PropertiesFeature.SelectedLaPresetPoint = value; }
        }

        PresetPointOption IZonesFeatureContext.SelectedPresetPoint
        {
            get { return _features.PropertiesFeature.SelectedPresetPoint; }
            set { _features.PropertiesFeature.SelectedPresetPoint = value; }
        }

        Func<FireSmokeScene, bool> IZonesFeatureContext.IsZonePresetCurrentForDisplayCallback
        {
            get { return _features.Callbacks.IsZonePresetCurrentForDisplayCallback; }
        }

        Func<FireSmokeScene, bool> IZonesFeatureContext.IsZonePresetCurrentForDetectionCallback
        {
            get { return _features.Callbacks.IsZonePresetCurrentForDetectionCallback; }
        }
    }
}
