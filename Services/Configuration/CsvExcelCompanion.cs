using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading;
using System.Xml;

namespace OpenCvWpfTracking.Services.Configuration
{
    // CSV remains authoritative. XLSX is a derived, centered Excel view; no Excel/COM dependency.
    public static class CsvExcelCompanion
    {
        private const string Ns="http://schemas.openxmlformats.org/spreadsheetml/2006/main";
        private static readonly object Sync=new object();
        private static readonly Dictionary<string,DateTime> Pending=new Dictionary<string,DateTime>(StringComparer.OrdinalIgnoreCase);
        private static readonly Timer Worker=new Timer(Drain,null,2000,2000);
        private static int Busy;
        public static void Schedule(string path)
        {
            if(string.IsNullOrWhiteSpace(path))return;
            lock(Sync)if(!Pending.ContainsKey(path))Pending[path]=DateTime.UtcNow;
        }
        private static void Drain(object state)
        {
            if(Interlocked.CompareExchange(ref Busy,1,0)!=0)return;
            try
            {
                string[] paths;
                lock(Sync){paths=Pending.Where(p=>(DateTime.UtcNow-p.Value).TotalSeconds>=2).Take(4).Select(p=>p.Key).ToArray();foreach(string p in paths)Pending.Remove(p);}
                foreach(string path in paths)Create(path);
            }
            finally {Interlocked.Exchange(ref Busy,0);}
        }
        public static bool Create(string csvPath)
        {
            string temp=null;
            try
            {
                if(!File.Exists(csvPath))return false;
                if(new FileInfo(csvPath).Length>128000000)throw new InvalidDataException("Excel companion exceeds size limit; CSV retained.");
                List<List<string>> rows;
                using(var stream=new FileStream(csvPath,FileMode.Open,FileAccess.Read,FileShare.Read))
                using(var reader=new StreamReader(stream,Encoding.UTF8,true))rows=Parse(reader.ReadToEnd());
                if(rows.Count==0 || rows.Count>1048576 || rows.Any(r=>r.Count>16384))throw new InvalidDataException("Excel row/column limit; CSV retained.");
                string destination=Path.ChangeExtension(csvPath,"xlsx");temp=destination+"."+Guid.NewGuid().ToString("N")+".tmp";
                using(var file=File.Create(temp))using(var zip=new ZipArchive(file,ZipArchiveMode.Create))
                {
                    Part(zip,"[Content_Types].xml", "<Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\"><Default Extension=\"rels\" ContentType=\"application/vnd.openxmlformats-package.relationships+xml\"/><Default Extension=\"xml\" ContentType=\"application/xml\"/><Override PartName=\"/xl/workbook.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml\"/><Override PartName=\"/xl/styles.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.styles+xml\"/><Override PartName=\"/xl/worksheets/sheet1.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml\"/></Types>");
                    Part(zip,"_rels/.rels","<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\"><Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument\" Target=\"xl/workbook.xml\"/></Relationships>");
                    Part(zip,"xl/workbook.xml","<workbook xmlns=\""+Ns+"\" xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\"><sheets><sheet name=\"Data\" sheetId=\"1\" r:id=\"rId1\"/></sheets></workbook>");
                    Part(zip,"xl/_rels/workbook.xml.rels","<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\"><Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet\" Target=\"worksheets/sheet1.xml\"/><Relationship Id=\"rId2\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles\" Target=\"styles.xml\"/></Relationships>");
                    Part(zip,"xl/styles.xml",Styles());
                    using(var entry=zip.CreateEntry("xl/worksheets/sheet1.xml",CompressionLevel.Fastest).Open())
                    using(var x=XmlWriter.Create(entry,new XmlWriterSettings{Encoding=new UTF8Encoding(false),CheckCharacters=true}))
                    {
                        x.WriteStartElement("worksheet",Ns);x.WriteStartElement("sheetViews",Ns);x.WriteStartElement("sheetView",Ns);x.WriteAttributeString("workbookViewId","0");x.WriteAttributeString("showGridLines","0");
                        x.WriteStartElement("pane",Ns);x.WriteAttributeString("ySplit","1");x.WriteAttributeString("topLeftCell","A2");x.WriteAttributeString("activePane","bottomLeft");x.WriteAttributeString("state","frozen");x.WriteEndElement();x.WriteEndElement();x.WriteEndElement();
                        int columns=rows.Max(r=>r.Count);var widths=new double[columns];
                        for(int col=0;col<columns;col++)widths[col]=Math.Min(48,Math.Max(Math.Max(12,col<rows[0].Count?rows[0][col].Sum(c=>c>255?2:1)*1.25+4:12),rows.Take(2000).Where(r=>r.Count>col).Select(r=>r[col].Sum(c=>c>255?2:1)+2.0).DefaultIfEmpty(12).Max()));
                        x.WriteStartElement("cols",Ns);for(int col=0;col<columns;col++){x.WriteStartElement("col",Ns);x.WriteAttributeString("min",(col+1).ToString());x.WriteAttributeString("max",(col+1).ToString());x.WriteAttributeString("width",widths[col].ToString(CultureInfo.InvariantCulture));x.WriteAttributeString("customWidth","1");x.WriteEndElement();}x.WriteEndElement();
                        x.WriteStartElement("sheetData",Ns);
                        for(int row=0;row<rows.Count;row++)
                        {
                            x.WriteStartElement("row",Ns);x.WriteAttributeString("r",(row+1).ToString());
                            double height=row==0?42:24;for(int col=0;col<rows[row].Count;col++)height=Math.Max(height,Math.Ceiling(rows[row][col].Sum(c=>c>255?2:1)/widths[col])*16+8);
                            x.WriteAttributeString("ht",Math.Min(409,height).ToString(CultureInfo.InvariantCulture));x.WriteAttributeString("customHeight","1");
                            for(int col=0;col<rows[row].Count;col++)Cell(x,Column(col)+(row+1),rows[row][col],col<rows[0].Count?rows[0][col]:"",row==0);
                            x.WriteEndElement();
                        }
                        x.WriteEndElement();x.WriteStartElement("autoFilter",Ns);x.WriteAttributeString("ref","A1:"+Column(columns-1)+rows.Count);x.WriteEndElement();x.WriteEndElement();
                    }
                }
                if(File.Exists(destination))File.Replace(temp,destination,null);else File.Move(temp,destination);
                return true;
            }
            catch(Exception ex){Console.WriteLine("[CSV XLSX] Excel view not updated / CSV retained / "+ex.GetType().Name);return false;}
            finally {if(temp!=null && File.Exists(temp))try{File.Delete(temp);}catch(IOException){} }
        }
        private static void Cell(XmlWriter x,string address,string value,string header,bool title)
        {
            value=value??"";if(value.Length>32767)throw new InvalidDataException("Excel cell too long; CSV retained.");
            int style=title?1:0;string type="inlineStr",serialized=value;
            string key=(header??"").ToUpperInvariant();double number;DateTimeOffset date;bool flag;
            if(!title)
            {
                if((key.Contains("TIME") || key.EndsWith("_AT")) && DateTimeOffset.TryParse(value.StartsWith("=\"") && value.EndsWith("\"")?value.Substring(2,value.Length-3):value,CultureInfo.InvariantCulture,DateTimeStyles.None,out date)) {type="n";style=2;serialized=date.DateTime.ToOADate().ToString("R",CultureInfo.InvariantCulture);}
                else if((key.Contains("SCORE") || key.Contains("CONFIDENCE") || key.Contains("RATIO")) && value.EndsWith("%") && double.TryParse(value.TrimEnd('%'),NumberStyles.Float,CultureInfo.InvariantCulture,out number)) {type="n";style=3;serialized=(number/100).ToString("R",CultureInfo.InvariantCulture);}
                else if(NumericHeader(key) && double.TryParse(value,NumberStyles.Float,CultureInfo.InvariantCulture,out number) && !double.IsNaN(number) && !double.IsInfinity(number)) {type="n";serialized=number.ToString("R",CultureInfo.InvariantCulture);}
                else if(new[]{"EOHTTPS","POSITIONSENSORS","DETECTED"}.Contains(key) && bool.TryParse(value,out flag)){type="b";serialized=flag?"1":"0";}
            }
            x.WriteStartElement("c",Ns);x.WriteAttributeString("r",address);x.WriteAttributeString("s",style.ToString());x.WriteAttributeString("t",type);
            if(type=="inlineStr"){x.WriteStartElement("is",Ns);x.WriteStartElement("t",Ns);x.WriteAttributeString("xml","space","http://www.w3.org/XML/1998/namespace","preserve");x.WriteString(value);x.WriteEndElement();x.WriteEndElement();}else x.WriteElementString("v",Ns,serialized);
            x.WriteEndElement();
        }
        private static bool NumericHeader(string key) => new[]{"SAVED_ORDER","NUMBER","PAN_OR_L_SPEED","TILT_OR_L_DELAY","EO_ZOOM_OR_W_SPEED","EO_FOCUS_OR_W_DELAY","IR_ZOOM","IR_FOCUS","EVENTID","PIXELWIDTH","PIXELHEIGHT","PIXELAREA","OBJECTCOUNT","CONFIDENCE","VISIONSCORE","FRAME","CANDIDATE","X","Y","WIDTH","HEIGHT","AREA_RATIO","AGENTPORT"}.Contains(key);
        private static string Column(int index){string result="";for(index++;index>0;index=(index-1)/26)result=(char)('A'+(index-1)%26)+result;return result;}
        private static void Part(ZipArchive zip,string name,string xml){using(var s=zip.CreateEntry(name,CompressionLevel.Fastest).Open())using(var w=new StreamWriter(s,new UTF8Encoding(false)))w.Write(xml);}
        private static string Styles()
        {
            string alignment="<alignment horizontal=\"center\" vertical=\"center\" wrapText=\"1\"/>";
            return "<styleSheet xmlns=\""+Ns+"\"><numFmts count=\"1\"><numFmt numFmtId=\"164\" formatCode=\"yyyy-mm-dd hh:mm:ss\"/></numFmts><fonts count=\"2\"><font><sz val=\"10\"/><name val=\"Malgun Gothic\"/></font><font><b/><sz val=\"10\"/><color rgb=\"FFFFFFFF\"/><name val=\"Malgun Gothic\"/></font></fonts><fills count=\"3\"><fill><patternFill patternType=\"none\"/></fill><fill><patternFill patternType=\"gray125\"/></fill><fill><patternFill patternType=\"solid\"><fgColor rgb=\"FF344550\"/><bgColor indexed=\"64\"/></patternFill></fill></fills><borders count=\"1\"><border><left/><right/><top/><bottom/><diagonal/></border></borders><cellStyleXfs count=\"1\"><xf numFmtId=\"0\" fontId=\"0\" fillId=\"0\" borderId=\"0\"/></cellStyleXfs><cellXfs count=\"4\">"+
                string.Concat(new[]{0,0,164,10}.Select((format,i)=>"<xf numFmtId=\""+format+"\" fontId=\""+(i==1?1:0)+"\" fillId=\""+(i==1?2:0)+"\" borderId=\"0\" xfId=\"0\" applyAlignment=\"1\" applyNumberFormat=\"1\">"+alignment+"</xf>"))+"</cellXfs><cellStyles count=\"1\"><cellStyle name=\"Normal\" xfId=\"0\" builtinId=\"0\"/></cellStyles></styleSheet>";
        }
        internal static List<List<string>> Parse(string text)
        {
            text=text.TrimStart('\uFEFF');var rows=new List<List<string>>();var row=new List<string>();var field=new StringBuilder();bool quote=false,closed=false;
            for(int i=0;i<text.Length;i++)
            {
                char c=text[i];if(quote){if(c=='"'){if(i+1<text.Length && text[i+1]=='"'){field.Append('"');i++;}else{quote=false;closed=true;}}else field.Append(c);continue;}
                if(c=='"'){if(field.Length>0 || closed)throw new FormatException("Invalid CSV quote.");quote=true;continue;}
                if(c==',' || c=='\r' || c=='\n'){row.Add(field.ToString());field.Clear();closed=false;if(c!=','){rows.Add(row);row=new List<string>();if(c=='\r' && i+1<text.Length && text[i+1]=='\n')i++;}}
                else{if(closed)throw new FormatException("Invalid CSV field.");field.Append(c);}
            }
            if(quote)throw new FormatException("Unclosed CSV quote.");if(field.Length>0 || row.Count>0 || closed){row.Add(field.ToString());rows.Add(row);}return rows;
        }
    }
}
