using SharpDX;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace GraphicsCore
{
    /**
     * The structure used to pass vertex information to compute shader
     */
    [StructLayout(LayoutKind.Sequential)]
    public struct WaveSolution
    {
        public Vector4 Pos;
        public Vector4 Normal;
        public Vector4 Tangent;

        public override string ToString()
        {
            return Pos.X.ToString() + " " + Pos.Y.ToString() + " " + Pos.Z.ToString();
        }


        public WaveSolution(float x, float y, float z)
        {
            Pos = new Vector4(x, y, z, 1);
            Normal = new Vector4();
            Tangent = new Vector4();

        }

        public const int Stride = 48;
    }
}
