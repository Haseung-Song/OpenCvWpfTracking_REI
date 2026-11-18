
namespace OpenCvWpfTracking.ViewModels.Main
{
    public partial class MainViewModel
    {

        public bool IsPresetPanOrderMode
        {
            get { return _scanTraversalFeature.IsPresetPanOrderMode; }
            set { _scanTraversalFeature.IsPresetPanOrderMode = value; }
        }

        public bool IsPresetPingPong
        {
            get { return _scanTraversalFeature.IsPresetPingPong; }
            set { _scanTraversalFeature.IsPresetPingPong = value; }
        }

    }
}
