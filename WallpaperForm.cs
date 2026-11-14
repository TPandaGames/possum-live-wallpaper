using System.Runtime.InteropServices;
using Microsoft.Web.WebView2.WinForms;

namespace PocketPossum;

/// <summary>Finds the desktop window that sits behind the icons, so a window can be parented to it.</summary>
static class DesktopHost
{
    delegate bool EnumProc(IntPtr hWnd, IntPtr lParam);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)] static extern IntPtr FindWindow(string? cls, string? title);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] static extern IntPtr FindWindowEx(IntPtr parent, IntPtr after, string? cls, string? title);
    [DllImport("user32.dll")] static extern IntPtr SendMessageTimeout(IntPtr h, uint msg, IntPtr w, IntPtr l, uint flags, uint timeout, out IntPtr result);
    [DllImport("user32.dll")] static extern bool EnumWindows(EnumProc proc, IntPtr lParam);
    [DllImport("user32.dll")] static extern bool ScreenToClient(IntPtr hWnd, ref Point pt);
    [DllImport("user32.dll")] static extern bool SetWindowPos(IntPtr h, IntPtr after, int x, int y, int cx, int cy, uint flags);
    [DllImport("user32.dll")] static extern IntPtr SetParent(IntPtr child, IntPtr parent);
    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW")] static extern IntPtr GetWindowLongPtr(IntPtr h, int i);
    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW")] static extern IntPtr SetWindowLongPtr(IntPtr h, int i, IntPtr v);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] static extern bool SystemParametersInfo(uint action, uint p, string? v, uint ini);

    const int GWL_STYLE = -16, GWL_EXSTYLE = -20;
    const long WS_CHILD = 0x40000000L, WS_POPUP = 0x80000000L, WS_VISIBLE = 0x10000000L;
    const long WS_EX_NOACTIVATE = 0x08000000L, WS_EX_TOOLWINDOW = 0x80L;
    const uint SWP_NOACTIVATE = 0x10, SWP_SHOWWINDOW = 0x40, SWP_FRAMECHANGED = 0x20;
    static readonly IntPtr HWND_BOTTOM = new(1);

    /// <summary>The window to parent wallpaper windows to, or Zero if the shell isn't ready.</summary>
    public static IntPtr Find()
    {
        var progman = FindWindow("Progman", null);
        if (progman == IntPtr.Zero) return IntPtr.Zero;

        // Ask the shell to create the WorkerW that sits behind the icons
        SendMessageTimeout(progman, 0x052C, new IntPtr(0xD), new IntPtr(1), 0, 1000, out _);

        // Classic layout (Windows 10 / 11 before 24H2): a top-level WorkerW holds the icons (SHELLDLL_DefView);
        // the WorkerW right after it in z-order is the one behind them.
        IntPtr behind = IntPtr.Zero;
        EnumWindows((top, _) =>
        {
            if (FindWindowEx(top, IntPtr.Zero, "SHELLDLL_DefView", null) != IntPtr.Zero)
                behind = FindWindowEx(IntPtr.Zero, top, "WorkerW", null);
            return true;
        }, IntPtr.Zero);
        if (behind != IntPtr.Zero) return behind;

        // Windows 11 24H2+: the icons and a WorkerW are both children of Progman
        var child = FindWindowEx(progman, IntPtr.Zero, "WorkerW", null);
        return child != IntPtr.Zero ? child : progman;
    }

    /// <summary>Makes <paramref name="form"/> a child of the desktop window, covering <paramref name="screen"/>.</summary>
    public static bool Attach(Form form, IntPtr parent, Rectangle screen)
    {
        var h = form.Handle;
        var style = (GetWindowLongPtr(h, GWL_STYLE).ToInt64() & ~WS_POPUP) | WS_CHILD | WS_VISIBLE;
        SetWindowLongPtr(h, GWL_STYLE, new IntPtr(style));
        var ex = GetWindowLongPtr(h, GWL_EXSTYLE).ToInt64() | WS_EX_NOACTIVATE | WS_EX_TOOLWINDOW;
        SetWindowLongPtr(h, GWL_EXSTYLE, new IntPtr(ex));
        SetParent(h, parent);

        var topLeft = screen.Location;
        ScreenToClient(parent, ref topLeft);
        // Bottom of the child z-order = behind the desktop icons on the 24H2 layout
        return SetWindowPos(h, HWND_BOTTOM, topLeft.X, topLeft.Y, screen.Width, screen.Height,
            SWP_NOACTIVATE | SWP_SHOWWINDOW | SWP_FRAMECHANGED);
    }

    /// <summary>Nudges Windows to repaint the real wallpaper after we exit.</summary>
    public static void RefreshWallpaper() => SystemParametersInfo(0x0014, 0, null, 0); // SPI_SETDESKWALLPAPER (null = reload)
}

/// <summary>The animation as a live desktop wallpaper on one monitor (sits behind the icons, no input).</summary>
sealed class WallpaperForm : Form
{
    readonly WebView2 _view = new() { Dock = DockStyle.Fill };
    readonly Screen _screen;
    readonly IntPtr _desktop;

    public WallpaperForm(Settings settings, SharedEnv.Holder env, Screen screen, IntPtr desktop)
    {
        _screen = screen;
        _desktop = desktop;
        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.Manual;
        BackColor = Color.Black;
        Bounds = screen.Bounds;
        Controls.Add(_view);

        Load += async (_, _) =>
        {
            var ok = DesktopHost.Attach(this, _desktop, _screen.Bounds);
            Log.Write($"wallpaper attach {_screen.DeviceName} {_screen.Bounds}: {ok}");
            // Only the wallpaper site renders on the desktop, so mute it: nobody expects a wallpaper to squeak.
            var s = settings.Clone();
            s.Sound = false;
            await WebHost.InitAsync(_view, env, s);
        };
    }

    protected override bool ShowWithoutActivation => true;

    [DllImport("shell32.dll")] static extern int SHQueryUserNotificationState(out int state);

    // QUNS_BUSY (full-screen app), QUNS_RUNNING_D3D_FULL_SCREEN (game), QUNS_PRESENTATION_MODE
    static bool SomethingFullscreen() =>
        SHQueryUserNotificationState(out var st) == 0 && (st == 2 || st == 3 || st == 4);

    readonly System.Windows.Forms.Timer _watch = new() { Interval = 2000 };
    bool _paused;

    /// <summary>Stops animating (and frees most of the browser's work) while a full-screen app covers the desktop.</summary>
    async void CheckPause()
    {
        var want = SomethingFullscreen();
        if (want == _paused || _view.CoreWebView2 == null) return;
        _paused = want;
        try
        {
            if (want)
            {
                _view.Visible = false;                 // Chromium stops requestAnimationFrame for hidden views
                await _view.CoreWebView2.TrySuspendAsync();
            }
            else
            {
                _view.CoreWebView2.Resume();
                _view.Visible = true;
            }
            Log.Write(want ? "wallpaper paused (full-screen app)" : "wallpaper resumed");
        }
        catch (Exception ex) { Log.Write("pause toggle failed: " + ex.Message); }
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        _watch.Tick += (_, _) => CheckPause();
        _watch.Start();
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        _watch.Stop();
        base.OnFormClosed(e);
    }
}
