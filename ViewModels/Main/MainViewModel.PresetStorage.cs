namespace OpenCvWpfTracking.ViewModels.Main
{
    public partial class MainViewModel
    {

        internal bool _presetStorageReady
        {
            get { return _presetStorageFeature._presetStorageReady; }
            set { _presetStorageFeature._presetStorageReady = value; }
        }

        public bool HasRegisteredEquipmentIdentity
        {
            get { return _presetStorageFeature.HasRegisteredEquipmentIdentity; }
        }

        public string RegisteredEquipmentIdentity
        {
            get { return _presetStorageFeature.RegisteredEquipmentIdentity; }
        }

        /// <summary>장비 설정에 등록된 이름을 파노라마와 같은 규칙으로 준비한다.</summary>
        private void InitializeEquipmentPresetFolders()
        {
            _presetStorageFeature.InitializeEquipmentPresetFolders();
        }

        /// <summary>장비를 바꾸면 이전 목록을 비우고 해당 장비의 저장값만 복원한다.</summary>
        private void SwitchEquipmentPresets()
        {
            _presetStorageFeature.SwitchEquipmentPresets();
        }

        internal static string SanitizePresetStorageField(string value)
        {
            return PresetStorageFeature.SanitizePresetStorageField(value);
        }

    }
}
