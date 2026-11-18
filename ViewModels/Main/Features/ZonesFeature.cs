using OpenCvWpfTracking.Models.Position;
using OpenCvWpfTracking.Services.Position;
using OpenCvWpfTracking.Common;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using OpenCvWpfTracking.Models.Main;
using OpenCvWpfTracking.Services.Video;
using static OpenCvWpfTracking.ViewModels.Main.MainViewModel;

namespace OpenCvWpfTracking.ViewModels.Main
{
    /// <summary>화재·연기 구역에 필요한 협력 기능만 받는 경계입니다. 화면 객체의 내부 구현을 알지 않습니다.</summary>
    internal interface IZonesFeatureContext
    {
        RtspSourceOption _connectedEoCtecSource { get; set; }

        PositionSnapshotService _positionSnapshotService { get; }

        ObservableCollection<FireEventRecord> AiDetectionEvents { get; }

        string AiRtsp0Address { get; set; }

        string AiRtsp1Address { get; set; }

        string ControlAgentIp { get; set; }

        string ControlAgentPortText { get; set; }

        int ConvertIrZoomStandardToStatusPosition(int standardPosition);

        BitmapSource EOCameraImage { get; set; }

        string EoConnectionStatusText { get; }

        string EoSourceAddress { get; set; }

        int EoVideoHeight { get; set; }

        int EoVideoWidth { get; set; }

        ObservableCollection<FireEventRecord> FireEvents { get; }

        ushort GetCurrentPresetStandardZoom();

        RtspChannelPerformance GetRtspPerformance(string streamName);

        BitmapSource IRCameraImage { get; set; }

        string IrConnectionStatusText { get; }

        string IrSourceAddress { get; set; }

        int IrVideoHeight { get; set; }

        int IrVideoWidth { get; set; }

        bool IsOperationCommandEnabled { get; }

        bool IsPresetScanControlLocked { get; }

        bool IsSelectedControlAgentLaAgent { get; }

        bool IsSelectedControlAgentWebAgent { get; }

        ObservableCollection<PresetPointOption> LaPresetPoints { get; }

        System.Windows.Input.ICommand MoveToLaPresetCommand { get; }

        System.Windows.Input.ICommand MoveToPresetCommand { get; }

        void OnPropertyChanged([global::System.Runtime.CompilerServices.CallerMemberName] string propertyName = null);

        PanoramaReferenceState PanoramaReference { get; }

        int PresetIrZoomPosition(PresetPointOption preset);

        ObservableCollection<PresetPointOption> PresetPoints { get; }

        System.Threading.Tasks.Task<int?> RequestAndWaitCtecEoPositionAsync(ContinuousMoveType moveType, RtspSourceOption sourceOption, System.Threading.CancellationToken cancellationToken);

        ControlAgentProfileOption SelectedControlAgentProfile { get; set; }

        PresetPointOption SelectedLaPresetPoint { get; set; }

        PresetPointOption SelectedPresetPoint { get; set; }

        Func<FireSmokeScene, bool> IsZonePresetCurrentForDisplayCallback { get; }

        Func<FireSmokeScene, bool> IsZonePresetCurrentForDetectionCallback { get; }
    }

    /// <summary>화재·연기 구역 담당 객체입니다. 상태를 보관하고 입력을 받아 결과를 알립니다.</summary>
    internal sealed class ZonesFeature
    {

        #region [협력 객체와 초기화]

        private readonly IZonesFeatureContext _context;

        internal ZonesFeature(IZonesFeatureContext context)
        {
            _context = context ?? throw new System.ArgumentNullException(nameof(context));
        }

        internal void InitializeState()
        {
            EoZoneOverlay = new ObservableCollection<ZoneOverlayBox>();

            IrZoneOverlay = new ObservableCollection<ZoneOverlayBox>();

            _showZoneOverlay = true;

            _zoneFile = new FireSmokeZoneFile();

            _zoneStatus = "구역 미등록";

            _eoZoneImageCount = -1;

            _irZoneImageCount = -1;
        }

        #endregion

        #region [상태 필드]

        internal bool _showZoneOverlay;

        internal string _eoZoneSignature;

        internal string _irZoneSignature;

        internal readonly ZoneOverlayDisplayCache _eoZoneDisplayCache = new ZoneOverlayDisplayCache();

        internal readonly ZoneOverlayDisplayCache _irZoneDisplayCache = new ZoneOverlayDisplayCache();

        internal string _eoZoneDisplayState;

        internal string _irZoneDisplayState;

        internal FireSmokeZoneFile _zoneFile;

        internal readonly object _zoneHistoryLock = new object();

        internal readonly List<ZoneFrameContext> _zoneHistory = new List<ZoneFrameContext>();

        internal DispatcherTimer _zoneTimer;

        internal int _zoneEpoch;

        internal int _eoZoneChannelEpoch;

        internal int _irZoneChannelEpoch;

        internal int _zoneCalibrationGeneration;

        internal bool _zoneZeroPending;

        internal string _zoneStatus;

        internal string _zoneLoadError;

