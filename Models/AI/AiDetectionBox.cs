namespace OpenCvWpfTracking.Models.AI
{
    /// <summary>
    /// [AI Detector] 객체 1개 [Bounding Box] 정보
    /// </summary>
    public class AiDetectionBox
    {
        /// <summary>
        /// [AI Detector] 객체 고유 ID
        /// </summary>
        public long ObjectId { get; set; }

        /// <summary>
        /// 2026-08-26: 현재 프레임에서 화면에 표시되는 AI 탐지 순번이다.
        /// 필터링 완료 후 1부터 부여하여 BBox 좌측 상단 식별표에 사용한다.
        /// </summary>
        public int DisplayOrder { get; set; }

        /// <summary>
        /// 2026-08-26: 이벤트 목록의 고정 EventId와 BBox를 연결하는 식별값이다.
        /// </summary>
        public int DetectionEventId { get; set; }

        /// <summary>
        /// [AI Detector] 클래스 인덱스
        /// 
        /// 0 = [Drone]
        /// 1 = [ONNX]
        /// 2 = [ClassIndex]
        /// </summary>
        public int ClassIndex { get; set; }

        /// <summary>
        /// 2026-08-31: Agent의 모델별 클래스 목록과 RTSP Mapping을 이용해
        /// 해석한 실제 클래스명이다. 조회 실패 시 비워 두고 Class Index를 표시한다.
        /// </summary>
        public string ResolvedClassName { get; set; }

        /// <summary>
        /// 2026-08-31: 클래스명 해석에 사용한 ONNX/HEF 파일명이다.
        /// 로그와 현장 Mapping 확인용으로만 사용한다.
        /// </summary>
        public string ModelFileName { get; set; }

        /// <summary>
        /// [AI Detector] 객체 탐지 신뢰도
        /// 
        /// 범위: [0.0 ~ 1.0]
        /// </summary>
        public double Confidence { get; set; }

        /// <summary>
        /// 2026-08-26: AI Agent가 0~1000 정수 스케일로 보내는 Confidence를
        /// UI/이벤트 공통 0~1 범위로 변환한다. 기존 0~1 프로토콜 값도 호환한다.
        /// </summary>
        public double NormalizedConfidence =>
            Confidence > 1.0
                ? Confidence / 1000.0
                : Confidence;

        /// <summary>
        /// [Bounding Box] 좌측 X 좌표
        /// </summary>
        public int Left { get; set; }

        /// <summary>
        /// [Bounding Box] 상단 Y 좌표
        /// </summary>
        public int Top { get; set; }

        /// <summary>
        /// [Bounding Box] 우측 X 좌표
        /// </summary>
        public int Right { get; set; }

        /// <summary>
        /// [Bounding Box] 하단 Y 좌표
        /// </summary>
        public int Bottom { get; set; }

        /// <summary>
        /// [Bounding Box] 너비
        /// </summary>
        public int Width => Right - Left;

        /// <summary>
        /// [Bounding Box] 높이
        /// </summary>
        public int Height => Bottom - Top;

        // 2026-09-17: BBox와 Label의 레이아웃 폭을 분리한다.
        public int OverlayLeft { get; set; }
        public int OverlayTop { get; set; }
        public int OverlayWidth { get; set; }
        public int OverlayHeight { get; set; }
        public int BoxOffsetX { get; set; }
        public int BoxOffsetY { get; set; }
        public int RenderWidth { get; set; }
        public int RenderHeight { get; set; }
        public int LabelOffsetX { get; set; }
        public int LabelOffsetY { get; set; }
        public int LabelWidth { get; set; }

        /// <summary>
        /// [AI Detector] [Class Index] 기준 표시 이름
        ///
        /// 현재 기준:
        /// [ClassIndex 0] => Drone
        ///
        /// [ClassIndex 1]은 [Drone + best.onnx] 통합 탐지 결과로
        /// 실제 객체 종류(배, 차량 등)는 추가 매핑 확인이 필요하다.
        /// </summary>
        public string ClassName
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(ResolvedClassName))
                {
                    return ResolvedClassName;
                }

                switch (ClassIndex)
                {
                    case 0:
                        // [Drone] 탐지 클래스
                        return "Drone";

                    default:
                        // [Index] 탐지 클래스
                        return $"Class {ClassIndex}";
                }

            }

        }

        /// <summary>
        /// [AI Detector] 화면 표시용 탐지 정보 문자열
        /// 
        /// [Confidence]는 [0.0 ~ 1.0] 범위로 수신되므로,
        /// 화면에는 [%] 단위로 변환하여 표시한다.
        /// </summary>
        public string DisplayText
        {
            get
            {
                return $"AI E{DetectionEventId}-#{DisplayOrder} | {ClassName} {NormalizedConfidence * 100:F1}%";
            }

        }

    }

    /// <summary>
    /// 2026-09-17: 로컬 영상 분석에서 검출한 FIRE / SMOKE BBox 표시 정보.
    /// AI Agent BBox와 컬렉션을 분리하여 어느 한쪽 갱신이 다른 결과를 지우지 않게 한다.
    /// </summary>
    public sealed class VisionDetectionBox
    {
        public int DisplayOrder { get; set; }
        public string DetectionType { get; set; }
        public double ConfidencePercent { get; set; }
        public int Left { get; set; }
        public int Top { get; set; }
        public int Right { get; set; }
        public int Bottom { get; set; }
        public int Width => Right - Left;

        public int Height => Bottom - Top;
        /// <summary>
        /// 2026-09-17: 우측 경계의 작은 BBox에서 라벨이 영상 밖으로 잘리지 않도록
        /// 라벨을 BBox 오른쪽 기준으로 배치할지 나타낸다.
        /// </summary>
        public bool PlaceLabelOnRight { get; set; }
        public int LabelOffsetX { get; set; }
        public int LabelOffsetY { get; set; }
        public int LabelWidth { get; set; } = 180;
        public int OverlayLeft { get; set; }
        public int OverlayTop { get; set; }
        public int OverlayWidth { get; set; }
        public int OverlayHeight { get; set; }
        public int BoxOffsetX { get; set; }
        public int BoxOffsetY { get; set; }
        public string DisplayText =>
            $"{DetectionType} #{DisplayOrder} | {ConfidencePercent:F1}%";
    }

}
