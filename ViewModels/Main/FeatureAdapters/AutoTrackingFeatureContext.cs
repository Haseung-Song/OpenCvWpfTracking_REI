using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using OpenCvWpfTracking.Services.Control;

namespace OpenCvWpfTracking.ViewModels.Main
{
    /// <summary>추적 기능을 기존 영상·장비·입력 담당 객체에 연결한다. 상태 사본이나 별도 통신 객체를 만들지 않는다.</summary>
    internal sealed class AutoTrackingFeatureContext : IAutoTrackingFeatureContext
    {
        private readonly FeatureComposition _features;

        /// <summary>기능 합성 객체를 보관하며 생성 시에는 다른 기능을 실행하지 않는다.</summary>
        internal AutoTrackingFeatureContext(FeatureComposition features)
        {
            _features = features ?? throw new ArgumentNullException(nameof(features));
        }

        public string SessionKey => (_features.RtspSettingsFeature._requestedControlProfile?.DeviceId ?? "") + "|" +
            _features.RtspSettingsFeature.SessionEoAddress + "|" + _features.PropertiesFeature.AiRtsp0OnnxIndex + "|" +
            string.Join("|", _features.PropertiesFeature.AiMappingList.Where(item => item.RtspIndex == 0).Select(item => item.OnnxIndex)) + "|" +
            _features.PropertiesFeature.AiRtspList.FirstOrDefault(item => item.Index == 0)?.Url;

        public double ZoomRatio => _features.InputCoordinatorFeature.ControlTuningFor(false).Ratio(_features.MoveControlFeature.GetCurrentPresetStandardZoom());

        public double Confidence => _features.PropertiesFeature.AiDisplayConfidenceThreshold;

        public bool CanIssueMove => !_features.CameraFeatureControlFeature.IsVideoCenterMoveActive &&
            DateTime.UtcNow.Ticks - _features.CameraFeatureControlFeature._lastCenterClickTicks >= TimeSpan.FromMilliseconds(300).Ticks;

        /// <summary>장비·AI·영상·렌즈·자세의 상태와 기존 자동 작업 간 충돌을 검사한다.</summary>
        public string GetBlockReason()
        {
            if (_features.AiFeature._isApplicationShutdownRequested || !_features.CameraFeatureControlFeature.CameraFeatureTransportReady)
            {
                return "장비 연결 해제/프로그램 종료";
            }

            if (_features.Runtime._aiDetectorClientService?.IsConnected != true || _features.PropertiesFeature.AiPowerStatusText != "ON")
            {
                return "AI 연결 해제";
            }

            if (_features.PropertiesFeature.IsOtherControlInputLocked || !_features.PropertiesFeature.IsOtherOperationCommandEnabled || _features.InputCoordinatorFeature._controlInputs.Count > 0)
            {
                return "수동 조작/제어 잠금";
            }

            if (_features.ScreenStateFeature._isHomePositionMoving || _features.ScreenStateFeature._isLaPresetScanRunning ||
                _features.ScreenStateFeature._isPresetScanRunning || _features.PanoramaFeature.IsPanoramaCaptureRunning ||
                _features.PanoramaFeature.IsPanoramaProcessingRunning || _features.RoutesFeature.IsRouteRunning)
            {
                return "HOME/SCAN/파노라마/경로 실행";
            }

            if (!_features.ScreenStateFeature._isEoFrameDisplayed || !_features.OpticalCenterFeature.OpticalFeedbackFresh(false) ||
                !_features.OpticalCenterFeature.SnapshotFrameFresh(false))
            {
                return "EO 영상/줌/PT 상태 수신 지연";
            }

            string aiUrl = _features.PropertiesFeature.AiRtspList.FirstOrDefault(item => item.Index == 0)?.Url;

            if (!Uri.TryCreate(aiUrl, UriKind.Absolute, out Uri ai) ||
                !Uri.TryCreate(_features.RtspSettingsFeature.SessionEoAddress, UriKind.Absolute, out Uri viewer) ||
                !string.Equals(ai.Host, viewer.Host, StringComparison.OrdinalIgnoreCase) || ai.Port != viewer.Port ||
                ai.PathAndQuery != viewer.PathAndQuery)
            {
                return "AI RTSP 0과 현재 EO 영상 주소 불일치";
            }

            return null;
        }

        /// <summary>수신 결과를 현재 모델의 실제 클래스명에 연결한다. 표시용 박스 유지 처리는 호출하지 않는다.</summary>
        public void ResolveClassNames(OpenCvWpfTracking.Models.AI.AiDetectionResult result)
        {
            _features.AiFeature.ResolveAiDetectionClassNames(result);
        }

        /// <summary>CMD07 응답으로 확인한 원본 해상도를 사용한다. 미확인 상태에서 1920×1080을 가정하지 않는다.</summary>
        public bool TryGetFrameSize(out int width, out int height)
        {
            width = height = 0;

            lock (_features.AiFeature._aiRtspVideoSizeSync)
            {
                if (!_features.AiFeature._aiRtspVideoSizes.TryGetValue(0, out Tuple<int, int, DateTime> size))
                {
                    return false;
                }

                width = size.Item1;
                height = size.Item2;
            }

            return width > 0 && height > 0 && width == _features.PropertiesFeature.EoVideoWidth && height == _features.PropertiesFeature.EoVideoHeight;
        }

        /// <summary>원본 박스 중심을 기존 REI 광학 투영과 LA/WEB 장비 이동 경로에 전달한다.</summary>
        public Task<bool> MoveAsync(TrackingTarget target, TrackingMotionLease lease)
        {
            return _features.CameraFeatureControlFeature.CenterVideoPointAsync(false, target.NormalizedX, target.NormalizedY,
                target.Width / (double)target.Height, target.Width, target.Height, lease);
        }

        /// <summary>입력 세대를 변경해 진행 중인 추적 송신을 무효화하고 PT 정지 후 도달 추적값을 비운다.</summary>
        public void StopOwnedMove()
        {
            Interlocked.Increment(ref _features.InputCoordinatorFeature._inputEpoch);

            try
            {
                if (!_features.Runtime._controlCommandService.StopPanTiltPositionMove())
                {
                    Common.ConsoleLogHelper.Warning("AUTO TRACKING", "PT 정지 송신 실패 / 실제 장비 정지 상태 확인 필요");
                }
            }
            finally
            {
                _features.MoveControlFeature.ClearActivePanTiltAbsoluteMove();
            }
        }

        /// <summary>체크 상태와 툴팁 상태를 기존 화면 알림으로 갱신한다.</summary>
        public void Publish(bool enabled, string status)
        {
            _features.ScreenStateFeature._isAutoTrackingEnabled = enabled;
            _features.Callbacks.Notify("IsAutoTrackingEnabled");
            _features.Callbacks.Notify("AutoTrackingStatusText");
            _features.Callbacks.Notify("IsControlInputLocked");
            _features.Callbacks.Notify("IsOperationCommandEnabled");
            _features.Callbacks.Notify("IsOperationLockOverlayVisible");
            _features.Callbacks.Notify("ControlLockTitle");
            _features.Callbacks.Notify("ControlLockMessage");
            _features.Callbacks.Notify("IsPanTiltSpeedControlEnabled");
        }
    }
}
