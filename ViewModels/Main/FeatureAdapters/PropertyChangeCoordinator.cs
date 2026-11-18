using System;
using System.Threading;

namespace OpenCvWpfTracking.ViewModels.Main
{
    /// <summary>속성 변경에 따른 저장 대상 전환·계측·연관 속성 알림 순서를 담당한다.</summary>
    internal interface IPropertyChangeContext
    {
        void SwitchEquipmentPresets();

        void Publish(string propertyName);

        void Notify(string propertyName);

        ref long _propertyChangedCount { get; }

        ref long _panPropertyChangedCount { get; }

        ref long _tiltPropertyChangedCount { get; }

        ref long _zoomPropertyChangedCount { get; }

        ref long _focusPropertyChangedCount { get; }
    }

    /// <summary>
    /// 알림 처리에 필요한 카운터·저장 대상·이벤트 발행 경로만 제공한다.
    /// 카운터는 원래 저장 위치를 공유하여 영상 진단과 속성 알림의 집계가 나뉘지 않게 한다.
    /// </summary>
    internal sealed class PropertyChangeContext : IPropertyChangeContext
    {
        private readonly FeatureComposition _features;

        private readonly Action<string> _publish;

        internal PropertyChangeContext(FeatureComposition features, Action<string> publish)
        {
            _features = features ?? throw new ArgumentNullException(nameof(features));

            _publish = publish ?? throw new ArgumentNullException(nameof(publish));
        }

        public ref long _propertyChangedCount
        {
            get { return ref _features._screenStateFeature._propertyChangedCount; }
        }

        public ref long _panPropertyChangedCount
        {
            get { return ref _features._screenStateFeature._panPropertyChangedCount; }
        }

        public ref long _tiltPropertyChangedCount
        {
            get { return ref _features._screenStateFeature._tiltPropertyChangedCount; }
        }

        public ref long _zoomPropertyChangedCount
        {
            get { return ref _features._screenStateFeature._zoomPropertyChangedCount; }
        }

        public ref long _focusPropertyChangedCount
        {
            get { return ref _features._screenStateFeature._focusPropertyChangedCount; }
        }

        public void SwitchEquipmentPresets()
        {
            _features._presetStorageFeature.SwitchEquipmentPresets();
        }

        public void Publish(string propertyName)
        {
            _publish(propertyName);
        }

        public void Notify(string propertyName)
        {
            _features.Callbacks.Notify(propertyName);
        }
    }

    /// <summary>
    /// 속성이 바뀌었을 때 따라야 하는 후속 처리의 순서를 담당한다.
    /// 원래 알림을 먼저 발행한 뒤 연관 속성을 알리는 기존 순서를 유지한다.
    /// </summary>
    internal sealed class PropertyChangeCoordinator
    {
        private readonly IPropertyChangeContext _context;

        internal PropertyChangeCoordinator(IPropertyChangeContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        /// <summary>저장 대상 전환 후 원래 알림을 발행하고, 관련 속성을 기존 순서로 알린다.</summary>
        internal void Handle(string propertyName)
        {
            if (propertyName == nameof(MainViewModel.ControlAgentIp) || propertyName == nameof(MainViewModel.ControlAgentPortText) || propertyName == nameof(MainViewModel.EoSourceAddress) || propertyName == nameof(MainViewModel.IrSourceAddress))
            {
                _context.SwitchEquipmentPresets();
            }

            Interlocked.Increment(ref _context._propertyChangedCount);

            switch (propertyName)
            {
                case nameof(MainViewModel.CurrentPanText):
                    Interlocked.Increment(ref _context._panPropertyChangedCount);
                    break;
                case nameof(MainViewModel.CurrentTiltText):
                    Interlocked.Increment(ref _context._tiltPropertyChangedCount);
                    break;
                case nameof(MainViewModel.CurrentEoZoomText):
                case nameof(MainViewModel.CurrentIrZoomText):
                    Interlocked.Increment(ref _context._zoomPropertyChangedCount);
                    break;
                case nameof(MainViewModel.CurrentEoFocusText):
                case nameof(MainViewModel.CurrentIrFocusText):
                    Interlocked.Increment(ref _context._focusPropertyChangedCount);
                    break;
            }

            _context.Publish(propertyName);

            if (propertyName == nameof(MainViewModel.SelectedControlAgentProfile) || propertyName == nameof(MainViewModel.ControlAgentIp) || propertyName == nameof(MainViewModel.ControlAgentPortText) || propertyName == nameof(MainViewModel.EoSourceAddress) || propertyName == nameof(MainViewModel.IrSourceAddress))
            {
                _context.Notify(nameof(MainViewModel.EoPowerControlStatusText));

                _context.Notify(nameof(MainViewModel.IrPowerControlStatusText));

                _context.Notify(nameof(MainViewModel.CurrentEoPowerStatusColor));

                _context.Notify(nameof(MainViewModel.CurrentIrPowerStatusColor));
            }

            if (propertyName == nameof(MainViewModel.EoPowerControlStatusText) || propertyName == nameof(MainViewModel.CurrentEoPowerText) || propertyName == nameof(MainViewModel.IsOperationCommandEnabled))
            {
                _context.Notify(nameof(MainViewModel.IsEoPowerControlEnabled));
            }

            if (propertyName == nameof(MainViewModel.IrPowerControlStatusText) || propertyName == nameof(MainViewModel.CurrentIrPowerText) || propertyName == nameof(MainViewModel.IsOperationCommandEnabled))
            {
                _context.Notify(nameof(MainViewModel.IsIrPowerControlEnabled));
            }

            if (propertyName == nameof(MainViewModel.IsEoPowerControlEnabled) || propertyName == nameof(MainViewModel.IsIrPowerControlEnabled))
            {
                if (propertyName == nameof(MainViewModel.IsEoPowerControlEnabled))
                {
                    _context.Notify(nameof(MainViewModel.IsEoPowerOnEnabled));

                    _context.Notify(nameof(MainViewModel.IsEoPowerOffEnabled));
                }
                else
                {
                    _context.Notify(nameof(MainViewModel.IsIrPowerOnEnabled));

                    _context.Notify(nameof(MainViewModel.IsIrPowerOffEnabled));
                }

                System.Windows.Input.CommandManager.InvalidateRequerySuggested();
            }
        }
    }
}
