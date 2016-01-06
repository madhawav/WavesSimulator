using GraphicsCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

using SharpDX;
using D3D = SharpDX.Direct3D;
using D3D11 = SharpDX.Direct3D11;
using DXGI = SharpDX.DXGI;
using System.Diagnostics;

namespace WavesSimulator
{

    /**
    * Based on  http://richardssoftware.net/ SlimDX Tutorial - Dynamic Vertex Buffers: Waves Demo
    * 
    * Contains some code directly copied from the below mentioned repository belonging to author of  http://richardssoftware.net/ SlimDX Tutorial.
    * Source:   https://github.com/ericrrichards/dx11/blob/master/DX11/29-WavesDemo/WavesDemo.cs
    *           https://github.com/ericrrichards/dx11/blob/master/DX11/LightingDemo/Program.cs
    *           https://github.com/ericrrichards/dx11/blob/master/DX11/27-ShapeModels/ShapeModelsDemo.cs
    *           https://github.com/ericrrichards/dx11/
    *
    * The code has been modified to work with SharpDX API
    * 
    * The mechanism for wave height calculation has been improved to use Direct Compute API.
    * The rendering code has been improved to include 
    *   Ambient, Diffuse and Specular Lighting with Support to Directional, Point and Spot Lights based on http://richardssoftware.net/ SlimDX Tutorial - Lightning Tutorials
    *   Reflection and Refraction on Water Surface with blending based on depth - inspired by Rasterteks D3D11 Tutorial -Tutorial 29 - Water (http://www.rastertek.com/tutdx11.html)
    * 
    **/
    public class WavesSimulator : D3DApp
    {

        //Geometry related buffers
        private D3D11.Buffer _groundVB;
        private D3D11.Buffer _groundIB;
        private D3D11.Buffer _wavesVB;
        private D3D11.Buffer _wavesIB;
        private D3D11.Buffer _ballVB;
        private D3D11.Buffer _ballIB;

        private int _groundIndexCount;
        private int _ballIndexCount;

        //WVP Matrices
        private Matrix _groundWorld;
        private Matrix _ballWorld;
        private Matrix _wavesWorld;
        private Matrix _view;
        private Matrix _proj;
        private Matrix _reflectView; // when the camera is placed, reflected from plane of water

        //Effect Techniques and Parameters
        private D3D11.Effect _fx;
        private D3D11.EffectMatrixVariable _fxWVP; //World*View*Projection
        private D3D11.EffectTechnique _tech;
        private D3D11.EffectMatrixVariable _fxWorld; //World
        private D3D11.EffectMatrixVariable _fxWIT; //World Inverse Transformed
        private D3D11.EffectVectorVariable _fxEyePosW; //World Eye Pos
        private D3D11.EffectVariable _fxDirLight;
        private D3D11.EffectVariable _fxPointLight;
        private D3D11.EffectVariable _fxSpotLight;
        private D3D11.EffectVariable _fxMaterial;
        private D3D11.EffectVectorVariable _fxClipPlane;
        private D3D11.EffectMatrixVariable _fxReflectViewProj;
        private D3D11.EffectShaderResourceVariable _fxDiffuseMap; //Used to pass texture of ground
        private D3D11.EffectShaderResourceVariable _fxRefractiveMap;
        private D3D11.EffectShaderResourceVariable _fxReflectiveMap;
        private D3D11.EffectShaderResourceVariable _fxgRefractionPositionMap;

        private D3D11.EffectScalarVariable _fxUseStructBuf;
        private D3D11.EffectShaderResourceVariable _fxSolutionSR; //Used to pass compute shader outputs to vertex shader


        //Textures and RSVs
        private D3D11.Texture2D _groundMap;
        private D3D11.ShaderResourceView _groundMapSRV;

        private D3D11.RenderTargetView refractRenderTargetView;
        private D3D11.Texture2D refractText;
        private D3D11.ShaderResourceView refractResourceView;


        private D3D11.RenderTargetView reflectRenderTargetView;
        private D3D11.Texture2D reflectText;
        private D3D11.ShaderResourceView reflectResourceView;

        private D3D11.RenderTargetView positionMapRenderTargetView; //position map is used to store information related to world position of each pixel rendered through reflection. This is needed to calculate blend factor for reflection and refraction
        private D3D11.Texture2D positionMapText;
        private D3D11.ShaderResourceView positionMapResourceView;


