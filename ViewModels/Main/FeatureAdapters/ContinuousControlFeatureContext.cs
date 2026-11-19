using OpenCvWpfTracking.Models.Main;
using System;
using System.Threading;
using static OpenCvWpfTracking.ViewModels.Main.MainViewModel;

namespace OpenCvWpfTracking.ViewModels.Main
{
    /// <summary>
    /// 담당 기능이 요청한 상태와 동작을 실제 소유 기능에 연결한다.
    /// get/set은 원래 상태를 읽고 쓰는 전달 창구이며 별도 상태 사본을 만들지 않는다.
    /// ref 반환은 값의 복사가 아니라 같은 저장 위치를 공유한다. 변경 알림은 원래 담당 기능의 규칙을 따른다.
    /// </summary>
    internal sealed class ContinuousControlFeatureContext : IContinuousControlFeatureContext
    {
        private readonly FeatureComposition _features;

        bool IContinuousControlFeatureContext.IsAutoTrackingActive
        {
            get { return _features.PropertiesFeature.IsAutoTrackingEnabled; }
        }

        /// <summary>현재 제어 작업의 진입 사유를 전달하고 자동 추적을 종료한다.</summary>
        void IContinuousControlFeatureContext.StopAutoTracking(string reason)
        {
            _features.AutoTrackingFeature.Stop(reason);
        }

        // 협력 대상만 보관한다. 생성자에서 통신·타이머 시작이나 다른 기능 실행은 하지 않는다.
        internal ContinuousControlFeatureContext(FeatureComposition features)
        {
            _features = features ?? throw new ArgumentNullException(nameof(features));
        }

        RtspSourceOption IContinuousControlFeatureContext.ActiveEoCtecSource
        {
            get { return _features.ScreenStateFeature._activeEoCtecSource; }
            set { _features.ScreenStateFeature._activeEoCtecSource = value; }
        }

        KeyboardPanTiltDirection IContinuousControlFeatureContext.ActivePanTiltMoveDirection
        {
            get { return _features.ScreenStateFeature._activePanTiltMoveDirection; }
            set { _features.ScreenStateFeature._activePanTiltMoveDirection = value; }
        }

        System.Collections.Generic.Dictionary<string, RtspSourceOption> IContinuousControlFeatureContext.AxisCtecSources
        {
            get { return _features.InputCoordinatorFeature._axisCtecSources; }
        }

        long IContinuousControlFeatureContext.CenterDeadlineTicks
        {
            get { return _features.CameraFeatureControlFeature._centerDeadlineTicks; }
            set { _features.CameraFeatureControlFeature._centerDeadlineTicks = value; }
        }

        RtspSourceOption IContinuousControlFeatureContext.ConnectedEoCtecSource
        {
            get { return _features.ScreenStateFeature._connectedEoCtecSource; }
            set { _features.ScreenStateFeature._connectedEoCtecSource = value; }
        }

        Services.Communication.IControlCommandServicePort IContinuousControlFeatureContext.ControlCommandService
        {
            get { return _features.Runtime._controlCommandService; }
        }

        System.Collections.Generic.Dictionary<string, Tuple<string, int, int>> IContinuousControlFeatureContext.ControlInputs
        {
            get { return _features.InputCoordinatorFeature._controlInputs; }
        }

        Services.Communication.ICtecCameraCommandServicePort IContinuousControlFeatureContext.CtecCameraCommandService
        {
            get { return _features.Runtime._ctecCameraCommandService; }
        }

        Services.Communication.ICtecCameraResponseServicePort IContinuousControlFeatureContext.CtecCameraResponseService
        {
            get { return _features.Runtime._ctecCameraResponseService; }
        }

        ref long IContinuousControlFeatureContext.CtecEoPositionOperationGeneration
        {
            get { return ref _features.ScreenStateFeature._ctecEoPositionOperationGeneration; }
        }

        ref global::System.Threading.CancellationTokenSource IContinuousControlFeatureContext.CtecEoPositionPollingCts
        {
            get { return ref _features.ScreenStateFeature._ctecEoPositionPollingCts; }
        }

        SemaphoreSlim IContinuousControlFeatureContext.CtecEoPositionQueryLock
        {
            get { return _features.Runtime._ctecEoPositionQueryLock; }
        }

        byte IContinuousControlFeatureContext.CurrentCtecEoFocusMode
        {
            get { return _features.ScreenStateFeature._currentCtecEoFocusMode; }
            set { _features.ScreenStateFeature._currentCtecEoFocusMode = value; }
        }

