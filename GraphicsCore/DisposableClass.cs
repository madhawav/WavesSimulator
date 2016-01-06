using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GraphicsCore
{
    /**
    * Based on http://richardssoftware.net/ SlimDX Tutorial. Code has been modified to support SharpDX. 
    * 
    * Contains some code directly copied from the below mentioned repository belonging to author of  http://richardssoftware.net/ SlimDX Tutorial.
    * Source: https://github.com/ericrrichards/dx11/blob/master/DX11/Core/DisposableClass.cs
    *
    * */
    public class DisposableClass : IDisposable
    {
        private bool _disposed;
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~DisposableClass()
        {
            Dispose(false);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed) return;
            if (disposing)
            {
                // free IDisposable objects
            }
            // release unmanaged objects
            _disposed = true;

        }
    }
}
