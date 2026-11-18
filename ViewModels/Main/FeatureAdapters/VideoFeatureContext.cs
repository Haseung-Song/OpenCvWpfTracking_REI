using OpenCvSharp;
using OpenCvWpfTracking.Services.Video;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using static OpenCvWpfTracking.ViewModels.Main.MainViewModel;

namespace OpenCvWpfTracking.ViewModels.Main
{
    /// <summary>
    /// 담당 기능이 요청한 상태와 동작을 실제 소유 기능에 연결한다.
    /// get/set은 원래 상태를 읽고 쓰는 전달 창구이며 별도 상태 사본을 만들지 않는다.
    /// ref 반환은 값의 복사가 아니라 같은 저장 위치를 공유한다. 변경 알림은 원래 담당 기능의 규칙을 따른다.
    /// </summary>
    internal sealed class VideoFeatureContext : IVideoFeatureContext
    {
        private readonly FeatureComposition _features;

        // 협력 대상만 보관한다. 생성자에서 통신·타이머 시작이나 다른 기능 실행은 하지 않는다.
        internal VideoFeatureContext(FeatureComposition features)
        {
            _features = features ?? throw new ArgumentNullException(nameof(features));
        }

        Models.Main.RtspSourceOption IVideoFeatureContext._activeEoCtecSource
        {
            get { return _features._screenStateFeature._activeEoCtecSource; }
            set { _features._screenStateFeature._activeEoCtecSource = value; }
        }

        Models.Main.RtspSourceOption IVideoFeatureContext._connectedEoCtecSource
        {
            get { return _features._screenStateFeature._connectedEoCtecSource; }
            set { _features._screenStateFeature._connectedEoCtecSource = value; }
        }

        CancellationTokenSource IVideoFeatureContext._controlAgentReconnectCts
        {
            get { return _features._screenStateFeature._controlAgentReconnectCts; }
            set { _features._screenStateFeature._controlAgentReconnectCts = value; }
        }

        Services.Communication.ICtecCameraResponseServicePort IVideoFeatureContext._ctecCameraResponseService
        {
            get { return _features.Runtime._ctecCameraResponseService; }
        }

        CancellationTokenSource IVideoFeatureContext._cts
        {
            get { return _features._screenStateFeature._cts; }
            set { _features._screenStateFeature._cts = value; }
        }

        byte IVideoFeatureContext._currentCtecEoFocusMode
        {
            get { return _features._screenStateFeature._currentCtecEoFocusMode; }
            set { _features._screenStateFeature._currentCtecEoFocusMode = value; }
        }

        ushort IVideoFeatureContext._currentCtecEoFocusPosition
        {
            get { return _features._screenStateFeature._currentCtecEoFocusPosition; }
            set { _features._screenStateFeature._currentCtecEoFocusPosition = value; }
        }

        ushort IVideoFeatureContext._currentCtecEoZoomPosition
        {
            get { return _features._screenStateFeature._currentCtecEoZoomPosition; }
            set { _features._screenStateFeature._currentCtecEoZoomPosition = value; }
        }

        short IVideoFeatureContext._currentEoFocus
        {
            get { return _features._screenStateFeature._currentEoFocus; }
            set { _features._screenStateFeature._currentEoFocus = value; }
        }

        short IVideoFeatureContext._currentEoZoom
        {
            get { return _features._screenStateFeature._currentEoZoom; }
            set { _features._screenStateFeature._currentEoZoom = value; }
        }

        ushort IVideoFeatureContext._currentIrFocus
        {
            get { return _features._screenStateFeature._currentIrFocus; }
            set { _features._screenStateFeature._currentIrFocus = value; }
        }

        ushort IVideoFeatureContext._currentIrZoom
        {
            get { return _features._screenStateFeature._currentIrZoom; }
            set { _features._screenStateFeature._currentIrZoom = value; }
        }

        KeyboardPanTiltDirection IVideoFeatureContext._currentKeyboardPanTiltDirection
        {
            get { return _features._screenStateFeature._currentKeyboardPanTiltDirection; }
            set { _features._screenStateFeature._currentKeyboardPanTiltDirection = value; }
        }

        ContinuousMoveType IVideoFeatureContext._currentMoveType
        {
            get { return _features._screenStateFeature._currentMoveType; }
            set { _features._screenStateFeature._currentMoveType = value; }
        }

