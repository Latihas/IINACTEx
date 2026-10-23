namespace RainbowMage.OverlayPlugin;

internal static class Util {
	/// <summary>
	///     文字列中に含まれる double.NaN.ToString() で出力される文字列を指定した文字列で置換します。
	/// </summary>
	/// <param name="str"></param>
	/// <param name="replace"></param>
	/// <returns></returns>
	public static string ReplaceNaNString(string str, string replace) => str.Replace(double.NaN.ToString(), replace);
}