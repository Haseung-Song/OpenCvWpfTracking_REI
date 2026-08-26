using OpenCvWpfTracking.Common;
using OpenCvWpfTracking.ViewModels.Main;
using System;
using System.Collections;
using System.Collections.Specialized;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Threading;

namespace OpenCvWpfTracking
{
    /// <summary>
    /// AI와 FIRE 이벤트를 현재 표 높이에 맞춘 페이지 단위로 표시하고 선택 삭제를 제공한다.
    /// </summary>
    public partial class EventAlertControl : UserControl
    {
        private EventPageController _aiPager;
        private EventPageController _firePager;
        private MainViewModel _subscribedViewModel;
        private DispatcherOperation _pendingEventTabSelection;
        private int _pendingEventTabIndex = -1;
        private bool _hadActiveAiEvent;
        private bool _hadActiveFireEvent;

        public EventAlertControl()
        {
            InitializeComponent();
            Loaded += EventAlertControl_Loaded;
            Unloaded += EventAlertControl_Unloaded;
            DataContextChanged += EventAlertControl_DataContextChanged;
        }

        /// <summary>
        /// 2026-08-21: 두 이벤트 표의 가변 페이지 View를 최초 한 번만 연결한다.
        /// </summary>
        private void EventAlertControl_Loaded(object sender, RoutedEventArgs e)
        {
            if (_aiPager == null)
            {
                _aiPager = new EventPageController(
                    AiEventGrid, AiTotalText, AiSelectedText,
                    AiPageText, AiPreviousButton, AiNextButton);
                _firePager = new EventPageController(
                    FireEventGrid, FireTotalText, FireSelectedText,
                    FirePageText, FirePreviousButton, FireNextButton);
            }

            SubscribeEventCollections(DataContext as MainViewModel);

            // 2026-08-24: 최초 표시와 상위 이벤트 알림 탭 진입 시에만 최신 이벤트 탭을 선택한다.
            // 행 추가마다 재선택하지 않아 사용자가 이후 AI/FIRE 하위 탭을 자유롭게 전환할 수 있다.
            SelectLatestEventTab();
        }

        /// <summary>
        /// 2026-08-24: Control 재로드 및 DataContext 교체 시 이벤트 컬렉션 구독을 안전하게 갱신한다.
        /// 새 이벤트 Add만 자동 탭 선택 대상으로 사용하며 삭제·초기화 Reset은 제외한다.
        /// </summary>
        private void EventAlertControl_DataContextChanged(
            object sender,
            DependencyPropertyChangedEventArgs e)
        {
            if (IsLoaded)
            {
                SubscribeEventCollections(e.NewValue as MainViewModel);
            }

        }

        private void EventAlertControl_Unloaded(object sender, RoutedEventArgs e)
        {
            SubscribeEventCollections(null);
        }

        private void SubscribeEventCollections(MainViewModel viewModel)
        {
            if (ReferenceEquals(_subscribedViewModel, viewModel))
            {
                return;
            }

            if (_subscribedViewModel != null)
            {
                _subscribedViewModel.PropertyChanged -=
                    SubscribedViewModel_PropertyChanged;
            }

            _subscribedViewModel = viewModel;

            if (_subscribedViewModel != null)
            {
                _hadActiveAiEvent = _subscribedViewModel.ActiveAiCount > 0;
                _hadActiveFireEvent = _subscribedViewModel.ActiveFireCount > 0;
                _subscribedViewModel.PropertyChanged +=
                    SubscribedViewModel_PropertyChanged;
            }
            else
            {
                _hadActiveAiEvent = false;
                _hadActiveFireEvent = false;
            }

        }

        private void SubscribedViewModel_PropertyChanged(
            object sender,
            PropertyChangedEventArgs e)
        {
            MainViewModel viewModel = sender as MainViewModel;
            if (viewModel == null)
            {
                return;
            }

            if (e.PropertyName == nameof(MainViewModel.ActiveAiCount))
            {
                bool hasActiveAiEvent = viewModel.ActiveAiCount > 0;
                if (hasActiveAiEvent && !_hadActiveAiEvent)
                {
                    QueueNewEventTabSelection(0);
                }

                _hadActiveAiEvent = hasActiveAiEvent;
            }
            else if (e.PropertyName == nameof(MainViewModel.ActiveFireCount))
            {
                bool hasActiveFireEvent = viewModel.ActiveFireCount > 0;
                if (hasActiveFireEvent && !_hadActiveFireEvent)
                {
                    QueueNewEventTabSelection(1);
                }

                _hadActiveFireEvent = hasActiveFireEvent;
            }

        }

