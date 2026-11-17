using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace EasyDock;

public partial class TopPillWindow : Window
{
    private readonly DispatcherTimer _clockTimer;
    private readonly DispatcherTimer _batteryTimer;

    public TopPillWindow()
    {
        InitializeComponent();

        Loaded += OnLoaded;
        SizeChanged += OnSizeChanged;

        // Allow dragging the opened pill bar
        OpenedPillBar.MouseLeftButtonDown += (_, e) =>
        {
            if (e.ButtonState == MouseButtonState.Pressed)
            {
                try { DragMove(); } catch { }
            }
        };

        // Live clock: "Tue Nov 17  8:29 PM"
        _clockTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _clockTimer.Tick += (_, _) => UpdateClock();
        _clockTimer.Start();
        UpdateClock();

        // Battery level polling
        _batteryTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(10) };
        _batteryTimer.Tick += (_, _) => UpdateBattery();
        _batteryTimer.Start();
        UpdateBattery();
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        NativeMethods.SetTopmostNoActivate(this);
        RepositionToTop();
    }

    private void OnSizeChanged(object sender, SizeChangedEventArgs e)
    {
        RepositionToTop();
    }

    public void RepositionToTop()
    {
        double screenW = SystemParameters.PrimaryScreenWidth;
        double defaultW = (ClosedPillHandle.Visibility == Visibility.Visible) ? 78 : 380;
        double w = ActualWidth > 0 ? ActualWidth : defaultW;
        Left = (screenW - w) / 2;
        Top  = 0; // Flush against the top screen bezel (like Image 1 & 2)
    }

    private void UpdateClock()
    {
        var now = DateTime.Now;
        // Exact format from Image 2: "Tue Nov 17  8:29 PM"
        DateTimeText.Text = now.ToString("ddd MMM dd  h:mm tt");
    }

    private void UpdateBattery()
    {
        try
        {
            if (NativeMethods.GetSystemPowerStatus(out var pwr))
            {
                if (pwr.BatteryLifePercent <= 100)
                {
                    double pct = pwr.BatteryLifePercent;
                    double fillWidth = Math.Max(2, (pct / 100.0) * 15.0);
                    BatteryFill.Width = fillWidth;

                    bool isCharging = (pwr.BatteryFlag & 0x08) != 0;
                    BatteryBolt.Visibility = isCharging ? Visibility.Visible : Visibility.Collapsed;

                    ToolTip = $"Battery: {pct}%{(isCharging ? " (Charging)" : "")}";
                }
                else
                {
                    BatteryFill.Width = 15;
                    BatteryBolt.Visibility = Visibility.Visible;
                }
            }
        }
        catch { }
    }

    // ─── Open / Collapse Toggle Handlers (Image 1 <-> Image 2) ────────────────
    private void ClosedPillHandle_Click(object sender, MouseButtonEventArgs e)
    {
        // Expand from Image 1 tiny notch into Image 2 full status bar
        ClosedPillHandle.Visibility = Visibility.Collapsed;
        OpenedPillBar.Visibility    = Visibility.Visible;
    }

    private void CollapseBtn_Click(object sender, RoutedEventArgs e)
    {
        // Collapse from Image 2 back into Image 1 tiny notch handle
        OpenedPillBar.Visibility    = Visibility.Collapsed;
        ClosedPillHandle.Visibility = Visibility.Visible;
    }

    // ─── System Control Button Handlers ──────────────────────────────────────
    private void ArrowTrayBtn_Click(object sender, RoutedEventArgs e)
    {
        // The arrow pointing to left from taskbar (opens Windows Hidden Tray Icons Flyout)
        NativeMethods.ToggleNotificationOverflow();
    }

    private void Sliders_Click(object sender, RoutedEventArgs e)
    {
        // Sliders icon: Opens Windows 11 Quick Settings (Win + A)
        NativeMethods.ToggleQuickSettings();
    }

    private void Volume_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            Process.Start(new ProcessStartInfo("sndvol.exe") { UseShellExecute = true });
        }
        catch
        {
            try { Process.Start(new ProcessStartInfo("ms-settings:sound") { UseShellExecute = true }); } catch { }
        }
    }

    private void Battery_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            Process.Start(new ProcessStartInfo("ms-settings:powersleep") { UseShellExecute = true });
        }
        catch { }
    }

    private void Clock_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            Process.Start(new ProcessStartInfo("ms-settings:dateandtime") { UseShellExecute = true });
        }
        catch { }
    }

    private void Notifications_Click(object sender, RoutedEventArgs e)
    {
        // Notification bell: Opens Windows 11 Notification Center (Win + N)
        NativeMethods.ToggleNotificationCenter();
    }

    private void Media_Click(object sender, RoutedEventArgs e)
    {
        // Media music note: opens Spotify or Windows media
        try
        {
            Process.Start(new ProcessStartInfo("spotify:") { UseShellExecute = true });
        }
        catch
        {
            try { Process.Start(new ProcessStartInfo("ms-windows-store://navigatetopage?Id=Spotify") { UseShellExecute = true }); } catch { }
        }
    }
}
