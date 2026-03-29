using System.Runtime.InteropServices;
using static ChineseConverter.NativeMethods;

namespace ChineseConverter;

public static class ChineseConverter {
	public static string Convert(string text, ChineseConversionDirection direction) {
		if (string.IsNullOrEmpty(text)) return string.Empty;
		var officeConversionEngine = OfficeConversionEngine.Create();
		if (officeConversionEngine != null) return officeConversionEngine.TcscConvert(text, direction);
		var dwMapFlags = direction == ChineseConversionDirection.TraditionalToSimplified ? 0x2000000u : 0x4000000u;
		var num = text.Length * 2 + 2;
		var intPtr = Marshal.AllocHGlobal(num);
		_ = LCMapString(2052, dwMapFlags, text, -1, intPtr, num);
		var result = Marshal.PtrToStringUni(intPtr);
		Marshal.FreeHGlobal(intPtr);
		return result;
	}
}