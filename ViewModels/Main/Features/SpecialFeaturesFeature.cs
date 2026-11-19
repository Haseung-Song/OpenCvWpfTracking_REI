using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using OpenCvWpfTracking.Common;
using OpenCvWpfTracking.Models.Main;
using OpenCvWpfTracking.Services.Communication.WebAgent;

namespace OpenCvWpfTracking.ViewModels.Main
{
    /// <summary>카메라 특수 기능 표시에 필요한 협력 기능만 받는 경계입니다. 화면 객체의 내부 구현을 알지 않습니다.</summary>
    internal interface ISpecialFeaturesFeatureContext
    {
        string ConnectionSessionId { get; set; }

        Services.Communication.IControlCommandServicePort ControlCommandService { get; }

        IWebAgentThermalPaletteServicePort WebAgentThermalPaletteService { get; }

        bool CameraFeatureTransportReady { get; }

        string ControlAgentIp { get; set; }

        string ControlAgentPortText { get; set; }

        bool IsControlInputLocked { get; }

        bool IsOperationCommandEnabled { get; }

        bool IsSelectedConnectionTarget { get; }

        bool IsSelectedControlAgentWebAgent { get; }

        /// <summary>
        /// 바인딩 속성 변경 알림
        /// </summary>
        void OnPropertyChanged([global::System.Runtime.CompilerServices.CallerMemberName] string propertyName = null);

        /// <summary>
        /// 2026-08-27: 카메라 이동 또는 영상 조건 변경 뒤 EO/IR 기준 프레임과 시간축 후보를 함께 초기화하여 화면 전체 변화가 이벤트가
        /// 되지 않게 한다.
        /// </summary>
        void ResetFireSmokeFrameAnalysis(string reason);

        ControlAgentProfileOption SelectedControlAgentProfile { get; set; }

        /// <summary>
        /// 운용 잠금 검증 후 BOOLEAN 명령을 송신한다. 송신 성공을 장비 적용 확인으로 취급하지 않는다.
        /// </summary>
        bool SetDigitalZoom(bool ir, bool on);

        /// <summary>
        /// 현재 열상 팔레트에 맞춰 선택 버튼의 강조 상태를 갱신한다.
        /// </summary>
        void UpdateThermalPaletteButtonVisual(int paletteType);

        Action QuerySpecialFeatureSupportCallback { get; }

        Action<CameraSpecialFeatureRow, int> SendSpecialFeatureCallback { get; }

        Action<CameraSpecialFeatureRow> QuerySpecialFeatureStateCallback { get; }
    }

    /// <summary>카메라 특수 기능 표시 담당 객체입니다. 상태를 보관하고 입력을 받아 결과를 알립니다.</summary>
    internal sealed class SpecialFeaturesFeature
    {

        #region [협력 객체와 초기화]

        private readonly ISpecialFeaturesFeatureContext _context;

        /// <summary>
        /// 카메라 특수 기능 기능에서 사용할 협력 객체를 연결하고 초기 상태를 준비한다.
        /// </summary>
        internal SpecialFeaturesFeature(ISpecialFeaturesFeatureContext context)
        {
            _context = context ?? throw new System.ArgumentNullException(nameof(context));
        }

        #endregion

        #region [상태 필드]

        internal ObservableCollection<CameraSpecialFeatureRow> _eoSpecialFeatures;

        internal ObservableCollection<CameraSpecialFeatureRow> _irSpecialFeatures;

        internal readonly Queue<CameraSpecialFeatureRow> _specialReadQueue = new Queue<CameraSpecialFeatureRow>();

        internal ICommand _refreshSpecialFeaturesCommand;

        internal int _specialFeatureEpoch;

        internal bool _specialWasReady;

        internal string _specialTargetKey;

        internal long _lastSpecialCapabilityQuery;

        internal byte _specialCapabilityReceived;

        internal long _specialCapabilityDeadline;

        internal string _specialFeatureSummary;

        #endregion

        #region [화면 속성과 이벤트]

        internal ObservableCollection<CameraSpecialFeatureRow> EoSpecialFeatures
        {
            get
            {
                EnsureSpecialFeatures();

                return _eoSpecialFeatures;
            }
        }

        internal ObservableCollection<CameraSpecialFeatureRow> IrSpecialFeatures
        {
            get
            {
                EnsureSpecialFeatures();

                return _irSpecialFeatures;
            }
        }

        internal bool SpecialFeatureReady
        {
            get
            {
                return _context.CameraFeatureTransportReady && _context.IsSelectedConnectionTarget;
            }
        }

        internal string SpecialFeatureAgentName
        {
            get
            {
                if (_context.IsSelectedControlAgentWebAgent)
                {
                    return "WEB AGENT";
                }
                else
                {
                    return "LA AGENT";
                }
            }
        }