        ushort IContinuousControlFeatureContext.CurrentCtecEoFocusPosition
        {
            get { return _features.ScreenStateFeature._currentCtecEoFocusPosition; }
            set { _features.ScreenStateFeature._currentCtecEoFocusPosition = value; }
        }

        ushort IContinuousControlFeatureContext.CurrentCtecEoZoomPosition
        {
            get { return _features.ScreenStateFeature._currentCtecEoZoomPosition; }
            set { _features.ScreenStateFeature._currentCtecEoZoomPosition = value; }
        }

        short IContinuousControlFeatureContext.CurrentEoFocus
        {
            get { return _features.ScreenStateFeature._currentEoFocus; }
            set { _features.ScreenStateFeature._currentEoFocus = value; }
        }

        short IContinuousControlFeatureContext.CurrentEoZoom
        {
            get { return _features.ScreenStateFeature._currentEoZoom; }
            set { _features.ScreenStateFeature._currentEoZoom = value; }
        }

        ushort IContinuousControlFeatureContext.CurrentIrFocus
        {
            get { return _features.ScreenStateFeature._currentIrFocus; }
            set { _features.ScreenStateFeature._currentIrFocus = value; }
        }

        ushort IContinuousControlFeatureContext.CurrentIrZoom
        {
            get { return _features.ScreenStateFeature._currentIrZoom; }
            set { _features.ScreenStateFeature._currentIrZoom = value; }
        }

        KeyboardPanTiltDirection IContinuousControlFeatureContext.CurrentKeyboardPanTiltDirection
        {
            get { return _features.ScreenStateFeature._currentKeyboardPanTiltDirection; }
            set { _features.ScreenStateFeature._currentKeyboardPanTiltDirection = value; }
        }

        ContinuousMoveType IContinuousControlFeatureContext.CurrentMoveType
        {
            get { return _features.ScreenStateFeature._currentMoveType; }
            set { _features.ScreenStateFeature._currentMoveType = value; }
        }

        int IContinuousControlFeatureContext.EnvironmentIrManualMoveDirection
        {
            get { return _features.ScreenStateFeature._environmentIrManualMoveDirection; }
            set { _features.ScreenStateFeature._environmentIrManualMoveDirection = value; }
        }

        ref global::System.DateTime IContinuousControlFeatureContext.EnvironmentIrManualMoveStartedUtc
        {
            get { return ref _features.ScreenStateFeature._environmentIrManualMoveStartedUtc; }
        }

        ContinuousMoveType IContinuousControlFeatureContext.EnvironmentIrManualMoveType
        {
            get { return _features.ScreenStateFeature._environmentIrManualMoveType; }
            set { _features.ScreenStateFeature._environmentIrManualMoveType = value; }
        }

        ref int IContinuousControlFeatureContext.EoFocusCommandSequence
        {
            get { return ref _features.ScreenStateFeature._eoFocusCommandSequence; }
        }

        System.Diagnostics.Stopwatch IContinuousControlFeatureContext.FocusLogStopwatch
        {
            get { return _features.Runtime._focusLogStopwatch; }
        }

        int IContinuousControlFeatureContext.InputEpoch
        {
            get { return _features.InputCoordinatorFeature._inputEpoch; }
            set { _features.InputCoordinatorFeature._inputEpoch = value; }
        }

        ref long IContinuousControlFeatureContext.IrLensStatusVersion
        {
            get { return ref _features.ScreenStateFeature._irLensStatusVersion; }
        }

        bool IContinuousControlFeatureContext.IsDeviceConnectionRequested
        {
            get { return _features.ScreenStateFeature._isDeviceConnectionRequested; }
            set { _features.ScreenStateFeature._isDeviceConnectionRequested = value; }
        }

        bool IContinuousControlFeatureContext.IsKeyboardPanLeftPressed
        {
            get { return _features.ScreenStateFeature._isKeyboardPanLeftPressed; }
            set { _features.ScreenStateFeature._isKeyboardPanLeftPressed = value; }
        }

        bool IContinuousControlFeatureContext.IsKeyboardPanRightPressed
        {
            get { return _features.ScreenStateFeature._isKeyboardPanRightPressed; }
            set { _features.ScreenStateFeature._isKeyboardPanRightPressed = value; }
        }

        bool IContinuousControlFeatureContext.IsKeyboardTiltDownPressed
        {
            get { return _features.ScreenStateFeature._isKeyboardTiltDownPressed; }
            set { _features.ScreenStateFeature._isKeyboardTiltDownPressed = value; }
        }

