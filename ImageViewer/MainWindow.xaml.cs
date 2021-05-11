using Microsoft.VisualBasic.FileIO;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security;
using System.Security.Permissions;
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

        private string LastImage;
        private FrameworkElement element;
        private bool mediaPaused = false;
        private Point origin;
        private Point start;
        private readonly string[] extensions = { ".bmp", ".gif", ".jpeg", ".jpg", ".jpe", ".jif", ".jfif", ".jfi", ".png", ".tiff", ".tif", ".webp" };
        private List<string> ImageList = new List<string>();
        public string CurrentImage = null;
        private FileSystemWatcher watcher;
        private FileStream mediaStream;
        private Bitmap bitmap;
        private BitmapSource bitmapSource;

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

            if (Environment.GetCommandLineArgs().Length > 1)
            {
                CurrentImage = Environment.GetCommandLineArgs()[1];

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

        internal void ImageFolderWatcher()
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
            string ext = (Path.GetExtension(e.FullPath) ?? string.Empty).ToLower();

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

        public static int CompareNatural(string strA, string strB)
        {
            return CompareNatural(strA, strB, CultureInfo.CurrentCulture, CompareOptions.IgnoreCase);
        }

        public static int CompareNatural(string strA, string strB, CultureInfo culture, CompareOptions options)
        {
            CompareInfo cmp = culture.CompareInfo;
            int iA = 0;
            int iB = 0;
            int softResult = 0;
            int softResultWeight = 0;
            while (iA < strA.Length && iB < strB.Length)
            {
                bool isDigitA = char.IsDigit(strA[iA]);
                bool isDigitB = char.IsDigit(strB[iB]);
                if (isDigitA != isDigitB)
                {
                    return cmp.Compare(strA, iA, strB, iB, options);
                }
                else if (!isDigitA && !isDigitB)
                {
                    int jA = iA + 1;
                    int jB = iB + 1;
                    while (jA < strA.Length && !char.IsDigit(strA[jA]))
                    {
                        jA++;
                    }

                    while (jB < strB.Length && !char.IsDigit(strB[jB]))
                    {
                        jB++;
                    }

                    int cmpResult = cmp.Compare(strA, iA, jA - iA, strB, iB, jB - iB, options);
                    if (cmpResult != 0)
                    {
                        // Certain strings may be considered different due to "soft" differences that are
                        // ignored if more significant differences follow, e.g. a hyphen only affects the
                        // comparison if no other differences follow
                        string sectionA = strA.Substring(iA, jA - iA);
                        string sectionB = strB.Substring(iB, jB - iB);
                        if (cmp.Compare(sectionA + "1", sectionB + "2", options) ==
                            cmp.Compare(sectionA + "2", sectionB + "1", options))
                        {
                            return cmp.Compare(strA, iA, strB, iB, options);
                        }
                        else if (softResultWeight < 1)
                        {
                            softResult = cmpResult;
                            softResultWeight = 1;
                        }
                    }
                    iA = jA;
                    iB = jB;
                }
                else
                {
                    char zeroA = (char)(strA[iA] - (int)char.GetNumericValue(strA[iA]));
                    char zeroB = (char)(strB[iB] - (int)char.GetNumericValue(strB[iB]));
                    int jA = iA;
                    int jB = iB;
                    while (jA < strA.Length && strA[jA] == zeroA)
                    {
                        jA++;
                    }

                    while (jB < strB.Length && strB[jB] == zeroB)
                    {
                        jB++;
                    }

                    int resultIfSameLength = 0;
                    do
                    {
                        isDigitA = jA < strA.Length && char.IsDigit(strA[jA]);
                        isDigitB = jB < strB.Length && char.IsDigit(strB[jB]);
                        int numA = isDigitA ? (int)char.GetNumericValue(strA[jA]) : 0;
                        int numB = isDigitB ? (int)char.GetNumericValue(strB[jB]) : 0;
                        if (isDigitA && (char)(strA[jA] - numA) != zeroA)
                        {
                            isDigitA = false;
                        }

                        if (isDigitB && (char)(strB[jB] - numB) != zeroB)
                        {
                            isDigitB = false;
                        }

                        if (isDigitA && isDigitB)
                        {
                            if (numA != numB && resultIfSameLength == 0)
                            {
                                resultIfSameLength = numA < numB ? -1 : 1;
                            }
                            jA++;
                            jB++;
                        }
                    }
                    while (isDigitA && isDigitB);
                    if (isDigitA != isDigitB)
                    {
                        // One number has more digits than the other (ignoring leading zeros) - the longer
                        // number must be larger
                        return isDigitA ? 1 : -1;
                    }
                    else if (resultIfSameLength != 0)
                    {
                        // Both numbers are the same length (ignoring leading zeros) and at least one of
                        // the digits differed - the first difference determines the result
                        return resultIfSameLength;
                    }
                    int lA = jA - iA;
                    int lB = jB - iB;
                    if (lA != lB)
                    {
                        // Both numbers are equivalent but one has more leading zeros
                        return lA > lB ? -1 : 1;
                    }
                    else if (zeroA != zeroB && softResultWeight < 2)
                    {
                        softResult = cmp.Compare(strA, iA, 1, strB, iB, 1, options);
                        softResultWeight = 2;
                    }
                    iA = jA;
                    iB = jB;
                }
            }
            if (iA < strA.Length || iB < strB.Length)
            {
                return iA < strA.Length ? 1 : -1;
            }
            else if (softResult != 0)
            {
                return softResult;
            }
            return 0;
        }

        internal void BuildImageList()
        {
            try
            {
                if (ImageList != null)
                {
                    ImageList.Clear();
                }

                string[] files = Directory.GetFiles(Path.GetDirectoryName(CurrentImage));
                Array.Sort(files, CompareNatural);

                ImageList = files.Where(item => extensions.Contains(Path.GetExtension(item).ToLower())).ToList();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                MessageBox.Show(ex.InnerException.Message);
                throw;
            }

            OpenImage();
        }

        public void OpenImage()
        {
            if (ImageList.Count == 0)
            {
                Application.Current.Shutdown();
            }

            if (CurrentImage == LastImage)
            {
                return;
            }

            ResetZoom();

            using (mediaStream = new FileStream(CurrentImage, FileMode.Open, FileAccess.Read, FileShare.None, 32767, true))
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
            Opacity = 0;
            Hide();
            ShowInTaskbar = false;
            Application.Current.Shutdown();
        }

        private ImageCodecInfo GetEncoder(ImageFormat format)
        {
            ImageCodecInfo[] codecs = ImageCodecInfo.GetImageDecoders();
            foreach (ImageCodecInfo codec in codecs)
            {
                if (codec.FormatID == format.Guid)
                {
                    return codec;
                }
            }
            return null;
        }

        private void RotateButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                watcher.EnableRaisingEvents = false;

                Bitmap img = (Bitmap)Image.FromFile(CurrentImage);

                img.RotateFlip(RotateFlipType.Rotate90FlipNone);

                Encoder myEncoder = Encoder.Quality;

                EncoderParameters myEncoderParameters = new EncoderParameters(1);

                EncoderParameter myEncoderParameter = new EncoderParameter(myEncoder, 90L);
                myEncoderParameters.Param[0] = myEncoderParameter;

                string _currentImage = Path.GetExtension(CurrentImage).ToLower();

                if (_currentImage == ".jpg" || _currentImage == ".jpeg" || _currentImage == ".jpe" || _currentImage == ".jif" || _currentImage == ".jfif" || _currentImage == ".jfi")
                {
                    img.Save(CurrentImage, GetEncoder(ImageFormat.Jpeg), myEncoderParameters);
                }
                else if (_currentImage == ".png")
                {
                    img.Save(CurrentImage, GetEncoder(ImageFormat.Png), myEncoderParameters);
                }
                else if (_currentImage == ".gif")
                {
                    MessageBox.Show("This program cannot save gif files. If you want to rotate this image and save it, use a program that is capable of properly saving gif files.", "ImageViewer", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else if (_currentImage == ".bmp")
                {
                    img.Save(CurrentImage, GetEncoder(ImageFormat.Bmp), myEncoderParameters);
                }
                else if (_currentImage == ".ico")
                {
                    MessageBox.Show("This program cannot save icon files. If you want to rotate this image and save it, use a program that is capable of properly saving icon files.", "ImageViewer", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else if (_currentImage == ".tiff" || _currentImage == ".tif")
                {
                    img.Save(CurrentImage, GetEncoder(ImageFormat.Tiff), myEncoderParameters);
                }
                else if (_currentImage == ".webp")
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
                  .TransformBounds(new Rect(element.RenderSize)).Width >= 20000)
            {
                return;
            }

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
          .TransformBounds(new Rect(element.RenderSize)).Width <= 50)
            {
                return;
            }

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
               .TransformBounds(new Rect(element.RenderSize)).Width >= 20000)
                {
                    return;
                }

                m.Scale(1.1, 1.1);
            }
            else
            {
                if (element.TransformToAncestor(border)
       .TransformBounds(new Rect(element.RenderSize)).Width <= 50)
                {
                    return;
                }

                m.Scale(1 / 1.1, 1 / 1.1);
            }

            element.RenderTransform = new MatrixTransform(m);
        }

        // ImagePlayer
        private void MouseLeftButtonDownElements(MouseButtonEventArgs e)
        {
            if (element.IsMouseCaptured)
            {
                return;
            }

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
            if (!element.IsMouseCaptured)
            {
                return;
            }

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
                {
                    watcher.Dispose();
                }
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