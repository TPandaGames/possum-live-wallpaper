using System.Text.Json;

namespace PocketPossum;

/// <summary>Settings dialog: scene, time and season, weather sync, and the idle timing that triggers the screensaver.</summary>
sealed class ConfigForm : Form
{
    sealed record Place(string Label, string Name, double Lat, double Lon)
    {
        public override string ToString() => Label;
    }

    static readonly HttpClient Http = CreateHttp();

    static HttpClient CreateHttp()
    {
        var c = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
        c.DefaultRequestHeaders.UserAgent.ParseAdd("PocketPossum/1.0 (+https://github.com/TPandaGames/possum-live-wallpaper)");
        return c;
    }

    public ConfigForm(Settings s)
    {
        Text = "Possum Operational ScreenSaver Settings";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = MinimizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        AutoScaleMode = AutoScaleMode.Dpi;
        ClientSize = new Size(404, 468);
        Font = new Font("Segoe UI", 9f);

        var tabs = new TabControl { Left = 8, Top = 8, Width = 388, Height = 412 };
        TabPage Tab(string title) { var t = new TabPage(title) { UseVisualStyleBackColor = true }; tabs.TabPages.Add(t); return t; }
        var tabScene = Tab("Scene");
        var tabTime = Tab("Time && season");
        var tabWeather = Tab("Weather");
        var tabStartup = Tab("Startup");

        // ---------------- Scene ----------------
        var theme = new ComboBox { Left = 90, Top = 20, Width = 270, DropDownStyle = ComboBoxStyle.DropDownList };
        theme.Items.AddRange(new object[] { "Porch (moonlit night)", "OLED Void (pure black)", "Synthwave" });
        theme.SelectedIndex = s.Theme switch { "void" => 1, "synth" => 2, _ => 0 };
        var clock = new CheckBox { Text = "Show floating clock && date", Left = 16, Top = 66, AutoSize = true, Checked = s.ShowClock };
        var sound = new CheckBox { Text = "Soft squeak sounds (and thunder)", Left = 16, Top = 96, AutoSize = true, Checked = s.Sound };
        var joey = new CheckBox { Text = "Baby joey passenger", Left = 16, Top = 126, AutoSize = true, Checked = s.Joey };
        var sceneNote = new Label
        {
            Text = "After dark, Bandit and the Maple family wander in. They can't be switched off yet.",
            Left = 16, Top = 170, Width = 350, Height = 36, ForeColor = SystemColors.GrayText,
        };
        tabScene.Controls.AddRange(new Control[] { new Label { Text = "Scene", Left = 16, Top = 24, AutoSize = true }, theme, clock, sound, joey, sceneNote });

        // ---------------- Time & season ----------------
        var dn = new GroupBox { Text = "Day / night", Left = 12, Top = 10, Width = 360, Height = 122 };
        var mode = new ComboBox { Left = 12, Top = 24, Width = 334, DropDownStyle = ComboBoxStyle.DropDownList };
        mode.Items.AddRange(new object[] { "Automatic (by time)", "Always day", "Always night" });
        mode.SelectedIndex = s.DayNight switch { "day" => 1, "night" => 2, _ => 0 };
        var dayHour = new NumericUpDown { Left = 130, Top = 58, Width = 52, Minimum = 0, Maximum = 23, Value = Math.Clamp(s.DayStartHour, 0, 23) };
        var nightHour = new NumericUpDown { Left = 130, Top = 88, Width = 52, Minimum = 0, Maximum = 23, Value = Math.Clamp(s.NightStartHour, 0, 23) };
        void SyncHours() => dayHour.Enabled = nightHour.Enabled = mode.SelectedIndex == 0;
        mode.SelectedIndexChanged += (_, _) => SyncHours();
        SyncHours();
        dn.Controls.AddRange(new Control[]
        {
            mode,
            new Label { Text = "Day starts at", Left = 12, Top = 62, AutoSize = true }, dayHour, new Label { Text = ":00", Left = 186, Top = 62, AutoSize = true },
            new Label { Text = "Night starts at", Left = 12, Top = 92, AutoSize = true }, nightHour, new Label { Text = ":00", Left = 186, Top = 92, AutoSize = true },
        });

        var sg = new GroupBox { Text = "Season", Left = 12, Top = 140, Width = 360, Height = 124 };
        var season = new ComboBox { Left = 12, Top = 24, Width = 334, DropDownStyle = ComboBoxStyle.DropDownList };
        season.Items.AddRange(new object[] { "Automatic (by today's date)", "Spring", "Summer", "Autumn", "Winter" });
        season.SelectedIndex = s.Season switch { "spring" => 1, "summer" => 2, "autumn" => 3, "winter" => 4, _ => 0 };
        var hemi = new ComboBox { Left = 100, Top = 56, Width = 246, DropDownStyle = ComboBoxStyle.DropDownList };
        hemi.Items.AddRange(new object[] { "Automatic (from time zone)", "Northern", "Southern" });
        hemi.SelectedIndex = s.Hemisphere switch { "north" => 1, "south" => 2, _ => 0 };
        void SyncHemi() => hemi.Enabled = season.SelectedIndex == 0;
        season.SelectedIndexChanged += (_, _) => SyncHemi();
        SyncHemi();
        sg.Controls.AddRange(new Control[]
        {
            season, new Label { Text = "Hemisphere", Left = 12, Top = 60, AutoSize = true }, hemi,
            new Label { Text = "Time zone: " + TimeZoneInfo.Local.DisplayName, Left = 12, Top = 90, Width = 334, Height = 28, ForeColor = SystemColors.GrayText },
        });

        var timeNote = new Label
        {
            Text = "With weather sync on, your location's real sunrise and sunset and its hemisphere are used instead.",
            Left = 16, Top = 276, Width = 350, Height = 36, ForeColor = SystemColors.GrayText,
        };
        tabTime.Controls.AddRange(new Control[] { dn, sg, timeNote });

        // ---------------- Weather ----------------
        Place? selected = s.HasWeatherLocation ? new Place(s.WeatherPlace, s.WeatherPlace, s.WeatherLat!.Value, s.WeatherLon!.Value) : null;

        var sync = new CheckBox { Text = "Sync the scene with the real weather", Left = 16, Top = 16, AutoSize = true, Checked = s.WeatherSync };
        var locLbl = new Label { Text = "Location", Left = 16, Top = 54, AutoSize = true };
        var locBox = new TextBox { Left = 16, Top = 74, Width = 262, PlaceholderText = "City or town, e.g. Dublin" };
        var search = new Button { Text = "Search", Left = 286, Top = 72, Width = 76 };
        var results = new ComboBox { Left = 16, Top = 106, Width = 346, DropDownStyle = ComboBoxStyle.DropDownList, Visible = false };
        var status = new Label { Left = 16, Top = 138, Width = 346, Height = 34, ForeColor = SystemColors.GrayText };
        var unitLbl = new Label { Text = "Temperature", Left = 16, Top = 184, AutoSize = true };
        var unit = new ComboBox { Left = 100, Top = 180, Width = 262, DropDownStyle = ComboBoxStyle.DropDownList };
        unit.Items.AddRange(new object[] { "Automatic (by system region)", "Celsius (°C)", "Fahrenheit (°F)" });
        unit.SelectedIndex = s.TempUnit switch { "c" => 1, "f" => 2, _ => 0 };
        var privacy = new Label
        {
            Text = "When on, the screensaver sends your chosen location's coordinates over HTTPS to open-meteo.com about every " +
                   "15 minutes to get the current weather and sunrise/sunset. Nothing else is sent, and it is off by default. " +
                   "Weather data by Open-Meteo.com (CC BY 4.0).",
            Left = 16, Top = 226, Width = 350, Height = 96, ForeColor = SystemColors.GrayText,
        };

        void ShowSelected() =>
            status.Text = selected is null ? "No location chosen yet." : $"Using: {selected.Label}  ({selected.Lat:0.##}, {selected.Lon:0.##})";
        ShowSelected();

        void SyncWeatherEnabled()
        {
            locBox.Enabled = search.Enabled = results.Enabled = unit.Enabled = sync.Checked;
        }
        sync.CheckedChanged += (_, _) => SyncWeatherEnabled();
        SyncWeatherEnabled();

        results.SelectedIndexChanged += (_, _) =>
        {
            if (results.SelectedItem is Place p) { selected = p; ShowSelected(); }
        };

        async Task SearchAsync()
        {
            var q = locBox.Text.Trim();
            if (q.Length < 2) { status.Text = "Type at least two letters."; return; }
            search.Enabled = false;
            status.Text = "Searching...";
            try
            {
                var url = "https://geocoding-api.open-meteo.com/v1/search?name=" + Uri.EscapeDataString(q) + "&count=8&language=en&format=json";
                using var doc = JsonDocument.Parse(await Http.GetStringAsync(url));
                results.Items.Clear();
                if (doc.RootElement.TryGetProperty("results", out var arr))
                {
                    foreach (var r in arr.EnumerateArray())
                    {
                        string name = r.GetProperty("name").GetString() ?? "";
                        string admin = r.TryGetProperty("admin1", out var a) ? a.GetString() ?? "" : "";
                        string country = r.TryGetProperty("country", out var c) ? c.GetString() ?? "" : "";
                        var label = string.Join(", ", new[] { name, admin, country }.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct());
                        results.Items.Add(new Place(label, name, r.GetProperty("latitude").GetDouble(), r.GetProperty("longitude").GetDouble()));
                    }
                }
                results.Visible = results.Items.Count > 0;
                if (results.Items.Count == 0) status.Text = "No matches. Try a nearby larger town.";
                else
                {
                    status.Text = $"{results.Items.Count} found. Pick one from the list.";
                    results.SelectedIndex = 0;
                }
            }
            catch (Exception ex)
            {
                Log.Write("geocoding failed: " + ex.Message);
                status.Text = "Search failed (are you online?): " + ex.Message;
            }
            finally { search.Enabled = sync.Checked; }
        }
        search.Click += async (_, _) => await SearchAsync();
        locBox.KeyDown += async (_, e) =>
        {
            if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; await SearchAsync(); }
        };

