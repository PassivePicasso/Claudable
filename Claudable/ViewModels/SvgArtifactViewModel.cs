using Claudable.Utilities;
using SkiaSharp;
using Svg.Skia;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Claudable.ViewModels;

public class SvgArtifactViewModel : INotifyPropertyChanged
{
    private string _name;
    private string _content;
    private string _uuid;
    private ImageSource _renderedImage;

    public string Name
    {
        get => _name;
        set { _name = value; OnPropertyChanged(); }
    }

    public string Content
    {
        get => _content;
        set
        {
            _content = value;
            OnPropertyChanged();
            RenderSvg();
        }
    }

    public string Uuid
    {
        get => _uuid;
        set { _uuid = value; OnPropertyChanged(); }
    }

    public ImageSource RenderedImage
    {
        get => _renderedImage;
        set { _renderedImage = value; OnPropertyChanged(); }
    }

    public ICommand SaveAsPngCommand { get; }
    public ICommand SaveAsIcoCommand { get; }

    public SvgArtifactViewModel()
    {
        SaveAsPngCommand = new RelayCommand(SaveAsPng);
        SaveAsIcoCommand = new RelayCommand(SaveAsIco);
    }

    private void RenderSvg()
    {
        if (string.IsNullOrEmpty(Content))
        {
            return;
        }

        try
        {
            using (var svg = new SKSvg())
            {
                svg.FromSvg(Content);

                int width = (int)svg.Picture.CullRect.Width;
                int height = (int)svg.Picture.CullRect.Height;

                using (var surface = SKSurface.Create(new SKImageInfo(width, height)))
                {
                    var canvas = surface.Canvas;
                    canvas.Clear(SKColors.Transparent);
                    canvas.DrawPicture(svg.Picture);

                    using (var image = surface.Snapshot())
                    using (var data = image.Encode(SKEncodedImageFormat.Png, 100))
                    {
                        var stream = new MemoryStream(data.ToArray());
                        var bitmap = new BitmapImage();
                        bitmap.BeginInit();
                        bitmap.CacheOption = BitmapCacheOption.OnLoad;
                        bitmap.StreamSource = stream;
                        bitmap.EndInit();
                        bitmap.Freeze();

                        RenderedImage = bitmap;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error rendering SVG: {ex.Message}");
            // Set a placeholder image or show an error image
            RenderedImage = new BitmapImage(new Uri("/Resources/error_placeholder.png", UriKind.Relative));
        }
    }

    private void SaveAsPng()
    {
        string outputPath = $"{Name}.png";
        SVGRasterizer.GenerateArtifactIcon(outputPath, Content);
        // Implement saving logic here
    }

    private void SaveAsIco()
    {
        string outputPath = $"{Name}.ico";
        SVGRasterizer.GenerateArtifactIcon(outputPath, Content, true);
        // Implement saving logic here
    }

    public event PropertyChangedEventHandler PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}