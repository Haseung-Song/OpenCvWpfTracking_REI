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

namespace OpenCvWpfTracking
{
    internal static class LegacyVideoIntegration
    {
        // 2026-10-15: inherit V31_13 palette and immutable shared brushes.
        internal const string EoColor="#C9E987",IrColor="#C4AFF3",AiColor="#39FF14",FireColor="#FF3B30",SmokeColor="#D8D0AB";
        internal const string AlertAiColor="#55EE32",AlertFireColor="#FF8178";
        private static readonly Brush Eo=FrozenBrush(EoColor),Ir=FrozenBrush(IrColor),Ai=FrozenBrush(AlertAiColor),Fire=FrozenBrush(AlertFireColor),Smoke=FrozenBrush(SmokeColor);
        private static readonly Brush FireBackground=FrozenBrush("#652D2A"),SmokeBackground=FrozenBrush("#595645"),AiBackground=FrozenBrush("#29452F");
        internal static Brush FrozenBrush(string color){var brush=(Brush)new BrushConverter().ConvertFromString(color);brush.Freeze();return brush;}
        internal static Brush ChannelBrush(bool ir)=>ir?Ir:Eo;
        internal static Brush EventBrush(string type)=>type=="FIRE"?Fire:type=="SMOKE"?Smoke:Ai;
        internal static Brush EventBackground(string type)=>type=="FIRE"?FireBackground:type=="SMOKE"?SmokeBackground:AiBackground;
        internal static async Task<string> SaveSnapshotAsync(MainViewModel vm,bool ir)
        {
            if(!vm.SnapshotFrameFresh(ir))throw new InvalidOperationException("연결된 채널의 최신 실제 프레임이 필요합니다.");
            var source=ir?vm.IRCameraImage:vm.EOCameraImage;
            if(source==null)throw new InvalidOperationException("저장할 영상이 없습니다.");
            var frame=source.CloneCurrentValue();frame.Freeze();
            return await Task.Run(()=>{
                string dir=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"Snapshots");Directory.CreateDirectory(dir);
                string file=Path.Combine(dir,(ir?"IR_":"EO_")+DateTime.Now.ToString("yyyyMMdd_HHmmss_fff")+"_"+Guid.NewGuid().ToString("N").Substring(0,8)+".png");
                var encoder=new PngBitmapEncoder();encoder.Frames.Add(BitmapFrame.Create(frame));
                using(var stream=new FileStream(file,FileMode.CreateNew,FileAccess.Write,FileShare.None))encoder.Save(stream);
                Common.ConsoleLogHelper.Info("SNAPSHOT","Saved source frame without WPF overlays / "+file);return file;
            });
        }
        internal static void Attach(FrameworkElement marker,FrameworkElement image,MainViewModel vm,Func<bool> ir)
        {
            var move=new TranslateTransform();marker.RenderTransform=move;
            var inline=new InlineOpticalCalibration((Grid)marker.Parent,image,vm,ir);
            var timer=new DispatcherTimer{Interval=TimeSpan.FromMilliseconds(100)};
            timer.Tick+=(s,e)=>{
                var source=ir()?vm.IRCameraImage:vm.EOCameraImage;if(source==null)return;
                var center=inline.Preview??vm.GetOpticalCenter(ir());double scale=Math.Min(image.ActualWidth/source.PixelWidth,image.ActualHeight/source.PixelHeight);
                move.X=(center.X-.5)*source.PixelWidth*scale;move.Y=(center.Y-.5)*source.PixelHeight*scale;
            };
            marker.Loaded+=(s,e)=>timer.Start();marker.Unloaded+=(s,e)=>timer.Stop();if(marker.IsLoaded)timer.Start();
            // V32_0 rollback: no added panorama/snapshot context menu.
        }
    }
}