        bool IContinuousControlFeatureContext.IsKeyboardTiltUpPressed
        {
            get { return _features.ScreenStateFeature._isKeyboardTiltUpPressed; }
            set { _features.ScreenStateFeature._isKeyboardTiltUpPressed = value; }
        }

        bool IContinuousControlFeatureContext.IsPanTiltMoveActive
        {
            get { return _features.ScreenStateFeature._isPanTiltMoveActive; }
            set { _features.ScreenStateFeature._isPanTiltMoveActive = value; }
        }

        System.Collections.Generic.Dictionary<string, Tuple<int, int>> IContinuousControlFeatureContext.IssuedControlAxes
        {
            get { return _features.InputCoordinatorFeature._issuedControlAxes; }
        }

        long IContinuousControlFeatureContext.LastEoFocusCommandElapsedMs
        {
            get { return _features.ScreenStateFeature._lastEoFocusCommandElapsedMs; }
            set { _features.ScreenStateFeature._lastEoFocusCommandElapsedMs = value; }
        }

        string IContinuousControlFeatureContext.LastEoFocusCommandName
        {
            get { return _features.ScreenStateFeature._lastEoFocusCommandName; }
            set { _features.ScreenStateFeature._lastEoFocusCommandName = value; }
        }

        ref global::System.DateTime IContinuousControlFeatureContext.LastEoFocusCommandTime
        {
            get { return ref _features.ScreenStateFeature._lastEoFocusCommandTime; }
        }

        ref long IContinuousControlFeatureContext.PanStopTxCount
        {
            get { return ref _features.ScreenStateFeature._panStopTxCount; }
        }

        ref long IContinuousControlFeatureContext.PanTiltStatusVersion
        {
            get { return ref _features.ScreenStateFeature._panTiltStatusVersion; }
        }

        bool IContinuousControlFeatureContext.PrecisionStopUncertain
        {
            get { return _features.WebAgentMotionFocusFeature._precisionStopUncertain; }
            set { _features.WebAgentMotionFocusFeature._precisionStopUncertain = value; }
        }

        string IContinuousControlFeatureContext.PtzPerformanceScenario
        {
            get { return _features.ScreenStateFeature._ptzPerformanceScenario; }
            set { _features.ScreenStateFeature._ptzPerformanceScenario = value; }
        }

        ref long IContinuousControlFeatureContext.TiltStopTxCount
        {
            get { return ref _features.ScreenStateFeature._tiltStopTxCount; }
        }

        bool IContinuousControlFeatureContext.VideoCenterBusy
        {
            get { return _features.CameraFeatureControlFeature._videoCenterBusy; }
            set { _features.CameraFeatureControlFeature._videoCenterBusy = value; }
        }

        ref global::System.DateTimeOffset IContinuousControlFeatureContext.ZoneCtecZoomAt
        {
            get { return ref _features.ZonesFeature._zoneCtecZoomAt; }
        }

        int IContinuousControlFeatureContext.ZoneCtecZoomEpoch
        {
            get { return _features.ZonesFeature._zoneCtecZoomEpoch; }
            set { _features.ZonesFeature._zoneCtecZoomEpoch = value; }
        }

        string IContinuousControlFeatureContext.ZoneCtecZoomKey
        {
            get { return _features.ZonesFeature._zoneCtecZoomKey; }
            set { _features.ZonesFeature._zoneCtecZoomKey = value; }
        }

        int IContinuousControlFeatureContext.ZoneEpoch
        {
            get { return _features.ZonesFeature._zoneEpoch; }
            set { _features.ZonesFeature._zoneEpoch = value; }
        }

        ref long IContinuousControlFeatureContext.ZoomStartTxCount
        {
            get { return ref _features.ScreenStateFeature._zoomStartTxCount; }
        }

        ref long IContinuousControlFeatureContext.ZoomStopTxCount
        {
            get { return ref _features.ScreenStateFeature._zoomStopTxCount; }
        }

        /// <summary>
        /// 환경장비 Web Agent가 IR 상태 패킷을 보내지 않는 경우에만 사용하는 성공한 명령값 기반 UI 상태 보완이다. 실제 Function
        /// 0x07 패킷이 수신되면 실측값으로 다시 덮어쓴다. 실제 처리는 연결된 기능 객체에 위임한다.
        /// </summary>
        void IContinuousControlFeatureContext.ApplyEnvironmentIrCommandedPosition(int? zoomPosition, int? focusPosition, string reason)
        {
            _features.ControlAgentFeature.ApplyEnvironmentIrCommandedPosition(zoomPosition, focusPosition, reason);
        }

