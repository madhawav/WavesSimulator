using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GraphicsCore
{
    using SharpDX;
    using D3D = SharpDX.Direct3D;
    using D3D11 = SharpDX.Direct3D11;
    using DXGI = SharpDX.DXGI;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using System.Windows.Forms;
    using SharpDX.Direct2D1;
    using System.Diagnostics;


    /**
     * Based on http://richardssoftware.net/ SlimDX Tutorial. Code has been modified to support SharpDX. 
     * 
     * Contains some code directly copied from the below mentioned repository belonging to author of  http://richardssoftware.net/ SlimDX Tutorial.
     * Source: https://github.com/ericrrichards/dx11/blob/master/DX11/Core/D3DApp.cs
     *
     */
    public class D3DApp : DisposableClass
    {
        public static D3DApp GD3DApp;
        private bool _disposed;


        public Form Window { get; protected set; }
        public IntPtr AppInst { get; protected set; }
        public float AspectRatio { get { return (float)ClientWidth / ClientHeight; } }
        public bool GammaCorrectedBackBuffer { get; set; }

        protected D3DApp(IntPtr hInstance)
        {
            AppInst = hInstance;
            MainWindowCaption = "D3D11 Application";
            DriverType = D3D.DriverType.Hardware;
            ClientWidth = 800;
            ClientHeight = 600;
            Enable4XMsaa = false;
            Window = null;
            AppPaused = false;
            Minimized = false;
            Maximized = false;
            Resizing = false;
            Msaa4XQuality = 0;
            Device = null;
            ImmediateContext = null;
            SwapChain = null;
            DepthStencilBuffer = null;
            RenderTargetView = null;
            DepthStencilView = null;
            Viewport = new Viewport();
            Timer = new GameTimer();

            GD3DApp = this;
        }

        protected override void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {

                    Util.ReleaseCom(ref RenderTargetView);
                    Util.ReleaseCom(ref DepthStencilView);


                    Util.ReleaseCom(ref DepthStencilBuffer);
                    if (ImmediateContext != null)
                    {
                        ImmediateContext.ClearState();
                    }

                    if (SwapChain != null)
                    {
                        if (SwapChain.IsFullScreen)
                        {
                            SwapChain.IsFullScreen = false;
                        }
                    }

                    Util.ReleaseCom(ref SwapChain);
                    Util.ReleaseCom(ref ImmediateContext);
                    Util.ReleaseCom(ref Device);

                }
                _disposed = true;
            }
            base.Dispose(disposing);
        }

        protected bool InitMainWindow()
        {
            try
            {
                Window = new D3DForm
                {
                    Text = MainWindowCaption,
                    Name = "D3DWndClassName",
                    FormBorderStyle = FormBorderStyle.Sizable,
                    ClientSize = new System.Drawing.Size(ClientWidth, ClientHeight),
                    StartPosition = FormStartPosition.CenterScreen,
                    MyWndProc = WndProc,
                    MinimumSize = new System.Drawing.Size(200, 200),
                };
                Window.MouseDown += OnMouseDown;
                Window.MouseUp += OnMouseUp;
                Window.MouseMove += OnMouseMove;
                Window.ResizeBegin += (sender, args) =>
                {
                    AppPaused = true;
                    Resizing = true;
                    Timer.Stop();
                };
                Window.ResizeEnd += (sender, args) =>
                {
                    AppPaused = false;
                    Resizing = false;
                    Timer.Start();
                    OnResize();
                };
                Window.Show();
                Window.Update();
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message + "\n" + ex.StackTrace, "Error");
                return false;
            }
        }

        protected bool InitDirect3D()
        {
            var creationFlags = D3D11.DeviceCreationFlags.None;
#if DEBUG
            // creationFlags |= D3D11.DeviceCreationFlags.Debug;
#endif



            try
            {
                Device = new D3D11.Device(DriverType, creationFlags);
            }
            catch (Exception ex)
            {
                MessageBox.Show("D3D11Device creation failed\n" + ex.Message + "\n" + ex.StackTrace, "Error");
                return false;
            }
            ImmediateContext = Device.ImmediateContext;
            if (Device.FeatureLevel != D3D.FeatureLevel.Level_11_0)
            {
                MessageBox.Show("Direct3D Feature Level 11 unsupported");
                return false;
            }

            Debug.Assert((Msaa4XQuality = Device.CheckMultisampleQualityLevels(DXGI.Format.R8G8B8A8_UNorm, 4)) > 0);
            try
            {
                DXGI.SwapChainDescription sd = new DXGI.SwapChainDescription()
                {
                    ModeDescription = new DXGI.ModeDescription(ClientWidth, ClientHeight, new DXGI.Rational(60, 1), DXGI.Format.R8G8B8A8_UNorm)
                    {
                        ScanlineOrdering = DXGI.DisplayModeScanlineOrder.Unspecified,
                        Scaling = DXGI.DisplayModeScaling.Unspecified
                    },
                    SampleDescription = Enable4XMsaa ? new DXGI.SampleDescription(4, Msaa4XQuality - 1) : new DXGI.SampleDescription(1, 0),
                    Usage = DXGI.Usage.RenderTargetOutput,
                    BufferCount = 1,
                    OutputHandle = Window.Handle,
                    IsWindowed = true,
                    SwapEffect = DXGI.SwapEffect.Discard,
                    Flags = DXGI.SwapChainFlags.None
                };


                using (var factory = new DXGI.Factory1())
                {
                    SwapChain = new DXGI.SwapChain(factory, Device, sd);
                }


            }
            catch (Exception ex)
            {
                MessageBox.Show("SwapChain creation failed\n" + ex.Message + "\n" + ex.StackTrace, "Error");
                return false;
            }
            OnResize();
            return true;


        }

        protected void CalculateFrameRateStats()
        {
            frameCount++;
            if ((Timer.TotalTime - timeElapsed) >= 1.0f)
            {
                var fps = (float)frameCount;
                var mspf = 1000.0f / fps;
                var s = string.Format("{0}\t FPS: {1}\t Frame Time: {2} (ms)", MainWindowCaption, fps, mspf);
                Window.Text = s;
                frameCount = 0;
                timeElapsed += 1.0f;
            }
        }

        public virtual void OnResize()
        {
            Debug.Assert(ImmediateContext != null);
            Debug.Assert(Device != null);
            Debug.Assert(SwapChain != null);
            Util.ReleaseCom(ref RenderTargetView);
            Util.ReleaseCom(ref DepthStencilView);
            Util.ReleaseCom(ref DepthStencilBuffer);
            SwapChain.ResizeBuffers(1, ClientWidth, ClientHeight, DXGI.Format.R8G8B8A8_UNorm, DXGI.SwapChainFlags.None);
            using (var resource = D3D11.Resource.FromSwapChain<D3D11.Texture2D>(SwapChain, 0))
            {
                RenderTargetView = new D3D11.RenderTargetView(Device, resource);
            }
            var depthStencilDesc = new D3D11.Texture2DDescription()
            {
                Width = ClientWidth,
                Height = ClientHeight,
                MipLevels = 1,
                ArraySize = 1,
                Format = DXGI.Format.D24_UNorm_S8_UInt,
                SampleDescription = (Enable4XMsaa) ? new DXGI.SampleDescription(4, Msaa4XQuality - 1) : new DXGI.SampleDescription(1, 0),
                Usage = D3D11.ResourceUsage.Default,
                BindFlags = D3D11.BindFlags.DepthStencil,
                CpuAccessFlags = D3D11.CpuAccessFlags.None,
                OptionFlags = D3D11.ResourceOptionFlags.None
            };
            DepthStencilBuffer = new D3D11.Texture2D(Device, depthStencilDesc);
            DepthStencilView = new D3D11.DepthStencilView(Device, DepthStencilBuffer);
            ImmediateContext.OutputMerger.SetTargets(DepthStencilView, RenderTargetView);
            Viewport = new Viewport(0, 0, ClientWidth, ClientHeight, 0.0f, 1.0f);
            ImmediateContext.Rasterizer.SetViewport(Viewport);
        }
        public virtual void UpdateScene(float dt) { }
        public virtual void DrawScene() { }

        public void Run()
        {
            Timer.Reset();
            while (_running)
            {
                Application.DoEvents();
                Timer.Tick();
                if (!AppPaused)
                {
                    CalculateFrameRateStats();
                    UpdateScene(Timer.DeltaTime);
                    DrawScene();
                }
                else
                {
                    System.Threading.Thread.Sleep(100);
                }
            }
            Dispose();
        }


        protected virtual void OnMouseMove(object sender, MouseEventArgs e)
        {

        }

        protected virtual void OnMouseUp(object sender, MouseEventArgs e)
        {

        }

        protected virtual void OnMouseDown(object sender, MouseEventArgs e)
        {

        }

        private const int WM_ACTIVATE = 0x0006;
        private const int WM_SIZE = 0x0005;
        private const int WM_DESTROY = 0x0002;

        public virtual bool Init()
        {
            if (!InitMainWindow())
            {
                return false;
            }
            if (!InitDirect3D())
            {
                return false;
            }

            _running = true;
            return true;
        }


        private bool WndProc(ref Message m)
        {
            switch (m.Msg)
            {
                case WM_ACTIVATE:
                    if (m.WParam.ToInt32().LowWord() == 0)
                    {
                        AppPaused = true;
                        Timer.Stop();
                    }
                    else
                    {
                        AppPaused = false;
                        Timer.Start();
                    }
                    return true;
                case WM_SIZE:
                    ClientWidth = m.LParam.ToInt32().LowWord();
                    ClientHeight = m.LParam.ToInt32().HighWord();
                    if (Device != null)
                    {
                        if (m.WParam.ToInt32() == 1)
                        { // SIZE_MINIMIZED
                            AppPaused = true;
                            Minimized = true;
                            Maximized = false;
                        }
                        else if (m.WParam.ToInt32() == 2)
                        { // SIZE_MAXIMIZED
                            AppPaused = false;
                            Minimized = false;
                            Maximized = true;
                            OnResize();
                        }
                        else if (m.WParam.ToInt32() == 0)
                        { // SIZE_RESTORED
                            if (Minimized)
                            {
                                AppPaused = false;
                                Minimized = false;
                                OnResize();
                            }
                            else if (Maximized)
                            {
                                AppPaused = false;
                                Maximized = false;
                                OnResize();
                            }
                            else if (Resizing)
                            {
                            }
                            else
                            {
                                OnResize();
                            }
                        }
                    }
                    return true;
                case WM_DESTROY:
                    _running = false;
                    return true;
            }
            return false;
        }


        protected bool AppPaused;
        protected bool Enable4XMsaa;
        protected String MainWindowCaption;
        protected D3D.DriverType DriverType;
        protected int ClientWidth;
        protected int ClientHeight;
        protected bool Minimized;
        protected bool Maximized;
        protected bool Resizing;
        protected int Msaa4XQuality;
        protected D3D11.Device Device;
        protected D3D11.DeviceContext ImmediateContext;
        protected DXGI.SwapChain SwapChain;
        protected D3D11.Texture2D DepthStencilBuffer;
        protected D3D11.RenderTargetView RenderTargetView;
        protected D3D11.DepthStencilView DepthStencilView;
        protected Viewport Viewport;
        protected GameTimer Timer;
        private int frameCount = 0;
        private float timeElapsed = 0;
        private bool _running;


    }
}


