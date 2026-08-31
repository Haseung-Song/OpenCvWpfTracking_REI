using OpenCvSharp;
using OpenCvWpfTracking.Common;
using OpenCvWpfTracking.Models.AI;
using OpenCvWpfTracking.Services.Video;
using System;
using System.Collections.Generic;
using System.Windows.Input;
using System.Windows.Media;

namespace OpenCvWpfTracking.ViewModels.Main
{
    public partial class MainViewModel
    {
        private readonly SmokeDetectionService _eoSmokeDetectionService =
            new SmokeDetectionService();
        private readonly SmokeDetectionService _irSmokeDetectionService =
            new SmokeDetectionService();
        private bool _wasMotionCompensatedSmokeAnalysis;
        private readonly object _aiSmokeCandidateSync = new object();
        private readonly List<Rect> _latestEoAiSmokeCandidates = new List<Rect>();
        private readonly List<Rect> _latestIrAiSmokeCandidates = new List<Rect>();
        private DateTime _latestEoAiSmokeCandidateTime = DateTime.MinValue;
        private DateTime _latestIrAiSmokeCandidateTime = DateTime.MinValue;
        private bool _isSmokeDetectionEnabled;
        private int _smokeDetectionSourceIndex;
        // 2026-08-31: 실운용 초기 오탐 억제를 위해 BALANCED를 기본값으로 사용한다.
        // 필요할 때만 UI에서 SENSITIVE 또는 STRICT로 변경한다.
        private int _smokeSensitivityIndex = 1;
        private double _smokeMinimumAreaRatio = 0.0015;
        private double _smokeChangeThresholdRatio = 0.035;
        // 2026-08-31: 1=전체 연기 단일 BBox, 2=연기 기둥별 BBox(기본값).
        private int _smokeBoxGroupingMode = 2;
        private Brush _smokeBoxMode1Background =
            new SolidColorBrush(Color.FromRgb(62, 81, 94));
        private Brush _smokeBoxMode2Background =
            new SolidColorBrush(Color.FromRgb(42, 111, 151));

        public ICommand SelectSmokeBoxMode1Command { get; private set; }

        public ICommand SelectSmokeBoxMode2Command { get; private set; }

        public int SmokeBoxGroupingMode => _smokeBoxGroupingMode;

        public Brush SmokeBoxMode1Background
        {
            get => _smokeBoxMode1Background;
            private set
            {
                _smokeBoxMode1Background = value;
                OnPropertyChanged();
            }
        }

        public Brush SmokeBoxMode2Background
        {
            get => _smokeBoxMode2Background;
            private set
            {
                _smokeBoxMode2Background = value;
                OnPropertyChanged();
            }
        }

        private void InitializeSmokeFeatures()
        {
            SelectSmokeBoxMode1Command =
                new RelayCommand(() => SetSmokeBoxGroupingMode(1));
            SelectSmokeBoxMode2Command =
                new RelayCommand(() => SetSmokeBoxGroupingMode(2));
        }

        private void SetSmokeBoxGroupingMode(int mode)
        {
            _smokeBoxGroupingMode = mode == 1 ? 1 : 2;
            SmokeBoxMode1Background = new SolidColorBrush(
                _smokeBoxGroupingMode == 1
                    ? Color.FromRgb(42, 111, 151)
                    : Color.FromRgb(62, 81, 94));
            SmokeBoxMode2Background = new SolidColorBrush(
                _smokeBoxGroupingMode == 2
                    ? Color.FromRgb(42, 111, 151)
                    : Color.FromRgb(62, 81, 94));
            OnPropertyChanged(nameof(SmokeBoxGroupingMode));
            ConsoleLogHelper.State(
                "SMOKE DETECTOR",
                "BBox grouping mode changed / MODE=" + _smokeBoxGroupingMode);
        }

        // 2026-08-31: AI가 확인한 SMOKE 영역을 영상처리 중복 억제용으로만 보관한다.
        private void UpdateAiSmokeCandidateSnapshot(AiDetectionResult result, DateTime receiveTime)
        {
            if (result == null || result.Boxes == null ||
                (result.RtspIndex != 0 && result.RtspIndex != 1))
            {
                return;
            }

            List<Rect> snapshot = new List<Rect>();
            foreach (AiDetectionBox box in result.Boxes)
            {
                if (box.NormalizedConfidence < AiDisplayConfidenceThreshold ||
                    box.ClassName.IndexOf("smoke", StringComparison.OrdinalIgnoreCase) < 0 ||
                    box.Width <= 0 || box.Height <= 0)
                {
                    continue;
                }

                snapshot.Add(new Rect(box.Left, box.Top, box.Width, box.Height));
            }

            lock (_aiSmokeCandidateSync)
            {
                List<Rect> target = result.RtspIndex == 0
                    ? _latestEoAiSmokeCandidates
                    : _latestIrAiSmokeCandidates;
                target.Clear();
                target.AddRange(snapshot);
                if (result.RtspIndex == 0)
                {
                    _latestEoAiSmokeCandidateTime = receiveTime;
                }
                else
                {
                    _latestIrAiSmokeCandidateTime = receiveTime;
                }
            }
        }

