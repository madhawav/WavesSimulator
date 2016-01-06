using SharpDX.Direct2D1;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using SharpDX.WIC;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GraphicsCore
{
    /*
     * Source : http://www.gamedev.net/topic/668642-sharpdx-loading-texture-from-file/
     * Refer Comments Section
     */
    public static class TextureLoader
    {
        private static readonly ImagingFactory Imgfactory = new ImagingFactory();


        private static BitmapSource LoadBitmap(string filename)
        {
            var d = new BitmapDecoder(
                Imgfactory,
                filename,
                DecodeOptions.CacheOnDemand
                );

            var frame = d.GetFrame(0);

            var fconv = new FormatConverter(Imgfactory);

            fconv.Initialize(
                frame,
                SharpDX.WIC.PixelFormat.Format32bppPRGBA,
                BitmapDitherType.None, null,
                0.0, BitmapPaletteType.Custom);
            return fconv;
        }

        public static Texture2D CreateTex2DFromFile(SharpDX.Direct3D11.Device device, string filename)
        {
            var bSource = LoadBitmap(filename);
            return CreateTex2DFromBitmap(device, bSource);
        }

        public static Texture2D CreateTex2DFromBitmap(SharpDX.Direct3D11.Device device, BitmapSource bsource)
        {

            Texture2DDescription desc;
            desc.Width = bsource.Size.Width;
            desc.Height = bsource.Size.Height;
            desc.ArraySize = 1;
            desc.BindFlags = BindFlags.ShaderResource;
            desc.Usage = ResourceUsage.Default;
            desc.CpuAccessFlags = CpuAccessFlags.None;
            desc.Format = Format.R8G8B8A8_UNorm;
            desc.MipLevels = 1;
            desc.OptionFlags = ResourceOptionFlags.None;
            desc.SampleDescription.Count = 1;
            desc.SampleDescription.Quality = 0;

            var s = new SharpDX.DataStream(bsource.Size.Height * bsource.Size.Width * 4, true, true);
            bsource.CopyPixels(bsource.Size.Width * 4, s);

            var rect = new SharpDX.DataRectangle(s.DataPointer, bsource.Size.Width * 4);

            var t2D = new Texture2D(device, desc, rect);

            return t2D;
        }
    }
}