        /// <summary>
        /// 2026-08-24: 실제 새 이벤트 행이 추가된 순간에만 해당 하위 탭을 한 번 선택한다.
        /// Dispatcher 한 주기 내 연속 추가는 마지막 이벤트 종류로 병합하여 선택 강제를 지속하지 않는다.
        /// </summary>
        private void QueueNewEventTabSelection(int tabIndex)
        {
            _pendingEventTabIndex = tabIndex == 1 ? 1 : 0;
            if (_pendingEventTabSelection != null &&
                _pendingEventTabSelection.Status == DispatcherOperationStatus.Pending)
            {
                return;
            }

            _pendingEventTabSelection = Dispatcher.BeginInvoke(
                DispatcherPriority.DataBind,
                new Action(() =>
                {
                    int resolvedIndex = _pendingEventTabIndex;
                    _pendingEventTabIndex = -1;
                    ApplyEventTabSelection(resolvedIndex, "NEW EVENT");
                }));
        }

        private void ApplyEventTabSelection(int tabIndex, string reason)
        {
            try
            {
                MainViewModel viewModel = DataContext as MainViewModel;
                if (viewModel == null || EventTypeTabControl == null || tabIndex < 0)
                {
                    ConsoleLogHelper.Warning(
                        "EVENT UI",
                        "Event tab auto-selection skipped / ViewModel, TabControl, or index is unavailable");
                    return;
                }

                int resolvedIndex = tabIndex == 1 ? 1 : 0;
                viewModel.SelectedEventAlertTabIndex = resolvedIndex;
                EventTypeTabControl.SetCurrentValue(
                    TabControl.SelectedIndexProperty,
                    resolvedIndex);

                ConsoleLogHelper.Info(
                    "EVENT UI",
                    "Event alert sub-tab selected / REASON=" + reason +
                    " / TYPE=" +
                    (resolvedIndex == 1 ? "FIRE DETECTION" : "AI DETECTION"));
            }
            catch (Exception exception)
            {
                ConsoleLogHelper.Error(
                    "EVENT UI",
                    "Event tab auto-selection failed / " + exception.Message);
            }

        }

        /// <summary>
        /// 2026-08-24: 상위 이벤트 알림 탭 진입 시 가장 최근 이벤트 종류를 즉시 표시한다.
        /// WPF TabControl이 보존한 이전 로컬 선택 상태보다 ViewModel 최신 이벤트 상태를 우선한다.
        /// </summary>
        public void SelectLatestEventTab()
        {
            try
            {
                MainViewModel viewModel =
                    DataContext as MainViewModel;

                if (viewModel == null || EventTypeTabControl == null)
                {
                    ConsoleLogHelper.Warning(
                        "EVENT UI",
                        "Event tab selection skipped / ViewModel or TabControl is unavailable");
                    return;
                }

                // 2026-08-24: 저장된 인덱스만 신뢰하지 않고 실제 목록의 최신 시각을
                // 비교한다. 테스트 프로그램 FIRE 이벤트도 항상 FIRE 탭으로 연결된다.
                DateTime? latestAiTime = viewModel.AiDetectionEvents.Count > 0
                    ? viewModel.AiDetectionEvents.Max(item => item.DetectedTime)
                    : (DateTime?)null;
                DateTime? latestFireTime = viewModel.FireDetectionEvents.Count > 0
                    ? viewModel.FireDetectionEvents.Max(item => item.DetectedTime)
                    : (DateTime?)null;
                int resolvedIndex = latestFireTime.HasValue &&
                    (!latestAiTime.HasValue || latestFireTime.Value >= latestAiTime.Value)
                    ? 1
                    : latestAiTime.HasValue
                        ? 0
                        : viewModel.SelectedEventAlertTabIndex;

                ApplyEventTabSelection(resolvedIndex, "EVENT PANEL OPEN");

                ConsoleLogHelper.Info(
                    "EVENT UI",
                    "Event alert tab opened / TYPE=" +
                    (resolvedIndex == 1 ? "FIRE DETECTION" : "AI DETECTION") +
                    " / AI_COUNT=" + viewModel.AiDetectionEvents.Count +
                    " / FIRE_COUNT=" + viewModel.FireDetectionEvents.Count);
            }
            catch (Exception exception)
            {
                ConsoleLogHelper.Error(
                    "EVENT UI",
                    "Event tab selection failed / " + exception.Message);
            }

        }

