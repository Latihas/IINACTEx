using System;
using System.IO;
using SilverDasher.ACT.Storages;

namespace SilverDasher.ACT.Doppelgangers;

internal abstract class Doppelganger
{
	internal SilverDasher Self;

	internal Messager Messager => Self.Messager;

	internal Negotiator Negotiator => Self.Negotiator;

	internal Agent Agent => Self.Agent;

	internal Notifier Notifier => Self.Notifier;

	internal Overseer Overseer => Self.Overseer;

	internal Painter Painter => Self.Painter;

	internal Keeper Keeper => Self.Keeper;

	internal Logger Logger => Self.Logger;

	internal Primal Primal => Self.Primal;

	internal Doppelganger(SilverDasher self)
	{
		Self = self;
	}

	internal void Log(string s)
	{
		Logger.Log(s);
	}

	internal void Debug(string s)
	{
		Logger.Debug(s);
	}

	internal void FileLog(string s)
	{
		string text = DateTime.Now.ToString("[yyyy-MM-dd HH:mm:ss] ") + s;
		lock (Self)
		{
			if (!Directory.Exists(DataStorage.LogPath))
			{
				Directory.CreateDirectory(DataStorage.LogPath);
			}
			File.AppendAllText(DataStorage.LogFile, text + "\n");
		}
	}

	internal abstract void Init();

	internal abstract void Deinit();
}