        private D3D11.BlendState _alphaBlend;
        private D3D11.InputLayout _inputLayout;


        //Direct Compute based Wave System
        private GraphicsCore.AcceleratedWave _awave;

        //Timing
        private float _tBase;

        // Camera variables
        private float _theta;
        private float _phi;
        private float _radius;
        private System.Drawing.Point _lastMousePos;
        private Vector3 _eyePosW;

        //Lightning Related
        private DirectionalLight _dirLight;
        private PointLight _pointLight;
        private SpotLight _spotLight;
        private Material _landMaterial;
        private Material _wavesMaterial;


        private bool _disposed;


        public WavesSimulator(IntPtr hInst)
            : base(hInst)
        {

            //Geometry Related Buffers
            _ballIB = null;
            _ballVB = null;
            _groundVB = null;
            _groundIB = null;
            _wavesVB = null;
            _wavesIB = null;


            _ballIndexCount = 0;
            _groundIndexCount = 0;

            //WVP Matrices
            _groundWorld = Matrix.Identity;
            _wavesWorld = Matrix.Translation(0, -2.0f, 0);
            _ballWorld = Matrix.Translation(-30, 15, 0);
            _view = Matrix.Identity;
            _proj = Matrix.Identity;
            _reflectView = Matrix.Identity;

            //Rendering Effects Related
            _fx = null;
            _fxWVP = null;
            _tech = null;
            _fxWorld = null;
            _fxWIT = null;
            _fxEyePosW = null;
            _fxDirLight = null;
            _fxPointLight = null;
            _fxSpotLight = null;
            _fxMaterial = null;
            _fxDiffuseMap = null;
            _fxRefractiveMap = null;
            _fxClipPlane = null;
            _fxReflectiveMap = null;
            _fxReflectViewProj = null;
            _fxUseStructBuf = null;
            _fxgRefractionPositionMap = null;

            //Textures and Views
            refractText = null;
            refractRenderTargetView = null;
            refractResourceView = null;

            reflectRenderTargetView = null;
            reflectResourceView = null;
            reflectText = null;

            positionMapRenderTargetView = null;
            positionMapResourceView = null;
            positionMapText = null;

            _groundMapSRV = null;
            _groundMap = null;

            //Input Format
            _inputLayout = null;


            //Camera Related
            _theta = 1.5f * MathF.PI;
            _phi = 0.1f * MathF.PI;
            _radius = 200.0f;
            _lastMousePos = new System.Drawing.Point(0, 0);
            _eyePosW = new Vector3();


            //Shading and Lighting
            _alphaBlend = null;

            _dirLight = new DirectionalLight
            {
                Ambient = new Color4(0.2f, 0.2f, 0.2f, 1),
                Diffuse = new Color4(0.5f, 0.5f, 0.5f, 1),
                Specular = new Color4(0.5f, 0.5f, 0.5f, 1),
                Direction = new Vector3(0.57735f, -0.57735f, 0.57735f)
            };

            _pointLight = new PointLight
            {
                Ambient = new Color4(0.3f, 0.3f, 0.3f, 1),
                Diffuse = new Color4(0.7f, 0.7f, 0.7f, 1),
                Specular = new Color4(0.7f, 0.7f, 0.7f, 1),
                Attenuation = new Vector3(0.1f, 0.1f, 0.1f),
                Range = 25.0f
            };
            _spotLight = new SpotLight
            {
                Ambient = new Color4(0, 0, 0, 0),
                Diffuse = new Color4(1.0f, 1.0f, 1.0f, 1),
                Specular = Color.White,
                Attenuation = new Vector3(1.0f, 0.0f, 0.0f),
                Spot = 96.0f,
                Range = 10000.0f
            };


            _landMaterial = new Material
            {
                Ambient = new Color4(1f, 1f, 1f, 1.0f),
                Diffuse = new Color4(1, 1, 1, 1.0f),
                Specular = new Color4(0.2f, 0.2f, 0.2f, 16.0f),
                Reflect = new Color4(1.0f, 1f, 1f, 1f)

            };
            _wavesMaterial = new Material
            {
                Ambient = new Color4(1, 1, 1, 0.8f),
                Diffuse = new Color4(0.137f, 0.42f, 0.556f, 1.0f),
                Specular = new Color4(0.8f, 0.8f, 0.8f, 96.0f),
                Reflect = new Color4(2f, 1, 1, 1) //R component of Reflect is used for Gama Correction in Effect
            };


            _disposed = false;
            MainWindowCaption = "Waves Simulator";

        }


