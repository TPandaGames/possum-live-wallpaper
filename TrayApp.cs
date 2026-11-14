using System.Diagnostics;

namespace PocketPossum;

/// <summary>Background tray app: launches the screensaver after N idle minutes (the in-app timing setting).</summary>
sealed class TrayApp : ApplicationContext
{
    readonly Settings _settings;
    readonly NotifyIcon _icon;
    readonly System.Windows.Forms.Timer _timer = new() { Interval = 1000 };
    Process? _saver;
    DateTime _lastLaunch = DateTime.MinValue;
    ConfigForm? _config;

    public TrayApp(Settings settings)
    {
        _settings = settings;

        var menu = new ContextMenuStrip();
        menu.Items.Add("Start screensaver now", null, (_, _) => Launch());
        _wallpaperItem = new ToolStripMenuItem("Live wallpaper") { CheckOnClick = false };
        _wallpaperItem.Click += (_, _) =>
        {
            _settings.LiveWallpaper = !_settings.LiveWallpaper;
            try { _settings.Save(); } catch (Exception ex) { Log.Write("save failed: " + ex.Message); }
            ApplyWallpaper();
        };
        menu.Items.Add(_wallpaperItem);
        menu.Items.Add("Settings…", null, (_, _) => ShowSettings());
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Exit", null, (_, _) => ExitThread());

        _icon = new NotifyIcon
        {
            Icon = SystemIcons.Application,
            Visible = true,
            ContextMenuStrip = menu,
        };
        _icon.DoubleClick += (_, _) => ShowSettings();
        UpdateTooltip();

        _timer.Tick += (_, _) => Tick();
        _timer.Start();
        ApplyWallpaper();
        Log.Write("tray started");
    }

    readonly ToolStripMenuItem _wallpaperItem;
    Process? _wallpaper;

    /// <summary>Starts or stops the wallpaper process to match the setting, and restarts it to pick up new settings.</summary>
    void ApplyWallpaper()
    {
        _wallpaperItem.Checked = _settings.LiveWallpaper;
        try
        {
            if (_wallpaper is { HasExited: false }) { _wallpaper.Kill(); _wallpaper.WaitForExit(3000); }
            _wallpaper = null;
            if (_settings.LiveWallpaper)
            {
                _wallpaper = Process.Start(new ProcessStartInfo(Environment.ProcessPath!, "/b") { UseShellExecute = false });
                Log.Write("tray started live wallpaper");
            }
            else DesktopHost.RefreshWallpaper();
        }
        catch (Exception ex) { Log.Write("wallpaper start failed: " + ex.Message); }
    }

    void UpdateTooltip()
    {
        var t = _settings.AutoStart
            ? $"Pocket Possum – starts after {_settings.IdleMinutes} min idle"
            : "Pocket Possum – auto-start off";
        _icon.Text = t.Length > 63 ? t[..63] : t;
    }

    void Tick()
    {
        if (!_settings.AutoStart) return;
        if (_saver is { HasExited: false }) return;
        if ((DateTime.UtcNow - _lastLaunch).TotalSeconds < 5) return; // avoid relaunch loops

        var limitMs = (uint)Math.Max(1, _settings.IdleMinutes) * 60_000u;
        if (NativeMethods.IdleMilliseconds() >= limitMs) Launch();
    }

    void Launch()
    {
        if (_saver is { HasExited: false }) return;
        _lastLaunch = DateTime.UtcNow;
        try
        {
            _saver = Process.Start(new ProcessStartInfo(Environment.ProcessPath!, "/s") { UseShellExecute = false });
            Log.Write($"tray launched saver (idle limit {_settings.IdleMinutes} min)");
        }
        catch (Exception ex) { Log.Write("launch failed: " + ex); }
    }

    void ShowSettings()
    {
        if (_config is { IsDisposed: false }) { _config.Activate(); return; }
        _config = new ConfigForm(_settings);
        _config.FormClosed += (_, _) => { UpdateTooltip(); ApplyWallpaper(); };
        _config.Show();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _timer.Stop();
            try { if (_wallpaper is { HasExited: false }) _wallpaper.Kill(); } catch { /* already gone */ }
            _icon.Visible = false;
            _icon.Dispose();
        }
        base.Dispose(disposing);
    }
}
