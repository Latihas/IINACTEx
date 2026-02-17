#define TRACE
using System;
using System.Diagnostics;
using System.IO;
using SilverDasher.ACT.Storages;

namespace SilverDasher.ACT.Doppelgangers;

internal class Logger : Doppelganger
{
	internal Logger(SilverDasher self)
		: base(self)
	{
	}

	internal override void Init()
	{
	}

	internal override void Deinit()
	{
	}

	internal new void Log(string s)
	{
		string s2 = DateTime.Now.ToString("[yyyy-MM-dd HH:mm:ss] ") + s;
		LogToView(s2);
		LogToFile(s2);
	}

	internal new void Debug(string s)
	{
		string s2 = DateTime.Now.ToString("[yyyy-MM-dd HH:mm:ss] ") + s;
		LogToFile(s2);
		if (Keeper.Config.ExtendedReport)
		{
			LogToView(s2);
		}
	}

	internal void LogToView(string s)
	{
		if (base.Painter.pluginControl != null)
		{
			base.Painter.pluginControl.Log(s);
		}
	}

	internal void LogToFile(string s)
	{
		Trace.WriteLine(s);
		lock (this)
		{
			if (!Directory.Exists(DataStorage.LogPath))
			{
				Directory.CreateDirectory(DataStorage.LogPath);
			}
			File.AppendAllText(DataStorage.LogFile, s + "\n");
		}
	}
}
