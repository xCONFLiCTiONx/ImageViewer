using Microsoft.Win32;
using System.Windows.Forms;

namespace ImageViewer
{
    internal class CheckDefaults
    {
        internal static bool AssociationNeedSet()
        {
            try
            {
                if (ValuesExist(@"Software\ImageViewer", @"xCONFLiCTiONx\ImageViewer"))
                {
                    return true;
                }
            }
            catch (System.Exception ex)
            {
                MessageBox.Show(ex.Message);
            }

            return false;
        }

        internal static bool ValuesExist(string keyPath, string value)
        {
            using (RegistryKey key = Registry.CurrentUser.CreateSubKey(keyPath))
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
