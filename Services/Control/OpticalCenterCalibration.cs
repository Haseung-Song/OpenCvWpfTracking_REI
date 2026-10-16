using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Xml.Linq;

namespace OpenCvWpfTracking.Services.Control
{
    // Normalized source-image coordinates, independent of window size and DPI.
    public sealed class OpticalCenterCalibration
    {
        private readonly List<Tuple<string,int,double,double>> anchors=new List<Tuple<string,int,double,double>>();
        private readonly string file;
        public OpticalCenterCalibration(string filePath)
        {
            file=filePath;
            if(!File.Exists(file))return;
            try{foreach(var e in XDocument.Load(file).Root.Elements("Point")){
                string key=(string)e.Attribute("Device");int zoom=(int)e.Attribute("Zoom");
                double x=(double)e.Attribute("X"),y=(double)e.Attribute("Y");
                if(Valid(x,y)&&zoom>=0&&zoom<=1000)anchors.Add(Tuple.Create(key,zoom,x,y));
            }}catch(Exception e){Common.ConsoleLogHelper.Warning("OPTICAL CENTER","Load failed: "+e.Message);}
        }
        public static bool Valid(double x,double y)=>!double.IsNaN(x)&&!double.IsNaN(y)&&!double.IsInfinity(x)&&!double.IsInfinity(y)&&x>=0&&x<=1&&y>=0&&y<=1;
        public Point Get(string key,int zoom)
        {
            var a=anchors.Where(p=>p.Item1==key).OrderBy(p=>p.Item2).ToArray();
            if(a.Length==0)return new Point(.5,.5);
            if(zoom<=a[0].Item2)return new Point(a[0].Item3,a[0].Item4);
            for(int i=1;i<a.Length;i++)if(zoom<=a[i].Item2){double t=(zoom-a[i-1].Item2)/(double)(a[i].Item2-a[i-1].Item2);return new Point(a[i-1].Item3+t*(a[i].Item3-a[i-1].Item3),a[i-1].Item4+t*(a[i].Item4-a[i-1].Item4));}
            return new Point(a[a.Length-1].Item3,a[a.Length-1].Item4);
        }
        public void Save(string key,int zoom,Point point)
        {
            if(!Valid(point.X,point.Y)||zoom<0||zoom>1000)throw new ArgumentException("Invalid optical center");
            var next=anchors.Where(p=>p.Item1!=key||p.Item2!=zoom).ToList();next.Add(Tuple.Create(key,zoom,point.X,point.Y));Persist(next);anchors.Clear();anchors.AddRange(next);
        }
        public void Reset(string key){var next=anchors.Where(p=>p.Item1!=key).ToList();Persist(next);anchors.Clear();anchors.AddRange(next);}
        private void Persist(List<Tuple<string,int,double,double>> next)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(file));
            var doc=new XDocument(new XElement("OpticalCenters",new XAttribute("Version",1),next.Select(p=>new XElement("Point",new XAttribute("Device",p.Item1),new XAttribute("Zoom",p.Item2),new XAttribute("X",p.Item3.ToString("R",CultureInfo.InvariantCulture)),new XAttribute("Y",p.Item4.ToString("R",CultureInfo.InvariantCulture))))));
            string tmp=file+".tmp";doc.Save(tmp);if(File.Exists(file))File.Replace(tmp,file,file+".bak");else File.Move(tmp,file);
        }
        // Legacy C++ four-point optical-axis intersection, with vertical/parallel checks.
        public static bool TryIntersection(Point aLow,Point bLow,Point aHigh,Point bHigh,out Point center)
        {
            center=new Point();Vector a=aHigh-aLow,b=bHigh-bLow;
            double determinant=a.X*b.Y-a.Y*b.X;
            if(a.Length<.01||b.Length<.01||Math.Abs(determinant)/(a.Length*b.Length)<.08)return false;
            Vector q=bLow-aLow;double t=(q.X*b.Y-q.Y*b.X)/determinant;
            center=aLow+t*a;return Valid(center.X,center.Y);
        }
    }
}
