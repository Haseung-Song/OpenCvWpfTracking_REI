using System;
using OpenCvWpfTracking.Models.Main;
using OpenCvWpfTracking.Services.Position;

namespace OpenCvWpfTracking.ViewModels.Main
{
    /// <summary>
    /// 화면 알림과 기존 이벤트의 콜백을 보관한다. 등록과 해제에 같은 대상의 델리게이트를 사용한다.
    /// 기능 판단이나 장비 통신을 수행하지 않는 화면 경계이다.
    /// </summary>
    internal sealed class FeatureCallbacks
    {

        // 화면 알림과 AI 수신 이벤트는 기존 화면 모델을 호출 대상으로 유지한다.
        internal Action<string> Notify { get; set; }
        internal Action DrainPendingAiUiUpdatesCallback { get; set; }
        internal Action<byte[], DateTime> OnAiDetectorPacketReceivedCallback { get; set; }
        internal EventHandler HandleDevicePowerFreshnessTickCallback { get; set; }

        // AI·화재 이벤트의 저장·불러오기·삭제를 화면 명령과 연결한다.
        internal Action SaveAiEventsCallback { get; set; }
        internal Action LoadAiEventsCallback { get; set; }
        internal Action ClearAiEventsCallback { get; set; }
        internal Action SaveFireEventsCallback { get; set; }
        internal Action LoadFireEventsCallback { get; set; }
        internal Action ClearFireEventsCallback { get; set; }

        // GPS·자세 설정은 원래 명령 진입점을 호출하며, 여기에서 통신을 시작하지 않는다.
        internal Action RequestGpsTelemetryCallback { get; set; }
        internal Action ApplyGpsAutoTxRateCallback { get; set; }
        internal Action RequestGpsAutoTxRateCallback { get; set; }
        internal Action UpdateGpsHeadingReferenceDisplayCallback { get; set; }
        internal Action ApplyRpyOffsetCallback { get; set; }
        internal Action ApplyRpyAutoTxRateCallback { get; set; }
        internal Action RequestRpyTelemetryCallback { get; set; }
        internal Action RequestStoredRpyOffsetCallback { get; set; }
        internal Action RequestStoredRpyRateCallback { get; set; }

        // 카메라 특수 기능의 지원 조회·설정·상태 조회에 사용할 호출을 보관한다.
        internal Action QuerySpecialFeatureSupportCallback { get; set; }
        internal Action<CameraSpecialFeatureRow, int> SendSpecialFeatureCallback { get; set; }
        internal Action<CameraSpecialFeatureRow> QuerySpecialFeatureStateCallback { get; set; }

        // 사용자의 시간 조회 요청과 시간 설정 성공 후 재조회는 호출 목적을 이름으로 구분한다.
        internal Action RequestSystemDateTimeCommandCallback { get; set; }
        internal Action SetSystemDateTimeFromPcCallback { get; set; }
        internal EventHandler HandleSystemTimePollTickCallback { get; set; }
        internal Action RefreshSystemDateTimeAfterSetCallback { get; set; }

        // 온도·자동 초점·특수 기능 응답은 기존 처리 대상으로 전달한다.
        internal Action QueryEquipmentTemperatureCallback { get; set; }
        internal Action<object> ExecuteAutoFocusCallback { get; set; }
        internal Predicate<object> CanExecuteAutoFocusCallback { get; set; }
        internal Action<byte[]> ApplySpecialFeatureCapabilityCallback { get; set; }
        internal Action<byte[]> ApplySpecialFeatureStateCallback { get; set; }
        internal Action<byte[]> ApplySpecialFeatureUnavailableCallback { get; set; }

        // 구역 표시와 탐지 판정은 같은 기준 함수를 사용하되, 사용하는 경로를 이름으로 구분한다.
        internal Func<FireSmokeScene, bool> IsZonePresetCurrentForDisplayCallback { get; set; }
        internal Func<FireSmokeScene, bool> IsZonePresetCurrentForDetectionCallback { get; set; }
    }
}
