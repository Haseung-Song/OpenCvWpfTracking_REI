using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Xml.Linq;
using OpenCvWpfTracking.Common;

namespace OpenCvWpfTracking.Services.Configuration
{
    /// <summary>
    /// 2026-11-06: 클릭 이동 전용 화각과 원본 영상 내부의 유효 영역을 장비·채널별로 읽는다.
    /// 검은 픽셀을 자동으로 잘라내지 않으며 기존 SYNC·영상 표시·배율표는 변경하지 않는다.
    /// </summary>
    public sealed class ClickProjectionSettings
    {
        public double Left { get; private set; }

        public double Top { get; private set; }

        public double Width { get; private set; } = 1;

        public double Height { get; private set; } = 1;

        public double? WideHorizontalFov { get; private set; }

        public string Basis { get; private set; } = "UNVERIFIED_DEFAULT";

        private static readonly Lazy<Dictionary<string, ClickProjectionSettings>> Settings = new Lazy<Dictionary<string, ClickProjectionSettings>>(() => Load(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config", "ClickProjection.xml")));

        /// <summary>고정 ID와 채널로 설정을 조회하고 없는 항목은 원본 영상 전체를 사용한다.</summary>
        public static ClickProjectionSettings Get(string deviceId, bool ir)
        {
            ClickProjectionSettings value;

            string key = DeviceCatalog.CanonicalDeviceId(deviceId) + (ir ? "/IR" : "/EO");

            if (Settings.Value.TryGetValue(key, out value))
            {
                return value;
            }
            else
            {
                return new ClickProjectionSettings();
            }
        }

        /// <summary>한 장비의 잘못된 영역 설정이 다른 장비의 정상 설정을 지우지 않도록 개별 검증한다.</summary>
        public static Dictionary<string, ClickProjectionSettings> Load(string file)
        {
            var result = new Dictionary<string, ClickProjectionSettings>(StringComparer.OrdinalIgnoreCase);

            if (!File.Exists(file))
            {
                return result;
            }

            try
            {
                foreach (var device in XDocument.Load(file).Root.Elements("Device"))
                {
                    string id = DeviceCatalog.CanonicalDeviceId((string)device.Attribute("Id"));

                    foreach (var lens in device.Elements("Lens"))
                    {
                        string channel = (string)lens.Attribute("Channel");

                        double left, top, width, height, fov;

                        bool valid = Read(lens, "Left", 0, out left);

                        valid &= Read(lens, "Top", 0, out top);

                        valid &= Read(lens, "Width", 1, out width);

                        valid &= Read(lens, "Height", 1, out height);

                        valid &= !string.IsNullOrWhiteSpace(id) && (channel == "EO" || channel == "IR");

                        bool hasFov = lens.Attribute("WideHFov") != null;

                        valid &= Read(lens, "WideHFov", 65.24, out fov) && left >= 0 && top >= 0 && width > 0 && height > 0 && left + width <= 1.000000001 && top + height <= 1.000000001 && fov > 0 && fov < 170;

                        string key = id + "/" + channel;

                        if (!valid || result.ContainsKey(key))
                        {
                            // 중복 키는 임의의 마지막 값을 쓰지 않고 해당 채널 전체를 기본값으로 복원한다.
                            result[key] = new ClickProjectionSettings();

                            ConsoleLogHelper.Warning("CLICK PROJECTION", "장비별 화각·영역 설정 오류 / DEVICE_ID=" + id + " / CAMERA=" + channel + " / 기존 기본값 사용");

                            continue;
                        }

                        result[key] = new ClickProjectionSettings
                        {
                            Left = left,
                            Top = top,
                            Width = width,
                            Height = height,
                            WideHorizontalFov = hasFov ? (double?)fov : null,
                            Basis = ((string)lens.Attribute("Basis") ?? "UNVERIFIED_DEFAULT").Trim()
                        };
                    }
                }
            }
            catch (Exception error)
            {
                result.Clear();

                ConsoleLogHelper.Warning("CLICK PROJECTION", "화각·영역 파일 읽기 실패 / 기존 기본값 사용 / " + error.GetType().Name);
            }

            return result;
        }

        /// <summary>디코딩 영상 좌표를 유효 영역 좌표로 바꾸고 영상 내부 여백 클릭은 제외한다.</summary>
        public bool TryMap(double nx, double ny, double aspect, out double x, out double y, out double activeAspect)
        {
            x = y = activeAspect = 0;

            if (!Finite(nx) || !Finite(ny) || !Finite(aspect) || aspect <= 0 || Math.Abs(nx) > 1 || Math.Abs(ny) > 1)
            {
                return false;
            }

            double sourceX = (nx + 1) / 2, sourceY = (ny + 1) / 2;

            if (sourceX < Left || sourceX > Left + Width || sourceY < Top || sourceY > Top + Height)
            {
                return false;
            }

            x = 2 * (sourceX - Left) / Width - 1;

            y = 2 * (sourceY - Top) / Height - 1;

            activeAspect = aspect * Width / Height;

            return true;
        }

        /// <summary>광학 중심은 기존 원본 정규화 좌표로 저장하되 투영 시 같은 유효 영역 기준으로 변환한다.</summary>
        public double CenterX(double source)
        {
            return (source - Left) / Width;
        }

        public double CenterY(double source)
        {
            return (source - Top) / Height;
        }

        /// <summary>영역만 지정한 경우 기존 원본 화각을 탄젠트 공간에서 잘라 투영 배율을 보존한다.</summary>
        public double ResolveWideHorizontalFov(double fullSourceFov)
        {
            return WideHorizontalFov ?? (2 * Math.Atan(Math.Tan(fullSourceFov * Math.PI / 360) * Width) * 180 / Math.PI);
        }

        private static bool Read(XElement element, string name, double fallback, out double value)
        {
            string text = (string)element.Attribute(name);

            value = fallback;

            return text == null || (double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value) && Finite(value));
        }

        private static bool Finite(double value)
        {
            return !double.IsNaN(value) && !double.IsInfinity(value);
        }
    }
}
