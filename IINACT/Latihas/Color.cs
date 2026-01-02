using System.Numerics;

namespace IINACT.Latihas;

public class Color {
    public static readonly Vector4 TWhite = new(1f, 1f, 1f, .3f);
    public static readonly Vector4 TRed = new(1f, 0f, 0f, .3f);
    public static readonly Vector4 LRed = new(1f, 0f, 0f, 1);
    public static readonly Vector4 TYellow = new(1f, 1f, 0f, .3f);
    public static readonly Vector4 LYellow = new(1f, 1f, 0f, 1);
    public static readonly Vector4 TBlue = new(0f, 0f, 1f, .3f);
    public static readonly Vector4 TGray = new(0.8f, 0.8f, 0.8f, .3f);
    public static readonly Vector4 TCyan = new(0f, 1f, 1f, .3f);
    public static readonly Vector4 TPurple = new(0.8f, 0f, 0.8f, .3f);
    public static readonly Vector4 LPurple = new(0.8f, 0f, 0.8f, 1);
    public static readonly Vector4 Black = new(0, 0, 0, 0);
}