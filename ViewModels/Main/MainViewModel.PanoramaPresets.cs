using OpenCvWpfTracking.Models.Main;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace OpenCvWpfTracking.ViewModels.Main
{
    public partial class MainViewModel
    {
        #region [파노라마 프리셋 연동 상태]

        private CancellationTokenSource _panoramaPresetCts;

        private string _panoramaPresetActiveContext;

        private bool _panoramaPanTiltConfirmed;

        public bool IsPanoramaPresetRunning => _panoramaPresetCts != null;

        public string PanoramaPresetContext => PanoramaDeviceKey + "|" + EoSourceAddress + "|" +
            OpenCvWpfTracking.Services.Video.PanoramaPresetRegistration.ReferenceStamp(PanoramaDeviceKey + "|" + EoSourceAddress);

        /// <summary>
        /// 현재 연결 장비에 해당하는 기존 프리셋만 번호 순서로 복사한다.
        /// </summary>
        public PresetPointOption[] GetPanoramaPresets()
        {
            return (IsSelectedControlAgentWebAgent ? PresetPoints : LaPresetPoints)
                .OrderBy(point => point.Number).ToArray();
        }

        public bool CanStartPanoramaPresets =>
            !IsPanoramaPresetRunning && IsOperationCommandEnabled && !IsControlInputLocked &&
            _isDeviceConnectionRequested && _laTcpService?.IsConnected == true &&
            _laPresetSingleMoveCts == null && _presetSingleMoveCts == null &&
            !_videoCenterBusy && _currentMoveType == ContinuousMoveType.None &&
            _controlInputs.Count == 0 && _issuedControlAxes.Count == 0 && _axisCtecSources.Count == 0 &&
            PanoramaPoseFresh && !PanoramaReference.Pending && PanoramaReference.Journal.Count == 0;

        /// <summary>
        /// 시작과 종료 모두 기존 제어 잠금 속성에 알림을 보내 화면과 키 입력을 함께 잠근다.
        /// </summary>
        private void NotifyPanoramaPresetLock()
        {
            OnPropertyChanged(nameof(IsPanoramaPresetRunning));
            OnPropertyChanged(nameof(IsOperationCommandEnabled));
            OnPropertyChanged(nameof(IsControlInputLocked));
            OnPropertyChanged(nameof(IsEventAlertControlEnabled));
            OnPropertyChanged(nameof(IsOperationLockOverlayVisible));
            OnPropertyChanged(nameof(IsPanTiltSpeedControlEnabled));
            OnPropertyChanged(nameof(ControlLockTitle));
            OnPropertyChanged(nameof(ControlLockMessage));
        }

        #endregion

        #region [기존 프리셋 단일 이동]

        /// <summary>
        /// 번호 클릭은 기존 프리셋의 단일 PTZF 이동 경로를 사용한다.
        /// 이미지 좌표로 새로운 이동 각도를 계산하거나 별도 패킷을 만들지 않는다.
        /// </summary>
        public async Task<bool> MovePanoramaPresetAsync(PresetPointOption point)
        {
            if (!CanStartPanoramaPresets || point == null || !GetPanoramaPresets().Contains(point) ||
                point.Number < 1 || point.Number > 100)
                throw new InvalidOperationException("장비 연결·위치·운용 잠금·번호를 확인하세요.");
            string context = PanoramaPresetContext;
            bool web = IsSelectedControlAgentWebAgent;
            var cts = new CancellationTokenSource();
            _panoramaPresetActiveContext = context;
            _panoramaPresetCts = cts;
            NotifyPanoramaPresetLock();
            bool completed = false;
            _panoramaPanTiltConfirmed = false;
            try
            {
                if (web)
                {
                    SelectedPresetPoint = point;
                    await MoveToSelectedPresetPointAsync();
                }
                else
                {
                    SelectedLaPresetPoint = point;
                    await MoveToSelectedLaPresetPointAsync();
                }
                cts.Token.ThrowIfCancellationRequested();
                string status = web ? PresetCommandStatusText : LaPresetCommandStatusText;
                bool sameContext = context == PanoramaPresetContext && PanoramaPoseFresh && GetPanoramaPresets().Contains(point);
                bool full = sameContext && status != null && status.EndsWith(" COMPLETED", StringComparison.Ordinal);
                bool lensOnly = sameContext && IsPanoramaLensOnlyIncomplete(status, _panoramaPanTiltConfirmed,
                    SignedPan(_currentPan - point.Pan), _currentTilt - point.Tilt);
                completed = full || lensOnly;
                if (!completed)
                    throw new InvalidOperationException("PRESET 이동 완료 미확인 / " + status);
                if (lensOnly)
                    OpenCvWpfTracking.Common.ConsoleLogHelper.Warning("PANORAMA PRESET",
                        "팬·틸트 도착 / 렌즈 확인 미완료 / P" + point.Number.ToString("00") + " / " + status);
                return full;
            }
            finally
            {
                if (!completed && context == PanoramaPresetContext && _laTcpService?.IsConnected == true)
                    _controlCommandService.StopPanTiltPositionMove();
                _panoramaPresetCts = null;
                _panoramaPresetActiveContext = null;
                cts.Dispose();
                NotifyPanoramaPresetLock();
            }
        }

        /// <summary>팬·틸트 도착은 확인됐으나 기존 렌즈 복원이 제한 시간 안에 끝나지 않은 경우를 구분한다.</summary>
        private static bool IsPanoramaLensOnlyIncomplete(string status, bool confirmed, double panError, double tiltError) =>
            confirmed && status != null && status.EndsWith(" INCOMPLETE", StringComparison.Ordinal) &&
            Math.Abs(panError) <= PresetPanTiltTargetTolerance + 1e-9 &&
            Math.Abs(tiltError) <= PresetPanTiltTargetTolerance + 1e-9;

        /// <summary>
        /// 연동 버튼과 공통 정지는 기존 단일 프리셋 이동의 취소 경로를 그대로 사용한다.
        /// </summary>
        public void CancelPanoramaPresets()
        {
            if (_panoramaPresetCts == null)
                return;

            _panoramaPresetCts.Cancel();

            if (_panoramaPresetActiveContext != PanoramaPresetContext || _laTcpService?.IsConnected != true)
            {
                _laPresetSingleMoveCts?.Cancel();
                _presetSingleMoveCts?.Cancel();
                return;
            }

            if (_laPresetSingleMoveCts != null)
                StopLaPresetMove();

            if (_presetSingleMoveCts != null)
                StopPresetMove();
        }

        #endregion
    }
}
