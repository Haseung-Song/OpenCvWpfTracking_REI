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

        /// <summary>
        /// 일반 WEB 줌은 수신·송신 위치를 그대로 사용한다. LA의 기존 방향 반전만 유지한다.
        /// </summary>
        int ConvertIrZoomStandardToStatusPosition(int standardPosition);

        BitmapSource EOCameraImage { get; set; }

        string EoConnectionStatusText { get; }

        string EoSourceAddress { get; set; }

        int EoVideoHeight { get; set; }

        int EoVideoWidth { get; set; }

        ObservableCollection<FireEventRecord> FireEvents { get; }

        /// <summary>
        /// 현재 EO Zoom 값을 프리셋 공통 범위 0~1000으로 변환한다.
        /// </summary>
        ushort GetCurrentPresetStandardZoom();

        /// <summary>
        /// EO/IR 채널별 수신·표시 성능 측정 상태를 선택한다.
        /// </summary>
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

        /// <summary>
        /// 바인딩 속성 변경 알림
        /// </summary>
        void OnPropertyChanged([global::System.Runtime.CompilerServices.CallerMemberName] string propertyName = null);

        PanoramaReferenceState PanoramaReference { get; }

        /// <summary>
        /// 과거 WEB 줌 저장값은 호환 변환만 적용하고 새 프리셋은 장비 위치를 그대로 사용한다.
        /// </summary>
        int PresetIrZoomPosition(PresetPointOption preset);

        ObservableCollection<PresetPointOption> PresetPoints { get; }

        /// <summary>
        /// [CTEC Position Inquiry] CGI 송신 후 실제 TCP 9000 Position 응답까지 기다린다. HTTP 200
        /// OK만으로 조회 완료 처리하지 않는다. Zoom은 0x47, Focus는 0x48 응답이 도착해야 한 번의 Inquiry가 끝난다.
        /// </summary>
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

        /// <summary>
        /// 감시·제외 구역 기능에서 사용할 협력 객체를 연결하고 초기 상태를 준비한다.
        /// </summary>
        internal ZonesFeature(IZonesFeatureContext context)
        {
            _context = context ?? throw new System.ArgumentNullException(nameof(context));
        }

        /// <summary>
        /// 기능 객체가 사용하는 초기 상태·목록·타이머를 준비한다.
        /// </summary>
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

        /// <summary>
        /// 현재 장면과 구역 참조 영상의 정합 상태를 갱신한다.
        /// </summary>
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

        /// <summary>
        /// 구역 참조에 사용할 현재 위치·렌즈 스냅샷을 가져온다.
        /// </summary>
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

        /// <summary>
        /// 현재 렌즈가 저장한 구역 참조 시야에 맞는지 표시를 갱신한다.
        /// </summary>
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

        /// <summary>
        /// 구역 내용의 변경을 비교할 해시 값을 만든다.
        /// </summary>
        internal static string ZoneHash(string text)
        {
            using (var sha = SHA256.Create())
            {
                return BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(text ?? ""))).Replace("-", "");
            }
        }

        /// <summary>
        /// 현재 장비와 채널에 대응하는 구역 저장 키를 만든다.
        /// </summary>
        internal string ZoneKey(string camera)
        {
            return ZoneHash(ZoneEquipmentKey + "|" + camera + "|" + (camera == "IR" ? _context.IrSourceAddress : _context.EoSourceAddress));
        }

        /// <summary>
        /// 구역 설정과 이동은 현재 선택한 Agent의 프리셋만 사용한다.
        /// </summary>
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

        /// <summary>
        /// 직접 캡처와 현재 Agent 장면만 표시하고 다른 Agent의 저장 자료는 보존한다.
        /// </summary>
        internal bool IsZoneSceneForCurrentAgent(FireSmokeScene scene)
        {
            if (scene == null)
            {
                return false;
            }

            return scene.PresetNumber == 0 || (_context.IsSelectedControlAgentLaAgent && scene.PresetKind == "LA") || (_context.IsSelectedControlAgentWebAgent && scene.PresetKind == "WEB");
        }

        /// <summary>
        /// 프리셋 줌을 구역 장면과 동일한 장비 수신 좌표로 변환한다.
        /// </summary>
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

        /// <summary>
        /// 구역에 연결된 프리셋의 좌표·렌즈 정보를 비교 문자열로 만든다.
        /// </summary>
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

        /// <summary>
        /// 현재 장비 위치가 구역 연결 프리셋과 일치하는지 확인한다.
        /// </summary>
        internal bool ZonePresetCurrent(FireSmokeScene scene)
        {
            return ZonePresetMismatchReason(scene) == null;
        }

        /// <summary>
        /// 프리셋 번호·장비 종류를 먼저 확인하고 현재 장면의 카메라 기준으로만 변경을 판정한다.
        /// </summary>
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

        /// <summary>
        /// 구역에 연결된 프리셋 위치로 이동을 요청한다.
        /// </summary>
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

        /// <summary>
        /// 구역 참조 촬영의 현재 준비·진행 상태를 표시한다.
        /// </summary>
        internal string ZoneCaptureState(string camera)
        {
            return ZoneFrameInvalidReason(CaptureZoneFrame(camera));
        }

        /// <summary>
        /// 구역 프리셋에 도달했는지 확인하지 못한 이유를 반환한다.
        /// </summary>
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

        /// <summary>
        /// 구역 참조와 선택한 프리셋의 연결 정보를 저장한다.
        /// </summary>
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

        /// <summary>
        /// 구역 저장소와 화면 갱신 이벤트를 초기화한다.
        /// </summary>
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

        /// <summary>
        /// 구역 기준 위치·시야의 상태를 화면 표시 문자열로 변환한다.
        /// </summary>
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

        /// <summary>
        /// 현재 장면 기준에 맞는 구역 도형과 표시 상태를 다시 구성한다.
        /// </summary>
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

        /// <summary>
        /// 구역 참조에 사용할 채널 영상 프레임을 안전하게 복사한다.
        /// </summary>
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

        /// <summary>
        /// 구역 참조 프레임을 사용할 수 없는 원인을 반환한다.
        /// </summary>
        internal string ZoneFrameInvalidReason(ZoneFrameContext f)
        {
            return ZoneFrameReadiness(f, true);
        }

        /// <summary>
        /// 구역 참조 프레임의 촬영 준비 여부를 확인한다.
        /// </summary>
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

        /// <summary>
        /// 구역 편집 창에 전달할 장면·도형·위치 데이터를 구성한다.
        /// </summary>
        internal FireSmokeZoneFile GetZoneEditorData()
        {
            if (_zoneLoadError != null)
            {
                throw new InvalidDataException("기존 구역 파일을 덮어쓰지 않습니다. 설정을 확인하세요: " + _zoneLoadError);
            }

            return FireSmokeZoneService.Clone(_zoneFile);
        }

        /// <summary>
        /// 현재 영상과 위치를 구역 장면 참조로 촬영한다.
        /// </summary>
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

        /// <summary>
        /// 저장한 구역 참조 영상 크기가 현재 영상과 호환되는지 확인한다.
        /// </summary>
        internal static bool IsZoneReferenceSizeCompatible(int sourceWidth, int sourceHeight, int displayWidth, int displayHeight)
        {
            if (sourceWidth <= 0 || sourceHeight <= 0 || displayWidth <= 0 || displayHeight <= 0)
            {
                return false;
            }

            double expectedHeight = sourceHeight * (displayWidth / (double)sourceWidth);

            return Math.Abs(displayHeight - expectedHeight) <= 1.0;
        }

        /// <summary>
        /// 편집한 장면과 구역 도형을 저장하고 관련 표시를 갱신한다.
        /// </summary>
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

        /// <summary>
        /// ZERO 확인 전에는 구역을 지우지 않고 과거 프레임·정합 결과의 적용만 중지한다.
        /// </summary>
        internal void BeginZoneReferenceChange()
        {
            _zoneCalibrationGeneration++;

            _zoneZeroPending = true;

            ResetZoneReferenceFrames();
        }

        /// <summary>
        /// 보관 중인 구역 참조 프레임을 해제하고 촬영 상태를 초기화한다.
        /// </summary>
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

        /// <summary>
        /// 변환과 적용 번호를 같은 XML에 확정해 재시작 시 중복 보정을 방지한다.
        /// </summary>
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

        /// <summary>
        /// 장면 기준 변경 후 구역 참조와 표시 상태를 마무리한다.
        /// </summary>
        internal void FinishZoneReferenceChange(bool notifyEditor = true)
        {
            _zoneZeroPending = false;

            if (notifyEditor)
            {
                ZoneReferenceChanged?.Invoke(null);
            }
        }

        /// <summary>
        /// 선택한 영상 채널에 맞춰 구역 참조와 표시 상태를 갱신한다.
        /// </summary>
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

        /// <summary>
        /// 영상 탐지 사각형과 현재 위치를 이용해 구역 판정 결과를 구한다.
        /// </summary>
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

        /// <summary>
        /// AI 탐지 사각형과 현재 위치를 이용해 감시·제외 구역 포함 여부를 판정한다.
        /// </summary>
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