        private IList<Rect> GetRecentAiSmokeCandidates(bool isInfrared)
        {
            lock (_aiSmokeCandidateSync)
            {
                DateTime timestamp = isInfrared
                    ? _latestIrAiSmokeCandidateTime
                    : _latestEoAiSmokeCandidateTime;
                if ((DateTime.Now - timestamp).TotalSeconds > 2.0)
                {
                    return new List<Rect>();
                }

                return new List<Rect>(isInfrared
                    ? _latestIrAiSmokeCandidates
                    : _latestEoAiSmokeCandidates);
            }
        }

        private void ClearAiSmokeCandidateSnapshots()
        {
            lock (_aiSmokeCandidateSync)
            {
                _latestEoAiSmokeCandidates.Clear();
                _latestIrAiSmokeCandidates.Clear();
                _latestEoAiSmokeCandidateTime = DateTime.MinValue;
                _latestIrAiSmokeCandidateTime = DateTime.MinValue;
            }
        }

        private void SetFireSmokeDetectorsForAiConnection(bool enabled, string reason)
        {
            TryRunAiUiAction(() =>
            {
                IsThermalFireDetectionEnabled = enabled;
                IsSmokeDetectionEnabled = enabled;
                if (!enabled)
                {
                    ResetFireSmokeFrameAnalysis(reason);
                }
            }, "FIRE/SMOKE detector connection sync");

            ConsoleLogHelper.State("FIRE / SMOKE",
                "AI connection detector sync / ENABLED=" + enabled + " / REASON=" + reason);
        }

        /// <summary>
        /// 2026-08-27: 카메라 이동 또는 영상 조건 변경 뒤 EO/IR 기준 프레임과
        /// 시간축 후보를 함께 초기화하여 화면 전체 변화가 이벤트가 되지 않게 한다.
        /// </summary>
        private void ResetFireSmokeFrameAnalysis(string reason)
        {
            _eoSmokeDetectionService.Reset();
            _irSmokeDetectionService.Reset();
            _eoFireDetectionService.Reset();
            _irFireDetectionService.Reset();

            ConsoleLogHelper.State(
                "FIRE / SMOKE",
                "Frame analysis reset / REASON=" + reason);
        }

        public bool IsSmokeDetectionEnabled
        {
            get => _isSmokeDetectionEnabled;
            set
            {
                if (_isSmokeDetectionEnabled == value)
                {
                    return;
                }

                _isSmokeDetectionEnabled = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(SmokePowerStatusText));
                OnPropertyChanged(nameof(FireAlertStatusText));

                ConsoleLogHelper.State(
                    "SMOKE DETECTOR",
                    value
                        ? "Smoke candidate detection enabled"
                        : "Smoke candidate detection disabled");
            }

        }

        /// <summary>
        /// 2026-08-27: 연결 상태 영역에서 연기 영상처리 사용 여부를 FIRE와 분리해 표시한다.
        /// </summary>
        public string SmokePowerStatusText =>
            IsSmokeDetectionEnabled ? "ON" : "OFF";

        /// <summary>
        /// 2026-08-27: 0=AUTO, 1=EO, 2=IR 보조 후보로 제한한다.
        /// </summary>
        public int SmokeDetectionSourceIndex
        {
            get => _smokeDetectionSourceIndex;
            set
            {
                int normalized = value < 0 ? 0 : value > 2 ? 2 : value;
                if (_smokeDetectionSourceIndex == normalized)
                {
                    return;
                }

                _smokeDetectionSourceIndex = normalized;
                OnPropertyChanged();
                ConsoleLogHelper.State(
                    "SMOKE DETECTOR",
                    "Source mode changed / MODE=" + normalized);
            }

        }

        /// <summary>
        /// 2026-08-27: 0=SENSITIVE, 1=BALANCED, 2=STRICT 감도 프리셋이다.
        /// </summary>
        public int SmokeSensitivityIndex
        {
            get => _smokeSensitivityIndex;
            set
            {
                int normalized = value < 0 ? 0 : value > 2 ? 2 : value;
                if (_smokeSensitivityIndex == normalized)
                {
                    return;
                }

                _smokeSensitivityIndex = normalized;
                OnPropertyChanged();

                if (normalized == 0)
                {
                    SmokeMinimumAreaRatio = 0.0005;
                    SmokeChangeThresholdRatio = 0.015;
                }
                else if (normalized == 2)
                {
                    SmokeMinimumAreaRatio = 0.0030;
                    SmokeChangeThresholdRatio = 0.055;
                }
                else
                {
                    SmokeMinimumAreaRatio = 0.0015;
                    SmokeChangeThresholdRatio = 0.035;
                }

                ConsoleLogHelper.State(
                    "SMOKE DETECTOR",
                    "Sensitivity changed / PRESET=" + normalized);
            }

        }

