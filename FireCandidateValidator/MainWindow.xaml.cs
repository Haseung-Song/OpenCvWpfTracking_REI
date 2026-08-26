using Microsoft.Win32;
using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using CvPoint = OpenCvSharp.Point;
using CvRect = OpenCvSharp.Rect;
using CvSize = OpenCvSharp.Size;

namespace FireCandidateValidator
{
    public partial class MainWindow : System.Windows.Window
    {
        private readonly FireCandidateAnalyzer _analyzer;
        private readonly DispatcherTimer _videoTimer;

        private VideoCapture _videoCapture;
        private Mat _currentSource;
        private Mat _currentRendered;
        private bool _isVideoMode;
        private bool _loadedVideo;
        private VideoWriter _videoWriter;
        // 2026-08-14: 0=BLACK HOT, 1=WHITE HOT, 2=RAINBOW. 장비와 무관한 화면 표시 상태다.
        // 2026-08-14: Ten display palettes are cycled independently of equipment.
        private const int DisplayPaletteCount = 10;
        private int _displayPaletteIndex;
        // 2026-08-14: 1=전체 단일 BBox, 2=화염별 분리 BBox.
        private int _fireBoxGroupingMode = 2;
        private readonly List<StableCandidateTrack> _stableCandidateTracks =
            new List<StableCandidateTrack>();
        private bool _lastPublishedDetectionState;

        /// <summary>
        /// MainWindow 동작 수행 함수.
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();

            _analyzer =
                new FireCandidateAnalyzer();

            _videoTimer =
                new DispatcherTimer(
                    TimeSpan.FromMilliseconds(33),
                    DispatcherPriority.Background,
                    VideoTimer_Tick,
                    Dispatcher);

            _videoTimer.Stop();
            UpdateSettingText();
        }

        /// <summary>
        /// OpenImage_Click 이벤트 처리 함수.
        /// </summary>
        private void OpenImage_Click(
            object sender,
            RoutedEventArgs e)
        {
            OpenFileDialog dialog =
                new OpenFileDialog
                {
                    Title = "IR 시험 이미지 선택",
                    Filter = "Image files|*.jpg;*.jpeg;*.png;*.bmp;*.tif;*.tiff|All files|*.*"
                };

            if (dialog.ShowDialog() != true)
            {
                return;
            }

            try
            {
                StopVideo();

                /*
                 * OpenCV ImRead는 Windows의 한글 파일명 / 폴더 경로에서
                 * 영상을 읽지 못하는 경우가 있다.
                 *
                 * 파일 접근은 Unicode 경로를 지원하는 .NET으로 수행하고,
                 * 임시 영문 경로에서 OpenCV가 JPG / PNG를 디코딩하도록 한다.
                 */
                string extension =
                    Path.GetExtension(
                        dialog.FileName);

                string temporaryImagePath =
                    Path.Combine(
                        Path.GetTempPath(),
                        "FireCandidateValidator_" +
                        Guid.NewGuid().ToString("N") +
                        extension);

                Mat image;

                try
                {
                    File.Copy(
                        dialog.FileName,
                        temporaryImagePath,
                        true);

                    image =
                        Cv2.ImRead(
                            temporaryImagePath,
                            ImreadModes.Color);
                }
                finally
                {
                    if (File.Exists(temporaryImagePath))
                    {
                        File.Delete(temporaryImagePath);
                    }

                }

                if (image == null || image.Empty())
                {
                    image?.Dispose();
                    StatusText.Text = "이미지를 해석할 수 없습니다. JPG / PNG 파일을 확인하십시오.";
                    StatusText.Foreground = Brushes.OrangeRed;
                    return;
                }

                ReplaceCurrentSource(image);
                _isVideoMode = false;
                _loadedVideo = false;
                _analyzer.Reset();
                ProcessCurrentFrame(true);
            }
            catch (Exception ex)
            {
                StatusText.Text = "이미지 열기 실패: " + ex.Message;
                StatusText.Foreground = Brushes.OrangeRed;
            }

        }