        public override bool Init()
        {
            if (!base.Init())
            {
                return false;
            }


            //Initialize Wave Physics
            _awave = new AcceleratedWave(Device, ImmediateContext, "FX/compute.fx", "ProcessVertex");
            _awave.Init(200, 200, 0.8f, 0.03f, 6.25f, 0.1f);

            BuildLandGeometryBuffers();
            BuildWavesGeometryBuffers();
            BuildBallGeometryBuffers();
            BuildFX();
            BuildVertexLayout();
            FillWavesGeometryBuffer();


            //Initialize Alpha Blending
            D3D11.BlendStateDescription alphaDesc = new D3D11.BlendStateDescription();
            alphaDesc.RenderTarget[0].IsBlendEnabled = true;
            alphaDesc.RenderTarget[0].SourceBlend = D3D11.BlendOption.SourceAlpha;
            alphaDesc.RenderTarget[0].DestinationBlend = D3D11.BlendOption.InverseSourceAlpha;
            alphaDesc.RenderTarget[0].BlendOperation = D3D11.BlendOperation.Add;
            alphaDesc.RenderTarget[0].SourceAlphaBlend = D3D11.BlendOption.One;
            alphaDesc.RenderTarget[0].DestinationAlphaBlend = D3D11.BlendOption.Zero;
            alphaDesc.RenderTarget[0].AlphaBlendOperation = D3D11.BlendOperation.Add;
            alphaDesc.RenderTarget[0].RenderTargetWriteMask = D3D11.ColorWriteMaskFlags.All;

            _alphaBlend = new D3D11.BlendState(base.Device, alphaDesc);

            //Setup Ground Texture
            _groundMap = TextureLoader.CreateTex2DFromFile(Device, "Textures\\grass.jpg");
            _groundMapSRV = new D3D11.ShaderResourceView(Device, _groundMap);

            return true;
        }



        public override void OnResize()
        {
            base.OnResize();
            //Since reflection and refraction quality depends on Screen Size, the maps are needed to be recreated on a resize.

            Util.ReleaseCom(ref reflectRenderTargetView);
            Util.ReleaseCom(ref reflectResourceView);
            Util.ReleaseCom(ref reflectText);

            Util.ReleaseCom(ref refractRenderTargetView);
            Util.ReleaseCom(ref refractResourceView);
            Util.ReleaseCom(ref refractText);

            Util.ReleaseCom(ref positionMapRenderTargetView);
            Util.ReleaseCom(ref positionMapResourceView);
            Util.ReleaseCom(ref positionMapText);

            // Recalculate perspective matrix
            _proj = Matrix.PerspectiveFovLH(0.25f * MathF.PI, AspectRatio, 1.0f, 1000.0f);

            D3D11.Texture2DDescription tdesc = new D3D11.Texture2DDescription();
            tdesc.Format = DXGI.Format.R32G32B32A32_Float;
            tdesc.MipLevels = 1;
            tdesc.Width = ClientWidth;
            tdesc.Height = ClientHeight;
            tdesc.ArraySize = 1;
            tdesc.SampleDescription.Count = 1;
            tdesc.Usage = D3D11.ResourceUsage.Default;
            tdesc.BindFlags = D3D11.BindFlags.RenderTarget | D3D11.BindFlags.ShaderResource;
            tdesc.CpuAccessFlags = D3D11.CpuAccessFlags.None;

            //Recreate refraction map
            refractText = new D3D11.Texture2D(Device, tdesc);
            refractRenderTargetView = new D3D11.RenderTargetView(Device, refractText);
            refractResourceView = new D3D11.ShaderResourceView(Device, refractText);

            //recreate reflection map
            reflectText = new D3D11.Texture2D(Device, tdesc);
            reflectRenderTargetView = new D3D11.RenderTargetView(Device, reflectText);
            reflectResourceView = new D3D11.ShaderResourceView(Device, reflectText);

            //recreate position map
            positionMapText = new D3D11.Texture2D(Device, tdesc);
            positionMapRenderTargetView = new D3D11.RenderTargetView(Device, positionMapText);
            positionMapResourceView = new D3D11.ShaderResourceView(Device, positionMapText);


        }

