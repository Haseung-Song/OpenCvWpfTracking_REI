using System.Windows.Input;

namespace OpenCvWpfTracking.Common
{
    /// <summary>2026-11-03: 메인·분리 영상과 제어 모델이 같은 단축키 판정식을 사용한다.</summary>
    internal static class ControlShortcutKeys
    {
        /// <summary>팬·틸트 방향키 여부를 확인한다. 입력 소유권과 실제 명령 처리는 호출부가 유지한다.</summary>
        internal static bool IsPanTiltKey(Key key)
        {
            return key == Key.Left || key == Key.Right || key == Key.Up || key == Key.Down;
        }

        /// <summary>줌·초점 제어에 사용하는 기존 네 키를 확인한다.</summary>
        internal static bool IsLensKey(Key key)
        {
            return key == Key.W || key == Key.S || key == Key.A || key == Key.D;
        }
    }
}
