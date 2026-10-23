using System;
using System.Runtime.InteropServices;

namespace RainbowMage.OverlayPlugin;

/// <summary>
///     ネイティブ関数を提供します。
/// </summary>
internal class NativeMethods {
	private const uint WINEVENT_OUTOFCONTEXT = 0;
	private const uint EVENT_SYSTEM_FOREGROUND = 3;
	private const uint EVENT_SYSTEM_MINIMIZEEND = 0x0017;
	public NativeMethods(TinyIoCContainer container) {
		WinEventDelegate dele = WinEventProc;
		var result = SetWinEventHook(EVENT_SYSTEM_FOREGROUND, EVENT_SYSTEM_FOREGROUND, IntPtr.Zero, dele, 0, 0,
			WINEVENT_OUTOFCONTEXT);
		if (result == IntPtr.Zero)
			container.Resolve<ILogger>().Log(LogLevel.Error, "Failed to register window foreground hook!");

		result = SetWinEventHook(EVENT_SYSTEM_MINIMIZEEND, EVENT_SYSTEM_MINIMIZEEND, IntPtr.Zero, dele, 0, 0,
			WINEVENT_OUTOFCONTEXT);
		if (result == IntPtr.Zero)
			container.Resolve<ILogger>().Log(LogLevel.Error, "Failed to register window minimized hook!");
	}

	[DllImport("user32.dll")]
	private static extern IntPtr SetWinEventHook(
		uint eventMin, uint eventMax, IntPtr hmodWinEventProc, WinEventDelegate lpfnWinEventProc, uint idProcess,
		uint idThread, uint dwFlags);

	private void WinEventProc(
		IntPtr hWinEventHook, uint eventType, IntPtr hwnd, int idObject, int idChild, uint dwEventThread,
		uint dwmsEventTime) {
		//ActiveWindowHandle = hwnd;
		//ActiveWindowChanged?.Invoke(null, hwnd);
	}


	[DllImport("kernel32.dll")]
	public static extern IntPtr GetCurrentProcess();

	[DllImport("kernel32.dll")]
	public static extern bool ReadProcessMemory(
		IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, IntPtr nSize, ref IntPtr lpNumberOfBytesRead);

	private delegate void WinEventDelegate(
		IntPtr hWinEventHook, uint eventType, IntPtr hwnd, int idObject, int idChild, uint dwEventThread,
		uint dwmsEventTime);

}