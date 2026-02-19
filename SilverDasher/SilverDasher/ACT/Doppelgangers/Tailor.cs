using System;
using System.Runtime.InteropServices;
using System.Text;

namespace SilverDasher.ACT.Doppelgangers;

public static class Tailor {
    [DllImport("SilverDasher.Weaver.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
    private static extern IntPtr Weave(ref byte a, ref byte b);

    [DllImport("SilverDasher.Weaver.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
    private static extern IntPtr Seal(ref byte a, ref byte b);

    // [DllImport("SilverDasher.Weaver.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
    // public static extern IntPtr Bension(ref byte r);

    [DllImport("SilverDasher.Weaver.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode, EntryPoint = "Judge")]
    private static extern IntPtr JudgeRaw();

    public static string Judge() => Convert(JudgeRaw());

    public static string Weave(string a, string b) => Convert(Weave(ref Encoding.UTF8.GetBytes(a)[0], ref Encoding.UTF8.GetBytes(b)[0]));

    public static string Seal(string a, string b) => Convert(Seal(ref Encoding.UTF8.GetBytes(a)[0], ref Encoding.UTF8.GetBytes(b)[0]));

    // public static string Bension(string r) => Convert(Bension(ref Encoding.UTF8.GetBytes(r)[0]));

    private static string Convert(IntPtr ptr) => Marshal.PtrToStringAnsi(ptr).Trim();
}