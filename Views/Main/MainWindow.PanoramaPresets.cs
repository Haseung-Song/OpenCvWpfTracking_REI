using OpenCvWpfTracking.Common;
using OpenCvWpfTracking.Services.Video;
using OpenCvWpfTracking.Views.Video;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace OpenCvWpfTracking
{
    public partial class MainWindow
    {
        private readonly List<PanoramaPresetLink> _panoramaPresetLinks = new List<PanoramaPresetLink>();

        private readonly PanoramaPresetOverlay _panoramaPresetOverlay = new PanoramaPresetOverlay();

        private DispatcherTimer _panoramaPresetTimer;

        private PanoramaCoordinateMetadata _panoramaCoordinates;

        private BitmapSource _linkedPanoramaSource;

        private string _linkedPanoramaContext;

        private string _linkedPanoramaPath;

        private DateTime _panoramaMoveStatusUntilUtc;

        private bool _panoramaPresetCoordinatesDirty;

        private DateTime _panoramaPresetSaveRetryUtc;

        private bool _panoramaPresetSaveFailed;

        /// <summary>번호 레이어와 좌표 JSON 읽기를 연결한다.</summary>
        private void InitializePanoramaPresets()
        {
            PanoramaPresetHost.Children.Add(_panoramaPresetOverlay);
            _panoramaPresetOverlay.PresetClicked += MovePanoramaPreset;
            vm.PositionReferenceChanged += InvalidatePanoramaPresetLinks;
            vm.PropertyChanged += ContextPanels_PropertyChanged;
            _panoramaPresetTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(300) };
            _panoramaPresetTimer.Tick += RefreshPanoramaPresetLinks;
            _panoramaPresetTimer.Start();
            RefreshPanoramaPresetLinks(null, EventArgs.Empty);
            Closed += (sender, args) =>
            {
                _panoramaPresetTimer.Stop();
                vm.PositionReferenceChanged -= InvalidatePanoramaPresetLinks;
                vm.PropertyChanged -= ContextPanels_PropertyChanged;
                vm.CancelPanoramaPresets();
            };
        }

        /// <summary>
        /// TCP 연결 해제 시 표시와 확대 창만 정리한다. 사진·JSON·연동 목록은 삭제하지 않는다.
        /// </summary>
        private void ContextPanels_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName != nameof(vm.IsContextPanelsVisible) || _isMainWindowClosing)
            {
                return;
            }

            // 프로필의 IP·영상 주소 바인딩까지 갱신된 뒤 같은 장비 키로 한 번에 표시한다.
            Dispatcher.BeginInvoke(new Action(() =>
            {
                if (_isMainWindowClosing)
                {
                    return;
                }

                if (!vm.IsContextPanelsVisible)
                {
                    vm.CancelPanoramaPresets();
                    ClosePanoramaPreview();
                    _companyMapWindow?.Close();
                }

                RefreshPanoramaPresetLinks(null, EventArgs.Empty);
            }));
        }

        /// <summary>사진·장비가 바뀔 때 좌표 JSON을 읽고 현재 PRESET을 다시 투영한다.</summary>
        private void RefreshPanoramaPresetLinks(object sender, EventArgs e)
        {
            if (_isMainWindowClosing)
                return;
            if (_displayedPanoramaDeviceContext != PanoramaImageDeviceContext)
            {
                // 장비만 전환된 경우 이전 장비의 사진을 새 장비의 마지막 사진으로 기록하지 않는다.
                RestorePanoramaForCurrentDevice();
            }
            var source = PanoramaImage.Source as BitmapSource;
            string context = vm.PanoramaPresetContext;
            if (!ReferenceEquals(source, _linkedPanoramaSource) || context != _linkedPanoramaContext ||
                _currentPanoramaFilePath != _linkedPanoramaPath)
            {
                vm.CancelPanoramaPresets();
                _linkedPanoramaSource = source;
                _linkedPanoramaContext = context;
                _linkedPanoramaPath = _currentPanoramaFilePath;
                _panoramaCoordinates = null;
                _panoramaPresetCoordinatesDirty = false;
                _panoramaPresetSaveRetryUtc = DateTime.MinValue;
                _panoramaPresetSaveFailed = false;
                try
                {
                    if (source != null)
                    {
                        string contextHash = PanoramaPresetRegistration.ContextIdentity(context);
                        _panoramaCoordinates = PanoramaPresetRegistration.Load(_linkedPanoramaPath, contextHash, source.PixelWidth, source.PixelHeight);
                        PanoramaPresetRegistration.RememberImage(_linkedPanoramaPath, contextHash);
                    }
                }
                catch (Exception ex)
                {
                    ConsoleLogHelper.Warning("PANORAMA COORDINATES", "좌표 정보 읽기 실패: " + ex.Message);
                }
            }
            if (!string.IsNullOrWhiteSpace(_linkedPanoramaPath))
                vm.RecoverPanoramaPresetReference();
            UpdatePanoramaPresetMarkers();
            UpdatePanoramaPresetLinkButton();
            if (!vm.IsPanoramaProcessingRunning)
            {
                NewPanoramaButton.IsEnabled = !vm.IsPanoramaPresetRunning;
                LoadPanoramaImageButton.IsEnabled = !vm.IsPanoramaPresetRunning;
            }
        }

        /// <summary>연동된 기존 번호의 PAN/TILT를 투영하며 범위 밖 지점은 숨긴다.</summary>
        private void UpdatePanoramaPresetMarkers()
        {
            _panoramaPresetLinks.Clear();
            if (!ReferenceEquals(PanoramaImage.Source, _linkedPanoramaSource) ||
                vm.PanoramaPresetContext != _linkedPanoramaContext || _currentPanoramaFilePath != _linkedPanoramaPath)
            {
                // 장비 전환 알림과 다음 타이머 사이에는 이전 사진 자료를 새 목록으로 저장하지 않는다.
                DrawPanoramaPresetLinks();
                return;
            }

            var points = vm.GetPanoramaPresets();
            var reference = vm.PanoramaReference;
            bool allowCapture = vm.IsContextPanelsVisible && !reference.Pending && reference.Journal.Count == 0 &&
                _panoramaCoordinates?.ReferenceUncertain == false;
            bool changed = allowCapture && PanoramaPresetRegistration.SynchronizeLinkedPresetNumbers(_panoramaCoordinates, points);

            // 이전 자료(null)는 기존 번호를 표시하되 자동 연동으로 임의 전환하지 않는다.
            if (_panoramaCoordinates != null && _panoramaCoordinates.AutoLinkEnabled != false)
            {
                foreach (var point in points.Where(point => _panoramaCoordinates.LinkedPresetNumbers?.Contains(point.Number) == true))
                {
                    bool captured;
                    var link = PanoramaPresetRegistration.ResolveLink(_panoramaCoordinates, point,
                        reference.PanOffset, reference.TiltOffset, allowCapture, out captured);
                    changed |= captured;
                    if (link != null)
                        _panoramaPresetLinks.Add(link);
                }
            }
            if (changed)
                _panoramaPresetCoordinatesDirty = true;

            // 변경 없는 300ms 타이머는 쓰지 않으며 저장 실패 시 5초 간격으로만 재시도한다.
            if (_panoramaPresetCoordinatesDirty && DateTime.UtcNow >= _panoramaPresetSaveRetryUtc)
                SavePanoramaPresetCoordinates();

            DrawPanoramaPresetLinks();
            if (!vm.IsContextPanelsVisible)
                PanoramaPresetStatusText.Text = "DEVICE DISCONNECTED";
            else if (!vm.IsPanoramaPresetRunning && DateTime.UtcNow >= _panoramaMoveStatusUntilUtc)
                PanoramaPresetStatusText.Text = _panoramaPresetSaveFailed
                    ? (_panoramaPresetCoordinatesDirty ? "PRESET LINK SAVE FAILED · RETRY PENDING" : "PRESET LINK SAVE FAILED · STATE UNCHANGED") :
                    _panoramaCoordinates?.AutoLinkEnabled == false ? "PRESET AUTO LINK OFF" :
                    vm.PanoramaReference.Pending || vm.PanoramaReference.Journal.Count > 0 || _panoramaCoordinates?.ReferenceUncertain == true ||
                    _panoramaPresetLinks.Any(link => !link.ReferenceValid)
                    ? "PRESET REFERENCE CHECK REQUIRED" : _panoramaCoordinates == null ? "PRESET COORDINATES UNAVAILABLE" :
                    _panoramaCoordinates.AutoLinkEnabled == true
                    ? "PRESET AUTO LINK ON · " + _panoramaPresetLinks.Count + " VISIBLE" :
                    "PRESETS " + _panoramaPresetLinks.Count + " · CLICK TO MOVE";
        }

        /// <summary>자동 연동 해제는 미확정 기준에서도 허용하며 활성화 조건과 분리한다.</summary>
        private void UpdatePanoramaPresetLinkButton()
        {
            bool enabled = _panoramaCoordinates?.AutoLinkEnabled == true;
            LinkPanoramaPresetsButton.Tag = enabled ? "AutoLinked" : null;
            LinkPanoramaPresetsButton.IsEnabled = _panoramaCoordinates != null && (enabled ||
                (vm.IsContextPanelsVisible && vm.IsOperationCommandEnabled && !vm.IsControlInputLocked &&
                !vm.PanoramaReference.Pending && vm.PanoramaReference.Journal.Count == 0));
        }

        /// <summary>같은 정규화 좌표를 메인 영상과 확대 창에 전달한다.</summary>
        private void DrawPanoramaPresetLinks()
        {
            var visible = vm.IsContextPanelsVisible ? _panoramaPresetLinks : Enumerable.Empty<PanoramaPresetLink>();
            int[] order = vm.GetPanoramaPresets().Select(point => point.Number).ToArray();
            _panoramaPresetOverlay.Update(_linkedPanoramaSource, visible, order);
            _panoramaPreviewWindow?.UpdatePresetLinks(visible, order, MovePanoramaPreset);
        }

        /// <summary>기준 변경 알림에서는 진행 중 이동만 취소하고 저장된 연동을 현재 기준으로 다시 그린다.</summary>
        private void InvalidatePanoramaPresetLinks()
        {
            if (_isMainWindowClosing || Dispatcher.HasShutdownStarted)
                return;
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.BeginInvoke(new Action(InvalidatePanoramaPresetLinks));
                return;
            }
            vm.CancelPanoramaPresets();
            // HOME·연결 변경·광학 보정 알림은 저장된 연동을 지우지 않는다.
            ConsoleLogHelper.State("PANORAMA REFERENCE", "좌표 기준 알림 / 연동 보존");

            // 프로필의 영상 주소 갱신까지 마친 다음 해당 사진·장비 자료부터 다시 읽는다.
            Dispatcher.BeginInvoke(new Action(() => RefreshPanoramaPresetLinks(null, EventArgs.Empty)));
        }

        /// <summary>장비·사진별 자동 연동을 저장한 뒤 버튼 상태를 확정한다. 카메라는 이동하지 않는다.</summary>
        private void LinkPanoramaPresetsButton_Click(object sender, RoutedEventArgs e)
        {
            // 사진·장비 변경 직후의 클릭도 먼저 해당 자료를 복원한다.
            RefreshPanoramaPresetLinks(null, EventArgs.Empty);
            if (_panoramaCoordinates == null || !LinkPanoramaPresetsButton.IsEnabled)
                return;

            bool turnOn = _panoramaCoordinates.AutoLinkEnabled != true;
            if (turnOn && _panoramaCoordinates.ReferenceUncertain)
            {
                var confirmedCoordinates = _panoramaCoordinates;
                var answer = MessageBox.Show(this,
                    "이전 버전에서 좌표 기준이 무효화된 사진입니다.\n\n" +
                    "같은 장비이며 촬영 이후 PAN/TILT ZERO를 변경하지 않았는지 확인하십시오.\n" +
                    "같은 좌표 기준으로 다시 연동하시겠습니까?",
                    "PRESET REFERENCE CHECK", MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No);
                if (answer != MessageBoxResult.Yes)
                    return;

                // 확인 창이 열린 동안 장비·사진 또는 작업 상태가 바뀌면 해당 확인을 재사용하지 않는다.
                RefreshPanoramaPresetLinks(null, EventArgs.Empty);
                if (!ReferenceEquals(confirmedCoordinates, _panoramaCoordinates) || !LinkPanoramaPresetsButton.IsEnabled)
                    return;
            }

            bool? previousMode = _panoramaCoordinates.AutoLinkEnabled;
            bool previousUncertain = _panoramaCoordinates.ReferenceUncertain;
            var previousNumbers = _panoramaCoordinates.LinkedPresetNumbers;
            var previousAnchors = _panoramaCoordinates.PresetAnchors;
            bool previousDirty = _panoramaPresetCoordinatesDirty;

            // 기존 앵커를 복사해 변경된 번호만 교체한다. ZERO로 고정된 사진 위치는 그대로 둔다.
            _panoramaCoordinates.PresetAnchors = previousAnchors == null ? null : new List<PanoramaPresetAnchor>(previousAnchors);
            _panoramaCoordinates.AutoLinkEnabled = turnOn;
            if (turnOn)
            {
                _panoramaCoordinates.ReferenceUncertain = false;
                var points = vm.GetPanoramaPresets();
                PanoramaPresetRegistration.SynchronizeLinkedPresetNumbers(_panoramaCoordinates, points);
                foreach (var point in points)
                {
                    bool captured;
                    PanoramaPresetRegistration.ResolveLink(_panoramaCoordinates, point,
                        vm.PanoramaReference.PanOffset, vm.PanoramaReference.TiltOffset, true, out captured);
                }
            }

            if (!SavePanoramaPresetCoordinates())
            {
                _panoramaCoordinates.AutoLinkEnabled = previousMode;
                _panoramaCoordinates.ReferenceUncertain = previousUncertain;
                _panoramaCoordinates.LinkedPresetNumbers = previousNumbers;
                _panoramaCoordinates.PresetAnchors = previousAnchors;
                _panoramaPresetCoordinatesDirty = previousDirty;
                PanoramaPresetStatusText.Text = "PRESET LINK SAVE FAILED · STATE UNCHANGED";
                _panoramaMoveStatusUntilUtc = DateTime.UtcNow.AddSeconds(5);
            }

            UpdatePanoramaPresetMarkers();
            UpdatePanoramaPresetLinkButton();
        }

        /// <summary>Config와 JPG 옆 JSON에 연동을 저장하며 기존 프리셋 저장 자료는 유지한다.</summary>
        private bool SavePanoramaPresetCoordinates()
        {
            try
            {
                if (_panoramaCoordinates == null || string.IsNullOrWhiteSpace(_linkedPanoramaPath))
                    return false;

                PanoramaPresetRegistration.Save(_linkedPanoramaPath, _panoramaCoordinates);
                _panoramaPresetCoordinatesDirty = false;
                _panoramaPresetSaveFailed = false;
                _panoramaPresetSaveRetryUtc = DateTime.MinValue;
                return true;
            }
            catch (Exception ex)
            {
                _panoramaPresetSaveFailed = true;
                _panoramaPresetSaveRetryUtc = DateTime.UtcNow.AddSeconds(5);
                ConsoleLogHelper.Warning("PANORAMA COORDINATES", "좌표 JSON 저장 실패: " + ex.Message);
                return false;
            }
        }

        /// <summary>번호 클릭은 기존 PRESET 이동으로만 전달한다.</summary>
        private async void MovePanoramaPreset(int number)
        {
            if (!ReferenceEquals(PanoramaImage.Source, _linkedPanoramaSource) ||
                vm.PanoramaPresetContext != _linkedPanoramaContext || _panoramaCoordinates?.ReferenceUncertain == true || !vm.CanStartPanoramaPresets)
                return;
            var point = vm.GetPanoramaPresets().FirstOrDefault(preset => preset.Number == number);
            if (point == null || !_panoramaPresetLinks.Any(link => link.Number == number && link.ReferenceValid &&
                link.Fingerprint == PanoramaPresetRegistration.PresetIdentity(point)))
                return;
            try
            {
                PanoramaPresetStatusText.Text = "P" + number.ToString("00") + " PTZF MOVING";
                bool full = await vm.MovePanoramaPresetAsync(point);
                PanoramaPresetStatusText.Text = "P" + number.ToString("00") +
                    (full ? (string.IsNullOrEmpty(vm.PresetFocusSkipText) ? " PTZF MOVE COMPLETED" :
                        " PTZ MOVE COMPLETED / " + vm.PresetFocusSkipText) : " PT ARRIVED / LENS UNCONFIRMED");
            }
            catch (OperationCanceledException)
            {
                PanoramaPresetStatusText.Text = "PRESET MOVE STOPPED";
            }
            catch (Exception ex)
            {
                PanoramaPresetStatusText.Text = "PRESET MOVE UNCONFIRMED · CHECK LOG";
                ConsoleLogHelper.Error("PANORAMA PRESET", "기존 PRESET 이동 실패", ex);
            }
            finally
            {
                _panoramaMoveStatusUntilUtc = DateTime.UtcNow.AddSeconds(5);
            }
        }
    }
}

