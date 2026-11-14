using System.Globalization;

namespace PocketPossum;

static class Program
{
    [STAThread]
    static void Main(string[] args)
    {
        ApplicationConfiguration.Initialize();
        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
        Application.ThreadException += (_, e) => Log.Write("ThreadException: " + e.Exception);
        AppDomain.CurrentDomain.UnhandledException += (_, e) => Log.Write("Unhandled: " + e.ExceptionObject);
        Log.Write("start " + string.Join(' ', args));

        // Screensaver convention: /s run, /c[:hwnd] configure, /p hwnd preview.
        // Windows may pass "/c:12345", "/c 12345" or "-s". No args behaves like /c.
        string mode = args.Length == 0 ? "/t" : "/c"; // double-click = run in tray
        string? arg2 = null;
        if (args.Length > 0)
        {
            var a = args[0].Trim().ToLowerInvariant();
            if (a.Length >= 2 && (a[0] == '/' || a[0] == '-'))
            {
                mode = "/" + a[1];
                if (a.Length > 3 && a[2] == ':') arg2 = a[3..];
            }
            if (arg2 == null && args.Length > 1) arg2 = args[1];
        }

        var settings = Settings.Load();

        switch (mode)
        {
            case "/s":
                RunScreensaver(settings);
                break;
            case "/t":
                {
                    using var trayMutex = new Mutex(true, "PocketPossumScreensaver.Tray", out bool firstTray);
                    if (!firstTray) break;
                    Application.Run(new TrayApp(settings));
                    break;
                }
            case "/b": // live desktop wallpaper
                RunWallpaper(settings);
                break;
            case "/p":
                if (long.TryParse(arg2, NumberStyles.Integer, CultureInfo.InvariantCulture, out var h))
                    Application.Run(new PreviewForm(new IntPtr(h), settings));
                break;
            case "/w": // dev: windowed, Esc to quit
                Application.Run(new ScreensaverForm(settings, SharedEnv.Create(), windowed: true));
                break;
            case "/x": // dev: windowed, capture a PNG to arg2 then exit
                Application.Run(new ScreensaverForm(settings, SharedEnv.Create(), windowed: true, shotPath: arg2));
                break;
            default:
                Application.Run(new ConfigForm(settings));
                break;
        }
    }

    static void RunWallpaper(Settings settings)
    {
        using var mutex = new Mutex(true, "PocketPossumScreensaver.Wallpaper", out bool first);
        if (!first) return;

        // After a fresh login the shell may not be ready yet: retry for up to ~30 s
        IntPtr desktop = IntPtr.Zero;
        for (int i = 0; i < 60 && desktop == IntPtr.Zero; i++)
        {
            desktop = DesktopHost.Find();
            if (desktop == IntPtr.Zero) Thread.Sleep(500);
        }
        if (desktop == IntPtr.Zero) { Log.Write("wallpaper: desktop window not found"); return; }

        var env = SharedEnv.Create();
        var ctx = new ApplicationContext();
        int open = 0;
        foreach (var screen in Screen.AllScreens)
        {
            var f = new WallpaperForm(settings, env, screen, desktop);
            open++;
            f.FormClosed += (_, _) => { if (--open == 0) ctx.ExitThread(); };
            f.Show();
        }
        Application.Run(ctx);
        DesktopHost.RefreshWallpaper();
    }

    static void RunScreensaver(Settings settings)
    {
        using var mutex = new Mutex(true, "PocketPossumScreensaver.Singleton", out bool first);
        if (!first) return;

        var env = SharedEnv.Create();
        var ctx = new ApplicationContext();
        int open = 0;

        // When any monitor's form decides to exit, close them all.
        ScreensaverForm.ExitRequested += () =>
        {
            foreach (var f in Application.OpenForms.OfType<ScreensaverForm>().ToList()) f.Close();
        };

        foreach (var screen in Screen.AllScreens)
        {
            var f = new ScreensaverForm(settings, env, screen: screen);
            open++;
            f.FormClosed += (_, _) => { if (--open == 0) ctx.ExitThread(); };
            f.Show();
        }
        Application.Run(ctx);
    }
}
