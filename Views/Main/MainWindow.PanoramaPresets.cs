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

        /// <summary>번호 레이어와 좌표 JSON 읽기를 연결한다.</summary>
        private void InitializePanoramaPresets()
        {
            // 마지막으로 연동한 외부 사진도 프로그램 재실행 시 같은 경로에서 복원한다.
            string remembered = PanoramaPresetRegistration.ReadAtomic<string>(
                PanoramaPresetRegistration.LastImagePath(PanoramaPresetRegistration.ContextIdentity(vm.PanoramaPresetContext)));
            if (!string.IsNullOrWhiteSpace(remembered) && System.IO.File.Exists(remembered))
            {
                try
                {
                    DisplayPanoramaFile(remembered, "Config last panorama restored");
                }
                catch (Exception ex)
                {
                    ConsoleLogHelper.Warning("PANORAMA STORAGE", "이전 사진 복원 실패: " + ex.Message);
                }
            }
            PanoramaPresetHost.Children.Add(_panoramaPresetOverlay);
            _panoramaPresetOverlay.PresetClicked += MovePanoramaPreset;
            vm.PositionReferenceChanged += InvalidatePanoramaPresetLinks;
            _panoramaPresetTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(300) };
            _panoramaPresetTimer.Tick += RefreshPanoramaPresetLinks;
            _panoramaPresetTimer.Start();
            Closed += (sender, args) =>
            {
                _panoramaPresetTimer.Stop();
                vm.PositionReferenceChanged -= InvalidatePanoramaPresetLinks;
                vm.CancelPanoramaPresets();
            };
        }

        /// <summary>사진·장비가 바뀔 때 좌표 JSON을 읽고 현재 PRESET을 다시 투영한다.</summary>
        private void RefreshPanoramaPresetLinks(object sender, EventArgs e)
        {
            if (_isMainWindowClosing)
                return;
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
            LinkPanoramaPresetsButton.IsEnabled = _panoramaCoordinates != null &&
                !vm.IsPanoramaPresetRunning && !vm.IsPanoramaProcessingRunning && vm.GetPanoramaPresets().Length > 0;
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
            var points = vm.GetPanoramaPresets();
            if (_panoramaCoordinates != null)
            {
                foreach (var point in points.Where(point => _panoramaCoordinates.LinkedPresetNumbers?.Contains(point.Number) == true))
                {
                    var reference = vm.PanoramaReference;
                    var link = PanoramaPresetRegistration.Project(_panoramaCoordinates, point, reference.PanOffset, reference.TiltOffset);
                    if (link != null)
                        _panoramaPresetLinks.Add(link);
                }
            }
            DrawPanoramaPresetLinks();
            if (!vm.IsPanoramaPresetRunning)
                PanoramaPresetStatusText.Text = vm.PanoramaReference.Pending || _panoramaCoordinates?.ReferenceUncertain == true
                    ? "PRESET REFERENCE CHECK REQUIRED" : _panoramaCoordinates == null ? "PRESET COORDINATES UNAVAILABLE" :
                    "PRESETS " + _panoramaPresetLinks.Count + " · CLICK TO MOVE";
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

            UpdatePanoramaPresetMarkers();
        }

        /// <summary>표시할 기존 번호만 저장하며 카메라는 이동하지 않는다.</summary>
        private void LinkPanoramaPresetsButton_Click(object sender, RoutedEventArgs e)
        {
            if (_panoramaCoordinates == null || vm.IsPanoramaPresetRunning || vm.IsPanoramaProcessingRunning)
                return;
            if (_panoramaCoordinates.ReferenceUncertain)
            {
                var answer = MessageBox.Show(this,
                    "이전 버전에서 좌표 기준이 무효화된 사진입니다.\n\n" +
                    "같은 장비이며 촬영 이후 PAN/TILT ZERO를 변경하지 않았는지 확인하십시오.\n" +
                    "같은 좌표 기준으로 다시 연동하시겠습니까?",
                    "PRESET REFERENCE CHECK", MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No);
                if (answer != MessageBoxResult.Yes)
                    return;
                _panoramaCoordinates.ReferenceUncertain = false;
            }
            _panoramaCoordinates.LinkedPresetNumbers = vm.GetPanoramaPresets().Select(point => point.Number).ToList();
            SavePanoramaPresetCoordinates();
            UpdatePanoramaPresetMarkers();
        }

        /// <summary>Config와 JPG 옆 JSON에 연동을 저장하며 기존 프리셋 저장 자료는 유지한다.</summary>
        private void SavePanoramaPresetCoordinates()
        {
            try
            {
                if (_panoramaCoordinates != null && !string.IsNullOrWhiteSpace(_linkedPanoramaPath))
                    PanoramaPresetRegistration.Save(_linkedPanoramaPath, _panoramaCoordinates);
            }
            catch (Exception ex)
            {
                ConsoleLogHelper.Warning("PANORAMA COORDINATES", "좌표 JSON 저장 실패: " + ex.Message);
            }
        }

        /// <summary>번호 클릭은 기존 PRESET 이동으로만 전달한다.</summary>
        private async void MovePanoramaPreset(int number)
        {
            if (!ReferenceEquals(PanoramaImage.Source, _linkedPanoramaSource) ||
                vm.PanoramaPresetContext != _linkedPanoramaContext || _panoramaCoordinates?.ReferenceUncertain == true || !vm.CanStartPanoramaPresets)
                return;
            var point = vm.GetPanoramaPresets().FirstOrDefault(preset => preset.Number == number);
            if (point == null || !_panoramaPresetLinks.Any(link => link.Number == number &&
                link.Fingerprint == PanoramaPresetRegistration.PresetIdentity(point)))
                return;
            try
            {
                PanoramaPresetStatusText.Text = "P" + number.ToString("00") + " PTZF MOVING";
                await vm.MovePanoramaPresetAsync(point);
                PanoramaPresetStatusText.Text = "P" + number.ToString("00") + " PTZF MOVE COMPLETED";
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
        }
    }
}
