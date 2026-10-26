using System;
using System.IO;
using System.Xml.Linq;

namespace OpenCvWpfTracking.ViewModels.Main
{
    public partial class MainViewModel
    {
        private bool traversalLoaded, panOrder, pingPong;

        private string TraversalPath => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config", "ScanTraversal.xml");

        /// <summary>
        /// 저장된 순회 설정을 복원해 현재 자동 감시 구성에 반영한다.
        /// </summary>
        private void LoadTraversal()
        {
            if (traversalLoaded)
                return;

            traversalLoaded = true;

            try
            {
                if (File.Exists(TraversalPath))
                {
                    var root = XDocument.Load(TraversalPath).Root;
                    panOrder = (bool?)root.Attribute("PanOrder") ?? false;
                    pingPong = (bool?)root.Attribute("PingPong") ?? false;
                }
            }
            catch (Exception e)
            {
                Common.ConsoleLogHelper.Warning("SCAN TRAVERSAL", e.Message);
            }
        }

        public bool IsPresetPanOrderMode
        {
            get
            {
                LoadTraversal();
                return panOrder;
            }

            set
            {
                LoadTraversal();
                panOrder = value;
                SaveTraversal();
                OnPropertyChanged();
            }
        }

        public bool IsPresetPingPong
        {
            get
            {
                LoadTraversal();
                return pingPong;
            }

            set
            {
                LoadTraversal();
                pingPong = value;
                SaveTraversal();
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// 현재 순회 설정을 저장한다.
        /// </summary>
        private void SaveTraversal()
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(TraversalPath));
                var doc = new XDocument(new XElement(
                    "Traversal",
                    new XAttribute(
                    "PanOrder",
                    panOrder
                ),
                    new XAttribute(
                    "PingPong",
                    pingPong
                )
                ));
                string temp = TraversalPath + ".tmp";
                doc.Save(temp);

                if (File.Exists(TraversalPath))
                    File.Replace(temp, TraversalPath, TraversalPath + ".bak");
                else
                    File.Move(temp, TraversalPath);
            }
            catch (Exception e)
            {
                Common.ConsoleLogHelper.Warning("SCAN TRAVERSAL", "Save failed: " + e.Message);
            }
        }
    }
}
