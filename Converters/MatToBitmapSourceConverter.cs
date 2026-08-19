using OpenCvSharp;
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

    }

}