using System;
using System.Text.RegularExpressions;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;

namespace OpenCvWpfTracking
{
    /// <summary>
    /// [MainWindow / 이동 제어 입력 처리]
    ///
    /// 이동 제어 Tab에서 사용하는 숫자 입력 검증과
    /// LostFocus 범위 보정 기능을 MainWindow.xaml.cs에서 분리한다.
    ///
    /// 대상 입력:
    /// 1. Pan Absolute         : -180.00 ~ 180.00
    /// 2. Tilt Absolute        : -90.00 ~ 90.00
    /// 3. Zoom / Focus Position: 0 ~ 1000
    /// 4. EO Zoom Ratio        : 1.0 ~ 50.0 / IR proportional 1.0 ~ 5.0
    ///
    /// 장비 제어 계산과 Packet 송신은 MainViewModel에서 수행하고,
    /// 이 파일에서는 화면 입력값의 형식과 범위만 관리한다.
    /// </summary>
    public partial class MainWindow
    {
        #region [Move Control Input Constants]

        /// <summary>
        /// Pan Absolute 최소 / 최대 입력값
        /// </summary>
        private const double MoveControlPanMinimumInput =
            -180.0;

        private const double MoveControlPanMaximumInput =
            180.0;

        /// <summary>
        /// Tilt Absolute 최소 / 최대 입력값
        /// </summary>
        private const double MoveControlTiltMinimumInput =
            -90.0;

        private const double MoveControlTiltMaximumInput =
            90.0;

        /// <summary>
        /// Zoom / Focus Position 최소 / 최대 입력값
        /// </summary>
        private const int MoveControlPositionMinimumInput =
            0;

        private const int MoveControlPositionMaximumInput =
            1000;

        /// <summary>
        /// EO 기준 광학 Zoom 배율 입력 범위
        ///
        /// EO XV-Z2050HC:
        /// 1.0 ~ 50.0배
        ///
        /// IR Infra-LWZ-30-150-AF:
        /// 같은 Position 진행률을 사용하여 1.0 ~ 5.0배로 적용한다.
        /// </summary>
        private const double MoveControlZoomRatioMinimumInput =
            1.0;

        private const double MoveControlZoomRatioMaximumInput =
            50.0;

        #endregion

        #region [Move Control Preview Text Input]

        /// <summary>
        /// Pan Absolute 숫자 입력 제한
        ///
        /// 허용 형식:
        /// 0
        /// -45
        /// 120.5
        /// -179.99
        ///
        /// Pan Absolute는 LA / Pelco-D 기준 -180 ~ 180이므로
        /// 첫 글자의 음수 기호를 허용한다.
        /// 실제 범위 제한은 LostFocus에서 최종 적용한다.
        /// </summary>
        private void PanDecimalNumberOnly_PreviewTextInput(
            object sender,
            TextCompositionEventArgs e)
        {
            if (!(sender is TextBox textBox))
            {
                e.Handled =
                    true;

                return;
            }

            string previewText =
                CreatePreviewText(
                    textBox,
                    e.Text);

            e.Handled =
                !Regex.IsMatch(
                    previewText,
                    @"^-?\d{0,3}(\.\d{0,2})?$");
        }

        /// <summary>
        /// Tilt Absolute 숫자 입력 제한
        ///
        /// 허용 형식:
        /// 0
        /// -10
        /// 10.25
        /// -54.81
        ///
        /// 부호는 첫 글자에 한 번만 허용하고,
        /// 소수점은 둘째 자리까지만 입력할 수 있다.
        /// </summary>
        private void DecimalNumberOnly_PreviewTextInput(
            object sender,
            TextCompositionEventArgs e)
        {
            if (!(sender is TextBox textBox))
            {
                e.Handled =
                    true;

                return;
            }

            string previewText =
                CreatePreviewText(
                    textBox,
                    e.Text);

            e.Handled =
                !Regex.IsMatch(
                    previewText,
                    @"^-?\d{0,3}(\.\d{0,2})?$");
        }

        /// <summary>
        /// Zoom / Focus Position 정수 입력 제한
        ///
        /// 표준 Position은 0 ~ 1000 정수만 사용한다.
        /// 범위를 넘는 붙여넣기 또는 직접 입력은
        /// LostFocus에서 0 ~ 1000으로 최종 보정한다.
        /// </summary>
        private void IntegerNumberOnly_PreviewTextInput(
            object sender,
            TextCompositionEventArgs e)
        {
            if (!(sender is TextBox textBox))
            {
                e.Handled =
                    true;

                return;
            }

            string previewText =
                CreatePreviewText(
                    textBox,
                    e.Text);

            e.Handled =
                !Regex.IsMatch(
                    previewText,
                    @"^\d{0,4}$");
        }

        /// <summary>
        /// Zoom Ratio 입력 제한
        ///
        /// EO 광학배율 1.0 ~ 50.0 기준이며,
        /// UI에서는 소수점 첫째 자리까지 입력할 수 있다.
        ///
        /// IR은 입력한 EO 배율과 같은 숫자를 사용하지 않고,
        /// EO에서 계산된 0 ~ 1000 진행률에 따라 1.0 ~ 5.0배로 움직인다.
        ///
        /// 예:
        /// 1
        /// 1.0
        /// 25.5
        /// 50.0
        /// </summary>
        private void ZoomRatio_PreviewTextInput(
            object sender,
            TextCompositionEventArgs e)
        {
            if (!(sender is TextBox textBox))
            {
                e.Handled =
                    true;

                return;
            }

            string previewText =
                CreatePreviewText(
                    textBox,
                    e.Text);

            e.Handled =
                !Regex.IsMatch(
                    previewText,
                    @"^\d{0,2}(\.\d{0,1})?$");
        }