        /// <summary>
        /// 현재 PAN / TILT SPEED UI 값으로 위치 이동 속도를 설정한다. Pan Position Speed : Command2 0x49
        /// Tilt Position Speed: Command2 0x4B UI 1~60 값을 위치 이동 속도 1~60 deg/s로 적용하며, UI 0은
        /// 위치 이동을 시작하지 않고 STOP으로 처리한다. 실제 처리는 연결된 기능 객체에 위임한다.
        /// </summary>
        bool IContinuousControlFeatureContext.ApplyPanTiltPositionSpeedFromUi(out double positionSpeed)
        {
            return _features.MoveControlFeature.ApplyPanTiltPositionSpeedFromUi(out positionSpeed);
        }

        /// <summary>
        /// EO 직접 제어 구성을 구분해 WEB AGENT AF 명령을 적용할 대상인지 확인한다. 실제 처리는 연결된 기능 객체에 위임한다.
        /// </summary>
        bool IContinuousControlFeatureContext.AutoFocusTargetUsesAgent(int target)
        {
            return _features.WebAgentMotionFocusFeature.AutoFocusTargetUsesAgent(target);
        }

        /// <summary>
        /// 기존 VIA 0 Pan 이동 작업 취소 실제 처리는 연결된 기능 객체에 위임한다.
        /// </summary>
        void IContinuousControlFeatureContext.CancelMoveControlPanOperation()
        {
            _features.MoveControlFeature.CancelMoveControlPanOperation();
        }

        /// <summary>
        /// 연동 버튼과 공통 정지는 기존 단일 프리셋 이동의 취소 경로를 그대로 사용한다. 실제 처리는 연결된 기능 객체에 위임한다.
        /// </summary>
        void IContinuousControlFeatureContext.CancelPanoramaPresets()
        {
            _features.PanoramaPresetsFeature.CancelPanoramaPresets();
        }

        /// <summary>
        /// 진행 중인 Pan / Tilt Absolute 이동 추적값을 초기화한다. 실제 처리는 연결된 기능 객체에 위임한다.
        /// </summary>
        void IContinuousControlFeatureContext.ClearActivePanTiltAbsoluteMove()
        {
            _features.MoveControlFeature.ClearActivePanTiltAbsoluteMove();
        }

        /// <summary>
        /// 연결 요청 중인 프로파일을 우선하여 해당 채널의 조정값을 가져온다. 실제 처리는 연결된 기능 객체에 위임한다.
        /// </summary>
        Services.Configuration.LensControlTuning IContinuousControlFeatureContext.ControlTuningFor(bool ir)
        {
            return _features.InputCoordinatorFeature.ControlTuningFor(ir);
        }

        string IContinuousControlFeatureContext.CurrentEoFocusText
        {
            get { return _features.PropertiesFeature.CurrentEoFocusText; }
        }

        string IContinuousControlFeatureContext.CurrentEoZoomText
        {
            get { return _features.PropertiesFeature.CurrentEoZoomText; }
        }

        string IContinuousControlFeatureContext.CurrentLaPresetSnapshotText
        {
            get { return _features.PropertiesFeature.CurrentLaPresetSnapshotText; }
        }

        string IContinuousControlFeatureContext.CurrentPresetSnapshotText
        {
            get { return _features.PropertiesFeature.CurrentPresetSnapshotText; }
        }

        byte IContinuousControlFeatureContext.EffectivePanTiltSpeedLevel
        {
            get { return _features.PropertiesFeature.EffectivePanTiltSpeedLevel; }
        }

        string IContinuousControlFeatureContext.EnvironmentEoFocusStatusText
        {
            get { return _features.PropertiesFeature.EnvironmentEoFocusStatusText; }
        }

        string IContinuousControlFeatureContext.EnvironmentEoZoomStatusText
        {
            get { return _features.PropertiesFeature.EnvironmentEoZoomStatusText; }
        }

        /// <summary>
        /// 선택한 EO/IR AF 버튼 인수를 조회·설정·단발 초점 요청으로 변환한다. 실제 처리는 연결된 기능 객체에 위임한다.
        /// </summary>
        void IContinuousControlFeatureContext.ExecuteAutoFocus(object parameter)
        {
            _features.WebAgentMotionFocusFeature.ExecuteAutoFocus(parameter);
        }

