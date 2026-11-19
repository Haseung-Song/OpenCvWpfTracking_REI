using System;
using System.Threading;
using System.Threading.Tasks;

namespace OpenCvWpfTracking.ViewModels.Main
{
    /// <summary>
    /// 담당 기능이 요청한 상태와 동작을 실제 소유 기능에 연결한다.
    /// get/set은 원래 상태를 읽고 쓰는 전달 창구이며 별도 상태 사본을 만들지 않는다.
    /// ref 반환은 값의 복사가 아니라 같은 저장 위치를 공유한다. 변경 알림은 원래 담당 기능의 규칙을 따른다.
    /// </summary>
    internal sealed class PresetLensFeatureContext : IPresetLensFeatureContext
    {
        private readonly FeatureComposition _features;

        string IPresetLensFeatureContext.ConnectionSessionId
        {
            get { return _features.DevicePowerFeature._connectionSessionId; }
        }

        // 협력 대상만 보관한다. 생성자에서 통신·타이머 시작이나 다른 기능 실행은 하지 않는다.
        internal PresetLensFeatureContext(FeatureComposition features)
        {
            _features = features ?? throw new ArgumentNullException(nameof(features));
        }

        Services.Communication.AutoFocusSnapshot[] IPresetLensFeatureContext.AutoFocus
        {
            get { return _features.WebAgentMotionFocusFeature._autoFocus; }
        }

        bool IPresetLensFeatureContext.AutoFocusActivatedForSession
        {
            get { return _features.WebAgentMotionFocusFeature._autoFocusActivatedForSession; }
            set { _features.WebAgentMotionFocusFeature._autoFocusActivatedForSession = value; }
        }

        Services.Communication.IControlCommandServicePort IPresetLensFeatureContext.ControlCommandService
        {
            get { return _features.Runtime._controlCommandService; }
        }

        short IPresetLensFeatureContext.CurrentEoFocus
        {
            get { return _features.ScreenStateFeature._currentEoFocus; }
            set { _features.ScreenStateFeature._currentEoFocus = value; }
        }

        short IPresetLensFeatureContext.CurrentEoZoom
        {
            get { return _features.ScreenStateFeature._currentEoZoom; }
            set { _features.ScreenStateFeature._currentEoZoom = value; }
        }

        ushort IPresetLensFeatureContext.CurrentIrZoom
        {
            get { return _features.ScreenStateFeature._currentIrZoom; }
            set { _features.ScreenStateFeature._currentIrZoom = value; }
        }

        CancellationTokenSource IPresetLensFeatureContext.LaPresetDirectScanCts
        {
            get { return _features.ScreenStateFeature._laPresetDirectScanCts; }
            set { _features.ScreenStateFeature._laPresetDirectScanCts = value; }
        }

        CancellationTokenSource IPresetLensFeatureContext.LaPresetSingleMoveCts
        {
            get { return _features.ScreenStateFeature._laPresetSingleMoveCts; }
            set { _features.ScreenStateFeature._laPresetSingleMoveCts = value; }
        }

        CancellationTokenSource IPresetLensFeatureContext.PresetDirectScanCts
        {
            get { return _features.ScreenStateFeature._presetDirectScanCts; }
            set { _features.ScreenStateFeature._presetDirectScanCts = value; }
        }

        CancellationTokenSource IPresetLensFeatureContext.PresetSingleMoveCts
        {
            get { return _features.ScreenStateFeature._presetSingleMoveCts; }
            set { _features.ScreenStateFeature._presetSingleMoveCts = value; }
        }

        /// <summary>
        /// EO 직접 제어 구성을 구분해 WEB AGENT AF 명령을 적용할 대상인지 확인한다. 실제 처리는 연결된 기능 객체에 위임한다.
        /// </summary>
        bool IPresetLensFeatureContext.AutoFocusTargetUsesAgent(int target)
        {
            return _features.WebAgentMotionFocusFeature.AutoFocusTargetUsesAgent(target);
        }

        bool IPresetLensFeatureContext.CameraFeatureTransportReady
        {
            get { return _features.CameraFeatureControlFeature.CameraFeatureTransportReady; }
        }

        /// <summary>
        /// 현재 장비 구성에 맞춰 IR Focus 상태값을 0~1000 위치로 반환한다. ROOFTOP / LA AGENT: Raw 1000(Far)
        /// ->; Standard 0(Far) Raw 0(Near) ->; Standard 1000(Near) ENVIRONMENT / WEB AGENT:
        /// 장비 상태값과 화면 Position 방향이 같으므로 역변환하지 않는다. 실제 처리는 연결된 기능 객체에 위임한다.
        /// </summary>
        int IPresetLensFeatureContext.GetCurrentIrFocusStandardPosition()
        {
            return _features.MoveControlFeature.GetCurrentIrFocusStandardPosition();
        }

        /// <summary>
        /// 현재 장비 구성에 맞춰 IR Zoom 상태값을 0~1000 위치로 반환한다. ROOFTOP / LA AGENT: Raw 1000(Wide)
        /// ->; Standard 0(Wide) Raw 0(Tele) ->; Standard 1000(Tele) ENVIRONMENT / WEB
        /// AGENT: 장비 상태값과 화면 Position 방향이 같으므로 역변환하지 않는다. 실제 처리는 연결된 기능 객체에 위임한다.
        /// </summary>
        int IPresetLensFeatureContext.GetCurrentIrZoomStandardPosition()
        {
            return _features.MoveControlFeature.GetCurrentIrZoomStandardPosition();
        }

