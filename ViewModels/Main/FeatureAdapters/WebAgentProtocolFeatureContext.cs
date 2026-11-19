using OpenCvWpfTracking.Services.Communication;
using System;
using static OpenCvWpfTracking.ViewModels.Main.MainViewModel;

namespace OpenCvWpfTracking.ViewModels.Main
{
    /// <summary>
    /// 담당 기능이 요청한 상태와 동작을 실제 소유 기능에 연결한다.
    /// get/set은 원래 상태를 읽고 쓰는 전달 창구이며 별도 상태 사본을 만들지 않는다.
    /// ref 반환은 값의 복사가 아니라 같은 저장 위치를 공유한다. 변경 알림은 원래 담당 기능의 규칙을 따른다.
    /// </summary>
    internal sealed class WebAgentProtocolFeatureContext : IWebAgentProtocolFeatureContext
    {
        private readonly FeatureComposition _features;

        // 협력 대상만 보관한다. 생성자에서 통신·타이머 시작이나 다른 기능 실행은 하지 않는다.
        internal WebAgentProtocolFeatureContext(FeatureComposition features)
        {
            _features = features ?? throw new ArgumentNullException(nameof(features));
        }

        ref double? IWebAgentProtocolFeatureContext._activePanAbsoluteTarget
        {
            get { return ref _features._screenStateFeature._activePanAbsoluteTarget; }
        }

        KeyboardPanTiltDirection IWebAgentProtocolFeatureContext._activePanTiltMoveDirection
        {
            get { return _features._screenStateFeature._activePanTiltMoveDirection; }
            set { _features._screenStateFeature._activePanTiltMoveDirection = value; }
        }

        ref double? IWebAgentProtocolFeatureContext._activeTiltAbsoluteTarget
        {
            get { return ref _features._screenStateFeature._activeTiltAbsoluteTarget; }
        }

        string IWebAgentProtocolFeatureContext._connectionSessionId
        {
            get { return _features._devicePowerFeature._connectionSessionId; }
            set { _features._devicePowerFeature._connectionSessionId = value; }
        }

        IControlCommandServicePort IWebAgentProtocolFeatureContext._controlCommandService
        {
            get { return _features.Runtime._controlCommandService; }
        }

        short IWebAgentProtocolFeatureContext._currentEoFocus
        {
            get { return _features._screenStateFeature._currentEoFocus; }
            set { _features._screenStateFeature._currentEoFocus = value; }
        }

        short IWebAgentProtocolFeatureContext._currentEoZoom
        {
            get { return _features._screenStateFeature._currentEoZoom; }
            set { _features._screenStateFeature._currentEoZoom = value; }
        }

        ushort IWebAgentProtocolFeatureContext._currentIrFocus
        {
            get { return _features._screenStateFeature._currentIrFocus; }
            set { _features._screenStateFeature._currentIrFocus = value; }
        }

        ushort IWebAgentProtocolFeatureContext._currentIrZoom
        {
            get { return _features._screenStateFeature._currentIrZoom; }
            set { _features._screenStateFeature._currentIrZoom = value; }
        }

        double IWebAgentProtocolFeatureContext._currentPan
        {
            get { return _features._screenStateFeature._currentPan; }
            set { _features._screenStateFeature._currentPan = value; }
        }

        double IWebAgentProtocolFeatureContext._currentTilt
        {
            get { return _features._screenStateFeature._currentTilt; }
            set { _features._screenStateFeature._currentTilt = value; }
        }

        ref long IWebAgentProtocolFeatureContext._eoLensStatusVersion
        {
            get { return ref _features._screenStateFeature._eoLensStatusVersion; }
        }

        string IWebAgentProtocolFeatureContext._irFocusDirectUnavailableSession
        {
            get { return _features._syncFeature._irFocusDirectUnavailableSession; }
            set { _features._syncFeature._irFocusDirectUnavailableSession = value; }
        }

        ref long IWebAgentProtocolFeatureContext._irLensStatusVersion
        {
            get { return ref _features._screenStateFeature._irLensStatusVersion; }
        }

        ref double? IWebAgentProtocolFeatureContext._lastPanAbsoluteTarget
        {
            get { return ref _features._screenStateFeature._lastPanAbsoluteTarget; }
        }

        ref long IWebAgentProtocolFeatureContext._panStartTxCount
        {
            get { return ref _features._screenStateFeature._panStartTxCount; }
        }

