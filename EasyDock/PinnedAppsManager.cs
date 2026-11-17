using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace EasyDock;

public static class PinnedAppsManager
{
    private static readonly string PinnedFolder = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        @"Microsoft\Internet Explorer\Quick Launch\User Pinned\TaskBar");

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    private struct SHFILEINFO
    {
        public IntPtr hIcon;
        public int iIcon;
        public uint dwAttributes;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
        public string szDisplayName;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
        public string szTypeName;
    }

    private const uint SHGFI_ICON      = 0x000000100;
    private const uint SHGFI_LARGEICON = 0x000000000;

    [DllImport("shell32.dll", CharSet = CharSet.Auto)]
    private static extern IntPtr SHGetFileInfo(string pszPath, uint dwFileAttributes,
        ref SHFILEINFO psfi, uint cbFileInfo, uint uFlags);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool DestroyIcon(IntPtr hIcon);

    public static List<DockAppItem> LoadRealPinnedApps()
    {
        var result = new List<DockAppItem>();

        if (!Directory.Exists(PinnedFolder))
            return result;

        var lnkFiles = Directory.GetFiles(PinnedFolder, "*.lnk");
        foreach (var lnk in lnkFiles)
        {
            string displayName = Path.GetFileNameWithoutExtension(lnk);

            // Ignore tombstones or hidden files
            if (displayName.StartsWith("Tombstone", StringComparison.OrdinalIgnoreCase))
                continue;

            string targetExe = ResolveShortcutTarget(lnk);
            ImageSource? icon = GetHighQualityIcon(lnk, targetExe);

            result.Add(new DockAppItem
            {
                Name     = displayName,
                ExePath  = !string.IsNullOrEmpty(targetExe) ? targetExe : lnk,
                IsPinned = true,
                Icon     = icon
            });
        }

        return result;
    }

    public static string ResolveShortcutTarget(string lnkPath)
    {
        try
        {
            var shellType = Type.GetTypeFromProgID("WScript.Shell");
            if (shellType != null)
            {
                dynamic? shell = Activator.CreateInstance(shellType);
                if (shell != null)
                {
                    dynamic? shortcut = shell.CreateShortcut(lnkPath);
                    string target = (string)(shortcut?.TargetPath ?? "");
                    return target;
                }
            }
        }
        catch { }
        return "";
    }

    public static ImageSource? GetHighQualityIcon(string lnkPath, string? targetExe)
    {
        // 1. Try SHGetFileInfo directly on the .lnk (extracts Windows 11 high-res icon)
        try
        {
            var shinfo = new SHFILEINFO();
            IntPtr res = SHGetFileInfo(lnkPath, 0, ref shinfo, (uint)Marshal.SizeOf(shinfo),
                                       SHGFI_ICON | SHGFI_LARGEICON);
            if (res != IntPtr.Zero && shinfo.hIcon != IntPtr.Zero)
            {
                var src = Imaging.CreateBitmapSourceFromHIcon(
                    shinfo.hIcon,
                    Int32Rect.Empty,
                    BitmapSizeOptions.FromEmptyOptions());
                DestroyIcon(shinfo.hIcon);
                return src;
            }
        }
        catch { }

        // 2. Fallback to Target Exe icon
        if (!string.IsNullOrEmpty(targetExe) && File.Exists(targetExe))
        {
            return IconHelper.GetIcon(targetExe);
        }

        return null;
    }
}
