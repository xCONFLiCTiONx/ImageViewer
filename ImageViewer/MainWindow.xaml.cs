using Microsoft.VisualBasic.FileIO;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Security.Permissions;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Point = System.Windows.Point;

namespace ImageViewer
{
    public partial class MainWindow : Window
    {
        #region Variables

        string LastImage;
        FrameworkElement element;
        bool mediaPaused = false;
        private Point origin;
        private Point start;
        private string[] extensions = { ".bmp", ".gif", ".jpeg", ".jpg", ".png", ".tiff" };
        public static string[] Args = Environment.GetCommandLineArgs();
        List<string> ImageList = null;
        public string CurrentImage = null;
        FileSystemWatcher watcher;

        #endregion Variables

        #region Main Entry

        [PermissionSet(SecurityAction.Demand, Name = "FullTrust")]
        public MainWindow()
        {
            InitializeComponent();

            EventSubs(true);

            if (Args.Length > 1)
            {
                CurrentImage = Args[1];

                ImageFolderWatcher();

                BuildImageList();

                OpenImage();
            }
        }

        #endregion Main Entry

        #region FileSystemWatcher

        private void ImageFolderWatcher()
        {
            watcher = new FileSystemWatcher();
            watcher.Path = Path.GetDirectoryName(CurrentImage);

            watcher.NotifyFilter = NotifyFilters.LastAccess | NotifyFilters.LastWrite
               | NotifyFilters.FileName | NotifyFilters.DirectoryName;

            watcher.Filter = "*.*";

            watcher.Changed += new FileSystemEventHandler(OnChanged);
            watcher.Created += new FileSystemEventHandler(OnChanged);
            watcher.Deleted += new FileSystemEventHandler(OnChanged);
            watcher.Renamed += new RenamedEventHandler(OnChanged);

            watcher.EnableRaisingEvents = true;
        }

        private void OnChanged(object source, FileSystemEventArgs e)
        {
            FilterExtensions(e.FullPath);
        }

        private void OnRenamed(object source, RenamedEventArgs e)
        {
            FilterExtensions(e.FullPath);
        }

        private void FilterExtensions(string filename)
        {
            var ext = (Path.GetExtension(filename) ?? string.Empty).ToLower();

            if (extensions.Any(ext.Equals))
            {
                BuildImageList();
            }
        }

        #endregion FileSystemWatcher

        #region Image Control

        private void SetCurrentImage(bool Reverse)
        {
            try
            {
                bool ImageFound = false;
                foreach (string listItem in ImageList)
                {
                    if (Reverse)
                    {
                        if (ImageFound)
                        {
                            CurrentImage = ImageList[ImageList.Count - ImageList.IndexOf(listItem) - 1];
                            goto Finish;
                        }
                        else if (ImageList[ImageList.Count - ImageList.IndexOf(listItem) - 1] == CurrentImage)
                        {
                            LastImage = CurrentImage;
                            ImageFound = true;
                        }
                    }
                    else
                    {
                        if (ImageFound)
                        {
                            CurrentImage = listItem;
                            goto Finish;
                        }
                        else if (listItem == CurrentImage)
                        {
                            LastImage = CurrentImage;
                            ImageFound = true;
                        }
                    }
                }
            Finish:;
                OpenImage();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                throw;
            }
        }

        private void BuildImageList()
        {
            if (ImageList != null)
            {
                ImageList.Clear();
            }
            Func<string, object> convert = str =>
            {
                try { return int.Parse(str); }
                catch { return str; }
            };

            IOrderedEnumerable<string> sortedList = Directory.GetFiles(Path.GetDirectoryName(CurrentImage), "*.*").OrderBy(str => Regex.Split(str.Replace(" ", ""), "([0-9]+)").Select(convert),
                new EnumerableComparer<object>());

            ImageList = sortedList.Where(item => extensions.Contains(Path.GetExtension(item))).ToList();
        }

        public void OpenImage()
        {
            if (CurrentImage == LastImage) return;

            ResetZoom();

            using (FileStream mediaStream = new FileStream(CurrentImage, FileMode.Open, FileAccess.Read))
            {
                if (Path.GetExtension(CurrentImage) == ".gif")
                {
                    using (Image gif = Image.FromStream(stream: mediaStream, useEmbeddedColorManagement: false, validateImageData: false))
                    {
                        mediaPaused = false;

                        ImagePlayer.Source = null;

                        Uri imageUri = new Uri(CurrentImage);

                        GIFPlayer.Source = imageUri;
                        GIFPlayer.Width = gif.PhysicalDimension.Width;
                        GIFPlayer.Height = gif.PhysicalDimension.Height;

                        GIFPlayer.MediaEnded += GIFPlayer_MediaEnded;

                        GIFPlayer.Play();

                        element = GIFPlayer;
                    }
                }
                else
                {
                    GIFPlayer.MediaEnded -= GIFPlayer_MediaEnded;
                    GIFPlayer.Close();
                    GIFPlayer.Source = null;

                    Bitmap bitmap = new Bitmap(mediaStream);

                    var bitmapSource = Imaging.CreateBitmapSourceFromHBitmap(bitmap.GetHbitmap(), IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());

                    ImagePlayer.Source = bitmapSource;

                    bitmap.Dispose();

                    ImagePlayer.Width = ImagePlayer.Source.Width;
                    ImagePlayer.Height = ImagePlayer.Source.Height;

                    element = ImagePlayer;
                }
            }
        }

