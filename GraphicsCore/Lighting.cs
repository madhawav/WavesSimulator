using SharpDX;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

/**  
 * Code directly copied from the below mentioned repository belonging to author of  http://richardssoftware.net/ SlimDX Tutorial.
 * Sources: https://github.com/ericrrichards/dx11/blob/master/DX11/Core/Lights.cs
 */
namespace GraphicsCore
{

    [StructLayout(LayoutKind.Sequential)]
    public struct Material
    {
        public Color4 Ambient;
        public Color4 Diffuse;
        public Color4 Specular;
        public Color4 Reflect;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct DirectionalLight
    {
        public Color4 Ambient;
        public Color4 Diffuse;
        public Color4 Specular;
        public Vector3 Direction;
        public float Pad;
    }
    [StructLayout(LayoutKind.Sequential)]
    public struct PointLight
    {
        public Color4 Ambient;
        public Color4 Diffuse;
        public Color4 Specular;
        public Vector3 Position;
        public float Range;
        public Vector3 Attenuation;
        public float Pad;
    }
    [StructLayout(LayoutKind.Sequential)]
    public struct SpotLight
    {
        public Color4 Ambient;
        public Color4 Diffuse;
        public Color4 Specular;
        public Vector3 Position;
        public float Range;
        public Vector3 Direction;
        public float Spot;
        public Vector3 Attenuation;
        public float Pad;
    }
}