        private void AiPreviousButton_Click(object sender, RoutedEventArgs e) => _aiPager?.MovePage(-1);
        private void AiNextButton_Click(object sender, RoutedEventArgs e) => _aiPager?.MovePage(1);
        private void FirePreviousButton_Click(object sender, RoutedEventArgs e) => _firePager?.MovePage(-1);
        private void FireNextButton_Click(object sender, RoutedEventArgs e) => _firePager?.MovePage(1);
        private void AiDeleteSelectedButton_Click(object sender, RoutedEventArgs e) => _aiPager?.DeleteSelected();
        private void FireDeleteSelectedButton_Click(object sender, RoutedEventArgs e) => _firePager?.DeleteSelected();

        /// <summary>
        /// 선택 개수 표시를 현재 두 표에 동기화한다.
        /// </summary>
        private void EventGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _aiPager?.UpdateStatus();
            _firePager?.UpdateStatus();
        }

        /// <summary>
        /// 2026-08-24: 열 머리글 클릭 시 전체 이벤트를 정렬한 뒤 페이지를 다시 구성한다.
        /// 첫 클릭은 오름차순, 같은 열의 다음 클릭은 내림차순으로 전환한다.
        /// </summary>
        private void EventGrid_Sorting(object sender, DataGridSortingEventArgs e)
        {
            try
            {
                DataGrid grid = sender as DataGrid;
                EventPageController pager = ReferenceEquals(grid, AiEventGrid)
                    ? _aiPager
                    : ReferenceEquals(grid, FireEventGrid)
                        ? _firePager
                        : null;

                if (grid == null || pager == null || e.Column == null ||
                    string.IsNullOrWhiteSpace(e.Column.SortMemberPath))
                {
                    e.Handled = true;
                    ConsoleLogHelper.Warning(
                        "EVENT UI",
                        "Event sort skipped / Grid, pager, or sort key is unavailable");
                    return;
                }

                ListSortDirection direction =
                    e.Column.SortDirection == ListSortDirection.Ascending
                        ? ListSortDirection.Descending
                        : ListSortDirection.Ascending;

                foreach (DataGridColumn column in grid.Columns)
                {
                    column.SortDirection = null;
                }

                e.Column.SortDirection = direction;
                e.Handled = true;
                pager.ApplySort(e.Column.SortMemberPath, direction);
            }
            catch (Exception exception)
            {
                e.Handled = true;
                ConsoleLogHelper.Error(
                    "EVENT UI",
                    "Event sort failed / " + exception.Message);
            }

        }

        /// <summary>
        /// 한 이벤트 표의 고정 페이지 이동과 선택 삭제 상태를 관리한다.
        /// </summary>
        private sealed class EventPageController
        {
            private const double DefaultHeaderHeight = 30.0;
            /*
             * 2026-08-26: 파노라마 운용 Overlay 표시 여부로 표 높이가 변해도
             * 전체 페이지 수가 1/15와 1/13 사이에서 바뀌지 않도록 고정한다.
             */
            private const int FixedPageSize = 22;
            private readonly DataGrid _grid;
            private readonly TextBlock _totalText;
            private readonly TextBlock _selectedText;
            private readonly TextBlock _pageText;
            private readonly Button _previousButton;
            private readonly Button _nextButton;
            private readonly IList _source;
            private readonly ICollectionView _view;
            private readonly List<object> _orderedItems = new List<object>();
            private int _pageIndex;
            private int _pageSize = FixedPageSize;
            private double _lastAvailableHeight = -1.0;
            private EventRecordComparer _activeComparer;

            internal EventPageController(
                DataGrid grid,
                TextBlock totalText,
                TextBlock selectedText,
                TextBlock pageText,
                Button previousButton,
                Button nextButton)
            {
                _grid = grid ?? throw new ArgumentNullException(nameof(grid));
                _totalText = totalText ?? throw new ArgumentNullException(nameof(totalText));
                _selectedText = selectedText ?? throw new ArgumentNullException(nameof(selectedText));
                _pageText = pageText ?? throw new ArgumentNullException(nameof(pageText));
                _previousButton = previousButton ?? throw new ArgumentNullException(nameof(previousButton));
                _nextButton = nextButton ?? throw new ArgumentNullException(nameof(nextButton));
                _source = grid.ItemsSource as IList;
                _view = CollectionViewSource.GetDefaultView(grid.ItemsSource);

                if (_view != null)
                {
                    _view.Filter = IsVisibleOnCurrentPage;
                }

                if (_source is INotifyCollectionChanged observableSource)
                {
                    observableSource.CollectionChanged += OnSourceCollectionChanged;
                }

                _grid.SizeChanged += OnGridSizeChanged;
                Refresh();
            }

