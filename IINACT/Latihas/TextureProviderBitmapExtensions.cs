using System.Drawing.Imaging;
using Dalamud.Interface.Textures;
using Dalamud.Interface.Textures.TextureWraps;
using Dalamud.Plugin.Services;

namespace IINACT.Latihas;

public static class TextureProviderBitmapExtensions
{
    // DXGI_FORMAT_B8G8R8A8_UNORM 的官方数值（替代硬编码的87，提高可读性）
    private const int DxgiFormatB8G8R8A8Unorm = 87;

    /// <summary>
    /// 从Bitmap同步创建IDalamudTextureWrap（自动处理格式转换和数据对齐）
    /// </summary>
    /// <param name="textureProvider">ITextureProvider实例（Dalamud注入）</param>
    /// <param name="bitmap">待转换的Bitmap对象</param>
    /// <param name="debugName">调试名称（排查资源泄漏）</param>
    /// <returns>可渲染的纹理对象（使用后必须Dispose）</returns>
    /// <exception cref="ArgumentNullException">bitmap为null时抛出</exception>
    public static IDalamudTextureWrap CreateFromBitmap(
        this ITextureProvider textureProvider,
        Bitmap bitmap,
        string? debugName = "BitmapTexture")
    {
        if (bitmap == null) throw new ArgumentNullException(nameof(bitmap));

        // 强制转换为32位ARGB格式（解决格式不兼容问题）
        using var argbBitmap = ConvertTo32BppArgb(bitmap);
        return CreateFromBitmapInternal(textureProvider, argbBitmap, debugName);
    }

    /// <summary>
    /// 从Bitmap异步创建IDalamudTextureWrap（适合大尺寸图片）
    /// </summary>
    public static async Task<IDalamudTextureWrap> CreateFromBitmapAsync(
        this ITextureProvider textureProvider,
        Bitmap bitmap,
        string? debugName = "BitmapTextureAsync",
        CancellationToken cancellationToken = default)
    {
        if (bitmap == null) throw new ArgumentNullException(nameof(bitmap));

        // 强制转换为32位ARGB并提取像素数据（线程安全）
        byte[] bgraPixels;
        RawImageSpecification specs;
        lock (bitmap)
        {
            using var argbBitmap = ConvertTo32BppArgb(bitmap);
            var bitmapData = argbBitmap.LockBits(
                new Rectangle(0, 0, argbBitmap.Width, argbBitmap.Height),
                ImageLockMode.ReadOnly,
                PixelFormat.Format32bppArgb);

            try
            {
                // 提取有效像素数据并转换为BGRA格式（解决顺序不匹配问题）
                bgraPixels = ExtractBgraPixels(bitmapData);
                
                // 配置纹理规格（匹配BGRA格式）
                specs = new RawImageSpecification
                {
                    Width = argbBitmap.Width,
                    Height = argbBitmap.Height,
                    DxgiFormat = DxgiFormatB8G8R8A8Unorm
                };
            }
            finally
            {
                argbBitmap.UnlockBits(bitmapData); // 必须解锁，避免内存泄漏
            }
        }

        // 异步创建纹理
        return await textureProvider.CreateFromRawAsync(
            specs,
            bgraPixels,
            debugName,
            cancellationToken);
    }

    #region 内部辅助方法
    /// <summary>
    /// 强制将Bitmap转换为32位ARGB格式（解决格式不兼容）
    /// </summary>
    private static Bitmap ConvertTo32BppArgb(Bitmap source)
    {
        if (source.PixelFormat == PixelFormat.Format32bppArgb)
        {
            return new Bitmap(source); // 已是目标格式，直接返回副本
        }

        // 强制创建32位ARGB格式的新Bitmap
        var target = new Bitmap(source.Width, source.Height, PixelFormat.Format32bppArgb);
        target.SetResolution(source.HorizontalResolution, source.VerticalResolution);
        
        // 绘制原图像到新Bitmap（完成格式转换）
        using var g = Graphics.FromImage(target);
        g.DrawImage(source, 0, 0);
        return target;
    }

    /// <summary>
    /// 从BitmapData中提取BGRA格式的像素数据（处理Stride和颜色顺序）
    /// </summary>
    private static byte[] ExtractBgraPixels(BitmapData bitmapData)
    {
        var width = bitmapData.Width;
        var height = bitmapData.Height;
        var stride = bitmapData.Stride;
        var pixelCount = width * height;
        var bgraPixels = new byte[pixelCount * 4]; // 纹理预期的纯像素数据（无对齐填充）

        // 逐行复制并转换ARGB→BGRA
        unsafe
        {
            byte* srcPtr = (byte*)bitmapData.Scan0.ToPointer();
            int dstIndex = 0;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    // ARGB顺序（Bitmap）→ BGRA顺序（DXGI）
                    byte a = srcPtr[x * 4 + 3];
                    byte r = srcPtr[x * 4 + 2];
                    byte g = srcPtr[x * 4 + 1];
                    byte b = srcPtr[x * 4 + 0];

                    // 写入BGRA数据
                    bgraPixels[dstIndex++] = b;
                    bgraPixels[dstIndex++] = g;
                    bgraPixels[dstIndex++] = r;
                    bgraPixels[dstIndex++] = a;
                }
                srcPtr += stride; // 移动到下一行（处理Stride对齐）
            }
        }

        return bgraPixels;
    }

    /// <summary>
    /// 同步创建纹理的核心逻辑（内部调用）
    /// </summary>
    private static IDalamudTextureWrap CreateFromBitmapInternal(
        ITextureProvider textureProvider,
        Bitmap argbBitmap,
        string? debugName)
    {
        var bitmapData = argbBitmap.LockBits(
            new Rectangle(0, 0, argbBitmap.Width, argbBitmap.Height),
            ImageLockMode.ReadOnly,
            PixelFormat.Format32bppArgb);

        try
        {
            // 提取BGRA格式的像素数据（去除Stride填充，转换颜色顺序）
            var bgraPixels = ExtractBgraPixels(bitmapData);
            
            var specs = new RawImageSpecification
            {
                Width = argbBitmap.Width,
                Height = argbBitmap.Height,
                DxgiFormat = DxgiFormatB8G8R8A8Unorm
            };

            // 创建纹理（使用纯BGRA数据，长度匹配）
            return textureProvider.CreateFromRaw(specs, bgraPixels, debugName);
        }
        finally
        {
            argbBitmap.UnlockBits(bitmapData);
        }
    }
    #endregion
}