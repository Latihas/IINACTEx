using System;
using System.Runtime.InteropServices;
using System.Text;

namespace SilverDasher.ACT.Doppelgangers;

public static class Tailor
{
	[DllImport("SilverDasher.Weaver.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
	public static extern IntPtr Weave(ref byte a, ref byte b);

	[DllImport("SilverDasher.Weaver.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
	public static extern IntPtr Seal(ref byte a, ref byte b);

	[DllImport("SilverDasher.Weaver.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
	public static extern IntPtr Bension(ref byte r);

	[DllImport("SilverDasher.Weaver.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode, EntryPoint = "Judge")]
	public static extern IntPtr JudgeRaw();

	public static string Judge()
	{
		return Convert(JudgeRaw());
	}

	public static string Weave(string a, string b)
	{
		byte[] bytes = Encoding.UTF8.GetBytes(a);
		return Convert(Weave(b: ref Encoding.UTF8.GetBytes(b)[0], a: ref bytes[0]));
	}

	public static string Seal(string a, string b)
	{
		byte[] bytes = Encoding.UTF8.GetBytes(a);
		return Convert(Seal(b: ref Encoding.UTF8.GetBytes(b)[0], a: ref bytes[0]));
	}

	public static string Bension(string r)
	{
		return Convert(Bension(ref Encoding.UTF8.GetBytes(r)[0]));
	}

	public static string Convert(IntPtr ptr)
	{
		return Marshal.PtrToStringAnsi(ptr).Trim();
	}
}