        internal DateTimeOffset _zoneCtecZoomAt;

        internal int _zoneCtecZoomEpoch;

        internal int _zoneLensQueryBusy;

        internal string _zoneCtecZoomKey;

        internal DateTimeOffset _zoneLensQueryErrorAt;

        internal int _zoneImageBusy;

        internal readonly object _zoneImageLock = new object();

        internal readonly Dictionary<string, ZoneFrameContext> _zoneImageFrames = new Dictionary<string, ZoneFrameContext>();

        internal DateTimeOffset _zoneImageErrorAt;

        internal int _eoZoneImageCount;

        internal int _irZoneImageCount;

        #endregion

        #region [화면 속성과 이벤트]

        internal ObservableCollection<ZoneOverlayBox> EoZoneOverlay { get; set; }

        internal ObservableCollection<ZoneOverlayBox> IrZoneOverlay { get; set; }

        internal bool ShowZoneOverlay
        {
            get { return _showZoneOverlay; }

            set
            {
                _showZoneOverlay = value;

                _eoZoneSignature = _irZoneSignature = null;

                _context.OnPropertyChanged();
            }
        }

        internal int ZoneCalibrationGeneration
        {
            get { return _zoneCalibrationGeneration; }
        }

        internal bool IsZoneReferencePending
        {
            get
            {
                return _zoneZeroPending || _context.PanoramaReference.Pending || _context.PanoramaReference.ZoneRebase != null;
            }
        }

        internal event Action<PanoramaZoneRebase> ZoneReferenceChanged;

        internal static string ZonePath
        {
            get
            {
                return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config", "FireSmokeZones.xml");
            }
        }

        internal string ZoneStatusText
        {
            get
            {
                return "구역 상태\n" + _zoneStatus;
            }
        }

        internal string ZoneEquipmentKey
        {
            get
            {
                return ZoneHash(OpenCvWpfTracking.Services.Configuration.DeviceCatalog.StorageDisplayName(_context.SelectedControlAgentProfile.DisplayName) + "|" + _context.ControlAgentIp + "|" + _context.ControlAgentPortText);
            }
        }

        #endregion

        #region [동작 처리]

        internal async void RefreshZoneImageRegistration()
        {
            if (System.Threading.Interlocked.CompareExchange(ref _zoneImageBusy, 1, 0) != 0)
            {
                return;
            }

            try
            {
                foreach (string camera in new[]
                {
                    "EO",
                    "IR"
                }

                )
                {
                    var frame = CaptureZoneFrame(camera);

                    var source = camera == "IR" ? _context.IRCameraImage : _context.EOCameraImage;

                    // 가까운 저장 장면부터 처리하고 장면이 많아도 작업량을 제한한다.
                    var scenes = _zoneFile.Scenes.Where(s => s.Camera == camera && s.Key == frame.Key && !s.Invalidated && s.FixedInstallationConfirmed && ZonePresetCurrent(s)).OrderBy(s => FireSmokeZoneService.PanDistance(s.Pan, frame.Position?.Pan ?? s.Pan) + Math.Abs(s.Tilt - (frame.Position?.Tilt ?? s.Tilt))).Take(8).ToArray();

                    if (source == null || scenes.Length == 0 || ZoneFrameReadiness(frame, true) != null)
                    {
                        continue;
                    }

                    var image = source.CloneCurrentValue();

                    image.Freeze();

                    var registered = await System.Threading.Tasks.Task.Run(() =>
                    {
                        var matches = new Dictionary<string, FireSmokeScene>();

                        byte[] pixels = ZoneImageRegistration.Encode(image);

                        foreach (var scene in scenes)
                        {
                            var aligned = ZoneImageRegistration.Match(scene, pixels);

                            if (aligned != null)
                            {
                                aligned.Pan = frame.Position.Pan.Value;
                                aligned.Tilt = frame.Position.Tilt.Value;
                                aligned.Zoom = (camera == "IR" ? frame.Position.IrZoom : frame.Position.EoZoom).Value;

                                matches[scene.Id + scene.Revision] = aligned;
                            }
                        }

                        return matches;
                    });

                    // 완료 시간이 아닌 취득 시간을 기록해 이동·배율 변경 전 결과가 남지 않게 한다.
                    frame.RegisteredScenes = registered;

                    lock (_zoneImageLock)
                    {
                        _zoneImageFrames[camera] = frame;
                    }

                    int previous = camera == "IR" ? _irZoneImageCount : _eoZoneImageCount;

                    if (previous != registered.Count)
                    {
                        if (camera == "IR")
                        {
                            _irZoneImageCount = registered.Count;
                        }
                        else
                        {
                            _eoZoneImageCount = registered.Count;
                        }

                        ConsoleLogHelper.State("ZONE IMAGE MATCH", camera + " / MATCHED=" + registered.Count + " / CHECKED=" + scenes.Length);
                    }

                    if (!ShowZoneOverlay)
                    {
                        continue;
                    }

                    RefreshZoneOverlay(CaptureZoneFrame(camera), camera);
                }
            }
            catch (Exception ex)
            {
                lock (_zoneImageLock)
                {
                    _zoneImageFrames.Clear();
                }

                if ((DateTimeOffset.Now - _zoneImageErrorAt).TotalSeconds >= 10)
                {
                    _zoneImageErrorAt = DateTimeOffset.Now;

                    ConsoleLogHelper.Error("ZONE IMAGE MATCH", "영상 대조 실패 / 구역 적용 보류", ex);
                }
            }
            finally
            {
                System.Threading.Interlocked.Exchange(ref _zoneImageBusy, 0);
            }
        }

