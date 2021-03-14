using Microsoft.Win32;
using System.Windows.Forms;

namespace ImageViewer
{
    internal class CheckDefaults
    {
        internal static bool AssociationNeedSet()
        {
            string[] extensions = { ".bmp", ".gif", ".jpeg", ".jpg", ".png", ".tiff" };

            foreach (string ext in extensions)
            {
                if (ValuesExist(ext, "ImageViewer"))
                {
                    return false;
                }
            }

            if (ValuesExist("ImageViewer", "ImageFile"))
            {
                return false;
            }
            if (ValuesExist(@"ImageViewer\DefaultIcon", "\"" + Application.ExecutablePath + "\""))
            {
                return false;
            }
            if (ValuesExist(@"ImageViewer\shell\open\command", "\"" + Application.ExecutablePath + "\" \"%1\""))
            {
                return false;
            }

            return true;
        }

        internal static bool ValuesExist(string keyPath, string value)
        {
            using (RegistryKey key = Registry.ClassesRoot.CreateSubKey(keyPath))
            {
                if (key.GetValue(null) as string != value)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