        private void BuildLandGeometryBuffers()
        {
            GeometryGenerator.MeshData grid = GeometryGenerator.CreateGrid(160.0f, 160.0f, 50, 50);
            var vertices = new List<VertexPN>();
            foreach (var vertex in grid.Vertices)
            {
                var pos = vertex.Position;
                pos.Y = GetHillHeight(pos.X, pos.Z);

                var normal = GetHillNormal(pos.X, pos.Z);

                vertices.Add(new VertexPN(pos, normal, vertex.TexC));
            }
            var vbd = new D3D11.BufferDescription(VertexPN.Stride * vertices.Count, D3D11.ResourceUsage.Immutable, D3D11.BindFlags.VertexBuffer, D3D11.CpuAccessFlags.None, D3D11.ResourceOptionFlags.None, 0);
            _groundVB = new D3D11.Buffer(Device, DataStream.Create<VertexPN>(vertices.ToArray(), false, false), vbd);

            var ibd = new D3D11.BufferDescription(sizeof(int) * grid.Indices.Count, D3D11.ResourceUsage.Immutable, D3D11.BindFlags.IndexBuffer, D3D11.CpuAccessFlags.None, D3D11.ResourceOptionFlags.None, 0);
            _groundIB = new D3D11.Buffer(Device, DataStream.Create<Int32>(grid.Indices.ToArray(), false, false), ibd);
            _groundIndexCount = grid.Indices.Count;

        }

        //Sine function of land
        private float GetHillHeight(float x, float z)
        {

            float dx = Math.Abs(x);
            float dz = Math.Abs(z);
            float r = (float)Math.Sqrt(dx * dx + dz * dz);
            r /= 10;
            r = r * r;
            r = -r;
            r += 20;


            return 0.3f * (z * MathF.Sin(0.1f * x) + x * MathF.Cos(0.1f * z));
        }

        private static Vector3 GetHillNormal(float x, float z)
        {
            var n = new Vector3(
                -0.03f * z * MathF.Cos(0.1f * x) - 0.3f * MathF.Cos(0.1f * z),
                1.0f,
                -0.3f * MathF.Sin(0.1f * x) + 0.03f * x * MathF.Sin(0.1f * z)
                );
            n.Normalize();

            return n;
        }


        private void BuildWavesGeometryBuffers()
        {
            var vbd = new D3D11.BufferDescription(VertexPN.Stride * _awave.VertexCount, D3D11.ResourceUsage.Dynamic, D3D11.BindFlags.VertexBuffer, D3D11.CpuAccessFlags.Write, D3D11.ResourceOptionFlags.None, 0);
            _wavesVB = new D3D11.Buffer(Device, vbd);

            var indices = new List<int>();
            var m = _awave.RowCount;
            var n = _awave.ColumnCount;
            for (int i = 0; i < m - 1; i++)
            {
                for (int j = 0; j < n - 1; j++)
                {
                    indices.Add(i * n + j);
                    indices.Add(i * n + j + 1);
                    indices.Add((i + 1) * n + j);

                    indices.Add((i + 1) * n + j);
                    indices.Add(i * n + j + 1);
                    indices.Add((i + 1) * n + j + 1);
                }
            }
            var ibd = new D3D11.BufferDescription(sizeof(int) * indices.Count, D3D11.ResourceUsage.Immutable, D3D11.BindFlags.IndexBuffer, D3D11.CpuAccessFlags.None, D3D11.ResourceOptionFlags.None, 0);
            _wavesIB = new D3D11.Buffer(Device, DataStream.Create<int>(indices.ToArray(), false, false), ibd);
        }

