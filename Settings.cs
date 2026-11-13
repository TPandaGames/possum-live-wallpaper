using System.Text.Json;

namespace PocketPossum;

public sealed class Settings
{
    public string Theme { get; set; } = "porch";   // porch | void | synth
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

    public string ToQuery() =>
        $"ss=1&theme={Theme}&clock={(ShowClock ? 1 : 0)}&sound={(Sound ? 1 : 0)}&joey={(Joey ? 1 : 0)}&dn={DayNight}&ds={DayStartHour}&ne={NightStartHour}&season={Season}&hemi={Hemisphere}";
}