        double IVideoFeatureContext._currentPan
        {
            get { return _features._screenStateFeature._currentPan; }
            set { _features._screenStateFeature._currentPan = value; }
        }

        byte IVideoFeatureContext._currentPowerStatus
        {
            get { return _features._screenStateFeature._currentPowerStatus; }
            set { _features._screenStateFeature._currentPowerStatus = value; }
        }

        double IVideoFeatureContext._currentTilt
        {
            get { return _features._screenStateFeature._currentTilt; }
            set { _features._screenStateFeature._currentTilt = value; }
        }

        ref int IVideoFeatureContext._deviceConnectRequested
        {
            get { return ref _features._rtspSettingsFeature._deviceConnectRequested; }
        }

        IFFmpegDecoderServicePort IVideoFeatureContext._eoDecoder
        {
            get { return _features.Runtime._eoDecoder; }
        }

        ThermalFireDetectionService IVideoFeatureContext._eoFireDetectionService
        {
            get { return _features._thermalFeature._eoFireDetectionService; }
        }

        ref long IVideoFeatureContext._eoLensReceivedTicks
        {
            get { return ref _features._inputCoordinatorFeature._eoLensReceivedTicks; }
        }

        ref long IVideoPerformanceContext._focusPropertyChangedCount
        {
            get { return ref _features._screenStateFeature._focusPropertyChangedCount; }
        }

        ref long IVideoPerformanceContext._function01ReceiveCount
        {
            get { return ref _features._screenStateFeature._function01ReceiveCount; }
        }

        IFFmpegDecoderServicePort IVideoFeatureContext._irDecoder
        {
            get { return _features.Runtime._irDecoder; }
        }

        ThermalFireDetectionService IVideoFeatureContext._irFireDetectionService
        {
            get { return _features._thermalFeature._irFireDetectionService; }
        }

        ref long IVideoFeatureContext._irLensReceivedTicks
        {
            get { return ref _features._inputCoordinatorFeature._irLensReceivedTicks; }
        }

        bool IVideoFeatureContext._isDeviceConnectionRequested
        {
            get { return _features._screenStateFeature._isDeviceConnectionRequested; }
            set { _features._screenStateFeature._isDeviceConnectionRequested = value; }
        }

        ref int IVideoFeatureContext._isEoFrameDispatchPending
        {
            get { return ref _features._screenStateFeature._isEoFrameDispatchPending; }
        }

        bool IVideoFeatureContext._isEoFrameDisplayed
        {
            get { return _features._screenStateFeature._isEoFrameDisplayed; }
            set { _features._screenStateFeature._isEoFrameDisplayed = value; }
        }

        bool IVideoFeatureContext._isEoRtspAddressValid
        {
            get { return _features._controlAgentFeature._isEoRtspAddressValid; }
            set { _features._controlAgentFeature._isEoRtspAddressValid = value; }
        }

        ref int IVideoFeatureContext._isIrFrameDispatchPending
        {
            get { return ref _features._screenStateFeature._isIrFrameDispatchPending; }
        }

        bool IVideoFeatureContext._isIrFrameDisplayed
        {
            get { return _features._screenStateFeature._isIrFrameDisplayed; }
            set { _features._screenStateFeature._isIrFrameDisplayed = value; }
        }

        bool IVideoFeatureContext._isIrRtspAddressValid
        {
            get { return _features._controlAgentFeature._isIrRtspAddressValid; }
            set { _features._controlAgentFeature._isIrRtspAddressValid = value; }
        }

        bool IVideoFeatureContext._isVideoConnecting
        {
            get { return _features._screenStateFeature._isVideoConnecting; }
            set { _features._screenStateFeature._isVideoConnecting = value; }
        }

        Services.Communication.ILAPacketParserPort IVideoFeatureContext._laPacketParser
        {
            get { return _features.Runtime._laPacketParser; }
        }

        ref double? IVideoFeatureContext._lastPanAbsoluteTarget
        {
            get { return ref _features._screenStateFeature._lastPanAbsoluteTarget; }
        }

        Services.Communication.ITcpClientServicePort IVideoFeatureContext._laTcpService
        {
            get { return _features.Runtime._laTcpService; }
        }

