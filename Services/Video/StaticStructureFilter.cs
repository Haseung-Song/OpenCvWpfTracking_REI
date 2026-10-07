using OpenCvSharp;
using System;
using System.Collections.Generic;

namespace OpenCvWpfTracking.Services.Video
{
    // V29_6: post-filter only; explicit flame-appearance bypass. Never changes V29_3 flame masks, contour or confirmation rules.
    // Fixed windows/roof edges may flicker in brightness, but their spatial pattern stays rigid.
    internal sealed class StaticStructureFilter
    {
        private sealed class Sample
        {
            internal Rect Anchor, Last;
            internal double[] AnchorPattern, LastPattern;
            internal DateTime Since, LastAt;
            internal int Observations;
            internal bool Matched;
        }
        private readonly List<Sample> _samples = new List<Sample>();
        private Size _size;
        internal Func<DateTime> Clock = () => DateTime.UtcNow;
        internal int SuppressedCount { get; private set; }
        internal void Reset() { _samples.Clear(); _size = default(Size); SuppressedCount = 0; }

        internal IList<Rect> Filter(Mat source, IList<Rect> candidates, DateTime now)
        {
            var visible = new List<Rect>(); SuppressedCount = 0;
            if (source == null || source.Empty()) { Reset(); return candidates ?? visible; }
            if (_size != source.Size()) { Reset(); _size = source.Size(); }
            _samples.RemoveAll(s => (now - s.LastAt).TotalSeconds > 1 || now < s.LastAt);
            foreach (var s in _samples) s.Matched = false;
            using (var gray = new Mat())
            {
                if (source.Channels() == 1) source.CopyTo(gray);
                else Cv2.CvtColor(source, gray, source.Channels() == 4 ? ColorConversionCodes.BGRA2GRAY : ColorConversionCodes.BGR2GRAY);
                foreach (var candidate in candidates ?? new List<Rect>())
                {
                    Rect rect = candidate & new Rect(0, 0, source.Width, source.Height);
                    if (rect.Width < 8 || rect.Height < 8 || HasFlameAppearance(source, gray, rect)) { visible.Add(candidate); continue; }
                    double[] pattern = Pattern(gray, rect);
                    Sample match = null; double best = 0.75;
                    foreach (var s in _samples)
                    {
                        double score = IoU(s.Last, rect);
                        if (!s.Matched && score > best) { best = score; match = s; }
                    }
                    if (match == null)
                    {
                        match = new Sample(); _samples.Add(match); Start(match, rect, pattern, now);
                    }
                    else
                    {
                        bool rigid = SameGeometry(match.Anchor, rect) && SameGeometry(match.Last, rect) &&
                            SamePattern(match.LastPattern, pattern) && SamePattern(match.AnchorPattern, pattern);
                        if (!rigid) Start(match, rect, pattern, now);
                        else { match.Observations++; match.Last = rect; match.LastPattern = pattern; match.LastAt = now; }
                    }
                    match.Matched = true;
                    // Require both elapsed time and observations. Never delay a newly appearing flame.
                    bool suppress = match.Observations >= 60 && (now - match.Since).TotalSeconds >= 6 &&
                        HasRigidEdges(pattern);
                    if (suppress) SuppressedCount++; else visible.Add(candidate);
                }
            }
            // Bounded bookkeeping, not a permanent exclusion map. Changed geometry/pattern releases immediately.
            if (_samples.Count > 128) _samples.RemoveRange(0, _samples.Count - 128);
            return visible;
        }
        // Protect stationary candle/torch cores before static-structure tracking.
        // This is not a thermal-temperature decision; only visible color and silhouette are used.
        private static bool HasFlameAppearance(Mat source, Mat gray, Rect rect)
        {
            if (source.Channels() >= 3)
            {
                using (var roi = new Mat(source, rect)) using (var bgr = new Mat()) using (var hsv = new Mat()) using (var warm = new Mat())
                {
                    if (source.Channels() == 4) Cv2.CvtColor(roi, bgr, ColorConversionCodes.BGRA2BGR); else roi.CopyTo(bgr);
                    Cv2.CvtColor(bgr, hsv, ColorConversionCodes.BGR2HSV);
                    Cv2.InRange(hsv, new Scalar(0,70,150), new Scalar(45,255,255), warm);
                    if (Cv2.CountNonZero(warm) >= Math.Max(3,rect.Width*(double)rect.Height*.08)) return true;
                }
            }
            using (var roi = new Mat(gray, rect)) using (var mask = new Mat())
            {
                double min,max; Cv2.MinMaxLoc(roi,out min,out max);
                if(max-min<80)return false;
                // Both polarities: IR palettes can show a flame bright or dark.
                foreach(bool dark in new[]{false,true})
                {
                    Cv2.Threshold(roi,mask,dark?min+(max-min)*.15:max-(max-min)*.15,255,dark?ThresholdTypes.BinaryInv:ThresholdTypes.Binary);
                    Point[][] contours; HierarchyIndex[] hierarchy; Cv2.FindContours(mask,out contours,out hierarchy,RetrievalModes.External,ContourApproximationModes.ApproxSimple);
                    foreach(var contour in contours)
                    {
                        var box=Cv2.BoundingRect(contour); double area=Cv2.ContourArea(contour),fill=area/Math.Max(1,box.Width*(double)box.Height);
                        if(area<8 || box.Height<box.Width*1.4 || fill<.18 || fill>.78)continue;
                        // A tall rectangular window is not a flame: require a narrower upper tip.
                        int topWidth=0,middleWidth=0;
                        for(int x=box.X;x<box.Right;x++) { if(mask.At<byte>(box.Y+box.Height/5,x)>0)topWidth++; if(mask.At<byte>(box.Y+box.Height/2,x)>0)middleWidth++; }
                        if(middleWidth>=3 && topWidth<middleWidth*.65)return true;
                    }
                }
            }
            return false;
        }
        private static void Start(Sample s, Rect rect, double[] pattern, DateTime now)
        {
            s.Anchor = s.Last = rect; s.AnchorPattern = s.LastPattern = pattern;
            s.Since = s.LastAt = now; s.Observations = 1;
        }
        private static double IoU(Rect a, Rect b)
        {
            Rect c = a & b; double intersection = Math.Max(0, c.Width) * (double)Math.Max(0, c.Height);
            return intersection / Math.Max(1.0, a.Width * (double)a.Height + b.Width * (double)b.Height - intersection);
        }
        private static bool SameGeometry(Rect a, Rect b)
        {
            double area = Math.Max(1, a.Width * (double)a.Height);
            double dx = (a.X + a.Width * .5) - (b.X + b.Width * .5);
            double dy = (a.Y + a.Height * .5) - (b.Y + b.Height * .5);
            return IoU(a, b) >= .94 && Math.Abs(b.Width * (double)b.Height - area) / area <= .04 &&
                Math.Sqrt(dx * dx + dy * dy) <= Math.Max(1.5, Math.Sqrt(area) * .006);
        }
        private static double[] Pattern(Mat gray, Rect rect)
        {
            using (var roi = new Mat(gray, rect)) using (var small = new Mat())
            {
                Cv2.Resize(roi, small, new Size(32, 32), 0, 0, InterpolationFlags.Area);
                Cv2.GaussianBlur(small, small, new Size(3, 3), 0);
                double mean = Cv2.Mean(small).Val0; var values = new double[1024];
                for (int y = 0; y < 32; y++) for (int x = 0; x < 32; x++) values[y * 32 + x] = small.At<byte>(y, x) - mean;
                return values;
            }
        }
        private static bool SamePattern(double[] a, double[] b)
        {
            double sum = 0; int changed = 0;
            for (int i = 0; i < a.Length; i++) { double d = Math.Abs(a[i] - b[i]); sum += d; if (d >= 12) changed++; }
            return sum / a.Length <= 2.5 && changed / (double)a.Length <= .015;
        }
        private static bool HasRigidEdges(double[] p)
        {
            int strong = 0, axis = 0;
            for (int y = 1; y < 31; y++) for (int x = 1; x < 31; x++)
            {
                int i = y * 32 + x; double dx = Math.Abs(p[i + 1] - p[i - 1]), dy = Math.Abs(p[i + 32] - p[i - 32]);
                if (Math.Max(dx, dy) < 18) continue;
                strong++; if (Math.Max(dx, dy) >= Math.Max(1, Math.Min(dx, dy)) * 3) axis++;
            }
            return strong >= 20 && axis / (double)strong >= .70;
        }
    }
}
