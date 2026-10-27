using OpenCvWpfTracking.Common;
using OpenCvWpfTracking.Models.Main;
using OpenCvWpfTracking.Services.Video;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace OpenCvWpfTracking.ViewModels.Main
{
    public partial class MainViewModel
    {
        private PanoramaReferenceState _panoramaReference;

        private string _panoramaReferenceContext;

        private sealed class PanoramaZeroTicket
        {
            public string Context;

            public bool PanAxis;

            public double? BeforeValue;

            public long PoseTicks;

            public double OtherBeforeValue;
        }

        private string PanoramaReferencePath => Path.Combine(PanoramaPresetRegistration.ConfigDirectory,
            "reference_" + PanoramaPresetRegistration.ContextIdentity(PanoramaPresetContext) + ".json");

        /// <summary>같은 장비 재연결은 같은 변환 기록을 사용하며 이전 사진과 연동 번호를 지우지 않는다.</summary>
        public PanoramaReferenceState PanoramaReference
        {
            get
            {
                string context = PanoramaPresetContext;
                if (_panoramaReference == null || _panoramaReferenceContext != context)
                {
                    _panoramaReferenceContext = context;
                    var saved = PanoramaPresetRegistration.ReadAtomic<PanoramaReferenceState>(PanoramaReferencePath);
                    _panoramaReference = saved ?? new PanoramaReferenceState
                    {
                        Pending = File.Exists(PanoramaReferencePath) || File.Exists(PanoramaReferencePath + ".bak")
                    };
                    if (_panoramaReference.Version != 1 || double.IsNaN(_panoramaReference.PanOffset) ||
                        double.IsInfinity(_panoramaReference.PanOffset) || double.IsNaN(_panoramaReference.TiltOffset) ||
                        double.IsInfinity(_panoramaReference.TiltOffset))
                    {
                        _panoramaReference = new PanoramaReferenceState { Pending = true };
                        ConsoleLogHelper.Warning("PANORAMA REFERENCE", "좌표 기준 기록 확인 필요 / 연동은 보존");
                    }
                    if (_panoramaReference.Journal == null)
                        _panoramaReference.Journal = new List<PanoramaPresetRebase>();
                }
                return _panoramaReference;
            }
        }

        /// <summary>재등록한 같은 번호를 과거 영점 변경 기록으로 다시 보정하지 않도록 원본 시각도 확인한다.</summary>
        private static string PanoramaPresetDataKey(PresetPointOption point)
        {
            return PanoramaPresetRegistration.ContextIdentity(PanoramaPresetRegistration.PresetIdentity(point) + "|" +
                point.EoZoomText + "|" + point.EoFocusText + "|" + point.IrZoomText + "|" + point.IrFocusText + "|" +
                point.PositionSnapshot?.CapturedAt.ToString("o"));
        }

        private static double SignedPan(double pan) => PanoramaPresetRegistration.Wrap(pan + 180) - 180;

        private static PresetPointOption RebasePanoramaPreset(PresetPointOption point, double pan, double tilt)
        {
            return new PresetPointOption(point.Number, pan, tilt, point.EoZoomText, point.EoFocusText,
                point.IrZoomText, point.IrFocusText, point.Name, point.SavedOrder,
                point.PositionSnapshot?.WithReferenceOffset(SignedPan(pan - point.Pan), tilt - point.Tilt)) { UsesLegacyIrZoomCalibration = point.UsesLegacyIrZoomCalibration };
        }

        /// <summary>Config의 변경 기록을 먼저 확정하므로 CSV 저장 중 종료되어도 같은 보정을 한 번만 적용한다.</summary>
        public void RecoverPanoramaPresetReference()
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

        private void RecoverPanoramaPresetReferenceCore()
        {
            var reference = PanoramaReference;
            if (reference.Pending)
                return;
            if (reference.Journal.Count == 0 && reference.ZoneRebase == null)
                return;
            var points = IsSelectedControlAgentWebAgent ? PresetPoints : LaPresetPoints;
            foreach (var entry in reference.Journal)
            {
                var point = points.FirstOrDefault(item => item.Number == entry.Number);
                if (point == null || PanoramaPresetDataKey(point) != entry.BeforeKey)
                    continue;
                int index = points.IndexOf(point);
                var rebased = RebasePanoramaPreset(point, entry.Pan, entry.Tilt);
                if (PanoramaPresetDataKey(rebased) != entry.AfterKey)
                    throw new InvalidOperationException("PRESET 좌표 변경 기록 검증 실패");
                points[index] = rebased;
                if (IsSelectedControlAgentWebAgent && SelectedPresetPoint?.Number == point.Number)
                    SelectedPresetPoint = rebased;
                if (!IsSelectedControlAgentWebAgent && SelectedLaPresetPoint?.Number == point.Number)
                    SelectedLaPresetPoint = rebased;
            }
            if (SavePresetStorage())
            {
                if (reference.ZoneRebase != null)
                    ApplyZoneReferenceRebase(reference.ZoneRebase);
                reference.Journal.Clear();
                reference.ZoneRebase = null;
                PanoramaPresetRegistration.WriteAtomic(PanoramaReferencePath, reference);
                FinishZoneReferenceChange(false);
            }
        }

        /// <summary>정지 후 새 위치 세 개가 안정된 경우에만 ZERO 직전 기준을 확보한다.</summary>
        private async Task<PanoramaZeroTicket> PreparePanoramaZeroReferenceAsync(bool panAxis)
        {
            string context = PanoramaPresetContext;
            if (PanoramaReference.Pending)
                throw new InvalidOperationException("이전 ZERO 좌표 기준 확인 필요");

            bool positionStopped = _controlCommandService.StopPanTiltPositionMove();
            bool continuousStopped = _controlCommandService.StopMove();
            if (!positionStopped || !continuousStopped)
                throw new InvalidOperationException("ZERO 전 정지 명령 송신 실패");

            var previous = Volatile.Read(ref _videoCenterPose);
            long ticks = previous?.Item3 ?? 0;
            int stable = 0;
            for (int attempt = 0; attempt < 80; attempt++)
            {
                await Task.Delay(100);
                if (context != PanoramaPresetContext)
                    throw new InvalidOperationException("ZERO 준비 중 연결 장비 변경");
                var pose = Volatile.Read(ref _videoCenterPose);
                if (pose == null || pose.Item3 <= ticks || !PanoramaPoseFresh)
                    continue;
                ticks = pose.Item3;
                bool still = previous != null && Math.Abs(SignedPan(pose.Item1 - previous.Item1)) <= .02 &&
                    Math.Abs(pose.Item2 - previous.Item2) <= .02;
                stable = still ? stable + 1 : 1;
                previous = pose;
                if (stable >= 3)
                {
                    var ticket = BeginPanoramaZeroReference(panAxis);
                    // 대기 중 마지막으로 검증한 수신 묶음을 사용한다.
                    ticket.BeforeValue = panAxis ? pose.Item1 : pose.Item2;
                    ticket.OtherBeforeValue = panAxis ? pose.Item2 : pose.Item1;
                    ticket.PoseTicks = pose.Item3;
                    ConsoleLogHelper.State("PANORAMA ZERO BEFORE", "AXIS=" + (panAxis ? "PAN" : "TILT") +
                        " / PAN=" + pose.Item1.ToString("F3") + " / TILT=" + pose.Item2.ToString("F3"));
                    return ticket;
                }
            }
            throw new InvalidOperationException("ZERO 전 안정된 새 위치 미수신 / ZERO 미송신");
        }

        /// <summary>ZERO 송신 전에 미확정 상태를 저장해 확인 없이 이전 좌표로 이동하지 않도록 한다.</summary>
        private PanoramaZeroTicket BeginPanoramaZeroReference(bool panAxis)
        {
            RecoverPanoramaPresetReference();
            var reference = PanoramaReference;
            if (reference.Pending || reference.Journal.Count > 0)
                throw new InvalidOperationException("이전 ZERO 좌표 기준 확인 필요 / 파노라마 연동 보존");
            var pose = Volatile.Read(ref _videoCenterPose);
            var ticket = new PanoramaZeroTicket
            {
                Context = PanoramaPresetContext,
                PanAxis = panAxis,
                BeforeValue = PanoramaPoseFresh && pose != null ? (double?)(panAxis ? pose.Item1 : pose.Item2) : null,
                OtherBeforeValue = pose == null ? 0 : (panAxis ? pose.Item2 : pose.Item1),
                PoseTicks = pose?.Item3 ?? 0
            };
            reference.Pending = true;
            PanoramaPresetRegistration.WriteAtomic(PanoramaReferencePath, reference);
            BeginZoneReferenceChange();
            return ticket;
        }

        /// <summary>새 수신값으로 ZERO 적용을 확인한 경우에만 좌표 변환과 기존 프리셋 스냅샷을 함께 갱신한다.</summary>
        private async Task FinishPanoramaZeroReferenceAsync(PanoramaZeroTicket ticket, bool sent)
        {
            if (ticket.Context != PanoramaPresetContext)
                return;
            var previous = PanoramaReference;
            if (!sent)
            {
                previous.Pending = false;
                PanoramaPresetRegistration.WriteAtomic(PanoramaReferencePath, previous);
                FinishZoneReferenceChange();
                return;
            }
            if (!ticket.BeforeValue.HasValue)
            {
                HomeZeroStatusText = "ZERO SENT / PRESET REFERENCE CHECK REQUIRED";
                ConsoleLogHelper.Warning("PANORAMA REFERENCE", "ZERO 전 위치 미확인 / 연동 보존·이동 기준 확인 필요");
                return;
            }
            int stable = 0;
            long lastTicks = ticket.PoseTicks;
            double after = 0;
            double lastValue = double.NaN;
            for (int attempt = 0; attempt < 50; attempt++)
            {
                if (ticket.Context != PanoramaPresetContext)
                    return;
                var pose = Volatile.Read(ref _videoCenterPose);
                if (pose != null && pose.Item3 > lastTicks && PanoramaPoseFresh)
                {
                    lastTicks = pose.Item3;
                    after = ticket.PanAxis ? SignedPan(pose.Item1) : pose.Item2;
                    double other = ticket.PanAxis ? pose.Item2 : pose.Item1;
                    double otherDelta = ticket.PanAxis ? other - ticket.OtherBeforeValue : SignedPan(other - ticket.OtherBeforeValue);
                    stable = Math.Abs(otherDelta) <= .05 && Math.Abs(after) <= .1
                        ? (double.IsNaN(lastValue) || Math.Abs(after - lastValue) <= .05 ? stable + 1 : 1) : 0;
                    lastValue = after;
                    if (stable >= 3)
                        break;
                }
                await Task.Delay(100);
            }
            if (stable < 3)
            {
                HomeZeroStatusText = "ZERO SENT / PRESET REFERENCE CHECK REQUIRED";
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
                    EquipmentKey = ZoneEquipmentKey,
                    PanDelta = ticket.PanAxis ? delta : 0,
                    TiltDelta = ticket.PanAxis ? 0 : delta
                },
                Journal = previous.Journal.ToList()
            };
            foreach (var point in GetPanoramaPresets())
            {
                double pan = ticket.PanAxis ? SignedPan(point.Pan + delta) : point.Pan;
                double tilt = point.Tilt + (ticket.PanAxis ? 0 : delta);
                var next = RebasePanoramaPreset(point, pan, tilt);
                updated.Journal.Add(new PanoramaPresetRebase
                {
                    Number = point.Number,
                    BeforeKey = PanoramaPresetDataKey(point),
                    AfterKey = PanoramaPresetDataKey(next),
                    Pan = pan,
                    Tilt = tilt
                });
            }
            PanoramaPresetRegistration.WriteAtomic(PanoramaReferencePath, updated);
            _panoramaReference = updated;
            RecoverPanoramaPresetReference();
            HomeZeroStatusText = "ZERO APPLIED / PRESET REFERENCE UPDATED";
            ConsoleLogHelper.State("PANORAMA REFERENCE", (ticket.PanAxis ? "PAN" : "TILT") +
                " ZERO 좌표 갱신 / BEFORE=" + ticket.BeforeValue.Value.ToString("F3") +
                " / AFTER=" + after.ToString("F3") + " / DELTA=" + delta.ToString("F3") +
                " / PAN_OFFSET=" + updated.PanOffset.ToString("F3") + " / TILT_OFFSET=" + updated.TiltOffset.ToString("F3") +
                " / 이미지·연동 번호 유지");
        }
    }
}