        ref long IWebAgentProtocolFeatureContext._panTiltStatusVersion
        {
            get { return ref _features._screenStateFeature._panTiltStatusVersion; }
        }

        Services.Position.PositionSnapshotService IWebAgentProtocolFeatureContext._positionSnapshotService
        {
            get { return _features._positionFeature._positionSnapshotService; }
        }

        string IWebAgentProtocolFeatureContext._ptzPerformanceScenario
        {
            get { return _features._screenStateFeature._ptzPerformanceScenario; }
            set { _features._screenStateFeature._ptzPerformanceScenario = value; }
        }

        ref long IWebAgentProtocolFeatureContext._tiltStartTxCount
        {
            get { return ref _features._screenStateFeature._tiltStartTxCount; }
        }

        ref global::System.Tuple<double, double, long> IWebAgentProtocolFeatureContext._videoCenterPose
        {
            get { return ref _features._cameraFeatureControlFeature._videoCenterPose; }
        }

        /// <summary>
        /// 2026-10-06: 0x31의 MCB/SCB 원본 상태를 보존한다. 요청 송신 여부로 상태를 추정하지 않고 이 수신값만 표시 기준으로
        /// 사용한다. 실제 처리는 연결된 기능 객체에 위임한다.
        /// </summary>
        void IWebAgentProtocolFeatureContext.ApplyDevicePowerStatus(byte mcbStatus, byte scbStatus, DateTime? sourceTime, string sourceFunction)
        {
            _features._devicePowerFeature.ApplyDevicePowerStatus(mcbStatus, scbStatus, sourceTime, sourceFunction);
        }

        /// <summary>
        /// 길이와 BOOLEAN 범위를 확인한 capability 응답으로 조회/설정 지원을 갱신한다. 실제 처리는 연결된 기능 객체에 위임한다.
        /// </summary>
        void IWebAgentProtocolFeatureContext.ApplyDigitalZoomCapability(byte[] p)
        {
            _features._cameraFeatureControlFeature.ApplyDigitalZoomCapability(p);
        }

        /// <summary>
        /// 유효한 장비 ON/OFF 응답을 적용하며 요청 확인과 추정 억제 상태를 갱신한다. 실제 처리는 연결된 기능 객체에 위임한다.
        /// </summary>
        void IWebAgentProtocolFeatureContext.ApplyDigitalZoomState(byte[] p)
        {
            _features._cameraFeatureControlFeature.ApplyDigitalZoomState(p);
        }

        /// <summary>
        /// DIGITAL_ZOOM 거절 응답 시 요청·추정을 해제한다. 수동 재요청 버튼은 유지한다. 실제 처리는 연결된 기능 객체에 위임한다.
        /// </summary>
        void IWebAgentProtocolFeatureContext.ApplyDigitalZoomUnavailable(int target)
        {
            _features._cameraFeatureControlFeature.ApplyDigitalZoomUnavailable(target);
        }

        /// <summary>
        /// 현재 연결 세션의 지원 응답을 반영하고 필요한 상태 조회를 예약한다. 실제 처리는 연결된 기능 객체에 위임한다.
        /// </summary>
        void IWebAgentProtocolFeatureContext.ApplySpecialFeatureCapability(byte[] p)
        {
            _features._specialFeaturesFeature.ApplySpecialFeatureCapability(p);
        }

        /// <summary>
        /// 검증한 기능 상태 응답을 해당 채널의 행에 반영한다. 실제 처리는 연결된 기능 객체에 위임한다.
        /// </summary>
        void IWebAgentProtocolFeatureContext.ApplySpecialFeatureState(byte[] p)
        {
            _features._specialFeaturesFeature.ApplySpecialFeatureState(p);
        }

        /// <summary>
        /// 기능 미지원 또는 실행 거절 응답을 해당 행에 반영한다. 실제 처리는 연결된 기능 객체에 위임한다.
        /// </summary>
        void IWebAgentProtocolFeatureContext.ApplySpecialFeatureUnavailable(byte[] p)
        {
            _features._specialFeaturesFeature.ApplySpecialFeatureUnavailable(p);
        }

        /// <summary>
        /// 연결 장비의 빌드 정보를 조회해 설정 지원 여부를 판단할 근거를 확보한다. 실제 처리는 연결된 기능 객체에 위임한다.
        /// </summary>
        void IWebAgentProtocolFeatureContext.BeginWebAgentBuildCheck()
        {
            _features._rpySettingsFeature.BeginWebAgentBuildCheck();
        }

