using System;
using System.Diagnostics.CodeAnalysis;

namespace RainbowMage.OverlayPlugin;

[SuppressMessage("ReSharper", "UnusedMember.Global")]
[SuppressMessage("ReSharper", "UnusedParameter.Global")]
public interface ILogger {
	void Log(LogLevel level, string message);
	void Log(LogLevel level, string format, params object?[] args);
	void RegisterListener(Action<LogEntry> listener);
	void ClearListener();
}

[SuppressMessage("ReSharper", "UnusedType.Global")]
[SuppressMessage("ReSharper", "UnusedMember.Global")]
[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public class LogEntry(LogLevel level, DateTime time, string message) {
	public string Message { get; set; } = message;
	public LogLevel Level { get; set; } = level;
	public DateTime Time { get; set; } = time;
}

[SuppressMessage("ReSharper", "UnusedType.Global")]
[SuppressMessage("ReSharper", "UnusedMember.Global")]
public class LogEventArgs(LogLevel level, string message) : EventArgs {
	public string Message { get; private set; } = message;
	public LogLevel Level { get; private set; } = level;
}

public enum LogLevel {
	Trace,
	Debug,
	Info,
	Warning,
	Error
}