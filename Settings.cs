using System.Globalization;
using System.Text.Json;

namespace PocketPossum;

public sealed class Settings
{
    public string Theme { get; set; } = "porch";   // porch | void | synth
    public string Animal { get; set; } = "possum"; // possum | cat | raccoon | dog | dachshund | lizard
    public bool ShowClock { get; set; } = true;
    public bool Sound { get; set; } = false;
    public bool Joey { get; set; } = false;
    public string DayNight { get; set; } = "auto"; // auto (system time) | day | night
    public int DayStartHour { get; set; } = 7;     // day begins (local time, 0-23)
    public int NightStartHour { get; set; } = 19;  // night begins (local time, 0-23)
    public string Season { get; set; } = "auto";   // auto (local date) | spring | summer | autumn | winter
    public string Hemisphere { get; set; } = "auto"; // auto (from time zone) | north | south
    public bool AutoStart { get; set; } = true;    // tray app launches the saver when idle
    public int IdleMinutes { get; set; } = 10;      // minutes of no keyboard/mouse activity

    public bool LiveWallpaper { get; set; } = false; // tray app also shows the scene as the desktop background

    // Weather sync (opt-in): the page fetches current conditions for this location from open-meteo.com
    public bool WeatherSync { get; set; } = false;
    public double? WeatherLat { get; set; }
    public double? WeatherLon { get; set; }
    public string WeatherPlace { get; set; } = "";  // display name, e.g. "Dublin, Ireland"
    public string TempUnit { get; set; } = "auto";  // auto (system region) | c | f

    public bool HasWeatherLocation => WeatherLat.HasValue && WeatherLon.HasValue;

    static string Dir => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "PocketPossum");
    static string FilePath => Path.Combine(Dir, "settings.json");
    public static string WebViewDataDir => Path.Combine(Dir, "WebView2");

    public static Settings Load()
    {
        try { return JsonSerializer.Deserialize<Settings>(File.ReadAllText(FilePath)) ?? new Settings(); }
        catch { return new Settings(); }
    }

    public void Save()
    {
        Directory.CreateDirectory(Dir);
        File.WriteAllText(FilePath, JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true }));
    }

    public Settings Clone() => (Settings)MemberwiseClone();

    public string ToQuery()
    {
        var q = $"ss=1&theme={Theme}&animal={Animal}&clock={(ShowClock ? 1 : 0)}&sound={(Sound ? 1 : 0)}&joey={(Joey ? 1 : 0)}" +
                $"&dn={DayNight}&ds={DayStartHour}&ne={NightStartHour}&season={Season}&hemi={Hemisphere}";

        if (WeatherSync && HasWeatherLocation)
        {
            q += $"&wx=1&lat={WeatherLat!.Value.ToString("R", CultureInfo.InvariantCulture)}" +
                 $"&lon={WeatherLon!.Value.ToString("R", CultureInfo.InvariantCulture)}" +
                 $"&place={Uri.EscapeDataString(WeatherPlace)}&unit={TempUnit}";
        }

        // Dev hook: extra query parameters from the environment (e.g. "wxtest=95" to fake a thunderstorm)
        var extra = Environment.GetEnvironmentVariable("POSSUM_URL_EXTRA");
        if (!string.IsNullOrWhiteSpace(extra)) q += "&" + extra.TrimStart('&');
        return q;
    }
}
