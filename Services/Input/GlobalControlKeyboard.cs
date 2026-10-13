using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Input;
using System.Windows.Threading;
using OpenCvWpfTracking.Common;

namespace OpenCvWpfTracking.Services.Input
{
    // 2026-10-08 V30_0: opt-in, non-consuming global input hook.
    // CallNextHookEx always runs: Windows/application shortcuts stay available.
    public sealed class GlobalControlKeyboard : IDisposable
    {
        private delegate IntPtr HookProc(int code, IntPtr message, IntPtr data);
        private readonly HookProc _callback;
        private readonly Dispatcher _dispatcher;
        private readonly HashSet<int> _held = new HashSet<int>();
        private readonly DispatcherTimer _watchdog;
        private IntPtr _hook;
        public event Action<Key, bool> KeyChanged;
        /// <summary>비차단 키보드 훅과 키 놓침 복구용 100ms 감시를 시작한다.</summary>
        public GlobalControlKeyboard(Dispatcher dispatcher)
        {
            _dispatcher = dispatcher; _callback = Hook;
            using (var process = Process.GetCurrentProcess()) using (var module = process.MainModule)
                _hook = SetWindowsHookEx(13, _callback, GetModuleHandle(module.ModuleName), 0);
            if (_hook == IntPtr.Zero) throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
            _watchdog = new DispatcherTimer(DispatcherPriority.Input, dispatcher) { Interval = TimeSpan.FromMilliseconds(100) };
            _watchdog.Tick += (s, e) =>
            {
                // 2026-10-13 V31_1: idle ticks need no snapshot; held-key iteration still uses a copy.
                if (_held.Count == 0) return;
                foreach (int vk in new List<int>(_held))
                    if ((GetAsyncKeyState(vk) & 0x8000) == 0) { _held.Remove(vk); KeyChanged?.Invoke(KeyInterop.KeyFromVirtualKey(vk), false); }
            };
            _watchdog.Start();
        }
        private static bool Supported(Key key) => key == Key.Left || key == Key.Right || key == Key.Up || key == Key.Down ||
            key == Key.W || key == Key.S || key == Key.A || key == Key.D || key == Key.R || key == Key.T || key == Key.Z || key == Key.C;
        /// <summary>훅 처리 실패를 기록하고 다른 프로그램으로 키 이벤트를 항상 전달한다.</summary>
        private IntPtr Hook(int code, IntPtr message, IntPtr data)
        {
            try { HandleHook(code, message, data); }
            catch (Exception ex) { ConsoleLogHelper.Warning("GLOBAL INPUT", "Hook callback failed / " + ex.GetType().Name + " / HRESULT=0x" + ex.HResult.ToString("X8")); }
            return CallNextHookEx(_hook, code, message, data);
        }
        /// <summary>주입 키와 시스템 조합을 제외하고 누름/해제를 추적한다. Z/C 반복은 유지한다.</summary>
        private void HandleHook(int code, IntPtr message, IntPtr data)
        {
            if (code >= 0 && _hook != IntPtr.Zero)
            {
                int flags = Marshal.ReadInt32(data, 8), vk = Marshal.ReadInt32(data);
                var key = KeyInterop.KeyFromVirtualKey(vk);
                int msg = message.ToInt32(); bool down = msg == 0x100 || msg == 0x104, up = msg == 0x101 || msg == 0x105;
                // Ignore injected keys and combinations with system modifiers.
                bool modifier = (GetAsyncKeyState(0x12) & 0x8000) != 0 || (GetAsyncKeyState(0x11) & 0x8000) != 0 ||
                    (GetAsyncKeyState(0x10) & 0x8000) != 0 || (GetAsyncKeyState(0x5B) & 0x8000) != 0 || (GetAsyncKeyState(0x5C) & 0x8000) != 0;
                if ((flags & 0x10) == 0 && modifier && down)
                {
                    foreach (int held in new List<int>(_held)) Dispatch(held, false);
                    _held.Clear();
                }
                if ((flags & 0x10) == 0 && Supported(key))
                {
                    if (up && _held.Remove(vk)) Dispatch(vk, false);
                    else if (down && !modifier)
                    {
                        bool first=_held.Add(vk);
                        if(first || key==Key.Z || key==Key.C)Dispatch(vk,true);
                    }
                }
            }
        }
        /// <summary>종료 중이 아닌 UI Dispatcher에 키 상태 전달을 예약한다.</summary>
        private void Dispatch(int vk, bool down)
        {
            if (!_dispatcher.HasShutdownStarted) _dispatcher.BeginInvoke(DispatcherPriority.Input, new Action(() =>
            { if (_hook != IntPtr.Zero) KeyChanged?.Invoke(KeyInterop.KeyFromVirtualKey(vk), down); }));
        }
        /// <summary>감시와 네이티브 훅을 해제하고 보유 키 및 구독자를 정리한다.</summary>
        public void Dispose()
        {
            _watchdog.Stop();
            if (_hook != IntPtr.Zero) { UnhookWindowsHookEx(_hook); _hook = IntPtr.Zero; }
            _held.Clear(); KeyChanged = null;
            ConsoleLogHelper.State("GLOBAL INPUT", "Hook released");
        }
        [DllImport("user32.dll", SetLastError = true)] private static extern IntPtr SetWindowsHookEx(int id, HookProc callback, IntPtr module, uint thread);
        [DllImport("user32.dll")] private static extern bool UnhookWindowsHookEx(IntPtr hook);
        [DllImport("user32.dll")] private static extern IntPtr CallNextHookEx(IntPtr hook, int code, IntPtr message, IntPtr data);
        [DllImport("user32.dll")] private static extern short GetAsyncKeyState(int key);
        [DllImport("kernel32.dll", CharSet = CharSet.Auto)] private static extern IntPtr GetModuleHandle(string name);
    }
}
