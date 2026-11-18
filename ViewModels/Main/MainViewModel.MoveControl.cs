using System.Threading.Tasks;

namespace OpenCvWpfTracking.ViewModels.Main
{
    /// <summary>
    /// Home/Zero, Absolute 이동, Lens Position과 PRESET L/W 제어를 관리한다.
    ///
    /// MainViewModel을 기능 영역별로 나눈 partial class이다.
    /// 모든 partial 파일은 실행 시 하나의 MainViewModel 타입으로 합쳐진다.
    /// </summary>
    public partial class MainViewModel
    {
        internal const int WebAgentZeroOverlayMinimumMilliseconds = 1200;

        /// <summary>
        /// 이동 제어 입력값을 VertiportNexus 기본값으로 초기화한다.
        ///
        /// Pan Absolute  : 0 (-180 ~ 180)
        /// Tilt Absolute : 0 (-90 ~ 90)
        /// Zoom Position : 0
        /// Zoom Ratio    : 1.0
        /// Focus Position: 0
        /// </summary>
        private void ResetMoveControlInput()
        {
            _moveControlFeature.ResetMoveControlInput();
        }

        /// <summary>
        /// Pan Absolute 이동 요청
        ///
        /// 입력 범위:
        /// -180 ~ 180
        ///
        /// 기존처럼 현재각과 목표각을 이용해 앱에서
        /// 최단거리 또는 원점 통과 경로를 직접 계산하지 않는다.
        ///
        /// UI RadioButton에서 선택한 PAN TURN MODE를
        /// Pelco-D 확장 명령 0x4D로 먼저 설정한 뒤,
        /// 목표 Pan 각도를 위치 이동 명령 0x45로 전송한다.
        ///
        /// SHORT:
        /// 0x4D / Data1 0x02
        ///
        /// VIA 0:
        /// 0x4D / Data1 0x01
        /// </summary>
        /// <summary>
        /// 현재 설정된 Pan / Tilt 좌표계의 0°, 0° 위치로 절대 이동한다.
        ///
        /// 기존 LA 0xB1 명령은 기계식 Homing(StartHoming) 명령이므로
        /// 카메라가 홈 센서 방향으로 이동한 뒤 해당 위치를 원점으로 잡는다.
        ///
        /// UI의 HOME POSITION은 사용자가 설정한 좌표 원점으로 복귀하는 기능이므로
        /// Pan 0°(0x45), Tilt 0°(0x47) 절대 이동 명령을 사용한다.
        /// </summary>
        private Task MoveHomePositionAsync()
        {
            return _moveControlFeature.MoveHomePositionAsync();
        }

        /// <summary>
        /// 현재 Pan Encoder 위치를 즉시 0으로 설정한다.
        /// MCB Set0 명령: MO=0;PX=0;MO=1;
        /// </summary>
        private Task SetPanZeroAsync()
        {
            return _moveControlFeature.SetPanZeroAsync();
        }

        /// <summary>
        /// 현재 Tilt Encoder 위치를 즉시 0으로 설정한다.
        /// MCB Set0 명령: MO=0;PX=0;MO=1;
        /// </summary>
        private Task SetTiltZeroAsync()
        {
            return _moveControlFeature.SetTiltZeroAsync();
        }

        /// <summary>
        /// [PAN] / [TILT] Absolute 위치 이동 정지
        ///
        /// 방향 버튼 중앙의 STOP은 연속 속도 이동(0x00)을 정지한다.
        /// Absolute 이동은 위치 이동 전용 정지 명령인 0x4F를 사용해야 하므로,
        /// Pan 정지(Data1=0x01)와 Tilt 정지(Data1=0x02)를 각각 송신한다.
        /// </summary>
        private void StopAbsoluteMove()
        {
            _moveControlFeature.StopAbsoluteMove();
        }

        /// <summary>
        /// MovePanAbsoluteFromInputAsync 이동 함수.
        /// </summary>
        private Task MovePanAbsoluteFromInputAsync()
        {
            return _moveControlFeature.MovePanAbsoluteFromInputAsync();
        }

        /// <summary>
        /// Tilt Absolute 이동 요청
        /// </summary>
        private void MoveTiltAbsoluteFromInput()
        {
            _moveControlFeature.MoveTiltAbsoluteFromInput();
        }

