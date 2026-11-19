using OpenCvWpfTracking.Models.Main;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static OpenCvWpfTracking.ViewModels.Main.MainViewModel;

namespace OpenCvWpfTracking.ViewModels.Main
{
    /// <summary>파노라마 프리셋 연동에 필요한 협력 기능만 받는 경계입니다. 화면 객체의 내부 구현을 알지 않습니다.</summary>
    internal interface IPanoramaPresetsFeatureContext
    {
        System.Collections.Generic.Dictionary<string, RtspSourceOption> _axisCtecSources { get; }

        Services.Communication.IControlCommandServicePort _controlCommandService { get; }

        System.Collections.Generic.Dictionary<string, Tuple<string, int, int>> _controlInputs { get; }

        ContinuousMoveType _currentMoveType { get; set; }

        double _currentPan { get; set; }

        double _currentTilt { get; set; }

        bool _isDeviceConnectionRequested { get; set; }

        System.Collections.Generic.Dictionary<string, Tuple<int, int>> _issuedControlAxes { get; }

        CancellationTokenSource _laPresetSingleMoveCts { get; set; }

        Services.Communication.ITcpClientServicePort _laTcpService { get; }

        CancellationTokenSource _presetSingleMoveCts { get; set; }

        bool _videoCenterBusy { get; set; }

        string ControlLockMessage { get; }

        string ControlLockTitle { get; }

        string EoSourceAddress { get; set; }

        bool HasRegisteredEquipmentIdentity { get; }

        bool IsControlInputLocked { get; }

        bool IsEventAlertControlEnabled { get; }

        bool IsOperationCommandEnabled { get; }

        bool IsOperationLockOverlayVisible { get; }

        bool IsPanTiltSpeedControlEnabled { get; }

        bool IsSelectedControlAgentWebAgent { get; }

        string LaPresetCommandStatusText { get; set; }

        System.Collections.ObjectModel.ObservableCollection<PresetPointOption> LaPresetPoints { get; }

        /// <summary>
        /// PRESET 1 선택 지점으로 WPF 저장값 기준 PTZF 전체를 직접 복원한다. LA 0x05 한 번에 의존하지 않고 Pan, Tilt,
        /// EO/IR Zoom, EO/IR Focus를 현재 프로젝트의 실제 제어 경로로 각각 실행한다.
        /// </summary>
        Task MoveToSelectedLaPresetPointAsync();

        /// <summary>
        /// 기존 웹 프리셋 이동을 번호 클릭과 순회 연동에서 완료까지 기다릴 수 있도록 공유한다.
        /// </summary>
        Task MoveToSelectedPresetPointAsync();

        /// <summary>
        /// 바인딩 속성 변경 알림
        /// </summary>
        void OnPropertyChanged([global::System.Runtime.CompilerServices.CallerMemberName] string propertyName = null);

        string PanoramaDeviceKey { get; }

        bool PanoramaPoseFresh { get; }

        Services.Video.PanoramaReferenceState PanoramaReference { get; }

        string PresetCommandStatusText { get; set; }

        System.Collections.ObjectModel.ObservableCollection<PresetPointOption> PresetPoints { get; }

        string RegisteredEquipmentIdentity { get; }

        ControlAgentProfileOption SelectedControlAgentProfile { get; set; }

        PresetPointOption SelectedLaPresetPoint { get; set; }

        PresetPointOption SelectedPresetPoint { get; set; }

        /// <summary>
        /// 단일 MOVE TO PRESET 이동만 즉시 정지한다. AUTO SCAN 반복 상태에는 영향을 주지 않으며, Pan/Tilt 위치 이동 정지
        /// 명령(0x4F)을 각각 송신한다.
        /// </summary>
        void StopLaPresetMove();

        /// <summary>
        /// 단일 MOVE TO PRESET 이동만 즉시 정지한다. AUTO SCAN 반복 상태에는 영향을 주지 않으며, Pan/Tilt 위치 이동 정지
        /// 명령(0x4F)을 각각 송신한다.
        /// </summary>
        void StopPresetMove();
    }

