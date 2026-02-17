using System;
using System.IO;
using Advanced_Combat_Tracker;

namespace SilverDasher.ACT.Storages;

public static class DataStorage
{
	public static int SelectedNest = 0;

	private static string[] nests = new string[3] { "garlandtools.cn", "silverdasher.com", "" };

	public static string LogFile = Path.Combine(LogPath, $"{DateTime.Now.ToFileTime()}.log");

	public static string Title => "银山雀儿";

	public static int Version => 393220;

	public static string BasePath => Path.Combine(ActGlobals.oFormActMain.AppDataFolder.FullName, "SilverDasher");

	public static string ConfigPath => Path.Combine(BasePath, "config.json");

	public static string LogPath => Path.Combine(BasePath, "logs");

	public static string SilverDasherNest => "https://nest." + nests[SelectedNest] + "/";

	public static string SilverDasherTree => "wss://tree." + nests[SelectedNest] + "/mqtt";

	public static void SetExtraServer(string server)
	{
		nests[2] = server;
	}
}