        tabWeather.Controls.AddRange(new Control[] { sync, locLbl, locBox, search, results, status, unitLbl, unit, privacy });

        // ---------------- Startup ----------------
        var auto = new CheckBox { Text = "Start the screensaver when idle for", Left = 16, Top = 22, AutoSize = true, Checked = s.AutoStart };
        var wait = new NumericUpDown
        {
            Left = 250, Top = 19, Width = 56, Minimum = 1, Maximum = 999,
            Value = Math.Clamp(s.IdleMinutes, 1, 999), Enabled = s.AutoStart,
        };
        auto.CheckedChanged += (_, _) => wait.Enabled = auto.Checked;
        var startup = new CheckBox { Text = "Run in the system tray when Windows starts", Left = 16, Top = 62, AutoSize = true, Checked = Startup.IsEnabled() };
        var wallpaper = new CheckBox { Text = "Use as live desktop wallpaper (while in the tray)", Left = 16, Top = 92, AutoSize = true, Checked = s.LiveWallpaper };
        var startupNote = new Label
        {
            Top = 132,
            Text = "Idle start works while Pocket Possum is running in the system tray. " +
                   "Move the mouse or press any key to exit the screensaver.",
            Left = 16, Width = 350, Height = 50, ForeColor = SystemColors.GrayText,
        };
        tabStartup.Controls.AddRange(new Control[] { auto, wait, new Label { Text = "minutes", Left = 312, Top = 23, AutoSize = true }, startup, wallpaper, startupNote });

