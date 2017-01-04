using System;
using System.IO;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace WallpaperSorter
{
    public class PreviewImage
    {
        public BitmapImage Image { get; set; }
        public String Name { get; set; }
    }
    /// <summary>
    /// Interaction logic for TagPicker.xaml
    /// </summary>
    public partial class TagPicker : Window
    {
        public List<PreviewImage> ImagePreviews { get; set; }

        public int ImageColumns
        {
            get
            {
                int output = (int)Math.Ceiling(Math.Sqrt(ImagePreviews.Count));
                return output;
            }
        }

        public int ImageRows
        {
            get
            {
                int output = (int)Math.Ceiling(((double)ImagePreviews.Count) / ((double)ImageColumns));
                return output;
            }
        }

        public TagPicker(String[] secondaryTags, List<System.IO.FileInfo> previewImages)
        {
            InitializeComponent();

            this.secondayTagListBox.ItemsSource = secondaryTags;
            if (previewImages != null)
            {
                ImagePreviews = new List<PreviewImage>();
                int i = 0;
                foreach (FileInfo previewImage in previewImages)
                {
                    if (i == 16)
                        break;
                    try {
                        previewImage.Refresh();
                        if (previewImage.Exists) {
                            BitmapImage image = new BitmapImage();
                            image.BeginInit();
                            image.UriSource = new Uri(previewImage.FullName);
                            image.CacheOption = BitmapCacheOption.OnLoad;
                            image.EndInit();
                            image.Freeze();

                            if (image.Width > 2000) {
                                image = CreateThumbnail(image);
                            }

                            PreviewImage img = new PreviewImage();
                            img.Image = image;
                            img.Name = previewImage.Name;
                            this.ImagePreviews.Add(img);
                        }
                        i++;
                    } catch { }
                }
            }

            previewImageItems.DataContext = this;
        }

        private Bitmap BitmapImage2Bitmap(BitmapImage bitmapImage)
        {
            // BitmapImage bitmapImage = new BitmapImage(new Uri("../Images/test.png", UriKind.Relative));

            using (MemoryStream outStream = new MemoryStream())
            {
                BitmapEncoder enc = new BmpBitmapEncoder();
                enc.Frames.Add(BitmapFrame.Create(bitmapImage));
                enc.Save(outStream);
                System.Drawing.Bitmap bitmap = new System.Drawing.Bitmap(outStream);

                return new Bitmap(bitmap);
            }
        }

        private BitmapImage CreateThumbnail(BitmapImage image)
        {
                int width = 2000;

                double old_height = image.Height;
                double old_width = image.Width;
                double ratio = width / old_width;
                int new_height = (int)Math.Round(ratio * image.Height);

                using (System.Drawing.Image thumbNail =
                    new Bitmap(width, new_height, System.Drawing.Imaging.PixelFormat.Format32bppPArgb))
                {
                    using (Graphics g = Graphics.FromImage(thumbNail))
                    {
                        g.CompositingQuality = CompositingQuality.HighQuality;
                        g.SmoothingMode = SmoothingMode.HighQuality;
                        g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                        System.Drawing.Rectangle rect = new System.Drawing.Rectangle(0, 0, width, new_height);
                        g.DrawImage(BitmapImage2Bitmap(image), rect);
                    }
                    using (MemoryStream ms = new MemoryStream())
                    {
                        thumbNail.Save(ms, System.Drawing.Imaging.ImageFormat.Png);


                        ms.Seek(0, SeekOrigin.Begin);
                        BitmapImage bi = new BitmapImage();
                        bi.BeginInit();
                        bi.CacheOption = BitmapCacheOption.OnLoad;
                        bi.StreamSource = ms;
                        bi.EndInit();
                        bi.Freeze();
                        return bi;
                    }
                }

        }


        public String SelectedTag
        {
            get
            {
                if (listRadio.IsChecked.GetValueOrDefault(false))
                {
                    if (this.secondayTagListBox.SelectedItem == null)
                        return String.Empty;
                    return this.secondayTagListBox.SelectedItem.ToString();
                }else if (customRadio.IsChecked.GetValueOrDefault(false))
                {
                    if (string.IsNullOrWhiteSpace(customText.Text))
                        return String.Empty;
                    return customText.Text.Trim();
                }
                throw new Exception("Nor adio box selected");
            }
        }


        private void button_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
            this.Close();
        }

        private void secondayTagListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            listRadio.IsChecked = true;
        }

        private void customText_TextChanged(object sender, TextChangedEventArgs e)
        {
            if(!String.IsNullOrWhiteSpace(customText.Text))
            customRadio.IsChecked = true;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            this.previewImageItems.DataContext = null;
            this.ImagePreviews.Clear();
        }

        private void image_MouseDown(object sender, MouseButtonEventArgs e)
        {
            System.Windows.Controls.Image imageControl = (System.Windows.Controls.Image)sender;
            fullImage.Source = imageControl.Source;
            fullImage.ToolTip = imageControl.ToolTip;
            previewImageItems.Visibility = Visibility.Hidden;
            fullImage.Visibility = Visibility.Visible;
        }

        private void fullImage_MouseDown(object sender, MouseButtonEventArgs e)
        {
            previewImageItems.Visibility = Visibility.Visible;
            fullImage.Visibility = Visibility.Hidden;

        }
    }
}
