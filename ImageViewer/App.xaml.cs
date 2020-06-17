using Microsoft.Shell;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
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
                        proc.StartInfo.Verb = "runas"; // run as admin
                        proc.Start();
                    }

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
