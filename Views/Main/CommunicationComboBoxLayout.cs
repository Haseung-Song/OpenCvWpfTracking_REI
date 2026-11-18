using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Threading;

namespace OpenCvWpfTracking
{
    /// <summary>통신 장비 선택 목록의 높이를 완전한 항목 단위로 맞춘다.</summary>
    public static class CommunicationComboBoxLayout
    {
        public static readonly DependencyProperty IsEnabledProperty = DependencyProperty.RegisterAttached("IsEnabled", typeof(bool), typeof(CommunicationComboBoxLayout), new PropertyMetadata(false, OnIsEnabledChanged));

        public static bool GetIsEnabled(DependencyObject element)
        {
            return (bool)element.GetValue(IsEnabledProperty);
        }

        public static void SetIsEnabled(DependencyObject element, bool value)
        {
            element.SetValue(IsEnabledProperty, value);
        }

        private static void OnIsEnabledChanged(DependencyObject element, DependencyPropertyChangedEventArgs e)
        {
            if (!(element is ComboBox combo))
            {
                return;
            }

            combo.DropDownOpened -= OnDropDownOpened;

            if ((bool)e.NewValue)
            {
                combo.DropDownOpened += OnDropDownOpened;
            }
        }

        private static void OnDropDownOpened(object sender, EventArgs e)
        {
            var combo = (ComboBox)sender;

            // 팝업의 실제 화면 제한과 항목 높이가 결정된 뒤 한 번만 정렬한다.
            combo.Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new Action(() => AlignRows(combo)));
        }

        private static void AlignRows(ComboBox combo)
        {
            if (!combo.IsDropDownOpen || combo.Items.Count == 0 || !(combo.Template.FindName("PART_Popup", combo) is Popup popup) || !(popup.Child is FrameworkElement root))
            {
                return;
            }

            var scroll = FindVisualChild<ScrollViewer>(root);

            if (scroll == null)
            {
                return;
            }

            // 이전 열기의 높이는 다시 계산하고 스크롤은 항목 단위로 이동시킨다.
            scroll.ClearValue(FrameworkElement.HeightProperty);

            scroll.CanContentScroll = true;

            root.UseLayoutRounding = true;
            root.SnapsToDevicePixels = true;

            root.UpdateLayout();

            var viewport = scroll.Template.FindName("PART_ScrollContentPresenter", scroll) as FrameworkElement;

            var item = combo.ItemContainerGenerator.ContainerFromIndex(0) as FrameworkElement;

            if (viewport == null || item == null)
            {
                return;
            }

            double rowHeight = item.ActualHeight + item.Margin.Top + item.Margin.Bottom;

            double available = viewport.ActualHeight;

            if (rowHeight <= 0 || available < rowHeight || double.IsInfinity(rowHeight))
            {
                return;
            }

            int rows = Math.Min(combo.Items.Count, (int)Math.Floor((available + 0.01) / rowHeight));

            double chrome = Math.Max(0, scroll.ActualHeight - available);

            // 잘린 마지막 행과 그 아래 빈 공간을 함께 줄이며 바깥 컨트롤 간격은 유지한다.
            scroll.Height = rows * rowHeight + chrome;

            root.UpdateLayout();

            if (combo.SelectedItem != null && combo.ItemContainerGenerator.ContainerFromItem(combo.SelectedItem) is FrameworkElement selected)
            {
                selected.BringIntoView();
            }
        }

        private static T FindVisualChild<T>(DependencyObject parent)
            where T : DependencyObject
        {
            for (int index = 0; index < VisualTreeHelper.GetChildrenCount(parent); index++)
            {
                var child = VisualTreeHelper.GetChild(parent, index);

                if (child is T match)
                {
                    return match;
                }

                var nested = FindVisualChild<T>(child);

                if (nested != null)
                {
                    return nested;
                }
            }

            return null;
        }
    }
}
