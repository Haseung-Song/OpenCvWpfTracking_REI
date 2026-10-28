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

        public ObservableCollection<ZoneOverlayBox> EoZoneOverlay { get; } = new ObservableCollection<ZoneOverlayBox>();

        public ObservableCollection<ZoneOverlayBox> IrZoneOverlay { get; } = new ObservableCollection<ZoneOverlayBox>();

        private bool _showZoneOverlay = true;

        private string _eoZoneSignature;

        private string _irZoneSignature;

        public bool ShowZoneOverlay
        {
            get => _showZoneOverlay;
            set
            {
                _showZoneOverlay = value;
                _eoZoneSignature = _irZoneSignature = null;
                OnPropertyChanged();
            }
        }

        private FireSmokeZoneFile _zoneFile = new FireSmokeZoneFile();

        private readonly object _zoneHistoryLock = new object();

        private readonly List<ZoneFrameContext> _zoneHistory = new List<ZoneFrameContext>();

        private DispatcherTimer _zoneTimer;

        private int _zoneEpoch;

        private int _eoZoneChannelEpoch;

        private int _irZoneChannelEpoch;

        private int _zoneCalibrationGeneration;

        public int ZoneCalibrationGeneration => _zoneCalibrationGeneration;

        private bool _zoneZeroPending;

        public bool IsZoneReferencePending => _zoneZeroPending || PanoramaReference.Pending || PanoramaReference.ZoneRebase != null;

        public event Action<PanoramaZoneRebase> ZoneReferenceChanged;

        private string _zoneStatus = "구역 미등록";

        private string _zoneLoadError;

        private DateTimeOffset _zoneCtecZoomAt;

        private int _zoneCtecZoomEpoch, _zoneLensQueryBusy;

        private string _zoneCtecZoomKey;

        private DateTimeOffset _zoneLensQueryErrorAt;

        private int _zoneImageBusy;

        private readonly object _zoneImageLock = new object();

        private readonly Dictionary<string, ZoneFrameContext> _zoneImageFrames = new Dictionary<string, ZoneFrameContext>();

        private DateTimeOffset _zoneImageErrorAt;

        private int _eoZoneImageCount = -1, _irZoneImageCount = -1;

        private async void RefreshZoneImageRegistration()
        {
            if (System.Threading.Interlocked.CompareExchange(ref _zoneImageBusy, 1, 0) != 0)
                return;

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
                    var source = camera == "IR" ? IRCameraImage : EOCameraImage;
                    // 가까운 저장 장면부터 처리하고 장면이 많아도 작업량을 제한한다.
                    var scenes = _zoneFile.Scenes.Where(s => s.Camera == camera &&
                        s.Key == frame.Key &&
                        !s.Invalidated &&
                        s.FixedInstallationConfirmed &&
                        ZonePresetCurrent(s)).OrderBy(s => FireSmokeZoneService.PanDistance(
                        s.Pan,
                        frame.Position?.Pan ?? s.Pan
                    ) + Math.Abs(s.Tilt - (frame.Position?.Tilt ?? s.Tilt))).Take(8).ToArray();

                    if (source == null || scenes.Length == 0 || ZoneFrameReadiness(frame, true) != null)
                        continue;

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
                        _zoneImageFrames[camera] = frame;
                    int previous = camera == "IR" ? _irZoneImageCount : _eoZoneImageCount;

                    if (previous != registered.Count)
                    {
                        if (camera == "IR")
                            _irZoneImageCount = registered.Count;
                        else
                            _eoZoneImageCount = registered.Count;

                        ConsoleLogHelper.State(
                            "ZONE IMAGE MATCH",
                            camera + " / MATCHED=" + registered.Count + " / CHECKED=" + scenes.Length
                        );
                    }

                    if (!ShowZoneOverlay)
                        continue;

                    RefreshZoneOverlay(CaptureZoneFrame(camera), camera);
                }
            }
            catch (Exception ex)
            {
                lock (_zoneImageLock)
                    _zoneImageFrames.Clear();

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

        // 문서화된 CTEC 0x47 렌즈 응답만 사용하고 불명확한 LA 0x08은 변경하지 않는다.
        private PositionSnapshot CaptureZonePosition()
        {
            var p = _positionSnapshotService.Capture("ZONE_FRAME");

            if (_connectedEoCtecSource == null)
                return p;

            double age = (DateTimeOffset.Now - _zoneCtecZoomAt).TotalSeconds;
            bool fresh = _zoneCtecZoomEpoch == _zoneEpoch &&
                _zoneCtecZoomKey == ZoneKey("EO") &&
                _zoneCtecZoomAt != default(DateTimeOffset) &&
                age >= 0 &&
                age <= 3;
            int? zoom = fresh ? (int?)GetCurrentPresetStandardZoom() : null;
            return new PositionSnapshot(
                p.CapturedAt,
                p.Source,
                p.PresetId,
                p.Pan,
                p.Tilt,
                zoom,
                p.EoFocus,
                p.IrZoom,
                p.IrFocus,
                p.Latitude,
                p.Longitude,
                p.Altitude,
                p.GpsSpeed,
                p.GpsCourse,
                p.GpsHdop,
                p.GpsSatelliteCount,
                p.GpsFixStatus,
                p.Roll,
                p.Pitch,
                p.Yaw,
                p.PtzStatus,
                fresh ? PositionDataStatus.Valid : PositionDataStatus.Stale,
                p.IrLensStatus,
                p.GpsStatus,
                p.ImuStatus,
                p.WebAgentConnected
            );
        }

        public async void RefreshZoneLensState()
        {
            var source = _connectedEoCtecSource;

            if (source == null || System.Threading.Interlocked.CompareExchange(ref _zoneLensQueryBusy, 1, 0) != 0)
                return;

            try
            {
                await RequestAndWaitCtecEoPositionAsync(ContinuousMoveType.EoZoom, source, System.Threading.CancellationToken.None);
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

        private static string ZonePath => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config", "FireSmokeZones.xml");

        public string ZoneStatusText => "구역 상태\n" + _zoneStatus;

        private static string ZoneHash(string text)
        {
            using (var sha = SHA256.Create())
                return BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(text ?? ""))).Replace("-", "");
        }

        public string ZoneEquipmentKey => ZoneHash(SelectedControlAgentProfile.DisplayName + "|" + ControlAgentIp + "|" + ControlAgentPortText);

        private string ZoneKey(string camera) =>
            ZoneHash(ZoneEquipmentKey + "|" + camera + "|" + (camera == "IR" ? IrSourceAddress : EoSourceAddress));

        public sealed class ZonePresetOption
        {
            public string Kind { get; set; }

            public PresetPointOption Point { get; set; }

            public override string ToString() => Kind + " P" + Point.Number.ToString("00") + " / " + Point.Name;
        }

        /// <summary>구역 설정과 이동은 현재 선택한 Agent의 프리셋만 사용한다.</summary>
        public List<ZonePresetOption> GetZonePresets()
        {
            if (IsSelectedControlAgentLaAgent)
            {
                return (LaPresetPoints ?? new ObservableCollection<PresetPointOption>())
                    .Select(p => new ZonePresetOption { Kind = "LA", Point = p }).ToList();
            }

            if (IsSelectedControlAgentWebAgent)
            {
                return (PresetPoints ?? new ObservableCollection<PresetPointOption>())
                    .Select(p => new ZonePresetOption { Kind = "WEB", Point = p }).ToList();
            }

            return new List<ZonePresetOption>();
        }

        /// <summary>직접 캡처와 현재 Agent 장면만 표시하고 다른 Agent의 저장 자료는 보존한다.</summary>
        public bool IsZoneSceneForCurrentAgent(FireSmokeScene scene)
        {
            if (scene == null)
                return false;

            return scene.PresetNumber == 0 ||
                (IsSelectedControlAgentLaAgent && scene.PresetKind == "LA") ||
                (IsSelectedControlAgentWebAgent && scene.PresetKind == "WEB");
        }

        /// <summary>프리셋 줌을 구역 장면과 동일한 장비 수신 좌표로 변환한다.</summary>
        private bool TryGetZonePresetZoomPosition(string camera, PresetPointOption preset, out int zoom)
        {
            if (camera == "IR")
            {
                // LA 표준 줌은 반전하고 WEB은 기존 위치·구형 호환 변환 결과를 그대로 사용한다.
                zoom = ConvertIrZoomStandardToStatusPosition(PresetIrZoomPosition(preset));
                return true;
            }

            return int.TryParse(preset.EoZoomText, out zoom);
        }

        public static string ZonePresetSignature(PresetPointOption p) =>
            p == null ? "" : string.Join(
            "|",
            p.Number,
            p.Pan.ToString(
            "R",
            System.Globalization.CultureInfo.InvariantCulture
        ),
            p.Tilt.ToString(
            "R",
            System.Globalization.CultureInfo.InvariantCulture
        ),
            p.EoZoomText,
            p.EoFocusText,
            p.IrZoomText,
            p.IrFocusText,
            p.PositionSnapshot?.CapturedAt.ToString("O") ?? "LEGACY"
        );

        public bool ZonePresetCurrent(FireSmokeScene scene) => ZonePresetMismatchReason(scene) == null;

        /// <summary>프리셋 번호·장비 종류를 먼저 확인하고 현재 장면의 카메라 기준으로만 변경을 판정한다.</summary>
        public string ZonePresetMismatchReason(FireSmokeScene scene)
        {
            if (scene == null)
                return "장면 없음";
            if (scene.PresetNumber == 0)
                return null;
            var current = GetZonePresets().FirstOrDefault(p => p.Kind == scene.PresetKind && p.Point.Number == scene.PresetNumber);
            return current == null ? "연결 프리셋 없음" : FireSmokeZoneService.PresetMismatch(scene, current.Point);
        }

        public void MoveZonePreset(ZonePresetOption p)
        {
            if (p == null || !GetZonePresets().Any(v => v.Kind == p.Kind && ReferenceEquals(v.Point, p.Point)))
                throw new InvalidOperationException("프리셋을 다시 선택하세요.");

            var command = p.Kind == "LA" ? MoveToLaPresetCommand : MoveToPresetCommand;

            if (p.Kind == "LA")
                SelectedLaPresetPoint = p.Point;
            else
                SelectedPresetPoint = p.Point;

            if (command == null || !command.CanExecute(null) || !IsOperationCommandEnabled || IsPresetScanControlLocked)
                throw new InvalidOperationException("현재 이동할 수 없습니다. 연결·작업 상태를 확인하세요.");

            command.Execute(null);
        }

        public string ZoneCaptureState(string camera) => ZoneFrameInvalidReason(CaptureZoneFrame(camera));

        public string ZonePresetArrivalReason(string camera, ZonePresetOption p)
        {
            if (p == null)
                return null;

            if (!GetZonePresets().Any(v => v.Kind == p.Kind && ReferenceEquals(v.Point, p.Point)))
                return "프리셋 변경 / 다시 선택";

            var frame = CaptureZoneFrame(camera);
            int zoom;

            if (!FireSmokeZoneService.ValidPose(
                frame.Position,
                camera
            ) ||
                !TryGetZonePresetZoomPosition(camera, p.Point, out zoom))
                return "프리셋 위치·ZOOM 미확인";

            // 구역 스냅샷은 원시 수신 좌표이다. LA 프리셋의 반전된 줌을 같은 기준으로 비교한다.
            int? currentZoom = camera == "IR" ? frame.Position.IrZoom : frame.Position.EoZoom;

            if (FireSmokeZoneService.PanDistance(
                frame.Position.Pan.Value,
                p.Point.Pan
            ) <= .3 &&
                Math.Abs(frame.Position.Tilt.Value - p.Point.Tilt) <= .3 &&
                FireSmokeZoneService.LensWithin(currentZoom, zoom, FireSmokeZoneService.ZoomTolerance))
                return null;

            return string.Format(
                System.Globalization.CultureInfo.InvariantCulture,
                "도착 대기 (줌: 장비 수신 기준)\n목표 P/T/Z: {0:F2} / {1:F2} / {2}\n수신 P/T/Z: {3:F2} / {4:F2} / {5}",
                p.Point.Pan,
                p.Point.Tilt,
                zoom,
                frame.Position.Pan.Value,
                frame.Position.Tilt.Value,
                currentZoom
            );
        }

        public void LinkZonePreset(FireSmokeScene scene, ZonePresetOption p)
        {
            if (p == null)
                return;

            if (!GetZonePresets().Any(v => v.Kind == p.Kind && ReferenceEquals(v.Point, p.Point)))
                throw new InvalidOperationException("프리셋 변경됨 / 다시 선택하세요.");

            int zoom;

            if (!TryGetZonePresetZoomPosition(scene.Camera, p.Point, out zoom) ||
                FireSmokeZoneService.PanDistance(
                scene.Pan,
                p.Point.Pan
            ) > scene.AngleTolerance ||
                Math.Abs(scene.Tilt - p.Point.Tilt) > scene.AngleTolerance ||
                !FireSmokeZoneService.LensWithin(scene.Zoom, zoom, FireSmokeZoneService.ZoomTolerance))
                throw new InvalidOperationException("선택 프리셋에 도착하지 않았습니다. 이동 후 캡처하세요.");

            scene.PresetKind = p.Kind;
            scene.PresetNumber = p.Point.Number;
            scene.PresetSignature = ZonePresetSignature(p.Point);
        }

        private void InitializeZoneFeatures()
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
                    RefreshZoneLensState();

                var eo = CaptureZoneFrame("EO");
                var ir = CaptureZoneFrame("IR");
                lock (_zoneHistoryLock)
                {
                    _zoneHistory.Add(eo);
                    _zoneHistory.Add(ir);
                    _zoneHistory.RemoveAll(f => (DateTimeOffset.Now - f.At).TotalSeconds > 8);
                }

                _zoneStatus = _zoneLoadError != null ? "설정 읽기 실패 / 확인 필요" : "EO : " + ZoneViewState(eo) + "\nIR : " + ZoneViewState(ir);
                OnPropertyChanged(nameof(ZoneStatusText));
                RefreshZoneOverlay(eo, "EO");
                RefreshZoneOverlay(ir, "IR");

                // 이동·연결 해제 후 활성 이벤트를 현재 유효한 것처럼 표시하지 않는다.
                foreach (var record in AiDetectionEvents.Concat(FireEvents).Where(r => r.Status == "ACTIVE" &&
                    r.FirstZoneAssessment != null))
                {
                    var frame = record.Camera == "IR" ? ir : eo;
                    string invalid = ZoneFrameInvalidReason(frame);

                    if (invalid != null ||
                        !_zoneFile.Scenes.Any(v => ZonePresetCurrent(v) &&
                        FireSmokeZoneService.ForFrame(
                        v,
                        frame
                    ) != null))
                        record.UpdateZoneAssessment(ZoneAssessment.Unknown(invalid ?? "등록 시점 불일치 / 다음 검출 프레임 확인 필요"));
                }
            };
            _zoneTimer.Start();
            System.Windows.Application.Current.Exit += (s, e) => _zoneTimer.Stop();
        }

        private string ZoneViewState(ZoneFrameContext f)
        {
            if (!_zoneFile.Scenes.Any(s => s.Key == f.Key))
                return "미등록";

            if (ZoneFrameReadiness(f, false) != null)
                return "미확인";

            var views = _zoneFile.Scenes.Where(ZonePresetCurrent).Select(s => FireSmokeZoneService.ForFrame(
                s,
                f
            )).Where(s => s != null).ToList();
            return views.Any(s => s.Zones.Count > 0) ? "영상 대조 일치" : views.Count > 0 ? "구역 화면 밖" : "구역 위치 미확인";
        }

        private void RefreshZoneOverlay(ZoneFrameContext frame, string camera)
        {
            var scenes = ShowZoneOverlay &&
                ZoneFrameReadiness(
                frame,
                false
            ) == null ? _zoneFile.Scenes.Where(ZonePresetCurrent).Select(s => FireSmokeZoneService.ForFrame(
                s,
                frame
            )).Where(s => s != null).ToList() : new List<FireSmokeScene>();
            string signature = frame.Width + ":" + frame.Height + ":" + frame.Position?.Pan + ":" + frame.Position?.Tilt + ":" + frame.HorizontalFov + ":" + string.Join(
                "|",
                scenes.Select(s => s.Id + s.Revision + string.Join(
                ";",
                s.Zones.Select(z => z.X.ToString("R") + "," + z.Y.ToString("R") + "," + z.Width.ToString("R") + "," + z.Height.ToString("R"))
            ))
            );

            if (signature == (camera == "IR" ? _irZoneSignature : _eoZoneSignature))
                return;

            if (camera == "IR")
                _irZoneSignature = signature;
            else
                _eoZoneSignature = signature;

            var target = camera == "IR" ? IrZoneOverlay : EoZoneOverlay;
            target.Clear();

            foreach (var scene in scenes)
                foreach (var zone in scene.Zones)
                    target.Add(new ZoneOverlayBox
                    {
                        Left = zone.X * frame.Width,
                        Top = zone.Y * frame.Height,
                        Width = zone.Width * frame.Width,
                        Height = zone.Height * frame.Height,
                        Polygon = zone.ProjectedPolygon?.Select(p => new System.Windows.Point(
                        p.X * frame.Width,
                        p.Y * frame.Height
                    )).ToList(),
                        Name = "ZONE " + zone.ToString(),
                        Color = zone.DisplayColor
                    });
        }

        public ZoneFrameContext CaptureZoneFrame(string camera)
        {
            var position = CaptureZonePosition();
            string model = SelectedControlAgentProfile?.DisplayName ?? "";
            bool supported = camera == "EO" &&
                (model.IndexOf(
                "MR300",
                StringComparison.OrdinalIgnoreCase
            ) >= 0 ||
                model.IndexOf(
                "MR500",
                StringComparison.OrdinalIgnoreCase
            ) >= 0) &&
                position.EoZoom.HasValue &&
                position.EoZoom.Value >= 0 &&
                position.EoZoom.Value <= 1000;
            var context = new ZoneFrameContext
            {
                RequireImageRegistration = true,
                At = DateTimeOffset.Now,
                Key = ZoneKey(camera),
                Epoch = System.Threading.Volatile.Read(ref _zoneEpoch),
                ChannelEpoch = camera == "IR" ? System.Threading.Volatile.Read(ref _irZoneChannelEpoch) : System.Threading.Volatile.Read(ref _eoZoneChannelEpoch),
                Position = position,
                HorizontalFov = supported ? new OpenCvWpfTracking.Services.Control.FieldOfViewSyncService().GetEoHfov((short)position.EoZoom.Value) : 0,
                Width = camera == "IR" ? IrVideoWidth : EoVideoWidth,
                Height = camera == "IR" ? IrVideoHeight : EoVideoHeight
            };
            lock (_zoneImageLock)
            {
                ZoneFrameContext aligned;

                if (_zoneImageFrames.TryGetValue(
                    camera,
                    out aligned
                ) &&
                    aligned.Key == context.Key &&
                    aligned.Epoch == context.Epoch &&
                    aligned.ChannelEpoch == context.ChannelEpoch &&
                    (context.At - aligned.At).TotalSeconds <= 1.5 &&
                    FireSmokeZoneService.ValidPose(
                    position,
                    camera
                ) &&
                    FireSmokeZoneService.ValidPose(
                    aligned.Position,
                    camera
                ) &&
                    FireSmokeZoneService.PanDistance(
                    position.Pan.Value,
                    aligned.Position.Pan.Value
                ) < .03 &&
                    Math.Abs(position.Tilt.Value - aligned.Position.Tilt.Value) < .03 &&
                    (camera == "IR" ? FireSmokeZoneService.LensWithin(position.IrZoom, aligned.Position.IrZoom.Value, FireSmokeZoneService.ZoomTolerance) : FireSmokeZoneService.LensWithin(position.EoZoom, aligned.Position.EoZoom.Value, FireSmokeZoneService.ZoomTolerance)))
                    context.RegisteredScenes = aligned.RegisteredScenes;
            }

            return context;
        }

        private string ZoneFrameInvalidReason(ZoneFrameContext f) => ZoneFrameReadiness(f, true);

        private string ZoneFrameReadiness(ZoneFrameContext f, bool requireStable)
        {
            if (IsZoneReferencePending)
                return "ZERO 좌표 기준 확인 대기 / 구역 보존";

            if (f == null ||
                f.Epoch != _zoneEpoch ||
                (DateTimeOffset.Now - f.At).TotalSeconds > 2 ||
                f.At > DateTimeOffset.Now.AddMilliseconds(100))
                return "프레임 시간/세션 불일치";

            string camera = f.Key == ZoneKey("EO") ? "EO" : f.Key == ZoneKey("IR") ? "IR" : null;

            if (camera == null)
                return "장비/영상 소스 변경";

            if (f.ChannelEpoch != (camera == "IR" ? _irZoneChannelEpoch : _eoZoneChannelEpoch))
                return "영상 재연결 전 프레임";

            if (requireStable && (!IsOperationCommandEnabled || IsPresetScanControlLocked))
                return "카메라 작업 중";

            if ((camera == "IR" ? IrConnectionStatusText : EoConnectionStatusText) != "Connected")
                return "영상 미연결";

            var perf = GetRtspPerformance(camera);
            lock (perf.Sync)
            {
                if (perf.LastFrame.ElapsedMilliseconds > 1000)
                    return "영상 프레임 지연";
            }

            if (!FireSmokeZoneService.ValidPose(f.Position, camera))
                return "PTZ/Zoom 상태 미수신·지연";

            // 이동 중 표시에는 현재 위치를 쓰되 캡처·판정은 안정된 프레임 대응이 필요하다.
            if (!requireStable)
                return null;

            var current = CaptureZonePosition();

            if (!FireSmokeZoneService.IsCameraPoseStable(f.Position, current, camera))
                return "검출 이후 카메라 위치/Zoom 변경";

            List<ZoneFrameContext> history;
            lock (_zoneHistoryLock)
                history = _zoneHistory.Where(h => h.Key == f.Key &&
                    h.Epoch == f.Epoch &&
                    h.ChannelEpoch == f.ChannelEpoch &&
                    h.At <= f.At &&
                    (f.At - h.At).TotalSeconds <= 2.5).ToList();

            if (history.Count < 4 || (f.At - history.First().At).TotalSeconds < 1.5 || (f.At - history.Last().At).TotalSeconds > 1.25)
                return "시점 안정화 대기 (약 2초)";

            // 최근 약 2초 동안 해당 채널의 줌 ±5와 기존 PT 기준을 함께 확인한다.
            if (history.Any(h => !FireSmokeZoneService.IsCameraPoseStable(f.Position, h.Position, camera)))
                return "카메라 이동/Zoom 변경 / 안정화 필요";

            return null;
        }

        public FireSmokeZoneFile GetZoneEditorData()
        {
            if (_zoneLoadError != null)
                throw new InvalidDataException("기존 구역 파일을 덮어쓰지 않습니다. 설정을 확인하세요: " + _zoneLoadError);

            return FireSmokeZoneService.Clone(_zoneFile);
        }

        public FireSmokeScene CaptureZoneScene(string camera, out BitmapSource image)
        {
            var frame = CaptureZoneFrame(camera);
            string reason = ZoneFrameInvalidReason(frame);

            if (reason != null)
                throw new InvalidOperationException(reason);

            // 이 이미지는 디코더·표시 픽셀만 포함하며 경고·검출 오버레이는 별도이다.
            // 별도 WPF 오버레이를 섞지 않도록 화면 트리 전체를 캡처하지 않는다.
            image = (camera == "IR" ? IRCameraImage : EOCameraImage)?.CloneCurrentValue();

            if (image == null)
                throw new InvalidOperationException("표시 영상 없음");

            image.Freeze();

            if (!IsZoneReferenceSizeCompatible(frame.Width, frame.Height, image.PixelWidth, image.PixelHeight))
                throw new InvalidOperationException("영상 크기 변경 중입니다. 잠시 후 다시 캡처하세요.");

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
                Name = SelectedControlAgentProfile.DisplayName + " / " + camera,
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

        // EO 표시 프레임은 의도적으로 축소하며 관심 구역은 정규화 좌표를 사용한다.
        // 기준 이미지는 작아도 원본 전체 프레임 비율을 유지한다.
        public static bool IsZoneReferenceSizeCompatible(int sourceWidth, int sourceHeight, int displayWidth, int displayHeight)
        {
            if (sourceWidth <= 0 || sourceHeight <= 0 || displayWidth <= 0 || displayHeight <= 0)
                return false;

            double expectedHeight = sourceHeight * (displayWidth / (double)sourceWidth);
            return Math.Abs(displayHeight - expectedHeight) <= 1.0;
        }

        public void SaveZoneEditorData(FireSmokeZoneFile data, int? expectedCalibrationGeneration = null)
        {
            if (expectedCalibrationGeneration.HasValue && expectedCalibrationGeneration != _zoneCalibrationGeneration)
                throw new InvalidOperationException("편집 중 원점이 변경되었습니다. 창을 닫고 다시 등록하세요.");

            var saved = FireSmokeZoneService.Clone(data);
            FireSmokeZoneService.Save(ZonePath, saved);
            _zoneFile = saved;
            ConsoleLogHelper.State("ZONE SAVE", "저장 완료 / SCENES=" + saved.Scenes.Count + " / PATH=" + ZonePath);
            lock (_zoneHistoryLock)
                _zoneHistory.Clear();
            OnPropertyChanged(nameof(ZoneStatusText));
        }

        /// <summary>ZERO 확인 전에는 구역을 지우지 않고 과거 프레임·정합 결과의 적용만 중지한다.</summary>
        private void BeginZoneReferenceChange()
        {
            _zoneCalibrationGeneration++;
            _zoneZeroPending = true;
            ResetZoneReferenceFrames();
        }

        private void ResetZoneReferenceFrames()
        {
            System.Threading.Interlocked.Increment(ref _zoneEpoch);
            lock (_zoneHistoryLock)
                _zoneHistory.Clear();
            lock (_zoneImageLock)
                _zoneImageFrames.Clear();
            _eoZoneSignature = null;
            _irZoneSignature = null;
            EoZoneOverlay.Clear();
            IrZoneOverlay.Clear();
        }

        /// <summary>변환과 적용 번호를 같은 XML에 확정해 재시작 시 중복 보정을 방지한다.</summary>
        private void ApplyZoneReferenceRebase(PanoramaZoneRebase operation)
        {
            if (_zoneLoadError != null)
                throw new IOException("구역 원본 확인 필요 / ZERO 변환 저장 보류: " + _zoneLoadError);
            var data = FireSmokeZoneService.Clone(_zoneFile);
            if (FireSmokeZoneService.RebaseZero(data, operation))
            {
                FireSmokeZoneService.Save(ZonePath, data);
                _zoneFile = data;
            }
            ResetZoneReferenceFrames();
            ZoneReferenceChanged?.Invoke(operation);
            ConsoleLogHelper.State("ZONE ZERO REFERENCE", "구역 보존·기준 갱신 / PAN_DELTA=" +
                operation.PanDelta.ToString("F3") + " / TILT_DELTA=" + operation.TiltDelta.ToString("F3") +
                " / SCENES=" + data.Scenes.Count(scene => scene.EquipmentKey == operation.EquipmentKey));
        }

        private void FinishZoneReferenceChange(bool notifyEditor = true)
        {
            _zoneZeroPending = false;
            if (notifyEditor)
                ZoneReferenceChanged?.Invoke(null);
        }

        private void ZoneChannelChanged(string camera)
        {
            if (camera == "IR")
                System.Threading.Interlocked.Increment(ref _irZoneChannelEpoch);
            else
                System.Threading.Interlocked.Increment(ref _eoZoneChannelEpoch);
        }

        private ZoneAssessment AssessZone(
            string camera,
            string type,
            double left,
            double top,
            double right,
            double bottom,
            ZoneFrameContext f
        )
        {
            string reason = ZoneFrameInvalidReason(f);
            return reason != null ? ZoneAssessment.Unknown(reason) : FireSmokeZoneService.Assess(
                new FireSmokeZoneFile { Scenes = _zoneFile.Scenes.Where(ZonePresetCurrent).ToList() },
                f,
                type,
                left,
                top,
                right,
                bottom
            );
        }

        private ZoneAssessment AssessAiZone(
            OpenCvWpfTracking.Models.AI.AiDetectionResult result,
            OpenCvWpfTracking.Models.AI.AiDetectionBox box,
            string type
        )
        {
            if ((type ?? "").IndexOf(
                "FIRE",
                StringComparison.OrdinalIgnoreCase
            ) < 0 &&
                (type ?? "").IndexOf(
                "SMOKE",
                StringComparison.OrdinalIgnoreCase
            ) < 0)
                return null;

            string camera = result.RtspIndex == 0 ? "EO" : "IR";

            if ((camera == "IR" ? AiRtsp1Address : AiRtsp0Address) != (camera == "IR" ? IrSourceAddress : EoSourceAddress))
                return ZoneAssessment.Unknown("AI와 Viewer 영상 소스 불일치");

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
                return ZoneAssessment.Unknown("AI 프레임 시각 지연/서버 시계 확인 필요");

            ZoneFrameContext f;
            lock (_zoneHistoryLock)
                f = _zoneHistory.Where(h => h.Key == ZoneKey(camera) &&
                    h.At <= at &&
                    (at - h.At).TotalSeconds <= 0.6).OrderByDescending(h => h.At).FirstOrDefault();
            return AssessZone(camera, type, box.Left, box.Top, box.Right, box.Bottom, f);
        }
    }
}
