namespace PocketPossum;

/// <summary>Settings dialog: scene options, day/night schedule, and the idle-timing that triggers the screensaver.</summary>
sealed class ConfigForm : Form
{
    public ConfigForm(Settings s)
    {
        Text = "Pocket Possum Settings";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = MinimizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        AutoScaleMode = AutoScaleMode.Dpi;
        ClientSize = new Size(360, 602);
        Font = new Font("Segoe UI", 9f);

        var themeLbl = new Label { Text = "Scene", Left = 16, Top = 20, AutoSize = true };
        var theme = new ComboBox { Left = 100, Top = 16, Width = 240, DropDownStyle = ComboBoxStyle.DropDownList };
        theme.Items.AddRange(new object[] { "Porch (moonlit night)", "OLED Void (pure black)", "Synthwave" });
        theme.SelectedIndex = s.Theme switch { "void" => 1, "synth" => 2, _ => 0 };

        var clock = new CheckBox { Text = "Show floating clock && date", Left = 16, Top = 58, AutoSize = true, Checked = s.ShowClock };
        var sound = new CheckBox { Text = "Soft squeak sounds", Left = 16, Top = 86, AutoSize = true, Checked = s.Sound };
        var joey = new CheckBox { Text = "Baby joey passenger", Left = 16, Top = 114, AutoSize = true, Checked = s.Joey };

        // ---- Day / night ----
        var dn = new GroupBox { Text = "Day / night", Left = 16, Top = 148, Width = 328, Height = 122 };
        var mode = new ComboBox { Left = 12, Top = 24, Width = 304, DropDownStyle = ComboBoxStyle.DropDownList };
        mode.Items.AddRange(new object[] { "Automatic (by system time)", "Always day", "Always night" });
        mode.SelectedIndex = s.DayNight switch { "day" => 1, "night" => 2, _ => 0 };

        var dayLbl = new Label { Text = "Day starts at", Left = 12, Top = 62, AutoSize = true };
        var dayHour = new NumericUpDown { Left = 130, Top = 58, Width = 52, Minimum = 0, Maximum = 23, Value = Math.Clamp(s.DayStartHour, 0, 23) };
        var dayUnit = new Label { Text = ":00", Left = 186, Top = 62, AutoSize = true };
        var nightLbl = new Label { Text = "Night starts at", Left = 12, Top = 92, AutoSize = true };
        var nightHour = new NumericUpDown { Left = 130, Top = 88, Width = 52, Minimum = 0, Maximum = 23, Value = Math.Clamp(s.NightStartHour, 0, 23) };
        var nightUnit = new Label { Text = ":00", Left = 186, Top = 92, AutoSize = true };
        mode.SelectedIndexChanged += (_, _) =>
            dayHour.Enabled = nightHour.Enabled = mode.SelectedIndex == 0;
        dayHour.Enabled = nightHour.Enabled = mode.SelectedIndex == 0;
        dn.Controls.AddRange(new Control[] { mode, dayLbl, dayHour, dayUnit, nightLbl, nightHour, nightUnit });

        // ---- Seasons ----
        var sg = new GroupBox { Text = "Season", Left = 16, Top = 280, Width = 328, Height = 124 };
        var season = new ComboBox { Left = 12, Top = 24, Width = 304, DropDownStyle = ComboBoxStyle.DropDownList };
        season.Items.AddRange(new object[] { "Automatic (by today's date)", "Spring", "Summer", "Autumn", "Winter" });
        season.SelectedIndex = s.Season switch { "spring" => 1, "summer" => 2, "autumn" => 3, "winter" => 4, _ => 0 };
        var hemiLbl = new Label { Text = "Hemisphere", Left = 12, Top = 60, AutoSize = true };
        var hemi = new ComboBox { Left = 100, Top = 56, Width = 216, DropDownStyle = ComboBoxStyle.DropDownList };
        hemi.Items.AddRange(new object[] { "Automatic (from time zone)", "Northern", "Southern" });
        hemi.SelectedIndex = s.Hemisphere switch { "north" => 1, "south" => 2, _ => 0 };
        var tz = new Label
        {
            Text = "Time zone: " + TimeZoneInfo.Local.DisplayName,
            Left = 12, Top = 90, Width = 304, Height = 28, ForeColor = SystemColors.GrayText,
        };
        sg.Controls.AddRange(new Control[] { season, hemiLbl, hemi, tz });
        season.SelectedIndexChanged += (_, _) => hemi.Enabled = season.SelectedIndex == 0;
        hemi.Enabled = season.SelectedIndex == 0;

        // ---- Timing ----
        var group = new GroupBox { Text = "Timing", Left = 16, Top = 414, Width = 328, Height = 104 };
        var auto = new CheckBox { Text = "Start when idle for", Left = 12, Top = 26, AutoSize = true, Checked = s.AutoStart };
        var wait = new NumericUpDown
        {
            Left = 152, Top = 23, Width = 56, Minimum = 1, Maximum = 999,
            Value = Math.Clamp(s.IdleMinutes, 1, 999), Enabled = s.AutoStart,
        };
        var unit = new Label { Text = "minutes", Left = 214, Top = 27, AutoSize = true };
        auto.CheckedChanged += (_, _) => wait.Enabled = auto.Checked;
        var startup = new CheckBox { Text = "Run in the tray when Windows starts", Left = 12, Top = 62, AutoSize = true, Checked = Startup.IsEnabled() };
        group.Controls.AddRange(new Control[] { auto, wait, unit, startup });

        var note = new Label
        {
            Text = "Idle start works while Pocket Possum is running in the system tray. "
                 + "Move the mouse or press any key to exit the screensaver.",
            Left = 16, Top = 526, Width = 328, Height = 34, ForeColor = SystemColors.GrayText,
        };

        var ok = new Button { Text = "OK", Left = 184, Top = 564, Width = 76, DialogResult = DialogResult.OK };
        var cancel = new Button { Text = "Cancel", Left = 268, Top = 564, Width = 76, DialogResult = DialogResult.Cancel };
        AcceptButton = ok;
        CancelButton = cancel;

        ok.Click += (_, _) =>
        {
            s.Theme = theme.SelectedIndex switch { 1 => "void", 2 => "synth", _ => "porch" };
            s.ShowClock = clock.Checked;
            s.Sound = sound.Checked;
            s.Joey = joey.Checked;
            s.DayNight = mode.SelectedIndex switch { 1 => "day", 2 => "night", _ => "auto" };
            s.DayStartHour = (int)dayHour.Value;
            s.NightStartHour = (int)nightHour.Value;
            s.Season = season.SelectedIndex switch { 1 => "spring", 2 => "summer", 3 => "autumn", 4 => "winter", _ => "auto" };
            s.Hemisphere = hemi.SelectedIndex switch { 1 => "north", 2 => "south", _ => "auto" };
            s.AutoStart = auto.Checked;
            s.IdleMinutes = (int)wait.Value;
            try { s.Save(); Startup.Set(startup.Checked); }
            catch (Exception ex) { MessageBox.Show(this, ex.Message, "Could not save settings", MessageBoxButtons.OK, MessageBoxIcon.Warning); }
            Close();
        };

        Controls.AddRange(new Control[] { themeLbl, theme, clock, sound, joey, dn, sg, group, note, ok, cancel });
    }
}
