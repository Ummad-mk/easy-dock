using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;

namespace EasyDock;

public partial class HoverPreviewWindow : Window
{
    // Physical pixel dimensions (before DPI scaling)
    private const int CardW     = 224;  // matches XAML Width
    private const int CardH     = 168;  // matches XAML Height
    private const int MarginPx  = 8;    // Margin in XAML Grid
    private const int FooterH   = 34;   // RowDefinition Height

    private IntPtr _thumbnail = IntPtr.Zero;

    public HoverPreviewWindow()
    {
        InitializeComponent();
        // Keep topmost without stealing focus
        Loaded += (_, _) => NativeMethods.SetTopmostNoActivate(this);
    }

    // ── Public API ──────────────────────────────────────────────────────────

    /// <summary>
    /// Position and show the preview window for a dock item.
    /// </summary>
    /// <param name="item">The app item being hovered.</param>
    /// <param name="dockLeft">The Left (WPF DIPs) of the DockWindow.</param>
    /// <param name="btnCenterY">Screen Y center of the hovered button in DIPs.</param>
    public void ShowForApp(DockAppItem item, double dockLeft, double btnCenterY)
    {
        // Set footer content
        AppNameText.Text = item.Name;
        AppIcon.Source   = item.Icon;
        AppIcon.Visibility = item.Icon != null ? Visibility.Visible : Visibility.Collapsed;

        // Position to the LEFT of the dock with a small gap
        Left = dockLeft - CardW - 8;
        Top  = btnCenterY - CardH / 2.0;

        // Clamp within screen bounds
        double screenH = SystemParameters.PrimaryScreenHeight;
        double screenW = SystemParameters.PrimaryScreenWidth;
        if (Top < 4)                    Top = 4;
        if (Top + CardH > screenH - 4) Top = screenH - CardH - 4;
        if (Left < 4)                   Left = 4;

        // Show window (ShowActivated=False keeps focus on current app)
        if (!IsVisible) Show();
        NativeMethods.SetTopmostNoActivate(this);

        // Register DWM live thumbnail
        RegisterThumbnail(item.WindowHandle);
    }

    /// <summary>Hide the preview and release the DWM thumbnail.</summary>
    public void HidePreview()
    {
        UnregisterThumbnail();
        Hide();
    }

    // ── DWM Thumbnail ───────────────────────────────────────────────────────

    private void RegisterThumbnail(IntPtr sourceHwnd)
    {
        UnregisterThumbnail();
        NoPreviewText.Visibility = Visibility.Collapsed;

        if (sourceHwnd == IntPtr.Zero || !NativeMethods.IsWindowVisible(sourceHwnd)
                                      || NativeMethods.IsIconic(sourceHwnd))
        {
            // Window is minimized or not visible — show placeholder
            NoPreviewText.Visibility = Visibility.Visible;
            return;
        }

        // Get our own HWND (requires window to be shown first)
        var interop = new WindowInteropHelper(this);
        IntPtr destHwnd = interop.EnsureHandle();
        if (destHwnd == IntPtr.Zero) return;

        int hr = NativeMethods.DwmRegisterThumbnail(destHwnd, sourceHwnd, out IntPtr thumb);
        if (hr != 0) { NoPreviewText.Visibility = Visibility.Visible; return; }

        _thumbnail = thumb;
        UpdateThumbnailRect();
    }

    private void UpdateThumbnailRect()
    {
        if (_thumbnail == IntPtr.Zero) return;

        var dpi    = VisualTreeHelper.GetDpi(this);
        int scaleX = (int)dpi.DpiScaleX;
        int scaleY = (int)dpi.DpiScaleY;
        if (scaleX < 1) scaleX = 1;
        if (scaleY < 1) scaleY = 1;

        // Convert WPF DIPs -> physical pixels for DWM
        int margin = (int)(MarginPx * dpi.DpiScaleX);
        int footer = (int)(FooterH  * dpi.DpiScaleY);
        int totalW = (int)(CardW    * dpi.DpiScaleX);
        int totalH = (int)(CardH    * dpi.DpiScaleY);

        int thumbLeft   = margin;
        int thumbTop    = margin;
        int thumbRight  = totalW - margin;
        int thumbBottom = totalH - margin - footer;

        var props = new NativeMethods.DWM_THUMBNAIL_PROPERTIES
        {
            dwFlags = NativeMethods.DWM_TNP_RECTDESTINATION
                    | NativeMethods.DWM_TNP_VISIBLE
                    | NativeMethods.DWM_TNP_OPACITY
                    | NativeMethods.DWM_TNP_SOURCECLIENTAREAONLY,
            rcDestination = new NativeMethods.RECT
            {
                left   = thumbLeft,
                top    = thumbTop,
                right  = thumbRight,
                bottom = thumbBottom
            },
            opacity              = 255,
            fVisible             = true,
            fSourceClientAreaOnly = false
        };

        NativeMethods.DwmUpdateThumbnailProperties(_thumbnail, ref props);
    }

    public void UnregisterThumbnail()
    {
        if (_thumbnail != IntPtr.Zero)
        {
            NativeMethods.DwmUnregisterThumbnail(_thumbnail);
            _thumbnail = IntPtr.Zero;
        }
    }

    protected override void OnClosed(EventArgs e)
    {
        UnregisterThumbnail();
        base.OnClosed(e);
    }
}
