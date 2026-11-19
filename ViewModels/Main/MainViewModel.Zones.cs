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

        /// <summary>
        /// 현재 렌즈가 저장한 구역 참조 시야에 맞는지 표시를 갱신한다.
        /// </summary>
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

            /// <summary>
            /// 현재 항목의 이름과 주요 값을 화면 표시용 문자열로 반환한다.
            /// </summary>
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

        /// <summary>
        /// 구역에 연결된 프리셋의 좌표·렌즈 정보를 비교 문자열로 만든다.
        /// </summary>
        public static string ZonePresetSignature(PresetPointOption p)
        {
            return ZonesFeature.ZonePresetSignature(p);
        }

        /// <summary>
        /// 현재 장비 위치가 구역 연결 프리셋과 일치하는지 확인한다.
        /// </summary>
        public bool ZonePresetCurrent(FireSmokeScene scene)
        {
            return _zonesFeature.ZonePresetCurrent(scene);
        }

        /// <summary>프리셋 번호·장비 종류를 먼저 확인하고 현재 장면의 카메라 기준으로만 변경을 판정한다.</summary>
        public string ZonePresetMismatchReason(FireSmokeScene scene)
        {
            return _zonesFeature.ZonePresetMismatchReason(scene);
        }

        /// <summary>
        /// 구역에 연결된 프리셋 위치로 이동을 요청한다.
        /// </summary>
        public void MoveZonePreset(ZonePresetOption p)
        {
            _zonesFeature.MoveZonePreset(p);
        }

        /// <summary>
        /// 구역 참조 촬영의 현재 준비·진행 상태를 표시한다.
        /// </summary>
        public string ZoneCaptureState(string camera)
        {
            return _zonesFeature.ZoneCaptureState(camera);
        }

        /// <summary>
        /// 구역 프리셋에 도달했는지 확인하지 못한 이유를 반환한다.
        /// </summary>
        public string ZonePresetArrivalReason(string camera, ZonePresetOption p)
        {
            return _zonesFeature.ZonePresetArrivalReason(camera, p);
        }

        /// <summary>
        /// 구역 참조와 선택한 프리셋의 연결 정보를 저장한다.
        /// </summary>
        public void LinkZonePreset(FireSmokeScene scene, ZonePresetOption p)
        {
            _zonesFeature.LinkZonePreset(scene, p);
        }

        /// <summary>
        /// 구역 저장소와 화면 갱신 이벤트를 초기화한다.
        /// </summary>
        private void InitializeZoneFeatures()
        {
            _zonesFeature.InitializeZoneFeatures();
        }

        /// <summary>
        /// 구역 참조에 사용할 채널 영상 프레임을 안전하게 복사한다.
        /// </summary>
        public ZoneFrameContext CaptureZoneFrame(string camera)
        {
            return _zonesFeature.CaptureZoneFrame(camera);
        }

        /// <summary>
        /// 구역 편집 창에 전달할 장면·도형·위치 데이터를 구성한다.
        /// </summary>
        public FireSmokeZoneFile GetZoneEditorData()
        {
            return _zonesFeature.GetZoneEditorData();
        }

        /// <summary>
        /// 현재 영상과 위치를 구역 장면 참조로 촬영한다.
        /// </summary>
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

        /// <summary>
        /// 편집한 장면과 구역 도형을 저장하고 관련 표시를 갱신한다.
        /// </summary>
        public void SaveZoneEditorData(FireSmokeZoneFile data, int? expectedCalibrationGeneration = null)
        {
            _zonesFeature.SaveZoneEditorData(data, expectedCalibrationGeneration);
        }

    }
}