        bool IWebAgentProtocolFeatureContext.CameraFeatureTransportReady
        {
            get { return _features._cameraFeatureControlFeature.CameraFeatureTransportReady; }
        }

        /// <summary>
        /// UI 큐에 넣기 전에 세션을 캡처하고 연결 변경 후 도착한 callback을 버린다. 실제 처리는 연결된 기능 객체에 위임한다.
        /// </summary>
        void IWebAgentProtocolFeatureContext.DispatchSpecialFeaturePacket(byte[] p, Action<byte[]> apply)
        {
            _features._specialFeaturesFeature.DispatchSpecialFeaturePacket(p, apply);
        }

        /// <summary>
        /// Web Agent의 0/360도 경계를 고려한 Tilt 차이를 반환한다. ROOFTOP LA는 기존 선형 -90 ~ 90도 차이를 유지한다.
        /// 실제 처리는 연결된 기능 객체에 위임한다.
        /// </summary>
        double IWebAgentProtocolFeatureContext.GetTiltDifference(double currentTilt, double targetTilt)
        {
            return _features._moveControlFeature.GetTiltDifference(currentTilt, targetTilt);
        }

        /// <summary>
        /// GPS 미지원·사용 불가 응답을 설정 상태와 안내에 반영한다.
        /// </summary>
        void IWebAgentProtocolFeatureContext.HandleGpsUnavailable(byte[] payload)
        {
            _features._gpsSettingsFeature.HandleGpsUnavailable(payload);
        }

        /// <summary>
        /// 정밀 명령의 실행 불가 응답을 확인해 보관된 동작 상태와 정지 처리를 정리한다. 실제 처리는 연결된 기능 객체에 위임한다.
        /// </summary>
        void IWebAgentProtocolFeatureContext.HandlePrecisionUnavailable(byte[] payload)
        {
            _features._webAgentMotionFocusFeature.HandlePrecisionUnavailable(payload);
        }

        /// <summary>
        /// 실패 응답의 원 명령을 확인해 해당 기능의 요청 상태만 갱신한다. 실제 처리는 연결된 기능 객체에 위임한다.
        /// </summary>
        void IWebAgentProtocolFeatureContext.HandleWebAgentEquipmentUnavailable(byte[] payload)
        {
            _features._webAgentEquipmentFeature.HandleWebAgentEquipmentUnavailable(payload);
        }

        bool IWebAgentProtocolFeatureContext.IsEnvironmentStatusSelected
        {
            get { return _features._propertiesFeature.IsEnvironmentStatusSelected; }
        }

        /// <summary>
        /// 채널별 렌즈 응답 시각을 원자적으로 기록한다. 실제 처리는 연결된 기능 객체에 위임한다.
        /// </summary>
        void IWebAgentProtocolFeatureContext.MarkLensEvidence(bool ir)
        {
            _features._inputCoordinatorFeature.MarkLensEvidence(ir);
        }

        /// <summary>
        /// 2026-10-14 실제 수신된 유효 Zoom/Focus 증거를 축별로 보관한다. 명령 추정값은 증거가 아니다. 실제 처리는 연결된 기능
        /// 객체에 위임한다.
        /// </summary>
        void IWebAgentProtocolFeatureContext.MarkPresetLensEvidence(bool ir, bool zoom, bool focus)
        {
            _features._presetLensFeature.MarkPresetLensEvidence(ir, zoom, focus);
        }

        /// <summary>
        /// EO 현재 줌·초점·배율 표시의 변경을 화면에 알린다.
        /// </summary>
        void IWebAgentProtocolFeatureContext.NotifyEoCurrentStatusChanged(bool panChanged, bool tiltChanged, bool zoomChanged, bool focusChanged, bool powerChanged)
        {
            _features._controlAgentFeature.NotifyEoCurrentStatusChanged(panChanged, tiltChanged, zoomChanged, focusChanged, powerChanged);
        }

        /// <summary>
        /// IR 현재 줌·초점·배율 표시의 변경을 화면에 알린다.
        /// </summary>
        void IWebAgentProtocolFeatureContext.NotifyIrCurrentStatusChanged()
        {
            _features._controlAgentFeature.NotifyIrCurrentStatusChanged();
        }

