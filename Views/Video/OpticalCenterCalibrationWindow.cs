using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using OpenCvWpfTracking.Services.Control;
using OpenCvWpfTracking.ViewModels.Main;

namespace OpenCvWpfTracking
{
    public sealed class OpticalCenterCalibrationWindow:Window
    {
        private readonly MainViewModel vm;private readonly bool ir;
        private readonly Image image;private readonly TextBlock status;
        private readonly CheckBox four=new CheckBox{Content="4점 광축 교점 계산",Foreground=Brushes.White};
        private readonly CheckBox confirmed=new CheckBox{Content="D-ZOOM OFF 및 영상 안정화 상태 확인",Foreground=Brushes.White};
        private readonly List<Point> points=new List<Point>();private readonly List<int> zooms=new List<int>();
        private Point? candidate;private Point pose;private int candidateZoom;
        public OpticalCenterCalibrationWindow(MainViewModel model,bool isIr)
        {
            vm=model;ir=isIr;Title=(ir?"IR":"EO")+" 영상 중심 교정";
            Width=Math.Min(1000,SystemParameters.WorkArea.Width*.9);Height=Math.Min(700,SystemParameters.WorkArea.Height*.9);
            MinWidth=Math.Min(600,Width);MinHeight=Math.Min(400,Height);WindowStartupLocation=WindowStartupLocation.CenterOwner;
            Background=new SolidColorBrush(Color.FromRgb(32,40,48));
            var root=new DockPanel{Margin=new Thickness(12)};Content=root;
            var tools=new StackPanel();var scroll=new ScrollViewer{Content=tools,VerticalScrollBarVisibility=ScrollBarVisibility.Auto,HorizontalScrollBarVisibility=ScrollBarVisibility.Disabled,Padding=new Thickness(3,4,12,12),MaxHeight=Height*.45};
            DockPanel.SetDock(scroll,Dock.Top);root.Children.Add(scroll);SizeChanged+=(s,e)=>scroll.MaxHeight=Math.Max(80,ActualHeight*.45);
            tools.Children.Add(new TextBlock{Foreground=Brushes.White,TextWrapping=TextWrapping.Wrap,Text="수동: 실제 광축 위치를 클릭 후 저장. 4점: PAN/TILT 고정, 낮은 줌에서 A·B → 높은 줌에서 같은 A·B 클릭. 줌은 본창에서 조작합니다. 모터/영상 자체를 이동하지 않습니다.",Margin=new Thickness(0,0,0,8)});
            tools.Children.Add(four);tools.Children.Add(confirmed);
            var buttons=new WrapPanel{Orientation=Orientation.Horizontal};tools.Children.Add(buttons);
            Add(buttons,"적용·저장",()=>{
                try{if(candidate==null||confirmed.IsChecked!=true)throw new InvalidOperationException("후보 선택과 D-ZOOM OFF 확인이 필요합니다.");
                    if(vm.OpticalZoom(ir)!=candidateZoom||PoseDifference(vm.OpticalPose,pose)>.005)throw new InvalidOperationException("후보 계산 후 줌 또는 PAN/TILT가 바뀌었습니다. 다시 선택하세요.");
                    vm.SaveOpticalCenter(ir,candidate.Value);status.Text="저장 완료: "+candidate.Value+" / ZOOM="+candidateZoom;points.Clear();zooms.Clear();
                }catch(Exception e){status.Text=e.Message;}});
            Add(buttons,"선택 취소",()=>{points.Clear();zooms.Clear();candidate=null;status.Text="다시 선택하세요.";});
            Add(buttons,"기본 중심 복원",()=>{try{vm.ResetOpticalCenter(ir);status.Text="기본 중심(0.5, 0.5)으로 복원했습니다.";}catch(Exception e){status.Text=e.Message;}});
            status=new TextBlock{Foreground=Brushes.LightSteelBlue,TextWrapping=TextWrapping.Wrap,Margin=new Thickness(0,5,0,8),Text="실측 교정입니다. 정렬 오차나 기구 유격을 숨기는 용도로 사용하지 마십시오."};tools.Children.Add(status);
            image=new Image{Stretch=Stretch.Uniform};image.SetBinding(Image.SourceProperty,new Binding(ir?"IRCameraImage":"EOCameraImage"){Source=vm});root.Children.Add(image);
            image.MouseLeftButtonDown+=Click;
        }
        private static void Add(Panel panel,string text,Action action){var b=new Button{Content=text,Margin=new Thickness(0,6,8,0),Padding=new Thickness(12,5,12,5)};b.Click+=(s,e)=>action();panel.Children.Add(b);}
        private static double PoseDifference(Point a,Point b){double dx=Math.Abs(a.X-b.X)%360;return Math.Max(Math.Min(dx,360-dx),Math.Abs(a.Y-b.Y));}
        private void Click(object sender,MouseButtonEventArgs e)
        {
            if(!vm.OpticalFeedbackFresh(ir)){status.Text="최신 장비 위치와 줌 응답을 기다리세요.";return;}
            var bitmap=image.Source as BitmapSource;if(bitmap==null)return;
            double scale=Math.Min(image.ActualWidth/bitmap.PixelWidth,image.ActualHeight/bitmap.PixelHeight);Point p=e.GetPosition(image);
            double x=(p.X-(image.ActualWidth-bitmap.PixelWidth*scale)/2)/(bitmap.PixelWidth*scale),y=(p.Y-(image.ActualHeight-bitmap.PixelHeight*scale)/2)/(bitmap.PixelHeight*scale);
            if(!OpticalCenterCalibration.Valid(x,y))return;
            if(four.IsChecked!=true){candidate=new Point(x,y);candidateZoom=vm.OpticalZoom(ir);pose=vm.OpticalPose;status.Text="수동 후보: "+candidate.Value;return;}
            if(points.Count==0)pose=vm.OpticalPose;
            if(PoseDifference(vm.OpticalPose,pose)>.005){points.Clear();zooms.Clear();candidate=null;status.Text="PAN/TILT 변화 감지. 4점을 다시 선택하세요.";return;}
            points.Add(new Point(x,y));zooms.Add(vm.OpticalZoom(ir));status.Text="선택 "+points.Count+"/4";
            if(points.Count<4)return;
            Point c;
            if(Math.Abs(zooms[0]-zooms[1])>12||Math.Abs(zooms[2]-zooms[3])>12||zooms[2]-zooms[0]<50||!OpticalCenterCalibration.TryIntersection(points[0],points[1],points[2],points[3],out c))
            {candidate=null;status.Text="교점 계산 불가: 줌 차이/동일 대상/평행 이동 여부를 확인하세요.";}
            else{candidate=c;candidateZoom=vm.OpticalZoom(ir);status.Text="교점 후보: "+c+". 실제 영상 확인 후 적용·저장하세요.";}
            points.Clear();zooms.Clear();
        }
    }
}