        ref long IVideoPerformanceContext._panPropertyChangedCount
        {
            get { return ref _features._screenStateFeature._panPropertyChangedCount; }
        }

        ref long IVideoPerformanceContext._panStartTxCount
        {
            get { return ref _features._screenStateFeature._panStartTxCount; }
        }

        ref long IVideoPerformanceContext._panStopTxCount
        {
            get { return ref _features._screenStateFeature._panStopTxCount; }
        }

        ref long IVideoPerformanceContext._propertyChangedCount
        {
            get { return ref _features._screenStateFeature._propertyChangedCount; }
        }

        ref long IVideoPerformanceContext._ptzfStatusUpdateCount
        {
            get { return ref _features._screenStateFeature._ptzfStatusUpdateCount; }
        }

        string IVideoPerformanceContext._ptzPerformanceScenario
        {
            get { return _features._screenStateFeature._ptzPerformanceScenario; }
            set { _features._screenStateFeature._ptzPerformanceScenario = value; }
        }

        string IVideoFeatureContext._requestedControlIp
        {
            get { return _features._rtspSettingsFeature._requestedControlIp; }
            set { _features._rtspSettingsFeature._requestedControlIp = value; }
        }

        string IVideoFeatureContext._requestedControlPort
        {
            get { return _features._rtspSettingsFeature._requestedControlPort; }
            set { _features._rtspSettingsFeature._requestedControlPort = value; }
        }

        ControlAgentProfileOption IVideoFeatureContext._requestedControlProfile
        {
            get { return _features._rtspSettingsFeature._requestedControlProfile; }
            set { _features._rtspSettingsFeature._requestedControlProfile = value; }
        }

        string IVideoFeatureContext._requestedEoAddress
        {
            get { return _features._rtspSettingsFeature._requestedEoAddress; }
            set { _features._rtspSettingsFeature._requestedEoAddress = value; }
        }

        string IVideoFeatureContext._requestedIrAddress
        {
            get { return _features._rtspSettingsFeature._requestedIrAddress; }
            set { _features._rtspSettingsFeature._requestedIrAddress = value; }
        }

        ref long IVideoPerformanceContext._tiltPropertyChangedCount
        {
            get { return ref _features._screenStateFeature._tiltPropertyChangedCount; }
        }

        ref long IVideoPerformanceContext._tiltStartTxCount
        {
            get { return ref _features._screenStateFeature._tiltStartTxCount; }
        }

        ref long IVideoPerformanceContext._tiltStopTxCount
        {
            get { return ref _features._screenStateFeature._tiltStopTxCount; }
        }

        long[] IVideoFeatureContext._videoCenterFrameTicks
        {
            get { return _features._cameraFeatureControlFeature._videoCenterFrameTicks; }
        }

        CancellationTokenSource IVideoFeatureContext._videoReconnectCts
        {
            get { return _features._screenStateFeature._videoReconnectCts; }
            set { _features._screenStateFeature._videoReconnectCts = value; }
        }

        ref int IVideoFeatureContext._visionSettingsGeneration
        {
            get { return ref _features._fireEventsFeature._visionSettingsGeneration; }
        }

        ref long IVideoPerformanceContext._zoomPropertyChangedCount
        {
            get { return ref _features._screenStateFeature._zoomPropertyChangedCount; }
        }

        ref long IVideoPerformanceContext._zoomStartTxCount
        {
            get { return ref _features._screenStateFeature._zoomStartTxCount; }
        }

        ref long IVideoPerformanceContext._zoomStopTxCount
        {
            get { return ref _features._screenStateFeature._zoomStopTxCount; }
        }

        string IVideoFeatureContext.AiPowerStatusText
        {
            get { return _features._propertiesFeature.AiPowerStatusText; }
            set { _features._propertiesFeature.AiPowerStatusText = value; }
        }

        void IVideoFeatureContext.ApplyControlAgentPanCoordinateMode(ControlAgentProfileOption profile)
        {
            _features._rtspSettingsFeature.ApplyControlAgentPanCoordinateMode(profile);
        }

        void IVideoFeatureContext.CancelMoveControlPanOperation()
        {
            _features._moveControlFeature.CancelMoveControlPanOperation();
        }

        Services.Position.ZoneFrameContext IVideoFeatureContext.CaptureZoneFrame(string camera)
        {
            return _features._zonesFeature.CaptureZoneFrame(camera);
        }

