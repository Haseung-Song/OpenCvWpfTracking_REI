using System;
using System.IO;
using System.Xml.Linq;

namespace OpenCvWpfTracking.ViewModels.Main
{
    /// <summary>감시 순서 계산에 필요한 협력 기능만 받는 경계입니다. 화면 객체의 내부 구현을 알지 않습니다.</summary>
    internal interface IScanTraversalFeatureContext
    {
        /// <summary>
        /// 바인딩 속성 변경 알림
        /// </summary>
        void OnPropertyChanged([global::System.Runtime.CompilerServices.CallerMemberName] string propertyName = null);
    }

    /// <summary>감시 순서 계산 담당 객체입니다. 상태를 보관하고 입력을 받아 결과를 알립니다.</summary>
    internal sealed class ScanTraversalFeature
    {

        #region [협력 객체와 초기화]

        private readonly IScanTraversalFeatureContext _context;

        /// <summary>
        /// 프리셋 순회 방식 기능에서 사용할 협력 객체를 연결하고 초기 상태를 준비한다.
        /// </summary>
        internal ScanTraversalFeature(IScanTraversalFeatureContext context)
        {
            _context = context ?? throw new System.ArgumentNullException(nameof(context));
        }

        #endregion

        #region [상태 필드]

        internal bool traversalLoaded;

        internal bool panOrder;

        internal bool pingPong;

        #endregion

        #region [화면 속성과 이벤트]

        internal string TraversalPath
        {
            get
            {
                return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config", "ScanTraversal.xml");
            }
        }

        internal bool IsPresetPanOrderMode
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

                _context.OnPropertyChanged();
            }
        }

        internal bool IsPresetPingPong
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

                _context.OnPropertyChanged();
            }
        }

        #endregion

        #region [동작 처리]

        /// <summary>
        /// 저장된 순회 설정을 복원해 현재 자동 감시 구성에 반영한다.
        /// </summary>
        internal void LoadTraversal()
        {
            if (traversalLoaded)
            {
                return;
            }

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

        /// <summary>
        /// 현재 순회 설정을 저장한다.
        /// </summary>
        internal void SaveTraversal()
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(TraversalPath));

                var doc = new XDocument(new XElement("Traversal", new XAttribute("PanOrder", panOrder), new XAttribute("PingPong", pingPong)));

                string temp = TraversalPath + ".tmp";

                doc.Save(temp);

                if (File.Exists(TraversalPath))
                {
                    File.Replace(temp, TraversalPath, TraversalPath + ".bak");
                }
                else
                {
                    File.Move(temp, TraversalPath);
                }
            }
            catch (Exception e)
            {
                Common.ConsoleLogHelper.Warning("SCAN TRAVERSAL", "Save failed: " + e.Message);
            }
        }

        #endregion

    }
}
