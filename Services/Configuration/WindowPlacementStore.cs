using System;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Xml;
using System.Xml.Linq;

namespace OpenCvWpfTracking.Services.Configuration
{
    /// <summary>화면 작업영역 대비 일반 창의 크기·위치와 마지막 표시 상태를 보관한다.</summary>
    internal sealed class SavedWindowPlacement
    {
        public double WidthRatio { get; set; } = 0.8;

        public double HeightRatio { get; set; } = 0.8;

        public double HorizontalPosition { get; set; } = 0.5;

        public double VerticalPosition { get; set; } = 0.5;

        public Rect MonitorBounds { get; set; }

        public bool FullScreen { get; set; }

        public bool Maximized { get; set; }
    }

    /// <summary>장비 설정과 분리된 사용자별 창 상태를 원자적으로 저장하고 화면 변화에 맞춰 복원한다.</summary>
    internal static class WindowPlacementStore
    {
        internal const double MaximumNormalRatio = 0.85;

        internal static string DefaultPath => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "TORUSS", "REI", "V35", "WindowPlacement.xml");

        /// <summary>유효한 작업영역과 저장 비율을 받아 화면 안쪽 일반 창 사각형을 반환한다.</summary>
        internal static Rect ResolveBounds(Rect workArea, SavedWindowPlacement saved)
        {
            if (!IsValid(workArea))
            {
                throw new ArgumentException("Invalid monitor work area.", nameof(workArea));
            }

            saved = saved ?? new SavedWindowPlacement();

            double maxWidth = workArea.Width * MaximumNormalRatio;

            double maxHeight = workArea.Height * MaximumNormalRatio;

            double width = Clamp(workArea.Width * Clamp(saved.WidthRatio, 0.1, MaximumNormalRatio), Math.Min(960, maxWidth), maxWidth);

            double height = Clamp(workArea.Height * Clamp(saved.HeightRatio, 0.1, MaximumNormalRatio), Math.Min(540, maxHeight), maxHeight);

            return new Rect(
                workArea.Left + (workArea.Width - width) * Clamp(saved.HorizontalPosition, 0, 1),
                workArea.Top + (workArea.Height - height) * Clamp(saved.VerticalPosition, 0, 1),
                width, height);
        }

        /// <summary>일반 창과 현재 작업영역을 상대 비율로 변환해 해상도 변경 후에도 위치 관계를 보존한다.</summary>
        internal static SavedWindowPlacement Capture(Rect bounds, Rect workArea, Rect monitorBounds, bool fullScreen, bool maximized)
        {
            if (!IsValid(bounds) || !IsValid(workArea) || !IsValid(monitorBounds))
            {
                throw new ArgumentException("Invalid window or monitor bounds.");
            }

            return new SavedWindowPlacement
            {
                WidthRatio = Clamp(bounds.Width / workArea.Width, 0.1, MaximumNormalRatio),
                HeightRatio = Clamp(bounds.Height / workArea.Height, 0.1, MaximumNormalRatio),
                HorizontalPosition = workArea.Width > bounds.Width ? Clamp((bounds.Left - workArea.Left) / (workArea.Width - bounds.Width), 0, 1) : 0.5,
                VerticalPosition = workArea.Height > bounds.Height ? Clamp((bounds.Top - workArea.Top) / (workArea.Height - bounds.Height), 0, 1) : 0.5,
                MonitorBounds = monitorBounds,
                FullScreen = fullScreen,
                Maximized = maximized
            };
        }

