using System;

namespace SilverDasher.ACT.Doppelgangers;

public abstract class Doppelganger(SilverDasher self) {
	internal readonly SilverDasher Self = self;

	internal Messager Messager => Self.Messager;

	internal Negotiator Negotiator => Self.Negotiator;

	internal Agent Agent => Self.Agent;

	internal Notifier Notifier => Self.Notifier;

	internal Overseer Overseer => Self.Overseer;

	internal Painter Painter => Self.Painter;

	internal Keeper Keeper => Self.Keeper;

	internal Logger Logger => Self.Logger;

	internal Primal Primal => Self.Primal;

	internal void Log(string s) => Logger.Log(s);

	internal void Debug(string s) => Logger.Debug(s);

	internal static void FileLog(string s) => Logger.LogToFile(DateTime.Now.ToString("[yyyy-MM-dd HH:mm:ss] ") + s);

	internal abstract void Init();

	internal abstract void Deinit();
}