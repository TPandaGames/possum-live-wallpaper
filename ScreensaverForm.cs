using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;

namespace PocketPossum;

/// <summary>One full-monitor, topmost, borderless window hosting the animation.</summary>
sealed class ScreensaverForm : Form
{
    public static event Action? ExitRequested;

    readonly bool _windowed;
    readonly string? _shotPath;
    readonly WebView2 _view = new() { Dock = DockStyle.Fill };
    readonly System.Windows.Forms.Timer _poll = new() { Interval = 30 };
    readonly DateTime _start = DateTime.UtcNow;
    readonly Point _startCursor = Cursor.Position;

    public ScreensaverForm(Settings settings, SharedEnv.Holder env, Screen? screen = null, bool windowed = false, string? shotPath = null)
    {
        _windowed = windowed;
        _shotPath = shotPath;

        BackColor = Color.Black;
        Text = "Pocket Possum";
        ShowInTaskbar = windowed;
        KeyPreview = true;
        Controls.Add(_view);

        if (windowed)
        {
            StartPosition = FormStartPosition.CenterScreen;
            ClientSize = new Size(1280, 720);
        }
        else
        {
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.Manual;
            Bounds = screen!.Bounds;
            TopMost = true;
            Cursor.Hide();
        }

        _poll.Tick += (_, _) => CheckForInput();
        Load += async (_, _) =>
        {
            await WebHost.InitAsync(_view, env, settings, msg => { if (msg == "input") { Log.Write("exit: page input"); RequestExit(); } });
            _view.CoreWebView2.NavigationCompleted += async (_, _) =>
            {
                if (!windowed)
                    await _view.CoreWebView2.ExecuteScriptAsync(InputHooks);
                if (_shotPath != null)
                {
                    // dev: POSSUM_SHOT_DELAY (ms) lets a screenshot wait longer for things to happen
                    await Task.Delay(int.TryParse(Environment.GetEnvironmentVariable("POSSUM_SHOT_DELAY"), out var ms) ? ms : 4000);
                    using var fs = File.Create(_shotPath);
                    await _view.CoreWebView2.CapturePreviewAsync(CoreWebView2CapturePreviewImageFormat.Png, fs);
                    Close();
                }
            };
            if (!windowed) _poll.Start();
        };
    }

    // Forward real input from the page to the host (the WebView swallows it before WinForms sees it).
    const string InputHooks = @"
        (() => {
          const t0 = performance.now(); let moved = 0, lx = null, ly = null;
          const bye = () => window.chrome.webview.postMessage('input');
          addEventListener('keydown', bye, true);
          addEventListener('mousedown', bye, true);
          addEventListener('wheel', bye, true);
          addEventListener('touchstart', bye, true);
          addEventListener('mousemove', e => {
            if (performance.now() - t0 < 1000) return;
            if (lx !== null) moved += Math.abs(e.screenX - lx) + Math.abs(e.screenY - ly);
            lx = e.screenX; ly = e.screenY;
            if (moved > 24) bye();
          }, true);
        })();";

    // Native fallback so input is caught even when the WebView doesn't have focus.
    void CheckForInput()
    {
        if ((DateTime.UtcNow - _start).TotalMilliseconds < 1000) return;

        var p = Cursor.Position;
        if (Math.Abs(p.X - _startCursor.X) + Math.Abs(p.Y - _startCursor.Y) > 24) { Log.Write($"exit: cursor {_startCursor} -> {p}"); RequestExit(); return; }

        // Mouse buttons and keyboard keys (VK 1..254, skipping VK_CANCEL).
        for (int vk = 1; vk < 255; vk++)
        {
            if (vk == 3) continue;
            if ((NativeMethods.GetAsyncKeyState(vk) & 0x8000) != 0) { Log.Write($"exit: vk 0x{vk:X2} down"); RequestExit(); return; }
        }
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (_windowed && e.KeyCode == Keys.Escape) Close();
        base.OnKeyDown(e);
    }

    void RequestExit()
    {
        if (_windowed) return;
        _poll.Stop();
        ExitRequested?.Invoke();
        Close();
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        _poll.Stop();
        if (!_windowed) Cursor.Show();
        base.OnFormClosed(e);
        if (_windowed) Application.ExitThread();
    }

    protected override CreateParams CreateParams
    {
        get
        {
            var cp = base.CreateParams;
            if (!_windowed) cp.ExStyle |= 0x80; // WS_EX_TOOLWINDOW: keep it out of Alt+Tab
            return cp;
        }
    }
}
