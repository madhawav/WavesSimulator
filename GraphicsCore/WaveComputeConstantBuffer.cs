using SharpDX;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace GraphicsCore
{
    [StructLayout(LayoutKind.Sequential)]
    public struct WaveComputeConstantBuffer
    {
        public uint RowCount;
        public uint ColumnCount;
        public float _spatialStep;
        public float dummy;
        public Vector4 k;
    }

}