        void IVideoFeatureContext.ClearActivePanTiltAbsoluteMove()
        {
            _features._moveControlFeature.ClearActivePanTiltAbsoluteMove();
        }

        void IVideoFeatureContext.ClearKeyboardPanTiltPressedState()
        {
            _features._continuousControlFeature.ClearKeyboardPanTiltPressedState();
        }

        Task<bool> IVideoFeatureContext.ConnectLaAsync()
        {
            return _features._controlAgentFeature.ConnectLaAsync();
        }

        string IVideoFeatureContext.ControlAgentIp
        {
            get { return _features._propertiesFeature.ControlAgentIp; }
            set { _features._propertiesFeature.ControlAgentIp = value; }
        }

        string IVideoFeatureContext.ControlAgentPortText
        {
            get { return _features._propertiesFeature.ControlAgentPortText; }
            set { _features._propertiesFeature.ControlAgentPortText = value; }
        }

        void IVideoFeatureContext.DisconnectAiAgent()
        {
            _features._aiFeature.DisconnectAiAgent();
        }

        BitmapSource IVideoFeatureContext.EOCameraImage
        {
            get { return _features._propertiesFeature.EOCameraImage; }
            set { _features._propertiesFeature.EOCameraImage = value; }
        }

        System.Collections.ObjectModel.ObservableCollection<Models.AI.AiDetectionBox> IVideoFeatureContext.EoDetectionBoxes
        {
            get { return _features._propertiesFeature.EoDetectionBoxes; }
        }

        System.Collections.ObjectModel.ObservableCollection<Models.AI.VisionDetectionBox> IVideoFeatureContext.EoFireSmokeDetectionBoxes
        {
            get { return _features._propertiesFeature.EoFireSmokeDetectionBoxes; }
        }

        string IVideoFeatureContext.EoSourceAddress
        {
            get { return _features._propertiesFeature.EoSourceAddress; }
            set { _features._propertiesFeature.EoSourceAddress = value; }
        }

        string IVideoFeatureContext.EoStatusText
        {
            get { return _features._propertiesFeature.EoStatusText; }
            set { _features._propertiesFeature.EoStatusText = value; }
        }

        int IVideoFeatureContext.EoVideoHeight
        {
            get { return _features._propertiesFeature.EoVideoHeight; }
            set { _features._propertiesFeature.EoVideoHeight = value; }
        }

        int IVideoFeatureContext.EoVideoWidth
        {
            get { return _features._propertiesFeature.EoVideoWidth; }
            set { _features._propertiesFeature.EoVideoWidth = value; }
        }

        IList<Rect> IVideoFeatureContext.GetRecentAiFireCandidates(bool isInfrared)
        {
            return _features._smokeFeature.GetRecentAiFireCandidates(isInfrared);
        }

        bool IVideoFeatureContext.HasActiveDeviceSession
        {
            get { return _features._rtspSettingsFeature.HasActiveDeviceSession; }
        }

        string IVideoFeatureContext.HomeZeroStatusText
        {
            get { return _features._propertiesFeature.HomeZeroStatusText; }
            set { _features._propertiesFeature.HomeZeroStatusText = value; }
        }

        void IVideoFeatureContext.InitializeThermalBlackHotAfterDeviceConnected()
        {
            _features._thermalFeature.InitializeThermalBlackHotAfterDeviceConnected();
        }

        void IVideoFeatureContext.InitializeWebAgentSession()
        {
            _features._webAgentProtocolFeature.InitializeWebAgentSession();
        }

        BitmapSource IVideoFeatureContext.IRCameraImage
        {
            get { return _features._propertiesFeature.IRCameraImage; }
            set { _features._propertiesFeature.IRCameraImage = value; }
        }

        System.Collections.ObjectModel.ObservableCollection<Models.AI.AiDetectionBox> IVideoFeatureContext.IrDetectionBoxes
        {
            get { return _features._propertiesFeature.IrDetectionBoxes; }
        }

        System.Collections.ObjectModel.ObservableCollection<Models.AI.VisionDetectionBox> IVideoFeatureContext.IrFireSmokeDetectionBoxes
        {
            get { return _features._propertiesFeature.IrFireSmokeDetectionBoxes; }
        }