        private void FillWavesGeometryBuffer()
        {
            WaveSolution[] solutions = _awave.GetSolutionSet();

            DataStream dataStream;
            var mappedData = ImmediateContext.MapSubresource(_wavesVB, 0, D3D11.MapMode.WriteDiscard, D3D11.MapFlags.None, out dataStream);
            for (int i = 0; i < _awave.VertexCount; i++)
            {
                WaveSolution wave = solutions[i];
                dataStream.Write(new VertexPN(new Vector3(wave.Pos.X, wave.Pos.Y, wave.Pos.Z), new Vector3(wave.Normal.X, wave.Normal.Y, wave.Normal.Z), new Vector2(wave.Pos.X / 200.0f, wave.Pos.Z / 200.0f)));

            }
            ImmediateContext.UnmapSubresource(_wavesVB, 0);
            dataStream.Close();
        }

        private void BuildBallGeometryBuffers()
        {
            var mesh = GeometryGenerator.CreateSphere(10, 30, 30);

            var vertices = new List<VertexPN>();
            foreach (var vertex in mesh.Vertices)
            {
                var pos = vertex.Position;
                vertices.Add(new VertexPN(pos, vertex.Normal, vertex.TexC));
            }
            var vbd = new D3D11.BufferDescription(VertexPN.Stride * vertices.Count, D3D11.ResourceUsage.Immutable, D3D11.BindFlags.VertexBuffer, D3D11.CpuAccessFlags.None, D3D11.ResourceOptionFlags.None, 0);
            _ballVB = new D3D11.Buffer(Device, DataStream.Create<VertexPN>(vertices.ToArray(), false, false), vbd);

            var ibd = new D3D11.BufferDescription(sizeof(int) * mesh.Indices.Count, D3D11.ResourceUsage.Immutable, D3D11.BindFlags.IndexBuffer, D3D11.CpuAccessFlags.None, D3D11.ResourceOptionFlags.None, 0);
            _ballIB = new D3D11.Buffer(Device, DataStream.Create<Int32>(mesh.Indices.ToArray(), false, false), ibd);
            _ballIndexCount = mesh.Indices.Count;


        }

        private void BuildFX()
        {
            SharpDX.D3DCompiler.ShaderBytecode compiledShader = null;
            try
            {
                compiledShader = new SharpDX.D3DCompiler.ShaderBytecode(System.IO.File.ReadAllBytes("fx/lighting.fxo"));
                _fx = new D3D11.Effect(Device, compiledShader);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                return;
            }
            finally
            {
                Util.ReleaseCom(ref compiledShader);
            }

            _tech = _fx.GetTechniqueByName("LightTech");
            _fxWVP = _fx.GetVariableByName("gWorldViewProj").AsMatrix();
            _fxWorld = _fx.GetVariableByName("gWorld").AsMatrix();
            _fxWIT = _fx.GetVariableByName("gWorldInvTranspose").AsMatrix();
            _fxEyePosW = _fx.GetVariableByName("gEyePosW").AsVector();
            _fxDirLight = _fx.GetVariableByName("gDirLight");
            _fxPointLight = _fx.GetVariableByName("gPointLight");
            _fxSpotLight = _fx.GetVariableByName("gSpotLight");
            _fxMaterial = _fx.GetVariableByName("gMaterial");
            _fxDiffuseMap = _fx.GetVariableByName("gDiffuseMap").AsShaderResource();
            _fxRefractiveMap = _fx.GetVariableByName("gRefractiveMap").AsShaderResource();
            _fxClipPlane = _fx.GetVariableByName("gClipPlane").AsVector();
            _fxReflectViewProj = _fx.GetVariableByName("gReflectViewProj").AsMatrix();
            _fxReflectiveMap = _fx.GetVariableByName("gReflectiveMap").AsShaderResource();
            _fxgRefractionPositionMap = _fx.GetVariableByName("gRefractionPositionMap").AsShaderResource();
            _fxUseStructBuf = _fx.GetVariableByName("gUseStructBuf").AsScalar();
            _fxSolutionSR = _fx.GetVariableByName("gSolution").AsShaderResource();

        }

        private void BuildVertexLayout()
        {
            var vertexDesc = VertexPN.GetVertexDescription();
            var passDesc = _tech.GetPassByIndex(0).Description;
            _inputLayout = new D3D11.InputLayout(Device, passDesc.Signature, vertexDesc);
        }