        /// <summary>
        /// PRESET 1 (LA TEST) 등록.
        ///
        /// LA 실제 구현 순서:
        /// 0x19 ID -> 0x91 Pan -> 0x93 Tilt -> 0x95 Zoom
        /// -> 0x97 Focus + AddPreset
        /// </summary>
        private void AddOrUpdateLaPresetPoint()
        {
            _moveControlFeature.AddOrUpdateLaPresetPoint();
        }

        /// <summary>
        /// PRESET 1 선택 지점으로 WPF 저장값 기준 PTZF 전체를 직접 복원한다.
        ///
        /// LA 0x05 한 번에 의존하지 않고 Pan, Tilt, EO/IR Zoom,
        /// EO/IR Focus를 현재 프로젝트의 실제 제어 경로로 각각 실행한다.
        /// </summary>
        private Task MoveToSelectedLaPresetPointAsync()
        {
            return _moveControlFeature.MoveToSelectedLaPresetPointAsync();
        }

        internal static double GetShortestPanDifference(double currentPan, double targetPan)
        {
            return MoveControlFeature.GetShortestPanDifference(currentPan, targetPan);
        }

        internal static int ParsePresetPosition(string text, int fallback)
        {
            return MoveControlFeature.ParsePresetPosition(text, fallback);
        }

        /// <summary>
        /// ClearAllLaPresetPoints 동작 수행 함수.
        /// </summary>
        private void ClearAllLaPresetPoints()
        {
            _moveControlFeature.ClearAllLaPresetPoints();
        }

        /// <summary>
        /// 2026-08-28: PRESET L의 선택 항목만 자동 순회 목록에서 제거한다.
        /// LA Agent는 개별 삭제 명령을 제공하지 않으므로 장비 전체 프리셋은
        /// 유지하고, 현재 Viewer의 선택 슬롯만 제거한다.
        /// </summary>
        private void DeleteSelectedLaPresetPoint()
        {
            _moveControlFeature.DeleteSelectedLaPresetPoint();
        }

        /// <summary>
        /// PRESET 1 저장 목록을 WPF가 직접 P01 -> P02 -> ... 순회한다.
        /// 각 지점마다 PTZF 전체 직접 복원 루틴을 사용한다.
        /// </summary>
        private Task StartLaPresetScanAsync()
        {
            return _moveControlFeature.StartLaPresetScanAsync();
        }

        /// <summary>
        /// UpdateLaPresetScan 갱신 함수.
        /// </summary>
        private void UpdateLaPresetScan()
        {
            _moveControlFeature.UpdateLaPresetScan();
        }

        /// <summary>
        /// StopLaPresetScan 중지 함수.
        /// </summary>
        private void StopLaPresetScan()
        {
            _moveControlFeature.StopLaPresetScan();
        }

        /// <summary>
        /// 단일 MOVE TO PRESET 이동만 즉시 정지한다.
        /// AUTO SCAN 반복 상태에는 영향을 주지 않으며,
        /// Pan/Tilt 위치 이동 정지 명령(0x4F)을 각각 송신한다.
        /// </summary>
        private void StopLaPresetMove()
        {
            _moveControlFeature.StopLaPresetMove();
        }

        /// <summary>
        /// AddOrUpdatePresetPoint 동작 수행 함수.
        /// </summary>
        private void AddOrUpdatePresetPoint()
        {
            _moveControlFeature.AddOrUpdatePresetPoint();
        }

        /// <summary>
        /// 선택 슬롯의 프리셋을 제거한다.
        ///
        /// TORUSS 명령:
        /// Command2 = 0x05
        /// Data1    = 0x00
        /// Data2    = Preset Number (1 ~ 100)
        /// </summary>
        private void DeletePresetPoint()
        {
            _moveControlFeature.DeletePresetPoint();
        }

        /// <summary>
        /// 2026-08-28: PRESET W 전체 삭제를 각 슬롯의 표준 CLEAR PRESET 명령으로 수행한다.
        /// 일부 송신 실패 시 성공한 항목만 화면 목록에서 제거하고 실패 개수를 알린다.
        /// </summary>
        private void ClearAllPresetPoints()
        {
            _moveControlFeature.ClearAllPresetPoints();
        }