        string IVideoFeatureContext.IrSourceAddress
        {
            get { return _features._propertiesFeature.IrSourceAddress; }
            set { _features._propertiesFeature.IrSourceAddress = value; }
        }

        string IVideoFeatureContext.IrStatusText
        {
            get { return _features._propertiesFeature.IrStatusText; }
            set { _features._propertiesFeature.IrStatusText = value; }
        }

        int IVideoFeatureContext.IrVideoHeight
        {
            get { return _features._propertiesFeature.IrVideoHeight; }
            set { _features._propertiesFeature.IrVideoHeight = value; }
        }

        int IVideoFeatureContext.IrVideoWidth
        {
            get { return _features._propertiesFeature.IrVideoWidth; }
            set { _features._propertiesFeature.IrVideoWidth = value; }
        }

        bool IVideoFeatureContext.IsCrosshairVisible
        {
            get { return _features._propertiesFeature.IsCrosshairVisible; }
            set { _features._propertiesFeature.IsCrosshairVisible = value; }
        }

        bool IVideoFeatureContext.IsDeviceSettingsLocked
        {
            get { return _features._rtspSettingsFeature.IsDeviceSettingsLocked; }
        }

        bool IVideoFeatureContext.IsFireSmokeFrameAnalysisAllowed()
        {
            return _features._smokeFeature.IsFireSmokeFrameAnalysisAllowed();
        }

        bool IVideoFeatureContext.IsFireSourceEnabled(bool isInfrared)
        {
            return _features._thermalFeature.IsFireSourceEnabled(isInfrared);
        }

        bool IVideoFeatureContext.IsPanoramaCaptureRunning
        {
            get { return _features._panoramaFeature.IsPanoramaCaptureRunning; }
            set { _features._panoramaFeature.IsPanoramaCaptureRunning = value; }
        }

        bool IVideoFeatureContext.IsPresetScanRunning
        {
            get { return _features._propertiesFeature.IsPresetScanRunning; }
            set { _features._propertiesFeature.IsPresetScanRunning = value; }
        }

        bool IVideoFeatureContext.IsRooftopStatusSelected
        {
            get { return _features._propertiesFeature.IsRooftopStatusSelected; }
        }

        bool IVideoFeatureContext.IsSmokeDetectionEnabled
        {
            get { return _features._smokeFeature.IsSmokeDetectionEnabled; }
            set { _features._smokeFeature.IsSmokeDetectionEnabled = value; }
        }

        bool IVideoFeatureContext.IsThermalFireDetectionEnabled
        {
            get { return _features._thermalFeature.IsThermalFireDetectionEnabled; }
            set { _features._thermalFeature.IsThermalFireDetectionEnabled = value; }
        }

        Task IVideoFeatureContext.MoveHomePositionAsync()
        {
            return _features._moveControlFeature.MoveHomePositionAsync();
        }

        void IVideoFeatureContext.NotifyEoCurrentStatusChanged(bool panChanged, bool tiltChanged, bool zoomChanged, bool focusChanged, bool powerChanged)
        {
            _features._controlAgentFeature.NotifyEoCurrentStatusChanged(panChanged, tiltChanged, zoomChanged, focusChanged, powerChanged);
        }

        void IVideoFeatureContext.NotifyIrCurrentStatusChanged()
        {
            _features._controlAgentFeature.NotifyIrCurrentStatusChanged();
        }

        string IVideoFeatureContext.PresetCommandStatusText
        {
            get { return _features._propertiesFeature.PresetCommandStatusText; }
            set { _features._propertiesFeature.PresetCommandStatusText = value; }
        }

        SmokeDetectionResult IVideoFeatureContext.ProcessSmokeFrame(Mat frame, string streamName, Rect fireCandidateRect)
        {
            return _features._smokeFeature.ProcessSmokeFrame(frame, streamName, fireCandidateRect);
        }

        void IVideoFeatureContext.ResetDevicePowerStatus()
        {
            _features._devicePowerFeature.ResetDevicePowerStatus();
        }

        void IVideoFeatureContext.ResetDigitalZoomSession()
        {
            _features._cameraFeatureControlFeature.ResetDigitalZoomSession();
        }

        void IVideoFeatureContext.ResetFireSmokeFrameAnalysis(string reason)
        {
            _features._smokeFeature.ResetFireSmokeFrameAnalysis(reason);
        }

