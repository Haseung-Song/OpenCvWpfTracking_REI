using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Markup;
using System.Globalization;
using OpenCvWpfTracking.Common;

namespace OpenCvWpfTracking
{
    // 2026-10-12: one content/style source for main EO/IR and popout shortcut panels.
    internal static class VideoShortcutGuide
    {
        internal static double Scale(double width,double height) => Math.Max(.4,Math.Min(1.15,Math.Min(width/1280,height/720)));
        // Labels must remain legible in the IR thumbnail; help may scale further down.
        internal static double LabelScale(double width,double height) => Math.Max(1.0,Math.Min(1.3,Math.Min(width/1280,height/720)));
        // 2026-10-15 V31_13: one Uniform source-image rectangle for both channels and all windows.
        internal static double SourceAspect(FrameworkElement video,double fallback)
        {
            ImageSource source=(video as Image)?.Source??((video as System.Windows.Shapes.Shape)?.Fill as ImageBrush)?.ImageSource;
            return source!=null&&source.Width>0&&source.Height>0?source.Width/source.Height:fallback;
        }
        internal static Rect VideoContentRect(FrameworkElement host,FrameworkElement video,double aspect)
        {
            if(host==null||video==null||video.ActualWidth<=0||video.ActualHeight<=0)return Rect.Empty;
            if(!ReferenceEquals(host,video)&&!host.IsAncestorOf(video))return Rect.Empty;
            if(!IsFinite(aspect)||aspect<=0)return Rect.Empty;
            double width=Math.Min(video.ActualWidth,video.ActualHeight*aspect),height=width/aspect;
            Point a=video.TranslatePoint(new Point((video.ActualWidth-width)/2,(video.ActualHeight-height)/2),host);
            Point b=video.TranslatePoint(new Point((video.ActualWidth+width)/2,(video.ActualHeight+height)/2),host);
            return new Rect(a,b);
        }
        private static void WatchOverlayLayout(FrameworkElement host,Action update)
        {
            bool reported=false;
            EventHandler handler=(s,e)=>{try{update();}catch(Exception error){if(!reported){reported=true;ConsoleLogHelper.Error("VIDEO UI / CONTENT RECT","Overlay update skipped; video unchanged",error);}}};
            host.Loaded+=(s,e)=>{host.LayoutUpdated-=handler;host.LayoutUpdated+=handler;handler(s,EventArgs.Empty);};
            host.Unloaded+=(s,e)=>host.LayoutUpdated-=handler;
            if(host.IsLoaded){host.LayoutUpdated+=handler;handler(host,EventArgs.Empty);}
        }
        internal static void AttachVideoAlarmPositions(FrameworkElement host,StackPanel panel,FrameworkElement video,Func<double> aspect)
        {
            if(host==null||panel==null||video==null||aspect==null)return;
            Rect previous=Rect.Empty;double previousWidth=-1,previousHeight=-1;
            WatchOverlayLayout(host,()=>
            {
                Rect area=VideoContentRect(host,video,aspect());if(area.IsEmpty)return;
                if(area==previous&&host.ActualWidth==previousWidth&&host.ActualHeight==previousHeight)return;
                previous=area;previousWidth=host.ActualWidth;previousHeight=host.ActualHeight;
                double scale=Math.Max(.6,Math.Min(1,Math.Min(area.Width/1000,area.Height/600)));
                panel.HorizontalAlignment=HorizontalAlignment.Right;panel.VerticalAlignment=VerticalAlignment.Top;
                panel.Margin=new Thickness(0,area.Top+Math.Max(4,10*scale),Math.Max(0,host.ActualWidth-area.Right)+8,0);
                panel.MaxWidth=Math.Max(1,area.Width/2-28);
                foreach(var child in panel.Children)
                    if(child is Border banner&&banner.Child is TextBlock text)
                    {
                        text.FontSize=Math.Max(11,18*scale);text.TextWrapping=TextWrapping.Wrap;text.TextAlignment=TextAlignment.Center;
                        banner.Padding=new Thickness(10*scale,4*scale,10*scale,4*scale);banner.BorderThickness=new Thickness(scale<.8?1:2);
                        if(text.Text.Contains("FIRE")){text.Foreground=LegacyVideoIntegration.EventBrush("FIRE");banner.BorderBrush=text.Foreground;}
                    }
            });
        }
        internal static void AttachVideoLabelPositions(FrameworkElement host,FrameworkElement video,
            FrameworkElement ratio,TextBlock status,Func<double> aspect)
        {
            if(host==null||video==null||ratio==null||status==null||aspect==null)return;
            Thickness originalRatio=ratio.Margin,originalStatus=status.Margin;
            Rect previous=Rect.Empty;Size previousSize=new Size();bool? previousConnected=null;
            WatchOverlayLayout(host,()=>
            {
                bool connected=(status.Text??"").EndsWith("] Connected",StringComparison.Ordinal);
                Rect area=connected?VideoContentRect(host,video,aspect()):Rect.Empty;
                Size size=new Size(host.ActualWidth,host.ActualHeight);
                if(previous==area&&previousSize==size&&previousConnected==connected)return;
                if(connected&&area.IsEmpty)return;
                Thickness r=originalRatio,s=originalStatus;
                if(connected)
                {
                    double bottom=Math.Max(0,host.ActualHeight-area.Bottom)+1;
                    r=new Thickness(area.Left+8,0,0,bottom);
                    s=new Thickness(0,0,Math.Max(0,host.ActualWidth-area.Right)+8,bottom);
                }
                if(ratio.Margin!=r)ratio.Margin=r;if(status.Margin!=s)status.Margin=s;
                previous=area;previousSize=size;previousConnected=connected;
            });
        }
        private static bool IsFinite(double value)=>!double.IsNaN(value)&&!double.IsInfinity(value);
        internal static void BindRatioVisibility(TextBlock label,bool ir)
        {
            var binding=new MultiBinding { Converter=new ConnectedRatioVisibilityConverter() };
            binding.Bindings.Add(new Binding(ir?"IrStatusText":"EoStatusText"));
            binding.Bindings.Add(new Binding("IsZoomRatioOverlayVisible"));
            label.SetBinding(UIElement.VisibilityProperty,binding);
        }
        internal static void BindRatioToggleVisibility(FrameworkElement toggle,bool ir) =>
            toggle.SetBinding(UIElement.VisibilityProperty,new Binding(ir?"IrStatusText":"EoStatusText") { Converter=new ConnectedVideoVisibilityConverter() });
        internal static void BindJogVisibility(FrameworkElement overlay,bool ir)
        {
            var binding=new MultiBinding{Converter=new ConnectedRatioVisibilityConverter()};
            binding.Bindings.Add(new Binding(ir?"IrStatusText":"EoStatusText"));
            binding.Bindings.Add(new Binding("IsVideoClickCenterEnabled"));
            overlay.SetBinding(UIElement.VisibilityProperty,binding);
        }
        internal static ToggleButton CreateRatioToggle(bool ir)
        {
            var toggle=new ToggleButton { Content="배율",Width=36,Height=22,Foreground=LegacyVideoIntegration.ChannelBrush(ir),Background=new SolidColorBrush(Color.FromArgb(225,27,34,41)),
                BorderBrush=(Brush)new BrushConverter().ConvertFromString("#53616D"),BorderThickness=new Thickness(1),FontSize=11,FontWeight=FontWeights.Bold,
                Template=RatioToggleTemplate(),
                ToolTip="EO/IR 메인·서브·분리 영상의 배율 표시/숨기기" };
            toggle.SetBinding(ToggleButton.IsCheckedProperty,new Binding("IsZoomRatioOverlayVisible") { Mode=BindingMode.TwoWay });
            BindRatioToggleVisibility(toggle,ir);
            return toggle;
        }
        internal static ControlTemplate RatioToggleTemplate() => (ControlTemplate)XamlReader.Parse(@"<ControlTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' TargetType='ToggleButton'>
<Border x:Name='Surface' Background='{TemplateBinding Background}' BorderBrush='{TemplateBinding BorderBrush}' BorderThickness='1' CornerRadius='2'>
<ContentPresenter HorizontalAlignment='Center' VerticalAlignment='Center'/></Border>
<ControlTemplate.Triggers>
<Trigger Property='IsChecked' Value='True'><Setter TargetName='Surface' Property='Background' Value='#EC26333F'/></Trigger>
<Trigger Property='IsMouseOver' Value='True'><Setter TargetName='Surface' Property='Background' Value='#F0354654'/></Trigger>
<Trigger Property='IsKeyboardFocused' Value='True'><Setter TargetName='Surface' Property='BorderBrush' Value='#C5D2DD'/></Trigger>
</ControlTemplate.Triggers></ControlTemplate>");
        // 2026-10-12 FIX6: same square arrow, overlay padding and colors as the popout guide.
        internal static ControlTemplate MainGuideTemplate() => (ControlTemplate)XamlReader.Parse(@"<ControlTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' TargetType='Expander'>
<Grid><Border x:Name='Info' Visibility='Collapsed' Padding='42,7,10,7' Background='#B21D232A' BorderBrush='{TemplateBinding Foreground}' BorderThickness='1' CornerRadius='3'><ContentPresenter Content='{TemplateBinding Content}'/></Border>
<ToggleButton x:Name='Arrow' Width='28' Height='28' Margin='4,4,0,0' Padding='0' HorizontalAlignment='Left' VerticalAlignment='Top' Content='▼' FontSize='15' FontWeight='Bold' Foreground='White' Background='#B21D232A' BorderBrush='{TemplateBinding Foreground}' BorderThickness='1' IsChecked='{Binding IsExpanded, RelativeSource={RelativeSource TemplatedParent}, Mode=TwoWay}' ToolTip='단축키 안내 표시하기'/></Grid>
<ControlTemplate.Triggers><Trigger Property='IsExpanded' Value='True'><Setter TargetName='Info' Property='Visibility' Value='Visible'/><Setter TargetName='Arrow' Property='Content' Value='▲'/><Setter TargetName='Arrow' Property='ToolTip' Value='단축키 안내 숨기기'/></Trigger></ControlTemplate.Triggers></ControlTemplate>");
        internal static StackPanel CreateContent(bool ir)
        {
            var panel=new StackPanel();
            Action<string,string,double,bool> add=(text,color,size,bold)=>panel.Children.Add(new TextBlock{
                Text=text,Foreground=(Brush)new BrushConverter().ConvertFromString(color),FontSize=size,
                FontWeight=bold?FontWeights.Bold:FontWeights.Normal,Margin=new Thickness(0,panel.Children.Count==0?0:4,0,0)});
            add((ir?"IR":"EO")+" CAMERA / SHORTCUT CONTROL",ir?LegacyVideoIntegration.IrColor:LegacyVideoIntegration.EoColor,18,true);
            add("R : EO MAIN    T : IR MAIN","#FFD166",13,true);
            add("ARROW KEYS : PAN / TILT CONTROL","#9FE870",13,true);
            add("Z : PT SPEED −    C : PT SPEED +  (HOLD)","#E5B7FF",13,true);
            add("W : ZOOM IN    S : ZOOM OUT","#8FD3FF",13,true);
            add("A : FOCUS NEAR    D : FOCUS FAR","#FFBC8A",13,true);
            add("키를 떼면 해당 입력이 정지합니다.","#D0D0D0",12,false);
            var warning=new TextBlock{Text="자동 운용 / 잠금 중에는 수동 제어가 제한됩니다.",Foreground=Brushes.Salmon,FontSize=13,Margin=new Thickness(0,7,0,0)};
            warning.SetBinding(UIElement.VisibilityProperty,new Binding("IsControlInputLocked"){Converter=new BooleanToVisibilityConverter()});panel.Children.Add(warning);
            return panel;
        }
    }
    internal sealed class DeviceOnOffBrushConverter : IValueConverter
    {
        private static readonly Brush OnBrush=LegacyVideoIntegration.FrozenBrush("#55D187");
        private static readonly Brush OffBrush=LegacyVideoIntegration.FrozenBrush("#FF6B6B");
        public object Convert(object value,Type targetType,object parameter,CultureInfo culture) =>
            string.Equals(value as string,"ON",StringComparison.OrdinalIgnoreCase)?OnBrush:OffBrush;
        public object ConvertBack(object value,Type targetType,object parameter,CultureInfo culture)=>Binding.DoNothing;
    }
    internal sealed class ConnectedVideoVisibilityConverter : IValueConverter
    {
        public object Convert(object value,Type targetType,object parameter,CultureInfo culture) =>
            value is string status && status.EndsWith("] Connected",StringComparison.Ordinal) ? Visibility.Visible : Visibility.Collapsed;
        public object ConvertBack(object value,Type targetType,object parameter,CultureInfo culture) => Binding.DoNothing;
    }
    internal sealed class ConnectedRatioVisibilityConverter : IMultiValueConverter
    {
        public object Convert(object[] values,Type targetType,object parameter,CultureInfo culture) =>
            values.Length==2 && values[0] is string status && status.EndsWith("] Connected",StringComparison.Ordinal) &&
            values[1] is bool visible && visible ? Visibility.Visible : Visibility.Collapsed;
        public object[] ConvertBack(object value,Type[] targetTypes,object parameter,CultureInfo culture) =>
            new object[]{Binding.DoNothing,Binding.DoNothing};
    }
}