        internal string SpecialFeatureTargetKey
        {
            get
            {
                return _context.ConnectionSessionId + "|" + _context.SelectedControlAgentProfile.DisplayName + "|" + _context.ControlAgentIp + "|" + _context.ControlAgentPortText;
            }
        }

        internal bool CanRefreshSpecialFeatures
        {
            get
            {
                return SpecialFeatureReady && DateTime.UtcNow.Ticks - _lastSpecialCapabilityQuery >= TimeSpan.FromSeconds(2).Ticks;
            }
        }

        internal string SpecialFeatureSummary
        {
            get
            {
                return SpecialFeatureAgentName + " · " + (!SpecialFeatureReady ? "선택 장비 연결 후 지원 조회가 가능합니다." : _specialFeatureSummary ?? "지원 미조회 · 조회 후 사용하세요.");
            }
        }

        internal ICommand RefreshSpecialFeaturesCommand
        {
            get
            {
                return _refreshSpecialFeaturesCommand ?? (_refreshSpecialFeaturesCommand = new RelayCommand(_context.QuerySpecialFeatureSupportCallback, () => CanRefreshSpecialFeatures));
            }
        }

        internal IEnumerable<CameraSpecialFeatureRow> SpecialFeatureRows
        {
            get
            {
                return _eoSpecialFeatures.Concat(_irSpecialFeatures);
            }
        }

        #endregion

        #region [동작 처리]

        /// <summary>
        /// 2026-10-14 규격 대상별 행을 한 번 생성한다. 기존 LENS 제어는 이동하거나 제거하지 않는다.
        /// </summary>
        internal void EnsureSpecialFeatures()
        {
            if (_eoSpecialFeatures != null)
            {
                return;
            }

            _eoSpecialFeatures = new ObservableCollection<CameraSpecialFeatureRow>();

            _irSpecialFeatures = new ObservableCollection<CameraSpecialFeatureRow>();

            foreach (var definition in CameraFeatureDefinition.All)
            {
                for (byte target = 0; target < 2; target++)
                {
                    if ((definition.Targets & (1 << target)) != 0)
                    {
                        var row = new CameraSpecialFeatureRow(definition, target, () => SpecialFeatureReady, () => _context.IsOperationCommandEnabled && !_context.IsControlInputLocked, _context.SendSpecialFeatureCallback, _context.QuerySpecialFeatureStateCallback, () => SpecialFeatureAgentName + " 연결 필요", () => _context.IsSelectedControlAgentWebAgent);

                        (target == 0 ? _eoSpecialFeatures : _irSpecialFeatures).Add(row);
                    }
                }
            }
        }

        /// <summary>
        /// 연결/모드 변경 시 이전 장비 capability와 지연된 UI 응답의 적용을 무효화한다.
        /// </summary>
        internal void ResetSpecialFeatureSession()
        {
            ++_specialFeatureEpoch;

            _specialReadQueue.Clear();

            _lastSpecialCapabilityQuery = 0;
            _specialFeatureSummary = null;
            _specialCapabilityReceived = 0;
            _specialCapabilityDeadline = 0;

            if (_eoSpecialFeatures != null)
            {
                foreach (var row in SpecialFeatureRows)
                {
                    row.Reset();
                }
            }

            _context.OnPropertyChanged(nameof(SpecialFeatureSummary));

            _context.OnPropertyChanged(nameof(CanRefreshSpecialFeatures));
        }

        /// <summary>
        /// EO와 IR 기능 지원 조회를 요청하고 응답 기한을 기록한다.
        /// </summary>
        internal void QuerySpecialFeatureSupport()
        {
            EnsureSpecialFeatures();

            if (!CanRefreshSpecialFeatures)
            {
                return;
            }

            _lastSpecialCapabilityQuery = DateTime.UtcNow.Ticks;
            _specialCapabilityReceived = 0;
            _specialCapabilityDeadline = DateTime.UtcNow.AddSeconds(3).Ticks;

            bool eo = _context.ControlCommandService.RequestCameraFeatureCapability(0), ir = _context.ControlCommandService.RequestCameraFeatureCapability(1);

            _specialFeatureSummary = eo && ir ? "EO / IR 지원 조회 전송 · 응답 대기" : "지원 조회 전송 실패 · 연결 확인";

            _context.OnPropertyChanged(nameof(SpecialFeatureSummary));

            ConsoleLogHelper.Command("SPECIAL FEATURE", "CAPABILITY / EO_TX=" + eo + " / IR_TX=" + ir);
        }

