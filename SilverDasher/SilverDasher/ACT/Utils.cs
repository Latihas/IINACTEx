using System;

namespace SilverDasher.ACT;

internal static class Utils {
	internal static string GetPluginDirectory() => SilverDasher.Datadir;
	internal static void ShowMessageBox(string message) => (SilverDasher.Instance ?? throw new InvalidOperationException("SilverDasher is not initialized.")).Logger.Log(message);

	public static string CurrentUTCTimeStamp(bool isMinseconds = false) {
		var timeSpan = DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, 0);
		return Convert.ToInt64(isMinseconds ? timeSpan.TotalMilliseconds : timeSpan.TotalSeconds).ToString();
	}
}
