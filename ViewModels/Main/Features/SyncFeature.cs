using OpenCvWpfTracking.Common;
using OpenCvWpfTracking.Models.Main;
using OpenCvWpfTracking.Services.Control;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
using static OpenCvWpfTracking.ViewModels.Main.MainViewModel;

namespace OpenCvWpfTracking.ViewModels.Main
{
    /// <summary>EO·IR 렌즈 동기화에 필요한 협력 기능만 받는 경계입니다. 화면 객체의 내부 구현을 알지 않습니다.</summary>
    internal interface ISyncFeatureContext
    {
        RtspSourceOption _connectedEoCtecSource { get; set; }

        string _connectionSessionId { get; set; }

        Services.Communication.IControlCommandServicePort _controlCommandService { get; }

        Services.Communication.ICtecCameraCommandServicePort _ctecCameraCommandService { get; }

        short _currentEoFocus { get; set; }

        short _currentEoZoom { get; set; }

        ushort _currentIrFocus { get; set; }

        ushort _currentIrZoom { get; set; }

        int _environmentIrZoomPhysicalTeleRaw { get; set; }

        int _environmentIrZoomPhysicalWideRaw { get; set; }

        Services.Video.IFFmpegDecoderServicePort _eoDecoder { get; }

        ref long _eoLensStatusVersion { get; }

        IFieldOfViewSyncServicePort _fieldOfViewSyncService { get; }

        Services.Video.IFFmpegDecoderServicePort _irDecoder { get; }

        ref long _irLensStatusVersion { get; }

        double _irZoomObservedStopLag { get; set; }

        bool _isEoFrameDisplayed { get; set; }

        bool _isIrFrameDisplayed { get; set; }

        SemaphoreSlim _lensSyncOperationLock { get; }

        string _ptzPerformanceScenario { get; set; }

        CancellationTokenSource _rooftopFocusSyncCts { get; set; }

        CancellationTokenSource _rooftopZoomSyncCts { get; set; }

        Services.Communication.WebAgent.IWebAgentZoomControlServicePort _webAgentZoomControlService { get; }

        /// <summary>
        /// 화면의 IR Focus 표준 목표값을 현재 장비가 보고하는 상태 좌표로 변환한다. LA 상태 좌표만 반대 방향이며, Web Agent 상태
        /// 좌표는 그대로 사용한다.
        /// </summary>
        int ConvertIrFocusStandardToStatusPosition(int standardPosition);

        /// <summary>
        /// IR 피드백을 확보하지 못하면 절대위치 성공/시간추정으로 프리셋 완료를 표시하지 않는다.
        /// </summary>
        Task<bool> EnsurePresetIrLensFeedbackAsync(bool zoom, CancellationToken token);

        System.Collections.ObjectModel.ObservableCollection<ZoomSyncLevelOption> FocusSyncLevelOptions { get; }

        string FocusSyncStatusText { get; set; }

        /// <summary>
        /// 프리셋 렌즈 복원 판단에 사용할 응답이 최신인지 확인한다.
        /// </summary>
        bool HasFreshPresetLens(bool ir, bool zoom);

        /// <summary>
        /// AF 동작·응답 대기·상태 불명 중 해당 채널의 수동 초점 충돌을 차단한다.
        /// </summary>
        bool IsAutoFocusBlockingManualFocus(bool ir, bool logWarning = true);

        bool IsEnvironmentStatusSelected { get; }

        bool IsSelectedControlAgentWebAgent { get; }

        /// <summary>
        /// 채널과 축별 마지막 렌즈 응답 시간을 조회한다.
        /// </summary>
        long PresetLensTicks(bool ir, bool zoom);

        /// <summary>
        /// [CTEC Position Inquiry] CGI 송신 후 실제 TCP 9000 Position 응답까지 기다린다. HTTP 200
        /// OK만으로 조회 완료 처리하지 않는다. Zoom은 0x47, Focus는 0x48 응답이 도착해야 한 번의 Inquiry가 끝난다.
        /// </summary>
        Task<int?> RequestAndWaitCtecEoPositionAsync(ContinuousMoveType moveType, RtspSourceOption sourceOption, CancellationToken cancellationToken);

        EquipmentStatusMode SelectedEquipmentStatusMode { get; set; }

        ZoomSyncLevelOption SelectedFocusSyncLevel { get; set; }

        ZoomSyncLevelOption SelectedZoomSyncLevel { get; set; }

        System.Collections.ObjectModel.ObservableCollection<ZoomSyncLevelOption> ZoomSyncLevelOptions { get; }

        string ZoomSyncStatusText { get; set; }
    }

    /// <summary>EO·IR 렌즈 동기화 담당 객체입니다. 상태를 보관하고 입력을 받아 결과를 알립니다.</summary>
    internal sealed class SyncFeature
    {

        #region [협력 객체와 초기화]

        private readonly ISyncFeatureContext _context;

        /// <summary>
        /// EO/IR 렌즈 동기화 기능에서 사용할 협력 객체를 연결하고 초기 상태를 준비한다.
        /// </summary>
        internal SyncFeature(ISyncFeatureContext context)
        {
            _context = context ?? throw new System.ArgumentNullException(nameof(context));
        }

        #endregion

        #region [상태 필드]

        internal string _irFocusDirectUnavailableSession;

        #endregion

        #region [동작 처리]

        /// <summary>
        /// 줌 동기화 선택을 목록의 이전 단계로 이동한다.
        /// </summary>
        internal void SelectPreviousZoomSyncLevel()
        {
            int currentIndex = _context.ZoomSyncLevelOptions.IndexOf(_context.SelectedZoomSyncLevel);

            if (currentIndex > 0)
            {
                _context.SelectedZoomSyncLevel = _context.ZoomSyncLevelOptions[currentIndex - 1];
            }
        }

        /// <summary>
        /// 줌 동기화 선택을 목록의 다음 단계로 이동한다.
        /// </summary>
        internal void SelectNextZoomSyncLevel()
        {
            int currentIndex = _context.ZoomSyncLevelOptions.IndexOf(_context.SelectedZoomSyncLevel);

            if (currentIndex >= 0 && currentIndex < _context.ZoomSyncLevelOptions.Count - 1)
            {
                _context.SelectedZoomSyncLevel = _context.ZoomSyncLevelOptions[currentIndex + 1];
            }
        }

