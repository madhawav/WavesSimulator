using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace GraphicsCore
{
    /**
    * Based on http://richardssoftware.net/ SlimDX Tutorial. Code has been modified to support SharpDX. 
    * 
    * Contains some code directly copied from the below mentioned repository belonging to author of  http://richardssoftware.net/ SlimDX Tutorial.
    * Source: https://github.com/ericrrichards/dx11/blob/master/DX11/Core/Util.cs
    *
    * */
    public static class Util
    {
        private static byte[] _unmanagedStaging = new byte[1024];

        public static void ReleaseCom<T>(ref T x) where T : class,IDisposable
        {
            if (x != null)
            {
                x.Dispose();
                x = null;
            }

        }
        public static int LowWord(this int i)
        {
            return i & 0xFFFF;
        }
        public static int HighWord(this int i)
        {
            return (i >> 16) & 0xFFFF;
        }

        public static byte[] GetArray(object o, out int len)
        {
            Array.Clear(_unmanagedStaging, 0, _unmanagedStaging.Length);
            len = Marshal.SizeOf(o);
            if (len >= _unmanagedStaging.Length)
            {
                _unmanagedStaging = new byte[len];
            }
            var ptr = Marshal.AllocHGlobal(len);
            Marshal.StructureToPtr(o, ptr, true);
            Marshal.Copy(ptr, _unmanagedStaging, 0, len);
            Marshal.FreeHGlobal(ptr);
            return _unmanagedStaging;

        }

    }
}