        /// <summary>
        /// ComboBox에서 선택한 프리셋으로 이동한다.
        ///
        /// 현재 UI의 PAN TURN MODE를 먼저 송신한 뒤
        /// 프리셋 이동 명령을 송신한다.
        ///
        /// TORUSS 명령:
        /// Command2 = 0x07
        /// Data1    = 0x00
        /// Data2    = Preset Number (1 ~ 100)
        /// </summary>
        private void MoveToSelectedPresetPoint()
        {
            _moveControlFeature.MoveToSelectedPresetPoint();
        }

        /// <summary>
        /// 프리셋 오토 스캔을 시작한다.
        ///
        /// TORUSS 명령:
        /// Command2 = 0x99
        /// Data1    = Speed (1 ~ 60)
        /// Data2    = Delay (1 ~ 60)
        /// </summary>
        private void StartPresetScan()
        {
            _moveControlFeature.StartPresetScan();
        }

        /// <summary>
        /// 실행 중인 스캔의 속도 / 정지시간을 변경한다.
        ///
        /// TORUSS 명령:
        /// Command2 = 0x9D
        /// Data1    = Speed (1 ~ 60)
        /// Data2    = Delay (1 ~ 60)
        /// </summary>
        private void UpdatePresetScan()
        {
            _moveControlFeature.UpdatePresetScan();
        }

        /// <summary>
        /// 프리셋 오토 스캔을 정지한다.
        ///
        /// TORUSS 명령:
        /// Command2 = 0x9B
        /// Data1    = 0x00
        /// Data2    = 0x00
        ///
        /// Data2 = 0x01인 프리셋 데이터 초기화는
        /// 오조작 위험이 있으므로 UI에 제공하지 않는다.
        /// </summary>
        private void StopPresetScan()
        {
            _moveControlFeature.StopPresetScan();
        }

        /// <summary>
        /// 단일 MOVE TO PRESET 이동만 즉시 정지한다.
        /// AUTO SCAN 반복 상태에는 영향을 주지 않으며,
        /// Pan/Tilt 위치 이동 정지 명령(0x4F)을 각각 송신한다.
        /// </summary>
        private void StopPresetMove()
        {
            _moveControlFeature.StopPresetMove();
        }

        /// <summary>
        /// 잠금 Overlay의 공통 STOP 버튼에서 현재 실행 중인
        /// PRESET L 또는 PRESET W AUTO SCAN을 정지한다.
        /// </summary>
        private void StopActivePresetScan()
        {
            _moveControlFeature.StopActivePresetScan();
        }

        /// <summary>
        /// Zoom Position 입력값 적용
        /// </summary>
        private Task SetZoomPositionFromInputAsync()
        {
            return _moveControlFeature.SetZoomPositionFromInputAsync();
        }

        /// <summary>
        /// Zoom Ratio 입력값 적용
        ///
        /// 입력값은 EO 광학 배율 1.0 ~ 90.0배 기준이다.
        /// EO 배율을 공통 Position 0 ~ 1000으로 변환한 뒤
        /// EO와 IR에 동일한 진행률을 적용한다.
        ///
        /// EO 1.0배  -> Position 0    -> IR 1.0배
        /// EO 90.0배 -> Position 1000 -> IR 9.0배
        /// </summary>
        private Task SetZoomRatioFromInputAsync()
        {
            return _moveControlFeature.SetZoomRatioFromInputAsync();
        }

        /// <summary>
        /// Focus Position 입력값 적용
        ///
        /// 표준 방향:
        /// 0    = Far
        /// 1000 = Near
        ///
        /// IR Raw 방향:
        /// 1000 = Far
        /// 0    = Near
        /// </summary>
        private Task SetFocusPositionFromInputAsync()
        {
            return _moveControlFeature.SetFocusPositionFromInputAsync();
        }

        internal static double? RoundNullableAngle(double? angle)
        {
            return MoveControlFeature.RoundNullableAngle(angle);
        }

        internal static double Clamp(double value, double min, double max)
        {
            return MoveControlFeature.Clamp(value, min, max);
        }

    }
}