        /// <summary>
        /// WEB AGENT에서 선택한 11단계(LEVEL 0~10)의 실측 화각 대응 위치를 적용한다. EO/IR 렌즈별 목표 위치는 각각 계산하며
        /// 동일한 값을 단순 복사하지 않는다. LA 구성은 동기화 탭과 실행 경로를 차단한다.
        /// </summary>
        internal async Task ApplySelectedZoomSyncLevelAsync()
        {
            // LA 장비에는 현재 EO/IR 동기화 보정 구성을 적용하지 않는다.
            if (!_context.IsEnvironmentStatusSelected)
            {
                return;
            }

            ConsoleLogHelper.Command("ZOOM SYNC", $"Apply requested / EQUIPMENT={_context.SelectedEquipmentStatusMode} / TARGET={_context.SelectedZoomSyncLevel?.Position.ToString() ?? "NULL"}");

            ZoomSyncLevelOption selectedLevel = _context.SelectedZoomSyncLevel;

            if (selectedLevel == null)
            {
                return;
            }

            if (!await _context._lensSyncOperationLock.WaitAsync(0))
            {
                _context.ZoomSyncStatusText = "BUSY / OTHER LENS SYNC RUNNING";

                return;
            }

            await StopZoomSyncAsync();

            // 2026-09-18: 같은 0~1000 값을 두 카메라에 복사하지 않는다.
            // IR(25~225 mm, 640x512/17um)의 선택 단계에서 목표 HFOV를 구하고,
            // XV-Z2090HC(6~540 mm)가 같은 HFOV가 되는 별도 위치를 계산한다.
            ZoomFovTarget fovTarget = _context._fieldOfViewSyncService.CreateTarget(selectedLevel.Level);

            short eoStandardPosition = fovTarget.EoPosition;

            short irStandardPosition = fovTarget.IrPosition;

            ConsoleLogHelper.State("ZOOM SYNC", $"2026-10-02 calibration selected / MODEL=PIECEWISE_LINEAR_IR_TO_EO / " + $"LEVEL={selectedLevel.Level} / EO_TARGET={eoStandardPosition} / IR_TARGET={irStandardPosition}");

            // 최대 단계는 프로토콜의 실제 망원 끝점을 요청해야 한다.
            // 이전 학습 범위로 환산하면 렌즈가 끝점에 닿기 전에 최대 배율로 오인할 수 있다.
            // 실제 렌즈가 최대 줌에 도달하기 전에 완료로 보고하지 않게 한다.
            bool verifyRawTeleEndpoint = _context.IsEnvironmentStatusSelected && selectedLevel.Level == 10;

            int irRawTarget = verifyRawTeleEndpoint ? 1000 : ConvertIrZoomSyncToStatusPosition(irStandardPosition);

            CancellationTokenSource zoomSyncCts = new CancellationTokenSource();

            _context._rooftopZoomSyncCts = zoomSyncCts;

            _context.ZoomSyncStatusText = $"APPLYING LEVEL {selectedLevel.Level}";

            _context._ptzPerformanceScenario = "ZOOM_SYNC";

            bool eoResult = false;

            bool irResult = false;

            try
            {
                long eoStartSequence = Interlocked.Read(ref _context._eoLensStatusVersion);

                Task<bool> irMoveTask = Interlocked.Read(ref _context._irLensStatusVersion) > 0 ? MoveIrZoomToPositionAsync(irRawTarget, zoomSyncCts.Token, verifyRawTeleEndpoint ? -1 : irStandardPosition, verifyRawTeleEndpoint ? 12 : IrZoomTargetTolerance, syncCalibration: true) : Task.FromResult(false);

                if (_context.SelectedEquipmentStatusMode == EquipmentStatusMode.Environment)
                {
                    bool eoCommanded = _context._webAgentZoomControlService.SetEoZoomPosition(eoStandardPosition);

                    Task<bool> eoMoveTask = eoCommanded ? WaitForEnvironmentEoLensTargetAsync(eoStandardPosition, true, eoStartSequence, zoomSyncCts.Token) : Task.FromResult(false);

                    bool[] results = await Task.WhenAll(eoMoveTask, irMoveTask);

                    eoResult = results[0];

                    irResult = results[1];
                }
                else
                {
                    RtspSourceOption ctecSource = _context._connectedEoCtecSource;

                    Task<bool> eoMoveTask = ctecSource == null ? Task.FromResult(false) : MoveRooftopEoZoomToRawPositionAsync(ctecSource, ConvertStandardZoomToCtecRaw(eoStandardPosition), zoomSyncCts.Token);

                    bool[] results = await Task.WhenAll(eoMoveTask, irMoveTask);

                    eoResult = results[0];

                    irResult = results[1];
                }

                ConsoleLogHelper.State("ZOOM SYNC", $"MODEL=MEASURED_LEVEL_0_10_261002_PIECEWISE / LEVEL={selectedLevel.Level} / " + $"EO_COMMANDED={eoStandardPosition} / IR_COMMANDED={irStandardPosition} / " + $"IR_COMMAND_RAW_TARGET={irRawTarget} / EO_ACTUAL={_context._currentEoZoom} / " + $"IR_ENDPOINT_MODE={(verifyRawTeleEndpoint ? "RAW_TELE_1000" : "CALIBRATED_SPAN")} / " + $"IR_SETTLED_RAW={_context._currentIrZoom} / IR_STANDARD_FINAL={NormalizeIrZoomSyncStatusPosition(_context._currentIrZoom)} / " + $"PHYSICAL_MIN={_context._environmentIrZoomPhysicalWideRaw} / PHYSICAL_MAX={_context._environmentIrZoomPhysicalTeleRaw} / " + $"EO_POSITION_ERROR={Math.Abs(_context._currentEoZoom - eoStandardPosition)} / " + $"IR_POSITION_ERROR={Math.Abs(NormalizeIrZoomSyncStatusPosition(_context._currentIrZoom) - irStandardPosition)} / " + $"EO_DONE={eoResult} / IR_DONE={irResult} / FINAL_RESULT={(eoResult && irResult ? "COMPLETED" : "INCOMPLETE")}");

                // 2026-09-18: 장비 기구 오차로 목표 허용범위를 조금 벗어난 경우를
                // 고장처럼 표시하지 않는다. INCOMPLETE는 IR 명령/피드백이 있어도
                // 실제 Zoom 위치가 전혀 움직이지 않은 경우에만 표시한다.
                _context.ZoomSyncStatusText = !irResult || !eoResult ? "INCOMPLETE / EO OR IR TARGET NOT CONFIRMED" : $"COMPLETED / LEVEL {selectedLevel.Level}";
            }
            catch (OperationCanceledException)
            {
                _context.ZoomSyncStatusText = "STOPPED";
            }
            catch (Exception ex)
            {
                _context.ZoomSyncStatusText = "ERROR / " + ex.Message;

                ConsoleLogHelper.Error("ZOOM SYNC", ex.ToString());
            }
            finally
            {
                if (ReferenceEquals(_context._rooftopZoomSyncCts, zoomSyncCts))
                {
                    _context._rooftopZoomSyncCts = null;
                }

                zoomSyncCts.Dispose();

                _context._ptzPerformanceScenario = "IDLE";

                _context._lensSyncOperationLock.Release();
            }
        }

        /// <summary>
        /// 옥상장비 CTEC EO Zoom을 목표 Raw Position으로 직접 이동한다. 기존 TELE / WIDE 연속 이동 기반 Sync는 다음
        /// 문제가 있었다. 1. TCP Position 응답이 계단식으로 늦게 반영됨 2. 프로그램이 목표 통과를 늦게 확인함 3. 다음 단계에서 반대
        /// 방향 보정이 발생함 4. 화면이 확대 → 축소 → 확대 형태로 왕복함 현재 방식은 VISCA Zoom Direct 명령으로 목표 Raw
        /// Position을 한 번만 송신하고, 이후 Inquiry는 도착 확인 용도로만 사용한다. 따라서 Sync 이동 중 TELE / WIDE 방향
        /// 전환 및 재보정은 수행하지 않는다.
        /// </summary>
        internal async Task<bool> MoveRooftopEoZoomToRawPositionAsync(RtspSourceOption ctecSource, int targetRawPosition, CancellationToken cancellationToken)
        {
            if (ctecSource == null)
            {
                return false;
            }

            int safeTargetRawPosition = Math.Max(0, Math.Min(CtecEoZoomPositionMax, targetRawPosition));

            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                ConsoleLogHelper.PrintLine();

                Console.WriteLine("[ROOFTOP ZOOM SYNC] DIRECT START");

                Console.WriteLine($"[ROOFTOP ZOOM SYNC] TARGET : {safeTargetRawPosition}");

                ConsoleLogHelper.PrintLine();

                bool commandResult = await _context._ctecCameraCommandService.MoveZoomPositionAsync(ctecSource.ControlIp, ctecSource.ControlUserName, ctecSource.ControlPassword, ctecSource.UseHttps, (ushort)safeTargetRawPosition);

                if (!commandResult)
                {
                    Console.WriteLine("[ROOFTOP ZOOM SYNC] DIRECT COMMAND FAILED");

                    ConsoleLogHelper.PrintLine();

                    return false;
                }

                Stopwatch timeout = Stopwatch.StartNew();

                while (timeout.ElapsedMilliseconds < RooftopZoomSyncTimeoutMs)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    int? currentPosition = await _context.RequestAndWaitCtecEoPositionAsync(ContinuousMoveType.EoZoom, ctecSource, cancellationToken);

                    if (currentPosition.HasValue)
                    {
                        int error = Math.Abs(currentPosition.Value - safeTargetRawPosition);

                        Console.WriteLine("[ROOFTOP ZOOM SYNC] DIRECT CHECK " + $"/ POSITION={currentPosition.Value} " + $"/ TARGET={safeTargetRawPosition} " + $"/ ERROR={error}");

                        if (error <= RooftopZoomSyncTolerance)
                        {
                            Console.WriteLine("[ROOFTOP ZOOM SYNC] DIRECT COMPLETED");

                            ConsoleLogHelper.PrintLine();

                            return true;
                        }
                    }

                    await Task.Delay(RooftopZoomSyncInquiryIntervalMs, cancellationToken);
                }

                Console.WriteLine("[ROOFTOP ZOOM SYNC] DIRECT TIMEOUT");

                ConsoleLogHelper.PrintLine();

                return false;
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("[ROOFTOP ZOOM SYNC] DIRECT CANCELED");

                ConsoleLogHelper.PrintLine();

                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine("[ROOFTOP ZOOM SYNC] DIRECT ERROR : " + ex.Message);

                ConsoleLogHelper.PrintLine();

