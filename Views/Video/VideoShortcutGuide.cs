using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Markup;
using System.Globalization;

namespace OpenCvWpfTracking
{
    // 2026-10-12: one content/style source for main EO/IR and popout shortcut panels.
    internal static class VideoShortcutGuide
    {
        internal static double Scale(double width,double height) => Math.Max(.4,Math.Min(1.15,Math.Min(width/1280,height/720)));
        internal static void BindRatioVisibility(TextBlock label,bool ir)
        {
            label.SetBinding(UIElement.VisibilityProperty,new Binding(ir?"IrStatusText":"EoStatusText") { Converter=new ConnectedVideoVisibilityConverter() });
        }
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
            add((ir?"IR":"EO")+" CAMERA / SHORTCUT CONTROL",ir?"#C6E77B":"#5EE7F7",18,true);
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
    internal sealed class ConnectedVideoVisibilityConverter : IValueConverter
    {
        public object Convert(object value,Type targetType,object parameter,CultureInfo culture) =>
            value is string status && status.EndsWith("] Connected",StringComparison.Ordinal) ? Visibility.Visible : Visibility.Collapsed;
        public object ConvertBack(object value,Type targetType,object parameter,CultureInfo culture) => Binding.DoNothing;
    }
}
