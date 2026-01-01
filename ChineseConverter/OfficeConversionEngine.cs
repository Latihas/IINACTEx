using System;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace ChineseConverter;

internal class OfficeConversionEngine
{
	private static readonly string MsoPath;

	private static readonly string Mstr2TscPath;

	static OfficeConversionEngine()
	{
		string text = null;
		var registryKey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Office\12.0\Common\InstallRoot");
		if (registryKey != null) text = Convert.ToString(registryKey.GetValue("Path"), null);
		var registryKey2 = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Office\12.0\Common\FilesPaths");
		if (registryKey2 != null) MsoPath = Convert.ToString(registryKey2.GetValue("mso.dll"), null);
		if (!string.IsNullOrEmpty(text)) Mstr2TscPath = Path.Combine(text, "ADDINS\\MSTR2TSC.DLL");
		if (string.IsNullOrEmpty(Mstr2TscPath) || !File.Exists(Mstr2TscPath)) Mstr2TscPath = null;
	}

	public static OfficeConversionEngine Create()
	{
		if (string.IsNullOrEmpty(MsoPath) || string.IsNullOrEmpty(Mstr2TscPath)) return null;
		return new OfficeConversionEngine();
	}

	public string TcscConvert(string input, ChineseConversionDirection direction)
	{
		var intPtr = IntPtr.Zero;
		var intPtr2 = IntPtr.Zero;
		try
		{
			intPtr = NativeMethods.LoadLibrary(MsoPath);
			intPtr2 = NativeMethods.LoadLibrary(Mstr2TscPath);
			if (!NativeMethods.TCSCInitialize()) return null;
			string result = null;
			if (NativeMethods.TCSCConvertText(input, input.Length, out var ppwszOutput, out _, direction, fCharBase: false, fLocalTerm: true))
			{
				result = Marshal.PtrToStringUni(ppwszOutput);
				NativeMethods.TCSCFreeConvertedText(ppwszOutput);
			}
			NativeMethods.TCSCUninitialize();
			return result;
		}
		finally
		{
			if (intPtr2 != IntPtr.Zero) NativeMethods.FreeLibrary(new HandleRef(this, intPtr2));
			if (intPtr != IntPtr.Zero) NativeMethods.FreeLibrary(new HandleRef(this, intPtr));
		}
	}
}