        void IVideoFeatureContext.SaveRtspCommunicationSettings()
        {
            _features._rtspSettingsFeature.SaveRtspCommunicationSettings();
        }

        ControlAgentProfileOption IVideoFeatureContext.SelectedControlAgentProfile
        {
            get { return _features._rtspSettingsFeature.SelectedControlAgentProfile; }
            set { _features._rtspSettingsFeature.SelectedControlAgentProfile = value; }
        }

        string IVideoFeatureContext.SessionEoAddress
        {
            get { return _features._rtspSettingsFeature.SessionEoAddress; }
        }

        string IVideoFeatureContext.SessionIrAddress
        {
            get { return _features._rtspSettingsFeature.SessionIrAddress; }
        }

        void IVideoFeatureContext.SetControlAgentConnectionStatus(string statusText, string statusColor, CancellationToken token)
        {
            _features._controlAgentFeature.SetControlAgentConnectionStatus(statusText, statusColor, token);
        }

        void IVideoFeatureContext.SetLocalFireSmokeDetectionState(int rtspIndex, bool hasFire, bool hasSmoke)
        {
            _features._propertiesFeature.SetLocalFireSmokeDetectionState(rtspIndex, hasFire, hasSmoke);
        }

        void IVideoFeatureContext.SetRawEoPanoramaFrame(BitmapSource frame)
        {
            _features._panoramaFeature.SetRawEoPanoramaFrame(frame);
        }

        void IVideoFeatureContext.ShowAlreadyConnected()
        {
            _features._rtspSettingsFeature.ShowAlreadyConnected();
        }

        Task IVideoFeatureContext.StartSelectedEoCtecResponseAsync()
        {
            return _features._continuousControlFeature.StartSelectedEoCtecResponseAsync();
        }

        void IVideoFeatureContext.StopCtecEoPositionPolling()
        {
            _features._continuousControlFeature.StopCtecEoPositionPolling();
        }

        Task IVideoFeatureContext.StopFocusSyncAsync(bool stopEo, bool stopIr)
        {
            return _features._syncFeature.StopFocusSyncAsync(stopEo, stopIr);
        }

        void IVideoFeatureContext.StopRoute()
        {
            _features._routesFeature.StopRoute();
        }

        Task IVideoFeatureContext.StopZoomSyncAsync()
        {
            return _features._syncFeature.StopZoomSyncAsync();
        }

        int IVideoFeatureContext.ThermalFireBoxGroupingMode
        {
            get { return _features._thermalFeature.ThermalFireBoxGroupingMode; }
        }

        double IVideoFeatureContext.ThermalHotThresholdRatio
        {
            get { return _features._thermalFeature.ThermalHotThresholdRatio; }
            set { _features._thermalFeature.ThermalHotThresholdRatio = value; }
        }

        double IVideoFeatureContext.ThermalMinimumAreaRatio
        {
            get { return _features._thermalFeature.ThermalMinimumAreaRatio; }
            set { _features._thermalFeature.ThermalMinimumAreaRatio = value; }
        }

        bool IVideoFeatureContext.TryGetRtspEndpoints(out string eoRtspAddress, out string irRtspAddress)
        {
            return _features._controlAgentFeature.TryGetRtspEndpoints(out eoRtspAddress, out irRtspAddress);
        }

        void IVideoFeatureContext.UpdateFireSmokeDetectionOverlay(string camera, ThermalFireDetectionResult thermalResult, SmokeDetectionResult smokeResult)
        {
            _features._fireEventsFeature.UpdateFireSmokeDetectionOverlay(camera, thermalResult, smokeResult);
        }

        void IVideoFeatureContext.UpdateThermalFireCandidateState(string camera, ThermalFireDetectionResult result, Services.Position.ZoneFrameContext zoneFrame)
        {
            _features._thermalFeature.UpdateThermalFireCandidateState(camera, result, zoneFrame);
        }

        void IVideoFeatureContext.UpdateVisionBBoxEvents(string camera, string detectionType, IList<Rect> candidates, IList<double> candidateScores, string detectionSource, Services.Position.ZoneFrameContext zoneFrame)
        {
            _features._fireEventsFeature.UpdateVisionBBoxEvents(camera, detectionType, candidates, candidateScores, detectionSource, zoneFrame);
        }
    }
}
