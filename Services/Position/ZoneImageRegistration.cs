using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Media.Imaging;

namespace OpenCvWpfTracking.Services.Position
{
    // Bounded feature matching; no lens-position-to-focal-length assumption.
    public static class ZoneImageRegistration
    {
        public static FireSmokeScene Match(FireSmokeScene scene, BitmapSource live)
        {
            if(scene?.ReferencePng==null || live==null) return null;
            return Match(scene,Encode(live));
        }
        public static byte[] Encode(BitmapSource live)
        {
            using(var stream=new MemoryStream())
            { var encoder=new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(live)); encoder.Save(stream); return stream.ToArray(); }
        }
        public static FireSmokeScene Match(FireSmokeScene scene, byte[] current)
        {
            if(scene?.ReferencePng==null || current==null)return null;
            using(var reference=Cv2.ImDecode(scene.ReferencePng,ImreadModes.Grayscale))
            using(var target=Cv2.ImDecode(current,ImreadModes.Grayscale))
            using(var a=Resize(reference)) using(var b=Resize(target))
            using(var orb=ORB.Create(1600)) using(var da=new Mat()) using(var db=new Mat())
            using(var matcher=new BFMatcher(NormTypes.Hamming))
            {
                if(a.Empty() || b.Empty())return null;
                KeyPoint[] ka,kb; orb.DetectAndCompute(a,null,out ka,da); orb.DetectAndCompute(b,null,out kb,db);
                if(da.Empty() || db.Empty())return null;
                var pairs=matcher.KnnMatch(da,db,2).Where(p=>p.Length==2 && p[0].Distance<.70*p[1].Distance && p[0].Distance<60)
                    .Select(p=>p[0]).GroupBy(p=>p.TrainIdx).Select(g=>g.OrderBy(p=>p.Distance).First()).ToArray();
                if(pairs.Length<18)return null;
                var src=pairs.Select(p=>ka[p.QueryIdx].Pt).ToArray(); var dst=pairs.Select(p=>kb[p.TrainIdx].Pt).ToArray();
                using(var mask=new Mat()) using(var si=InputArray.Create(src)) using(var di=InputArray.Create(dst))
                using(var h=Cv2.FindHomography(si,di,HomographyMethods.Ransac,2.5,mask))
                {
                    if(h.Empty())return null;
                    int count=Cv2.CountNonZero(mask); if(count<14 || count<(pairs.Length*.55))return null;
                    var inliers=src.Where((p,i)=>mask.At<byte>(i)!=0).ToArray();
                    // Repeated roof/window texture and a tiny matching patch cannot establish a scene transform.
                    if((inliers.Max(p=>p.X)-inliers.Min(p=>p.X))<a.Width*.20 ||
                       (inliers.Max(p=>p.Y)-inliers.Min(p=>p.Y))<a.Height*.12)return null;
                    var result=new FireSmokeScene {Id=scene.Id,Name=scene.Name,Revision=scene.Revision,Key=scene.Key,Camera=scene.Camera};
                    foreach(var z in scene.Zones)
                    {
                        var corners=new[]{new Point2f((float)(z.X*a.Width),(float)(z.Y*a.Height)),new Point2f((float)((z.X+z.Width)*a.Width),(float)(z.Y*a.Height)),
                            new Point2f((float)((z.X+z.Width)*a.Width),(float)((z.Y+z.Height)*a.Height)),new Point2f((float)(z.X*a.Width),(float)((z.Y+z.Height)*a.Height))};
                        var points=Cv2.PerspectiveTransform(corners,h).Select(p=>new System.Windows.Point(p.X/b.Width,p.Y/b.Height)).ToList();
                        if(points.Any(p=>double.IsNaN(p.X)||double.IsInfinity(p.X)||double.IsNaN(p.Y)||double.IsInfinity(p.Y)))return null;
                        // Reject folded/extreme geometry before it can affect overlay or zone assessments.
                        double orientation=0;
                        for(int i=0;i<4;i++) {var p=points[i];var q=points[(i+1)%4];var r=points[(i+2)%4];double cross=(q.X-p.X)*(r.Y-q.Y)-(q.Y-p.Y)*(r.X-q.X);if(i==0)orientation=cross;else if(cross*orientation<=0)return null;}
                        double area=Math.Abs(points.Select((p,i)=>p.X*points[(i+1)%4].Y-p.Y*points[(i+1)%4].X).Sum())*.5;
                        if(area<z.Width*z.Height*.015 || area>z.Width*z.Height*64)return null;
                        points=ZoneViewProjection.Clip(points,0,0,1,1);if(points.Count<3)continue;
                        double left=points.Min(p=>p.X),top=points.Min(p=>p.Y),right=points.Max(p=>p.X),bottom=points.Max(p=>p.Y);
                        if(right-left<.003 || bottom-top<.003)continue;
                        result.Zones.Add(new FireSmokeZone {Name=z.Name,Kind=z.Kind,Fire=z.Fire,Smoke=z.Smoke,X=left,Y=top,Width=right-left,Height=bottom-top,ProjectedPolygon=points});
                    }
                    return result;
                }
            }
        }
        private static Mat Resize(Mat image)
        {
            var result=new Mat(); if(image.Empty())return result;
            double scale=Math.Min(1,640.0/image.Width);Cv2.Resize(image,result,new Size(Math.Max(1,(int)Math.Round(image.Width*scale)),Math.Max(1,(int)Math.Round(image.Height*scale))),0,0,InterpolationFlags.Area);return result;
        }
    }
}
