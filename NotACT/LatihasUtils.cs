using System;
using System.Drawing;

namespace Advanced_Combat_Tracker;

public static class LatihasUtils {
	public static Color ToAdjustedColor(this Color color, bool InvertLuminosity, float OffsetLuminosity = 0f, float OffsetSaturation = 0f) {
		var colorRGB = new ColorRGB(color);
		var h = colorRGB.H;
		var num = colorRGB.S + OffsetSaturation;
		var num2 = colorRGB.L + OffsetLuminosity;
		if (num > 1f)
			num = 1f;
		if (num < -1f)
			num = -1f;
		if (num2 > 1f)
			num2 = 1f;
		if (num2 < -1f)
			num2 = -1f;
		colorRGB = ColorRGB.FromHSL(h, num, num2);
		if (InvertLuminosity) {
			colorRGB = !(colorRGB.L < 0.55) || !(colorRGB.L > 0.45) ? ColorRGB.FromHSL(colorRGB.H, colorRGB.S, 1f - colorRGB.L) : ColorRGB.FromHSL(colorRGB.H, colorRGB.S, 0.65);
			return Color.FromArgb(color.A, colorRGB.R, colorRGB.G, colorRGB.B);
		}
		return Color.FromArgb(color.A, colorRGB.R, colorRGB.G, colorRGB.B);
	}

	private class ColorRGB {
		public byte R;

		public byte G;

		public byte B;

		public byte A;

		public float H => ((Color)this).GetHue() / 360f;

		public float S => ((Color)this).GetSaturation();

		public float L => ((Color)this).GetBrightness();

		public ColorRGB() {
			R = byte.MaxValue;
			G = byte.MaxValue;
			B = byte.MaxValue;
			A = byte.MaxValue;
		}

		public ColorRGB(Color value) {
			R = value.R;
			G = value.G;
			B = value.B;
			A = value.A;
		}

		public static implicit operator Color(ColorRGB rgb) => Color.FromArgb(rgb.A, rgb.R, rgb.G, rgb.B);

		public static explicit operator ColorRGB(Color c) => new(c);

		public static ColorRGB FromHSL(double H, double S, double L) => FromHSLA(H, S, L, 1.0);

		public static ColorRGB FromHSLA(double H, double S, double L, double A) {
			if (S > 1.0)
				S = 1.0;
			if (S < 0.0)
				S = 0.0;
			if (L > 1.0)
				L = 1.0;
			if (L < 0.0)
				L = 0.0;
			if (A > 1.0)
				A = 1.0;
			if (A < 0.0)
				A = 0.0;
			var num = L;
			var num2 = L;
			var num3 = L;
			var num4 = L <= 0.5 ? L * (1.0 + S) : L + S - L * S;
			if (num4 > 0.0) {
				var num5 = L + L - num4;
				var num6 = (num4 - num5) / num4;
				H *= 6.0;
				var num7 = (int)H;
				var num8 = num4 * num6 * (H - num7);
				var num9 = num5 + num8;
				var num10 = num4 - num8;
				switch (num7) {
					case 0:
						num = num4;
						num2 = num9;
						num3 = num5;
						break;
					case 1:
						num = num10;
						num2 = num4;
						num3 = num5;
						break;
					case 2:
						num = num5;
						num2 = num4;
						num3 = num9;
						break;
					case 3:
						num = num5;
						num2 = num10;
						num3 = num4;
						break;
					case 4:
						num = num9;
						num2 = num5;
						num3 = num4;
						break;
					case 5:
						num = num4;
						num2 = num5;
						num3 = num10;
						break;
				}
			}
			return new ColorRGB {
				R = Convert.ToByte(num * 255.0),
				G = Convert.ToByte(num2 * 255.0),
				B = Convert.ToByte(num3 * 255.0),
				A = Convert.ToByte(A * 255.0)
			};
		}
	}
}