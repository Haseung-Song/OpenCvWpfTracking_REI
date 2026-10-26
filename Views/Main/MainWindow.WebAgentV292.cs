using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace OpenCvWpfTracking
{
    public partial class MainWindow
    {
        private Button _precisionJogButton;

        /// <summary>조그는 왼쪽 버튼을 누른 경우에만 시작하고 마우스를 캡처한다.</summary>
        private void PrecisionJog_Down(object sender, MouseButtonEventArgs e)
        {
            var button = sender as Button;
            byte direction;
            if (button == null || e.ChangedButton != MouseButton.Left ||
                !byte.TryParse(Convert.ToString(button.Tag), out direction))
                return;

            if (!vm.StartPrecisionJog(direction))
                return;
            _precisionJogButton = button;
            if (!button.CaptureMouse())
                EndPrecisionJog("마우스 캡처 실패");
            e.Handled = true;
        }

        /// <summary>
        /// 보관된 조그 입력의 왼쪽 버튼 해제를 처리한다.
        /// </summary>
        private void PrecisionJog_Up(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton != MouseButton.Left)
                return;
            EndPrecisionJog("마우스 버튼 해제");
            e.Handled = true;
        }

        /// <summary>
        /// 보관된 조그 버튼을 이탈하면 해당 입력을 종료한다.
        /// </summary>
        private void PrecisionJog_Leave(object sender, MouseEventArgs e)
        {
            if (ReferenceEquals(sender, _precisionJogButton))
                EndPrecisionJog("조그 버튼 이탈");
        }

        /// <summary>
        /// 보관된 조그 입력의 마우스 캡처가 사라지면 종료한다.
        /// </summary>
        private void PrecisionJog_LostCapture(object sender, MouseEventArgs e)
        {
            if (ReferenceEquals(sender, _precisionJogButton))
                EndPrecisionJog("마우스 캡처 유실");
        }

        /// <summary>
        /// 보관된 조그 버튼 상태와 마우스 캡처를 정리한다.
        /// </summary>
        private void EndPrecisionJog(string reason)
        {
            var button = _precisionJogButton;
            _precisionJogButton = null;
            if (button == null)
                return;
            vm?.StopPrecisionMotion(reason);
            if (button.IsMouseCaptured)
                button.ReleaseMouseCapture();
        }

        /// <summary>
        /// 보관된 정밀 패널이 닫힐 때 동작 상태를 정리하는 미연결 호환 처리이다.
        /// </summary>
        private void PrecisionPanel_Collapsed(object sender, RoutedEventArgs e)
        {
            if (!ReferenceEquals(sender, e.OriginalSource))
                return;
            EndPrecisionJog("시험 영역 접힘");
            vm?.StopPrecisionMotion("시험 영역 접힘");
        }
    }
}
