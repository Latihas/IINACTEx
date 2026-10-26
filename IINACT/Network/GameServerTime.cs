using System;

namespace IINACT.Network;

internal static class GameServerTime {
	public static long LastSeverTimestamp { get; private set; }

	private static long LastSeverTimestampTicks { get; set; }

	public static DateTime LastServerTime {
		get => LastSeverTimestamp > 0
			? field.AddTicks(LastSeverTimestamp * 10_000L).ToLocalTime()
			: DateTime.Now;
	} = DateTime.MinValue.AddYears(1969);

	public static DateTime CurrentServerTime => LastSeverTimestamp > 0
		? LastServerTime.AddMilliseconds(
			Environment.TickCount64 - LastSeverTimestampTicks)
		: DateTime.Now;

	internal static void SetLastServerTimestamp(ulong timestamp) {
		if (timestamp > 0) {
			LastSeverTimestamp = (long)timestamp;
			LastSeverTimestampTicks = Environment.TickCount64;
		}
	}
}