        public override void UpdateScene(float dt)
        {
            base.UpdateScene(dt);

            // Get camera position from polar coords
            var x = _radius * MathF.Sin(_phi) * MathF.Cos(_theta);
            var z = _radius * MathF.Sin(_phi) * MathF.Sin(_theta);
            var y = _radius * MathF.Cos(_phi);

            _eyePosW = new Vector3(x, y, z);

            // Build the view matrix
            var pos = new Vector3(x, y, z);
            var target = new Vector3(0);
            var up = new Vector3(0, 1, 0);
            _view = Matrix.LookAtLH(pos, target, up);
            //reflect by the water surface at y=0 level
            _reflectView = Matrix.LookAtLH(new Vector3(pos.X, -pos.Y, pos.Z), target, up);


            // camera update code omitted...
            if ((Timer.TotalTime - _tBase) >= 5.25f)
            {
                _tBase += 5.25f;

                var i = 5 + MathF.Rand() % 190;
                var j = 5 + MathF.Rand() % 190;
                var r = MathF.Rand(1.0f, 2.0f);
                //_waves.Disturb(i, j, r);
                //_waves.DisturbLine(i, 2, 190, 1f);
                GenerateCircleWave();
            }
            //_waves.Update(dt);
            _awave.UpdateWave(dt);



            // animate lights
            _pointLight.Position = new Vector3(
                70.0f * MathF.Cos(0.2f * Timer.TotalTime),
                Math.Max(GetHillHeight(_pointLight.Position.X, _pointLight.Position.Z), -3.0f) + 10.0f,
                70.0f * MathF.Sin(0.2f * Timer.TotalTime)
            );
            _spotLight.Position = _eyePosW;
            _spotLight.Direction = Vector3.Normalize(target - pos);
        }

        //Create a circular wave coming in towards center. This is done by placing many ripples in a circle.
        private void GenerateCircleWave()
        {
            for (float i = 0; i < 360; i += 1)
            {
                float x = MathF.Cos(i) * 90;
                float z = MathF.Sin(i) * 90;
                x += 105;
                z += 105;
                int X = Convert.ToInt32(x);
                int Z = Convert.ToInt32(z);
                _awave.DisturbSharp(X, Z, 1.5f);
            }
        }


        public void DrawLand()
        {
            DrawLand(new Vector4(0, 0, 0, 1), _view);
        }

        public void DrawLand(Vector4 clipPlane, Matrix view)
        {

            var viewProj = view * _proj;

            ImmediateContext.InputAssembler.SetVertexBuffers(0, new D3D11.VertexBufferBinding(_groundVB, VertexPN.Stride, 0));
            ImmediateContext.InputAssembler.SetIndexBuffer(_groundIB, DXGI.Format.R32_UInt, 0);

            int len;

            for (int p = 0; p < _tech.Description.PassCount; p++)
            {

                var wvp = _groundWorld * view * _proj;
                _fxWVP.SetMatrix(wvp);

                _fxClipPlane.Set(clipPlane);

                var invTranspose = Matrix.Invert(Matrix.Transpose(_groundWorld));

                _fxWIT.SetMatrix(invTranspose);
                _fxWorld.SetMatrix(_groundWorld);
                var array = Util.GetArray(_landMaterial, out len);
                _fxMaterial.SetRawValue(DataStream.Create<byte>(array, false, false), len);

                _fxDiffuseMap.SetResource(_groundMapSRV);

                var pass = _tech.GetPassByIndex(p);
                pass.Apply(ImmediateContext);

                ImmediateContext.DrawIndexed(_groundIndexCount, 0, 0);
                _fxDiffuseMap.SetResource(null);

                _fxClipPlane.Set(new float[] { 0, 0, 0, 1 });

            }

        }


        public void DrawBall()
        {
            DrawBall(new Vector4(0, 0, 0, 1), _view);
        }

