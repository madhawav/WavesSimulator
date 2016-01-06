using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using SharpDX;
using D3D = SharpDX.Direct3D;
using D3D11 = SharpDX.Direct3D11;
using DXGI = SharpDX.DXGI;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace GraphicsCore
{

    /**
    * Contains some code directly copied from the below mentioned repository belonging to author of  http://richardssoftware.net/ SlimDX Tutorial.
    * Source: https://github.com/ericrrichards/dx11/blob/master/DX11/Core/Waves.cs
    *  
    * The Code has been modified to utilize Direct Compute API to undertake Vertex Height Calculations. 
    **/
    public class AcceleratedWave : DisposableClass
    {
        private bool _disposed = false;
        private D3D11.Device Device;
        private D3D11.DeviceContext ImmediateContext;

        private float _waveTimeStep;
        private float _waveDamping;
        private float _waveSpeed;



        private const int BLOCK_SIZE = 32;

        private D3D11.Buffer _inputBuf1 = null;
        private D3D11.Buffer _inputBuf2 = null;

        private D3D11.ShaderResourceView _inputBuf1View = null;
        private D3D11.ShaderResourceView _inputBuf2View = null;

        private D3D11.UnorderedAccessView _inputBuf1UAV = null;
        private D3D11.UnorderedAccessView _inputBuf2UAV = null;

        private D3D11.ComputeShader _compShader = null;

        private WaveComputeConstantBuffer _wavesCB;
        private D3D11.Buffer _wavesCBBuffer = null;

        private float _wavet;

        private bool ownCompute;

        private bool rippleWriteOpen;
        private DataStream rippleWriteBuffer;

        private WaveSolutionSwapChain _waveSwapChain;

        private void __construct(D3D11.Device dev, D3D11.DeviceContext context)
        {
            this.Device = dev;
            this.ImmediateContext = context;
            _wavet = 0;
            _waveSwapChain = null;
            rippleWriteOpen = false;
        }
        public AcceleratedWave(D3D11.Device dev, D3D11.DeviceContext context, D3D11.ComputeShader shader)
        {
            __construct(dev, context);
            ownCompute = false;
            _compShader = shader;
        }

        public AcceleratedWave(D3D11.Device dev, D3D11.DeviceContext context, String shaderLocation, String shaderMethodName)
        {
            __construct(dev, context);
            _compShader = BuildCompute(shaderLocation, shaderMethodName);
            ownCompute = true;
        }

        public WaveSolution[] GetSolutionSet()
        {
            CloseCurrentBuffer();

            DataStream stream;
            WaveSolution[] read;
            ImmediateContext.MapSubresource(_waveSwapChain.CurrentBuffer, 0, D3D11.MapMode.Read, D3D11.MapFlags.None, out stream);

            read = stream.ReadRange<WaveSolution>(VertexCount);

            ImmediateContext.UnmapSubresource(_waveSwapChain.CurrentBuffer, 0);

            return read;
        }

        public void Init(int m, int n, float dx, float dt, float speed, float damping)
        {
            SetupWave(m, n, dx, dt, speed, damping);


            BuildWaveCB();
            BuildBuffers();
            FillBuffers();

            _waveSwapChain = new WaveSolutionSwapChain(_inputBuf1View, _inputBuf1UAV, _inputBuf1, _inputBuf2View, _inputBuf2UAV, _inputBuf2);

        }

        private void SetupWave(int m, int n, float dx, float dt, float speed, float damping)
        {
            this._waveDamping = damping;
            this._waveSpeed = speed;
            _wavesCB = new WaveComputeConstantBuffer();
            _wavesCB.RowCount = (uint)m;
            _wavesCB.ColumnCount = (uint)n;

            //VertexCount = m * n;
            TriangleCount = (m - 1) * (n - 1) * 2;
            _waveTimeStep = dt;
            _wavesCB._spatialStep = dx;

            var d = damping * dt + 2.0f;
            var e = (speed * speed) * (dt * dt) / (dx * dx);
            float k1 = (damping * dt - 2.0f) / d;
            float k2 = (4.0f - 8.0f * e) / d;
            float k3 = (2.0f * e) / d;

            _wavesCB.k = new Vector4(k1, k2, k3, 1);

        }




        private D3D11.ComputeShader BuildCompute(string fileName, string methodName)
        {
            string errors = null;
            var shaderFlags = SharpDX.D3DCompiler.ShaderFlags.None;
#if DEBUG

            shaderFlags |= SharpDX.D3DCompiler.ShaderFlags.SkipOptimization;
#endif

            SharpDX.D3DCompiler.ShaderBytecode compiledShader = null;
            try
            {
                //var v = SharpDX.D3DCompiler.ShaderBytecode.CompileFromFile("FX/compute.fx", "VectorAdd", "cs_5_0");

                compiledShader = SharpDX.D3DCompiler.ShaderBytecode.CompileFromFile(
                fileName,
                methodName,
                "cs_5_0");

                return new D3D11.ComputeShader(Device, compiledShader);


            }
            catch (Exception ex)
            {
                if (!string.IsNullOrEmpty(errors))
                {
                    throw new Exception(errors);
                }
                throw ex;

            }
            finally
            {
                Util.ReleaseCom(ref compiledShader);
            }
        }

        private void BuildBuffers()
        {
            D3D11.BufferDescription buf_desc = new D3D11.BufferDescription();
            buf_desc.BindFlags = D3D11.BindFlags.ShaderResource | D3D11.BindFlags.UnorderedAccess;
            buf_desc.Usage = D3D11.ResourceUsage.Default;
            buf_desc.StructureByteStride = WaveSolution.Stride;
            buf_desc.SizeInBytes = WaveSolution.Stride * VertexCount;
            buf_desc.OptionFlags = D3D11.ResourceOptionFlags.BufferStructured;
            buf_desc.CpuAccessFlags = D3D11.CpuAccessFlags.Write | D3D11.CpuAccessFlags.Read;

            _inputBuf1 = new D3D11.Buffer(Device, buf_desc);
            _inputBuf2 = new D3D11.Buffer(Device, buf_desc);

            D3D11.ShaderResourceViewDescription vdesc = new D3D11.ShaderResourceViewDescription();
            vdesc.Dimension = D3D.ShaderResourceViewDimension.Buffer;
            vdesc.Format = DXGI.Format.Unknown;
            vdesc.Buffer.FirstElement = 0;
            vdesc.Buffer.ElementCount = VertexCount;


            _inputBuf1View = new D3D11.ShaderResourceView(Device, _inputBuf1, vdesc);
            _inputBuf2View = new D3D11.ShaderResourceView(Device, _inputBuf2, vdesc);



            D3D11.UnorderedAccessViewDescription uavdesc = new D3D11.UnorderedAccessViewDescription();
            uavdesc.Format = DXGI.Format.Unknown;
            uavdesc.Dimension = D3D11.UnorderedAccessViewDimension.Buffer;
            uavdesc.Buffer.FirstElement = 0;
            uavdesc.Buffer.ElementCount = VertexCount;

            _inputBuf1UAV = new D3D11.UnorderedAccessView(Device, _inputBuf1, uavdesc);
            _inputBuf2UAV = new D3D11.UnorderedAccessView(Device, _inputBuf2, uavdesc);


            return;

        }

        private void FillBuffers()
        {

            float speed = _waveSpeed;
            float dx = _wavesCB._spatialStep;
            float dt = _waveTimeStep;

            uint m = _wavesCB.RowCount;
            uint n = _wavesCB.ColumnCount;

            var d = _waveDamping * dt + 2.0f;
            var e = (speed * speed) * (dt * dt) / (dx * dx);

            var w2 = (n - 1) * dx * 0.5f;
            var d2 = (m - 1) * dx * 0.5f;



            var WaveSolutions = new WaveSolution[m * n];

            for (var i = 0; i < m; i++)
            {
                var z = d2 - i * dx;
                for (var j = 0; j < n; j++)
                {
                    var x = -w2 + j * dx;
                    WaveSolutions[i * n + j] = new WaveSolution();
                    WaveSolutions[i * n + j].Pos = new Vector4(x, 0, z, 0);
                    WaveSolutions[i * n + j].Normal = new Vector4(0, 1, 0, 1);
                    WaveSolutions[i * n + j].Tangent = new Vector4(1.0f, 0, 0, 1);
                }
            }

            ImmediateContext.UpdateSubresource<WaveSolution>(WaveSolutions, _inputBuf1);

            WaveSolutions = new WaveSolution[m * n];

            for (var i = 0; i < m; i++)
            {
                var z = d2 - i * dx;
                for (var j = 0; j < n; j++)
                {
                    var x = -w2 + j * dx;
                    WaveSolutions[i * n + j] = new WaveSolution();
                    WaveSolutions[i * n + j].Pos = new Vector4(x, 0, z, 0);
                    WaveSolutions[i * n + j].Normal = new Vector4(0, 1, 0, 1);
                    WaveSolutions[i * n + j].Tangent = new Vector4(1.0f, 0, 0, 1);
                }
            }
            ImmediateContext.UpdateSubresource<WaveSolution>(WaveSolutions, _inputBuf2);

        }

        private void BuildWaveCB()
        {
            var buffer = new D3D11.Buffer(Device, new D3D11.BufferDescription
            {
                Usage = D3D11.ResourceUsage.Default,
                SizeInBytes = Marshal.SizeOf(_wavesCB),
                BindFlags = D3D11.BindFlags.ConstantBuffer
            });

            /*var data = DataStream.Create<WavesCB>(new WavesCB[] { _wavesCB }, true, true);
            data.Position = 0;*/

            ImmediateContext.UpdateSubresource<WaveComputeConstantBuffer>(new WaveComputeConstantBuffer[] { _wavesCB }, buffer);
            _wavesCBBuffer = buffer;


        }

        public D3D11.ShaderResourceView CurrentSolutionSRV
        {
            get
            {
                return _waveSwapChain.CurrentSRV;
            }
        }

        public void UpdateWave(float dt)
        {
            _wavet += dt;

            if (!(_wavet >= _waveTimeStep))
            {
                return;
            }

            CloseCurrentBuffer();

            _waveSwapChain.Swap();

            while (_wavet > _waveTimeStep)
                _wavet -= _waveTimeStep;

            ImmediateContext.ComputeShader.Set(_compShader);
            ImmediateContext.ComputeShader.SetConstantBuffer(0, _wavesCBBuffer);
            ImmediateContext.ComputeShader.SetShaderResources(0, _waveSwapChain.PastSolution);
            ImmediateContext.ComputeShader.SetUnorderedAccessView(0, _waveSwapChain.CurrentSolution);

            ImmediateContext.Dispatch((int)_wavesCB.ColumnCount / BLOCK_SIZE + 1, (int)_wavesCB.RowCount / BLOCK_SIZE + 1, 1);

            ImmediateContext.ComputeShader.SetUnorderedAccessView(0, null);
            ImmediateContext.ComputeShader.SetShaderResource(0, null);
            ImmediateContext.ComputeShader.Set(null);

        }

        private void OpenCurrentBuffer()
        {
            if (!rippleWriteOpen)
            {
                ImmediateContext.MapSubresource(_waveSwapChain.CurrentBuffer, 0, D3D11.MapMode.ReadWrite, D3D11.MapFlags.None, out rippleWriteBuffer);
            }
            rippleWriteOpen = true;
        }

        private void CloseCurrentBuffer()
        {
            if (rippleWriteOpen)
            {
                rippleWriteBuffer.Close();
                ImmediateContext.UnmapSubresource(_waveSwapChain.CurrentBuffer, 0);
            }
            rippleWriteOpen = false;
        }
        private WaveSolution GetSolution(int index)
        {
            OpenCurrentBuffer();
            int pos = WaveSolution.Stride * index;
            rippleWriteBuffer.Position = pos;
            WaveSolution v = rippleWriteBuffer.Read<WaveSolution>();
            return v;
        }
        private void SetSolution(int index, WaveSolution v)
        {
            OpenCurrentBuffer();
            int pos = WaveSolution.Stride * index;
            rippleWriteBuffer.Position = pos;
            rippleWriteBuffer.Write<WaveSolution>(v);
        }

        private void IncrementSolution(int index, float amount)
        {
            WaveSolution v = GetSolution(index);
            v.Pos.Y += amount;
            SetSolution(index, v);
        }
        public void Disturb(int i, int j, float magnitude)
        {
            int ColumnCount = (int)_wavesCB.ColumnCount;

            Debug.Assert(i > 1 && i < _wavesCB.RowCount - 2);
            Debug.Assert(j > 1 && j < _wavesCB.ColumnCount - 2);

            var m2 = 0.5f * magnitude;

            IncrementSolution(i * ColumnCount + j, magnitude);
            IncrementSolution(i * ColumnCount + j + 1, m2);
            IncrementSolution(i * ColumnCount + j - 1, m2);
            IncrementSolution((i + 1) * ColumnCount + j, m2);
            IncrementSolution((i - 1) * ColumnCount + j, m2);

        }

        public void DisturbSharp(int i, int j, float magnitude)
        {
            Debug.Assert(i > 1 && i < _wavesCB.RowCount - 2);
            Debug.Assert(j > 1 && j < _wavesCB.ColumnCount - 2);

            IncrementSolution(i * (int)_wavesCB.ColumnCount + j, magnitude);

        }


        protected override void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    if (ownCompute)
                        Util.ReleaseCom(ref _compShader);
                    Util.ReleaseCom(ref _inputBuf1View);
                    Util.ReleaseCom(ref _inputBuf2View);
                    Util.ReleaseCom(ref _inputBuf2UAV);
                    Util.ReleaseCom(ref _inputBuf1UAV);
                    Util.ReleaseCom(ref _inputBuf1);
                    Util.ReleaseCom(ref _inputBuf2);
                }
            }
            base.Dispose(disposing);
        }

        public int RowCount
        {
            get
            {
                return (int)_wavesCB.RowCount;
            }
        }

        public int ColumnCount
        {
            get
            {
                return (int)_wavesCB.ColumnCount;
            }
        }
        public int TriangleCount { get; set; }

        public int VertexCount
        {
            get
            {
                return RowCount * ColumnCount;
            }
        }
    }
}
