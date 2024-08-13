using SkiaSharp;
using Svg.Skia;
using System.IO;

namespace Claudable.Utilities
{
    public static class SVGRasterizer
    {
        public static void GenerateArtifactIcon(string outputPath, string svgContent, bool asIcon = false, int size = 256)
        {
            using (var svg = new SKSvg())
            {
                svg.FromSvg(svgContent);

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
                    using (var data = image.Encode(asIcon ? SKEncodedImageFormat.Ico : SKEncodedImageFormat.Png, 100))
                    using (var stream = File.OpenWrite(outputPath))
                    {
                        data.SaveTo(stream);
                    }
                }
            }
        }
    }
}