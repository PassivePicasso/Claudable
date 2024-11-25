using SkiaSharp;
using Svg.Skia;
using System.IO;

namespace Claudable.Utilities;

public static class SVGRasterizer
{
    public static void GenerateArtifactIcon(string outputPath, string svgContent, bool asIcon = false, int size = 256)
    {
        try
        {
            using (var svg = new SKSvg())
            {
                if (svg.FromSvg(svgContent) == null)
                {
                    throw new ArgumentException("Invalid SVG content");
                }

                using (var surface = SKSurface.Create(new SKImageInfo(size, size)))
                {
                    var canvas = surface.Canvas;
                    canvas.Clear(SKColors.Transparent);

                    // Calculate scaling factor
                    float scale = Math.Min((float)size / svg.Picture.CullRect.Width, (float)size / svg.Picture.CullRect.Height);
                    canvas.Scale(scale);

                    // Center the SVG
                    float xOffset = (size / scale - svg.Picture.CullRect.Width) / 2;
                    float yOffset = (size / scale - svg.Picture.CullRect.Height) / 2;
                    canvas.Translate(xOffset, yOffset);

                    canvas.DrawPicture(svg.Picture);

                    using (var image = surface.Snapshot())
                    {
                        SKData data;
                        if (asIcon)
                        {
                            // For icon format, we need to create multiple sizes
                            data = CreateIconData(image);
                        }
                        else
                        {
                            data = image.Encode(SKEncodedImageFormat.Png, 100);
                        }

                        if (data != null)
                        {
                            using (var stream = File.OpenWrite(outputPath))
                            {
                                data.SaveTo(stream);
                            }
                        }
                        else
                        {
                            throw new InvalidOperationException("Failed to encode the image");
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            throw new Exception($"Error generating artifact icon: {ex.Message}", ex);
        }
    }

    private static SKImage Resize(SKImage srcImg, int size)
    {
        var skInfo = new SKImageInfo(size, size);
        skInfo.ColorType = SKImageInfo.PlatformColorType;
        skInfo.AlphaType = SKAlphaType.Premul;
        using var surface = SKSurface.Create(skInfo);
        using var paint = new SKPaint();

        // high quality with antialiasing
        paint.IsAntialias = true;
        paint.FilterQuality = SKFilterQuality.High;

        // draw the bitmap to fill the surface
        surface.Canvas.DrawImage(srcImg, new SKRectI(0, 0, size, size), paint);
        surface.Canvas.Flush();

        return surface.Snapshot();
    }

    private static SKData CreateIconData(SKImage image)
    {
        // Icon format requires multiple sizes. We'll create 16x16, 32x32, 48x48, and 256x256
        int[] sizes = { 16, 32, 48, 256 };
        using (var iconStream = new MemoryStream())
        {
            // Write ICO header
            iconStream.Write(new byte[] { 0, 0, 1, 0, (byte)sizes.Length, 0 }, 0, 6);

            long dataOffset = 6 + 16 * sizes.Length;

            foreach (int size in sizes)
            {
                using var resizedImage = Resize(image, size);
                using var pngData = resizedImage.Encode(SKEncodedImageFormat.Png, 100);

                // Write ICO directory entry
                iconStream.WriteByte((byte)size);
                iconStream.WriteByte((byte)size);
                iconStream.WriteByte(0); // No color palette
                iconStream.WriteByte(0); // Reserved
                iconStream.Write(new byte[] { 1, 0 }, 0, 2); // Color planes
                iconStream.Write(new byte[] { 32, 0 }, 0, 2); // Bits per pixel
                var sizeBytes = BitConverter.GetBytes((int)pngData.Size);
                iconStream.Write(sizeBytes, 0, 4);
                var offsetBytes = BitConverter.GetBytes((int)dataOffset);
                iconStream.Write(offsetBytes, 0, 4);

                dataOffset += pngData.Size;
            }

            // Write PNG data for each size
            foreach (int size in sizes)
            {
                using var resizedImage = Resize(image, size);
                using var pngData = resizedImage.Encode(SKEncodedImageFormat.Png, 100);

                pngData.SaveTo(iconStream);
            }

            return SKData.CreateCopy(iconStream.ToArray());
        }
    }
}