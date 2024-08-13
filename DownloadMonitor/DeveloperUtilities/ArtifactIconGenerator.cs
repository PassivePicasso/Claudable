using SkiaSharp;
using Svg.Skia;
using System.IO;
using System.Text;

public static class ArtifactIconGenerator
{
    public static void GenerateArtifactIcon(string outputPath, int size = 256)
    {
        string svgContent = GenerateSvgContent();

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
                using (var data = image.Encode(SKEncodedImageFormat.Png, 100))
                using (var stream = File.OpenWrite(outputPath))
                {
                    data.SaveTo(stream);
                }
            }
        }
    }

    private static string GenerateSvgContent()
    {
        var svg = new StringBuilder();
        svg.AppendLine("<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"100\" height=\"100\" viewBox=\"0 0 100 100\">");

        // File icon background
        svg.AppendLine("  <rect x=\"10\" y=\"10\" width=\"80\" height=\"80\" rx=\"10\" ry=\"10\" fill=\"#e6e6e6\"/>");

        // 'A' letter
        svg.AppendLine("  <text x=\"50\" y=\"65\" font-family=\"Arial\" font-size=\"50\" font-weight=\"bold\" text-anchor=\"middle\" fill=\"#464646\">A</text>");

        // Arrow
        svg.AppendLine("  <g fill=\"none\" stroke=\"#464646\" stroke-width=\"5\" stroke-linecap=\"round\">");
        svg.AppendLine("    <line x1=\"30\" y1=\"70\" x2=\"15\" y2=\"55\"/>");
        svg.AppendLine("    <line x1=\"15\" y1=\"55\" x2=\"25\" y2=\"55\"/>");
        svg.AppendLine("    <line x1=\"15\" y1=\"55\" x2=\"15\" y2=\"65\"/>");
        svg.AppendLine("  </g>");

        svg.AppendLine("</svg>");
        return svg.ToString();
    }
}