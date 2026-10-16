using System;
using System.IO;
using System.Xml.Linq;
namespace OpenCvWpfTracking.ViewModels.Main
{
    public partial class MainViewModel
    {
        private bool traversalLoaded,panOrder,pingPong;
        private string TraversalPath=>Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"Config","ScanTraversal.xml");
        private void LoadTraversal()
        {
            if(traversalLoaded)return;traversalLoaded=true;
            try{if(File.Exists(TraversalPath)){var root=XDocument.Load(TraversalPath).Root;panOrder=(bool?)root.Attribute("PanOrder")??false;pingPong=(bool?)root.Attribute("PingPong")??false;}}
            catch(Exception e){Common.ConsoleLogHelper.Warning("SCAN TRAVERSAL",e.Message);}
        }
        public bool IsPresetPanOrderMode{get{LoadTraversal();return panOrder;}set{LoadTraversal();panOrder=value;SaveTraversal();OnPropertyChanged();}}
        public bool IsPresetPingPong{get{LoadTraversal();return pingPong;}set{LoadTraversal();pingPong=value;SaveTraversal();OnPropertyChanged();}}
        private void SaveTraversal()
        {
            try{Directory.CreateDirectory(Path.GetDirectoryName(TraversalPath));var doc=new XDocument(new XElement("Traversal",new XAttribute("PanOrder",panOrder),new XAttribute("PingPong",pingPong)));string temp=TraversalPath+".tmp";doc.Save(temp);if(File.Exists(TraversalPath))File.Replace(temp,TraversalPath,TraversalPath+".bak");else File.Move(temp,TraversalPath);}
            catch(Exception e){Common.ConsoleLogHelper.Warning("SCAN TRAVERSAL","Save failed: "+e.Message);}
        }
    }
}
