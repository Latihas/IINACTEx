using Advanced_Combat_Tracker;

namespace IINACT;

internal static class NetworkLogCleanup
{
    private const int MinimumRetentionDays = 1;
    private const int MaximumRetentionDays = 3650;

    public static void Cleanup(Configuration configuration)
    {
        try
        {
            if (!configuration.AutoDeleteNetworkLogs)
                return;

            var retentionDays = Math.Clamp(configuration.NetworkLogRetentionDays, MinimumRetentionDays, MaximumRetentionDays);
            if (retentionDays != configuration.NetworkLogRetentionDays)
            {
                configuration.NetworkLogRetentionDays = retentionDays;
                configuration.Save();
            }

            var logDirectory = configuration.LogFilePath;
            if (!Directory.Exists(logDirectory))
                return;

            var filter = ActGlobals.oFormActMain.LogFileFilter;
            if (string.IsNullOrWhiteSpace(filter))
            {
                Plugin.Log.Warning("Log file filter is empty, exiting cleanup early");
                return;
            }

            var cutoffUtc = DateTime.UtcNow.AddDays(-retentionDays);
            var deletedCount = 0;

            foreach (var file in Directory.EnumerateFiles(logDirectory, filter, SearchOption.TopDirectoryOnly))
            {
                try
                {
                    var fileInfo = new FileInfo(file);
                    if (fileInfo.LastWriteTimeUtc >= cutoffUtc)
                        continue;

                    fileInfo.Delete();
                    deletedCount++;
                }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                {
                    Plugin.Log.Warning(ex, $"Failed to delete network log file '{file}'");
                }
            }

            if (deletedCount > 0)
            {
                Plugin.Log.Information(
                    $"Deleted {deletedCount} network log files older than {retentionDays} days from '{logDirectory}'");
            }
        }
        catch (Exception ex)
        {
            Plugin.Log.Error(ex, "Failed while deleting old network log files");
        }
    }
}

