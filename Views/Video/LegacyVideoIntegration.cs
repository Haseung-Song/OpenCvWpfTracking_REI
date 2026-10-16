using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using System.Threading.Tasks;
using OpenCvWpfTracking.ViewModels.Main;

// 2026-10-16: 기존 채널 색상과 영상 십자선 연결.
// 코드 정비: 한국어 설명과 함수 간격·region을 정리하며 기존 운용 흐름은 유지한다.
namespace OpenCvWpfTracking
{
    internal static class LegacyVideoIntegration
    {

        #region [필드 및 상태 정보]

        // 2026-10-15: 기존 최종본의 채널 색상과 변경 불가능한 공유 브러시를 유지한다.
        internal const string EoColor = "#C9E987", IrColor = "#C4AFF3", AiColor = "#39FF14", FireColor = "#FF3B30", SmokeColor = "#D8D0AB";
        internal const string AlertAiColor = "#55EE32", AlertFireColor = "#FF8178";
        private static readonly Brush Eo = FrozenBrush(EoColor), Ir = FrozenBrush(IrColor), Ai = FrozenBrush(AlertAiColor), Fire = FrozenBrush(AlertFireColor), Smoke = FrozenBrush(SmokeColor);
        private static readonly Brush FireBackground = FrozenBrush("#652D2A"), SmokeBackground = FrozenBrush("#595645"), AiBackground = FrozenBrush("#29452F");

        #endregion

        #region [기능 처리 함수]

        /// <summary>
        /// 수정 불가능한 공유 색상 브러시를 만든다.
        /// </summary>
        internal static Brush FrozenBrush(string color)
        {
            var brush = (Brush)new BrushConverter().ConvertFromString(color);
            brush.Freeze();
            return brush;
        }

        /// <summary>
        /// EO 또는 IR의 기존 채널 표시색을 반환한다.
        /// </summary>
        internal static Brush ChannelBrush(bool ir) => ir ? Ir : Eo;
        /// <summary>
        /// 이벤트 종류에 대응하는 기존 글자색을 반환한다.
        /// </summary>
        internal static Brush EventBrush(string type) => type == "FIRE" ? Fire : type == "SMOKE" ? Smoke : Ai;
        /// <summary>
        /// 이벤트 종류에 대응하는 기존 행 배경색을 반환한다.
        /// </summary>
        internal static Brush EventBackground(string type) => type == "FIRE" ? FireBackground : type == "SMOKE" ? SmokeBackground : AiBackground;
        /// <summary>
        /// 최신 실제 영상 프레임을 확인하여 이미지 파일로 저장한다.
        /// </summary>
        internal static async Task<string> SaveSnapshotAsync(MainViewModel vm, bool ir)
        {
            if (!vm.SnapshotFrameFresh(ir))
                throw new InvalidOperationException("연결된 채널의 최신 실제 프레임이 필요합니다.");
            var source = ir ? vm.IRCameraImage : vm.EOCameraImage;
            if (source == null)
                throw new InvalidOperationException("저장할 영상이 없습니다.");
            var frame = source.CloneCurrentValue();
            frame.Freeze();
            return await Task.Run(() =>
            {
                string dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Snapshots");
                Directory.CreateDirectory(dir);
                string file = Path.Combine(dir, (ir ? "IR_" : "EO_") + DateTime.Now.ToString("yyyyMMdd_HHmmss_fff") + "_" + Guid.NewGuid().ToString("N").Substring(0, 8) + ".png");
                var encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(frame));
                using (var stream = new FileStream(file, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                    encoder.Save(stream);
                Common.ConsoleLogHelper.Info("SNAPSHOT", "Saved source frame without WPF overlays / " + file);
                return file;
            });
        }

        /// <summary>
        /// 원본 영상에 십자선 보정과 중심 위치 갱신을 연결한다.
        /// </summary>
        internal static void Attach(FrameworkElement marker, FrameworkElement image, MainViewModel vm, Func<bool> ir)
        {
            var move = new TranslateTransform();
            marker.RenderTransform = move;
            var inline = new InlineOpticalCalibration((Grid)marker.Parent, image, vm, ir);
            var timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(100)
            };
            timer.Tick += (s, e) =>
            {
                var source = ir() ? vm.IRCameraImage : vm.EOCameraImage;
                if (source == null)
                    return;
                var center = inline.Preview ?? vm.GetOpticalCenter(ir());
                double scale = Math.Min(image.ActualWidth / source.PixelWidth, image.ActualHeight / source.PixelHeight);
                move.X = (center.X - .5) * source.PixelWidth * scale;
                move.Y = (center.Y - .5) * source.PixelHeight * scale;
            };
            marker.Loaded += (s, e) => timer.Start();
            marker.Unloaded += (s, e) => timer.Stop();
            if (marker.IsLoaded)
                timer.Start();
        // 추가 파노라마 및 이미지 저장 컨텍스트 메뉴는 복구 대상에서 제외한다.
        }

        #endregion

    }
}