        /// <summary>
        /// 현재 장비 구성에 맞춰 IR Focus 상태값을 0~1000 위치로 반환한다. ROOFTOP / LA AGENT: Raw 1000(Far)
        /// ->; Standard 0(Far) Raw 0(Near) ->; Standard 1000(Near) ENVIRONMENT / WEB AGENT:
        /// 장비 상태값과 화면 Position 방향이 같으므로 역변환하지 않는다. 실제 처리는 연결된 기능 객체에 위임한다.
        /// </summary>
        int IContinuousControlFeatureContext.GetCurrentIrFocusStandardPosition()
        {
            return _features.MoveControlFeature.GetCurrentIrFocusStandardPosition();
        }

        /// <summary>
        /// 현재 장비 구성에 맞춰 IR Zoom 상태값을 0~1000 위치로 반환한다. ROOFTOP / LA AGENT: Raw 1000(Wide)
        /// ->; Standard 0(Wide) Raw 0(Tele) ->; Standard 1000(Tele) ENVIRONMENT / WEB
        /// AGENT: 장비 상태값과 화면 Position 방향이 같으므로 역변환하지 않는다. 실제 처리는 연결된 기능 객체에 위임한다.
        /// </summary>
        int IContinuousControlFeatureContext.GetCurrentIrZoomStandardPosition()
        {
            return _features.MoveControlFeature.GetCurrentIrZoomStandardPosition();
        }

        /// <summary>
        /// 현재 EO Focus 값을 프리셋 공통 범위 0~1000으로 변환한다. 실제 처리는 연결된 기능 객체에 위임한다.
        /// </summary>
        ushort IContinuousControlFeatureContext.GetCurrentPresetStandardFocus()
        {
            return _features.MoveControlFeature.GetCurrentPresetStandardFocus();
        }

        /// <summary>
        /// 현재 EO Zoom 값을 프리셋 공통 범위 0~1000으로 변환한다. 실제 처리는 연결된 기능 객체에 위임한다.
        /// </summary>
        ushort IContinuousControlFeatureContext.GetCurrentPresetStandardZoom()
        {
            return _features.MoveControlFeature.GetCurrentPresetStandardZoom();
        }

        /// <summary>
        /// AF 동작·응답 대기·상태 불명 중 해당 채널의 수동 초점 충돌을 차단한다. 실제 처리는 연결된 기능 객체에 위임한다.
        /// </summary>
        bool IContinuousControlFeatureContext.IsAutoFocusBlockingManualFocus(bool ir, bool logWarning)
        {
            return _features.WebAgentMotionFocusFeature.IsAutoFocusBlockingManualFocus(ir, logWarning);
        }

        bool IContinuousControlFeatureContext.IsControlInputLocked
        {
            get { return _features.PropertiesFeature.IsControlInputLocked; }
        }

        bool IContinuousControlFeatureContext.IsFovSpeedEnabled
        {
            get { return _features.InputCoordinatorFeature.IsFovSpeedEnabled; }
            set { _features.InputCoordinatorFeature.IsFovSpeedEnabled = value; }
        }

        bool IContinuousControlFeatureContext.IsSelectedControlAgentWebAgent
        {
            get { return _features.RtspSettingsFeature.IsSelectedControlAgentWebAgent; }
        }

        bool IContinuousControlFeatureContext.IsWebAgentFocusControlReady
        {
            get { return _features.WebAgentMotionFocusFeature.IsWebAgentFocusControlReady; }
        }

        /// <summary>
        /// 채널별 렌즈 응답 시각을 원자적으로 기록한다. 실제 처리는 연결된 기능 객체에 위임한다.
        /// </summary>
        void IContinuousControlFeatureContext.MarkLensEvidence(bool ir)
        {
            _features.InputCoordinatorFeature.MarkLensEvidence(ir);
        }

        /// <summary>
        /// 팬·틸트 이동 명령 시점을 기록해 이전 수신 위치를 새 이동의 완료로 오인하지 않는다. 실제 처리는 연결된 기능 객체에 위임한다.
        /// </summary>
        void IContinuousControlFeatureContext.MarkPanTiltMoveCommandIssued()
        {
            _features.WebAgentProtocolFeature.MarkPanTiltMoveCommandIssued();
        }

        /// <summary>
        /// 2026-10-14 실제 수신된 유효 Zoom/Focus 증거를 축별로 보관한다. 명령 추정값은 증거가 아니다. 실제 처리는 연결된 기능
        /// 객체에 위임한다.
        /// </summary>
        void IContinuousControlFeatureContext.MarkPresetLensEvidence(bool ir, bool zoom, bool focus)
        {
            _features.PresetLensFeature.MarkPresetLensEvidence(ir, zoom, focus);
        }

