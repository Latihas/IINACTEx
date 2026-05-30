using System;
using System.Diagnostics.CodeAnalysis;

namespace Advanced_Combat_Tracker;

[SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
public class HistoryRecord(int type, DateTime startTime, DateTime endTime, string label, string charName, string folderHint = "")
	: IComparable<HistoryRecord>, IEquatable<HistoryRecord> {
	[SuppressMessage("ReSharper", "UnusedMember.Global")]
	public TimeSpan Duration => EndTime - StartTime;
	[SuppressMessage("ReSharper", "UnusedMember.Global")]
	public string CharName { get; set; } = charName;
	public DateTime StartTime { get; set; } = startTime;
	public DateTime EndTime { get; set; } = endTime;
	public int Type { get; set; } = type;
	public string Label { get; set; } = label;
	public string FolderHint { get; set; } = folderHint;
	public int CompareTo(HistoryRecord? other) => StartTime.CompareTo(other!.StartTime);
	public bool Equals(HistoryRecord? other) => StartTime.Equals(other!.StartTime);

	public override bool Equals(object? obj) {
		if (obj == DBNull.Value) return false;
		if (obj == null) return false;
		var historyRecord = (HistoryRecord)obj;
		return StartTime.Equals(historyRecord.StartTime);
	}

	public override string ToString() {
		var duration = EndTime - StartTime;
		var durationString = duration.ToString(duration.TotalHours >= 1 ? @"hh\:mm\:ss" : @"mm\:ss");
		var folderHint = ActGlobals.oFormActMain.LogFilePath.Equals(FolderHint, StringComparison.OrdinalIgnoreCase)
			? string.Empty
			: FolderHint.ToLower();
		var labelPrefix = Type == 1 ? "     " : string.Empty;
		return $"{labelPrefix}{Label} - {StartTime:MM/dd/yyyy h:mm:ss tt} [{durationString}] {folderHint}";
	}

	public override int GetHashCode() => ToString().GetHashCode();
}