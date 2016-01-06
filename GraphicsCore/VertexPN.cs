using SharpDX;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

using SharpDX;

using D3D11 = SharpDX.Direct3D11;
using DXGI = SharpDX.DXGI;
namespace GraphicsCore
{
    /**
    * Based on http://richardssoftware.net/ SlimDX Tutorial. Code has been modified to support SharpDX. 
    * 
    * Contains code directly copied from the below mentioned repository belonging to author of  http://richardssoftware.net/ SlimDX Tutorial.
    * Source: https://github.com/ericrrichards/dx11/blob/master/DX11/Core/Vertex.cs
    *
    **/

    [StructLayout(LayoutKind.Sequential)]
    public struct VertexPN
    {
        public Vector3 Position;
        public Vector3 Normal;
        public Vector2 Text;
        public VertexPN(Vector3 position, Vector3 normal, Vector2 text)
        {
            Position = position;
            Normal = normal;
            Text = text;
        }

        public const int Stride = 32;


        public static D3D11.InputElement[] GetVertexDescription()
        {
            var vertexDesc = new[] {
                new D3D11.InputElement("POSITION", 0, DXGI.Format.R32G32B32_Float, 
                    0, 0, D3D11.InputClassification.PerVertexData, 0),
                new D3D11.InputElement("NORMAL", 0, DXGI.Format.R32G32B32_Float, 
                    12, 0, D3D11.InputClassification.PerVertexData, 0),
                new D3D11.InputElement("TEXCOORD", 0,DXGI.Format.R32G32_Float,24,0,D3D11.InputClassification.PerVertexData,0)
                 
            };

            return vertexDesc;
        }
    }
}
