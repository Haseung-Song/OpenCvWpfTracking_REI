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

        RtspSourceOption IZonesFeatureContext._connectedEoCtecSource
        {
            get { return _features._screenStateFeature._connectedEoCtecSource; }
            set { _features._screenStateFeature._connectedEoCtecSource = value; }
        }

        PositionSnapshotService IZonesFeatureContext._positionSnapshotService
        {
            get { return _features._positionFeature._positionSnapshotService; }
        }

        ObservableCollection<FireEventRecord> IZonesFeatureContext.AiDetectionEvents
        {
            get { return _features._eventAlertsFeature.AiDetectionEvents; }
        }

        string IZonesFeatureContext.AiRtsp0Address
        {
            get { return _features._propertiesFeature.AiRtsp0Address; }
            set { _features._propertiesFeature.AiRtsp0Address = value; }
        }

        string IZonesFeatureContext.AiRtsp1Address
        {
            get { return _features._propertiesFeature.AiRtsp1Address; }
            set { _features._propertiesFeature.AiRtsp1Address = value; }
        }

        string IZonesFeatureContext.ControlAgentIp
        {
            get { return _features._propertiesFeature.ControlAgentIp; }
            set { _features._propertiesFeature.ControlAgentIp = value; }
        }

        string IZonesFeatureContext.ControlAgentPortText
        {
            get { return _features._propertiesFeature.ControlAgentPortText; }
            set { _features._propertiesFeature.ControlAgentPortText = value; }
        }

        /// <summary>
        /// 일반 WEB 줌은 수신·송신 위치를 그대로 사용한다. LA의 기존 방향 반전만 유지한다. 실제 처리는 연결된 기능 객체에 위임한다.
        /// </summary>
        int IZonesFeatureContext.ConvertIrZoomStandardToStatusPosition(int standardPosition)
        {
            return _features._syncFeature.ConvertIrZoomStandardToStatusPosition(standardPosition);
        }

        BitmapSource IZonesFeatureContext.EOCameraImage
        {
            get { return _features._propertiesFeature.EOCameraImage; }
            set { _features._propertiesFeature.EOCameraImage = value; }
        }

        string IZonesFeatureContext.EoConnectionStatusText
        {
            get { return _features._propertiesFeature.EoConnectionStatusText; }
        }

        string IZonesFeatureContext.EoSourceAddress
        {
            get { return _features._propertiesFeature.EoSourceAddress; }
            set { _features._propertiesFeature.EoSourceAddress = value; }
        }

        int IZonesFeatureContext.EoVideoHeight
        {
            get { return _features._propertiesFeature.EoVideoHeight; }
            set { _features._propertiesFeature.EoVideoHeight = value; }
        }

        int IZonesFeatureContext.EoVideoWidth
        {
            get { return _features._propertiesFeature.EoVideoWidth; }
            set { _features._propertiesFeature.EoVideoWidth = value; }
        }

        ObservableCollection<FireEventRecord> IZonesFeatureContext.FireEvents
        {
            get { return _features._fireEventsFeature.FireEvents; }
        }

        /// <summary>
        /// 현재 EO Zoom 값을 프리셋 공통 범위 0~1000으로 변환한다. 실제 처리는 연결된 기능 객체에 위임한다.
        /// </summary>
        ushort IZonesFeatureContext.GetCurrentPresetStandardZoom()
        {
            return _features._moveControlFeature.GetCurrentPresetStandardZoom();
        }

        /// <summary>
        /// EO/IR 채널별 수신·표시 성능 측정 상태를 선택한다. 실제 처리는 연결된 기능 객체에 위임한다.
        /// </summary>
        RtspChannelPerformance IZonesFeatureContext.GetRtspPerformance(string streamName)
        {
            return _features._videoFeature.GetRtspPerformance(streamName);
        }

        BitmapSource IZonesFeatureContext.IRCameraImage
        {
            get { return _features._propertiesFeature.IRCameraImage; }
            set { _features._propertiesFeature.IRCameraImage = value; }
        }

        string IZonesFeatureContext.IrConnectionStatusText
        {
            get { return _features._propertiesFeature.IrConnectionStatusText; }
        }

        string IZonesFeatureContext.IrSourceAddress
        {
            get { return _features._propertiesFeature.IrSourceAddress; }
            set { _features._propertiesFeature.IrSourceAddress = value; }
        }

        int IZonesFeatureContext.IrVideoHeight
        {
            get { return _features._propertiesFeature.IrVideoHeight; }
            set { _features._propertiesFeature.IrVideoHeight = value; }
        }

        int IZonesFeatureContext.IrVideoWidth
        {
            get { return _features._propertiesFeature.IrVideoWidth; }
            set { _features._propertiesFeature.IrVideoWidth = value; }
        }

        bool IZonesFeatureContext.IsOperationCommandEnabled
        {
            get { return _features._propertiesFeature.IsOperationCommandEnabled; }
        }

        bool IZonesFeatureContext.IsPresetScanControlLocked
        {
            get { return _features._propertiesFeature.IsPresetScanControlLocked; }
        }

        bool IZonesFeatureContext.IsSelectedControlAgentLaAgent
        {
            get { return _features._rtspSettingsFeature.IsSelectedControlAgentLaAgent; }
        }

        bool IZonesFeatureContext.IsSelectedControlAgentWebAgent
        {
            get { return _features._rtspSettingsFeature.IsSelectedControlAgentWebAgent; }
        }

        ObservableCollection<PresetPointOption> IZonesFeatureContext.LaPresetPoints
        {
            get { return _features._propertiesFeature.LaPresetPoints; }
        }

        System.Windows.Input.ICommand IZonesFeatureContext.MoveToLaPresetCommand
        {
            get { return _features._propertiesFeature.MoveToLaPresetCommand; }
        }

        System.Windows.Input.ICommand IZonesFeatureContext.MoveToPresetCommand
        {
            get { return _features._propertiesFeature.MoveToPresetCommand; }
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
            get { return _features._panoramaReferenceFeature.PanoramaReference; }
        }

        /// <summary>
        /// 과거 WEB 줌 저장값은 호환 변환만 적용하고 새 프리셋은 장비 위치를 그대로 사용한다. 실제 처리는 연결된 기능 객체에 위임한다.
        /// </summary>
        int IZonesFeatureContext.PresetIrZoomPosition(PresetPointOption preset)
        {
            return _features._presetLensFeature.PresetIrZoomPosition(preset);
        }

        ObservableCollection<PresetPointOption> IZonesFeatureContext.PresetPoints
        {
            get { return _features._propertiesFeature.PresetPoints; }
        }

        /// <summary>
        /// [CTEC Position Inquiry] CGI 송신 후 실제 TCP 9000 Position 응답까지 기다린다. HTTP 200
        /// OK만으로 조회 완료 처리하지 않는다. Zoom은 0x47, Focus는 0x48 응답이 도착해야 한 번의 Inquiry가 끝난다. 실제
        /// 처리는 연결된 기능 객체에 위임한다.
        /// </summary>
        System.Threading.Tasks.Task<int?> IZonesFeatureContext.RequestAndWaitCtecEoPositionAsync(ContinuousMoveType moveType, RtspSourceOption sourceOption, System.Threading.CancellationToken cancellationToken)
        {
            return _features._continuousControlFeature.RequestAndWaitCtecEoPositionAsync(moveType, sourceOption, cancellationToken);
        }

        ControlAgentProfileOption IZonesFeatureContext.SelectedControlAgentProfile
        {
            get { return _features._rtspSettingsFeature.SelectedControlAgentProfile; }
            set { _features._rtspSettingsFeature.SelectedControlAgentProfile = value; }
        }

        PresetPointOption IZonesFeatureContext.SelectedLaPresetPoint
        {
            get { return _features._propertiesFeature.SelectedLaPresetPoint; }
            set { _features._propertiesFeature.SelectedLaPresetPoint = value; }
        }

        PresetPointOption IZonesFeatureContext.SelectedPresetPoint
        {
            get { return _features._propertiesFeature.SelectedPresetPoint; }
            set { _features._propertiesFeature.SelectedPresetPoint = value; }
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