        public void DrawBall(Vector4 clipPlane, Matrix view)
        {
            var viewProj = view * _proj;

            ImmediateContext.InputAssembler.SetVertexBuffers(0, new D3D11.VertexBufferBinding(_ballVB, VertexPN.Stride, 0));
            ImmediateContext.InputAssembler.SetIndexBuffer(_ballIB, DXGI.Format.R32_UInt, 0);

            int len;

            for (int p = 0; p < _tech.Description.PassCount; p++)
            {

                var wvp = _ballWorld * view * _proj;
                _fxWVP.SetMatrix(wvp);

                _fxClipPlane.Set(clipPlane);

                var invTranspose = Matrix.Invert(Matrix.Transpose(_ballWorld));

                _fxWIT.SetMatrix(invTranspose);
                _fxWorld.SetMatrix(_ballWorld);
                var array = Util.GetArray(_landMaterial, out len);
                _fxMaterial.SetRawValue(DataStream.Create<byte>(array, false, false), len);



                var pass = _tech.GetPassByIndex(p);
                pass.Apply(ImmediateContext);

                ImmediateContext.DrawIndexed(_ballIndexCount, 0, 0);
                _fxDiffuseMap.SetResource(null);

                _fxClipPlane.Set(new float[] { 0, 0, 0, 1 });

            }
        }

        public void DrawWater()
        {
            int len;
            ImmediateContext.InputAssembler.SetVertexBuffers(0, new D3D11.VertexBufferBinding(_wavesVB, VertexPN.Stride, 0));
            ImmediateContext.InputAssembler.SetIndexBuffer(_wavesIB, DXGI.Format.R32_UInt, 0);
            var invTranspose = Matrix.Invert(Matrix.Transpose(_wavesWorld));


            for (int p = 0; p < _tech.Description.PassCount; p++)
            {

                ImmediateContext.OutputMerger.BlendState = _alphaBlend;

                _fxClipPlane.Set(new float[] { 0, 0, 0, 1 });
                _fxWVP.SetMatrix(_wavesWorld * _view * _proj);
                _fxUseStructBuf.Set(true);

                _fxWIT.SetMatrix(invTranspose);
                _fxWorld.SetMatrix(_wavesWorld);
                var array = Util.GetArray(_wavesMaterial, out len);
                _fxMaterial.SetRawValue(DataStream.Create<byte>(array, false, false), len);


                _fxReflectViewProj.SetMatrix(_wavesWorld * _reflectView * _proj);

                _fxReflectiveMap.SetResource(reflectResourceView);
                _fxRefractiveMap.SetResource(refractResourceView);
                _fxgRefractionPositionMap.SetResource(positionMapResourceView);
                _fxSolutionSR.SetResource(_awave.CurrentSolutionSRV);

                var pass = _tech.GetPassByIndex(p);

                pass.Apply(ImmediateContext);
                ImmediateContext.DrawIndexed(3 * _awave.TriangleCount, 0, 0);

                ImmediateContext.Rasterizer.State = null;
                _fxRefractiveMap.SetResource(null);
                _fxReflectiveMap.SetResource(null);
                _fxgRefractionPositionMap.SetResource(null);
                _fxSolutionSR.SetResource(null);
                _fxUseStructBuf.Set(false);
            }

        }

        //We need to draw the scene separately, from the viewpoint of reflection camera, in order to get reflection on water surface. This method draws thus required Reflection Map
        public void RenderReflectionMap()
        {
            ImmediateContext.ClearRenderTargetView(reflectRenderTargetView, Color.LightSteelBlue);
            ImmediateContext.ClearDepthStencilView(DepthStencilView, D3D11.DepthStencilClearFlags.Depth | D3D11.DepthStencilClearFlags.Stencil, 1.0f, 0);

            ImmediateContext.OutputMerger.SetTargets(DepthStencilView, reflectRenderTargetView);

            DrawLand(new Vector4(0, 1, 0, 4), _reflectView);
            DrawBall(new Vector4(0, 0, 0, 1), _reflectView);

            ImmediateContext.OutputMerger.SetTargets(DepthStencilView, RenderTargetView);

        }

        //We need to draw the scene separately, involving only whats below water surface, from the point of view of camera, in order to visualize refraction. 
        public void RenderRefractionMap()
        {
            ImmediateContext.ClearRenderTargetView(refractRenderTargetView, Color.LightSteelBlue);
            ImmediateContext.ClearDepthStencilView(DepthStencilView, D3D11.DepthStencilClearFlags.Depth | D3D11.DepthStencilClearFlags.Stencil, 1.0f, 0);
            ImmediateContext.ClearRenderTargetView(positionMapRenderTargetView, Color.Black);

            ImmediateContext.OutputMerger.SetTargets(DepthStencilView, 2, new D3D11.RenderTargetView[] { refractRenderTargetView, positionMapRenderTargetView });
            DrawLand();


            ImmediateContext.OutputMerger.SetTargets(DepthStencilView, RenderTargetView);


        }