        #endregion

        #region [Move Control Lost Focus]

        /// <summary>
        /// Pan Absolute 입력 완료 처리
        ///
        /// 범위:
        /// -180.00 ~ 180.00
        /// </summary>
        private void PanAngle_LostFocus(
            object sender,
            System.Windows.RoutedEventArgs e)
        {
            ClampDecimalTextBoxValue(
                sender,
                MoveControlPanMinimumInput,
                MoveControlPanMaximumInput,
                2);
        }

        /// <summary>
        /// Tilt Absolute 입력 완료 처리
        ///
        /// 범위:
        /// -90.00 ~ 90.00
        ///
        /// LA Middleware에서 수신하는 실제 Tilt 기준값을 그대로 사용하고,
        /// 이 입력 단계에서는 별도 Zero Offset을 적용하지 않는다.
        /// </summary>
        private void TiltAngle_LostFocus(
            object sender,
            System.Windows.RoutedEventArgs e)
        {
            ClampDecimalTextBoxValue(
                sender,
                MoveControlTiltMinimumInput,
                MoveControlTiltMaximumInput,
                2);
        }


        /// <summary>
        /// Zoom / Focus Position 입력 완료 처리
        ///
        /// 범위:
        /// 0 ~ 1000
        /// </summary>
        private void PositionValue_LostFocus(
            object sender,
            System.Windows.RoutedEventArgs e)
        {
            ClampIntegerTextBoxValue(
                sender,
                MoveControlPositionMinimumInput,
                MoveControlPositionMaximumInput);
        }

        /// <summary>
        /// Zoom Ratio 입력 완료 처리
        ///
        /// EO XV-Z2050HC와 IR 임시 운용 기준을 동일하게 적용하여
        /// 1.0 ~ 50.0배로 제한한다.
        /// </summary>
        private void ZoomRatio_LostFocus(
            object sender,
            System.Windows.RoutedEventArgs e)
        {
            ClampDecimalTextBoxValue(
                sender,
                MoveControlZoomRatioMinimumInput,
                MoveControlZoomRatioMaximumInput,
                1);
        }

        #endregion

        #region [Move Control Input Helpers]

        /// <summary>
        /// 현재 TextBox에서 선택된 문자열을 새 입력 문자열로 교체한 뒤,
        /// 실제로 적용될 전체 문자열을 생성한다.
        ///
        /// PreviewTextInput은 TextBox.Text에 새 문자가 반영되기 전에 호출되므로
        /// 선택 영역 / Caret 위치를 직접 계산해야 정확한 형식 검사가 가능하다.
        /// </summary>
        private static string CreatePreviewText(
            TextBox textBox,
            string inputText)
        {
            string currentText =
                textBox.Text ??
                string.Empty;

            int selectionStart =
                textBox.SelectionStart;

            int selectionLength =
                textBox.SelectionLength;

            string removedSelectionText =
                currentText.Remove(
                    selectionStart,
                    selectionLength);

            return removedSelectionText.Insert(
                selectionStart,
                inputText ??
                string.Empty);
        }

        /// <summary>
        /// TextBox 소수 입력값을 지정 범위로 보정한다.
        ///
        /// 입력값이 비어 있거나 숫자로 변환되지 않으면 최소값을 적용한다.
        /// 보정된 값은 Binding Source에 즉시 반영한다.
        /// </summary>
        private static void ClampDecimalTextBoxValue(
            object sender,
            double minimum,
            double maximum,
            int decimalDigits)
        {
            if (!(sender is TextBox textBox))
            {
                return;
            }

            double parsedValue;

            if (!double.TryParse(
                    textBox.Text,
                    out parsedValue))
            {
                parsedValue =
                    minimum;
            }

            double clampedValue =
                Math.Max(
                    minimum,
                    Math.Min(
                        maximum,
                        parsedValue));

            clampedValue =
                Math.Round(
                    clampedValue,
                    decimalDigits,
                    MidpointRounding.AwayFromZero);

            textBox.Text =
                clampedValue.ToString(
                    "F" +
                    decimalDigits);

            UpdateTextBindingSource(
                textBox);
        }

        /// <summary>
        /// TextBox 정수 입력값을 지정 범위로 보정한다.
        /// </summary>
        private static void ClampIntegerTextBoxValue(
            object sender,
            int minimum,
            int maximum)
        {
            if (!(sender is TextBox textBox))
            {
                return;
            }

            int parsedValue;

            if (!int.TryParse(
                    textBox.Text,
                    out parsedValue))
            {
                parsedValue =
                    minimum;
            }

            int clampedValue =
                Math.Max(
                    minimum,
                    Math.Min(
                        maximum,
                        parsedValue));

            textBox.Text =
                clampedValue.ToString();

            UpdateTextBindingSource(
                textBox);
        }

        /// <summary>
        /// LostFocus에서 보정한 TextBox.Text 값을
        /// MainViewModel Binding Property에 즉시 반영한다.
        /// </summary>
        private static void UpdateTextBindingSource(
            TextBox textBox)
        {
            BindingExpression bindingExpression =
                textBox.GetBindingExpression(
                    TextBox.TextProperty);

            if (bindingExpression == null)
            {
                return;
            }

            bindingExpression.UpdateSource();
        }
        #endregion
    }

}