        internal PositionSnapshot CaptureZonePosition()
        {
            var p = _context._positionSnapshotService.Capture("ZONE_FRAME");

            if (_context._connectedEoCtecSource == null)
            {
                return p;
            }

            double age = (DateTimeOffset.Now - _zoneCtecZoomAt).TotalSeconds;

            bool fresh = _zoneCtecZoomEpoch == _zoneEpoch && _zoneCtecZoomKey == ZoneKey("EO") && _zoneCtecZoomAt != default(DateTimeOffset) && age >= 0 && age <= 3;

            int? zoom = fresh ? (int?)_context.GetCurrentPresetStandardZoom() : null;

            return new PositionSnapshot(p.CapturedAt, p.Source, p.PresetId, p.Pan, p.Tilt, zoom, p.EoFocus, p.IrZoom, p.IrFocus, p.Latitude, p.Longitude, p.Altitude, p.GpsSpeed, p.GpsCourse, p.GpsHdop, p.GpsSatelliteCount, p.GpsFixStatus, p.Roll, p.Pitch, p.Yaw, p.PtzStatus, fresh ? PositionDataStatus.Valid : PositionDataStatus.Stale, p.IrLensStatus, p.GpsStatus, p.ImuStatus, p.WebAgentConnected);
        }

        internal async void RefreshZoneLensState()
        {
            var source = _context._connectedEoCtecSource;

            if (source == null || System.Threading.Interlocked.CompareExchange(ref _zoneLensQueryBusy, 1, 0) != 0)
            {
                return;
            }

            try
            {
                await _context.RequestAndWaitCtecEoPositionAsync(ContinuousMoveType.EoZoom, source, System.Threading.CancellationToken.None);
            }
            catch (Exception ex)
            {
                if ((DateTimeOffset.Now - _zoneLensQueryErrorAt).TotalSeconds >= 5)
                {
                    _zoneLensQueryErrorAt = DateTimeOffset.Now;

                    ConsoleLogHelper.Error("ZONE LENS QUERY", "렌즈 상태 조회 실패 / 자동 캡처 보류", ex);
                }
            }
            finally
            {
                System.Threading.Interlocked.Exchange(ref _zoneLensQueryBusy, 0);
            }
        }

