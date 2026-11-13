using Microsoft.Web.WebView2.WinForms;

namespace PocketPossum;

/// <summary>The tiny preview inside the Windows "Screen Saver Settings" dialog (/p hwnd).</summary>
sealed class PreviewForm : Form
{
    readonly IntPtr _parent;
    readonly System.Windows.Forms.Timer _alive = new() { Interval = 500 };
    readonly WebView2 _view = new() { Dock = DockStyle.Fill };

    public PreviewForm(IntPtr parent, Settings settings)
    {
        _parent = parent;
        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        BackColor = Color.Black;
        Controls.Add(_view);

        NativeMethods.GetClientRect(parent, out var r);
        StartPosition = FormStartPosition.Manual;
        Bounds = new Rectangle(0, 0, r.Right - r.Left, r.Bottom - r.Top);

        // Reparent as a child of the preview pane.
        var style = NativeMethods.GetWindowLongPtr(Handle, NativeMethods.GWL_STYLE).ToInt64();
        style = (style & ~NativeMethods.WS_POPUP) | NativeMethods.WS_CHILD;
        NativeMethods.SetWindowLongPtr(Handle, NativeMethods.GWL_STYLE, new IntPtr(style));
        NativeMethods.SetParent(Handle, parent);

        // Preview is a thumbnail: use the real settings (scene, day/night, season, weather) but never play sound.
        var s = settings.Clone();
        s.Sound = false;
        Load += async (_, _) => await WebHost.InitAsync(_view, SharedEnv.Create(), s);

        _alive.Tick += (_, _) => { if (!NativeMethods.IsWindow(_parent)) Close(); };
        _alive.Start();
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        _alive.Stop();
        base.OnFormClosed(e);
        Application.ExitThread();
    }
}
