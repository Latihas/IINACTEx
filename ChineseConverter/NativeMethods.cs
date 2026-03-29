using System;
using System.Runtime.InteropServices;

namespace ChineseConverter;

internal static class NativeMethods {
	[DllImport("KERNEL32.DLL", CharSet = CharSet.Unicode)]
	public static extern int LCMapString(int locale, uint dwMapFlags, [MarshalAs(UnmanagedType.LPTStr)] string lpSrcStr, int cchSrc, IntPtr lpDestStr, int cchDest);

	[DllImport("KERNEL32.DLL", CharSet = CharSet.Unicode)]
	public static extern IntPtr LoadLibrary([MarshalAs(UnmanagedType.LPTStr)] string lpFileName);

	[DllImport("KERNEL32.DLL", SetLastError = true)]
	[return: MarshalAs(UnmanagedType.Bool)]
	public static extern bool FreeLibrary(HandleRef hModule);

	[DllImport("MSTR2TSC.DLL")]
	[return: MarshalAs(UnmanagedType.Bool)]
	public static extern bool TCSCInitialize();

	[DllImport("MSTR2TSC.DLL")]
	[return: MarshalAs(UnmanagedType.Bool)]
	public static extern bool TCSCUninitialize();

	[DllImport("MSTR2TSC.DLL")]
	[return: MarshalAs(UnmanagedType.Bool)]
	public static extern bool TCSCFreeConvertedText(IntPtr pv);

	[DllImport("MSTR2TSC.DLL", CharSet = CharSet.Unicode)]
	[return: MarshalAs(UnmanagedType.Bool)]
	public static extern bool TCSCConvertText([MarshalAs(UnmanagedType.LPTStr)] string pwszInput, int cchInput, out IntPtr ppwszOutput, out int pcchOutput, ChineseConversionDirection dwDirection, [MarshalAs(UnmanagedType.Bool)] bool fCharBase,
		[MarshalAs(UnmanagedType.Bool)] bool fLocalTerm);
}