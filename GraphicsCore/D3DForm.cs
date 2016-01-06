using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GraphicsCore
{
    /**
    * Based on http://richardssoftware.net/ SlimDX Tutorial. Code has been modified to support SharpDX. 
    * 
    * Contains some code directly copied from the below mentioned repository belonging to author of  http://richardssoftware.net/ SlimDX Tutorial.
    * Source: https://github.com/ericrrichards/dx11/blob/master/DX11/Core/Controls/D3DForm.cs
    *
    **/
    public delegate bool MyWndProc(ref Message m);
    public class D3DForm : Form
    {
        public MyWndProc MyWndProc;
        protected override void WndProc(ref Message m)
        {
            if (MyWndProc != null)
            {
                if (MyWndProc(ref m)) return;
            }
            base.WndProc(ref m);
        }
    }
}
