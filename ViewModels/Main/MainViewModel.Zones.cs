using OpenCvWpfTracking.Services.Position;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows.Media.Imaging;
using OpenCvWpfTracking.Models.Main;
using OpenCvWpfTracking.Services.Video;

namespace OpenCvWpfTracking.ViewModels.Main
{
    public partial class MainViewModel
    {
        public sealed class ZoneOverlayBox
        {
            public double Left { get; set; }

            public double Top { get; set; }

            public double Width { get; set; }

            public double Height { get; set; }

            public List<System.Windows.Point> Polygon { get; set; }

            public string Name { get; set; }

            public string Color { get; set; }
        }

        public ObservableCollection<ZoneOverlayBox> EoZoneOverlay
        {
            get { return _zonesFeature.EoZoneOverlay; }
            private set { _zonesFeature.EoZoneOverlay = value; }
        }

        public ObservableCollection<ZoneOverlayBox> IrZoneOverlay
        {
            get { return _zonesFeature.IrZoneOverlay; }
            private set { _zonesFeature.IrZoneOverlay = value; }
        }

        public bool ShowZoneOverlay
        {
            get { return _zonesFeature.ShowZoneOverlay; }
            set { _zonesFeature.ShowZoneOverlay = value; }
        }

        public int ZoneCalibrationGeneration
        {
            get { return _zonesFeature.ZoneCalibrationGeneration; }
        }

        public bool IsZoneReferencePending
        {
            get { return _zonesFeature.IsZoneReferencePending; }
        }

        public event Action<PanoramaZoneRebase> ZoneReferenceChanged
        {
            add
            {
                _zonesFeature.ZoneReferenceChanged += value;
            }

            remove
            {
                _zonesFeature.ZoneReferenceChanged -= value;
            }
        }

        public void RefreshZoneLensState()
        {
            _zonesFeature.RefreshZoneLensState();
        }

        public string ZoneStatusText
        {
            get { return _zonesFeature.ZoneStatusText; }
        }

        public string ZoneEquipmentKey
        {
            get { return _zonesFeature.ZoneEquipmentKey; }
        }

        public sealed class ZonePresetOption
        {
            public string Kind { get; set; }

            public PresetPointOption Point { get; set; }

            public override string ToString()
            {
                return Kind + " P" + Point.Number.ToString("00") + " / " + Point.Name;
            }
        }

        /// <summary>구역 설정과 이동은 현재 선택한 Agent의 프리셋만 사용한다.</summary>
        public List<ZonePresetOption> GetZonePresets()
        {
            return _zonesFeature.GetZonePresets();
        }

        /// <summary>직접 캡처와 현재 Agent 장면만 표시하고 다른 Agent의 저장 자료는 보존한다.</summary>
        public bool IsZoneSceneForCurrentAgent(FireSmokeScene scene)
        {
            return _zonesFeature.IsZoneSceneForCurrentAgent(scene);
        }

        public static string ZonePresetSignature(PresetPointOption p)
        {
            return ZonesFeature.ZonePresetSignature(p);
        }

        public bool ZonePresetCurrent(FireSmokeScene scene)
        {
            return _zonesFeature.ZonePresetCurrent(scene);
        }

        /// <summary>프리셋 번호·장비 종류를 먼저 확인하고 현재 장면의 카메라 기준으로만 변경을 판정한다.</summary>
        public string ZonePresetMismatchReason(FireSmokeScene scene)
        {
            return _zonesFeature.ZonePresetMismatchReason(scene);
        }

        public void MoveZonePreset(ZonePresetOption p)
        {
            _zonesFeature.MoveZonePreset(p);
        }

        public string ZoneCaptureState(string camera)
        {
            return _zonesFeature.ZoneCaptureState(camera);
        }

        public string ZonePresetArrivalReason(string camera, ZonePresetOption p)
        {
            return _zonesFeature.ZonePresetArrivalReason(camera, p);
        }

        public void LinkZonePreset(FireSmokeScene scene, ZonePresetOption p)
        {
            _zonesFeature.LinkZonePreset(scene, p);
        }

        private void InitializeZoneFeatures()
        {
            _zonesFeature.InitializeZoneFeatures();
        }

        public ZoneFrameContext CaptureZoneFrame(string camera)
        {
            return _zonesFeature.CaptureZoneFrame(camera);
        }

        public FireSmokeZoneFile GetZoneEditorData()
        {
            return _zonesFeature.GetZoneEditorData();
        }

        public FireSmokeScene CaptureZoneScene(string camera, out BitmapSource image)
        {
            return _zonesFeature.CaptureZoneScene(camera, out image);
        }

        // EO 표시 프레임은 의도적으로 축소하며 관심 구역은 정규화 좌표를 사용한다.
        // 기준 이미지는 작아도 원본 전체 프레임 비율을 유지한다.
        public static bool IsZoneReferenceSizeCompatible(int sourceWidth, int sourceHeight, int displayWidth, int displayHeight)
        {
            return ZonesFeature.IsZoneReferenceSizeCompatible(sourceWidth, sourceHeight, displayWidth, displayHeight);
        }

        public void SaveZoneEditorData(FireSmokeZoneFile data, int? expectedCalibrationGeneration = null)
        {
            _zonesFeature.SaveZoneEditorData(data, expectedCalibrationGeneration);
        }

    }
}
