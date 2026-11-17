using System.Windows;
using System.Windows.Threading;
using WpfMsgBox = System.Windows.MessageBox;

namespace EasyDock;

public partial class App : System.Windows.Application
{
    private TopPillWindow? _pillWindow;
    private DockWindow?    _dockWindow;
    private System.Windows.Forms.NotifyIcon? _tray;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Attach to caller's PowerShell / CMD console to provide clear terminal output
        NativeMethods.AttachConsole(NativeMethods.ATTACH_PARENT_PROCESS);

        Console.WriteLine();
        Console.WriteLine("==================================================================");
        Console.WriteLine("  EasyDock - Mac-Style Dock & Top Status Pill for Windows 11");
        Console.WriteLine("==================================================================");
        Console.WriteLine("  [✓] Top Pill:  Active at top-center of screen (click '<' to toggle)");
        Console.WriteLine("  [✓] Right Dock: Active on right edge (auto-hiding, 70% height)");
        Console.WriteLine("  [i] Hover over the right edge of screen to open the dock");
        Console.WriteLine("  [i] System tray: Right-click icon in taskbar tray -> Exit EasyDock");
        Console.WriteLine("==================================================================");
        Console.WriteLine();

        // Global exception safety
        DispatcherUnhandledException += (_, ex) =>
        {
            Console.WriteLine($"[EasyDock Error] {ex.Exception.Message}");
            ex.Handled = true;
        };

        AppDomain.CurrentDomain.UnhandledException += (_, ex) =>
        {
            Console.WriteLine($"[EasyDock Fatal] {ex.ExceptionObject}");
        };

        try
        {
            NativeMethods.SetTaskbarAutoHide(true);
        }
        catch { }

        // Launch shell windows
        _pillWindow = new TopPillWindow();
        _pillWindow.Show();

        _dockWindow = new DockWindow();
        _dockWindow.Show();

        SetupTrayIcon();
    }

    private void SetupTrayIcon()
    {
        _tray = new System.Windows.Forms.NotifyIcon
        {
            Text    = "EasyDock",
            Visible = true,
            Icon    = System.Drawing.SystemIcons.Application
        };

        var menu = new System.Windows.Forms.ContextMenuStrip();
        var exitItem = new System.Windows.Forms.ToolStripMenuItem("Exit EasyDock");
        exitItem.Click += (_, _) =>
        {
            try
            {
                NativeMethods.SetTaskbarAutoHide(false);
            }
            catch { }

            _tray!.Visible = false;
            Console.WriteLine("[EasyDock] Exiting gracefully...");
            Shutdown();
        };

        menu.Items.Add(exitItem);
        _tray.ContextMenuStrip = menu;
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _tray?.Dispose();
        base.OnExit(e);
    }
}
