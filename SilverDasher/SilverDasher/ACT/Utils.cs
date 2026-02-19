using System;
using System.Windows;
using SilverDasher.ACT.Storages;

namespace SilverDasher.ACT;

internal static class Utils {
    internal static string GetPluginDirectory() => SilverDasher.Datadir;
    // {
    //     foreach (var actPlugin in ActGlobals.oFormActMain.ActPlugins.Where(actPlugin => actPlugin.pluginFile.Name == "SilverDasher.dll")) {
    //         return actPlugin.pluginFile.Directory.FullName;
    //     }
    //     var location = Assembly.GetExecutingAssembly().Location;
    //     return location == "" ? throw new IOException("Failed to get plugin directory.") : Path.GetDirectoryName(location);
    // }

    internal static void ShowMessageBox(string message) {
        MessageBox.Show(message, DataStorage.Title, MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.OK, MessageBoxOptions.ServiceNotification);
    }

    // public static string CurrentTimeStamp(bool isMinseconds = false) {
    //     var timeSpan = DateTime.Now - new DateTime(1970, 1, 1, 0, 0, 0, 0);
    //     return Convert.ToInt64(isMinseconds ? timeSpan.TotalMilliseconds : timeSpan.TotalSeconds).ToString();
    // }

    public static string CurrentUTCTimeStamp(bool isMinseconds = false) {
        var timeSpan = DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, 0);
        return Convert.ToInt64(isMinseconds ? timeSpan.TotalMilliseconds : timeSpan.TotalSeconds).ToString();
    }
}