        /// <summary>사용자 상태 XML을 제한된 크기로 읽고 누락·손상·외부 엔터티가 있으면 기본 상태를 반환한다.</summary>
        internal static SavedWindowPlacement Load(string path)
        {
            if (!File.Exists(path))
            {
                return null;
            }

            try
            {
                var settings = new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null, MaxCharactersInDocument = 8192 };

                using (var reader = XmlReader.Create(path, settings))
                {
                    XElement root = XDocument.Load(reader).Root;

                    if (root == null || root.Name != "WindowPlacement" || (string)root.Attribute("Version") != "1")
                    {
                        return null;
                    }

                    var saved = new SavedWindowPlacement
                    {
                        WidthRatio = Number(root, "WidthRatio"),
                        HeightRatio = Number(root, "HeightRatio"),
                        HorizontalPosition = Number(root, "HorizontalPosition"),
                        VerticalPosition = Number(root, "VerticalPosition"),
                        MonitorBounds = new Rect(Number(root, "MonitorLeft"), Number(root, "MonitorTop"), Number(root, "MonitorWidth"), Number(root, "MonitorHeight")),
                        FullScreen = (bool)root.Attribute("FullScreen"),
                        Maximized = (bool)root.Attribute("Maximized")
                    };

                    return IsValid(saved.MonitorBounds) ? saved : null;
                }
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException || ex is XmlException || ex is FormatException || ex is ArgumentException || ex is OverflowException)
            {
                Common.ConsoleLogHelper.Warning("MAIN WINDOW", "창 상태 파일을 읽지 못해 기본 크기를 사용합니다.");

                return null;
            }
        }

        /// <summary>유효한 창 상태만 임시 XML에 기록한 뒤 기존 파일을 교체하고 임시 파일을 정리한다.</summary>
        internal static void Save(string path, SavedWindowPlacement saved)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path)));

            string temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";

            try
            {
                var root = new XElement("WindowPlacement",
                    new XAttribute("Version", 1),
                    new XAttribute("WidthRatio", saved.WidthRatio),
                    new XAttribute("HeightRatio", saved.HeightRatio),
                    new XAttribute("HorizontalPosition", saved.HorizontalPosition),
                    new XAttribute("VerticalPosition", saved.VerticalPosition),
                    new XAttribute("MonitorLeft", saved.MonitorBounds.Left),
                    new XAttribute("MonitorTop", saved.MonitorBounds.Top),
                    new XAttribute("MonitorWidth", saved.MonitorBounds.Width),
                    new XAttribute("MonitorHeight", saved.MonitorBounds.Height),
                    new XAttribute("FullScreen", saved.FullScreen),
                    new XAttribute("Maximized", saved.Maximized));

                new XDocument(root).Save(temporary);

                if (File.Exists(path))
                {
                    File.Replace(temporary, path, null);
                }
                else
                {
                    File.Move(temporary, path);
                }
            }
            finally
            {
                if (File.Exists(temporary))
                {
                    File.Delete(temporary);
                }
            }
        }

        /// <summary>XML 수치를 문화권과 관계없이 해석하고 무한대·NaN은 손상된 값으로 거부한다.</summary>
        private static double Number(XElement root, string name)
        {
            double value = double.Parse((string)root.Attribute(name) ?? "", CultureInfo.InvariantCulture);

            if (double.IsNaN(value) || double.IsInfinity(value))
            {
                throw new FormatException("Invalid window placement value.");
            }

            return value;
        }

        /// <summary>크기가 양수이고 모든 좌표가 유한한 사각형인지 판별한다.</summary>
        private static bool IsValid(Rect bounds)
        {
            return !bounds.IsEmpty && bounds.Width > 0 && bounds.Height > 0 &&
                !double.IsNaN(bounds.X) && !double.IsInfinity(bounds.X) &&
                !double.IsNaN(bounds.Y) && !double.IsInfinity(bounds.Y) &&
                !double.IsNaN(bounds.Width) && !double.IsInfinity(bounds.Width) &&
                !double.IsNaN(bounds.Height) && !double.IsInfinity(bounds.Height);
        }

        /// <summary>입력 값을 지정한 최솟값과 최댓값 사이로 제한한다.</summary>
        private static double Clamp(double value, double minimum, double maximum)
        {
            return Math.Max(minimum, Math.Min(maximum, value));
        }
    }
}