        public override void DrawScene()
        {
            base.DrawScene();

            //First update reflection and refraction maps
            RenderRefractionMap();
            RenderReflectionMap();

            ImmediateContext.ClearRenderTargetView(RenderTargetView, Color.LightSteelBlue);
            ImmediateContext.ClearDepthStencilView(DepthStencilView, D3D11.DepthStencilClearFlags.Depth | D3D11.DepthStencilClearFlags.Stencil, 1.0f, 0);

            ImmediateContext.InputAssembler.InputLayout = _inputLayout;
            ImmediateContext.InputAssembler.PrimitiveTopology = D3D.PrimitiveTopology.TriangleList;

            int len;
            var array = Util.GetArray(_dirLight, out len);

            //Pass lights data and position of eye (used for specular lighting)
            _fxDirLight.SetRawValue(DataStream.Create<byte>(array, false, false), len);
            array = Util.GetArray(_pointLight, out len);
            _fxPointLight.SetRawValue(DataStream.Create<byte>(array, false, false), len);
            array = Util.GetArray(_spotLight, out len);
            _fxSpotLight.SetRawValue(DataStream.Create<byte>(array, false, false), len);

            _fxEyePosW.Set(_eyePosW);

            DrawLand();
            DrawWater();
            DrawBall();


            SwapChain.Present(0, DXGI.PresentFlags.None);

        }


        //Camera management
        protected override void OnMouseDown(object sender, MouseEventArgs mouseEventArgs)
        {
            _lastMousePos = mouseEventArgs.Location;
            Window.Capture = true;
        }
        protected override void OnMouseUp(object sender, MouseEventArgs e)
        {
            Window.Capture = false;
        }
        protected override void OnMouseMove(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                var dx = MathF.ToRadians(0.25f * (e.X - _lastMousePos.X));
                var dy = MathF.ToRadians(0.25f * (e.Y - _lastMousePos.Y));

                _theta += dx;
                _phi += dy;

                _phi = MathF.Clamp(_phi, 0.1f, MathF.PI - 0.1f);
            }
            else if (e.Button == MouseButtons.Right)
            {
                var dx = 0.2f * (e.X - _lastMousePos.X);
                var dy = 0.2f * (e.Y - _lastMousePos.Y);
                _radius += dx - dy;

                _radius = MathF.Clamp(_radius, 50.0f, 500.0f);
            }
            _lastMousePos = e.Location;
        }



        protected override void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    Util.ReleaseCom(ref _groundMapSRV);
                    Util.ReleaseCom(ref _groundMap);

                    Util.ReleaseCom(ref reflectRenderTargetView);
                    Util.ReleaseCom(ref reflectResourceView);
                    Util.ReleaseCom(ref reflectText);

                    Util.ReleaseCom(ref refractRenderTargetView);
                    Util.ReleaseCom(ref refractResourceView);
                    Util.ReleaseCom(ref refractText);

                    Util.ReleaseCom(ref positionMapRenderTargetView);
                    Util.ReleaseCom(ref positionMapResourceView);
                    Util.ReleaseCom(ref positionMapText);

                    Util.ReleaseCom(ref _awave);

                    Util.ReleaseCom(ref _groundVB);
                    Util.ReleaseCom(ref _groundIB);

                    Util.ReleaseCom(ref _ballVB);
                    Util.ReleaseCom(ref _ballIB);

                    Util.ReleaseCom(ref _wavesVB);
                    Util.ReleaseCom(ref _wavesIB);

                    Util.ReleaseCom(ref _fxDiffuseMap);
                    Util.ReleaseCom(ref _fx);

                    Util.ReleaseCom(ref _inputLayout);

                    Util.ReleaseCom(ref _alphaBlend);
                }
                _disposed = true;
            }
            base.Dispose(disposing);
        }



        
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            WavesSimulator app = new WavesSimulator(System.Diagnostics.Process.GetCurrentProcess().Handle);
            if (!app.Init())
                MessageBox.Show("App Failed to Initialize");
            app.Run();

            app.Dispose();

        }
    }
}
