using System.Reflection;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;

namespace PocketPossum;

/// <summary>Configures a WebView2 to serve the embedded site at https://possum.local/ with no UI chrome.</summary>
static class WebHost
{
    const string Origin = "https://possum.local/";
    static readonly Assembly Asm = typeof(WebHost).Assembly;

    public static async Task InitAsync(WebView2 view, SharedEnv.Holder env, Settings settings, Action<string>? onMessage = null)
    {
        await view.EnsureCoreWebView2Async(await env.Get());
        var core = view.CoreWebView2;

        core.Settings.AreDefaultContextMenusEnabled = false;
        core.Settings.AreDevToolsEnabled = false;
        core.Settings.IsStatusBarEnabled = false;
        core.Settings.IsZoomControlEnabled = false;
        core.Settings.AreBrowserAcceleratorKeysEnabled = false;
        core.Settings.IsPinchZoomEnabled = false;
        core.Settings.IsSwipeNavigationEnabled = false;
        core.Settings.IsGeneralAutofillEnabled = false;

        core.AddWebResourceRequestedFilter(Origin + "*", CoreWebView2WebResourceContext.All);
        core.WebResourceRequested += (_, e) =>
        {
            var path = new Uri(e.Request.Uri).AbsolutePath.TrimStart('/');
            if (path == "") path = "index.html";
            using var s = Asm.GetManifestResourceStream("www/" + path);
            if (s == null)
            {
                e.Response = core.Environment.CreateWebResourceResponse(null, 404, "Not Found", "");
                return;
            }
            var ms = new MemoryStream();
            s.CopyTo(ms);
            ms.Position = 0;
            e.Response = core.Environment.CreateWebResourceResponse(ms, 200, "OK",
                $"Content-Type: {Mime(path)}\r\nCache-Control: no-store");
        };

        // Surface page script errors in log.txt (a screensaver has no dev tools)
        await core.AddScriptToExecuteOnDocumentCreatedAsync(
            "addEventListener('error', e => chrome.webview.postMessage('js-error: ' + e.message + ' @line ' + e.lineno));");
        core.WebMessageReceived += (_, e) =>
        {
            var m = e.TryGetWebMessageAsString();
            if (m.StartsWith("js-error") || m.StartsWith("log:")) Log.Write(m);
            else onMessage?.Invoke(m);
        };

        view.DefaultBackgroundColor = Color.Black;
        core.Navigate($"{Origin}index.html?{settings.ToQuery()}");
    }

    static string Mime(string path) => Path.GetExtension(path).ToLowerInvariant() switch
    {
        ".html" => "text/html; charset=utf-8",
        ".css" => "text/css; charset=utf-8",
        ".js" => "text/javascript; charset=utf-8",
        ".woff2" => "font/woff2",
        _ => "application/octet-stream",
    };
}
