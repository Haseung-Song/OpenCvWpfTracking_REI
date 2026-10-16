using System;
using System.IO;
using System.Threading;
using System.Windows;
using OpenCvWpfTracking.Services.Control;

namespace OpenCvWpfTracking.ViewModels.Main
{
    public partial class MainViewModel
    {
        private OpticalCenterCalibration opticalCenters;
        private OpticalCenterCalibration OpticalCenters=>opticalCenters??(opticalCenters=new OpticalCenterCalibration(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"Config","OpticalCenters.xml")));
        private static string CameraIdentity(string address)
        {Uri uri;return Uri.TryCreate(address,UriKind.Absolute,out uri)?uri.Scheme+"://"+uri.Host+":"+uri.Port+uri.AbsolutePath:address;}
        private string OpticalCenterKey(bool ir)=>(SelectedControlAgentProfile?.DisplayName??"DIRECT")+"|"+ControlAgentIp+":"+ControlAgentPortText+"|"+CameraIdentity(ir?IrSourceAddress:EoSourceAddress)+"|"+(ir?"IR":"EO");
        public string OpticalDeviceKey(bool ir)=>OpticalCenterKey(ir);
        public int OpticalZoom(bool ir)=>ir?GetCurrentIrZoomStandardPosition():GetCurrentPresetStandardZoom();
        public Point GetOpticalCenter(bool ir)=>OpticalCenters.Get(OpticalCenterKey(ir),OpticalZoom(ir));
        public bool OpticalFeedbackFresh(bool ir)
        {
            var pose=Volatile.Read(ref _videoCenterPose);
            long age=pose==null?long.MaxValue:DateTime.UtcNow.Ticks-pose.Item3;
            return IsLensFresh(ir)&&age>=0&&age<TimeSpan.FromSeconds(2).Ticks;
        }
        public bool SnapshotFrameFresh(bool ir){long age=DateTime.UtcNow.Ticks-Interlocked.Read(ref _videoCenterFrameTicks[ir?1:0]);return age>=0&&age<TimeSpan.FromSeconds(1).Ticks&&(ir?IrConnectionStatusText:EoConnectionStatusText)=="Connected";}
        public Point OpticalPose=>new Point(_currentPan,_currentTilt);
        public double CurrentPanoramaPan=>NormalizeToSignedAngle(_currentPan);
        public double CurrentPanoramaTilt=>_currentTilt;
        // 2026-10-15: one atomic snapshot; disconnect cannot null the second read mid-expression.
        public bool PanoramaPoseFresh{get{var pose=Volatile.Read(ref _videoCenterPose);if(pose==null)return false;long age=DateTime.UtcNow.Ticks-pose.Item3;return age>=0&&age<TimeSpan.FromSeconds(2).Ticks;}}
        public string PanoramaDeviceKey=>(SelectedControlAgentProfile?.DisplayName??"DIRECT")+"|"+ControlAgentIp+":"+ControlAgentPortText;
        public bool IsPanoramaIrSource=>_activePanTiltZoomSource==1;
        public event Action PositionReferenceChanged;
        public Tuple<double,double> ControlTuningForPanorama(bool ir)
        {
            var tuning=ControlTuningFor(ir);double ratio=tuning.Ratio(OpticalZoom(ir));
            double hfov=2*Math.Atan(Math.Tan(tuning.WideHorizontalFov*Math.PI/360)/Math.Max(1,ratio))*180/Math.PI;
            return Tuple.Create(hfov,ir?5.0/4:16.0/9);
        }
        public void SaveOpticalCenter(bool ir,Point center)
        {
            if(!OpticalFeedbackFresh(ir))throw new InvalidOperationException("새 줌/PAN/TILT 수신값이 필요합니다.");
            OpticalCenters.Save(OpticalCenterKey(ir),OpticalZoom(ir),center);
            Common.ConsoleLogHelper.State("OPTICAL CENTER",(ir?"IR":"EO")+" / ZOOM="+OpticalZoom(ir)+" / NORMALIZED="+center);
        }
        public void ResetOpticalCenter(bool ir)=>OpticalCenters.Reset(OpticalCenterKey(ir));
        private void InvalidatePanoramaReference()=>PositionReferenceChanged?.Invoke();
    }
}