        private void GIFPlayer_MediaEnded(object sender, RoutedEventArgs e)
        {
            if (!mediaPaused)
            {
                GIFPlayer.Position = new TimeSpan(0, 0, 1);

                GIFPlayer.Play();
            }
        }

        #endregion Image Control

        #region Events

        protected override void OnContentRendered(EventArgs e)
        {
            base.OnContentRendered(e);

            BlurBehind.EnableBlur(mainWindow);

            element.RenderTransformOrigin = new Point(0.5, 0.5);
        }

        private void EventSubs(bool Startup)
        {
            if (Startup)
            {
                mainWindow.MouseWheel += MainWindow_MouseWheel;
                mainWindow.MouseRightButtonDown += delegate { ResetZoom(); };

                ZoomResetButton.Click += delegate { ResetZoom(); };

                // ImagePlayer
                ImagePlayer.MouseRightButtonDown += delegate { ResetZoom(); };
                ImagePlayer.MouseLeftButtonDown += ImagePlayer_MouseLeftButtonDown;
                ImagePlayer.MouseLeftButtonUp += ImagePlayer_MouseLeftButtonUp;
                ImagePlayer.MouseMove += ImagePlayer_MouseMove;

                ImagePlayer.MaxHeight = SystemParameters.PrimaryScreenHeight * 85 / 100;
                ImagePlayer.MaxWidth = SystemParameters.PrimaryScreenWidth * 85 / 100;

                // GIFPlayer
                GIFPlayer.MouseRightButtonDown += delegate { ResetZoom(); };
                GIFPlayer.MouseLeftButtonDown += GIFPlayer_MouseLeftButtonDown;
                GIFPlayer.MouseLeftButtonUp += GIFPlayer_MouseLeftButtonUp;
                GIFPlayer.MouseMove += GIFPlayer_MouseMove;

                GIFPlayer.MaxHeight = SystemParameters.PrimaryScreenHeight * 85 / 100;
                GIFPlayer.MaxWidth = SystemParameters.PrimaryScreenWidth * 85 / 100;
            }
            else
            {
                try
                {
                    mainWindow.MouseWheel -= MainWindow_MouseWheel;

                    // ImagePlayer
                    ImagePlayer.MouseLeftButtonDown -= ImagePlayer_MouseLeftButtonDown;
                    ImagePlayer.MouseLeftButtonUp -= ImagePlayer_MouseLeftButtonUp;
                    ImagePlayer.MouseMove -= ImagePlayer_MouseMove;

                    // GIFPlayer
                    GIFPlayer.MouseLeftButtonDown -= GIFPlayer_MouseLeftButtonDown;
                    GIFPlayer.MouseLeftButtonUp -= GIFPlayer_MouseLeftButtonUp;
                    GIFPlayer.MouseMove -= GIFPlayer_MouseMove;
                    GIFPlayer.MediaEnded -= GIFPlayer_MediaEnded;
                    GIFPlayer.Close();
                    GIFPlayer.Source = null;

                    // filewatcher
                    watcher.Changed -= new FileSystemEventHandler(OnChanged);
                    watcher.Created -= new FileSystemEventHandler(OnChanged);
                    watcher.Deleted -= new FileSystemEventHandler(OnChanged);
                    watcher.Renamed -= new RenamedEventHandler(OnChanged);
                }
                catch (Exception)
                {
                    // ignore
                }
            }
        }

        private void MainWindow_KeyDown(object sender, KeyEventArgs e)
        {
            base.OnKeyDown(e);
            switch (e.Key)
            {
                case Key.Left:
                    SetCurrentImage(true);
                    break;
                case Key.Right:
                    SetCurrentImage(false);
                    break;
                case Key.Up:
                    ZoomIn(false);
                    break;
                case Key.Down:
                    ZoomOut(false);
                    break;
                case Key.Space:
                    if (GIFPlayer.Source != null)
                    {
                        if (!mediaPaused)
                        {
                            mediaPaused = true;
                            GIFPlayer.Pause();
                        }
                        else
                        {
                            mediaPaused = false;
                            GIFPlayer.Play();
                        }
                    }
                    break;
            }
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string FileToDelete = CurrentImage;

                SetCurrentImage(false);

                if (CurrentImage == FileToDelete)
                {
                    SetCurrentImage(true);
                }

                FileSystem.DeleteFile(FileToDelete, UIOption.OnlyErrorDialogs, RecycleOption.SendToRecycleBin);

                BuildImageList();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "ImageViewer", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                throw;
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            EventSubs(false);

            Environment.Exit(0);
        }

