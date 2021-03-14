using Microsoft.Win32;
using System;
using System.Windows.Forms;

namespace ImageViewer
{
    public class SetDefaults
    {
        public static class FileAssociations
        {
            public const int SHCNE_ASSOCCHANGED = 0x8000000;
            public const int SHCNF_FLUSH = 0x1000;

            public static bool RegistryValueExists()
            {
                RegistryKey root = Registry.ClassesRoot.OpenSubKey(@"\ImageViewer", false);

                if (root == null)
                {
                    return false;
                }
                else
                {
                    return true;
                }
            }

            public static void SetAssociation()
            {
                string[] extensions = { ".bmp", ".gif", ".jpeg", ".jpg", ".png", ".tiff" };

                foreach (string ext in extensions)
                {
                    SetKeyValue(ext, "ImageViewer");
                }

                SetKeyValue("ImageViewer", "ImageFile");
                SetKeyValue(@"ImageViewer\DefaultIcon", "\"" + Application.ExecutablePath + "\"");
                SetKeyValue(@"ImageViewer\shell\open\command", "\"" + Application.ExecutablePath + "\" \"%1\"");

                NativeMethods.SHChangeNotify(SHCNE_ASSOCCHANGED, SHCNF_FLUSH, IntPtr.Zero, IntPtr.Zero);
            }

            private static void SetKeyValue(string keyPath, string value)
            {
                using (RegistryKey key = Registry.ClassesRoot.CreateSubKey(keyPath))
                {
                    if (key.GetValue(null) as string != value)
                    {
                        key.SetValue(null, value);
                    }
                }
            }
        }
    }
}
