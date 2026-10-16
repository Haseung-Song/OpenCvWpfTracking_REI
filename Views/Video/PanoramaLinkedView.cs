using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using System.Xml.Linq;
using OpenCvWpfTracking.Models.Main;
using OpenCvWpfTracking.ViewModels.Main;

namespace OpenCvWpfTracking
{
    // A saved panorama is navigated by telemetry; it is not a live stitched image.
    public sealed class PanoramaLinkedView:FrameworkElement
    {
        public static readonly DependencyProperty SourceProperty=DependencyProperty.Register("Source",typeof(ImageSource),typeof(PanoramaLinkedView),new FrameworkPropertyMetadata(null,FrameworkPropertyMetadataOptions.AffectsRender,(d,e)=>((PanoramaLinkedView)d).InvalidateReference()));
        public ImageSource Source{get=>(ImageSource)GetValue(SourceProperty);set=>SetValue(SourceProperty,value);}
        private readonly MainViewModel vm;private readonly Func<string> sourceId;
        private readonly DispatcherTimer timer;private bool registered,registerNext,follow;
        private double anchorU=.5,anchorV=.5,anchorPan,anchorTilt,verticalSpan=24;
        private string key,lastKey;
        private Rect area;private double centerU,centerV,worldWidth,worldHeight,imageX,imageY;
        private readonly string file=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"Config","PanoramaRegistration.xml");
        public PanoramaLinkedView(MainViewModel model,Func<string> identity)
        {
            vm=model;sourceId=identity;ClipToBounds=true;Cursor=Cursors.Hand;
            timer=new DispatcherTimer{Interval=TimeSpan.FromMilliseconds(120)};timer.Tick+=(s,e)=>InvalidateVisual();
            Loaded+=(s,e)=>{vm.PositionReferenceChanged-=InvalidatePositionReference;vm.PositionReferenceChanged+=InvalidatePositionReference;timer.Start();};
            Unloaded+=(s,e)=>{timer.Stop();vm.PositionReferenceChanged-=InvalidatePositionReference;};
            ToolTip="상단 GUI에서 경로 선택·기준 맞춤·시선 따라가기 / 지점 클릭: 선택";
            MouseLeftButtonDown+=Click;
        }
        private void InvalidateReference()
        {
            if(!Dispatcher.CheckAccess()){if(!Dispatcher.HasShutdownStarted)Dispatcher.BeginInvoke(new Action(InvalidateReference));return;}
            registered=false;follow=false;registerNext=false;InvalidateVisual();
        }
        public void SetFollowing(bool enabled){follow=enabled&&registered;InvalidateVisual();}
        private bool referenceWriteFailed;
        private void InvalidatePositionReference()
        {
            if(!Dispatcher.CheckAccess()){if(!Dispatcher.HasShutdownStarted)Dispatcher.BeginInvoke(new Action(InvalidatePositionReference));return;}
            InvalidateReference();
            // 2026-10-16: ZERO invalidates saved pose references too; reloading must not revive old alignment.
            try{if(File.Exists(file)){
                var doc=XDocument.Load(file);
                foreach(var entry in doc.Root.Elements("Image").Where(p=>((string)p.Attribute("Key")??"").StartsWith(vm.PanoramaDeviceKey+"|",StringComparison.Ordinal)))entry.SetAttributeValue("Invalid",true);
                WriteRegistration(doc);
            }}catch(Exception error){referenceWriteFailed=true;Common.ConsoleLogHelper.Error("PANORAMA REFERENCE","Reference invalidated; saved-reference loading blocked",error);}
        }
        public void BeginRegistration(){if(!vm.PanoramaPoseFresh)throw new InvalidOperationException("최신 PAN/TILT 수신값이 필요합니다.");follow=false;registerNext=true;InvalidateVisual();}
        public void LoadRegistration(){var p=ReadEntry();if(p==null)throw new InvalidOperationException("저장된 기준이 없습니다. '사진 기준 맞춤'을 선택하세요.");anchorU=(double)p.Attribute("U");anchorV=(double)p.Attribute("V");anchorPan=(double)p.Attribute("Pan");anchorTilt=(double)p.Attribute("Tilt");verticalSpan=(double)p.Attribute("Span");registered=Services.Control.OpticalCenterCalibration.Valid(anchorU,anchorV)&&verticalSpan>0&&verticalSpan<=180&&!double.IsNaN(anchorPan)&&!double.IsInfinity(anchorPan)&&!double.IsNaN(anchorTilt)&&!double.IsInfinity(anchorTilt);if(!registered)throw new InvalidOperationException("저장된 기준 데이터 오류");InvalidateVisual();}
        private XElement ReadEntry()=>!referenceWriteFailed&&File.Exists(file)?XDocument.Load(file).Root.Elements("Image").FirstOrDefault(p=>(string)p.Attribute("Key")==key&&(bool?)p.Attribute("Invalid")!=true):null;
        private void Save()
        {
            var doc=File.Exists(file)?XDocument.Load(file):new XDocument(new XElement("Panoramas"));doc.Root.Elements("Image").Where(p=>(string)p.Attribute("Key")==key).Remove();
            doc.Root.Add(new XElement("Image",new XAttribute("Key",key),new XAttribute("U",anchorU),new XAttribute("V",anchorV),new XAttribute("Pan",anchorPan),new XAttribute("Tilt",anchorTilt),new XAttribute("Span",verticalSpan)));
            WriteRegistration(doc);referenceWriteFailed=false;
        }
        private void WriteRegistration(XDocument doc){Directory.CreateDirectory(Path.GetDirectoryName(file));string temp=file+".tmp";doc.Save(temp);if(File.Exists(file))File.Replace(temp,file,file+".bak");else File.Move(temp,file);}
        public static double Wrap(double value)=>value-Math.Floor(value);
        protected override void OnRender(DrawingContext dc)
        {
            base.OnRender(dc);dc.DrawRectangle(new SolidColorBrush(Color.FromRgb(16,22,28)),null,new Rect(RenderSize));
            var image=Source;if(image==null||image.Width<=0||ActualWidth<1||ActualHeight<1||!vm.IsContextPanelsVisible)return;
            key=vm.PanoramaDeviceKey+"|"+sourceId()+"|"+image.Width+"x"+image.Height;
            if(lastKey!=key){registered=false;follow=false;registerNext=false;lastKey=key;}
            double fit=Math.Min(ActualWidth/image.Width,ActualHeight/image.Height);
            area=new Rect((ActualWidth-image.Width*fit)/2,(ActualHeight-image.Height*fit)/2,image.Width*fit,image.Height*fit);
            centerU=Wrap(anchorU+(vm.CurrentPanoramaPan-anchorPan)/360.0);centerV=anchorV-(vm.CurrentPanoramaTilt-anchorTilt)/verticalSpan;
            if(follow&&registered){worldWidth=ActualWidth*4;worldHeight=worldWidth*image.Height/image.Width;imageX=ActualWidth/2-centerU*worldWidth;imageY=ActualHeight/2-centerV*worldHeight;area=new Rect(0,0,ActualWidth,ActualHeight);}
            else{worldWidth=area.Width;worldHeight=area.Height;imageX=area.X;imageY=area.Y;}
            dc.PushClip(new RectangleGeometry(area));
            for(int i=follow?-1:0;i<=(follow?1:0);i++)dc.DrawImage(image,new Rect(imageX+i*worldWidth,imageY,worldWidth,worldHeight));
            if(registered&&vm.PanoramaPoseFresh)
            {
                Point? previous=null;var route=vm.CurrentRoute;
                var points=route.PanOrder?route.Points.OrderBy(p=>p.Pan).ThenBy(p=>p.Tilt):route.Points.OrderBy(p=>p.Order);
                foreach(var p in points){Point pos=Project(p.Pan,p.Tilt);var color=LegacyVideoIntegration.ChannelBrush(vm.RouteInfrared);
                    if(previous.HasValue){Point end=pos;double difference=pos.X-previous.Value.X;if(Math.Abs(difference)>worldWidth/2)end.X+=difference>0?-worldWidth:worldWidth;
                        for(int copy=-1;copy<=1;copy++)dc.DrawLine(new Pen(color,1.5),previous.Value+new Vector(copy*worldWidth,0),end+new Vector(copy*worldWidth,0));}
                    for(int copy=-1;copy<=1;copy++){Point shown=pos+new Vector(copy*worldWidth,0);bool active=vm.SelectedRoutePoint!=null&&vm.SelectedRoutePoint.Order==p.Order;dc.DrawEllipse(active?Brushes.White:color,new Pen(Brushes.Black,1),shown,active?6:4,active?6:4);if(route.Points.Count<120||active)DrawText(dc,p.Order.ToString(),shown+new Vector(6,-14),color);}previous=pos;
                }
                bool ir=vm.IsPanoramaIrSource;var tuning=vm.ControlTuningForPanorama(ir);
                double hfov=tuning.Item1,vfov=2*Math.Atan(Math.Tan(hfov*Math.PI/360)/tuning.Item2)*180/Math.PI;
                Point c=Project(vm.CurrentPanoramaPan,vm.CurrentPanoramaTilt);double w=Math.Max(3,hfov/360*worldWidth),h=Math.Max(3,vfov/verticalSpan*worldHeight);
                for(int copy=-1;copy<=1;copy++)dc.DrawRectangle(null,new Pen(LegacyVideoIntegration.ChannelBrush(ir),2),new Rect(c.X+copy*worldWidth-w/2,c.Y-h/2,w,h));
            }
            dc.Pop();DrawText(dc,registerNext?"현재 영상과 같은 기준물을 사진에서 클릭":!registered?"상단 '사진 기준 맞춤'으로 현재 시선 연결":!vm.PanoramaPoseFresh?"위치 수신 대기 · 표시 중지":(follow?"시선 따라가기":"전체 보기")+" · "+vm.CurrentRoute.Name+" / "+vm.CurrentRoute.Points.Count+"점",new Point(8,5),Brushes.LightSteelBlue);
        }
        private Point Project(double pan,double tilt)
        {
            double u=Wrap(anchorU+(pan-anchorPan)/360.0),v=anchorV-(tilt-anchorTilt)/verticalSpan;
            double x=imageX+u*worldWidth;if(follow){while(x< -worldWidth/2)x+=worldWidth;while(x>ActualWidth+worldWidth/2)x-=worldWidth;}
            return new Point(x,imageY+v*worldHeight);
        }
        private static void DrawText(DrawingContext dc,string text,Point at,Brush color)
        {dc.DrawText(new FormattedText(text,CultureInfo.CurrentCulture,FlowDirection.LeftToRight,new Typeface("Malgun Gothic"),11,color,1),at);}
        private void Click(object sender,MouseButtonEventArgs e)
        {
            Point mouse=e.GetPosition(this);
            if(registerNext&&area.Contains(mouse)&&vm.PanoramaPoseFresh){try{anchorU=(mouse.X-area.X)/area.Width;anchorV=(mouse.Y-area.Y)/area.Height;anchorPan=vm.CurrentPanoramaPan;anchorTilt=vm.CurrentPanoramaTilt;verticalSpan=vm.SelectedPanoramaTiltRangeDegrees;Save();registered=true;registerNext=false;InvalidateVisual();e.Handled=true;}catch(Exception error){MessageBox.Show(error.Message);}return;}
            if(!registered||!vm.PanoramaPoseFresh||!area.Contains(mouse))return;
            // Select copies at both sides of the 360-degree seam, not only the central projection.
            var hit=vm.CurrentRoute.Points.Select(p=>new{Point=p,Distance=Enumerable.Range(-1,3).Min(copy=>(Project(p.Pan,p.Tilt)+new Vector(copy*worldWidth,0)-mouse).Length)}).OrderBy(p=>p.Distance).FirstOrDefault();
            if(hit!=null&&hit.Distance<14){vm.SelectedRoutePoint=hit.Point;InvalidateVisual();e.Handled=true;}
        }
    }
}
