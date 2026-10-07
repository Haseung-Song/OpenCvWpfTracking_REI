using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using OpenCvWpfTracking.Common;
using OpenCvWpfTracking.Models.Main;
using OpenCvWpfTracking.ViewModels.Main;

namespace OpenCvWpfTracking.Services.Configuration
{
    public sealed class DeviceEntry
    {
        public string Id, Name, AgentIp, AgentPort, EoUrl, EoUser, EoPassword, IrUrl, IrUser, IrPassword;
        public string ControlIp, ControlUser, ControlPassword;
        public ControlAgentType AgentType;
        public CameraControlType EoControlType;
        public bool UseHttps, PositionSensors;
        public int Order;
        public string Label => Order.ToString(CultureInfo.InvariantCulture) + ". " + Name;
        public string EoLabel => Label + " - EO";
        public string IrLabel => Label + " - IR";
        public string EoAddress => DeviceCatalog.Address(EoUrl,EoUser,EoPassword);
        public string IrAddress => DeviceCatalog.Address(IrUrl,IrUser,IrPassword);
    }

    // UTF-8 CSV is a sensitive configuration file, not encrypted credential storage.
    // Invalid configuration is never silently replaced or logged with row contents.
    public static class DeviceCatalog
    {
        public const string Header="DeviceId,Name,AgentType,AgentIp,AgentPort,EoUrl,EoId,EoPw,IrUrl,IrId,IrPw,EoControlType,EoControlIp,EoControlId,EoControlPw,EoHttps,PositionSensors";
        private static readonly Lazy<List<DeviceEntry>> Cache=new Lazy<List<DeviceEntry>>(()=>
        {
            try { string path=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"Config","Devices.csv");var entries=Load(path);CsvExcelCompanion.Schedule(path);return entries; }
            catch(Exception) { ConsoleLogHelper.State("DEVICE CSV","장비 설정 읽기 실패 / Config/Devices.csv 확인 필요 (원본 유지)"); return new List<DeviceEntry>(); }
        });
        public static IReadOnlyList<DeviceEntry> Entries => Cache.Value;
        public static DeviceEntry Get(string id) => Entries.FirstOrDefault(e=>e.Id==id) ?? new DeviceEntry { Name="", EoUrl="",IrUrl="" };
        public static string Address(string url,string user,string password)
        {
            if(string.IsNullOrEmpty(url)) return "";
            if(string.IsNullOrEmpty(user) && string.IsNullOrEmpty(password)) return url;
            var u=new UriBuilder(url) { UserName=user??"",Password=password??"" };
            return u.Uri.AbsoluteUri;
        }
        public static List<DeviceEntry> Load(string path)
        {
            var rows=Parse(File.ReadAllText(path,Encoding.UTF8));
            if(rows.Count<2 || string.Join(",",rows[0])!=Header) throw new FormatException("Invalid device CSV header.");
            var result=new List<DeviceEntry>(); var ids=new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach(var row in rows.Skip(1))
            {
                if(row.All(string.IsNullOrWhiteSpace)) continue;
                if(row.Count!=17 || result.Count>=256) throw new FormatException("Invalid device CSV field count.");
                ControlAgentType agent; CameraControlType control; int port; bool https,sensors;
                if(!ids.Add(row[0]) || string.IsNullOrWhiteSpace(row[0]) || string.IsNullOrWhiteSpace(row[1]) ||
                    row[1].Any(char.IsControl) || row[1].Contains("://") ||
                    !Enum.TryParse(row[2],true,out agent) || !Enum.IsDefined(typeof(ControlAgentType),agent) ||
                    string.IsNullOrWhiteSpace(row[3]) || !int.TryParse(row[4],out port) || port<1 || port>65535 ||
                    !Enum.TryParse(row[11],true,out control) || !Enum.IsDefined(typeof(CameraControlType),control) ||
                    !bool.TryParse(row[15],out https) || !bool.TryParse(row[16],out sensors))
                    throw new FormatException("Invalid device CSV values.");
                foreach(var index in new[]{5,8})
                {
                    Uri uri;
                    if(!Uri.TryCreate(row[index],UriKind.Absolute,out uri) || uri.Scheme!="rtsp" || string.IsNullOrWhiteSpace(uri.Host) || uri.UserInfo.Length>0)
                        throw new FormatException("RTSP URL must exclude credentials; use separate Id/Pw columns.");
                }
                if(control==CameraControlType.CtecCgi && string.IsNullOrWhiteSpace(row[12])) throw new FormatException("Missing camera control host.");
                result.Add(new DeviceEntry { Id=row[0],Name=row[1],AgentType=agent,AgentIp=row[3],AgentPort=row[4],
                    EoUrl=row[5],EoUser=row[6],EoPassword=row[7],IrUrl=row[8],IrUser=row[9],IrPassword=row[10],
                    EoControlType=control,ControlIp=row[12],ControlUser=row[13],ControlPassword=row[14],
                    UseHttps=https,PositionSensors=sensors,Order=result.Count+1 });
            }
            if(result.Count==0) throw new FormatException("Empty device CSV.");
            return result;
        }
        // Standard quoted CSV, including commas, doubled quotes, and quoted newlines.
        internal static List<List<string>> Parse(string text)
        {
            return CsvExcelCompanion.Parse(text);
        }
    }
}