        /// <summary>
        /// AF 응답의 형식·세션·대상·작업을 확인하고 지원 여부와 실제 상태를 반영한다. 실제 처리는 연결된 기능 객체에 위임한다.
        /// </summary>
        void IWebAgentProtocolFeatureContext.ParseAutoFocusResponse(byte[] payload)
        {
            _features._webAgentMotionFocusFeature.ParseAutoFocusResponse(payload);
        }

        /// <summary>
        /// 파서가 검증한 표본을 현재 연결의 UI에만 반영한다. 실제 처리는 연결된 기능 객체에 위임한다.
        /// </summary>
        void IWebAgentProtocolFeatureContext.ParseEquipmentTemperature(byte[] payload)
        {
            _features._webAgentEquipmentFeature.ParseEquipmentTemperature(payload);
        }

        /// <summary>
        /// GPS 자동 전송 주기 응답을 해석해 현재 설정 표시를 갱신한다. 실제 처리는 연결된 기능 객체에 위임한다.
        /// </summary>
        void IWebAgentProtocolFeatureContext.ParseGpsAutoTxRateResponse(byte[] payload)
        {
            _features._gpsSettingsFeature.ParseGpsAutoTxRateResponse(payload);
        }

        /// <summary>
        /// 저장된 자세 보정값 응답을 해석해 입력·표시 상태에 반영한다. 실제 처리는 연결된 기능 객체에 위임한다.
        /// </summary>
        void IWebAgentProtocolFeatureContext.ParseStoredRpyOffsetResponse(byte[] payload)
        {
            _features._rpySettingsFeature.ParseStoredRpyOffsetResponse(payload);
        }

        /// <summary>
        /// 저장된 자세 전송 주기 응답을 해석해 표시 상태를 갱신한다. 실제 처리는 연결된 기능 객체에 위임한다.
        /// </summary>
        void IWebAgentProtocolFeatureContext.ParseStoredRpyRateResponse(byte[] payload)
        {
            _features._rpySettingsFeature.ParseStoredRpyRateResponse(payload);
        }

        /// <summary>
        /// GPS 응답을 해석하고 유효한 위치·방향 데이터를 반영한다. 실제 처리는 연결된 기능 객체에 위임한다.
        /// </summary>
        void IWebAgentProtocolFeatureContext.ParseWebAgentGps(byte[] payload)
        {
            _features._gpsSettingsFeature.ParseWebAgentGps(payload);
        }

        /// <summary>
        /// WEB AGENT의 RPY 보정 설정 응답을 읽어 성공·실패 상태에 반영한다.
        /// </summary>
        void IWebAgentProtocolFeatureContext.ParseWebAgentRpyOffsetAck(byte[] payload)
        {
            _features._rpySettingsFeature.ParseWebAgentRpyOffsetAck(payload);
        }

        /// <summary>
        /// WEB AGENT의 RPY 전송 주파수 설정 응답을 읽어 성공·실패 상태에 반영한다.
        /// </summary>
        void IWebAgentProtocolFeatureContext.ParseWebAgentRpyRateAck(byte[] payload)
        {
            _features._rpySettingsFeature.ParseWebAgentRpyRateAck(payload);
        }

        /// <summary>
        /// 장비 UTC 응답을 읽어 화면과 상태 유효성 판정용 시각 기준을 갱신한다. 실제 처리는 연결된 기능 객체에 위임한다.
        /// </summary>
        void IWebAgentProtocolFeatureContext.ParseWebAgentSystemDateTime(byte[] payload)
        {
            _features._systemDateTimeFeature.ParseWebAgentSystemDateTime(payload);
        }

        /// <summary>
        /// 최신 온도 캐시 조회는 새 센서 측정이나 장비 시간 설정이 아니다. 실제 처리는 연결된 기능 객체에 위임한다.
        /// </summary>
        void IWebAgentProtocolFeatureContext.QueryEquipmentTemperature()
        {
            _features._webAgentEquipmentFeature.QueryEquipmentTemperature();
        }

        /// <summary>
        /// 위치 상태 알림을 화면 스레드에 예약해 수신 스레드의 직접 화면 접근을 피한다. 실제 처리는 연결된 기능 객체에 위임한다.
        /// </summary>
        void IWebAgentProtocolFeatureContext.QueuePositionStatusNotification(bool includeLastSnapshot)
        {
            _features._positionFeature.QueuePositionStatusNotification(includeLastSnapshot);
        }

