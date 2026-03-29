using System;
using System.Runtime.InteropServices;
using System.Text;

namespace SilverDasher.ACT.Doppelgangers;

public static partial class Tailor {
	[LibraryImport("SilverDasher.Weaver.dll")]
	private static partial IntPtr Weave(ref byte a, ref byte b);

	[LibraryImport("SilverDasher.Weaver.dll")]
	private static partial IntPtr Seal(ref byte a, ref byte b);


	[LibraryImport("SilverDasher.Weaver.dll", EntryPoint = "Judge")]
	private static partial IntPtr JudgeRaw();

	public static string Judge() => Convert(JudgeRaw());

	public static string Weave(string a, string b) => Convert(Weave(ref Encoding.UTF8.GetBytes(a)[0], ref Encoding.UTF8.GetBytes(b)[0]));

	public static string Seal(string a, string b) => Convert(Seal(ref Encoding.UTF8.GetBytes(a)[0], ref Encoding.UTF8.GetBytes(b)[0]));

	private static string Convert(IntPtr ptr) => Marshal.PtrToStringAnsi(ptr)!.Trim();
}