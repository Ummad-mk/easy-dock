using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Interop;

namespace EasyDock;

/// <summary>
/// All Win32 / Shell P/Invoke declarations for EasyDock.
/// </summary>
public static class NativeMethods
{
    // ─── Attach to parent console for CLI feedback ───────────────────────────
    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern bool AttachConsole(int dwProcessId);
    public const int ATTACH_PARENT_PROCESS = -1;

    // ─── Window Styles & Constants ───────────────────────────────────────────
    public const int GWL_EXSTYLE_INDEX     = -20;
    public const int WS_EX_TOOLWINDOW_FLAG = 0x00000080;
    public const int WS_EX_APPWINDOW_FLAG  = 0x00040000;
    public const int WS_EX_NOACTIVATE_FLAG = 0x08000000;

    public const uint GW_OWNER = 4;

    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtr", SetLastError = true)]
    public static extern IntPtr GetWindowLongPtr(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtr", SetLastError = true)]
    public static extern IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

    [DllImport("user32.dll")]
    public static extern IntPtr GetWindow(IntPtr hWnd, uint uCmd);

    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    public static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

    // ─── Window Rect & Positioning ───────────────────────────────────────────
    [StructLayout(LayoutKind.Sequential)]
    public struct RECT { public int left, top, right, bottom; }

    [StructLayout(LayoutKind.Sequential)]
    public struct POINT { public int X; public int Y; }

    [DllImport("user32.dll")]
    public static extern bool GetCursorPos(out POINT lpPoint);

    [DllImport("user32.dll")]
    public static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

    public static readonly IntPtr HWND_TOPMOST = new(-1);
    public const uint SWP_NOSIZE     = 0x0001;
    public const uint SWP_NOMOVE     = 0x0002;
    public const uint SWP_NOACTIVATE = 0x0010;
    public const uint SWP_SHOWWINDOW = 0x0040;

    [DllImport("user32.dll", SetLastError = true)]
    public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter,
                                           int X, int Y, int cx, int cy, uint uFlags);

    public static void SetTopmostNoActivate(Window window)
    {
        try
        {
            var hwnd = new WindowInteropHelper(window).Handle;
            if (hwnd != IntPtr.Zero)
            {
                SetWindowPos(hwnd, HWND_TOPMOST, 0, 0, 0, 0,
                             SWP_NOSIZE | SWP_NOMOVE | SWP_NOACTIVATE | SWP_SHOWWINDOW);
            }
        }
        catch { }
    }

    // ─── Running Tasks Enumeration ───────────────────────────────────────────
    public delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

    [DllImport("user32.dll")]
    public static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

    [DllImport("user32.dll")]
    public static extern bool IsWindowVisible(IntPtr hWnd);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    public static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

    [DllImport("user32.dll")]
    public static extern int GetWindowTextLength(IntPtr hWnd);

    [DllImport("user32.dll")]
    public static extern bool IsIconic(IntPtr hWnd);

    [DllImport("user32.dll")]
    public static extern IntPtr GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

    [DllImport("user32.dll")]
    public static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    public static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
    public const int SW_RESTORE  = 9;
    public const int SW_MINIMIZE = 6;