        /// <summary>
        /// UI 큐에 넣기 전에 세션을 캡처하고 연결 변경 후 도착한 callback을 버린다.
        /// </summary>
        internal void DispatchSpecialFeaturePacket(byte[] p, Action<byte[]> apply)
        {
            int epoch = _specialFeatureEpoch;

            string session = _context.ConnectionSessionId, target = SpecialFeatureTargetKey;

            var copy = (byte[])p.Clone();

            Application.Current?.Dispatcher.BeginInvoke(new Action(() =>
            {
                if (epoch == _specialFeatureEpoch && session == _context.ConnectionSessionId && target == SpecialFeatureTargetKey && SpecialFeatureReady)
                {
                    apply(copy);
                }
            }));
        }

        /// <summary>
        /// 현재 연결 세션의 지원 응답을 반영하고 필요한 상태 조회를 예약한다.
        /// </summary>
        internal void ApplySpecialFeatureCapability(byte[] p)
        {
            byte target;

            List<CameraFeatureCapability> entries;

            if (!CameraFeatureProtocol.TryCapability(p, out target, out entries))
            {
                return;
            }

            // NEXT·PREV의 번호 순회만 기존 지원 마스크를 사용한다. 색상 직접 선택과 분리한다.
            var paletteCapability = entries.FirstOrDefault(entry => entry.Id == 0x05);

            if (target == 1 && paletteCapability != null)
            {
                _context.WebAgentThermalPaletteService.ApplyPaletteCapability(paletteCapability.OptionMask);
            }

            EnsureSpecialFeatures();

            _specialCapabilityReceived |= (byte)(1 << target);

            foreach (var row in SpecialFeatureRows.Where(r => r.Target == target))
            {
                row.SetCapability(entries.FirstOrDefault(c => c.Id == row.Id));

                if (!row.IsSemanticPalette && row.Capability != null && (row.Capability.Flags & 3) == 3 && !row.IsTrigger && row.Id != 0x1D && !_specialReadQueue.Contains(row))
                {
                    _specialReadQueue.Enqueue(row);
                }
            }

            _specialFeatureSummary = "지원 EO " + ((_specialCapabilityReceived & 1) != 0 ? _eoSpecialFeatures.Count(r => r.Capability != null && (r.Capability.Flags & 1) != 0).ToString() : "미응답") + " / IR " + ((_specialCapabilityReceived & 2) != 0 ? _irSpecialFeatures.Count(r => r.Capability != null && (r.Capability.Flags & 1) != 0).ToString() : "미응답") + " · 값은 장비 응답 기준";

            _context.OnPropertyChanged(nameof(SpecialFeatureSummary));

            CommandManager.InvalidateRequerySuggested();
        }

        /// <summary>
        /// 검증한 기능 상태 응답을 해당 채널의 행에 반영한다.
        /// </summary>
        internal void ApplySpecialFeatureState(byte[] p)
        {
            byte target, id;

            bool supported, valid;

            short value;

            if (!CameraFeatureProtocol.TryState(p, out target, out id, out supported, out valid, out value))
            {
                return;
            }

            EnsureSpecialFeatures();

            var row = SpecialFeatureRows.FirstOrDefault(r => r.Target == target && r.Id == id);

            row?.SetState(supported, valid, value, BitConverter.ToUInt64(p, 10));

            if (target == 1 && id == 0x05 && supported && valid)
            {
                _context.WebAgentThermalPaletteService.ApplyReportedPalette(value);
            }

            CommandManager.InvalidateRequerySuggested();
        }

        /// <summary>
        /// 기능 미지원 또는 실행 거절 응답을 해당 행에 반영한다.
        /// </summary>
        internal void ApplySpecialFeatureUnavailable(byte[] p)
        {
            if (p.Length < 6 || p[0] != 1 || p[1] > 1 || p[4] > 1 || (p[3] != 0xE9 && p[3] != 0xE7))
            {
                return;
            }

            EnsureSpecialFeatures();

            byte id = p[3] == 0xE9 ? p[2] : p[5];

            var row = SpecialFeatureRows.FirstOrDefault(r => r.Target == p[4] && r.Id == id);

            row?.Reject(p[1] == 0);

            CommandManager.InvalidateRequerySuggested();
        }

