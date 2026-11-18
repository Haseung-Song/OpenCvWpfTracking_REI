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

        Task MoveToSelectedLaPresetPointAsync();

        Task MoveToSelectedPresetPointAsync();

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

        void StopLaPresetMove();

        void StopPresetMove();
    }

    /// <summary>파노라마 프리셋 연동 담당 객체입니다. 상태를 보관하고 입력을 받아 결과를 알립니다.</summary>
    internal sealed class PanoramaPresetsFeature
    {

        #region [협력 객체와 초기화]

        private readonly IPanoramaPresetsFeatureContext _context;

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

        internal PresetPointOption[] GetPanoramaPresets()
        {
            return (_context.IsSelectedControlAgentWebAgent ? _context.PresetPoints : _context.LaPresetPoints).OrderBy(point => point.Number).ToArray();
        }

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

        internal static bool IsPanoramaLensOnlyIncomplete(string status, bool confirmed, double panError, double tiltError)
        {
            return confirmed && status != null && status.EndsWith(" INCOMPLETE", StringComparison.Ordinal) && Math.Abs(panError) <= PresetPanTiltTargetTolerance + 1e-9 && Math.Abs(tiltError) <= PresetPanTiltTargetTolerance + 1e-9;
        }

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
