using OpenCvWpfTracking.Common;
using OpenCvWpfTracking.Models.Main;
using OpenCvWpfTracking.Services.Video;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static OpenCvWpfTracking.ViewModels.Main.MainViewModel;

namespace OpenCvWpfTracking.ViewModels.Main
{
    /// <summary>파노라마 좌표 기준에 필요한 협력 기능만 받는 경계입니다. 화면 객체의 내부 구현을 알지 않습니다.</summary>
    internal interface IPanoramaReferenceFeatureContext
    {
        Services.Communication.IControlCommandServicePort _controlCommandService { get; }

        ref global::System.Tuple<double, double, long> _videoCenterPose { get; }

        void ApplyZoneReferenceRebase(PanoramaZoneRebase operation);

        void BeginZoneReferenceChange();

        void FinishZoneReferenceChange(bool notifyEditor = true);

        PresetPointOption[] GetPanoramaPresets();

        string HomeZeroStatusText { get; set; }

        bool IsSelectedControlAgentWebAgent { get; }

        System.Collections.ObjectModel.ObservableCollection<PresetPointOption> LaPresetPoints { get; }

        bool PanoramaPoseFresh { get; }

        string PanoramaPresetContext { get; }

        string PanoramaPresetContextHash { get; }

        System.Collections.ObjectModel.ObservableCollection<PresetPointOption> PresetPoints { get; }

        bool SavePresetStorage();

        PresetPointOption SelectedLaPresetPoint { get; set; }

        PresetPointOption SelectedPresetPoint { get; set; }

        string ZoneEquipmentKey { get; }
    }

    /// <summary>파노라마 좌표 기준 담당 객체입니다. 상태를 보관하고 입력을 받아 결과를 알립니다.</summary>
    internal sealed class PanoramaReferenceFeature
    {

        #region [협력 객체와 초기화]

        private readonly IPanoramaReferenceFeatureContext _context;

        internal PanoramaReferenceFeature(IPanoramaReferenceFeatureContext context)
        {
            _context = context ?? throw new System.ArgumentNullException(nameof(context));
        }

        #endregion

        #region [상태 필드]

        internal PanoramaReferenceState _panoramaReference;

        internal string _panoramaReferenceContext;

        #endregion

        #region [화면 속성과 이벤트]

        internal string PanoramaReferencePath
        {
            get
            {
                return Path.Combine(PanoramaPresetRegistration.ConfigDirectory, "reference_" + _context.PanoramaPresetContextHash + ".json");
            }
        }

        internal PanoramaReferenceState PanoramaReference
        {
            get
            {
                string context = _context.PanoramaPresetContext;

                if (_panoramaReference == null || _panoramaReferenceContext != context)
                {
                    _panoramaReferenceContext = context;

                    var saved = PanoramaPresetRegistration.ReadAtomic<PanoramaReferenceState>(PanoramaReferencePath);

                    _panoramaReference = saved ?? new PanoramaReferenceState
                    {
                        Pending = File.Exists(PanoramaReferencePath) || File.Exists(PanoramaReferencePath + ".bak")
                    };

                    if (_panoramaReference.Version != 1 || double.IsNaN(_panoramaReference.PanOffset) || double.IsInfinity(_panoramaReference.PanOffset) || double.IsNaN(_panoramaReference.TiltOffset) || double.IsInfinity(_panoramaReference.TiltOffset))
                    {
                        _panoramaReference = new PanoramaReferenceState
                        {
                            Pending = true
                        };

                        ConsoleLogHelper.Warning("PANORAMA REFERENCE", "좌표 기준 기록 확인 필요 / 연동은 보존");
                    }

                    if (_panoramaReference.Journal == null)
                    {
                        _panoramaReference.Journal = new List<PanoramaPresetRebase>();
                    }
                }

                return _panoramaReference;
            }
        }

        #endregion

        #region [동작 처리]

        internal static string PanoramaPresetDataKey(PresetPointOption point)
        {
            return PanoramaPresetRegistration.ContextIdentity(PanoramaPresetRegistration.PresetIdentity(point) + "|" + point.EoZoomText + "|" + point.EoFocusText + "|" + point.IrZoomText + "|" + point.IrFocusText + "|" + point.PositionSnapshot?.CapturedAt.ToString("o"));
        }

        internal static double SignedPan(double pan)
        {
            return PanoramaPresetRegistration.Wrap(pan + 180) - 180;
        }

        internal static PresetPointOption RebasePanoramaPreset(PresetPointOption point, double pan, double tilt)
        {
            return new PresetPointOption(point.Number, pan, tilt, point.EoZoomText, point.EoFocusText, point.IrZoomText, point.IrFocusText, point.Name, point.SavedOrder, point.PositionSnapshot?.WithReferenceOffset(SignedPan(pan - point.Pan), tilt - point.Tilt))
            {
                UsesLegacyIrZoomCalibration = point.UsesLegacyIrZoomCalibration
            };
        }