        /// <summary>
        /// supported/settable·지원값·운용 잠금을 재검증한 단발 E9만 전송한다.
        /// </summary>
        internal void SendSpecialFeature(CameraSpecialFeatureRow row, int value)
        {
            if (!SpecialFeatureReady || !_context.IsOperationCommandEnabled || _context.IsControlInputLocked || !row.CanEdit || !row.Allows(value))
            {
                return;
            }

            // 지원 장비의 Defog/WDR 상호 배타 조건을 임의의 자동 OFF 명령으로 해결하지 않는다.
            var peers = row.Target == 0 ? _eoSpecialFeatures : _irSpecialFeatures;

            if ((row.Id == 0x01 && value == 1 && peers.Any(r => r.Id == 0x10 && r.HasFreshValue(3))) || (row.Id == 0x10 && value == 3 && peers.Any(r => r.Id == 0x01 && r.HasFreshValue(1))))
            {
                row.Warn("Defog / WDR 동시 사용 불가 · 기존 기능 OFF 후 적용");

                return;
            }

            if (row.IsSemanticPalette)
            {
                bool result = value == 0 ? _context.WebAgentThermalPaletteService.SelectWhiteHot() : value == 1 ? _context.WebAgentThermalPaletteService.SelectBlackHot() : _context.WebAgentThermalPaletteService.SelectRainbow();

                row.MarkSent(value, result);

                if (result)
                {
                    _context.UpdateThermalPaletteButtonVisual(value == 0 ? 1 : value == 1 ? 0 : 2);

                    _context.ResetFireSmokeFrameAnalysis("WEB 팔레트 요청");
                }

                ConsoleLogHelper.Command("WEB 팔레트", "특수기능 의미 선택 / 선택=" + value + " / 전송=" + result);

                return;
            }

            bool sent = row.Id == 0x1D ? _context.SetDigitalZoom(row.Target == 1, value == 1) : _context.ControlCommandService.SetCameraFeature(row.Id, row.Target, (byte)value);

            row.MarkSent(value, sent);

            if (sent && !row.IsTrigger && row.Capability != null && (row.Capability.Flags & 2) != 0)
            {
                var rest = _specialReadQueue.Where(r => r != row).ToArray();

                _specialReadQueue.Clear();

                _specialReadQueue.Enqueue(row);

                foreach (var queued in rest)
                {
                    _specialReadQueue.Enqueue(queued);
                }
            }

            ConsoleLogHelper.Command("SPECIAL FEATURE", "TARGET=" + row.Target + " / ID=0x" + row.Id.ToString("X2") + " / VALUE=" + value + " / TX=" + sent + " / not device ACK");

            CommandManager.InvalidateRequerySuggested();
        }

        /// <summary>
        /// 해당 행의 조회 가능 조건을 확인한 후 상태를 요청한다.
        /// </summary>
        internal void QuerySpecialFeatureState(CameraSpecialFeatureRow row)
        {
            if (!row.CanQuery)
            {
                return;
            }

            row.QuerySent(_context.ControlCommandService.RequestCameraFeatureState(row.Target, row.Id));
        }

        /// <summary>
        /// 기존 250ms UI 타이머를 사용한다. 일괄 조회도 한 틱당 한 건으로 제한한다.
        /// </summary>
        internal void TickSpecialFeatures()
        {
            bool ready = SpecialFeatureReady;

            string targetKey = SpecialFeatureTargetKey;

            if (ready != _specialWasReady || targetKey != _specialTargetKey)
            {
                _specialWasReady = ready;
                _specialTargetKey = targetKey;

                ResetSpecialFeatureSession();

                if (ready)
                {
                    QuerySpecialFeatureSupport();
                }
            }

            if (_eoSpecialFeatures == null)
            {
                return;
            }

            if (_specialCapabilityDeadline != 0 && DateTime.UtcNow.Ticks >= _specialCapabilityDeadline)
            {
                _specialCapabilityDeadline = 0;

                if (_specialCapabilityReceived != 3)
                {
                    _specialFeatureSummary = "지원 응답 없음: " + ((_specialCapabilityReceived & 1) == 0 ? "EO " : "") + ((_specialCapabilityReceived & 2) == 0 ? "IR" : "") + " · 지원 조회로 재시도";

                    foreach (var row in SpecialFeatureRows.Where(r => (_specialCapabilityReceived & (1 << r.Target)) == 0 && !r.CapabilityKnown))
                    {
                        row.Warn("지원 응답 없음 · 제어 잠금");
                    }

                    _context.OnPropertyChanged(nameof(SpecialFeatureSummary));
                }
            }

            bool changed = false;

            foreach (var row in SpecialFeatureRows)
            {
                changed |= row.Refresh();
            }

            if (ready && _specialReadQueue.Count > 0)
            {
                var row = _specialReadQueue.Peek();

                if (row.CanQuery)
                {
                    _specialReadQueue.Dequeue();

                    QuerySpecialFeatureState(row);
                }
                else if (row.Capability == null || (row.Capability.Flags & 3) != 3)
                {
                    _specialReadQueue.Dequeue();
                }
            }

            _context.OnPropertyChanged(nameof(CanRefreshSpecialFeatures));

            if (changed)
            {
                CommandManager.InvalidateRequerySuggested();
            }
        }

        #endregion

    }
}