        public double SmokeMinimumAreaRatio
        {
            get => _smokeMinimumAreaRatio;
            set
            {
                double normalized = Math.Max(0.0005, Math.Min(0.05, value));
                if (Math.Abs(_smokeMinimumAreaRatio - normalized) < 0.0001)
                {
                    return;
                }

                _smokeMinimumAreaRatio = normalized;
                OnPropertyChanged();
            }

        }

        public double SmokeChangeThresholdRatio
        {
            get => _smokeChangeThresholdRatio;
            set
            {
                double normalized = Math.Max(0.015, Math.Min(0.15, value));
                if (Math.Abs(_smokeChangeThresholdRatio - normalized) < 0.001)
                {
                    return;
                }

                _smokeChangeThresholdRatio = normalized;
                OnPropertyChanged();
            }

        }

        /// <summary>
        /// 2026-08-28: AUTO SCAN과 파노라마 촬영 중에도 FIRE/SMOKE 분석 및
        /// 이벤트 갱신을 유지하고, 일반 수동 이동 중에만 분석을 보류한다.
        /// </summary>
        private bool IsFireSmokeFrameAnalysisAllowed()
        {
            if (IsLaPresetScanRunning ||
                IsPresetScanRunning ||
                IsPanoramaCaptureRunning ||
                IsPanoramaProcessingRunning)
            {
                return true;
            }

            return !IsControlInputLocked &&
                   _currentMoveType == ContinuousMoveType.None &&
                   !_activePanAbsoluteTarget.HasValue &&
                   !_activeTiltAbsoluteTarget.HasValue;
        }

        /// <summary>
        /// 2026-08-28: 자동 운용으로 영상 전체가 이동하는 구간을 확인한다.
        /// SMOKE 분석기는 이 상태에서 전역 이동량을 먼저 보정한다.
        /// </summary>
        private bool IsAutomaticCameraMotionActive()
        {
            return IsLaPresetScanRunning ||
                   IsPresetScanRunning ||
                   IsPanoramaCaptureRunning;
        }

        private SmokeDetectionResult ProcessSmokeFrame(
            Mat frame,
            string streamName,
            Rect fireCandidateRect)
        {
            bool isInfrared =
                string.Equals(streamName, "IR", StringComparison.OrdinalIgnoreCase);
            bool sourceEnabled =
                SmokeDetectionSourceIndex == 0 ||
                (SmokeDetectionSourceIndex == 1 && !isInfrared) ||
                (SmokeDetectionSourceIndex == 2 && isInfrared);

            SmokeDetectionService service =
                isInfrared
                    ? _irSmokeDetectionService
                    : _eoSmokeDetectionService;
            bool compensateCameraMotion =
                IsAutomaticCameraMotionActive();

            if (_wasMotionCompensatedSmokeAnalysis != compensateCameraMotion)
            {
                _wasMotionCompensatedSmokeAnalysis = compensateCameraMotion;
                ConsoleLogHelper.State(
                    "SMOKE DETECTOR",
                    "Camera motion compensation " +
                    (compensateCameraMotion ? "enabled" : "disabled") +
                    " / AUTO_SCAN_L=" + IsLaPresetScanRunning +
                    " / AUTO_SCAN_W=" + IsPresetScanRunning +
                    " / PANORAMA=" + IsPanoramaCaptureRunning);
            }

            if (!IsFireSmokeFrameAnalysisAllowed())
            {
                SmokeDetectionResult disabledResult = service.Process(
                    frame,
                    false,
                    isInfrared,
                    SmokeMinimumAreaRatio,
                    SmokeChangeThresholdRatio,
                    Rect.Empty,
                    new List<Rect>(),
                    SmokeBoxGroupingMode,
                    false);
                return disabledResult;
            }

            SmokeDetectionResult result = service.Process(
                frame,
                IsSmokeDetectionEnabled && sourceEnabled,
                isInfrared,
                SmokeMinimumAreaRatio,
                SmokeChangeThresholdRatio,
                fireCandidateRect,
                GetRecentAiSmokeCandidates(isInfrared),
                SmokeBoxGroupingMode,
                compensateCameraMotion);
            return result;
        }

    }

}
