using System;
using System.IO;
using System.Reflection;
using System.Windows;
using Advanced_Combat_Tracker;
using SilverDasher.ACT.Storages;

namespace SilverDasher.ACT;

internal class Utils
{
	internal static string GetPluginDirectory()
	{
		foreach (ActPluginData actPlugin in ActGlobals.oFormActMain.ActPlugins)
		{
			if (actPlugin.pluginFile.Name == "SilverDasher.dll")
			{
				return actPlugin.pluginFile.Directory.FullName;
			}
		}
		string location = Assembly.GetExecutingAssembly().Location;
		if (location == "")
		{
			throw new IOException("Failed to get plugin directory.");
		}
		return Path.GetDirectoryName(location);
	}

	internal static void ShowMessageBox(string message)
	{
		MessageBox.Show(message, DataStorage.Title, MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.OK, MessageBoxOptions.ServiceNotification);
	}

	public static string CurrentTimeStamp(bool isMinseconds = false)
	{
		TimeSpan timeSpan = DateTime.Now - new DateTime(1970, 1, 1, 0, 0, 0, 0);
		return Convert.ToInt64(isMinseconds ? timeSpan.TotalMilliseconds : timeSpan.TotalSeconds).ToString();
	}

	public static string CurrentUTCTimeStamp(bool isMinseconds = false)
	{
		TimeSpan timeSpan = DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, 0);
		return Convert.ToInt64(isMinseconds ? timeSpan.TotalMilliseconds : timeSpan.TotalSeconds).ToString();
	}
}
