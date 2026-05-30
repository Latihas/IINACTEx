using System;

namespace Advanced_Combat_Tracker;

public class LogLineEntry(DateTime time, string logLine, int parsedType, int globalTimeSorter) {
	public int GlobalTimeSorter { get; } = globalTimeSorter;
	public DateTime Time { get; set; } = time;
	public string LogLine { get; } = logLine;
	public int Type { get; } = parsedType;
	public bool SearchSelected { get; set; } = false;
}