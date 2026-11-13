namespace PocketPossum;

/// <summary>Tiny diagnostic log at %LOCALAPPDATA%\PocketPossum\log.txt (screensavers have no console).</summary>
static class Log
{
    public static void Write(string msg)
    {
        try
        {
            var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "PocketPossum");
            Directory.CreateDirectory(dir);
            var file = Path.Combine(dir, "log.txt");
            if (File.Exists(file) && new FileInfo(file).Length > 100_000) File.Delete(file);
            File.AppendAllText(file, $"{DateTime.Now:s} {msg}{Environment.NewLine}");
        }
        catch { /* never let logging break the saver */ }
    }
}
