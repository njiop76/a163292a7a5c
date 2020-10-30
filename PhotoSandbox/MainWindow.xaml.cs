using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;
using ImageMagick;

namespace PhotoSandbox
{
    public partial class MainWindow : Window
    {
        public MagickImage magickImage;

        public MainWindow()
        {
            InitializeComponent();

            this.magickImage = new MagickImage("Image.jpg");

            Refresh();
        }

        public void Refresh()
        {
            using (Stream stream = new MemoryStream())
            {
                magickImage.Write(stream);
                stream.Position = 0;

                BitmapImage bitmapImage = new BitmapImage();

                bitmapImage.BeginInit();
                bitmapImage.StreamSource = stream;
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad; // Force chargement depuis stream
                bitmapImage.EndInit();

                this.imgImage.Source = bitmapImage;
            }
        }
    }
}
