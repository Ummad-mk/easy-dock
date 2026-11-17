using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace EasyDock;

public partial class TopPillWindow : Window
{
    private readonly DispatcherTimer _clockTimer;
    private readonly DispatcherTimer _batteryTimer;

    // Popup for battery info (anchored near the clicked button)
    private System.Windows.Controls.Primitives.Popup? _batteryPopup;
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
                    // No battery / AC only
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
        if (sender is not System.Windows.Controls.Button btn) return;

        // Toggle: close popup if already open
        if (_batteryPopup?.IsOpen == true)
        {
            _batteryPopup.IsOpen = false;
            return;
        }

        // Read current battery status
        string statusLine;
        try
        {
            if (NativeMethods.GetSystemPowerStatus(out var pwr))
            {
                if (pwr.BatteryLifePercent <= 100)
                {
                    bool charging = (pwr.BatteryFlag & 0x08) != 0;
                    string chargeStr = charging ? " ⚡ Charging" : "";
                    statusLine = $"{pwr.BatteryLifePercent}%{chargeStr}";
                }
                else
                {
                    statusLine = "AC Power (no battery)";
                }
            }
            else
            {
                statusLine = "Unavailable";
            }
        }
        catch
        {
            statusLine = "Unavailable";
        }

        // Build the popup card
        var text = new TextBlock
        {
            Text              = $"🔋  {statusLine}",
            Foreground        = new SolidColorBrush(Colors.White),
            FontFamily        = new System.Windows.Media.FontFamily("Segoe UI Variable Display, Segoe UI"),
            FontSize          = 12,
            FontWeight        = FontWeights.SemiBold,
            VerticalAlignment = VerticalAlignment.Center
        };

        var settingsLink = new System.Windows.Controls.Button
        {
            Content             = "Battery Settings…",
            Foreground          = new SolidColorBrush(System.Windows.Media.Color.FromRgb(0x0A, 0x84, 0xFF)),
            Background          = System.Windows.Media.Brushes.Transparent,
            BorderBrush         = System.Windows.Media.Brushes.Transparent,
            FontFamily          = new System.Windows.Media.FontFamily("Segoe UI Variable Display, Segoe UI"),
            FontSize            = 11,
            Cursor              = System.Windows.Input.Cursors.Hand,
            HorizontalAlignment = System.Windows.HorizontalAlignment.Left,
            Margin              = new Thickness(0, 4, 0, 0)
        };
        settingsLink.Click += (_, _) =>
        {
            _batteryPopup!.IsOpen = false;
            try { System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("ms-settings:powersleep") { UseShellExecute = true }); } catch { }
        };

        var stack = new StackPanel { Margin = new Thickness(0) };
        stack.Children.Add(text);
        stack.Children.Add(settingsLink);

        var card = new System.Windows.Controls.Border
        {
            Background      = new SolidColorBrush(System.Windows.Media.Color.FromArgb(0xF4, 0x0D, 0x11, 0x17)),
            BorderBrush     = new SolidColorBrush(System.Windows.Media.Color.FromArgb(0x50, 0xFF, 0xFF, 0xFF)),
            BorderThickness = new Thickness(1),
            CornerRadius    = new System.Windows.CornerRadius(8),
            Padding         = new Thickness(14, 10, 14, 10),
            Effect          = new System.Windows.Media.Effects.DropShadowEffect
            {
                BlurRadius  = 14,
                ShadowDepth = 0,
                Opacity     = 0.55,
                Color       = Colors.Black
            },
            Child = stack
        };

        // Compute screen position of the battery button: show popup below it
        var dpi       = VisualTreeHelper.GetDpi(this);
        var screenPos = btn.PointToScreen(new System.Windows.Point(0, 0));
        double bx = screenPos.X / (dpi.DpiScaleX > 0 ? dpi.DpiScaleX : 1.0);
        double by = screenPos.Y / (dpi.DpiScaleY > 0 ? dpi.DpiScaleY : 1.0);
        double bh = btn.ActualHeight;
        double bw = btn.ActualWidth;

        if (_batteryPopup == null)
        {
            _batteryPopup = new System.Windows.Controls.Primitives.Popup
            {
                Placement          = System.Windows.Controls.Primitives.PlacementMode.AbsolutePoint,
                AllowsTransparency = true,
                StaysOpen          = false,   // closes on click outside
                IsOpen             = false
            };
        }

        _batteryPopup.Child            = card;
        _batteryPopup.HorizontalOffset = bx + bw / 2 - 90; // roughly center popup under button
        _batteryPopup.VerticalOffset   = by + bh + 6;
        _batteryPopup.IsOpen           = true;
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
