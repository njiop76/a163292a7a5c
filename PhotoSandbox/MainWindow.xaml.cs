using System;
using System.Drawing.Imaging;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media.Imaging;
using ImageMagick;
using Microsoft.Win32;

namespace PhotoSandbox
{
    public partial class MainWindow : Window
    {
        private MagickImage magickImage;

        public MainWindow()
        {
            InitializeComponent();

            this.magickImage = new MagickImage("Image.jpg");

            Refresh();
        }

        /// <summary>
        /// Refresh the image
        /// </summary>
        public void Refresh()
        {
            using (Stream stream = new MemoryStream())
            {
                //Reload image
                magickImage.Write(stream);
                stream.Position = 0;

                BitmapImage bitmapImage = new BitmapImage();

                bitmapImage.BeginInit();
                bitmapImage.StreamSource = stream;
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad; // Force chargement depuis stream
                bitmapImage.EndInit();

                this.imgImage.Source = bitmapImage;

                //Set canvas size for scrollbar
                imgCanvas.Width = bitmapImage.Width;
                imgCanvas.Height = bitmapImage.Height;

                //Reinit crop rectangle
                Canvas.SetTop(cropTool, 0);
                Canvas.SetLeft(cropTool, 0);
                cropTool.Width = bitmapImage.Width;
                cropTool.Height = bitmapImage.Height;

                rectangleGeo1.Rect = new Rect(0, 0, bitmapImage.Width, bitmapImage.Height);
                rectangleGeo2.Rect = new Rect(0, 0, bitmapImage.Width, bitmapImage.Height);
            }
        }

        /// <summary>
        /// Get Filter for the OpenFileDialog
        /// </summary>
        /// <returns></returns>
        private string GetFileDialogFilter()
        {

            var codecs = ImageCodecInfo.GetImageEncoders();
            var codecFilter = "Image Files|";
            foreach (var codec in codecs)
                codecFilter += codec.FilenameExtension + ";";

            return codecFilter;
        }

        /// <summary>
        /// Open dialog to change image
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MenuOpenClick(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = GetFileDialogFilter();

            if (openFileDialog.ShowDialog() == true)
            {
                this.magickImage = new MagickImage(openFileDialog.FileName);
                Refresh();
            }
        }

        /// <summary>
        /// Rotate image 90° to the left
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MenuRotateLeftClick(object sender, RoutedEventArgs e)
        {
            this.magickImage.Rotate(-90);
            Refresh();
        }

        /// <summary>
        /// Rotate image 90° to the right
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MenuRotateRightClick(object sender, RoutedEventArgs e)
        {
            this.magickImage.Rotate(90);
            Refresh();
        }

        /// <summary>
        /// Save the image considering crop
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MenuSaveClick(object sender, RoutedEventArgs e)
        {
            var imageToSave = magickImage.Clone();
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = GetFileDialogFilter();

            // Crop with the good ratio
            double xRatio = imageToSave.Width / imgCanvas.ActualWidth;
            double yRatio = imageToSave.Height / imgCanvas.ActualHeight;
            MagickGeometry geometry = new MagickGeometry();
            geometry.Width = (int)(cropTool.ActualWidth * xRatio);
            geometry.Height = (int)(cropTool.ActualHeight * yRatio);
            geometry.X = (int)(Canvas.GetLeft(cropTool) * xRatio);
            geometry.Y = (int)(Canvas.GetTop(cropTool) * yRatio);
            imageToSave.Crop(geometry);

            if (saveFileDialog.ShowDialog() == true)
            {
                if (File.Exists(saveFileDialog.FileName))
                    File.Delete(saveFileDialog.FileName);
                using (FileStream fs = new FileStream(saveFileDialog.FileName, FileMode.Create))
                {
                    imageToSave.Write(fs);
                }
            }
        }


        private void ResizeThumb_DragDelta(object sender, DragDeltaEventArgs e)
        {
            var senderControl = sender as Control;
            FrameworkElement item = senderControl.Parent as FrameworkElement;
            FrameworkElement parentItem = item.Parent as FrameworkElement;

            if (item != null)
            {
                double deltaVertical, deltaHorizontal;

                switch (senderControl.VerticalAlignment)
                {
                    case VerticalAlignment.Bottom:
                        //Drag the bottom side
                        deltaVertical = Math.Min(-e.VerticalChange,
                            item.ActualHeight - item.MinHeight);
                        double h = item.Height - deltaVertical;
                        //Don't go over image size
                        item.Height = Math.Min(parentItem.Height, h);
                        break;
                    case VerticalAlignment.Top:
                        //Drag the top side
                        deltaVertical = Math.Min(e.VerticalChange,
                            item.ActualHeight - item.MinHeight);
                        double top = Canvas.GetTop(item) + deltaVertical;
                        //Don't go over 0
                        if (top < 0)
                        {
                            deltaVertical += (0 - top);
                            top = 0;
                        }
                        Canvas.SetTop(item, top);
                        item.Height -= deltaVertical;
                        break;
                    default:
                        break;
                }

                switch (senderControl.HorizontalAlignment)
                {
                    case HorizontalAlignment.Left:
                        //Drag the left side
                        deltaHorizontal = Math.Min(e.HorizontalChange,
                            item.ActualWidth - item.MinWidth);
                        double left = Canvas.GetLeft(item) + deltaHorizontal;
                        //Don't go over 0
                        if (left < 0)
                        {
                            deltaHorizontal += (0 - left);
                            left = 0;
                        }
                        Canvas.SetLeft(item, left);
                        item.Width -= deltaHorizontal;
                        break;
                    case HorizontalAlignment.Right:
                        //Drag the right side
                        deltaHorizontal = Math.Min(-e.HorizontalChange,
                            item.ActualWidth - item.MinWidth);
                        double w = item.Width - deltaHorizontal;
                        //Don't go over image size
                        item.Width = Math.Min(parentItem.Width, w);
                        break;
                    default:
                        break;
                }

                // Resize the shade rect
                rectangleGeo2.Rect = new Rect(Canvas.GetLeft(item), Canvas.GetTop(item), item.Width, item.Height);
            }

            e.Handled = true;
        }
    }
}
