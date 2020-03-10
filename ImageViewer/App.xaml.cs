using Microsoft.Shell;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using static ImageViewer.Properties.Settings;

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
                if (AdminCheck.IsAdministrator())
                {
                    SetDefaults.FileAssociations.SetAssociation();
                }
                else
                {
                    MessageBox.Show("It appears that you are running ImageViewer directly. Please run as administrator if you are trying to set defaults.", "ImageViewer", MessageBoxButton.OK, MessageBoxImage.Information);

                    Environment.Exit(0);
                }
            }
            if (AdminCheck.IsAdministrator() && Default.SetDefaults)
            {
                SetDefaults.FileAssociations.SetAssociation();

                Default.SetDefaults = false;

                Default.Save();

                MessageBox.Show("ImageViewer is now set as a file handler for common images. Please set the Windows default image viewer also.", "ImageViewer", MessageBoxButton.OK, MessageBoxImage.Information);

                Process.Start("ms-settings:defaultapps");

                Environment.Exit(0);
            }
            if (Default.UpgradeRequired)
            {
                Default.Upgrade();

                Default.UpgradeRequired = false;

                if (Default.FirstRun)
                {
                    if (Default.FirstRun)
                    {
                        var results = MessageBox.Show("Would you like me to set this program as the default image viewer for common images?", "ImageViewer", MessageBoxButton.YesNo, MessageBoxImage.Information);
                        if (results == MessageBoxResult.Yes)
                        {
                            Default.SetDefaults = true;

                            ForceSetAssociation = true;
                        }
                        else
                        {
                            Default.SetDefaults = false;
                        }
                    }
                }

                Default.FirstRun = false;

                Default.Save();
            }

            if (Default.KeepDefaults || ForceSetAssociation)
            {
                SetAssociations();
            }

            int args = Environment.GetCommandLineArgs().Length;
            if (args == 1 && Default.SetDefaults)
            {
                MessageBox.Show("This program is meant to be opened by images with this app being the default handler for common image types.", "ImageViewer", MessageBoxButton.OK, MessageBoxImage.Information);

                Environment.Exit(0);
            }
            else if (args == 1)
            {
                var result = MessageBox.Show("This program is meant to be opened by images with this app being the default handler for common image types." + Environment.NewLine + Environment.NewLine + "Would you like me to set this program as the default image viewer for common images?", "ImageViewer", MessageBoxButton.YesNo, MessageBoxImage.Information);
                if (result == MessageBoxResult.Yes)
                {
                    Default.SetDefaults = true;

                    Default.Save();

                    SetAssociations();
                }

                Environment.Exit(0);
            }

            if (SingleInstance<App>.InitializeAsFirstInstance("ImageViewer"))
            {
                App app = new App();
                app.InitializeComponent();
                app.Run();

                SingleInstance<App>.Cleanup();
            }
        }

        private static void SetAssociations()
        {
            if (!SetDefaults.FileAssociations.RegistryValueExists())
            {
                if (AdminCheck.IsAdministrator())
                {
                    SetDefaults.FileAssociations.SetAssociation();
                }
                else
                {
                    using (Process process = new Process())
                    {
                        process.StartInfo.FileName = System.Windows.Forms.Application.ExecutablePath;
                        process.StartInfo.Verb = "runas";
                        process.Start();

                        Environment.Exit(0);
                    }
                }
            }
        }

        public bool SignalExternalCommandLineArgs(IList<string> args)
        {
            MainWindow mainWindow = this.MainWindow as MainWindow;

            if (args.Count > 1)
            {
                mainWindow.CurrentImage = args[1];
                mainWindow.OpenImage();
            }

            if (!mainWindow.IsActive)
            {
                mainWindow.WindowState = WindowState.Minimized;
                mainWindow.WindowState = WindowState.Maximized;
            }

            return true;
        }
    }
}