    /// <summary>파노라마 프리셋 연동 담당 객체입니다. 상태를 보관하고 입력을 받아 결과를 알립니다.</summary>
    internal sealed class PanoramaPresetsFeature
    {

        #region [협력 객체와 초기화]

        private readonly IPanoramaPresetsFeatureContext _context;

        /// <summary>
        /// 파노라마 프리셋 기능에서 사용할 협력 객체를 연결하고 초기 상태를 준비한다.
        /// </summary>
        internal PanoramaPresetsFeature(IPanoramaPresetsFeatureContext context)
        {
            _context = context ?? throw new System.ArgumentNullException(nameof(context));
        }

        #endregion

        #region [상태 필드]

        internal CancellationTokenSource _panoramaPresetCts;

        internal string _panoramaPresetActiveContext;

        internal bool _panoramaPanTiltConfirmed;

        #endregion

        #region [화면 속성과 이벤트]

        internal bool IsPanoramaPresetRunning
        {
            get
            {
                return _panoramaPresetCts != null;
            }
        }

        internal string LegacyPanoramaPresetContext
        {
            get
            {
                return _context.PanoramaDeviceKey + "|" + _context.EoSourceAddress + "|" + OpenCvWpfTracking.Services.Video.PanoramaPresetRegistration.ReferenceStamp(_context.PanoramaDeviceKey + "|" + _context.EoSourceAddress);
            }
        }

        internal string PanoramaPresetContext
        {
            get
            {
                if (_context.HasRegisteredEquipmentIdentity)
                {
                    return _context.RegisteredEquipmentIdentity;
                }
                else
                {
                    return LegacyPanoramaPresetContext;
                }
            }
        }