        internal void RecoverPanoramaPresetReference()
        {
            try
            {
                RecoverPanoramaPresetReferenceCore();
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
            {
                PanoramaReference.Pending = true;

                ConsoleLogHelper.Warning("PANORAMA REFERENCE", "프리셋 좌표 복원 저장 확인 필요 / 연동 보존: " + ex.Message);
            }
        }

        internal void RecoverPanoramaPresetReferenceCore()
        {
            var reference = PanoramaReference;

            if (reference.Pending)
            {
                return;
            }

            if (reference.Journal.Count == 0 && reference.ZoneRebase == null)
            {
                return;
            }

            var points = _context.IsSelectedControlAgentWebAgent ? _context.PresetPoints : _context.LaPresetPoints;

            foreach (var entry in reference.Journal)
            {
                var point = points.FirstOrDefault(item => item.Number == entry.Number);

                if (point == null || PanoramaPresetDataKey(point) != entry.BeforeKey)
                {
                    continue;
                }

                int index = points.IndexOf(point);

                var rebased = RebasePanoramaPreset(point, entry.Pan, entry.Tilt);

                if (PanoramaPresetDataKey(rebased) != entry.AfterKey)
                {
                    throw new InvalidOperationException("PRESET 좌표 변경 기록 검증 실패");
                }

                points[index] = rebased;

                if (_context.IsSelectedControlAgentWebAgent && _context.SelectedPresetPoint?.Number == point.Number)
                {
                    _context.SelectedPresetPoint = rebased;
                }

                if (!_context.IsSelectedControlAgentWebAgent && _context.SelectedLaPresetPoint?.Number == point.Number)
                {
                    _context.SelectedLaPresetPoint = rebased;
                }
            }

            if (_context.SavePresetStorage())
            {
                if (reference.ZoneRebase != null)
                {
                    _context.ApplyZoneReferenceRebase(reference.ZoneRebase);
                }

                reference.Journal.Clear();

                reference.ZoneRebase = null;

                PanoramaPresetRegistration.WriteAtomic(PanoramaReferencePath, reference);

                _context.FinishZoneReferenceChange(false);
            }
        }

        internal async Task<PanoramaZeroTicket> PreparePanoramaZeroReferenceAsync(bool panAxis)
        {
            string context = _context.PanoramaPresetContext;

            if (PanoramaReference.Pending)
            {
                throw new InvalidOperationException("이전 ZERO 좌표 기준 확인 필요");
            }

            bool positionStopped = _context._controlCommandService.StopPanTiltPositionMove();

            bool continuousStopped = _context._controlCommandService.StopMove();

            if (!positionStopped || !continuousStopped)
            {
                throw new InvalidOperationException("ZERO 전 정지 명령 송신 실패");
            }

            var previous = Volatile.Read(ref _context._videoCenterPose);

            long ticks = previous?.Item3 ?? 0;

            int stable = 0;

            for (int attempt = 0; attempt < 80; attempt++)
            {
                await Task.Delay(100);

                if (context != _context.PanoramaPresetContext)
                {
                    throw new InvalidOperationException("ZERO 준비 중 연결 장비 변경");
                }

                var pose = Volatile.Read(ref _context._videoCenterPose);

                if (pose == null || pose.Item3 <= ticks || !_context.PanoramaPoseFresh)
                {
                    continue;
                }

                ticks = pose.Item3;

                bool still = previous != null && Math.Abs(SignedPan(pose.Item1 - previous.Item1)) <= .02 && Math.Abs(pose.Item2 - previous.Item2) <= .02;

                stable = still ? stable + 1 : 1;

                previous = pose;

                if (stable >= 3)
                {
                    var ticket = BeginPanoramaZeroReference(panAxis);

                    // 대기 중 마지막으로 검증한 수신 묶음을 사용한다.
                    ticket.BeforeValue = panAxis ? pose.Item1 : pose.Item2;

                    ticket.OtherBeforeValue = panAxis ? pose.Item2 : pose.Item1;

                    ticket.PoseTicks = pose.Item3;

                    ConsoleLogHelper.State("PANORAMA ZERO BEFORE", "AXIS=" + (panAxis ? "PAN" : "TILT") + " / PAN=" + pose.Item1.ToString("F3") + " / TILT=" + pose.Item2.ToString("F3"));

                    return ticket;
                }
            }

            throw new InvalidOperationException("ZERO 전 안정된 새 위치 미수신 / ZERO 미송신");
        }

        internal PanoramaZeroTicket BeginPanoramaZeroReference(bool panAxis)
        {
            RecoverPanoramaPresetReference();

            var reference = PanoramaReference;

            if (reference.Pending || reference.Journal.Count > 0)
            {
                throw new InvalidOperationException("이전 ZERO 좌표 기준 확인 필요 / 파노라마 연동 보존");
            }

            var pose = Volatile.Read(ref _context._videoCenterPose);

            var ticket = new PanoramaZeroTicket
            {
                Context = _context.PanoramaPresetContext,
                PanAxis = panAxis,
                BeforeValue = _context.PanoramaPoseFresh && pose != null ? (double?)(panAxis ? pose.Item1 : pose.Item2) : null,
                OtherBeforeValue = pose == null ? 0 : (panAxis ? pose.Item2 : pose.Item1),
                PoseTicks = pose?.Item3 ?? 0
            };

            reference.Pending = true;

            PanoramaPresetRegistration.WriteAtomic(PanoramaReferencePath, reference);

            _context.BeginZoneReferenceChange();

            return ticket;
        }

        internal async Task FinishPanoramaZeroReferenceAsync(PanoramaZeroTicket ticket, bool sent)
        {
            if (ticket.Context != _context.PanoramaPresetContext)
            {
                return;
            }

            var previous = PanoramaReference;

            if (!sent)
            {
                previous.Pending = false;

                PanoramaPresetRegistration.WriteAtomic(PanoramaReferencePath, previous);

                _context.FinishZoneReferenceChange();

                return;
            }

            if (!ticket.BeforeValue.HasValue)
            {
                _context.HomeZeroStatusText = "ZERO SENT / PRESET REFERENCE CHECK REQUIRED";

                ConsoleLogHelper.Warning("PANORAMA REFERENCE", "ZERO 전 위치 미확인 / 연동 보존·이동 기준 확인 필요");

                return;
            }

            int stable = 0;

            long lastTicks = ticket.PoseTicks;

            double after = 0;

            double lastValue = double.NaN;

            for (int attempt = 0; attempt < 50; attempt++)
            {
                if (ticket.Context != _context.PanoramaPresetContext)
                {
                    return;
                }

                var pose = Volatile.Read(ref _context._videoCenterPose);

                if (pose != null && pose.Item3 > lastTicks && _context.PanoramaPoseFresh)
                {
                    lastTicks = pose.Item3;

                    after = ticket.PanAxis ? SignedPan(pose.Item1) : pose.Item2;

                    double other = ticket.PanAxis ? pose.Item2 : pose.Item1;

                    double otherDelta = ticket.PanAxis ? other - ticket.OtherBeforeValue : SignedPan(other - ticket.OtherBeforeValue);

                    stable = Math.Abs(otherDelta) <= .05 && Math.Abs(after) <= .1 ? (double.IsNaN(lastValue) || Math.Abs(after - lastValue) <= .05 ? stable + 1 : 1) : 0;

                    lastValue = after;

                    if (stable >= 3)
                    {
                        break;
                    }
                }

                await Task.Delay(100);
            }

            if (stable < 3)
            {
                _context.HomeZeroStatusText = "ZERO SENT / PRESET REFERENCE CHECK REQUIRED";

                ConsoleLogHelper.Warning("PANORAMA REFERENCE", "ZERO 후 새 수신값 미확인 / 연동 보존·이동 기준 확인 필요");

                return;
            }

            double delta = ticket.PanAxis ? SignedPan(after - ticket.BeforeValue.Value) : after - ticket.BeforeValue.Value;

            var updated = new PanoramaReferenceState
            {
                PanOffset = previous.PanOffset + (ticket.PanAxis ? delta : 0),
                TiltOffset = previous.TiltOffset + (ticket.PanAxis ? 0 : delta),
                Pending = false,
                ZoneRebase = new PanoramaZoneRebase
                {
                    OperationId = Guid.NewGuid().ToString("N"),
                    EquipmentKey = _context.ZoneEquipmentKey,
                    PanDelta = ticket.PanAxis ? delta : 0,
                    TiltDelta = ticket.PanAxis ? 0 : delta
                },
                Journal = previous.Journal.ToList()
            };

            foreach (var point in _context.GetPanoramaPresets())
            {
                double pan = ticket.PanAxis ? SignedPan(point.Pan + delta) : point.Pan;

                double tilt = point.Tilt + (ticket.PanAxis ? 0 : delta);

                var next = RebasePanoramaPreset(point, pan, tilt);

                updated.Journal.Add(new PanoramaPresetRebase { Number = point.Number, BeforeKey = PanoramaPresetDataKey(point), AfterKey = PanoramaPresetDataKey(next), Pan = pan, Tilt = tilt });
            }

            PanoramaPresetRegistration.WriteAtomic(PanoramaReferencePath, updated);

            _panoramaReference = updated;

            RecoverPanoramaPresetReference();

            _context.HomeZeroStatusText = "ZERO APPLIED / PRESET REFERENCE UPDATED";

            ConsoleLogHelper.State("PANORAMA REFERENCE", (ticket.PanAxis ? "PAN" : "TILT") + " ZERO 좌표 갱신 / BEFORE=" + ticket.BeforeValue.Value.ToString("F3") + " / AFTER=" + after.ToString("F3") + " / DELTA=" + delta.ToString("F3") + " / PAN_OFFSET=" + updated.PanOffset.ToString("F3") + " / TILT_OFFSET=" + updated.TiltOffset.ToString("F3") + " / 이미지·연동 번호 유지");
        }

        #endregion

    }
}