                return false;
            }
        }

        /// <summary>
        /// 진행 중인 줌 동기화를 취소하고 관련 렌즈 이동을 중지한다.
        /// </summary>
        internal async Task StopZoomSyncAsync()
        {
            CancellationTokenSource cts = _context._rooftopZoomSyncCts;

            _context._rooftopZoomSyncCts = null;

            if (cts != null)
            {
                cts.Cancel();
            }

            // 2026-09-17: IR 렌즈 전용 Stop만 사용한다.
            // PTZF 전체정지(FF 01 00 00 00 00 01)는 Pan/Tilt까지 중지시키므로
            // Zoom Sync 중지 경로에서는 절대 송신하지 않는다.
            _context._controlCommandService.StopIrZoom();

            RtspSourceOption ctecSource = _context._connectedEoCtecSource;

            if (ctecSource != null)
            {
                await _context._ctecCameraCommandService.StopZoomAsync(ctecSource.ControlIp, ctecSource.ControlUserName, ctecSource.ControlPassword, ctecSource.UseHttps);
            }

            _context.ZoomSyncStatusText = "STOPPED";
        }

        /// <summary>
        /// 일반 WEB 줌은 수신·송신 위치를 그대로 사용한다. LA의 기존 방향 반전만 유지한다.
        /// </summary>
        internal int ConvertIrZoomStandardToStatusPosition(int standardPosition)
        {
            int safe = Math.Max(0, Math.Min(1000, standardPosition));

            if (_context.IsSelectedControlAgentWebAgent)
            {
                return safe;
            }
            else
            {
                return 1000 - safe;
            }
        }

        /// <summary>
        /// 상태 표시·새 프리셋 저장은 SYNC 끝점 보정과 독립적인 장비 위치를 사용한다.
        /// </summary>
        internal int NormalizeIrZoomStatusPosition(int rawPosition)
        {
            int safe = Math.Max(0, Math.Min(1000, rawPosition));

            if (_context.IsSelectedControlAgentWebAgent)
            {
                return safe;
            }
            else
            {
                return 1000 - safe;
            }
        }

        /// <summary>
        /// 공유 구동부에서 명시적으로 요청한 SYNC 작업에만 끝점 환산을 적용한다.
        /// </summary>
        internal int NormalizeIrZoomControlPosition(int rawPosition, bool syncCalibration)
        {
            if (syncCalibration)
            {
                return NormalizeIrZoomSyncStatusPosition(rawPosition);
            }
            else
            {
                return NormalizeIrZoomStatusPosition(rawPosition);
            }
        }

        /// <summary>
        /// SYNC 화각 단계에 한정해 학습된 물리 끝점을 장비 명령 위치로 환산한다.
        /// </summary>
        internal int ConvertIrZoomSyncToStatusPosition(int standardPosition)
        {
            int safe = Math.Max(0, Math.Min(1000, standardPosition));

            if (_context.SelectedEquipmentStatusMode == EquipmentStatusMode.Rooftop)
            {
                return 1000 - safe;
            }

            int span = Math.Max(1, _context._environmentIrZoomPhysicalTeleRaw - _context._environmentIrZoomPhysicalWideRaw);

            return _context._environmentIrZoomPhysicalWideRaw + (int)Math.Round(safe * span / 1000.0);
        }

        /// <summary>
        /// IR 줌 상태 응답을 동기화에 사용하는 위치 범위로 정규화한다.
        /// </summary>
        internal int NormalizeIrZoomSyncStatusPosition(int rawPosition)
        {
            int safeRaw = Math.Max(0, Math.Min(1000, rawPosition));

            if (_context.SelectedEquipmentStatusMode == EquipmentStatusMode.Rooftop)
            {
                return 1000 - safeRaw;
            }

            int span = Math.Max(1, _context._environmentIrZoomPhysicalTeleRaw - _context._environmentIrZoomPhysicalWideRaw);

            return Math.Max(0, Math.Min(1000, (int)Math.Round((safeRaw - _context._environmentIrZoomPhysicalWideRaw) * 1000.0 / span)));
        }

        /// <summary>
        /// 환경 장비 IR 줌 끝점 응답을 위치 보정 근거에 반영한다.
        /// </summary>
        internal void UpdateEnvironmentIrZoomEndpointCalibration(int standardTarget, int settledRaw)
        {
            if (_context.SelectedEquipmentStatusMode != EquipmentStatusMode.Environment)
            {
                return;
            }

            if (standardTarget <= 0 && settledRaw >= 0 && settledRaw <= EnvironmentIrZoomEndpointLearningLimit)
            {
                _context._environmentIrZoomPhysicalWideRaw = settledRaw;
            }
            else if (standardTarget >= 1000 && settledRaw >= 930 && settledRaw <= 1000)
            {
                _context._environmentIrZoomPhysicalTeleRaw = settledRaw;
            }
            else
            {
                return;
            }

            ConsoleLogHelper.State("IR ZOOM CALIBRATION", $"STANDARD_TARGET={standardTarget} / SETTLED_RAW={settledRaw} / " + $"PHYSICAL_WIDE={_context._environmentIrZoomPhysicalWideRaw} / PHYSICAL_TELE={_context._environmentIrZoomPhysicalTeleRaw} / " + $"STANDARD_FINAL={NormalizeIrZoomSyncStatusPosition(settledRaw)}");
        }

        /// <summary>
        /// 2026-10-16: 환경부 장비는 신규 CMD2=0xB7 절대위치를 우선 사용하고 실제 IR 렌즈 피드백으로 도달 여부를 확인한다. 새
        /// 피드백이 계속 수신되는데도 위치가 전혀 변하지 않을 때만 0x31 폐루프 경로로 전환한다.
        /// </summary>
        internal async Task<bool> MoveIrZoomToPositionAsync(int targetPosition, CancellationToken cancellationToken, int standardTarget = -1, int tolerance = IrZoomTargetTolerance, bool syncCalibration = false, bool explicitAbsolute = false)
        {
            int target = Math.Max(0, Math.Min(1000, targetPosition));

            ConsoleLogHelper.State("IR ZOOM COORDINATES", $"MODE={(syncCalibration ? "SYNC_CALIBRATED" : "DEVICE_POSITION")} / RAW_TARGET={target} / INPUT_TARGET={standardTarget}");

            Stopwatch total = Stopwatch.StartNew();

            int operationStart = _context._currentIrZoom;

            int retryCount = 0;

            int settled = operationStart;

            bool operationMovementObserved = false;

            int previousDirection = 0;

            int directionReversalCount = 0;

            string lastDirection = "NONE";

            if (explicitAbsolute && _context.IsSelectedControlAgentWebAgent)
            {
                return await TryMoveEnvironmentIrZoomAbsoluteAsync(target, standardTarget, cancellationToken, 1, false) == true;
            }

            if (IsIrZoomTargetReached(target, operationStart, standardTarget, tolerance, syncCalibration))
            {
                if (_context.SelectedEquipmentStatusMode == EquipmentStatusMode.Environment)
                {
                    long existingSequence = Interlocked.Read(ref _context._irLensStatusVersion);

                    return await WaitForIrLensFeedbackAsync(existingSequence, cancellationToken) && IsIrZoomTargetReached(target, _context._currentIrZoom, standardTarget, tolerance, syncCalibration);
                }

                return true;
            }

            if (_context.SelectedEquipmentStatusMode == EquipmentStatusMode.Environment)
            {
                bool? absoluteResult = await TryMoveEnvironmentIrZoomAbsoluteAsync(target, standardTarget, cancellationToken, tolerance, syncCalibration);

                if (absoluteResult.HasValue && absoluteResult.Value)
                {
                    return true;
                }

                if (!absoluteResult.HasValue)
                {
                    // 피드백 단절이나 절대 명령 중 실제 이동이 관측된 경우에는
                    // 연속 명령을 겹쳐 보내지 않는다.
                    return false;
                }
            }

            // 2026-09-21: 각 Pulse가 완전히 정착한 뒤 새 오차를 계산한다.
            // 장비 관성으로 목표를 통과해도 사용자가 다시 APPLY할 필요가 없도록
            // 같은 작업 안에서 최대 2회의 제한된 방향 반전을 허용한다.
            for (int attempt = 1; attempt <= IrZoomSyncMaxMoveAttempts; attempt++)
            {
                int start = _context._currentIrZoom;

                if (IsIrZoomTargetReached(target, start, standardTarget, tolerance, syncCalibration))
                {
                    return true;
                }

                int currentDirection = target > start ? 1 : -1;

                if (previousDirection != 0 && currentDirection != previousDirection)
                {
                    directionReversalCount++;

                    if (directionReversalCount > IrZoomMaximumDirectionReversals)
                    {
                        ConsoleLogHelper.Warning("IR ZOOM SYNC", $"RESULT=REVERSAL_LIMIT / TARGET={target} / CURRENT_POSITION={start} / " + $"REVERSAL_COUNT={directionReversalCount}");

                        settled = start;

                        break;
                    }
                }

                previousDirection = currentDirection;

                // 오차·정지 계산은 원시 좌표 방향으로 유지하고 광학 구동 방향만 장비별로 변환한다.
                // LA는 망원으로 갈수록 원시 줌이 감소하며 WEB은 증가한다.
                bool zoomIn = currentDirection > 0;

                bool opticalZoomIn = _context.IsSelectedControlAgentWebAgent ? zoomIn : !zoomIn;

                string direction = opticalZoomIn ? "ZoomIn" : "ZoomOut";

                ConsoleLogHelper.State("IR ZOOM DIRECTION", $"AGENT={(_context.IsSelectedControlAgentWebAgent ? "WEB" : "LA")} / RAW_START={start} / RAW_TARGET={target} / " + $"RAW_DIRECTION={currentDirection} / OPTICAL_DIRECTION={direction}");

                lastDirection = direction;

                long startSequence = Interlocked.Read(ref _context._irLensStatusVersion);

                bool started = opticalZoomIn ? _context._controlCommandService.StartIrZoomTele() : _context._controlCommandService.StartIrZoomWide();

                if (!started)
                {
                    return false;
                }

                int previous = start;

                bool movementObserved = false;

                bool stopped = false;

                int unchangedFeedbackCount = 0;

                long lastFeedbackMs = total.ElapsedMilliseconds;

                int stopLead = IrZoomMinimumStopLead;

                // 2026-09-18: LEVEL 5~10 시험에서 목표와 50~170 step만 남은 상태로
                // 연속 구동을 시작하면 첫 500ms 피드백 전에 목표를 지나 100~300 step
                // 과주행했다. 짧은 거리는 상태 수신을 기다리는 연속 구동 대신 동일
                // 방향의 짧은 Pulse만 허용하고 정착값을 다시 판정한다.
                int startRemaining = zoomIn ? target - start : start - target;

                if (startRemaining <= IrZoomShortPulseThreshold)
                {
                    int physicalSpan = syncCalibration && _context.SelectedEquipmentStatusMode == EquipmentStatusMode.Environment ? Math.Max(1, _context._environmentIrZoomPhysicalTeleRaw - _context._environmentIrZoomPhysicalWideRaw) : 1000;

                    int pulseMilliseconds = Math.Max(IrZoomShortPulseMinimumMs, Math.Min(IrZoomShortPulseMaximumMs, (int)Math.Round(startRemaining * EnvironmentIrZoomFullTravelMs / (double)physicalSpan * IrZoomShortPulseScale)));

                    try
                    {
                        await Task.Delay(pulseMilliseconds, cancellationToken);
                    }
                    finally
                    {
                        stopped = _context._controlCommandService.StopIrZoom();
                    }

                    int pulseStopPosition = _context._currentIrZoom;

                    settled = await WaitForIrLensSettledPositionAsync(true, cancellationToken);

                    bool pulseMovementObserved = Math.Abs(settled - start) > 1;

                    operationMovementObserved |= pulseMovementObserved;

                    if (syncCalibration && standardTarget >= 0 && tolerance <= IrZoomTargetTolerance)
                    {
                        UpdateEnvironmentIrZoomEndpointCalibration(standardTarget, settled);
                    }

                    // 목표와 반대 방향의 실제 이동은 재시도로 덮지 않고 정지 상태로 종료한다.
                    if ((settled - start) * currentDirection < -Math.Max(1, tolerance))
                    {
                        ConsoleLogHelper.Warning("IR ZOOM DIRECTION", $"RESULT=OPPOSITE_FEEDBACK / RAW_START={start} / RAW_TARGET={target} / SETTLED_RAW={settled}");

                        return false;
                    }

                    bool pulseReached = IsIrZoomTargetReached(target, settled, standardTarget, tolerance, syncCalibration);

                    ConsoleLogHelper.State("IR ZOOM SYNC", $"STANDARD_TARGET={standardTarget} / COMMAND_RAW_TARGET={target} / START_POSITION={operationStart} / CURRENT_POSITION={start} / " + $"DIRECTION={direction} / CONTROL_MODE=SHORT_PULSE / PULSE_MS={pulseMilliseconds} / " + $"STOP_POSITION={pulseStopPosition} / SETTLED_RAW={settled} / " + $"STANDARD_FINAL={NormalizeIrZoomControlPosition(settled, syncCalibration)} / " + $"PHYSICAL_WIDE={_context._environmentIrZoomPhysicalWideRaw} / PHYSICAL_TELE={_context._environmentIrZoomPhysicalTeleRaw} / " + $"NORMALIZED_ERROR={(standardTarget >= 0 ? Math.Abs(NormalizeIrZoomControlPosition(settled, syncCalibration) - standardTarget) : Math.Abs(settled - target))} / " + $"MOVEMENT_OBSERVED={pulseMovementObserved} / REVERSAL_COUNT={directionReversalCount} / RETRY_COUNT={retryCount} / " + $"RESULT={(pulseReached ? "COMPLETED" : "RETRY")}");

                    if (pulseReached)
                    {
                        return true;
                    }

                    retryCount++;

                    await Task.Delay(250, cancellationToken);

                    continue;
                }

                try
                {
                    while (total.ElapsedMilliseconds < IrZoomSyncTimeoutMs)
                    {
                        bool feedback = await WaitForIrLensFeedbackAsync(startSequence, cancellationToken);

                        if (!feedback)
                        {
                            ConsoleLogHelper.Warning("IR ZOOM SYNC", $"RESULT=FEEDBACK_TIMEOUT / TARGET={target} / START_POSITION={operationStart} / " + $"CURRENT_POSITION={_context._currentIrZoom} / DIRECTION={direction} / RETRY_COUNT={retryCount}");

                            break;
                        }

                        long sequence = Interlocked.Read(ref _context._irLensStatusVersion);

                        int current = _context._currentIrZoom;

                        int delta = current - previous;

                        long nowMs = total.ElapsedMilliseconds;

                        long feedbackIntervalMs = Math.Max(1, nowMs - lastFeedbackMs);

                        lastFeedbackMs = nowMs;
                        startSequence = sequence;

                        if (delta * currentDirection < -Math.Max(1, tolerance))
                        {
                            ConsoleLogHelper.Warning("IR ZOOM DIRECTION", $"RESULT=OPPOSITE_FEEDBACK / RAW_START={start} / RAW_TARGET={target} / CURRENT_RAW={current}");

                            return false;
                        }

                        if (delta != 0)
                        {
                            movementObserved = true;
                            operationMovementObserved = true;
                            unchangedFeedbackCount = 0;
                        }
                        else
                        {
                            unchangedFeedbackCount++;
                        }

                        // 2026-09-18: 실장비 로그에서 STOP_POSITION=206 이후
                        // SETTLED_POSITION=397까지 약 190 step 이동했다. 직전 delta만
                        // 사용하던 120 cap은 LEVEL 2를 크게 통과하므로, 관측된 정지
                        // 지연과 상태 step을 함께 사용해 더 일찍 STOP한다.
                        int observedStopLag = (int)Math.Round(_context._irZoomObservedStopLag);

                        stopLead = movementObserved ? Math.Max(IrZoomMinimumStopLead, Math.Min(IrZoomMaximumStopLead, Math.Max(observedStopLag + 25, Math.Abs(delta) * 2 + 30))) : IrZoomMinimumStopLead;

                        int remaining = zoomIn ? target - current : current - target;

                        ConsoleLogHelper.State("IR ZOOM SYNC", $"TARGET={target} / START_POSITION={operationStart} / CURRENT_POSITION={current} / " + $"DIRECTION={direction} / STATUS_STEP={delta} / FEEDBACK_INTERVAL_MS={feedbackIntervalMs} / " + $"PREDICTED_STOP_LEAD={stopLead} / RETRY_COUNT={retryCount}");

                        if (movementObserved && remaining <= stopLead)
                        {
                            stopped = _context._controlCommandService.StopIrZoom();

                            break;
                        }

                        if (unchangedFeedbackCount >= 4)
                        {
                            ConsoleLogHelper.Warning("IR ZOOM SYNC", $"RESULT=NO_POSITION_CHANGE / TARGET={target} / CURRENT_POSITION={current} / " + $"DIRECTION={direction} / RETRY_COUNT={retryCount}");

                            break;
                        }

                        previous = current;
                    }
                }
                finally
                {
                    if (!stopped)
                    {
                        _context._controlCommandService.StopIrZoom();
                    }
                }

                int stopPosition = _context._currentIrZoom;

                settled = await WaitForIrLensSettledPositionAsync(true, cancellationToken);

                int stopTravel = zoomIn ? settled - stopPosition : stopPosition - settled;

                if (stopTravel >= 0 && stopTravel <= 500)
                {
                    _context._irZoomObservedStopLag = _context._irZoomObservedStopLag * 0.65 + stopTravel * 0.35;
                }

                if (syncCalibration && standardTarget >= 0 && tolerance <= IrZoomTargetTolerance)
                {
                    UpdateEnvironmentIrZoomEndpointCalibration(standardTarget, settled);
                }

                bool reached = IsIrZoomTargetReached(target, settled, standardTarget, tolerance, syncCalibration);

                int finalError = Math.Abs(settled - target);

                ConsoleLogHelper.State("IR ZOOM SYNC", $"STANDARD_TARGET={standardTarget} / COMMAND_RAW_TARGET={target} / START_POSITION={operationStart} / DIRECTION={direction} / " + $"PREDICTED_STOP_LEAD={stopLead} / STOP_POSITION={stopPosition} / " + $"SETTLED_RAW={settled} / STANDARD_FINAL={NormalizeIrZoomControlPosition(settled, syncCalibration)} / STOP_TRAVEL={stopTravel} / " + $"PHYSICAL_WIDE={_context._environmentIrZoomPhysicalWideRaw} / PHYSICAL_TELE={_context._environmentIrZoomPhysicalTeleRaw} / " + $"OBSERVED_STOP_LAG={_context._irZoomObservedStopLag:F1} / RAW_ERROR={finalError} / " + $"NORMALIZED_ERROR={(standardTarget >= 0 ? Math.Abs(NormalizeIrZoomControlPosition(settled, syncCalibration) - standardTarget) : finalError)} / RETRY_COUNT={retryCount} / " + $"RESULT={(reached ? "COMPLETED" : "RETRY")}");

                if (reached)
                {
                    return true;
                }

                if (!movementObserved)
                {
                    ConsoleLogHelper.Warning("IR ZOOM SYNC", $"FEEDBACK RECEIVED BUT POSITION UNCHANGED / ATTEMPT={attempt} / POSITION={settled}");
                }

                retryCount++;

                await Task.Delay(250, cancellationToken);
            }

            bool positionMatched = IsIrZoomTargetReached(target, settled, standardTarget, tolerance, syncCalibration);

            bool finalResult = positionMatched;

            ConsoleLogHelper.State("IR ZOOM SYNC", $"STANDARD_TARGET={standardTarget} / COMMAND_RAW_TARGET={target} / START_POSITION={operationStart} / SETTLED_RAW={settled} / " + $"STANDARD_FINAL={NormalizeIrZoomControlPosition(settled, syncCalibration)} / " + $"PHYSICAL_WIDE={_context._environmentIrZoomPhysicalWideRaw} / PHYSICAL_TELE={_context._environmentIrZoomPhysicalTeleRaw} / " + $"NORMALIZED_ERROR={(standardTarget >= 0 ? Math.Abs(NormalizeIrZoomControlPosition(settled, syncCalibration) - standardTarget) : Math.Abs(settled - target))} / DIRECTION={lastDirection} / " + $"REVERSAL_COUNT={directionReversalCount} / RETRY_COUNT={retryCount} / POSITION_MATCH={positionMatched} / MOVEMENT_OBSERVED={operationMovementObserved} / " + $"RESULT={(finalResult ? "OPERATIONAL" : "NO_MOVEMENT")}");

            return finalResult;
        }

        /// <summary>
        /// true: 실제 위치 도달, false: 새 피드백이 지속되지만 이동 없음(수동 대체 가능), null: 송신 실패·피드백 단절·이동 중
        /// 미도달(명령 중첩 금지).
        /// </summary>
        internal async Task<bool?> TryMoveEnvironmentIrZoomAbsoluteAsync(int rawTarget, int standardTarget, CancellationToken cancellationToken, int tolerance = IrZoomTargetTolerance, bool syncCalibration = false)
        {
            const int noMovementWaitMs = 3000;

            const int absoluteTimeoutMs = 10000;

            const int minimumFreshSamples = 3;

            int startPosition = _context._currentIrZoom;

            long sequence = Interlocked.Read(ref _context._irLensStatusVersion);

            int freshSamples = 0;

            int matchedSamples = 0;

            bool movementObserved = false;

            Stopwatch timer = Stopwatch.StartNew();

            cancellationToken.ThrowIfCancellationRequested();

            bool sent = _context._controlCommandService.IrZoomGoPosition((short)rawTarget);

            ConsoleLogHelper.Command("IR ZOOM ABSOLUTE", $"COMMAND=0xB7 / STANDARD_TARGET={standardTarget} / " + $"COMMAND_RAW_TARGET={rawTarget} / START_RAW={startPosition} / SENT={sent}");

            if (!sent)
            {
                return null;
            }

            while (timer.ElapsedMilliseconds < absoluteTimeoutMs)
            {
                if (!await WaitForIrLensFeedbackAsync(sequence, cancellationToken))
                {
                    ConsoleLogHelper.Warning("IR ZOOM ABSOLUTE", $"RESULT=FEEDBACK_TIMEOUT / TARGET={rawTarget} / CURRENT={_context._currentIrZoom}");

                    return null;
                }

                sequence = Interlocked.Read(ref _context._irLensStatusVersion);

                freshSamples++;

                int current = _context._currentIrZoom;

                movementObserved |= Math.Abs(current - startPosition) > 1;

                matchedSamples = IsIrZoomTargetReached(rawTarget, current, standardTarget, tolerance, syncCalibration) ? matchedSamples + 1 : 0;

                if (matchedSamples >= 2)
                {
                    int settled = await WaitForIrLensSettledPositionAsync(true, cancellationToken);

                    bool reached = IsIrZoomTargetReached(rawTarget, settled, standardTarget, tolerance, syncCalibration);

                    if (syncCalibration && reached && standardTarget >= 0 && tolerance <= IrZoomTargetTolerance)
                    {
                        UpdateEnvironmentIrZoomEndpointCalibration(standardTarget, settled);
                    }

                    ConsoleLogHelper.State("IR ZOOM ABSOLUTE", $"RESULT={(reached ? "COMPLETED" : "SETTLED_OUTSIDE_TARGET")} / " + $"COMMAND_RAW_TARGET={rawTarget} / SETTLED_RAW={settled} / " + $"STANDARD_FINAL={NormalizeIrZoomControlPosition(settled, syncCalibration)} / " + $"FRESH_SAMPLES={freshSamples}");

                    return reached ? true : (bool?)null;
                }

                if (!movementObserved && timer.ElapsedMilliseconds >= noMovementWaitMs && freshSamples >= minimumFreshSamples)
                {
                    // 정지 명령 후에도 다음 실제 수신값이 그대로인지 확인한다.
                    // 지연 실행된 절대 명령과 연속 제어가 겹치지 않도록 한다.
                    if (!_context._controlCommandService.StopIrZoom() || !await WaitForIrLensFeedbackAsync(sequence, cancellationToken))
                    {
                        return null;
                    }

                    int afterStop = _context._currentIrZoom;

                    if (Math.Abs(afterStop - startPosition) > 1)
                    {
                        ConsoleLogHelper.Warning("IR ZOOM ABSOLUTE", $"RESULT=LATE_MOVEMENT / START={startPosition} / CURRENT={afterStop}");

                        return null;
                    }

                    ConsoleLogHelper.Warning("IR ZOOM ABSOLUTE", $"RESULT=NO_MOVEMENT_FALLBACK / TARGET={rawTarget} / " + $"CURRENT={afterStop} / FRESH_SAMPLES={freshSamples}");

                    return false;
                }
            }

            ConsoleLogHelper.Warning("IR ZOOM ABSOLUTE", $"RESULT=MOVED_BUT_TARGET_NOT_REACHED / TARGET={rawTarget} / " + $"CURRENT={_context._currentIrZoom} / FRESH_SAMPLES={freshSamples}");

            return null;
        }

        /// <summary>
        /// 2026-09-18: 화면/명령의 표준 좌표가 제공되면 장비 Raw 물리 끝점을 공통 정규화한 값으로 판정한다. Raw 좌표 전용 호출만
        /// 기존 허용 오차를 유지한다.
        /// </summary>
        internal bool IsIrZoomTargetReached(int target, int actual, int standardTarget = -1, int tolerance = IrZoomTargetTolerance, bool syncCalibration = false)
        {
            if (standardTarget >= 0)
            {
                return Math.Abs(NormalizeIrZoomControlPosition(actual, syncCalibration) - standardTarget) <= tolerance;
            }

            return Math.Abs(actual - target) <= tolerance;
        }

        /// <summary>
        /// IR 렌즈의 새 유효 상태 응답이 들어올 때까지 제한 시간 안에서 기다린다.
        /// </summary>
        internal async Task<bool> WaitForIrLensFeedbackAsync(long afterSequence, CancellationToken cancellationToken)
        {
            Stopwatch timeout = Stopwatch.StartNew();

            while (timeout.ElapsedMilliseconds < LensSyncFeedbackTimeoutMs)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (Interlocked.Read(ref _context._irLensStatusVersion) > afterSequence)
                {
                    return true;
                }

                await Task.Delay(20, cancellationToken);
            }

            return false;
        }

        /// <summary>
        /// IR 렌즈의 새 응답에서 이동이 멈춘 정착 위치를 확인한다.
        /// </summary>
        internal async Task<int> WaitForIrLensSettledPositionAsync(bool zoom, CancellationToken cancellationToken)
        {
            long sequence = Interlocked.Read(ref _context._irLensStatusVersion);

            int previous = zoom ? _context._currentIrZoom : _context._currentIrFocus;

            int stable = 0;

            Stopwatch timeout = Stopwatch.StartNew();

            int settleTimeoutMs = zoom ? IrZoomSettleTimeoutMs : IrFocusSyncSettleTimeoutMs;

            int requiredStableSamples = zoom ? IrZoomStableSampleCount : IrFocusSyncStableSampleCount;

            while (timeout.ElapsedMilliseconds < settleTimeoutMs)
            {
                if (!await WaitForIrLensFeedbackAsync(sequence, cancellationToken))
                {
                    break;
                }

                sequence = Interlocked.Read(ref _context._irLensStatusVersion);

                int current = zoom ? _context._currentIrZoom : _context._currentIrFocus;

                stable = Math.Abs(current - previous) <= 1 ? stable + 1 : 0;

                previous = current;

                if (stable >= requiredStableSamples)
                {
                    break;
                }
            }

            if (zoom)
            {
                return _context._currentIrZoom;
            }
            else
            {
                return _context._currentIrFocus;
            }
        }

        /// <summary>
        /// 새 EO 렌즈 응답이 목표 허용 범위에 연속 도달할 때까지 기다린다.
        /// </summary>
        internal async Task<bool> WaitForEnvironmentEoLensTargetAsync(int target, bool zoom, long afterSequence, CancellationToken cancellationToken)
        {
            Stopwatch timeout = Stopwatch.StartNew();

            int stable = 0;

            while (timeout.ElapsedMilliseconds < RooftopZoomSyncTimeoutMs)
            {
                cancellationToken.ThrowIfCancellationRequested();

                long currentSequence = Interlocked.Read(ref _context._eoLensStatusVersion);

                if (currentSequence > afterSequence)
                {
                    afterSequence = currentSequence;

                    int actual = zoom ? _context._currentEoZoom : _context._currentEoFocus;

                    stable = Math.Abs(actual - target) <= LensSyncTargetTolerance ? stable + 1 : 0;

                    if (stable >= 2)
                    {
                        return true;
                    }
                }

                await Task.Delay(25, cancellationToken);
            }

            return false;
        }

        /// <summary>
        /// 표준 줌 위치 0~1000을 CTEC EO 줌 원시 위치 범위로 환산한다.
        /// </summary>
        internal static int ConvertStandardZoomToCtecRaw(int standardPosition)
        {
            int safePosition = Math.Max(0, Math.Min(1000, standardPosition));

            return (int)Math.Round(safePosition * CtecEoZoomPositionMax / 1000.0);
        }

        /// <summary>
        /// IR Focus를 목표 Position으로 이동한다. 사용 명령: - Direct : Command2 0x2B / Position Big
        /// Endian - Near Start : Command2 0x31 / Data1 0x03 - Far Start : Command2 0x31 /
        /// Data1 0x04 - Focus Stop : Command2 0x31 / Data1 0x05 상태 확인: - Function 0x07의
        /// IR Focus Position 0 ~ 1000 0x2B 직접 절대이동을 우선 시도한다. WEB은 명시적인 미지원 응답에만 기존 제어로
        /// 전환하며 단순 지연·미도달에는 명령을 겹쳐 보내지 않는다.
        /// </summary>
        internal async Task<bool> MoveIrFocusToPositionAsync(int targetPosition, CancellationToken cancellationToken, int tolerance = IrFocusSyncTolerance, bool explicitAbsolute = false)
        {
            if (targetPosition < 0 || targetPosition > 1000)
            {
                ConsoleLogHelper.Warning("IR FOCUS SYNC", "RESULT=INVALID_TARGET");
                return false;
            }

            if (explicitAbsolute && _context.IsSelectedControlAgentWebAgent)
                return await TryMoveIrFocusDirectAsync(targetPosition, 1, cancellationToken, true) == true;
            if (!await _context.EnsurePresetIrLensFeedbackAsync(false, cancellationToken))
                return false;
            if (_context.IsSelectedControlAgentWebAgent && Math.Abs(targetPosition - _context._currentIrFocus) <= tolerance)
            {
                int? settled = await WaitForIrFocusStableEvidenceAsync(cancellationToken);
                return settled.HasValue && Math.Abs(settled.Value - targetPosition) <= tolerance;
            }

            if (!_context.IsSelectedControlAgentWebAgent)
            {
                // 이전 수동/절대 명령의 잔여 움직임을 새 명령의 반대 방향 응답으로 판단하지 않는다.
                string initialSession = _context._connectionSessionId;
                int? initialSettled = await WaitForIrFocusStableEvidenceAsync(cancellationToken);
                if (initialSession != _context._connectionSessionId)
                    throw new OperationCanceledException("LA IR Focus 준비 중 연결 세션이 변경되었습니다.");
                if (!initialSettled.HasValue)
                    return false;
                if (Math.Abs(initialSettled.Value - targetPosition) <= tolerance)
                    return true;
            }

            bool? directResult = await TryMoveIrFocusDirectAsync(targetPosition, tolerance, cancellationToken);
            if (directResult == true)
            {
                return true;
            }

            // 통신 단절·역방향·정지 미확인은 연속 명령으로 덮지 않는다.
            if (!directResult.HasValue)
                return false;
            // WEB의 지연·미도달을 미지원으로 간주해 연속 명령을 겹쳐 보내지 않는다.
            if (_context.IsSelectedControlAgentWebAgent && _irFocusDirectUnavailableSession != _context._connectionSessionId)
                return false;
            var planner = new IrFocusMotionPlanner();
            var total = Stopwatch.StartNew();
            int previousDirection = 0;
            int consecutiveNoMovement = 0;
            // 2026-10-16: 축별 유효 응답과 정지 후 정착값만 사용한다.
            // 옛 0x28 시험 경로·시간 추정·움직임만으로 성공하는 판정은 사용하지 않는다.
            for (int phase = 1; phase <= 20 && total.ElapsedMilliseconds < IrFocusSyncTimeoutMs; phase++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                int start = _context._currentIrFocus;
                int error = Math.Abs(targetPosition - start);
                if (error <= tolerance)
                {
                    int? stable = await WaitForIrFocusStableEvidenceAsync(cancellationToken);
                    if (!stable.HasValue)
                        return false;
                    if (Math.Abs(stable.Value - targetPosition) <= tolerance)
                    {
                        ConsoleLogHelper.State("IR FOCUS SYNC", $"RESULT=COMPLETED / TARGET={targetPosition} / ACTUAL={stable.Value} / TOLERANCE={tolerance} / ELAPSED_MS={total.ElapsedMilliseconds}");
                        return true;
                    }

                    continue;
                }

                int direction = targetPosition > start ? 1 : -1;
                if (previousDirection != 0 && direction != previousDirection && !planner.TryReverse())
                {
                    ConsoleLogHelper.Warning("IR FOCUS SYNC", $"RESULT=REVERSAL_LIMIT / TARGET={targetPosition} / ACTUAL={start}");
                    return false;
                }

                previousDirection = direction;
                bool pulse = planner.UsePulse(error, tolerance);
                int duration = pulse ? planner.PulseMilliseconds(error) : planner.CoarseMilliseconds(error, tolerance);
                long focusTicks = _context.PresetLensTicks(true, false);
                bool started = direction < 0 ? _context._controlCommandService.StartIrFocusNear() : _context._controlCommandService.StartIrFocusFar();
                if (!started)
                    return false;
                var active = Stopwatch.StartNew();
                long lastFeedbackMs = 0;
                int previous = start;
                int stoppedPosition = start;
                long commandOnMs = 0;
                bool stopped = false;
                bool feedbackLost = false;
                bool wrongDirection = false;
                int freshSamples = 0;
                ConsoleLogHelper.Command("IR FOCUS SYNC", $"PHASE={phase} / MODE={(pulse ? "FINE_PULSE" : "COARSE")} / TARGET={targetPosition} / START={start} / DIRECTION={(direction < 0 ? "NEAR" : "FAR")} / MAX_ON_MS={duration}");
                try
                {
                    while (active.ElapsedMilliseconds < duration && total.ElapsedMilliseconds < IrFocusSyncTimeoutMs)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        long nowTicks = _context.PresetLensTicks(true, false);
                        if (nowTicks > focusTicks && _context.HasFreshPresetLens(true, false))
                        {
                            focusTicks = nowTicks;
                            long nowMs = active.ElapsedMilliseconds;
                            int current = _context._currentIrFocus;
                            int delta = current - previous;
                            planner.ObserveFeedback(delta, nowMs - lastFeedbackMs);
                            freshSamples++;
                            lastFeedbackMs = nowMs;
                            previous = current;
                            if (delta * direction < -Math.Max(2, tolerance))
                            {
                                wrongDirection = true;
                                break;
                            }

                            int remaining = (targetPosition - current) * direction;
                            if (remaining <= 0 || (!pulse && current != start && remaining <= planner.StopLead(tolerance)))
                                break;
                            if (!pulse && current != start)
                            {
                                int predicted = planner.CoarseMilliseconds(Math.Max(1, remaining), tolerance);
                                duration = Math.Min(duration, (int)nowMs + predicted);
                            }
                        }

                        if (active.ElapsedMilliseconds - lastFeedbackMs >= 3000)
                        {
                            feedbackLost = true;
                            break;
                        }

                        await Task.Delay(10, cancellationToken);
                    }

                    stoppedPosition = _context._currentIrFocus;
                }
                finally
                {
                    commandOnMs = active.ElapsedMilliseconds;
                    stopped = _context._controlCommandService.StopIrFocus();
                }

                if (!stopped || feedbackLost || wrongDirection)
                {
                    ConsoleLogHelper.Warning("IR FOCUS SYNC", $"RESULT={(wrongDirection ? "DIRECTION_MISMATCH" : feedbackLost ? "FOCUS_FEEDBACK_TIMEOUT" : "STOP_SEND_FAILED")} / TARGET={targetPosition} / ACTUAL={_context._currentIrFocus}");
                    return false;
                }

                int? settled = await WaitForIrFocusStableEvidenceAsync(cancellationToken);
                if (!settled.HasValue)
                    return false;
                planner.ObserveSettled(start, stoppedPosition, settled.Value, commandOnMs, pulse);
                int finalError = Math.Abs(settled.Value - targetPosition);
                ConsoleLogHelper.State("IR FOCUS SYNC", $"PHASE={phase} / MODE={(pulse ? "FINE_PULSE" : "COARSE")} / TARGET={targetPosition} / STOP_POSITION={stoppedPosition} / SETTLED={settled.Value} / ERROR={finalError} / ON_MS={commandOnMs} / TOTAL_PHASE_MS={active.ElapsedMilliseconds} / RATE={planner.UnitsPerSecond:F1} / LEAD={planner.StopLead(tolerance)} / REVERSALS={planner.ReversalCount} / FRESH_SAMPLES={freshSamples}");
                if (finalError <= tolerance)
                {
                    ConsoleLogHelper.State("IR FOCUS SYNC", $"RESULT=COMPLETED / TARGET={targetPosition} / ACTUAL={settled.Value} / TOLERANCE={tolerance} / ELAPSED_MS={total.ElapsedMilliseconds}");
                    return true;
                }

                consecutiveNoMovement = Math.Abs(settled.Value - start) <= 1 ? consecutiveNoMovement + 1 : 0;
                if (consecutiveNoMovement > 0)
                    planner.ObserveNoMovement();
                if (consecutiveNoMovement >= 4)
                {
                    ConsoleLogHelper.Warning("IR FOCUS SYNC", "RESULT=NO_MOVEMENT / no automatic absolute fallback");
                    return false;
                }
            }

            ConsoleLogHelper.Warning("IR FOCUS SYNC", $"RESULT=INCOMPLETE / TARGET={targetPosition} / ACTUAL={_context._currentIrFocus} / ELAPSED_MS={total.ElapsedMilliseconds}");
            return false;
        }

        /// <summary>
        /// 장비별 IR 초점 절대 명령을 시도하고 새 응답으로 도달·정지 여부를 확인한다.
        /// </summary>
        internal async Task<bool?> TryMoveIrFocusDirectAsync(int targetPosition, int tolerance, CancellationToken token, bool explicitAbsolute = false)
        {
            int startPosition = _context._currentIrFocus;
            string session = _context._connectionSessionId;
            if ((!explicitAbsolute && Math.Abs(targetPosition - startPosition) <= tolerance) || _irFocusDirectUnavailableSession == session)
            {
                return false;
            }

            long feedbackTicks = _context.PresetLensTicks(true, false);
            // LA 목표는 이미 GUI에서 한 번 반전된 수신 Raw 좌표다. 송신 직전에 다시 반전하지 않는다.
            // WEB의 기존 표준 좌표 경로는 유지한다.
            int wirePosition = _context.IsSelectedControlAgentWebAgent ? _context.ConvertIrFocusStandardToStatusPosition(targetPosition) : targetPosition;
            token.ThrowIfCancellationRequested();
            bool sent = _context.IsSelectedControlAgentWebAgent ? _context._controlCommandService.IrFocusGoPosition((short)wirePosition) : _context._controlCommandService.LaIrFocusGoPosition(wirePosition);
            if (!sent)
            {
                return null;
            }

            ConsoleLogHelper.Command("IR FOCUS DIRECT", $"AGENT={(_context.IsSelectedControlAgentWebAgent ? "WEB" : "LA")} / CMD2=0x2B / GUI_TARGET={_context.ConvertIrFocusStandardToStatusPosition(targetPosition)} / TARGET={targetPosition} / WIRE_POSITION={wirePosition} / START={startPosition}");
            var timer = Stopwatch.StartNew();
            long lastFreshMs = 0;
            long lastMovementMs = 0;
            int previous = startPosition;
            int expectedDirection = Math.Sign(targetPosition - startPosition);
            bool moved = false;
            bool completed = false;
            bool wrongDirection = false;
            bool stopSent = true;
            try
            {
                while (timer.ElapsedMilliseconds < IrFocusSyncTimeoutMs)
                {
                    if (session != _context._connectionSessionId || _irFocusDirectUnavailableSession == session)
                    {
                        break;
                    }

                    token.ThrowIfCancellationRequested();
                    long nextTicks = _context.PresetLensTicks(true, false);
                    if (nextTicks > feedbackTicks && _context.HasFreshPresetLens(true, false))
                    {
                        feedbackTicks = nextTicks;
                        lastFreshMs = timer.ElapsedMilliseconds;
                        int current = _context._currentIrFocus;
                        int delta = current - previous;
                        previous = current;
                        if (delta != 0)
                            lastMovementMs = timer.ElapsedMilliseconds;
                        moved |= Math.Abs(current - startPosition) > 1;
                        if (delta != 0 && Math.Sign(delta) != expectedDirection && Math.Abs(delta) > Math.Max(2, tolerance))
                        {
                            wrongDirection = true;
                            ConsoleLogHelper.Warning("IR FOCUS DIRECT", $"RESULT=DIRECTION_MISMATCH / TARGET={targetPosition} / ACTUAL={current}");
                            break;
                        }

                        if (Math.Abs(current - targetPosition) <= tolerance)
                        {
                            int? stable = await WaitForIrFocusStableEvidenceAsync(token);
                            if (stable.HasValue && Math.Abs(stable.Value - targetPosition) <= tolerance)
                            {
                                ConsoleLogHelper.State("IR FOCUS DIRECT", $"RESULT=COMPLETED / TARGET={targetPosition} / ACTUAL={stable.Value} / ELAPSED_MS={timer.ElapsedMilliseconds}");
                                completed = true;
                                return true;
                            }
                        }
                    }

                    if ((!moved && timer.ElapsedMilliseconds >= (_context.IsSelectedControlAgentWebAgent ? 5000 : 1800)) || (moved && timer.ElapsedMilliseconds - lastMovementMs >= 2500) || (moved && timer.ElapsedMilliseconds - lastFreshMs >= 2500))
                    {
                        break;
                    }

                    await Task.Delay(20, token);
                }
            }
            finally
            {
                // 취소·예외·미지원 전환 때도 같은 연결에 정지 명령을 보장한다.
                if (!completed && session == _context._connectionSessionId)
                {
                    stopSent = _context._controlCommandService.StopIrFocus();
                }
            }

            token.ThrowIfCancellationRequested();
            if (session != _context._connectionSessionId)
            {
                throw new OperationCanceledException("IR Focus 이동 중 연결 세션이 변경되었습니다.");
            }

            if (_context.IsSelectedControlAgentWebAgent)
            {
                ConsoleLogHelper.Warning("IR FOCUS DIRECT", "목표 미도달 / 절대 요청을 미지원으로 저장하지 않습니다.");
                if (explicitAbsolute || _irFocusDirectUnavailableSession != session)
                    return false;
            }
            else
            {
                // LA의 일시적 실패를 세션 전체의 절대명령 미지원으로 저장하지 않는다.
                if (!stopSent || wrongDirection)
                    return null;
                int? stopped = await WaitForIrFocusStableEvidenceAsync(token);
                if (!stopped.HasValue)
                    return null;
                if (session != _context._connectionSessionId)
                    throw new OperationCanceledException("LA IR Focus 정착 확인 중 연결 세션이 변경되었습니다.");
                if (Math.Abs(stopped.Value - targetPosition) <= tolerance)
                    return true;
            }

            await Task.Delay(120, token);
            ConsoleLogHelper.Warning("IR FOCUS DIRECT", $"RESULT=FALLBACK_0x31 / TARGET={targetPosition} / ACTUAL={_context._currentIrFocus} / MOVED={moved} / ELAPSED_MS={timer.ElapsedMilliseconds}");
            return false;
        }

        /// <summary>
        /// 2026-10-16: 정지 이후 새 Focus 유효 응답 3개가 안정된 경우에만 위치를 반환한다. Zoom 응답만 갱신되거나 수신이 끊기면
        /// 정착 완료로 판단하지 않는다.
        /// </summary>
        internal async Task<int?> WaitForIrFocusStableEvidenceAsync(CancellationToken token)
        {
            long ticks = _context.PresetLensTicks(true, false);

            int previous = _context._currentIrFocus;

            int stableSamples = 0;

            long stableStartedMs = 0;

            var timer = Stopwatch.StartNew();

            while (timer.ElapsedMilliseconds < 3500)
            {
                token.ThrowIfCancellationRequested();

                long nextTicks = _context.PresetLensTicks(true, false);

                if (nextTicks > ticks && _context.HasFreshPresetLens(true, false))
                {
                    ticks = nextTicks;

                    int current = _context._currentIrFocus;

                    if (Math.Abs(current - previous) <= 1)
                    {
                        stableSamples++;
                    }
                    else
                    {
                        stableSamples = 0;
                        stableStartedMs = timer.ElapsedMilliseconds;
                    }

                    previous = current;

                    if (stableSamples >= 3 && timer.ElapsedMilliseconds >= 800 && timer.ElapsedMilliseconds - stableStartedMs >= 500)
                    {
                        return current;
                    }
                }

                await Task.Delay(20, token);
            }

            ConsoleLogHelper.Warning("IR FOCUS SYNC", $"RESULT=SETTLE_NOT_CONFIRMED / ACTUAL={_context._currentIrFocus}");

            return null;
        }

        /// <summary>
        /// 옥상장비 CTEC EO Focus를 목표 Raw Position으로 직접 이동한다. VISCA Focus Direct Position 명령을
        /// 한 번만 송신하고, TCP Port 9000 Focus Position Inquiry 응답은 목표 도착 확인 용도로만 사용한다.
        /// </summary>
        internal async Task<bool> MoveRooftopEoFocusToRawPositionAsync(RtspSourceOption ctecSource, int targetRawPosition, CancellationToken cancellationToken)
        {
            if (ctecSource == null)
            {
                return false;
            }

            int safeTargetRawPosition = Math.Max(0, Math.Min(CtecEoFocusPositionMax, targetRawPosition));

            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                ConsoleLogHelper.PrintLine();

                Console.WriteLine("[ROOFTOP FOCUS SYNC] DIRECT START");

                Console.WriteLine($"[ROOFTOP FOCUS SYNC] TARGET : " + $"{safeTargetRawPosition}");

                ConsoleLogHelper.PrintLine();

                bool commandResult = await _context._ctecCameraCommandService.MoveFocusPositionAsync(ctecSource.ControlIp, ctecSource.ControlUserName, ctecSource.ControlPassword, ctecSource.UseHttps, (ushort)safeTargetRawPosition);

                if (!commandResult)
                {
                    Console.WriteLine("[ROOFTOP FOCUS SYNC] " + "DIRECT COMMAND FAILED");

                    ConsoleLogHelper.PrintLine();

                    return false;
                }

                Stopwatch timeout = Stopwatch.StartNew();

                while (timeout.ElapsedMilliseconds < RooftopFocusSyncTimeoutMs)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    int? currentPosition = await _context.RequestAndWaitCtecEoPositionAsync(ContinuousMoveType.EoFocus, ctecSource, cancellationToken);

                    if (currentPosition.HasValue)
                    {
                        int error = Math.Abs(currentPosition.Value - safeTargetRawPosition);

                        Console.WriteLine("[ROOFTOP FOCUS SYNC] DIRECT CHECK " + $"/ POSITION={currentPosition.Value} " + $"/ TARGET={safeTargetRawPosition} " + $"/ ERROR={error}");

                        if (error <= RooftopFocusSyncTolerance)
                        {
                            Console.WriteLine("[ROOFTOP FOCUS SYNC] " + "DIRECT COMPLETED");

                            ConsoleLogHelper.PrintLine();

                            return true;
                        }
                    }

                    await Task.Delay(RooftopFocusSyncInquiryIntervalMs, cancellationToken);
                }

                Console.WriteLine("[ROOFTOP FOCUS SYNC] DIRECT TIMEOUT");

                ConsoleLogHelper.PrintLine();

                return false;
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("[ROOFTOP FOCUS SYNC] DIRECT CANCELED");

                ConsoleLogHelper.PrintLine();

                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine("[ROOFTOP FOCUS SYNC] DIRECT ERROR : " + ex.Message);

                ConsoleLogHelper.PrintLine();

                return false;
            }
        }

        /// <summary>
        /// 진행 중인 Focus Sync 확인 작업과 장비 이동을 정지한다.
        /// </summary>
        internal async Task StopFocusSyncAsync(bool stopEo = true, bool stopIr = true)
        {
            CancellationTokenSource cts = _context._rooftopFocusSyncCts;

            _context._rooftopFocusSyncCts = null;

            if (cts != null)
            {
                cts.Cancel();
            }

            // IR Focus Sync는 Near / Far 연속 이동 방식이므로
            // 사용자 STOP 또는 취소 시 반드시 Focus Stop을 송신한다.
            //
            // Pan / Tilt 오동작을 발생시킨 0x28 명령은 사용하지 않는다.
            //
            if (stopIr)
            {
                _context._controlCommandService.StopIrFocus();
            }

            RtspSourceOption ctecSource = _context._connectedEoCtecSource;

            if (stopEo && ctecSource != null)
            {
                await _context._ctecCameraCommandService.StopFocusAsync(ctecSource.ControlIp, ctecSource.ControlUserName, ctecSource.ControlPassword, ctecSource.UseHttps);
            }

            _context.FocusSyncStatusText = "STOPPED";
        }

        /// <summary>
        /// Web Agent 표준 Focus Position 0 ~ 1000을 CTEC EO Focus Raw Position 0 ~ 32768로
        /// 변환한다.
        /// </summary>
        internal static int ConvertStandardFocusToCtecRaw(int standardPosition)
        {
            int safePosition = Math.Max(0, Math.Min(1000, standardPosition));

            return (int)Math.Round(safePosition * CtecEoFocusPositionMax / 1000.0);
        }

        #endregion

    }
}
