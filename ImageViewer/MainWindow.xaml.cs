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
    public partial class MainWindow : Window, IDisposable
    {
        #region Variables

        string LastImage;
        FrameworkElement element;
        bool mediaPaused = false;
        private Point origin;
        private Point start;
        private readonly string[] extensions = { ".bmp", ".gif", ".jpeg", ".jpg", ".png", ".tiff", ".webp" };
        public static string[] Args = Environment.GetCommandLineArgs();
        List<string> ImageList = new List<string>();
        public string CurrentImage = null;
        FileSystemWatcher watcher;
        FileStream mediaStream;
        Bitmap bitmap;
        BitmapSource bitmapSource;

        #endregion Variables

        #region Main Entry

        [PermissionSet(SecurityAction.Demand, Name = "FullTrust")]
        public MainWindow()
        {
            InitializeComponent();

            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            Application.Current.DispatcherUnhandledException += Current_DispatcherUnhandledException;

            mainWindow.MaxHeight = (SystemParameters.PrimaryScreenHeight);
            mainWindow.MaxWidth = (SystemParameters.PrimaryScreenWidth);
            mainWindow.Height = mainWindow.MaxHeight;
            mainWindow.Width = mainWindow.MaxWidth;

            mainWindow.Top = 0;
            mainWindow.Left = 0;

            EventSubs(true);

            if (Args.Length > 1)
            {
                CurrentImage = Args[1];

                ImageFolderWatcher();

                BuildImageList();
            }
        }

        private void Current_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            MessageBox.Show(e.Exception.Message);
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            MessageBox.Show(e.ExceptionObject.ToString());
        }

        #endregion Main Entry

        #region FileSystemWatcher

        private void ImageFolderWatcher()
        {
            watcher = new FileSystemWatcher
            {
                Path = Path.GetDirectoryName(CurrentImage),

                NotifyFilter = NotifyFilters.LastAccess | NotifyFilters.LastWrite
               | NotifyFilters.FileName | NotifyFilters.DirectoryName,

                Filter = "*.*"
            };

            watcher.Changed += new FileSystemEventHandler(OnChanged);
            watcher.Created += new FileSystemEventHandler(OnChanged);
            watcher.Deleted += new FileSystemEventHandler(OnChanged);
            watcher.Renamed += new RenamedEventHandler(OnChanged);

            watcher.EnableRaisingEvents = true;
        }

        private void OnChanged(object source, FileSystemEventArgs e)
        {
            var ext = (Path.GetExtension(e.FullPath) ?? string.Empty).ToLower();

            if (extensions.Any(ext.Equals))
            {
                Dispatcher.Invoke(() => SetCurrentImage());

                if (CurrentImage == e.FullPath)
                {
                    Dispatcher.Invoke(() => SetCurrentImage(true));
                }

                Dispatcher.Invoke(() => BuildImageList());
            }
        }

        #endregion FileSystemWatcher

        #region Image Control

        internal void SetCurrentImage(bool Reverse = false)
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

        internal void BuildImageList()
        {
            if (ImageList != null)
            {
                ImageList.Clear();
            }

            object convert(string str)
            {
                try { return int.Parse(str); }
                catch { return str; }
            }

            IOrderedEnumerable<string> sortedList = Directory.GetFiles(Path.GetDirectoryName(CurrentImage), "*.*").OrderBy(str => Regex.Split(str.Replace(" ", ""), "([0-9]+)").Select(convert),
                new EnumerableComparer<object>());

            ImageList = sortedList.Where(item => extensions.Contains(Path.GetExtension(item))).ToList();

            OpenImage();
        }

        public void OpenImage()
        {
            if (ImageList.Count == 0)
            {
                Application.Current.Shutdown();
            }

            if (CurrentImage == LastImage) return;

            ResetZoom();

            using (mediaStream = new FileStream(CurrentImage, FileMode.Open, FileAccess.Read))
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

                    bitmap = new Bitmap(mediaStream);

                    bitmapSource = Imaging.CreateBitmapSourceFromHBitmap(bitmap.GetHbitmap(), IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());

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

        private void LeftButton_MouseDown(object sender, MouseButtonEventArgs e)
        {
            SetCurrentImage(true);
        }

        private void RightButton_MouseDown(object sender, MouseButtonEventArgs e)
        {
            SetCurrentImage();
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
                PreviewMouseDown += MainWindow_PreviewMouseDown;

                mainWindow.MouseWheel += MainWindow_MouseWheel;
                mainWindow.MouseRightButtonDown += delegate { ResetZoom(); };

                ZoomResetButton.Click += delegate { ResetZoom(); };

                // ImagePlayer
                ImagePlayer.MouseRightButtonDown += delegate { ResetZoom(); };
                ImagePlayer.MouseLeftButtonDown += ImagePlayer_MouseLeftButtonDown;
                ImagePlayer.MouseLeftButtonUp += ImagePlayer_MouseLeftButtonUp;
                ImagePlayer.MouseMove += ImagePlayer_MouseMove;

                ImagePlayer.MaxHeight = SystemParameters.MaximizedPrimaryScreenHeight * 85 / 100;
                ImagePlayer.MaxWidth = SystemParameters.MaximizedPrimaryScreenWidth * 85 / 100;

                // GIFPlayer
                GIFPlayer.MouseRightButtonDown += delegate { ResetZoom(); };
                GIFPlayer.MouseLeftButtonDown += GIFPlayer_MouseLeftButtonDown;
                GIFPlayer.MouseLeftButtonUp += GIFPlayer_MouseLeftButtonUp;
                GIFPlayer.MouseMove += GIFPlayer_MouseMove;

                GIFPlayer.MaxHeight = SystemParameters.MaximizedPrimaryScreenHeight * 85 / 100;
                GIFPlayer.MaxWidth = SystemParameters.MaximizedPrimaryScreenWidth * 85 / 100;
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
                    SetCurrentImage();
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
                watcher.EnableRaisingEvents = false;

                string FileToDelete = CurrentImage;

                SetCurrentImage();

                if (CurrentImage == FileToDelete)
                {
                    SetCurrentImage(true);
                }

                FileSystem.DeleteFile(FileToDelete, UIOption.OnlyErrorDialogs, RecycleOption.SendToRecycleBin);

                BuildImageList();

                watcher.EnableRaisingEvents = true;
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
            this.Opacity = 0;
            this.Hide();
            this.ShowInTaskbar = false;
            Application.Current.Shutdown();
        }

        private void RotateButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                watcher.EnableRaisingEvents = false;

                Bitmap img = (Bitmap)Image.FromFile(CurrentImage);

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
                else if (Path.GetExtension(CurrentImage).ToLower() == ".webp")
                {
                    MessageBox.Show("This program cannot save webp files. If you want to rotate this image and save it, use a program that is capable of properly saving webp files.", "ImageViewer", MessageBoxButton.OK, MessageBoxImage.Information);
                }

                OpenImage();
            }
            catch
            {
                MessageBox.Show("An error occurred. Please make sure that the file isn't set to Read-only.", "ImageViewer", MessageBoxButton.OK, MessageBoxImage.Warning);
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
                m.Scale(1.2, 1.2);
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
          .TransformBounds(new Rect(element.RenderSize)).Width <= 50) return;

            Matrix m = element.RenderTransform.Value;

            if (isButton)
            {
                m.Scale(1 / 1.2, 1 / 1.2);
            }
            else
            {
                m.Scale(1 / 1.1, 1 / 1.1);
            }

            element.RenderTransform = new MatrixTransform(m);
        }

        private void MainWindow_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Middle)
            {
                ResetZoom();
            }
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
       .TransformBounds(new Rect(element.RenderSize)).Width <= 50) return;

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

            Application.Current.Shutdown();
        }

        #endregion Events

        #region Dispose
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (watcher != null)
                    watcher.Dispose();
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion Dispose
    }
}