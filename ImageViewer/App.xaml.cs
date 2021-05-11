using Microsoft.Shell;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;

namespace ImageViewer
{
    public partial class App : Application, ISingleInstanceApp
    {
        public static bool ForceSetAssociation = false;

        [STAThread]
        public static void Main()
        {
            if (Environment.GetCommandLineArgs().Length == 1)
            {
                if (!CheckDefaults.AssociationNeedSet())
                {
                    if (AdminCheck.IsAdministrator())
                    {
                        SetDefaults.FileAssociations.SetAssociation();

                        Process.Start("ms-settings:defaultapps");

                        Environment.Exit(0);
                    }
                    else
                    {
                        using (Process proc = new Process())
                        {
                            proc.StartInfo.FileName = Assembly.GetEntryAssembly().Location;
                            proc.StartInfo.Verb = "runas";
                            proc.Start();
                        }

                        Environment.Exit(0);
                    }
                }
                else
                {
                    MessageBox.Show("Imageviwer is already associated with the common image types and ready to use. This program is not meant to be run directly. To use ImageViwer open file explorer and find an image and open the image directly. If the image is a common file type ImageViwer will open the image.", "ImageViewer", MessageBoxButton.OK, MessageBoxImage.Information);

                    Environment.Exit(0);
                }
            }

            if (SingleInstance<App>.InitializeAsFirstInstance("ImageViewer"))
            {
                App app = new App();
                app.InitializeComponent();

                app.Run();
                SingleInstance<App>.Cleanup();
            }
        }

        public bool SignalExternalCommandLineArgs(IList<string> args)
        {
            MainWindow mainWindow = MainWindow as MainWindow;

            if (args.Count > 1)
            {
                Dispatcher.Invoke(() => mainWindow.CurrentImage = args[1]);
                Dispatcher.Invoke(() => mainWindow.ImageFolderWatcher());
                Dispatcher.Invoke(() => mainWindow.BuildImageList());
            }

            if (!mainWindow.IsActive)
            {
                mainWindow.WindowState = WindowState.Minimized;
                mainWindow.WindowState = WindowState.Normal;
            }

            return true;
        }
    }
}
