using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace EasyDock;

/// <summary>Represents a single app entry in the dock (pinned or running).</summary>
public class DockAppItem : INotifyPropertyChanged
{
    private bool _isRunning;
    private bool _isFocused;
    private ImageSource? _icon;

    public string   Name          { get; set; } = "";
    public string   ExePath       { get; set; } = "";
    public IntPtr   WindowHandle  { get; set; } = IntPtr.Zero;
    public bool     IsPinned      { get; set; }

    public ImageSource? Icon
    {
        get => _icon;
        set { _icon = value; OnPropertyChanged(); }
    }

    public bool IsRunning
    {
        get => _isRunning;
        set { _isRunning = value; OnPropertyChanged(); }
    }

    public bool IsFocused
    {
        get => _isFocused;
        set { _isFocused = value; OnPropertyChanged(); }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? n = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
}

/// <summary>Helpers for extracting icons from executables and live windows.</summary>
public static class IconHelper
{
    public static ImageSource? GetIcon(string exePath)
    {
        if (string.IsNullOrWhiteSpace(exePath) || !File.Exists(exePath))
            return null;
        try
        {
            using var ico = System.Drawing.Icon.ExtractAssociatedIcon(exePath);
            if (ico is null) return null;
            return Imaging.CreateBitmapSourceFromHIcon(
                ico.Handle,
                Int32Rect.Empty,
                BitmapSizeOptions.FromEmptyOptions());
        }
        catch { return null; }
    }

    public static ImageSource? GetWindowIcon(IntPtr hwnd, string? exePath)
    {
        try
        {
            // 1. Try WM_GETICON
            IntPtr hIcon = NativeMethods.SendMessage(hwnd, NativeMethods.WM_GETICON, NativeMethods.ICON_BIG, IntPtr.Zero);
            if (hIcon == IntPtr.Zero)
                hIcon = NativeMethods.SendMessage(hwnd, NativeMethods.WM_GETICON, NativeMethods.ICON_SMALL2, IntPtr.Zero);
            if (hIcon == IntPtr.Zero)
                hIcon = NativeMethods.SendMessage(hwnd, NativeMethods.WM_GETICON, NativeMethods.ICON_SMALL, IntPtr.Zero);

            if (hIcon != IntPtr.Zero)
            {
                return Imaging.CreateBitmapSourceFromHIcon(hIcon, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
            }

            // 2. Try Class icon
            hIcon = NativeMethods.GetClassLongPtr(hwnd, NativeMethods.GCLP_HICON);
            if (hIcon == IntPtr.Zero)
                hIcon = NativeMethods.GetClassLongPtr(hwnd, NativeMethods.GCLP_HICONSM);

            if (hIcon != IntPtr.Zero)
            {
                return Imaging.CreateBitmapSourceFromHIcon(hIcon, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
            }
        }
        catch { }

        // 3. Fallback to executable icon
        if (!string.IsNullOrEmpty(exePath))
            return GetIcon(exePath);

        return null;
    }
}