            /// <summary>
            /// 2026-08-24: 원본 전체 목록에 정렬을 적용한 후 첫 페이지부터 다시 표시한다.
            /// </summary>
            internal void ApplySort(
                string sortMemberPath,
                ListSortDirection direction)
            {
                if (string.IsNullOrWhiteSpace(sortMemberPath))
                {
                    return;
                }

                _activeComparer = new EventRecordComparer(sortMemberPath, direction);
                _pageIndex = 0;
                RebuildOrderedItems();

                if (_view is ListCollectionView listView)
                {
                    listView.CustomSort = _activeComparer;
                }
                else if (_view != null && _view.CanSort)
                {
                    _view.SortDescriptions.Clear();
                    _view.SortDescriptions.Add(
                        new SortDescription(sortMemberPath, direction));
                }

                ConsoleLogHelper.Info(
                    "EVENT UI",
                    "Event list sorted / COLUMN=" + sortMemberPath +
                    " / DIRECTION=" + direction +
                    " / TOTAL=" + SourceCount);
                Refresh();
            }

            internal void MovePage(int offset)
            {
                _pageIndex = Math.Max(0, Math.Min(PageCount - 1, _pageIndex + offset));
                ConsoleLogHelper.Info(
                    "EVENT UI",
                    "Event page moved / PAGE=" + (_pageIndex + 1) + "/" + PageCount);
                Refresh();
            }

            internal void DeleteSelected()
            {
                if (_source == null || _grid.SelectedItems.Count == 0)
                {
                    return;
                }

                int removedCount = 0;
                foreach (object item in _grid.SelectedItems.Cast<object>().ToArray())
                {
                    if (_source.Contains(item))
                    {
                        _source.Remove(item);
                        removedCount++;
                    }

                }

                ConsoleLogHelper.Info(
                    "EVENT UI",
                    "Selected event rows deleted / COUNT=" + removedCount);
                Refresh();
            }

            internal void UpdateStatus()
            {
                _totalText.Text = SourceCount + " EVENTS";
                _selectedText.Text = _grid.SelectedItems.Count + " SELECTED";
                _pageText.Text = (_pageIndex + 1) + " / " + PageCount;
                _previousButton.IsEnabled = _pageIndex > 0;
                _nextButton.IsEnabled = _pageIndex + 1 < PageCount;
            }

            private int SourceCount => _source?.Count ?? 0;

            private int PageCount => Math.Max(
                1,
                (int)Math.Ceiling(SourceCount / (double)Math.Max(1, _pageSize)));

            private bool IsVisibleOnCurrentPage(object item)
            {
                if (_source == null || item == null)
                {
                    return false;
                }

                int index = _orderedItems.IndexOf(item);
                int firstIndex = _pageIndex * _pageSize;
                return index >= firstIndex && index < firstIndex + _pageSize;
            }

            /// <summary>
            /// 2026-08-24: 필터가 현재 페이지만 고르도록 전체 정렬 순서의 스냅샷을 갱신한다.
            /// </summary>
            private void RebuildOrderedItems()
            {
                _orderedItems.Clear();

                if (_source == null)
                {
                    return;
                }

                _orderedItems.AddRange(_source.Cast<object>());
                if (_activeComparer != null)
                {
                    _orderedItems.Sort(_activeComparer);
                }

                // 2026-08-26: 정렬 결과 기준으로 각 페이지에 1~22 행 번호를 다시 부여한다.
                for (int index = 0; index < _orderedItems.Count; index++)
                {
                    if (_orderedItems[index] is FireEventRecord eventRecord)
                    {
                        eventRecord.DisplayIndex = (index % FixedPageSize) + 1;
                    }

                }

            }