        internal static string ZoneHash(string text)
        {
            using (var sha = SHA256.Create())
            {
                return BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(text ?? ""))).Replace("-", "");
            }
        }

        internal string ZoneKey(string camera)
        {
            return ZoneHash(ZoneEquipmentKey + "|" + camera + "|" + (camera == "IR" ? _context.IrSourceAddress : _context.EoSourceAddress));
        }

        internal List<ZonePresetOption> GetZonePresets()
        {
            if (_context.IsSelectedControlAgentLaAgent)
            {
                return (_context.LaPresetPoints ?? new ObservableCollection<PresetPointOption>()).Select(p => new ZonePresetOption { Kind = "LA", Point = p }).ToList();
            }

            if (_context.IsSelectedControlAgentWebAgent)
            {
                return (_context.PresetPoints ?? new ObservableCollection<PresetPointOption>()).Select(p => new ZonePresetOption { Kind = "WEB", Point = p }).ToList();
            }

            return new List<ZonePresetOption>();
        }

        internal bool IsZoneSceneForCurrentAgent(FireSmokeScene scene)
        {
            if (scene == null)
            {
                return false;
            }

            return scene.PresetNumber == 0 || (_context.IsSelectedControlAgentLaAgent && scene.PresetKind == "LA") || (_context.IsSelectedControlAgentWebAgent && scene.PresetKind == "WEB");
        }

        internal bool TryGetZonePresetZoomPosition(string camera, PresetPointOption preset, out int zoom)
        {
            if (camera == "IR")
            {
                // LA 표준 줌은 반전하고 WEB은 기존 위치·구형 호환 변환 결과를 그대로 사용한다.
                zoom = _context.ConvertIrZoomStandardToStatusPosition(_context.PresetIrZoomPosition(preset));

                return true;
            }

            return int.TryParse(preset.EoZoomText, out zoom);
        }

        internal static string ZonePresetSignature(PresetPointOption p)
        {
            if (p == null)
            {
                return "";
            }
            else
            {
                return string.Join("|", p.Number, p.Pan.ToString("R", System.Globalization.CultureInfo.InvariantCulture), p.Tilt.ToString("R", System.Globalization.CultureInfo.InvariantCulture), p.EoZoomText, p.EoFocusText, p.IrZoomText, p.IrFocusText, p.PositionSnapshot?.CapturedAt.ToString("O") ?? "LEGACY");
            }
        }

        internal bool ZonePresetCurrent(FireSmokeScene scene)
        {
            return ZonePresetMismatchReason(scene) == null;
        }

        internal string ZonePresetMismatchReason(FireSmokeScene scene)
        {
            if (scene == null)
            {
                return "장면 없음";
            }

            if (scene.PresetNumber == 0)
            {
                return null;
            }

            var current = GetZonePresets().FirstOrDefault(p => p.Kind == scene.PresetKind && p.Point.Number == scene.PresetNumber);

            if (current == null)
            {
                return "연결 프리셋 없음";
            }
            else
            {
                return FireSmokeZoneService.PresetMismatch(scene, current.Point);
            }
        }

        internal void MoveZonePreset(ZonePresetOption p)
        {
            if (p == null || !GetZonePresets().Any(v => v.Kind == p.Kind && ReferenceEquals(v.Point, p.Point)))
            {
                throw new InvalidOperationException("프리셋을 다시 선택하세요.");
            }

            var command = p.Kind == "LA" ? _context.MoveToLaPresetCommand : _context.MoveToPresetCommand;

            if (p.Kind == "LA")
            {
                _context.SelectedLaPresetPoint = p.Point;
            }
            else
            {
                _context.SelectedPresetPoint = p.Point;
            }

            if (command == null || !command.CanExecute(null) || !_context.IsOperationCommandEnabled || _context.IsPresetScanControlLocked)
            {
                throw new InvalidOperationException("현재 이동할 수 없습니다. 연결·작업 상태를 확인하세요.");
            }

            command.Execute(null);
        }

        internal string ZoneCaptureState(string camera)
        {
            return ZoneFrameInvalidReason(CaptureZoneFrame(camera));
        }

        internal string ZonePresetArrivalReason(string camera, ZonePresetOption p)
        {
            if (p == null)
            {
                return null;
            }

            if (!GetZonePresets().Any(v => v.Kind == p.Kind && ReferenceEquals(v.Point, p.Point)))
            {
                return "프리셋 변경 / 다시 선택";
            }

            var frame = CaptureZoneFrame(camera);

            int zoom;

            if (!FireSmokeZoneService.ValidPose(frame.Position, camera) || !TryGetZonePresetZoomPosition(camera, p.Point, out zoom))
            {
                return "프리셋 위치·ZOOM 미확인";
            }

            // 구역 스냅샷은 원시 수신 좌표이다. LA 프리셋의 반전된 줌을 같은 기준으로 비교한다.
            int? currentZoom = camera == "IR" ? frame.Position.IrZoom : frame.Position.EoZoom;

            if (FireSmokeZoneService.PanDistance(frame.Position.Pan.Value, p.Point.Pan) <= .3 && Math.Abs(frame.Position.Tilt.Value - p.Point.Tilt) <= .3 && FireSmokeZoneService.LensWithin(currentZoom, zoom, FireSmokeZoneService.ZoomTolerance))
            {
                return null;
            }

            return string.Format(System.Globalization.CultureInfo.InvariantCulture, "도착 대기 (줌: 장비 수신 기준)\n목표 P/T/Z: {0:F2} / {1:F2} / {2}\n수신 P/T/Z: {3:F2} / {4:F2} / {5}", p.Point.Pan, p.Point.Tilt, zoom, frame.Position.Pan.Value, frame.Position.Tilt.Value, currentZoom);
        }

        internal void LinkZonePreset(FireSmokeScene scene, ZonePresetOption p)
        {
            if (p == null)
            {
                return;
            }

            if (!GetZonePresets().Any(v => v.Kind == p.Kind && ReferenceEquals(v.Point, p.Point)))
            {
                throw new InvalidOperationException("프리셋 변경됨 / 다시 선택하세요.");
            }

            int zoom;

            if (!TryGetZonePresetZoomPosition(scene.Camera, p.Point, out zoom) || FireSmokeZoneService.PanDistance(scene.Pan, p.Point.Pan) > scene.AngleTolerance || Math.Abs(scene.Tilt - p.Point.Tilt) > scene.AngleTolerance || !FireSmokeZoneService.LensWithin(scene.Zoom, zoom, FireSmokeZoneService.ZoomTolerance))
            {
                throw new InvalidOperationException("선택 프리셋에 도착하지 않았습니다. 이동 후 캡처하세요.");
            }

            scene.PresetKind = p.Kind;
            scene.PresetNumber = p.Point.Number;

            scene.PresetSignature = ZonePresetSignature(p.Point);
        }

        internal void InitializeZoneFeatures()
        {
            try
            {
                _zoneFile = FireSmokeZoneService.Load(ZonePath);
            }
            catch (Exception ex)
            {
                _zoneLoadError = ex.Message;

                ConsoleLogHelper.Error("ZONE LOAD", "설정 읽기 실패 / 원본 파일 유지", ex);
            }

            _zoneTimer = new DispatcherTimer(DispatcherPriority.Background)
            {
                Interval = TimeSpan.FromMilliseconds(500)
            };

            _zoneTimer.Tick += (s, e) =>
            {
                RefreshZoneImageRegistration();

                if (ShowZoneOverlay && _zoneFile.Scenes.Any(scene => scene.Camera == "EO"))
                {
                    RefreshZoneLensState();
                }

                var eo = CaptureZoneFrame("EO");

                var ir = CaptureZoneFrame("IR");

                lock (_zoneHistoryLock)
                {
                    _zoneHistory.Add(eo);

                    _zoneHistory.Add(ir);

                    _zoneHistory.RemoveAll(f => (DateTimeOffset.Now - f.At).TotalSeconds > 8);
                }

                _zoneStatus = _zoneLoadError != null ? "설정 읽기 실패 / 확인 필요" : "EO : " + ZoneViewState(eo) + "\nIR : " + ZoneViewState(ir);

                _context.OnPropertyChanged(nameof(ZoneStatusText));

                RefreshZoneOverlay(eo, "EO");

                RefreshZoneOverlay(ir, "IR");

                // 이동·연결 해제 후 활성 이벤트를 현재 유효한 것처럼 표시하지 않는다.
                foreach (var record in _context.AiDetectionEvents.Concat(_context.FireEvents).Where(r => r.Status == "ACTIVE" && r.FirstZoneAssessment != null))
                {
                    var frame = record.Camera == "IR" ? ir : eo;

                    string invalid = ZoneFrameInvalidReason(frame);

                    if (invalid != null || !_zoneFile.Scenes.Any(v => ZonePresetCurrent(v) && FireSmokeZoneService.ForFrame(v, frame) != null))
                    {
                        record.UpdateZoneAssessment(ZoneAssessment.Unknown(invalid ?? "등록 시점 불일치 / 다음 검출 프레임 확인 필요"));
                    }
                }
            };

            _zoneTimer.Start();

            System.Windows.Application.Current.Exit += (s, e) => _zoneTimer.Stop();
        }

        internal string ZoneViewState(ZoneFrameContext f)
        {
            if (!_zoneFile.Scenes.Any(s => s.Key == f.Key))
            {
                return "미등록";
            }

            if (ZoneFrameReadiness(f, false) != null)
            {
                return "미확인";
            }

            var views = _zoneFile.Scenes.Where(_context.IsZonePresetCurrentForDisplayCallback).Select(s => FireSmokeZoneService.ForFrame(s, f)).Where(s => s != null).ToList();

            if (views.Any(s => s.Zones.Count > 0))
            {
                return "영상 대조 일치";
            }
            else
            {
                if (views.Count > 0)
                {
                    return "구역 화면 밖";
                }
                else
                {
                    return "구역 위치 미확인";
                }
            }
        }

        internal void RefreshZoneOverlay(ZoneFrameContext frame, string camera)
        {
            bool ready = ShowZoneOverlay && ZoneFrameReadiness(frame, false) == null && _context.IsOperationCommandEnabled && !_context.IsPresetScanControlLocked;

            var eligible = _zoneFile.Scenes.Where(scene => scene.Camera == camera && scene.Key == frame.Key && !scene.Invalidated && scene.FixedInstallationConfirmed && ZonePresetCurrent(scene)).ToList();

            var scenes = ready ? eligible.Select(s => FireSmokeZoneService.ForFrame(s, frame)).Where(s => s != null).ToList() : new List<FireSmokeScene>();

            DateTimeOffset confirmedAt = default(DateTimeOffset);

            lock (_zoneImageLock)
            {
                ZoneFrameContext matched;

                if (frame.RegisteredScenes != null && _zoneImageFrames.TryGetValue(camera, out matched) && ReferenceEquals(frame.RegisteredScenes, matched.RegisteredScenes))
                {
                    confirmedAt = matched.At;
                }
            }

            int held;

            var cache = camera == "IR" ? _irZoneDisplayCache : _eoZoneDisplayCache;

            scenes = cache.Update(camera, frame, scenes, eligible.Select(scene => scene.Id + scene.Revision), confirmedAt, ready, out held);

            string state = !ready ? "표시 해제 / 연결·상태·작업 조건 확인" : held > 0 ? "정합 재확인 / 마지막 유효 구역 임시 표시 / 탐지 판정 별도" : scenes.Count > 0 ? "최신 정합 구역 표시" : "영상 대조 미확인 / 표시 구역 없음";

            if (state != (camera == "IR" ? _irZoneDisplayState : _eoZoneDisplayState))
            {
                if (camera == "IR")
                {
                    _irZoneDisplayState = state;
                }
                else
                {
                    _eoZoneDisplayState = state;
                }

                ConsoleLogHelper.State("ZONE OVERLAY", camera + " / " + state + " / COUNT=" + scenes.Count);
            }

            // 수신 각도의 미세 변화만으로 컬렉션 전체를 지우지 않는다.
            string signature = frame.Width + ":" + frame.Height + ":" + string.Join("|", scenes.Select(s => s.Id + s.Revision + string.Join(";", s.Zones.Select(z => z.X.ToString("R") + "," + z.Y.ToString("R") + "," + z.Width.ToString("R") + "," + z.Height.ToString("R")))));

            if (signature == (camera == "IR" ? _irZoneSignature : _eoZoneSignature))
            {
                return;
            }

            if (camera == "IR")
            {
                _irZoneSignature = signature;
            }
            else
            {
                _eoZoneSignature = signature;
            }

            var target = camera == "IR" ? IrZoneOverlay : EoZoneOverlay;

            var boxes = new List<ZoneOverlayBox>();

            foreach (var scene in scenes)
            {
                foreach (var zone in scene.Zones)
                {
                    boxes.Add(new ZoneOverlayBox { Left = zone.X * frame.Width, Top = zone.Y * frame.Height, Width = zone.Width * frame.Width, Height = zone.Height * frame.Height, Polygon = zone.ProjectedPolygon?.Select(p => new System.Windows.Point(p.X * frame.Width, p.Y * frame.Height)).ToList(), Name = "ZONE " + zone.ToString(), Color = zone.DisplayColor });
                }
            }

            // 기존 항목은 한 번에 빈 목록으로 만들지 않고 필요한 항목만 교체한다.
            for (int i = 0; i < boxes.Count; i++)
            {
                if (i < target.Count)
                {
                    target[i] = boxes[i];
                }
                else
                {
                    target.Add(boxes[i]);
                }
            }

            while (target.Count > boxes.Count)
            {
                target.RemoveAt(target.Count - 1);
            }
        }

        internal ZoneFrameContext CaptureZoneFrame(string camera)
        {
            var position = CaptureZonePosition();

            string model = _context.SelectedControlAgentProfile?.DisplayName ?? "";

            bool supported = camera == "EO" && (model.IndexOf("MR300", StringComparison.OrdinalIgnoreCase) >= 0 || model.IndexOf("MR500", StringComparison.OrdinalIgnoreCase) >= 0) && position.EoZoom.HasValue && position.EoZoom.Value >= 0 && position.EoZoom.Value <= 1000;

            var context = new ZoneFrameContext
            {
                RequireImageRegistration = true,
                At = DateTimeOffset.Now,
                Key = ZoneKey(camera),
                Epoch = System.Threading.Volatile.Read(ref _zoneEpoch),
                ChannelEpoch = camera == "IR" ? System.Threading.Volatile.Read(ref _irZoneChannelEpoch) : System.Threading.Volatile.Read(ref _eoZoneChannelEpoch),
                Position = position,
                HorizontalFov = supported ? new OpenCvWpfTracking.Services.Control.FieldOfViewSyncService().GetEoHfov((short)position.EoZoom.Value) : 0,
                Width = camera == "IR" ? _context.IrVideoWidth : _context.EoVideoWidth,
                Height = camera == "IR" ? _context.IrVideoHeight : _context.EoVideoHeight
            };

            lock (_zoneImageLock)
            {
                ZoneFrameContext aligned;

                if (_zoneImageFrames.TryGetValue(camera, out aligned) && aligned.Key == context.Key && aligned.Epoch == context.Epoch && aligned.ChannelEpoch == context.ChannelEpoch && (context.At - aligned.At).TotalSeconds <= 1.5 && FireSmokeZoneService.ValidPose(position, camera) && FireSmokeZoneService.ValidPose(aligned.Position, camera) && FireSmokeZoneService.PanDistance(position.Pan.Value, aligned.Position.Pan.Value) < .03 && Math.Abs(position.Tilt.Value - aligned.Position.Tilt.Value) < .03 && (camera == "IR" ? FireSmokeZoneService.LensWithin(position.IrZoom, aligned.Position.IrZoom.Value, FireSmokeZoneService.ZoomTolerance) : FireSmokeZoneService.LensWithin(position.EoZoom, aligned.Position.EoZoom.Value, FireSmokeZoneService.ZoomTolerance)))
                {
                    context.RegisteredScenes = aligned.RegisteredScenes;
                }
            }

            return context;
        }

        internal string ZoneFrameInvalidReason(ZoneFrameContext f)
        {
            return ZoneFrameReadiness(f, true);
        }

        internal string ZoneFrameReadiness(ZoneFrameContext f, bool requireStable)
        {
            if (IsZoneReferencePending)
            {
                return "ZERO 좌표 기준 확인 대기 / 구역 보존";
            }

            if (f == null || f.Epoch != _zoneEpoch || (DateTimeOffset.Now - f.At).TotalSeconds > 2 || f.At > DateTimeOffset.Now.AddMilliseconds(100))
            {
                return "프레임 시간/세션 불일치";
            }

            string camera = f.Key == ZoneKey("EO") ? "EO" : f.Key == ZoneKey("IR") ? "IR" : null;

            if (camera == null)
            {
                return "장비/영상 소스 변경";
            }

            if (f.ChannelEpoch != (camera == "IR" ? _irZoneChannelEpoch : _eoZoneChannelEpoch))
            {
                return "영상 재연결 전 프레임";
            }

            if (requireStable && (!_context.IsOperationCommandEnabled || _context.IsPresetScanControlLocked))
            {
                return "카메라 작업 중";
            }

            if ((camera == "IR" ? _context.IrConnectionStatusText : _context.EoConnectionStatusText) != "Connected")
            {
                return "영상 미연결";
            }

            var perf = _context.GetRtspPerformance(camera);

            lock (perf.Sync)
            {
                if (perf.LastFrame.ElapsedMilliseconds > 1000)
                {
                    return "영상 프레임 지연";
                }
            }

            if (!FireSmokeZoneService.ValidPose(f.Position, camera))
            {
                return "PTZ/Zoom 상태 미수신·지연";
            }

            // 이동 중 표시에는 현재 위치를 쓰되 캡처·판정은 안정된 프레임 대응이 필요하다.
            if (!requireStable)
            {
                return null;
            }

            var current = CaptureZonePosition();

            if (!FireSmokeZoneService.IsCameraPoseStable(f.Position, current, camera))
            {
                return "검출 이후 카메라 위치/Zoom 변경";
            }

            List<ZoneFrameContext> history;

            lock (_zoneHistoryLock)
            {
                history = _zoneHistory.Where(h => h.Key == f.Key && h.Epoch == f.Epoch && h.ChannelEpoch == f.ChannelEpoch && h.At <= f.At && (f.At - h.At).TotalSeconds <= 2.5).ToList();
            }

            if (history.Count < 4 || (f.At - history.First().At).TotalSeconds < 1.5 || (f.At - history.Last().At).TotalSeconds > 1.25)
            {
                return "시점 안정화 대기 (약 2초)";
            }

            // 최근 약 2초 동안 해당 채널의 줌 ±5와 기존 PT 기준을 함께 확인한다.
            if (history.Any(h => !FireSmokeZoneService.IsCameraPoseStable(f.Position, h.Position, camera)))
            {
                return "카메라 이동/Zoom 변경 / 안정화 필요";
            }

            return null;
        }

        internal FireSmokeZoneFile GetZoneEditorData()
        {
            if (_zoneLoadError != null)
            {
                throw new InvalidDataException("기존 구역 파일을 덮어쓰지 않습니다. 설정을 확인하세요: " + _zoneLoadError);
            }

            return FireSmokeZoneService.Clone(_zoneFile);
        }

        internal FireSmokeScene CaptureZoneScene(string camera, out BitmapSource image)
        {
            var frame = CaptureZoneFrame(camera);

            string reason = ZoneFrameInvalidReason(frame);

            if (reason != null)
            {
                throw new InvalidOperationException(reason);
            }

            // 이 이미지는 디코더·표시 픽셀만 포함하며 경고·검출 오버레이는 별도이다.
            // 별도 WPF 오버레이를 섞지 않도록 화면 트리 전체를 캡처하지 않는다.
            image = (camera == "IR" ? _context.IRCameraImage : _context.EOCameraImage)?.CloneCurrentValue();

            if (image == null)
            {
                throw new InvalidOperationException("표시 영상 없음");
            }

            image.Freeze();

            if (!IsZoneReferenceSizeCompatible(frame.Width, frame.Height, image.PixelWidth, image.PixelHeight))
            {
                throw new InvalidOperationException("영상 크기 변경 중입니다. 잠시 후 다시 캡처하세요.");
            }

            byte[] png;

            using (var stream = new MemoryStream())
            {
                var encoder = new PngBitmapEncoder();

                encoder.Frames.Add(BitmapFrame.Create(image));

                encoder.Save(stream);

                png = stream.ToArray();
            }

            return new FireSmokeScene
            {
                Name = _context.SelectedControlAgentProfile.DisplayName + " / " + camera,
                Camera = camera,
                Key = frame.Key,
                EquipmentKey = ZoneEquipmentKey,
                RegisteredAt = frame.At.ToString("O"),
                Pan = frame.Position.Pan.Value,
                Tilt = frame.Position.Tilt.Value,
                Zoom = (camera == "IR" ? frame.Position.IrZoom : frame.Position.EoZoom).Value,
                FrameWidth = frame.Width,
                FrameHeight = frame.Height,
                ReferencePng = png,
                HorizontalFov = frame.HorizontalFov
            };
        }

        internal static bool IsZoneReferenceSizeCompatible(int sourceWidth, int sourceHeight, int displayWidth, int displayHeight)
        {
            if (sourceWidth <= 0 || sourceHeight <= 0 || displayWidth <= 0 || displayHeight <= 0)
            {
                return false;
            }

            double expectedHeight = sourceHeight * (displayWidth / (double)sourceWidth);

            return Math.Abs(displayHeight - expectedHeight) <= 1.0;
        }

        internal void SaveZoneEditorData(FireSmokeZoneFile data, int? expectedCalibrationGeneration = null)
        {
            if (expectedCalibrationGeneration.HasValue && expectedCalibrationGeneration != _zoneCalibrationGeneration)
            {
                throw new InvalidOperationException("편집 중 원점이 변경되었습니다. 창을 닫고 다시 등록하세요.");
            }

            var saved = FireSmokeZoneService.Clone(data);

            FireSmokeZoneService.Save(ZonePath, saved);

            _zoneFile = saved;

            ConsoleLogHelper.State("ZONE SAVE", "저장 완료 / SCENES=" + saved.Scenes.Count + " / PATH=" + ZonePath);

            lock (_zoneHistoryLock)
            {
                _zoneHistory.Clear();
            }

            _context.OnPropertyChanged(nameof(ZoneStatusText));
        }

        internal void BeginZoneReferenceChange()
        {
            _zoneCalibrationGeneration++;

            _zoneZeroPending = true;

            ResetZoneReferenceFrames();
        }

        internal void ResetZoneReferenceFrames()
        {
            _eoZoneDisplayCache.Clear();

            _irZoneDisplayCache.Clear();

            System.Threading.Interlocked.Increment(ref _zoneEpoch);

            lock (_zoneHistoryLock)
            {
                _zoneHistory.Clear();
            }

            lock (_zoneImageLock)
            {
                _zoneImageFrames.Clear();
            }

            _eoZoneSignature = null;
            _irZoneSignature = null;

            EoZoneOverlay.Clear();

            IrZoneOverlay.Clear();
        }

        internal void ApplyZoneReferenceRebase(PanoramaZoneRebase operation)
        {
            if (_zoneLoadError != null)
            {
                throw new IOException("구역 원본 확인 필요 / ZERO 변환 저장 보류: " + _zoneLoadError);
            }

            var data = FireSmokeZoneService.Clone(_zoneFile);

            if (FireSmokeZoneService.RebaseZero(data, operation))
            {
                FireSmokeZoneService.Save(ZonePath, data);

                _zoneFile = data;
            }

            ResetZoneReferenceFrames();

            ZoneReferenceChanged?.Invoke(operation);

            ConsoleLogHelper.State("ZONE ZERO REFERENCE", "구역 보존·기준 갱신 / PAN_DELTA=" + operation.PanDelta.ToString("F3") + " / TILT_DELTA=" + operation.TiltDelta.ToString("F3") + " / SCENES=" + data.Scenes.Count(scene => scene.EquipmentKey == operation.EquipmentKey));
        }

        internal void FinishZoneReferenceChange(bool notifyEditor = true)
        {
            _zoneZeroPending = false;

            if (notifyEditor)
            {
                ZoneReferenceChanged?.Invoke(null);
            }
        }

        internal void ZoneChannelChanged(string camera)
        {
            if (camera == "IR")
            {
                System.Threading.Interlocked.Increment(ref _irZoneChannelEpoch);
            }
            else
            {
                System.Threading.Interlocked.Increment(ref _eoZoneChannelEpoch);
            }
        }

        internal ZoneAssessment AssessZone(string camera, string type, double left, double top, double right, double bottom, ZoneFrameContext f)
        {
            string reason = ZoneFrameInvalidReason(f);

            if (reason != null)
            {
                return ZoneAssessment.Unknown(reason);
            }
            else
            {
                return FireSmokeZoneService.Assess(new FireSmokeZoneFile { Scenes = _zoneFile.Scenes.Where(_context.IsZonePresetCurrentForDetectionCallback).ToList() }, f, type, left, top, right, bottom);
            }
        }

        internal ZoneAssessment AssessAiZone(OpenCvWpfTracking.Models.AI.AiDetectionResult result, OpenCvWpfTracking.Models.AI.AiDetectionBox box, string type)
        {
            if ((type ?? "").IndexOf("FIRE", StringComparison.OrdinalIgnoreCase) < 0 && (type ?? "").IndexOf("SMOKE", StringComparison.OrdinalIgnoreCase) < 0)
            {
                return null;
            }

            string camera = result.RtspIndex == 0 ? "EO" : "IR";

            if ((camera == "IR" ? _context.AiRtsp1Address : _context.AiRtsp0Address) != (camera == "IR" ? _context.IrSourceAddress : _context.EoSourceAddress))
            {
                return ZoneAssessment.Unknown("AI와 Viewer 영상 소스 불일치");
            }

            DateTimeOffset at;

            try
            {
                at = DateTimeOffset.FromUnixTimeMilliseconds(result.FrameTime);
            }
            catch (ArgumentOutOfRangeException)
            {
                return ZoneAssessment.Unknown("AI 프레임 시각 해석 불가");
            }

            if (Math.Abs((DateTimeOffset.Now - at).TotalSeconds) > 2)
            {
                return ZoneAssessment.Unknown("AI 프레임 시각 지연/서버 시계 확인 필요");
            }

            ZoneFrameContext f;

            lock (_zoneHistoryLock)
            {
                f = _zoneHistory.Where(h => h.Key == ZoneKey(camera) && h.At <= at && (at - h.At).TotalSeconds <= 0.6).OrderByDescending(h => h.At).FirstOrDefault();
            }

            return AssessZone(camera, type, box.Left, box.Top, box.Right, box.Bottom, f);
        }

        #endregion

    }
}
