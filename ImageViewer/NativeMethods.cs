using System;
using System.Runtime.InteropServices;
using static BlurBehind;

namespace ImageViewer
{
    internal static class NativeMethods
    {
        [DllImport("Shell32.dll")]
        internal static extern int SHChangeNotify(int eventId, int flags, IntPtr item1, IntPtr item2);

        [DllImport("user32.dll")]
        internal static extern int SetWindowCompositionAttribute(IntPtr hwnd, ref WindowCompositionAttributeData data);
    }
}
