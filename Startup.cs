using Microsoft.Win32;

namespace PocketPossum;

/// <summary>"Launch with Windows" via HKCU\...\Run (no admin needed).</summary>
static class Startup
{
    const string KeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    const string ValueName = "PocketPossum";

    public static bool IsEnabled()
    {
        using var k = Registry.CurrentUser.OpenSubKey(KeyPath);
        return k?.GetValue(ValueName) != null;
    }

    public static void Set(bool enabled)
    {
        using var k = Registry.CurrentUser.CreateSubKey(KeyPath);
        if (enabled) k.SetValue(ValueName, $"\"{Environment.ProcessPath}\" /t");
        else k.DeleteValue(ValueName, throwOnMissingValue: false);
    }
}