        internal string PanoramaPresetContextHash
        {
            get
            {
                var registration = OpenCvWpfTracking.Services.Video.PanoramaPresetRegistration.ContextIdentity(LegacyPanoramaPresetContext);

                if (!_context.HasRegisteredEquipmentIdentity)
                {
                    return registration;
                }

                string directory = OpenCvWpfTracking.Services.Video.PanoramaPresetRegistration.DeviceDirectory(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Panoramas"), _context.IsSelectedControlAgentWebAgent, _context.PanoramaDeviceKey, _context.SelectedControlAgentProfile.DisplayName, _context.SelectedControlAgentProfile.DeviceId);

                return OpenCvWpfTracking.Services.Video.PanoramaPresetRegistration.EquipmentContextHash(directory, registration);
            }
        }

        internal bool CanStartPanoramaPresets
        {
            get
            {
                return !IsPanoramaPresetRunning && _context.IsOperationCommandEnabled && !_context.IsControlInputLocked && _context._isDeviceConnectionRequested && _context._laTcpService?.IsConnected == true && _context._laPresetSingleMoveCts == null && _context._presetSingleMoveCts == null && !_context._videoCenterBusy && _context._currentMoveType == ContinuousMoveType.None && _context._controlInputs.Count == 0 && _context._issuedControlAxes.Count == 0 && _context._axisCtecSources.Count == 0 && _context.PanoramaPoseFresh && !_context.PanoramaReference.Pending && _context.PanoramaReference.Journal.Count == 0;
            }
        }

        #endregion

        #region [동작 처리]

        /// <summary>
        /// 현재 연결 장비에 해당하는 기존 프리셋만 번호 순서로 복사한다.
        /// </summary>
        internal PresetPointOption[] GetPanoramaPresets()
        {
            return (_context.IsSelectedControlAgentWebAgent ? _context.PresetPoints : _context.LaPresetPoints).OrderBy(point => point.Number).ToArray();
        }

        /// <summary>
        /// 시작과 종료 모두 기존 제어 잠금 속성에 알림을 보내 화면과 키 입력을 함께 잠근다.
        /// </summary>
        internal void NotifyPanoramaPresetLock()
        {
            _context.OnPropertyChanged(nameof(IsPanoramaPresetRunning));

            _context.OnPropertyChanged(nameof(_context.IsOperationCommandEnabled));

            _context.OnPropertyChanged(nameof(_context.IsControlInputLocked));

            _context.OnPropertyChanged(nameof(_context.IsEventAlertControlEnabled));

            _context.OnPropertyChanged(nameof(_context.IsOperationLockOverlayVisible));

            _context.OnPropertyChanged(nameof(_context.IsPanTiltSpeedControlEnabled));

            _context.OnPropertyChanged(nameof(_context.ControlLockTitle));

            _context.OnPropertyChanged(nameof(_context.ControlLockMessage));
        }

        /// <summary>
        /// 번호 클릭은 기존 프리셋의 단일 PTZF 이동 경로를 사용한다. 이미지 좌표로 새로운 이동 각도를 계산하거나 별도 패킷을 만들지 않는다.
        /// </summary>
        internal async Task<bool> MovePanoramaPresetAsync(PresetPointOption point)
        {
            if (!CanStartPanoramaPresets || point == null || !GetPanoramaPresets().Contains(point) || point.Number < 1 || point.Number > 100)
            {
                throw new InvalidOperationException("장비 연결·위치·운용 잠금·번호를 확인하세요.");
            }

            string context = PanoramaPresetContext;

            bool web = _context.IsSelectedControlAgentWebAgent;

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
                    _context.SelectedPresetPoint = point;

                    await _context.MoveToSelectedPresetPointAsync();
                }
                else
                {
                    _context.SelectedLaPresetPoint = point;

                    await _context.MoveToSelectedLaPresetPointAsync();
                }

                cts.Token.ThrowIfCancellationRequested();

                string status = web ? _context.PresetCommandStatusText : _context.LaPresetCommandStatusText;

                bool sameContext = context == PanoramaPresetContext && _context.PanoramaPoseFresh && GetPanoramaPresets().Contains(point);

                bool full = sameContext && status != null && status.EndsWith(" COMPLETED", StringComparison.Ordinal);

                bool lensOnly = sameContext && IsPanoramaLensOnlyIncomplete(status, _panoramaPanTiltConfirmed, SignedPan(_context._currentPan - point.Pan), _context._currentTilt - point.Tilt);

                completed = full || lensOnly;

                if (!completed)
                {
                    throw new InvalidOperationException("PRESET 이동 완료 미확인 / " + status);
                }

                if (lensOnly)
                {
                    OpenCvWpfTracking.Common.ConsoleLogHelper.Warning("PANORAMA PRESET", "팬·틸트 도착 / 렌즈 확인 미완료 / P" + point.Number.ToString("00") + " / " + status);
                }

                return full;
            }
            finally
            {
                if (!completed && context == PanoramaPresetContext && _context._laTcpService?.IsConnected == true)
                {
                    _context._controlCommandService.StopPanTiltPositionMove();
                }

                _panoramaPresetCts = null;
                _panoramaPresetActiveContext = null;

                cts.Dispose();

                NotifyPanoramaPresetLock();
            }
        }

        /// <summary>
        /// 팬·틸트 도착은 확인됐으나 기존 렌즈 복원이 제한 시간 안에 끝나지 않은 경우를 구분한다.
        /// </summary>
        internal static bool IsPanoramaLensOnlyIncomplete(string status, bool confirmed, double panError, double tiltError)
        {
            return confirmed && status != null && status.EndsWith(" INCOMPLETE", StringComparison.Ordinal) && Math.Abs(panError) <= PresetPanTiltTargetTolerance + 1e-9 && Math.Abs(tiltError) <= PresetPanTiltTargetTolerance + 1e-9;
        }

        /// <summary>
        /// 연동 버튼과 공통 정지는 기존 단일 프리셋 이동의 취소 경로를 그대로 사용한다.
        /// </summary>
        internal void CancelPanoramaPresets()
        {
            if (_panoramaPresetCts == null)
            {
                return;
            }

            _panoramaPresetCts.Cancel();

            if (_panoramaPresetActiveContext != PanoramaPresetContext || _context._laTcpService?.IsConnected != true)
            {
                _context._laPresetSingleMoveCts?.Cancel();

                _context._presetSingleMoveCts?.Cancel();

                return;
            }

            if (_context._laPresetSingleMoveCts != null)
            {
                _context.StopLaPresetMove();
            }

            if (_context._presetSingleMoveCts != null)
            {
                _context.StopPresetMove();
            }
        }

        #endregion

    }
}