        /// <summary>
        /// 현재 EO Focus 값을 프리셋 공통 범위 0~1000으로 변환한다. 실제 처리는 연결된 기능 객체에 위임한다.
        /// </summary>
        ushort IPresetLensFeatureContext.GetCurrentPresetStandardFocus()
        {
            return _features.MoveControlFeature.GetCurrentPresetStandardFocus();
        }

        /// <summary>
        /// 현재 EO Zoom 값을 프리셋 공통 범위 0~1000으로 변환한다. 실제 처리는 연결된 기능 객체에 위임한다.
        /// </summary>
        ushort IPresetLensFeatureContext.GetCurrentPresetStandardZoom()
        {
            return _features.MoveControlFeature.GetCurrentPresetStandardZoom();
        }

        /// <summary>
        /// AF 동작·응답 대기·상태 불명 중 해당 채널의 수동 초점 충돌을 차단한다. 실제 처리는 연결된 기능 객체에 위임한다.
        /// </summary>
        bool IPresetLensFeatureContext.IsAutoFocusBlockingManualFocus(bool ir, bool logWarning)
        {
            return _features.WebAgentMotionFocusFeature.IsAutoFocusBlockingManualFocus(ir, logWarning);
        }

        bool IPresetLensFeatureContext.IsAutoFocusPanelVisible
        {
            get { return _features.WebAgentMotionFocusFeature.IsAutoFocusPanelVisible; }
        }

        bool IPresetLensFeatureContext.IsEnvironmentStatusSelected
        {
            get { return _features.PropertiesFeature.IsEnvironmentStatusSelected; }
        }

        /// <summary>
        /// 2026-09-18: 화면/명령의 표준 좌표가 제공되면 장비 Raw 물리 끝점을 공통 정규화한 값으로 판정한다. Raw 좌표 전용 호출만
        /// 기존 허용 오차를 유지한다. 실제 처리는 연결된 기능 객체에 위임한다.
        /// </summary>
        bool IPresetLensFeatureContext.IsIrZoomTargetReached(int target, int actual, int standardTarget, int tolerance, bool syncCalibration)
        {
            return _features.SyncFeature.IsIrZoomTargetReached(target, actual, standardTarget, tolerance, syncCalibration);
        }

        bool IPresetLensFeatureContext.IsSelectedControlAgentWebAgent
        {
            get { return _features.RtspSettingsFeature.IsSelectedControlAgentWebAgent; }
        }

        /// <summary>
        /// 2026-10-16: 환경부 장비는 신규 CMD2=0xB7 절대위치를 우선 사용하고 실제 IR 렌즈 피드백으로 도달 여부를 확인한다. 새
        /// 피드백이 계속 수신되는데도 위치가 전혀 변하지 않을 때만 0x31 폐루프 경로로 전환한다. 실제 처리는 연결된 기능 객체에 위임한다.
        /// </summary>
        Task<bool> IPresetLensFeatureContext.MoveIrZoomToPositionAsync(int targetPosition, CancellationToken cancellationToken, int standardTarget, int tolerance, bool syncCalibration, bool explicitAbsolute)
        {
            return _features.SyncFeature.MoveIrZoomToPositionAsync(targetPosition, cancellationToken, standardTarget, tolerance, syncCalibration, explicitAbsolute);
        }

        /// <summary>
        /// PRESET 1 저장값 기준 EO / IR Focus를 각각 복원한다. 실제 처리는 연결된 기능 객체에 위임한다.
        /// </summary>
        Task<bool> IPresetLensFeatureContext.MoveLaPresetFocusDirectAsync(int eoStandardPosition, int irStandardPosition, CancellationToken cancellationToken, int eoTolerance, int irTolerance, bool? onlyIr)
        {
            return _features.MoveControlFeature.MoveLaPresetFocusDirectAsync(eoStandardPosition, irStandardPosition, cancellationToken, eoTolerance, irTolerance, onlyIr);
        }

        /// <summary>
        /// PRESET 1 저장값 기준 EO / IR Zoom을 각각 복원한다. 실제 처리는 연결된 기능 객체에 위임한다.
        /// </summary>
        Task<bool> IPresetLensFeatureContext.MoveLaPresetZoomDirectAsync(int eoStandardPosition, int irPosition, CancellationToken cancellationToken, int eoTolerance, int irTolerance, bool? onlyIr)
        {
            return _features.MoveControlFeature.MoveLaPresetZoomDirectAsync(eoStandardPosition, irPosition, cancellationToken, eoTolerance, irTolerance, onlyIr);
        }

        Services.Video.PanoramaReferenceState IPresetLensFeatureContext.PanoramaReference
        {
            get { return _features.PanoramaReferenceFeature.PanoramaReference; }
        }

        int IPresetLensFeatureContext.PresetScanDelay
        {
            get { return _features.PropertiesFeature.PresetScanDelay; }
            set { _features.PropertiesFeature.PresetScanDelay = value; }
        }

        /// <summary>
        /// IR 렌즈의 새 응답에서 이동이 멈춘 정착 위치를 확인한다.
        /// </summary>
        Task<int> IPresetLensFeatureContext.WaitForIrLensSettledPositionAsync(bool zoom, CancellationToken cancellationToken)
        {
            return _features.SyncFeature.WaitForIrLensSettledPositionAsync(zoom, cancellationToken);
        }
    }
}