        /// <summary>
        /// 0x24는 최대 30Hz이므로 내부 위치 상태는 즉시 갱신하되, 추가 RPY UI 문자열은 최대 10Hz로 제한하고 진단 로그는 약 1Hz로
        /// 제한한다. 실제 처리는 연결된 기능 객체에 위임한다.
        /// </summary>
        void IWebAgentProtocolFeatureContext.RecordRpyTelemetry(double roll, double pitch, double yaw)
        {
            _features._rpySettingsFeature.RecordRpyTelemetry(roll, pitch, yaw);
        }

        /// <summary>
        /// 새 연결의 GPS 전송 주기를 조회해 이전 세션 표시가 남지 않게 한다. 실제 처리는 연결된 기능 객체에 위임한다.
        /// </summary>
        void IWebAgentProtocolFeatureContext.RequestGpsAutoTxRateAfterConnection()
        {
            _features._gpsSettingsFeature.RequestGpsAutoTxRateAfterConnection();
        }

        /// <summary>
        /// 연결 세션의 capability·응답·요청·추정 및 클릭 증거를 초기화한다. 실제 처리는 연결된 기능 객체에 위임한다.
        /// </summary>
        void IWebAgentProtocolFeatureContext.ResetDigitalZoomSession()
        {
            _features._cameraFeatureControlFeature.ResetDigitalZoomSession();
        }

        /// <summary>
        /// 연결 세대를 변경하고 이전 장비의 AF 상태·대기 요청·실행 확인을 초기화한다. 실제 처리는 연결된 기능 객체에 위임한다.
        /// </summary>
        void IWebAgentProtocolFeatureContext.ResetWebAgentControlSession()
        {
            _features._webAgentMotionFocusFeature.ResetWebAgentControlSession();
        }

        /// <summary>
        /// 장비 변경·재연결 시 이전 온도와 요청 상태를 남기지 않는다. 실제 처리는 연결된 기능 객체에 위임한다.
        /// </summary>
        void IWebAgentProtocolFeatureContext.ResetWebAgentEquipmentState()
        {
            _features._webAgentEquipmentFeature.ResetWebAgentEquipmentState();
        }

        /// <summary>
        /// GPS·IMU 수신 원시값을 방위 기준 계산에 보관한다.
        /// </summary>
        void IWebAgentProtocolFeatureContext.UpdateGpsImuRawReference(double yaw)
        {
            _features._gpsSettingsFeature.UpdateGpsImuRawReference(yaw);
        }

        /// <summary>
        /// EO 줌·초점 응답과 수신 시각을 위치 상태에 반영한다.
        /// </summary>
        void IWebAgentProtocolFeatureContext.UpdatePositionEoLens(int eoZoom, int eoFocus)
        {
            _features._positionFeature.UpdatePositionEoLens(eoZoom, eoFocus);
        }

        /// <summary>
        /// IMU Roll·Pitch·Yaw 응답을 위치 상태에 반영한다.
        /// </summary>
        void IWebAgentProtocolFeatureContext.UpdatePositionImu(double roll, double pitch, double yaw, bool valid)
        {
            _features._positionFeature.UpdatePositionImu(roll, pitch, yaw, valid);
        }

        /// <summary>
        /// IR 줌·초점 응답과 수신 시각을 위치 상태에 반영한다.
        /// </summary>
        void IWebAgentProtocolFeatureContext.UpdatePositionIrLens(int irZoom, int irFocus)
        {
            _features._positionFeature.UpdatePositionIrLens(irZoom, irFocus);
        }

        /// <summary>
        /// 수신한 WEB AGENT 빌드 번호를 지원 기능 판단에 반영한다.
        /// </summary>
        void IWebAgentProtocolFeatureContext.UpdateWebAgentBuild(string buildText)
        {
            _features._rpySettingsFeature.UpdateWebAgentBuild(buildText);
        }

        Action<byte[]> IWebAgentProtocolFeatureContext.ApplySpecialFeatureCapabilityCallback
        {
            get { return _features.Callbacks.ApplySpecialFeatureCapabilityCallback; }
        }

        Action<byte[]> IWebAgentProtocolFeatureContext.ApplySpecialFeatureStateCallback
        {
            get { return _features.Callbacks.ApplySpecialFeatureStateCallback; }
        }

        Action<byte[]> IWebAgentProtocolFeatureContext.ApplySpecialFeatureUnavailableCallback
        {
            get { return _features.Callbacks.ApplySpecialFeatureUnavailableCallback; }
        }
    }
}