        /// <summary>
        /// EO 현재 줌·초점·배율 표시의 변경을 화면에 알린다.
        /// </summary>
        void IContinuousControlFeatureContext.NotifyEoCurrentStatusChanged(bool panChanged, bool tiltChanged, bool zoomChanged, bool focusChanged, bool powerChanged)
        {
            _features.ControlAgentFeature.NotifyEoCurrentStatusChanged(panChanged, tiltChanged, zoomChanged, focusChanged, powerChanged);
        }

        /// <summary>
        /// IR 현재 줌·초점·배율 표시의 변경을 화면에 알린다.
        /// </summary>
        void IContinuousControlFeatureContext.NotifyIrCurrentStatusChanged()
        {
            _features.ControlAgentFeature.NotifyIrCurrentStatusChanged();
        }

        /// <summary>
        /// 바인딩 속성 변경 알림 실제 처리는 연결된 기능 객체에 위임한다.
        /// </summary>
        void IContinuousControlFeatureContext.OnPropertyChanged(string propertyName)
        {
            _features.Callbacks.Notify(propertyName);
        }

        string IContinuousControlFeatureContext.PanTiltSpeedDisplayText
        {
            get { return _features.PropertiesFeature.PanTiltSpeedDisplayText; }
        }

        byte IContinuousControlFeatureContext.PanTiltSpeedLevel
        {
            get { return _features.PropertiesFeature.PanTiltSpeedLevel; }
            set { _features.PropertiesFeature.PanTiltSpeedLevel = value; }
        }

        /// <summary>
        /// 위치 이동 중 속도 변경 시 현재 Absolute 목표를 그대로 재송신한다. 일부 장비는 0x49 / 0x4B 속도 설정을 다음 위치 이동
        /// 명령부터 적용하므로, 진행 중인 0x45 / 0x47 목표를 다시 보내야 변경된 속도가 현재 이동에도 즉시 반영된다. 실제 처리는 연결된
        /// 기능 객체에 위임한다.
        /// </summary>
        void IContinuousControlFeatureContext.ReapplyActivePanTiltAbsoluteTarget(double positionSpeed)
        {
            _features.MoveControlFeature.ReapplyActivePanTiltAbsoluteTarget(positionSpeed);
        }

        string IContinuousControlFeatureContext.RooftopEoFocusStatusText
        {
            get { return _features.PropertiesFeature.RooftopEoFocusStatusText; }
        }

        string IContinuousControlFeatureContext.RooftopEoZoomStatusText
        {
            get { return _features.PropertiesFeature.RooftopEoZoomStatusText; }
        }

        RtspSourceOption IContinuousControlFeatureContext.SelectedEoRtspSource
        {
            get { return _features.PropertiesFeature.SelectedEoRtspSource; }
            set { _features.PropertiesFeature.SelectedEoRtspSource = value; }
        }

        EquipmentStatusMode IContinuousControlFeatureContext.SelectedEquipmentStatusMode
        {
            get { return _features.PropertiesFeature.SelectedEquipmentStatusMode; }
            set { _features.PropertiesFeature.SelectedEquipmentStatusMode = value; }
        }

        /// <summary>
        /// 이전 방향의 경과 시간을 먼저 반영한 후 추정 배율의 증감 방향을 변경한다. 실제 처리는 연결된 기능 객체에 위임한다.
        /// </summary>
        void IContinuousControlFeatureContext.SetDigitalEstimateDirection(int direction, bool ir)
        {
            _features.CameraFeatureControlFeature.SetDigitalEstimateDirection(direction, ir);
        }

        /// <summary>
        /// 진행 중 정밀 동작 상태를 정리하고 필요할 때 양 축 정지를 요청한다. 실제 처리는 연결된 기능 객체에 위임한다.
        /// </summary>
        void IContinuousControlFeatureContext.StopPrecisionMotion(string reason, bool force)
        {
            _features.WebAgentMotionFocusFeature.StopPrecisionMotion(reason, force);
        }

        /// <summary>
        /// 현재 장비와 채널에 대응하는 구역 저장 키를 만든다.
        /// </summary>
        string IContinuousControlFeatureContext.ZoneKey(string camera)
        {
            return _features.ZonesFeature.ZoneKey(camera);
        }
    }
}
