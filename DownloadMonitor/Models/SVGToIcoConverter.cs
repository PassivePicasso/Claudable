//using SkiaSharp;
//using Svg.Skia;
//using System;
//using System.IO;

//namespace DownloadMonitor.Utilities
//{
//    public class SvgToIcoConverter
//    {
//        public static bool ConvertSvgToIco(string svgFilePath, string icoFilePath, int[] sizes = null)
//        {
//            if (string.IsNullOrEmpty(svgFilePath) || string.IsNullOrEmpty(icoFilePath))
//            {
//                throw new ArgumentException("File paths cannot be null or empty.");
//            }

//            if (!File.Exists(svgFilePath))
//            {
//                throw new FileNotFoundException("SVG file not found.", svgFilePath);
//            }

//            if (sizes == null || sizes.Length == 0)
//            {
//                sizes = new int[] { 16, 32, 48, 64, 128, 256 };
//            }

//            try
//            {
//                using (var svgStream = File.OpenRead(svgFilePath))
//                using (var icoStream = File.Create(icoFilePath))
//                {
//                    var svgImage = SKSvg.CreateFromStream(svgStream);
//                    var iconWriter = new BinaryWriter(icoStream);

//                    // Write ICO header
//                    WriteIcoHeader(iconWriter, sizes.Length);

//                    long dataOffset = 6 + (16 * sizes.Length);

//                    // Write directory entries and image data
//                    foreach (int size in sizes)
//                    {
//                        using (var surface = SKSurface.Create(new SKImageInfo(size, size, SKColorType.Bgra8888)))
//                        {
//                            var canvas = surface.Canvas;
//                            canvas.Clear(SKColors.Transparent);
//                            canvas.DrawPicture(svgImage.Picture, SKMatrix.CreateScale(size / svgImage.Picture.CullRect.Width, size / svgImage.Picture.CullRect.Height));

//                            using (var image = surface.Snapshot())
//                            using (var data = image.Encode(SKEncodedImageFormat.Png, 100))
//                            {
//                                WriteDirectoryEntry(iconWriter, size, dataOffset, (uint)data.Size);
//                                dataOffset += data.Size;
//                            }
//                        }
//                    }

//                    // Write actual image data
//                    foreach (int size in sizes)
//                    {
//                        using (var surface = SKSurface.Create(new SKImageInfo(size, size, SKColorType.Bgra8888)))
//                        {
//                            var canvas = surface.Canvas;
//                            canvas.Clear(SKColors.Transparent);
//                            canvas.DrawPicture(svgImage.Picture, SKMatrix.CreateScale(size / svgImage.Picture.CullRect.Width, size / svgImage.Picture.CullRect.Height));

//                            using (var image = surface.Snapshot())
//                            using (var data = image.Encode(SKEncodedImageFormat.Png, 100))
//                            {
//                                data.SaveTo(icoStream);
//                            }
//                        }
//                    }
//                }

//                return true;
//            }
//            catch (Exception ex)
//            {
//                Console.WriteLine($"Error converting SVG to ICO: {ex.Message}");
//                return false;
//            }
//        }

//        private static void WriteIcoHeader(BinaryWriter writer, int imageCount)
//        {
//            writer.Write((short)0);  // Reserved
//            writer.Write((short)1);  // Type (1 for ICO)
//            writer.Write((short)imageCount);  // Number of images
//        }

//        private static void WriteDirectoryEntry(BinaryWriter writer, int size, long dataOffset, uint dataSize)
//        {
//            writer.Write((byte)size);  // Width
//            writer.Write((byte)size);  // Height
//            writer.Write((byte)0);  // Color palette
//            writer.Write((byte)0);  // Reserved
//            writer.Write((short)1);  // Color planes
//            writer.Write((short)32);  // Bits per pixel
//            writer.Write((uint)dataSize);  // Size of image data
//            writer.Write((uint)dataOffset);  // Offset of image data
//        }
//    }
//}
