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
            set { _showZoneOverlay = value; _eoZoneSignature = _irZoneSignature = null; OnPropertyChanged(); }
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
        private int _eoZoneImageCount=-1, _irZoneImageCount=-1;
        private async void RefreshZoneImageRegistration()
        {
            if(System.Threading.Interlocked.CompareExchange(ref _zoneImageBusy,1,0)!=0)return;
            try
            {
                foreach(string camera in new[]{"EO","IR"})
                {
                    var frame=CaptureZoneFrame(camera);
                    var source=camera=="IR" ? IRCameraImage : EOCameraImage;
                    // Prioritize the closest stored views; bound work even with many registered scenes.
                    var scenes=_zoneFile.Scenes.Where(s=>s.Camera==camera && s.Key==frame.Key && !s.Invalidated && s.FixedInstallationConfirmed && ZonePresetCurrent(s))
                        .OrderBy(s=>FireSmokeZoneService.PanDistance(s.Pan,frame.Position?.Pan ?? s.Pan)+Math.Abs(s.Tilt-(frame.Position?.Tilt ?? s.Tilt))).Take(8).ToArray();
                    if(source==null || scenes.Length==0 || ZoneFrameReadiness(frame,true)!=null)continue;
                    var image=source.CloneCurrentValue();image.Freeze();
                    var registered=await System.Threading.Tasks.Task.Run(()=>
                    {
                        var matches=new Dictionary<string,FireSmokeScene>();
                        byte[] pixels=ZoneImageRegistration.Encode(image);
                        foreach(var scene in scenes)
                        {
                            var aligned=ZoneImageRegistration.Match(scene,pixels);
                            if(aligned!=null) { aligned.Pan=frame.Position.Pan.Value;aligned.Tilt=frame.Position.Tilt.Value;aligned.Zoom=(camera=="IR"?frame.Position.IrZoom:frame.Position.EoZoom).Value;matches[scene.Id+scene.Revision]=aligned; }
                        }
                        return matches;
                    });
                    // Keep acquisition time, not completion time. Old results cannot survive a PTZ/zoom change.
                    frame.RegisteredScenes=registered;
                    lock(_zoneImageLock)_zoneImageFrames[camera]=frame;
                    int previous=camera=="IR"?_irZoneImageCount:_eoZoneImageCount;
                    if(previous!=registered.Count)
                    {
                        if(camera=="IR")_irZoneImageCount=registered.Count;else _eoZoneImageCount=registered.Count;
                        ConsoleLogHelper.State("ZONE IMAGE MATCH",camera+" / MATCHED="+registered.Count+" / CHECKED="+scenes.Length);
                    }
                    if(!ShowZoneOverlay)continue;
                    RefreshZoneOverlay(CaptureZoneFrame(camera),camera);
                }
            }
            catch(Exception ex)
            {
                lock(_zoneImageLock)_zoneImageFrames.Clear();
                if((DateTimeOffset.Now-_zoneImageErrorAt).TotalSeconds>=10)
                { _zoneImageErrorAt=DateTimeOffset.Now;ConsoleLogHelper.Error("ZONE IMAGE MATCH","영상 대조 실패 / 구역 적용 보류",ex); }
            }
            finally {System.Threading.Interlocked.Exchange(ref _zoneImageBusy,0);}
        }
        // Only the documented CTEC 0x47 lens response is used; unknown LA 0x08 remains untouched.
        private PositionSnapshot CaptureZonePosition()
        {
            var p = _positionSnapshotService.Capture("ZONE_FRAME");
            if (_connectedEoCtecSource == null) return p;
            double age = (DateTimeOffset.Now - _zoneCtecZoomAt).TotalSeconds;
            bool fresh = _zoneCtecZoomEpoch == _zoneEpoch && _zoneCtecZoomKey == ZoneKey("EO") &&
                _zoneCtecZoomAt != default(DateTimeOffset) && age >= 0 && age <= 3;
            int? zoom = fresh ? (int?)GetCurrentPresetStandardZoom() : null;
            return new PositionSnapshot(p.CapturedAt,p.Source,p.PresetId,p.Pan,p.Tilt,zoom,p.EoFocus,p.IrZoom,p.IrFocus,
                p.Latitude,p.Longitude,p.Altitude,p.GpsSpeed,p.GpsCourse,p.GpsHdop,p.GpsSatelliteCount,p.GpsFixStatus,
                p.Roll,p.Pitch,p.Yaw,p.PtzStatus,fresh?PositionDataStatus.Valid:PositionDataStatus.Stale,p.IrLensStatus,p.GpsStatus,p.ImuStatus,p.WebAgentConnected);
        }
        public async void RefreshZoneLensState()
        {
            var source = _connectedEoCtecSource;
            if (source == null || System.Threading.Interlocked.CompareExchange(ref _zoneLensQueryBusy,1,0) != 0) return;
            try { await RequestAndWaitCtecEoPositionAsync(ContinuousMoveType.EoZoom,source,System.Threading.CancellationToken.None); }
            catch (Exception ex) { if((DateTimeOffset.Now-_zoneLensQueryErrorAt).TotalSeconds>=5) { _zoneLensQueryErrorAt=DateTimeOffset.Now; ConsoleLogHelper.Error("ZONE LENS QUERY", "렌즈 상태 조회 실패 / 자동 캡처 보류", ex); } }
            finally { System.Threading.Interlocked.Exchange(ref _zoneLensQueryBusy,0); }
        }
        private static string ZonePath => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config", "FireSmokeZones.xml");
        public string ZoneStatusText => "구역 상태\n" + _zoneStatus;
        private static string ZoneHash(string text)
        {
            using (var sha = SHA256.Create()) return BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(text ?? ""))).Replace("-", "");
        }
        public string ZoneEquipmentKey => ZoneHash(SelectedControlAgentProfile.DisplayName + "|" + ControlAgentIp + "|" + ControlAgentPortText);
        private string ZoneKey(string camera) => ZoneHash(ZoneEquipmentKey + "|" + camera + "|" +
            (camera == "IR" ? IrSourceAddress : EoSourceAddress));
        public sealed class ZonePresetOption
        {
            public string Kind { get; set; }
            public PresetPointOption Point { get; set; }
            public override string ToString() => Kind + " P" + Point.Number.ToString("00") + " / " + Point.Name;
        }
        public List<ZonePresetOption> GetZonePresets() => (LaPresetPoints ?? new ObservableCollection<PresetPointOption>())
            .Select(p => new ZonePresetOption { Kind = "LA", Point = p }).Concat((PresetPoints ?? new ObservableCollection<PresetPointOption>())
            .Select(p => new ZonePresetOption { Kind = "WEB", Point = p })).ToList();
        public static string ZonePresetSignature(PresetPointOption p) => p == null ? "" :
            string.Join("|", p.Number, p.Pan.ToString("R", System.Globalization.CultureInfo.InvariantCulture),
                p.Tilt.ToString("R", System.Globalization.CultureInfo.InvariantCulture), p.EoZoomText, p.EoFocusText, p.IrZoomText, p.IrFocusText,
                p.PositionSnapshot?.CapturedAt.ToString("O") ?? "LEGACY");
        public bool ZonePresetCurrent(FireSmokeScene scene) => scene.PresetNumber == 0 || GetZonePresets().Any(p =>
            p.Kind == scene.PresetKind && p.Point.Number == scene.PresetNumber && ZonePresetSignature(p.Point) == scene.PresetSignature);
        public void MoveZonePreset(ZonePresetOption p)
        {
            if (p == null || !GetZonePresets().Any(v => v.Kind == p.Kind && ReferenceEquals(v.Point, p.Point)))
                throw new InvalidOperationException("프리셋을 다시 선택하세요.");
            var command = p.Kind == "LA" ? MoveToLaPresetCommand : MoveToPresetCommand;
            if (p.Kind == "LA") SelectedLaPresetPoint = p.Point; else SelectedPresetPoint = p.Point;
            if (command == null || !command.CanExecute(null) || !IsOperationCommandEnabled || IsPresetScanControlLocked)
                throw new InvalidOperationException("현재 이동할 수 없습니다. 연결·작업 상태를 확인하세요.");
            command.Execute(null);
        }
        public string ZoneCaptureState(string camera) => ZoneFrameInvalidReason(CaptureZoneFrame(camera));
        public string ZonePresetArrivalReason(string camera, ZonePresetOption p)
        {
            if (p == null) return null;
            if (!GetZonePresets().Any(v => v.Kind == p.Kind && ReferenceEquals(v.Point, p.Point))) return "프리셋 변경 / 다시 선택";
            var frame = CaptureZoneFrame(camera); int zoom;
            if (!FireSmokeZoneService.ValidPose(frame.Position, camera) || !int.TryParse(camera == "IR" ? p.Point.IrZoomText : p.Point.EoZoomText, out zoom)) return "프리셋 위치·ZOOM 미확인";
            int? currentZoom = camera == "IR" ? frame.Position.IrZoom : frame.Position.EoZoom;
            if (FireSmokeZoneService.PanDistance(frame.Position.Pan.Value, p.Point.Pan) <= .3 &&
                Math.Abs(frame.Position.Tilt.Value - p.Point.Tilt) <= .3 && currentZoom == zoom) return null;
            return string.Format(System.Globalization.CultureInfo.InvariantCulture,
                "도착 대기\n목표 P/T/Z: {0:F2} / {1:F2} / {2}\n수신 P/T/Z: {3:F2} / {4:F2} / {5}",
                p.Point.Pan, p.Point.Tilt, zoom, frame.Position.Pan.Value, frame.Position.Tilt.Value, currentZoom);
        }
        public void LinkZonePreset(FireSmokeScene scene, ZonePresetOption p)
        {
            if (p == null) return;
            if (!GetZonePresets().Any(v => v.Kind == p.Kind && ReferenceEquals(v.Point, p.Point)))
                throw new InvalidOperationException("프리셋 변경됨 / 다시 선택하세요.");
            int zoom;
            if (!int.TryParse(scene.Camera == "IR" ? p.Point.IrZoomText : p.Point.EoZoomText, out zoom) ||
                FireSmokeZoneService.PanDistance(scene.Pan, p.Point.Pan) > scene.AngleTolerance ||
                Math.Abs(scene.Tilt - p.Point.Tilt) > scene.AngleTolerance || scene.Zoom != zoom)
                throw new InvalidOperationException("선택 프리셋에 도착하지 않았습니다. 이동 후 캡처하세요.");
            scene.PresetKind = p.Kind; scene.PresetNumber = p.Point.Number; scene.PresetSignature = ZonePresetSignature(p.Point);
        }
        private void InitializeZoneFeatures()
        {
            try { _zoneFile = FireSmokeZoneService.Load(ZonePath); }
            catch (Exception ex) { _zoneLoadError = ex.Message; ConsoleLogHelper.Error("ZONE LOAD", "설정 읽기 실패 / 원본 파일 유지", ex); }
            _zoneTimer = new DispatcherTimer(DispatcherPriority.Background) { Interval = TimeSpan.FromMilliseconds(500) };
            _zoneTimer.Tick += (s, e) =>
            {
                RefreshZoneImageRegistration();
                if(ShowZoneOverlay && _zoneFile.Scenes.Any(scene=>scene.Camera=="EO")) RefreshZoneLensState();
                var eo = CaptureZoneFrame("EO"); var ir = CaptureZoneFrame("IR");
                lock (_zoneHistoryLock)
                {
                    _zoneHistory.Add(eo); _zoneHistory.Add(ir);
                    _zoneHistory.RemoveAll(f => (DateTimeOffset.Now - f.At).TotalSeconds > 8);
                }
                _zoneStatus = _zoneLoadError != null ? "설정 읽기 실패 / 확인 필요" :
                    "EO : " + ZoneViewState(eo) + "\nIR : " + ZoneViewState(ir);
                OnPropertyChanged(nameof(ZoneStatusText));
                RefreshZoneOverlay(eo, "EO"); RefreshZoneOverlay(ir, "IR");
                // Do not leave ACTIVE events looking applicable after movement/disconnect.
                foreach (var record in AiDetectionEvents.Concat(FireEvents).Where(r => r.Status == "ACTIVE" && r.FirstZoneAssessment != null))
                {
                    var frame = record.Camera == "IR" ? ir : eo;
                    string invalid = ZoneFrameInvalidReason(frame);
                    if (invalid != null || !_zoneFile.Scenes.Any(v => ZonePresetCurrent(v) && FireSmokeZoneService.ForFrame(v, frame) != null))
                        record.UpdateZoneAssessment(ZoneAssessment.Unknown(invalid ?? "등록 시점 불일치 / 다음 검출 프레임 확인 필요"));
                }
            };
            _zoneTimer.Start();
            System.Windows.Application.Current.Exit += (s, e) => _zoneTimer.Stop();
        }
        private string ZoneViewState(ZoneFrameContext f)
        {
            if (!_zoneFile.Scenes.Any(s => s.Key == f.Key)) return "미등록";
            if (ZoneFrameReadiness(f, false) != null) return "미확인";
            var views=_zoneFile.Scenes.Where(ZonePresetCurrent).Select(s=>FireSmokeZoneService.ForFrame(s,f)).Where(s=>s!=null).ToList();
            return views.Any(s=>s.Zones.Count>0) ? "영상 대조 일치" :
                views.Count>0 ? "구역 화면 밖" : "구역 위치 미확인";
        }
        private void RefreshZoneOverlay(ZoneFrameContext frame, string camera)
        {
            var scenes = ShowZoneOverlay && ZoneFrameReadiness(frame, false) == null
                ? _zoneFile.Scenes.Where(ZonePresetCurrent).Select(s=>FireSmokeZoneService.ForFrame(s,frame)).Where(s=>s!=null).ToList()
                : new List<FireSmokeScene>();
            string signature = frame.Width + ":" + frame.Height + ":" + frame.Position?.Pan + ":" + frame.Position?.Tilt + ":" + frame.HorizontalFov + ":" + string.Join("|", scenes.Select(s => s.Id + s.Revision+string.Join(";",s.Zones.Select(z=>z.X.ToString("R")+","+z.Y.ToString("R")+","+z.Width.ToString("R")+","+z.Height.ToString("R")))));
            if (signature == (camera == "IR" ? _irZoneSignature : _eoZoneSignature)) return;
            if (camera == "IR") _irZoneSignature = signature; else _eoZoneSignature = signature;
            var target = camera == "IR" ? IrZoneOverlay : EoZoneOverlay; target.Clear();
            foreach (var scene in scenes)
                foreach (var zone in scene.Zones)
                    target.Add(new ZoneOverlayBox { Left = zone.X * frame.Width, Top = zone.Y * frame.Height,
                        Width = zone.Width * frame.Width, Height = zone.Height * frame.Height,
                        Polygon=zone.ProjectedPolygon?.Select(p=>new System.Windows.Point(p.X*frame.Width,p.Y*frame.Height)).ToList(),
                        Name = "ZONE " + zone.ToString(), Color = "#C8A2FF" });
        }
        public ZoneFrameContext CaptureZoneFrame(string camera)
        {
            var position=CaptureZonePosition();
            string model=SelectedControlAgentProfile?.DisplayName ?? "";
            bool supported=camera=="EO" && (model.IndexOf("MR300",StringComparison.OrdinalIgnoreCase)>=0 || model.IndexOf("MR500",StringComparison.OrdinalIgnoreCase)>=0) &&
                position.EoZoom.HasValue && position.EoZoom.Value>=0 && position.EoZoom.Value<=1000;
            var context = new ZoneFrameContext
            {
                RequireImageRegistration=true,
                At = DateTimeOffset.Now, Key = ZoneKey(camera), Epoch = System.Threading.Volatile.Read(ref _zoneEpoch),
                ChannelEpoch = camera == "IR" ? System.Threading.Volatile.Read(ref _irZoneChannelEpoch) : System.Threading.Volatile.Read(ref _eoZoneChannelEpoch),
                Position = position,
                HorizontalFov=supported ? new OpenCvWpfTracking.Services.Control.FieldOfViewSyncService().GetEoHfov((short)position.EoZoom.Value) : 0,
                Width = camera == "IR" ? IrVideoWidth : EoVideoWidth,
                Height = camera == "IR" ? IrVideoHeight : EoVideoHeight
            };
            lock(_zoneImageLock)
            {
                ZoneFrameContext aligned;
                if(_zoneImageFrames.TryGetValue(camera,out aligned) && aligned.Key==context.Key && aligned.Epoch==context.Epoch &&
                    aligned.ChannelEpoch==context.ChannelEpoch && (context.At-aligned.At).TotalSeconds<=1.5 &&
                    FireSmokeZoneService.ValidPose(position,camera) && FireSmokeZoneService.ValidPose(aligned.Position,camera) &&
                    FireSmokeZoneService.PanDistance(position.Pan.Value,aligned.Position.Pan.Value)<.03 &&
                    Math.Abs(position.Tilt.Value-aligned.Position.Tilt.Value)<.03 &&
                    (camera=="IR" ? position.IrZoom==aligned.Position.IrZoom : position.EoZoom==aligned.Position.EoZoom))
                    context.RegisteredScenes=aligned.RegisteredScenes;
            }
            return context;
        }
        private string ZoneFrameInvalidReason(ZoneFrameContext f)
            => ZoneFrameReadiness(f, true);
        private string ZoneFrameReadiness(ZoneFrameContext f, bool requireStable)
        {
            if (f == null || f.Epoch != _zoneEpoch || (DateTimeOffset.Now - f.At).TotalSeconds > 2 || f.At > DateTimeOffset.Now.AddMilliseconds(100))
                return "프레임 시간/세션 불일치";
            string camera = f.Key == ZoneKey("EO") ? "EO" : f.Key == ZoneKey("IR") ? "IR" : null;
            if (camera == null) return "장비/영상 소스 변경";
            if (f.ChannelEpoch != (camera == "IR" ? _irZoneChannelEpoch : _eoZoneChannelEpoch)) return "영상 재연결 전 프레임";
            if (requireStable && (!IsOperationCommandEnabled || IsPresetScanControlLocked)) return "카메라 작업 중";
            if ((camera == "IR" ? IrConnectionStatusText : EoConnectionStatusText) != "Connected") return "영상 미연결";
            var perf = GetRtspPerformance(camera);
            lock (perf.Sync) { if (perf.LastFrame.ElapsedMilliseconds > 1000) return "영상 프레임 지연"; }
            if (!FireSmokeZoneService.ValidPose(f.Position, camera)) return "PTZ/Zoom 상태 미수신·지연";
            // Live display uses current pose during movement; capture/assessment still require stable frame association.
            if (!requireStable) return null;
            var current = CaptureZonePosition();
            if (!FireSmokeZoneService.ValidPose(current, camera) ||
                FireSmokeZoneService.PanDistance(current.Pan.Value, f.Position.Pan.Value) > 0.05 ||
                Math.Abs(current.Tilt.Value - f.Position.Tilt.Value) > 0.05 ||
                (camera == "IR" ? current.IrZoom != f.Position.IrZoom : current.EoZoom != f.Position.EoZoom))
                return "검출 이후 카메라 위치/Zoom 변경";
            List<ZoneFrameContext> history;
            lock (_zoneHistoryLock) history = _zoneHistory.Where(h => h.Key == f.Key && h.Epoch == f.Epoch && h.ChannelEpoch == f.ChannelEpoch &&
                h.At <= f.At && (f.At - h.At).TotalSeconds <= 4.5).ToList();
            if (history.Count < 7 || (f.At - history.First().At).TotalSeconds < 3.5 ||
                (f.At - history.Last().At).TotalSeconds > 0.9) return "시점 안정화 대기 (약 4초)";
            if (history.Any(h => !FireSmokeZoneService.ValidPose(h.Position, camera) ||
                FireSmokeZoneService.PanDistance(h.Position.Pan.Value, f.Position.Pan.Value) > 0.05 ||
                Math.Abs(h.Position.Tilt.Value - f.Position.Tilt.Value) > 0.05 ||
                (camera == "IR" ? h.Position.IrZoom != f.Position.IrZoom : h.Position.EoZoom != f.Position.EoZoom)))
                return "카메라 이동/Zoom 변경 / 안정화 필요";
            return null;
        }
        public FireSmokeZoneFile GetZoneEditorData()
        {
            if (_zoneLoadError != null) throw new InvalidDataException("기존 구역 파일을 덮어쓰지 않습니다. 설정을 확인하세요: " + _zoneLoadError);
            return FireSmokeZoneService.Clone(_zoneFile);
        }
        public FireSmokeScene CaptureZoneScene(string camera, out BitmapSource image)
        {
            var frame = CaptureZoneFrame(camera);
            string reason = ZoneFrameInvalidReason(frame);
            if (reason != null) throw new InvalidOperationException(reason);
            // These images are decoder/display pixels only. AI/VP/ZONE and warning
            // layers are separate WPF elements; do not capture the visual tree.
            image = (camera == "IR" ? IRCameraImage : EOCameraImage)?.CloneCurrentValue();
            if (image == null) throw new InvalidOperationException("표시 영상 없음");
            image.Freeze();
            if (!IsZoneReferenceSizeCompatible(frame.Width, frame.Height, image.PixelWidth, image.PixelHeight))
                throw new InvalidOperationException("영상 크기 변경 중입니다. 잠시 후 다시 캡처하세요.");
            byte[] png;
            using (var stream = new MemoryStream())
            {
                var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(image)); encoder.Save(stream); png = stream.ToArray();
            }
            return new FireSmokeScene
            {
                Name = SelectedControlAgentProfile.DisplayName + " / " + camera,
                Camera = camera, Key = frame.Key, EquipmentKey = ZoneEquipmentKey, RegisteredAt = frame.At.ToString("O"),
                Pan = frame.Position.Pan.Value, Tilt = frame.Position.Tilt.Value,
                Zoom = (camera == "IR" ? frame.Position.IrZoom : frame.Position.EoZoom).Value,
                FrameWidth = frame.Width, FrameHeight = frame.Height, ReferencePng = png, HorizontalFov=frame.HorizontalFov
            };
        }
        // EO display frames are deliberately resized. ROIs are normalized, so
        // the reference may be smaller while retaining the full-frame aspect.
        public static bool IsZoneReferenceSizeCompatible(int sourceWidth, int sourceHeight, int displayWidth, int displayHeight)
        {
            if (sourceWidth <= 0 || sourceHeight <= 0 || displayWidth <= 0 || displayHeight <= 0) return false;
            double expectedHeight = sourceHeight * (displayWidth / (double)sourceWidth);
            return Math.Abs(displayHeight - expectedHeight) <= 1.0;
        }
        public void SaveZoneEditorData(FireSmokeZoneFile data, int? expectedCalibrationGeneration = null)
        {
            if (expectedCalibrationGeneration.HasValue && expectedCalibrationGeneration != _zoneCalibrationGeneration)
                throw new InvalidOperationException("편집 중 원점이 변경되었습니다. 창을 닫고 다시 등록하세요.");
            var saved = FireSmokeZoneService.Clone(data); FireSmokeZoneService.Save(ZonePath, saved); _zoneFile = saved;
            ConsoleLogHelper.State("ZONE SAVE","저장 완료 / SCENES="+saved.Scenes.Count+" / PATH="+ZonePath);
            lock (_zoneHistoryLock) _zoneHistory.Clear();
            OnPropertyChanged(nameof(ZoneStatusText));
        }
        private void InvalidateZoneCalibration()
        {
            _zoneCalibrationGeneration++;
            System.Threading.Interlocked.Increment(ref _zoneEpoch);
            var data = FireSmokeZoneService.Clone(_zoneFile);
            foreach (var scene in data.Scenes.Where(s => s.EquipmentKey == ZoneEquipmentKey)) scene.Invalidated = true;
            try { SaveZoneEditorData(data); }
            catch (Exception ex) { _zoneFile = data; _zoneLoadError = "ZERO 후 설정 저장 실패 / 재등록 필요"; ConsoleLogHelper.Error("ZONE ZERO", "구역 무효화 저장 실패", ex); }
        }
        private void ZoneChannelChanged(string camera)
        {
            if (camera == "IR") System.Threading.Interlocked.Increment(ref _irZoneChannelEpoch);
            else System.Threading.Interlocked.Increment(ref _eoZoneChannelEpoch);
        }
        private ZoneAssessment AssessZone(string camera, string type, double left, double top, double right, double bottom, ZoneFrameContext f)
        {
            string reason = ZoneFrameInvalidReason(f);
            return reason != null ? ZoneAssessment.Unknown(reason) : FireSmokeZoneService.Assess(
                new FireSmokeZoneFile { Scenes = _zoneFile.Scenes.Where(ZonePresetCurrent).ToList() }, f, type, left, top, right, bottom);
        }
        private ZoneAssessment AssessAiZone(OpenCvWpfTracking.Models.AI.AiDetectionResult result, OpenCvWpfTracking.Models.AI.AiDetectionBox box, string type)
        {
            if ((type ?? "").IndexOf("FIRE", StringComparison.OrdinalIgnoreCase) < 0 &&
                (type ?? "").IndexOf("SMOKE", StringComparison.OrdinalIgnoreCase) < 0) return null;
            string camera = result.RtspIndex == 0 ? "EO" : "IR";
            if ((camera == "IR" ? AiRtsp1Address : AiRtsp0Address) != (camera == "IR" ? IrSourceAddress : EoSourceAddress))
                return ZoneAssessment.Unknown("AI와 Viewer 영상 소스 불일치");
            DateTimeOffset at;
            try { at = DateTimeOffset.FromUnixTimeMilliseconds(result.FrameTime); }
            catch (ArgumentOutOfRangeException) { return ZoneAssessment.Unknown("AI 프레임 시각 해석 불가"); }
            if (Math.Abs((DateTimeOffset.Now - at).TotalSeconds) > 2) return ZoneAssessment.Unknown("AI 프레임 시각 지연/서버 시계 확인 필요");
            ZoneFrameContext f;
            lock (_zoneHistoryLock) f = _zoneHistory.Where(h => h.Key == ZoneKey(camera) && h.At <= at && (at - h.At).TotalSeconds <= 0.6)
                .OrderByDescending(h => h.At).FirstOrDefault();
            return AssessZone(camera, type, box.Left, box.Top, box.Right, box.Bottom, f);
        }
    }
}