        /// <summary>
        /// OpenVideo_Click 이벤트 처리 함수.
        /// </summary>
        private void OpenVideo_Click(
            object sender,
            RoutedEventArgs e)
        {
            OpenFileDialog dialog =
                new OpenFileDialog
                {
                    Title = "IR 시험 동영상 선택",
                    Filter = "Video files|*.mp4;*.avi;*.mkv;*.mov;*.wmv|All files|*.*"
                };

            if (dialog.ShowDialog() != true)
            {
                return;
            }

            StopVideo();
            _videoCapture = new VideoCapture(dialog.FileName);

            if (!_videoCapture.IsOpened())
            {
                _videoCapture.Dispose();
                _videoCapture = null;
                StatusText.Text = "동영상 파일을 열 수 없습니다.";
                return;
            }

            _isVideoMode = true;
            _loadedVideo = true;
            _analyzer.Reset();

            double fps = _videoCapture.Fps;
            double interval = fps > 1.0 ? 1000.0 / fps : 33.0;
            _videoTimer.Interval = TimeSpan.FromMilliseconds(Math.Max(15.0, interval));
            _videoTimer.Start();
        }

        /// <summary>
        /// CreateTestPattern_Click 이벤트 처리 함수.
        /// </summary>
        private void CreateTestPattern_Click(
            object sender,
            RoutedEventArgs e)
        {
            StopVideo();

            Mat testPattern =
                new Mat(
                    new CvSize(1280, 720),
                    MatType.CV_8UC3,
                    new Scalar(35, 25, 20));

            Cv2.Rectangle(testPattern, new CvRect(80, 80, 1120, 560), new Scalar(75, 65, 55), -1);
            Cv2.Line(testPattern, new CvPoint(100, 520), new CvPoint(1180, 520), new Scalar(120, 110, 100), 12);

            // 고온 팔레트 영상에서 화염으로 표현될 수 있는 국부 적색/주황 시험 영역
            Cv2.Ellipse(testPattern, new CvPoint(720, 390), new CvSize(75, 140), 0, 0, 360, new Scalar(0, 80, 255), -1);
            Cv2.Ellipse(testPattern, new CvPoint(670, 430), new CvSize(45, 90), 0, 0, 360, new Scalar(0, 180, 255), -1);
            Cv2.Circle(testPattern, new CvPoint(750, 300), 35, new Scalar(40, 220, 255), -1);

            ReplaceCurrentSource(testPattern);
            _isVideoMode = false;
            _loadedVideo = false;
            _analyzer.Reset();
            ProcessCurrentFrame(true);
        }

        /// <summary>
        /// 선택한 10/15/20/30 px 크기의 국부 화염 패턴을 생성하여
        /// Small Fire 최소 검출 크기를 동일 조건에서 반복 시험한다.
        /// </summary>
        private void CreateSmallFireTest_Click(
            object sender,
            RoutedEventArgs e)
        {
            ComboBoxItem selectedItem =
                SmallFireSizeCombo.SelectedItem as ComboBoxItem;

            int pixelSize = 10;

            if (selectedItem != null)
            {
                int.TryParse(
                    selectedItem.Tag?.ToString(),
                    out pixelSize);
            }

            pixelSize =
                Math.Max(10, Math.Min(30, pixelSize));

            StopVideo();

            Mat testPattern =
                new Mat(
                    new CvSize(1280, 720),
                    MatType.CV_8UC3,
                    new Scalar(48, 48, 48));

            Cv2.Rectangle(
                testPattern,
                new CvRect(80, 80, 1120, 560),
                new Scalar(72, 72, 72),
                -1);

            CvPoint center =
                new CvPoint(640, 360);

            Cv2.Ellipse(
                testPattern,
                center,
                new CvSize(
                    Math.Max(3, pixelSize / 2),
                    Math.Max(4, pixelSize / 2)),
                0,
                0,
                360,
                new Scalar(0, 90, 255),
                -1);

            Cv2.Circle(
                testPattern,
                new CvPoint(center.X, center.Y - pixelSize / 4),
                Math.Max(2, pixelSize / 4),
                new Scalar(30, 220, 255),
                -1);

            ReplaceCurrentSource(testPattern);
            _isVideoMode = false;
            _loadedVideo = false;
            _analyzer.Reset();
            ProcessCurrentFrame(true);

            StatusText.Text +=
                " / SYNTHETIC TARGET=" + pixelSize + " px";
        }

        /// <summary>
        /// Stop_Click 이벤트 처리 함수.
        /// </summary>
        private void Stop_Click(
            object sender,
            RoutedEventArgs e)
        {
            StopVideo();
            StatusText.Text = "동영상 검증을 정지했습니다.";
        }

