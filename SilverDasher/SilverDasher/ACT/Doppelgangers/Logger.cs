#define TRACE
using System;
using System.Diagnostics;

namespace SilverDasher.ACT.Doppelgangers;

internal class Logger(SilverDasher self) : Doppelganger(self) {
    internal override void Init() {
    }

    internal override void Deinit() {
    }

    internal new void Log(string s) {
        var s2 = DateTime.Now.ToString("[yyyy-MM-dd HH:mm:ss] ") + s;
        LogToView(s2);
        LogToFile(s2);
    }

    internal new void Debug(string s) {
        var s2 = DateTime.Now.ToString("[yyyy-MM-dd HH:mm:ss] ") + s;
        LogToFile(s2);
        if (Keeper.Config.ExtendedReport) LogToView(s2);
    }

    private void LogToView(string s) => Painter.pluginControl?.Log(s);

    internal static void LogToFile(string s) {
        Trace.WriteLine(s);
        lock (SilverDasher.FileLogs) SilverDasher.FileLogs.Add(s);
    }
}