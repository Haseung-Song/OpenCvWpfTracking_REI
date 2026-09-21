using OpenCvSharp;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace OpenCvWpfTracking.Converters
{
    public static class MatToBitmapSourceConverter
    {
        /// <summary>
        /// [OpenCV] [Mat] → [WPF] [BitmapSource] 변환
        /// </summary>
        public static BitmapSource Convert(Mat frame)
        {
            if (frame == null ||
                frame.Empty())
            {
                return null;
            }

            PixelFormat pixelFormat = PixelFormats.Bgr24;

            // [채널 수]에 따른 [PixelFormat] 선택
            if (frame.Channels() == 1)
            {
                pixelFormat =
                    PixelFormats.Gray8;
            }
            else if (frame.Channels() == 4)
            {
                pixelFormat =
                    PixelFormats.Bgra32;
            }
            return BitmapSource.Create(
                frame.Width,
                frame.Height,
                96,
                96,
                pixelFormat,
                null,
                frame.Data,
                (int)(
                    frame.Step()
                    * frame.Height),
                (int)
                    frame.Step());
        }

        /// <summary>
        /// 2026-09-17: RTSP 연속 표시용 재사용 버퍼. 해상도/PixelFormat이 같으면
        /// WriteableBitmap을 새로 만들지 않고 픽셀만 복사하여 Gen0 GC를 줄인다.
        /// UI Dispatcher thread에서 호출해야 한다.
        /// </summary>
        public static WriteableBitmap UpdateOrCreate(
            Mat frame,
            WriteableBitmap bitmap)
        {
            if (frame == null || frame.Empty()) return bitmap;

            PixelFormat pixelFormat = PixelFormats.Bgr24;
            if (frame.Channels() == 1) pixelFormat = PixelFormats.Gray8;
            else if (frame.Channels() == 4) pixelFormat = PixelFormats.Bgra32;

            if (bitmap == null ||
                bitmap.PixelWidth != frame.Width ||
                bitmap.PixelHeight != frame.Height ||
                bitmap.Format != pixelFormat)
            {
                bitmap = new WriteableBitmap(
                    frame.Width,
                    frame.Height,
                    96,
                    96,
                    pixelFormat,
                    null);
            }

            int stride = (int)frame.Step();
            int bufferSize = checked(stride * frame.Height);
            bitmap.WritePixels(
                new Int32Rect(0, 0, frame.Width, frame.Height),
                frame.Data,
                bufferSize,
                stride);

            return bitmap;
        }

    }

}