        /// <summary>
        /// Reset_Click 이벤트 처리 함수.
        /// </summary>
        private void Reset_Click(object sender, RoutedEventArgs e)
        {
            StopVideo();
            StopVideoWriter();
            _analyzer.Reset();
            _loadedVideo = false;
            ReplaceCurrentSource(null);
            ReplaceRendered(null);
            ResultImage.Source = null;
            MaskImage.Source = null;
            StatusText.Text = "초기화 완료 - 이미지 또는 IR 영상을 불러오십시오.";
            StatusText.Foreground = Brushes.LightGreen;
        }

        /// <summary>
        /// SaveResult_Click 이벤트 처리 함수.
        /// </summary>
        private void SaveResult_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (_currentRendered == null || _currentRendered.Empty())
            {
                StatusText.Text = "저장할 결과 영상이 없습니다.";
                return;
            }

            SaveFileDialog dialog =
                new SaveFileDialog
                {
                    Title = "검출 결과 저장",
                    Filter = "PNG image|*.png|JPEG image|*.jpg",
                    FileName = "FireCandidate_Result_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".png"
                };

            if (dialog.ShowDialog() != true)
            {
                return;
            }

            Cv2.ImWrite(dialog.FileName, _currentRendered);
            StatusText.Text = "결과 저장 완료: " + Path.GetFileName(dialog.FileName);
        }

        /// <summary>
        /// SaveVideo_Click 이벤트 처리 함수.
        /// </summary>
        private void SaveVideo_Click(object sender, RoutedEventArgs e)
        {
            if (!_loadedVideo || _videoCapture == null)
            {
                StatusText.Text = "영상을 먼저 불러오십시오.";
                return;
            }

            if (_videoWriter != null)
            {
                StopVideoWriter();
                StatusText.Text = "검출 영상 저장을 완료했습니다.";
                return;
            }

            SaveFileDialog dialog = new SaveFileDialog
            {
                Title = "검출 영상 저장",
                Filter = "AVI video|*.avi",
                FileName = "FireCandidate_Result_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".avi"
            };

            if (dialog.ShowDialog() != true)
            {
                return;
            }

            double fps = _videoCapture.Fps > 1 ? _videoCapture.Fps : 30;
            _videoWriter = new VideoWriter(
                dialog.FileName,
                FourCC.MJPG,
                fps,
                new CvSize(_videoCapture.FrameWidth, _videoCapture.FrameHeight));

            if (!_videoWriter.IsOpened())
            {
                StopVideoWriter();
                StatusText.Text = "영상 저장기를 열 수 없습니다.";
                return;
            }

            SaveVideoButton.Content = "영상 저장 종료";
            StatusText.Text = "검출 영상 저장 중";
        }

        /// <summary>
        /// Setting_ValueChanged 설정 함수.
        /// </summary>
        private void Setting_ValueChanged(
            object sender,
            RoutedPropertyChangedEventArgs<double> e)
        {
            if (!IsLoaded)
            {
                return;
            }

            UpdateSettingText();

            if (!_isVideoMode && _currentSource != null)
            {
                _analyzer.Reset();
                ProcessCurrentFrame(true);
            }

        }

        /// <summary>
        /// VideoTimer_Tick 동작 수행 함수.
        /// </summary>
        private void VideoTimer_Tick(
            object sender,
            EventArgs e)
        {
            if (_videoCapture == null)
            {
                return;
            }

            Mat frame = new Mat();

            if (!_videoCapture.Read(frame) || frame.Empty())
            {
                frame.Dispose();
                StopVideo();
                StatusText.Text = "동영상 검증 완료";
                return;
            }

            ReplaceCurrentSource(frame);
            ProcessCurrentFrame(false);
        }

        /// <summary>
        /// ProcessCurrentFrame 처리 함수.
        /// </summary>
        private void ProcessCurrentFrame(bool singleFrame)
        {
            if (_currentSource == null || _currentSource.Empty())
            {
                return;
            }

            int confirmationFrames =
                singleFrame
                    ? 1
                    : Math.Max(1, (int)Math.Round(ConfirmationSlider.Value));

            using (FireCandidateAnalysis analysis =
                   _analyzer.Analyze(
                       _currentSource,
                       ThresholdSlider.Value,
                       AreaSlider.Value,
                       confirmationFrames,
                       _fireBoxGroupingMode))
            {
                IList<CvRect> displayCandidates =
                    UpdateStableCandidateTracks(
                        analysis.Candidates,
                        analysis.IsConfirmed);

                using (Mat displaySource = ApplySelectedPalette(_currentSource))
                {
                    Mat rendered = displaySource.Clone();

                    double displayScale =
                        Math.Max(
                            1.0,
                            Math.Max(
                                rendered.Width / 1280.0,
                                rendered.Height / 720.0));
                    int lineThickness =
                        Math.Max(3, (int)Math.Round(2.0 * displayScale));

                    List<CvRect> occupiedLabelRects = new List<CvRect>();

                    foreach (CvRect candidate in displayCandidates)
                    {
                        Scalar color = new Scalar(0, 0, 255);
                        CvRect displayRect =
                            CreateVisibleDetectionRect(
                                candidate,
                                rendered.Width,
                                rendered.Height,
                                displayScale);

                        Cv2.Rectangle(
                            rendered,
                            displayRect,
                            color,
                            lineThickness);
                        double labelScale = Math.Max(0.8, 0.8 * displayScale);
                        int labelThickness = Math.Max(2, (int)Math.Round(1.5 * displayScale));
                        int baseline;
                        CvSize labelSize = Cv2.GetTextSize(
                            "FIRE DETECTION",
                            HersheyFonts.HersheySimplex,
                            labelScale,
                            labelThickness,
                            out baseline);
                        CvPoint labelOrigin = FindAvailableLabelOrigin(
                            displayRect,
                            labelSize,
                            baseline,
                            occupiedLabelRects,
                            rendered.Width,
                            rendered.Height,
                            displayScale);

                        Cv2.PutText(
                            rendered,
                            "FIRE DETECTION",
                            labelOrigin,
                            HersheyFonts.HersheySimplex,
                            labelScale,
                            new Scalar(0, 0, 255),
                            labelThickness);
                    }

                    ReplaceRendered(rendered);
                }

                if (_videoWriter != null && _videoWriter.IsOpened())
                {
                    _videoWriter.Write(_currentRendered);
                }
                ResultImage.Source = ToBitmapSource(_currentRendered);
                MaskImage.Source = ToBitmapSource(analysis.Mask);

                CvRect largestCandidate =
                    displayCandidates
                        .OrderByDescending(candidate => candidate.Width * candidate.Height)
                        .FirstOrDefault();

                string largestPixelText =
                    largestCandidate.Width > 0
                        ? largestCandidate.Width + "x" + largestCandidate.Height +
                          " px / " + (largestCandidate.Width * largestCandidate.Height) + " px²"
                        : "-";

                StatusText.Text =
                    string.Format(
                        "{0} / 후보 {1}개 / 연속 {2} frame / 최대 BBox {3} / 최대 면적비 {4:P3}",
                        displayCandidates.Count > 0 ? "FIRE DETECTOR DETECTED" : "FIRE DETECTOR MONITORING",
                        displayCandidates.Count,
                        analysis.ContinuousFrames,
                        largestPixelText,
                        analysis.LargestAreaRatio);

                StatusText.Foreground =
                    displayCandidates.Count > 0
                        ? Brushes.OrangeRed
                        : Brushes.LightGreen;

                PublishLiveEvent(
                    displayCandidates.Count > 0,
                    displayCandidates);
            }

        }

        /// <summary>
        /// [2026-08-24] 모든 FIRE DETECTION 문구를 BBox 위쪽에 배치하고,
        /// 여러 화염의 문구가 겹치면 더 높은 행으로 이동한다.
        /// </summary>
        private static CvPoint FindAvailableLabelOrigin(
            CvRect detectionRect,
            CvSize labelSize,
            int baseline,
            IList<CvRect> occupiedLabels,
            int frameWidth,
            int frameHeight,
            double displayScale)
        {
            int gap = Math.Max(4, (int)Math.Round(5 * displayScale));
            int labelHeight = labelSize.Height + baseline + 2;
            int clampedX = Math.Max(0, Math.Min(detectionRect.X, frameWidth - labelSize.Width - 1));
            int preferredTop = detectionRect.Y - labelHeight - gap;

            for (int attempt = 0; attempt < 12; attempt++)
            {
                int rowOffset = attempt * (labelHeight + gap);
                int candidateTop = preferredTop - rowOffset;
                candidateTop = Math.Max(0, Math.Min(candidateTop, frameHeight - labelHeight - 1));

                CvRect candidateLabel = new CvRect(
                    clampedX,
                    candidateTop,
                    labelSize.Width,
                    labelHeight);

                if (!occupiedLabels.Any(existing => RectanglesOverlap(existing, candidateLabel)))
                {
                    occupiedLabels.Add(candidateLabel);
                    return new CvPoint(clampedX, candidateTop + labelSize.Height);
                }

            }

            int fallbackTop = Math.Max(0, Math.Min(preferredTop, frameHeight - labelHeight - 1));
            occupiedLabels.Add(new CvRect(clampedX, fallbackTop, labelSize.Width, labelHeight));
            return new CvPoint(clampedX, fallbackTop + labelSize.Height);
        }

        /// <summary>
        /// 두 문구 영역의 교차 여부를 반환한다.
        /// </summary>
        private static bool RectanglesOverlap(CvRect left, CvRect right)
        {
            return left.X < right.Right &&
                   left.Right > right.X &&
                   left.Y < right.Bottom &&
                   left.Bottom > right.Y;
        }

        /// <summary>
        /// 프레임 간 후보를 같은 화염으로 추적하여 BBox 위치를 완만하게 보정하고,
        /// 일시적인 미검출은 8프레임까지 유지해 깜빡임을 줄인다.
        /// </summary>
        private IList<CvRect> UpdateStableCandidateTracks(
            IList<CvRect> candidates,
            bool isConfirmed)
        {
            bool[] matched = new bool[_stableCandidateTracks.Count];

            foreach (CvRect candidate in candidates)
            {
                int bestIndex = -1;
                double bestScore = 0.0;

                for (int index = 0; index < _stableCandidateTracks.Count; index++)
                {
                    if (matched[index])
                    {
                        continue;
                    }

                    double score = CalculateMatchScore(
                        _stableCandidateTracks[index].Rectangle,
                        candidate);
                    if (score > bestScore)
                    {
                        bestScore = score;
                        bestIndex = index;
                    }

                }

                if (bestIndex >= 0 && bestScore >= 0.20)
                {
                    StableCandidateTrack track = _stableCandidateTracks[bestIndex];
                    track.Rectangle = SmoothRectangle(track.Rectangle, candidate, 0.35);
                    track.MissingFrames = 0;
                    track.SeenFrames++;
                    track.IsVisible = track.IsVisible || (isConfirmed && track.SeenFrames >= 2);
                    matched[bestIndex] = true;
                }
                else
                {
                    _stableCandidateTracks.Add(
                        new StableCandidateTrack
                        {
                            Rectangle = candidate,
                            IsVisible = isConfirmed && !_isVideoMode,
                            SeenFrames = 1
                        });
                    Array.Resize(ref matched, _stableCandidateTracks.Count);
                    matched[matched.Length - 1] = true;
                }

            }

            for (int index = _stableCandidateTracks.Count - 1; index >= 0; index--)
            {
                if (index >= matched.Length || !matched[index])
                {
                    _stableCandidateTracks[index].MissingFrames++;
                }

                if (_stableCandidateTracks[index].MissingFrames > 8)
                {
                    _stableCandidateTracks.RemoveAt(index);
                }

            }

            return _stableCandidateTracks
                .Where(track => track.IsVisible)
                .Select(track => track.Rectangle)
                .ToList();
        }

        /// <summary>
        /// 두 BBox의 겹침과 중심 거리로 동일 화염 여부 점수를 계산한다.
        /// </summary>
        private static double CalculateMatchScore(CvRect first, CvRect second)
        {
            int intersectionWidth =
                Math.Max(0, Math.Min(first.Right, second.Right) - Math.Max(first.Left, second.Left));
            int intersectionHeight =
                Math.Max(0, Math.Min(first.Bottom, second.Bottom) - Math.Max(first.Top, second.Top));
            double intersection = intersectionWidth * intersectionHeight;
            double union = Math.Max(1.0, first.Width * first.Height + second.Width * second.Height - intersection);
            double iou = intersection / union;
            double centerDistance =
                Math.Sqrt(
                    Math.Pow(first.X + first.Width / 2.0 - second.X - second.Width / 2.0, 2) +
                    Math.Pow(first.Y + first.Height / 2.0 - second.Y - second.Height / 2.0, 2));
            double allowedDistance =
                Math.Max(12.0, Math.Max(first.Width, first.Height) * 0.8);
            double proximity = Math.Max(0.0, 1.0 - centerDistance / allowedDistance);
            return Math.Max(iou, proximity * 0.8);
        }

        /// <summary>
        /// 현재 측정값 일부만 반영하여 화염 BBox의 급격한 위치·크기 변화를 억제한다.
        /// </summary>
        private static CvRect SmoothRectangle(
            CvRect previous,
            CvRect current,
            double currentWeight)
        {
            double previousWeight = 1.0 - currentWeight;
            return new CvRect(
                (int)Math.Round(previous.X * previousWeight + current.X * currentWeight),
                (int)Math.Round(previous.Y * previousWeight + current.Y * currentWeight),
                Math.Max(1, (int)Math.Round(previous.Width * previousWeight + current.Width * currentWeight)),
                Math.Max(1, (int)Math.Round(previous.Height * previousWeight + current.Height * currentWeight)));
        }

        /// <summary>
        /// 테스트 프로그램의 탐지 시작·해제를 메인 Viewer가 읽는 공유 이벤트 파일에 기록한다.
        /// </summary>
        private void PublishLiveEvent(
            bool isDetected,
            IList<CvRect> candidates)
        {
            if (!_loadedVideo || _lastPublishedDetectionState == isDetected)
            {
                return;
            }

            _lastPublishedDetectionState = isDetected;
            string directory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "OpenCvWpfTracking",
                "FireEvents");
            Directory.CreateDirectory(directory);
            string path = Path.Combine(directory, "TestProgramLiveEvents.txt");
            using (FileStream stream =
                   new FileStream(path, FileMode.Append, FileAccess.Write, FileShare.ReadWrite))
            using (StreamWriter writer = new StreamWriter(stream))
            {
                DateTime eventTime = DateTime.Now;
                if (isDetected)
                {
                    foreach (CvRect candidate in candidates)
                    {
                        writer.WriteLine(
                            string.Join(
                                "|",
                                eventTime.ToString("O"),
                                "DETECTED",
                                "1",
                                candidate.Width.ToString(),
                                candidate.Height.ToString(),
                                (candidate.Width * candidate.Height).ToString(),
                                "TEST PROGRAM"));
                    }

                }
                else
                {
                    writer.WriteLine(
                        string.Join(
                            "|",
                            eventTime.ToString("O"),
                            "CLEARED",
                            "0",
                            "0",
                            "0",
                            "0",
                            "TEST PROGRAM"));
                }

            }

        }

        /// <summary>
        /// 작은 후보의 실제 크기는 유지하면서 축소 화면에서도 확인할 수 있는
        /// 최소 표시 영역을 계산한다.
        /// </summary>
        private static CvRect CreateVisibleDetectionRect(
            CvRect source,
            int frameWidth,
            int frameHeight,
            double displayScale)
        {
            int minimumSide =
                Math.Max(28, (int)Math.Round(24 * displayScale));
            int targetWidth = Math.Max(source.Width, minimumSide);
            int targetHeight = Math.Max(source.Height, minimumSide);
            int centerX = source.X + source.Width / 2;
            int centerY = source.Y + source.Height / 2;
            int x = Math.Max(0, centerX - targetWidth / 2);
            int y = Math.Max(0, centerY - targetHeight / 2);

            targetWidth = Math.Min(targetWidth, frameWidth - x);
            targetHeight = Math.Min(targetHeight, frameHeight - y);

            return new CvRect(x, y, targetWidth, targetHeight);
        }

        /// <summary>
        /// FireBoxMode1_Click 이벤트 처리 함수.
        /// </summary>
        private void FireBoxMode1_Click(object sender, RoutedEventArgs e)
        {
            SetFireBoxGroupingMode(1);
        }

        /// <summary>
        /// FireBoxMode2_Click 이벤트 처리 함수.
        /// </summary>
        private void FireBoxMode2_Click(object sender, RoutedEventArgs e)
        {
            SetFireBoxGroupingMode(2);
        }

        /// <summary>
        /// SetFireBoxGroupingMode 설정 함수.
        /// </summary>
        private void SetFireBoxGroupingMode(int mode)
        {
            _fireBoxGroupingMode = mode == 1 ? 1 : 2;
            FireBoxMode1Button.Background = new SolidColorBrush(
                _fireBoxGroupingMode == 1 ? Color.FromRgb(42, 111, 151) : Color.FromRgb(62, 81, 94));
            FireBoxMode2Button.Background = new SolidColorBrush(
                _fireBoxGroupingMode == 2 ? Color.FromRgb(42, 111, 151) : Color.FromRgb(62, 81, 94));
            ProcessCurrentFrame(_isVideoMode ? false : true);
        }

        // 2026-08-14: 콤보박스 대신 메인 Viewer와 같은 직접/상대 팔레트 버튼을 사용한다.
        private void BlackHotPalette_Click(object sender, RoutedEventArgs e) => SetDisplayPalette(0);
        private void WhiteHotPalette_Click(object sender, RoutedEventArgs e) => SetDisplayPalette(1);
        /// <summary>
        /// RandomPalette_Click 이벤트 처리 함수.
        /// </summary>
        private void RandomPalette_Click(object sender, RoutedEventArgs e)
        {
            // 2026-08-14: RANDOM은 장비와 동일하게 다음 팔레트를 1회 선택한다.
            SetDisplayPalette((_displayPaletteIndex + 1) % DisplayPaletteCount);
            HighlightRandomPaletteButton();
        }
        private void PreviousPalette_Click(object sender, RoutedEventArgs e) => SetDisplayPalette((_displayPaletteIndex - 1 + DisplayPaletteCount) % DisplayPaletteCount);
        private void NextPalette_Click(object sender, RoutedEventArgs e) => SetDisplayPalette((_displayPaletteIndex + 1) % DisplayPaletteCount);

        /// <summary>
        /// SetDisplayPalette 설정 함수.
        /// </summary>
        private void SetDisplayPalette(int paletteIndex)
        {
            _displayPaletteIndex = paletteIndex;
            UpdatePaletteButtonVisuals();
            if (IsLoaded && !_isVideoMode && _currentSource != null)
            {
                _analyzer.Reset();
                ProcessCurrentFrame(true);
            }

        }

        /// <summary>
        /// ApplySelectedPalette 설정 함수.
        /// </summary>
        private Mat ApplySelectedPalette(Mat source)
        {
            Mat gray = new Mat();
            if (source.Channels() == 1)
            {
                source.CopyTo(gray);
            }
            else
            {
                Cv2.CvtColor(source, gray, ColorConversionCodes.BGR2GRAY);
            }

            int index = _displayPaletteIndex;

            if (index == 0)
            {
                Mat blackHot = new Mat();
                Cv2.CvtColor(gray, blackHot, ColorConversionCodes.GRAY2BGR);
                gray.Dispose();
                return blackHot;
            }

            if (index == 1)
            {
                // 2026-08-14: WHITE HOT display is the inverse of BLACK HOT.
                Cv2.BitwiseNot(gray, gray);
                Mat whiteHot = new Mat();
                Cv2.CvtColor(gray, whiteHot, ColorConversionCodes.GRAY2BGR);
                gray.Dispose();
                return whiteHot;
            }

            ColormapTypes[] maps =
            {
                ColormapTypes.Inferno,
                ColormapTypes.Jet,
                ColormapTypes.Hot,
                ColormapTypes.Plasma,
                ColormapTypes.Magma,
                ColormapTypes.Ocean,
                ColormapTypes.Winter,
                ColormapTypes.Rainbow
            };

            Mat colored = new Mat();
            Cv2.ApplyColorMap(gray, colored, maps[Math.Min(maps.Length - 1, index - 2)]);
            gray.Dispose();
            return colored;
        }

        // 2026-08-14: Initially neutral; show a palette colour after user selection only.
        /// <summary>
        /// UpdatePaletteButtonVisuals 갱신 함수.
        /// </summary>
        private void UpdatePaletteButtonVisuals()
        {
            SolidColorBrush neutral = new SolidColorBrush(Color.FromRgb(244, 244, 244));
            SolidColorBrush darkText = new SolidColorBrush(Color.FromRgb(32, 38, 45));
            BlackHotPaletteButton.Background = neutral;
            BlackHotPaletteButton.Foreground = darkText;
            WhiteHotPaletteButton.Background = neutral;
            WhiteHotPaletteButton.Foreground = darkText;
            RandomPaletteButton.Background = neutral;
            RandomPaletteButton.Foreground = darkText;

            if (_displayPaletteIndex == 0)
            {
                BlackHotPaletteButton.Background = Brushes.Black;
                BlackHotPaletteButton.Foreground = Brushes.White;
            }
            else if (_displayPaletteIndex == 1)
            {
                WhiteHotPaletteButton.Background = Brushes.White;
            }
            else if (_displayPaletteIndex == 9)
            {
                HighlightRandomPaletteButton();
            }

        }

        /// <summary>
        /// HighlightRandomPaletteButton 동작 수행 함수.
        /// </summary>
        private void HighlightRandomPaletteButton()
        {
            LinearGradientBrush rainbow = new LinearGradientBrush { StartPoint = new System.Windows.Point(0, 0), EndPoint = new System.Windows.Point(1, 0) };
            rainbow.GradientStops.Add(new GradientStop(Colors.Red, 0));
            rainbow.GradientStops.Add(new GradientStop(Colors.Yellow, 0.35));
            rainbow.GradientStops.Add(new GradientStop(Colors.LimeGreen, 0.60));
            rainbow.GradientStops.Add(new GradientStop(Colors.DodgerBlue, 0.82));
            rainbow.GradientStops.Add(new GradientStop(Colors.MediumPurple, 1));
            RandomPaletteButton.Background = rainbow;
            RandomPaletteButton.Foreground = Brushes.White;
        }

        /// <summary>
        /// UpdateSettingText 갱신 함수.
        /// </summary>
        private void UpdateSettingText()
        {
            if (ThresholdText == null || AreaText == null || ConfirmationText == null)
            {
                return;
            }

            ThresholdText.Text = ThresholdSlider.Value.ToString("0.00");
            AreaText.Text = AreaSlider.Value.ToString("0.0000");
            ConfirmationText.Text = Math.Round(ConfirmationSlider.Value) + " frames";
        }

        /// <summary>
        /// ReplaceCurrentSource 동작 수행 함수.
        /// </summary>
        private void ReplaceCurrentSource(Mat source)
        {
            if (_currentSource != null)
            {
                _currentSource.Dispose();
            }

            _currentSource = source;
        }

        /// <summary>
        /// ReplaceRendered 동작 수행 함수.
        /// </summary>
        private void ReplaceRendered(Mat rendered)
        {
            if (_currentRendered != null)
            {
                _currentRendered.Dispose();
            }

            _currentRendered = rendered;
        }

        /// <summary>
        /// StopVideo 중지 함수.
        /// </summary>
        private void StopVideo()
        {
            if (_lastPublishedDetectionState)
            {
                PublishLiveEvent(false, new List<CvRect>());
            }

            _videoTimer.Stop();
            _isVideoMode = false;
            _stableCandidateTracks.Clear();

            if (_videoCapture != null)
            {
                _videoCapture.Release();
                _videoCapture.Dispose();
                _videoCapture = null;
            }

        }

        private sealed class StableCandidateTrack
        {
            public CvRect Rectangle { get; set; }
            public int MissingFrames { get; set; }
            public int SeenFrames { get; set; }
            public bool IsVisible { get; set; }
        }

        /// <summary>
        /// StopVideoWriter 중지 함수.
        /// </summary>
        private void StopVideoWriter()
        {
            if (_videoWriter != null)
            {
                _videoWriter.Release();
                _videoWriter.Dispose();
                _videoWriter = null;
            }

            if (SaveVideoButton != null)
            {
                SaveVideoButton.Content = "영상 저장 시작";
            }

        }

        /// <summary>
        /// ToBitmapSource 동작 수행 함수.
        /// </summary>
        private static BitmapSource ToBitmapSource(Mat source)
        {
            if (source == null || source.Empty())
            {
                return null;
            }

            using (Mat bgra = new Mat())
            {
                if (source.Channels() == 1)
                {
                    Cv2.CvtColor(source, bgra, ColorConversionCodes.GRAY2BGRA);
                }
                else if (source.Channels() == 3)
                {
                    Cv2.CvtColor(source, bgra, ColorConversionCodes.BGR2BGRA);
                }
                else
                {
                    source.CopyTo(bgra);
                }

                int stride = bgra.Width * bgra.ElemSize();
                int bufferSize = stride * bgra.Height;
                byte[] pixels = new byte[bufferSize];
                Marshal.Copy(bgra.Data, pixels, 0, bufferSize);

                BitmapSource bitmap =
                    BitmapSource.Create(
                        bgra.Width,
                        bgra.Height,
                        96,
                        96,
                        PixelFormats.Bgra32,
                        null,
                        pixels,
                        stride);

                bitmap.Freeze();
                return bitmap;
            }

        }

        /// <summary>
        /// Window_Closing 이벤트 처리 함수.
        /// </summary>
        private void Window_Closing(
            object sender,
            CancelEventArgs e)
        {
            StopVideo();
            StopVideoWriter();

            if (_currentSource != null)
            {
                _currentSource.Dispose();
            }

            if (_currentRendered != null)
            {
                _currentRendered.Dispose();
            }

        }

    }

}