        // ---------------- Buttons ----------------
        var ok = new Button { Text = "OK", Left = 232, Top = 430, Width = 76 };
        var cancel = new Button { Text = "Cancel", Left = 316, Top = 430, Width = 76, DialogResult = DialogResult.Cancel };
        AcceptButton = ok;
        CancelButton = cancel;

        ok.Click += (_, _) =>
        {
            if (sync.Checked && selected is null)
            {
                tabs.SelectedTab = tabWeather;
                MessageBox.Show(this, "Search for your city or town and pick it from the list, or turn weather sync off.",
                    "Choose a location", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            s.Theme = theme.SelectedIndex switch { 1 => "void", 2 => "synth", _ => "porch" };
            s.ShowClock = clock.Checked;
            s.Sound = sound.Checked;
            s.Joey = joey.Checked;
            s.DayNight = mode.SelectedIndex switch { 1 => "day", 2 => "night", _ => "auto" };
            s.DayStartHour = (int)dayHour.Value;
            s.NightStartHour = (int)nightHour.Value;
            s.Season = season.SelectedIndex switch { 1 => "spring", 2 => "summer", 3 => "autumn", 4 => "winter", _ => "auto" };
            s.Hemisphere = hemi.SelectedIndex switch { 1 => "north", 2 => "south", _ => "auto" };
            s.WeatherSync = sync.Checked;
            if (selected is not null)
            {
                s.WeatherLat = selected.Lat;
                s.WeatherLon = selected.Lon;
                s.WeatherPlace = selected.Name;
            }
            s.TempUnit = unit.SelectedIndex switch { 1 => "c", 2 => "f", _ => "auto" };
            s.AutoStart = auto.Checked;
            s.LiveWallpaper = wallpaper.Checked;
            s.IdleMinutes = (int)wait.Value;
            try { s.Save(); Startup.Set(startup.Checked); }
            catch (Exception ex) { MessageBox.Show(this, ex.Message, "Could not save settings", MessageBoxButtons.OK, MessageBoxIcon.Warning); }
            Close();
        };

        Controls.AddRange(new Control[] { tabs, ok, cancel });
    }
}
