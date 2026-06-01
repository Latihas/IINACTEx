using System;
using System.Runtime.InteropServices;

namespace ChineseConverter;

internal static partial class NativeMethods {
	[LibraryImport("KERNEL32.DLL", StringMarshalling = StringMarshalling.Utf16)]
	public static partial int LCMapString(int locale, uint dwMapFlags, [MarshalAs(UnmanagedType.LPTStr)] string lpSrcStr, int cchSrc, IntPtr lpDestStr, int cchDest);

	[LibraryImport("KERNEL32.DLL", StringMarshalling = StringMarshalling.Utf16)]
	public static partial IntPtr LoadLibrary([MarshalAs(UnmanagedType.LPTStr)] string lpFileName);

	[DllImport("KERNEL32.DLL", SetLastError = true)]
	[return: MarshalAs(UnmanagedType.Bool)]
	public static extern bool FreeLibrary(HandleRef hModule);

	[LibraryImport("MSTR2TSC.DLL")]
	[return: MarshalAs(UnmanagedType.Bool)]
	public static partial bool TCSCInitialize();

	[LibraryImport("MSTR2TSC.DLL")]
	[return: MarshalAs(UnmanagedType.Bool)]
	public static partial void TCSCUninitialize();

	[LibraryImport("MSTR2TSC.DLL")]
	[return: MarshalAs(UnmanagedType.Bool)]
	public static partial void TCSCFreeConvertedText(IntPtr pv);

	[LibraryImport("MSTR2TSC.DLL", StringMarshalling = StringMarshalling.Utf16)]
	[return: MarshalAs(UnmanagedType.Bool)]
	public static partial bool TCSCConvertText([MarshalAs(UnmanagedType.LPTStr)] string pwszInput, int cchInput, out IntPtr ppwszOutput, out int pcchOutput, ChineseConversionDirection dwDirection, [MarshalAs(UnmanagedType.Bool)] bool fCharBase,
		[MarshalAs(UnmanagedType.Bool)] bool fLocalTerm);
}