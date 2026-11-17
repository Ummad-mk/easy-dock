using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using WpfButton   = System.Windows.Controls.Button;
using WpfCtxMenu  = System.Windows.Controls.ContextMenu;
using WpfMenuItem = System.Windows.Controls.MenuItem;
using MediaColor  = System.Windows.Media.Color;

namespace EasyDock;

public class FirstLetterConverter : IValueConverter
{
    public static readonly FirstLetterConverter Instance = new();
    public object Convert(object v, Type t, object p, CultureInfo c)
        => v is string s && s.Length > 0 ? s[0].ToString().ToUpper() : "?";
    public object ConvertBack(object v, Type t, object p, CultureInfo c)
        => throw new NotImplementedException();
}

public partial class DockWindow : Window
{
    private double _screenW;
    private double _screenH;
    private double _dockH;      // 70% screen height
    private double _dockY;      // 15% from top
    private const double DockWidth = 62; // Reduced to 0.7 of previous width (62px)

    // Auto-hide setting: default to false (Always Visible) so it never disappears on its own
    private bool _isAutoHide    = false;
    private bool _isDockVisible = true;

    private readonly ObservableCollection<DockAppItem> _pinned  = new();
    private readonly ObservableCollection<DockAppItem> _running = new();

    private readonly DispatcherTimer _windowPoller;
    private readonly DispatcherTimer _hideTimer;
    private readonly DispatcherTimer _cursorTracker;
    private DispatcherTimer? _slideTimer;
    private double _slideTarget;

    public DockWindow()
    {
        InitializeComponent();
        Loaded += OnLoaded;

        PinnedList.ItemsSource  = _pinned;
        RunningList.ItemsSource = _running;

        // Poll active running windows every 1.5 seconds
        _windowPoller = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(1500) };
        _windowPoller.Tick += (_, _) => RefreshRunningApps();