    // ─── Keyboard Events & System Tray Overflow ─────────────────────────────
    [DllImport("user32.dll")]
    public static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);
    public const byte VK_LWIN   = 0x5B;
    public const byte VK_A      = 0x41;
    public const byte VK_N      = 0x4E;
    public const byte VK_B      = 0x42;
    public const byte VK_RETURN = 0x0D;
    public const uint KEYEVENTF_KEYUP = 0x0002;

    public static void ToggleNotificationOverflow()
    {
        try
        {
            // Win + B, then Enter opens the Windows notification overflow flyout
            keybd_event(VK_LWIN, 0, 0, UIntPtr.Zero);
            keybd_event(VK_B, 0, 0, UIntPtr.Zero);
            keybd_event(VK_B, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
            keybd_event(VK_LWIN, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);

            keybd_event(VK_RETURN, 0, 0, UIntPtr.Zero);
            keybd_event(VK_RETURN, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
        }
        catch { }
    }

    public static void ToggleQuickSettings()
    {
        try
        {
            // Win + A opens Windows Quick Settings (sliders)
            keybd_event(VK_LWIN, 0, 0, UIntPtr.Zero);
            keybd_event(VK_A, 0, 0, UIntPtr.Zero);
            keybd_event(VK_A, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
            keybd_event(VK_LWIN, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
        }
        catch { }
    }

    public static void ToggleNotificationCenter()
    {
        try
        {
            // Win + N opens Windows Notification Center
            keybd_event(VK_LWIN, 0, 0, UIntPtr.Zero);
            keybd_event(VK_N, 0, 0, UIntPtr.Zero);
            keybd_event(VK_N, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
            keybd_event(VK_LWIN, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
        }
        catch { }
    }

    // ─── Icon Extraction Messages ────────────────────────────────────────────
    public const uint WM_GETICON = 0x007F;
    public static readonly IntPtr ICON_SMALL  = new(0);
    public static readonly IntPtr ICON_BIG    = new(1);
    public static readonly IntPtr ICON_SMALL2 = new(2);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    public static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll", EntryPoint = "GetClassLongPtr", SetLastError = true)]
    public static extern IntPtr GetClassLongPtr(IntPtr hWnd, int nIndex);
    public const int GCLP_HICON   = -14;
    public const int GCLP_HICONSM = -34;

    /// <summary>
    /// Checks whether an HWND represents a genuine visible top-level application window.
    /// Filters out background tool windows, shell trays, and worker windows.
    /// </summary>
    public static bool IsRealTaskbarWindow(IntPtr hwnd, uint currentPid)
    {
        if (!IsWindowVisible(hwnd)) return false;

        // Skip our own process windows
        GetWindowThreadProcessId(hwnd, out uint pid);
        if (pid == currentPid) return false;

        // Window must have non-empty text
        int len = GetWindowTextLength(hwnd);
        if (len == 0) return false;

        // Style checks: ToolWindows are excluded unless AppWindow is explicitly set
        long exStyle = (long)GetWindowLongPtr(hwnd, GWL_EXSTYLE_INDEX);
        if ((exStyle & WS_EX_TOOLWINDOW_FLAG) != 0 && (exStyle & WS_EX_APPWINDOW_FLAG) == 0)
            return false;

        // Owned windows: if owned and not AppWindow, skip
        IntPtr owner = GetWindow(hwnd, GW_OWNER);
        if (owner != IntPtr.Zero && (exStyle & WS_EX_APPWINDOW_FLAG) == 0)
            return false;

        // Must have non-zero dimensions
        if (GetWindowRect(hwnd, out RECT r))
        {
            if (r.right - r.left <= 20 || r.bottom - r.top <= 20) return false;
        }

        // Exclude system desktop windows
        var sb = new StringBuilder(64);
        GetClassName(hwnd, sb, sb.Capacity);
        string cls = sb.ToString();
        if (cls is "Progman" or "WorkerW" or "Shell_TrayWnd" or "Shell_SecondaryTrayWnd" or "Windows.UI.Core.CoreWindow")
            return false;

        return true;
    }

    // ─── Battery Status ──────────────────────────────────────────────────────
    [StructLayout(LayoutKind.Sequential)]
    public struct SYSTEM_POWER_STATUS
    {
        public byte  ACLineStatus;         // 0=offline, 1=online, 255=unknown
        public byte  BatteryFlag;          // 8=charging, 128=no system battery
        public byte  BatteryLifePercent;   // 0-100 or 255
        public byte  SystemStatusFlag;
        public uint  BatteryLifeTime;
        public uint  BatteryFullLifeTime;
    }

    [DllImport("kernel32.dll")]
    public static extern bool GetSystemPowerStatus(out SYSTEM_POWER_STATUS lpSystemPowerStatus);

    // ─── Windows Taskbar Auto-Hide ───────────────────────────────────────────
    [StructLayout(LayoutKind.Sequential)]
    private struct APPBARDATA
    {
        public uint   cbSize;
        public IntPtr hWnd;
        public uint   uCallbackMessage;
        public uint   uEdge;
        public RECT   rc;
        public int    lParam;
    }

    private const uint ABM_SETSTATE    = 0x0000000A;
    private const int  ABS_AUTOHIDE    = 0x0000001;
    private const int  ABS_ALWAYSONTOP = 0x0000002;

    [DllImport("shell32.dll")]
    private static extern uint SHAppBarMessage(uint dwMessage, ref APPBARDATA pData);

    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    private static extern IntPtr FindWindow(string lpClassName, string? lpWindowName);

    public static void SetTaskbarAutoHide(bool autoHide)
    {
        try
        {
            IntPtr taskbarHwnd = FindWindow("Shell_TrayWnd", null);
            if (taskbarHwnd == IntPtr.Zero) return;

            var data = new APPBARDATA
            {
                cbSize = (uint)Marshal.SizeOf<APPBARDATA>(),
                hWnd   = taskbarHwnd,
                lParam = autoHide ? ABS_AUTOHIDE : ABS_ALWAYSONTOP
            };
            SHAppBarMessage(ABM_SETSTATE, ref data);
        }
        catch { }
    }

    // ─── Wi-Fi Signal Quality ────────────────────────────────────────────────
    [DllImport("wlanapi.dll", SetLastError = true)]
    private static extern uint WlanOpenHandle(uint dwClientVersion, IntPtr pReserved,
                                               out uint pdwNegotiatedVersion, out IntPtr phClientHandle);

    [DllImport("wlanapi.dll")]
    private static extern uint WlanCloseHandle(IntPtr hClientHandle, IntPtr pReserved);

    [DllImport("wlanapi.dll")]
    private static extern uint WlanEnumInterfaces(IntPtr hClientHandle, IntPtr pReserved,
                                                   out IntPtr ppInterfaceList);

    [DllImport("wlanapi.dll")]
    private static extern void WlanFreeMemory(IntPtr pMemory);

    [DllImport("wlanapi.dll")]
    private static extern uint WlanQueryInterface(IntPtr hClientHandle, ref Guid pInterfaceGuid,
        uint OpCode, IntPtr pReserved, out uint pdwDataSize, out IntPtr ppData, IntPtr pWlanOpcodeValueType);

    private const uint WLAN_INTF_OPCODE_CURRENT_CONNECTION = 7;

    public static int GetWifiSignalQuality()
    {
        try
        {
            if (WlanOpenHandle(2, IntPtr.Zero, out _, out IntPtr handle) != 0) return -1;
            if (WlanEnumInterfaces(handle, IntPtr.Zero, out IntPtr ifaceList) != 0)
            {
                WlanCloseHandle(handle, IntPtr.Zero);
                return -1;
            }

            int numIfaces = Marshal.ReadInt32(ifaceList, 8);
            int quality = -1;
            if (numIfaces > 0)
            {
                IntPtr guidPtr = IntPtr.Add(ifaceList, 12);
                Guid guid = Marshal.PtrToStructure<Guid>(guidPtr);

                if (WlanQueryInterface(handle, ref guid, WLAN_INTF_OPCODE_CURRENT_CONNECTION,
                    IntPtr.Zero, out _, out IntPtr data, IntPtr.Zero) == 0)
                {
                    quality = Marshal.ReadInt32(data, 96);
                    if (quality < 0 || quality > 100) quality = -1;
                    WlanFreeMemory(data);
                }
            }
            WlanFreeMemory(ifaceList);
            WlanCloseHandle(handle, IntPtr.Zero);
            return quality;
        }
        catch { return -1; }
    }
}
