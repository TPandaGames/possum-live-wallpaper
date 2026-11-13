using Microsoft.Web.WebView2.Core;

namespace PocketPossum;

/// <summary>Lazily-created WebView2 environment shared by every monitor's window.</summary>
static class SharedEnv
{
    public sealed class Holder
    {
        Task<CoreWebView2Environment>? _task;
        public Task<CoreWebView2Environment> Get() => _task ??= CoreWebView2Environment.CreateAsync(
            browserExecutableFolder: null,
            userDataFolder: Settings.WebViewDataDir,
            options: new CoreWebView2EnvironmentOptions("--autoplay-policy=no-user-gesture-required"));
    }

    public static Holder Create() => new();
}
