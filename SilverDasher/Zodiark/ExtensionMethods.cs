using System;

namespace Zodiark;

public static class ExtensionMethods
{
	public static string ToHex(this IntPtr p)
	{
		return $"0x{(ulong)(long)p:X}";
	}
}
