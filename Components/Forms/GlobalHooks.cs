using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Windows.Forms;

namespace PassbookKiosk
{
    class GlobalHooks
    {
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int id, LowLevelMouseHook callback, IntPtr hMod, uint dwThreadId);
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int id, LowLevelKeyboardProc callback, IntPtr hMod, uint dwThreadId);
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern bool UnhookWindowsHookEx(IntPtr hook);
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hook, int nCode, IntPtr wp, IntPtr lp);
        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string name);
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern short GetAsyncKeyState(Keys key);

        private static IntPtr ptrMouseHook;
        private static IntPtr ptrKeyBoardHook;

        #region <MouseHook>
        private delegate IntPtr LowLevelMouseHook(int nCode, IntPtr wParam, IntPtr lParam);
        private static LowLevelMouseHook objMouseProcess;
        
        private struct POINT
        {
            public int x;
            public int y;
        }
        private enum MouseMessages
        {
            WM_LBUTTONDOWN = 0x0201,
            WM_LBUTTONUP = 0x0202,
            WM_MOUSEMOVE = 0x0200,
            WM_MOUSEWHEEL = 0x020A,
            WM_RBUTTONDOWN = 0x0204,
            WM_RBUTTONUP = 0x0205,
            WM_MIDDLEBUTTONCLICK = 0x0207
        }
        private struct MSLLHOOKSTRUCT
        {
            public POINT pt;
            public uint mouseData;
            public uint flags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        private IntPtr MouseKeyCode(int nCode, IntPtr wp, IntPtr lp)
        {
            if (nCode >= 0)
            {
                MSLLHOOKSTRUCT objKeyInfo = (MSLLHOOKSTRUCT)Marshal.PtrToStructure(lp, typeof(MSLLHOOKSTRUCT));
                if (MouseMessages.WM_RBUTTONDOWN == (MouseMessages)wp)
                {
                    UnHook();
                }
                return (IntPtr)1;
            }
            return CallNextHookEx(ptrMouseHook, nCode, wp, lp);
        }
        #endregion <MouseHook>

        #region <KeyboardHook>
        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);
        private static LowLevelKeyboardProc objKeyboardProcess;
        
        //Structure for Keyboard Hooking
        //[StructLayout(LayoutKind.Sequential)]
        private struct KBDLLHOOKSTRUCT
        {
            public int vkCode;
            public Keys key;
            public int scanCode;
            public int flags;
            public int time;
            public IntPtr extra;

        }

        private IntPtr captureKey(int nCode, IntPtr wp, IntPtr lp)
        {
            if (nCode >= 0)
            {
                KBDLLHOOKSTRUCT objKeyInfo = (KBDLLHOOKSTRUCT)Marshal.PtrToStructure(lp, typeof(KBDLLHOOKSTRUCT));
                return (IntPtr)1;
            }
            return CallNextHookEx(ptrKeyBoardHook, nCode, wp, lp);
        }
        #endregion <KeyboardHook>

        public void Hook()
        {
            ProcessModule objCurrentModule = Process.GetCurrentProcess().MainModule;
            if(objKeyboardProcess == null)
                objKeyboardProcess = new LowLevelKeyboardProc(captureKey);
            ptrKeyBoardHook = IntPtr.Zero;
            ptrKeyBoardHook = SetWindowsHookEx(13, objKeyboardProcess, GetModuleHandle(objCurrentModule.ModuleName), 0);

            if (objMouseProcess == null)
                objMouseProcess = new LowLevelMouseHook(MouseKeyCode);
            ptrMouseHook = IntPtr.Zero;
            ptrMouseHook = SetWindowsHookEx(14, objMouseProcess, GetModuleHandle(objCurrentModule.ModuleName), 0);
        }

        public void UnHook()
        {
            try
            {
                if (ptrMouseHook != null)
                    UnhookWindowsHookEx(ptrMouseHook);
                if (ptrKeyBoardHook != null)
                    UnhookWindowsHookEx(ptrKeyBoardHook);
            }
            catch { }
            finally
            {
                ptrMouseHook = ptrKeyBoardHook = IntPtr.Zero;
                GC.Collect();
            }
        }
    }
}