        // Delay timer before hiding when Auto-Hide is enabled
        _hideTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(1200) };
        _hideTimer.Tick += (_, _) =>
        {
            _hideTimer.Stop();
            if (_isAutoHide && !IsMouseOver)
            {
                SlideDock(show: false);
            }
        };

        // Physical cursor tracker to ensure edge triggering is 100% reliable even with DPI scaling
        _cursorTracker = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(40) };
        _cursorTracker.Tick += CursorTracker_Tick;

        MouseEnter += (_, _) =>
        {
            _hideTimer.Stop();
            if (!_isDockVisible)
            {
                SlideDock(show: true);
            }
        };

        MouseLeave += (_, _) =>
        {
            if (_isAutoHide)
            {
                _hideTimer.Start();
            }
        };
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        _screenW = SystemParameters.PrimaryScreenWidth;
        _screenH = SystemParameters.PrimaryScreenHeight;

        // 70% of screen height, leaving 15% on top and 15% on bottom
        _dockH = _screenH * 0.70;
        _dockY = _screenH * 0.15;

        Height = _dockH;
        Top    = _dockY;

        NativeMethods.SetTopmostNoActivate(this);

        // Load ONLY the real apps pinned to user's Windows Taskbar
        LoadRealPinnedApps();
        RefreshRunningApps();

        _windowPoller.Start();
        _cursorTracker.Start();

        // Start fully visible and STAY visible!
        Left = _screenW - DockWidth - 6;
        _isDockVisible = true;
        UpdatePinButtonVisual();
    }

    // ─── Edge Detection via Global Cursor Position ───────────────────────────
    private void CursorTracker_Tick(object? sender, EventArgs e)
    {
        if (!_isAutoHide) return;

        if (NativeMethods.GetCursorPos(out var pt))
        {
            // Calculate screen pixel bounds (handles DPI scaling)
            double screenPixelW = System.Windows.Forms.Screen.PrimaryScreen?.Bounds.Width ?? _screenW;
            double dpiFactor = screenPixelW / _screenW;
            double cursorX = pt.X / dpiFactor;
            double cursorY = pt.Y / dpiFactor;

            bool inVerticalDockZone = cursorY >= _dockY && cursorY <= (_dockY + _dockH);

            // If mouse hits the right edge inside the 70% dock zone: expand immediately!
            if (!_isDockVisible && inVerticalDockZone && cursorX >= (_screenW - 14))
            {
                _hideTimer.Stop();
                SlideDock(show: true);
            }
            // If dock is visible and mouse moved far to the left: auto-hide
            else if (_isDockVisible && (cursorX < (_screenW - DockWidth - 40) || !inVerticalDockZone))
            {
                if (!_hideTimer.IsEnabled)
                {
                    _hideTimer.Start();
                }
            }
            else if (_isDockVisible && cursorX >= (_screenW - DockWidth - 10) && inVerticalDockZone)
            {
                _hideTimer.Stop();
            }
        }
    }

    // ─── Smooth Slide Animation ──────────────────────────────────────────────
    public void SlideDock(bool show)
    {
        if (show == _isDockVisible) return;

        double shownLeft  = _screenW - DockWidth - 6;
        double hiddenLeft = _screenW - 16; // 16px frosted handle remains visible on right screen edge

        _slideTarget   = show ? shownLeft : hiddenLeft;
        _isDockVisible = show;

        if (show)
        {
            CollapsedIndicator.Visibility = Visibility.Collapsed;
            DockContentPanel.Opacity      = 1.0;
        }

        _slideTimer?.Stop();
        _slideTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(15) };
        _slideTimer.Tick += (_, _) =>
        {
            double delta = _slideTarget - Left;
            if (Math.Abs(delta) < 2)
            {
                Left = _slideTarget;
                _slideTimer.Stop();

                if (!show)
                {
                    CollapsedIndicator.Visibility = Visibility.Visible;
                    DockContentPanel.Opacity      = 0.0;
                }
                return;
            }
            Left += delta * 0.32;
        };
        _slideTimer.Start();
    }

    // ─── Pin / Auto-Hide Mode Toggle ─────────────────────────────────────────
    private void PinModeBtn_Click(object sender, RoutedEventArgs e)
    {
        _isAutoHide = !_isAutoHide;
        UpdatePinButtonVisual();

        if (!_isAutoHide)
        {
            _hideTimer.Stop();
            SlideDock(show: true);
        }
    }

    private void UpdatePinButtonVisual()
    {
        if (_isAutoHide)
        {
            PinIconPath.Stroke  = new SolidColorBrush(MediaColor.FromRgb(0x8E, 0x8E, 0x93)); // Apple System Gray
            PinModeBtn.ToolTip  = "Auto-Hide is ON (Click to keep Dock always visible)";
        }
        else
        {
            PinIconPath.Stroke  = new SolidColorBrush(MediaColor.FromRgb(0x0A, 0x84, 0xFF)); // Apple System Blue
            PinModeBtn.ToolTip  = "Dock is ALWAYS VISIBLE (Click to enable Auto-Hide)";
        }
    }

    // ─── Load User's Real Pinned Taskbar Apps ─────────────────────────────────
    private void LoadRealPinnedApps()
    {
        _pinned.Clear();
        var realPinned = PinnedAppsManager.LoadRealPinnedApps();
        foreach (var app in realPinned)
        {
            _pinned.Add(app);
        }
    }

    // ─── Running Tasks Detection ─────────────────────────────────────────────
    private void RefreshRunningApps()
    {
        uint currentPid = (uint)Process.GetCurrentProcess().Id;
        var foregroundHwnd = NativeMethods.GetForegroundWindow();
        var foundWindows = new List<(IntPtr hwnd, string title, string? exePath)>();

        NativeMethods.EnumWindows((hwnd, _) =>
        {
            if (!NativeMethods.IsRealTaskbarWindow(hwnd, currentPid))
                return true;

            int len = NativeMethods.GetWindowTextLength(hwnd);
            var sb = new StringBuilder(len + 1);
            NativeMethods.GetWindowText(hwnd, sb, sb.Capacity);
            string title = sb.ToString().Trim();

            NativeMethods.GetWindowThreadProcessId(hwnd, out uint pid);
            string? exe = null;
            try
            {
                exe = Process.GetProcessById((int)pid).MainModule?.FileName;
            }
            catch { }

            foundWindows.Add((hwnd, title, exe));
            return true;
        }, IntPtr.Zero);

        Dispatcher.Invoke(() =>
        {
            var pinnedTargetSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var p in _pinned)
            {
                if (!string.IsNullOrEmpty(p.ExePath))
                {
                    pinnedTargetSet.Add(p.ExePath);
                    pinnedTargetSet.Add(Path.GetFileName(p.ExePath));
                    pinnedTargetSet.Add(Path.GetFileNameWithoutExtension(p.ExePath));
                }
                pinnedTargetSet.Add(p.Name);
            }

            // Update pinned status (is running? is focused?)
            foreach (var pinned in _pinned)
            {
                var match = foundWindows.FirstOrDefault(f =>
                    (f.exePath != null && (
                        string.Equals(f.exePath, pinned.ExePath, StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(Path.GetFileName(f.exePath), Path.GetFileName(pinned.ExePath), StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(Path.GetFileNameWithoutExtension(f.exePath), pinned.Name, StringComparison.OrdinalIgnoreCase)
                    )) ||
                    f.title.Contains(pinned.Name, StringComparison.OrdinalIgnoreCase));

                pinned.IsRunning    = match != default;
                pinned.WindowHandle = match.hwnd;
                pinned.IsFocused    = pinned.IsRunning && match.hwnd == foregroundHwnd;

                if (pinned.Icon == null && match.hwnd != IntPtr.Zero)
                {
                    pinned.Icon = IconHelper.GetWindowIcon(match.hwnd, pinned.ExePath);
                }
            }

            // Group running apps that are NOT in pinned
            var unpinnedRunning = foundWindows
                .Where(f =>
                {
                    if (f.exePath != null)
                    {
                        if (pinnedTargetSet.Contains(f.exePath) ||
                            pinnedTargetSet.Contains(Path.GetFileName(f.exePath)) ||
                            pinnedTargetSet.Contains(Path.GetFileNameWithoutExtension(f.exePath)))
                            return false;
                    }
                    if (pinnedTargetSet.Any(p => f.title.Contains(p, StringComparison.OrdinalIgnoreCase)))
                        return false;

                    return true;
                })
                .GroupBy(f => f.exePath?.ToLowerInvariant() ?? f.title)
                .Select(g =>
                {
                    var first = g.First();
                    var existing = _running.FirstOrDefault(r =>
                        (first.exePath != null && string.Equals(r.ExePath, first.exePath, StringComparison.OrdinalIgnoreCase)) ||
                        r.Name == first.title);

                    if (existing != null)
                    {
                        existing.WindowHandle = first.hwnd;
                        existing.IsFocused    = first.hwnd == foregroundHwnd;
                        return existing;
                    }

                    string displayName = !string.IsNullOrEmpty(first.exePath)
                        ? Path.GetFileNameWithoutExtension(first.exePath)
                        : first.title;

                    return new DockAppItem
                    {
                        Name         = displayName,
                        ExePath      = first.exePath ?? "",
                        WindowHandle = first.hwnd,
                        IsRunning    = true,
                        IsFocused    = first.hwnd == foregroundHwnd,
                        Icon         = IconHelper.GetWindowIcon(first.hwnd, first.exePath)
                    };
                })
                .ToList();

            // Remove closed apps from running list
            for (int i = _running.Count - 1; i >= 0; i--)
            {
                if (!unpinnedRunning.Any(u => u.WindowHandle == _running[i].WindowHandle))
                {
                    _running.RemoveAt(i);
                }
            }

            // Add newly opened unpinned apps
            foreach (var app in unpinnedRunning)
            {
                if (!_running.Any(r => r.WindowHandle == app.WindowHandle))
                {
                    _running.Add(app);
                }
            }

            // Separator between pinned and running
            SeparatorBorder.Visibility = (_pinned.Count > 0 && _running.Count > 0)
                ? Visibility.Visible : Visibility.Collapsed;
        });
    }

    // ─── Click Interactions ──────────────────────────────────────────────────
    private void AppIcon_Click(object sender, RoutedEventArgs e)
    {
        if (sender is WpfButton btn && btn.Tag is DockAppItem item)
        {
            if (item.WindowHandle != IntPtr.Zero && NativeMethods.IsWindowVisible(item.WindowHandle))
            {
                if (item.IsFocused)
                {
                    NativeMethods.ShowWindow(item.WindowHandle, NativeMethods.SW_MINIMIZE);
                }
                else
                {
                    if (NativeMethods.IsIconic(item.WindowHandle))
                    {
                        NativeMethods.ShowWindow(item.WindowHandle, NativeMethods.SW_RESTORE);
                    }
                    NativeMethods.SetForegroundWindow(item.WindowHandle);
                }
            }
            else if (!string.IsNullOrWhiteSpace(item.ExePath))
            {
                try
                {
                    Process.Start(new ProcessStartInfo(item.ExePath) { UseShellExecute = true });
                }
                catch { }
            }
        }
    }

    private void ArrowTrayBtn_Click(object sender, RoutedEventArgs e)
    {
        // Toggle the native Windows Notification Area Overflow flyout (shown in reference image)
        NativeMethods.ToggleNotificationOverflow();
    }

    private void DockBody_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.OriginalSource == DockBody)
        {
            var menu = new WpfCtxMenu();

            var pinItem = new WpfMenuItem
            {
                Header = _isAutoHide ? "📌 Keep Dock Always Visible" : "🚀 Enable Auto-Hide Dock",
                FontWeight = FontWeights.SemiBold
            };
            pinItem.Click += (_, _) =>
            {
                _isAutoHide = !_isAutoHide;
                UpdatePinButtonVisual();
                if (!_isAutoHide) SlideDock(show: true);
            };
            menu.Items.Add(pinItem);

            menu.PlacementTarget = DockBody;
            menu.IsOpen = true;
            e.Handled = true;
        }
    }

    private void AppIcon_RightClick(object sender, MouseButtonEventArgs e)
    {
        if (sender is WpfButton btn && btn.Tag is DockAppItem item)
        {
            var menu = new WpfCtxMenu();

            if (item.IsRunning)
            {
                var closeItem = new WpfMenuItem { Header = "Close Window" };
                closeItem.Click += (_, _) =>
                {
                    if (item.WindowHandle != IntPtr.Zero)
                    {
                        NativeMethods.ShowWindow(item.WindowHandle, NativeMethods.SW_MINIMIZE);
                    }
                };
                menu.Items.Add(closeItem);
            }

            if (item.IsPinned)
            {
                var unpinItem = new WpfMenuItem { Header = "Unpin from Dock" };
                unpinItem.Click += (_, _) =>
                {
                    _pinned.Remove(item);
                    if (item.IsRunning) _running.Add(item);
                };
                menu.Items.Add(unpinItem);
            }
            else
            {
                var pinItem = new WpfMenuItem { Header = "Pin to Dock" };
                pinItem.Click += (_, _) =>
                {
                    item.IsPinned = true;
                    _running.Remove(item);
                    _pinned.Add(item);
                };
                menu.Items.Add(pinItem);
            }

            menu.PlacementTarget = btn;
            menu.IsOpen = true;
            e.Handled = true;
        }
    }
}