            /// <summary>
            /// 2026-08-26: 표의 실제 높이에 맞춰 행 높이만 조정하고 페이지당 22행은 유지한다.
            /// 촬영 Overlay로 우측 패널 높이가 바뀌어도 전체 페이지 수는 변하지 않는다.
            /// </summary>
            private bool UpdatePageSize()
            {
                double headerHeight = IsFinitePositive(_grid.ColumnHeaderHeight)
                    ? _grid.ColumnHeaderHeight
                    : DefaultHeaderHeight;
                double availableHeight = Math.Max(0.0, _grid.ActualHeight - headerHeight);
                if (availableHeight > 0.0)
                {
                    _grid.RowHeight = availableHeight / FixedPageSize;
                }

                bool layoutChanged =
                    Math.Abs(_lastAvailableHeight - availableHeight) >= 1.0;

                _lastAvailableHeight = availableHeight;

                if (_pageSize == FixedPageSize)
                {
                    if (layoutChanged)
                    {
                        ConsoleLogHelper.State(
                            "EVENT UI",
                            "Fixed event page capacity retained / ROWS=" +
                            FixedPageSize +
                            " / HEIGHT=" +
                            _grid.ActualHeight.ToString("F0"));
                    }

                    return false;
                }

                _pageSize = FixedPageSize;
                _pageIndex = Math.Max(0, Math.Min(PageCount - 1, _pageIndex));
                ConsoleLogHelper.State(
                    "EVENT UI",
                    "Fixed event page capacity restored / ROWS=" + _pageSize +
                    " / HEIGHT=" + _grid.ActualHeight.ToString("F0"));
                return true;
            }

            private void OnGridSizeChanged(object sender, SizeChangedEventArgs e)
            {
                if (UpdatePageSize())
                {
                    Refresh();
                }

            }

            private void OnSourceCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
            {
                Refresh();
            }

            private void Refresh()
            {
                UpdatePageSize();
                RebuildOrderedItems();
                _pageIndex = Math.Max(0, Math.Min(PageCount - 1, _pageIndex));
                _view?.Refresh();
                UpdateStatus();
            }

            private static bool IsFinitePositive(double value)
            {
                return !double.IsNaN(value) && !double.IsInfinity(value) && value > 0.0;
            }

            /// <summary>
            /// 이벤트 열별 데이터 형식에 맞춰 날짜, 숫자, 문자열을 비교한다.
            /// </summary>
            private sealed class EventRecordComparer : IComparer, IComparer<object>
            {
                private readonly string _sortMemberPath;
                private readonly int _directionFactor;

                internal EventRecordComparer(
                    string sortMemberPath,
                    ListSortDirection direction)
                {
                    _sortMemberPath = sortMemberPath;
                    _directionFactor = direction == ListSortDirection.Ascending ? 1 : -1;
                }

                public int Compare(object left, object right)
                {
                    FireEventRecord leftEvent = left as FireEventRecord;
                    FireEventRecord rightEvent = right as FireEventRecord;

                    if (ReferenceEquals(leftEvent, rightEvent))
                    {
                        return 0;
                    }

                    if (leftEvent == null)
                    {
                        return -1 * _directionFactor;
                    }

                    if (rightEvent == null)
                    {
                        return 1 * _directionFactor;
                    }

                    int result;
                    switch (_sortMemberPath)
                    {
                        case "DetectedTime":
                            result = leftEvent.DetectedTime.CompareTo(rightEvent.DetectedTime);
                            break;
                        case "Camera":
                            result = CompareText(leftEvent.Camera, rightEvent.Camera);
                            break;
                        case "DetectionType":
                            result = CompareText(leftEvent.DetectionType, rightEvent.DetectionType);
                            break;
                        case "Confidence":
                            result = ParseConfidence(leftEvent.Confidence)
                                .CompareTo(ParseConfidence(rightEvent.Confidence));
                            break;
                        case "PixelArea":
                            result = leftEvent.PixelArea.CompareTo(rightEvent.PixelArea);
                            break;
                        case "Status":
                            result = CompareText(leftEvent.Status, rightEvent.Status);
                            break;
                        default:
                            result = leftEvent.EventId.CompareTo(rightEvent.EventId);
                            break;
                    }

                    if (result == 0)
                    {
                        result = leftEvent.EventId.CompareTo(rightEvent.EventId);
                    }

                    return result * _directionFactor;
                }

                private static int CompareText(string left, string right)
                {
                    return StringComparer.OrdinalIgnoreCase.Compare(
                        left ?? string.Empty,
                        right ?? string.Empty);
                }

                private static double ParseConfidence(string value)
                {
                    string normalized = (value ?? string.Empty)
                        .Trim()
                        .TrimEnd('%');
                    return double.TryParse(
                        normalized,
                        NumberStyles.Float,
                        CultureInfo.InvariantCulture,
                        out double result)
                        ? result
                        : double.MinValue;
                }

            }

        }

    }

}