        private void RotateButton_Click(object sender, RoutedEventArgs e)
        {
            watcher.EnableRaisingEvents = false;

            using (Image img = Image.FromFile(CurrentImage))
            {
                img.RotateFlip(RotateFlipType.Rotate90FlipNone);

                if (Path.GetExtension(CurrentImage).ToLower() == ".jpg")
                {
                    img.Save(CurrentImage, ImageFormat.Jpeg);
                }
                else if (Path.GetExtension(CurrentImage).ToLower() == ".png")
                {
                    img.Save(CurrentImage, ImageFormat.Png);
                }
                else if (Path.GetExtension(CurrentImage).ToLower() == ".gif")
                {
                    MessageBox.Show("This program cannot save gif files. If you want to rotate this image and save it, use a program that is capable of properly saving gif files.", "ImageViewer", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else if (Path.GetExtension(CurrentImage).ToLower() == ".bmp")
                {
                    img.Save(CurrentImage, ImageFormat.Bmp);
                }
                else if (Path.GetExtension(CurrentImage).ToLower() == ".ico")
                {
                    MessageBox.Show("This program cannot save icon files. If you want to rotate this image and save it, use a program that is capable of properly saving icon files.", "ImageViewer", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else if (Path.GetExtension(CurrentImage).ToLower() == ".tiff")
                {
                    img.Save(CurrentImage, ImageFormat.Tiff);
                }

                OpenImage();
            }

            watcher.EnableRaisingEvents = true;
        }

        // Zoom
        private void ZoomInButton_Click(object sender, RoutedEventArgs e)
        {
            ZoomIn(true);
        }

        private void ZoomOutButton_Click(object sender, RoutedEventArgs e)
        {
            ZoomOut(true);
        }

        private void ZoomIn(bool isButton)
        {
            if (element.TransformToAncestor(border)
                  .TransformBounds(new Rect(element.RenderSize)).Width >= 20000) return;

            Matrix m = element.RenderTransform.Value;
            
            if (isButton)
            {
                m.Scale(1.5, 1.5);
            }
            else
            {
                m.Scale(1.1, 1.1);
            }

            element.RenderTransform = new MatrixTransform(m);
        }

        private void ZoomOut(bool isButton)
        {
            if (element.TransformToAncestor(border)
          .TransformBounds(new Rect(element.RenderSize)).Width <= 200) return;

            Matrix m = element.RenderTransform.Value;

            if (isButton)
            {
                m.Scale(1 / 1.5, 1 / 1.5);
            }
            else
            {
                m.Scale(1 / 1.1, 1 / 1.1);
            }

            element.RenderTransform = new MatrixTransform(m);
        }

        private void ResetZoom()
        {
            if (GIFPlayer.Source == null)
            {
                Matrix m = ImagePlayer.RenderTransform.Value;
                m.SetIdentity();
                ImagePlayer.RenderTransform = new MatrixTransform(m);
            }
            else
            {
                Matrix m = GIFPlayer.RenderTransform.Value;
                m.SetIdentity();
                GIFPlayer.RenderTransform = new MatrixTransform(m);
            }
        }

        private void MainWindow_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            Matrix m = element.RenderTransform.Value;

            if (e.Delta > 0)
            {
                if (element.TransformToAncestor(border)
               .TransformBounds(new Rect(element.RenderSize)).Width >= 20000) return;

                m.Scale(1.1, 1.1);
            }
            else
            {
                if (element.TransformToAncestor(border)
       .TransformBounds(new Rect(element.RenderSize)).Width <= 200) return;
                
                m.Scale(1 / 1.1, 1 / 1.1);
            }

            element.RenderTransform = new MatrixTransform(m);
        }

        // ImagePlayer
        private void MouseLeftButtonDownElements(MouseButtonEventArgs e)
        {
            if (element.IsMouseCaptured) return;
            element.CaptureMouse();

            start = e.GetPosition(border);
            origin.X = element.RenderTransform.Value.OffsetX;
            origin.Y = element.RenderTransform.Value.OffsetY;
        }
        private void ImagePlayer_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            MouseLeftButtonDownElements(e);
        }

        private void MouseMoveElements(MouseEventArgs e)
        {
            if (!element.IsMouseCaptured) return;
            Point p = e.MouseDevice.GetPosition(border);

            Matrix m = element.RenderTransform.Value;
            m.OffsetX = origin.X + (p.X - start.X);
            m.OffsetY = origin.Y + (p.Y - start.Y);

            element.RenderTransform = new MatrixTransform(m);
        }

        private void ImagePlayer_MouseMove(object sender, MouseEventArgs e)
        {
            MouseMoveElements(e);
        }

        private void GIFPlayer_MouseMove(object sender, MouseEventArgs e)
        {
            MouseMoveElements(e);
        }

        private void ImagePlayer_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            element.ReleaseMouseCapture();
        }

        // GIFPlayer
        private void GIFPlayer_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            MouseLeftButtonDownElements(e);
        }

        private void GIFPlayer_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            element.ReleaseMouseCapture();
        }

        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true;

            EventSubs(false);

            Environment.Exit(0);
        }

        #endregion Events
    }
}