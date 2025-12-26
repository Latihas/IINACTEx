using System.Numerics;

namespace IINACT.Latihas;

public class Color
{
    public static readonly Vector4 TWhite = new Vector4(1f, 1f, 1f, .3f);           
    public static readonly Vector4 TRed = new Vector4(1f, 0f, 0f, .3f);             
    public static readonly Vector4 LRed = new Vector4(1f, 0f, 0f, 1);             
    public static readonly Vector4 TYellow = new Vector4(1f, 1f, 0f, .3f);        
    public static readonly Vector4 LYellow = new Vector4(1f, 1f, 0f, 1);        
    public static readonly Vector4 TBlue = new Vector4(0f, 0f, 1f, .3f);            
    public static readonly Vector4 TGray = new Vector4(0.8f, 0.8f, 0.8f, .3f); 
    public static readonly Vector4 TCyan = new Vector4(0f, 1f, 1f, .3f);           
    public static readonly Vector4 TPurple = new Vector4(0.8f, 0f, 0.8f, .3f);     
    public static readonly Vector4 LPurple = new Vector4(0.8f, 0f, 0.8f, 1);     
    public static readonly Vector4 Black = new Vector4(0,0, 0, 0);     
}
