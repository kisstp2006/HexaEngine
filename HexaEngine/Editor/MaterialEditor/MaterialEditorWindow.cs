﻿namespace HexaEngine.Editor.MaterialEditor
{
    using HexaEngine.Core;
    using HexaEngine.Core.Debugging;
    using HexaEngine.Core.Graphics;
    using HexaEngine.Core.Graphics.Buffers;
    using HexaEngine.Core.Graphics.Primitives;
    using HexaEngine.Core.Input;
    using HexaEngine.Core.IO;
    using HexaEngine.Core.IO.Materials;
    using HexaEngine.Core.IO.Metadata;
    using HexaEngine.Core.UI;
    using HexaEngine.Editor.Dialogs;
    using HexaEngine.Editor.MaterialEditor.Generator;
    using HexaEngine.Editor.MaterialEditor.Generator.Enums;
    using HexaEngine.Editor.MaterialEditor.Nodes;
    using HexaEngine.Editor.MaterialEditor.Nodes.Functions;
    using HexaEngine.Editor.MaterialEditor.Nodes.Textures;
    using HexaEngine.Editor.NodeEditor;
    using HexaEngine.ImGuiNET;
    using HexaEngine.Lights;
    using HexaEngine.Lights.Structs;
    using HexaEngine.Lights.Types;
    using HexaEngine.Mathematics;
    using HexaEngine.Scenes;
    using System.IO;
    using System.Numerics;
    using System.Reflection;
    using System.Text;

    public class MaterialEditorWindow : EditorWindow
    {
        private readonly OpenFileDialog openFileDialog = new(null, ".matlib");
        private readonly SaveFileDialog saveFileDialog = new(null, ".matlib");

        private const string MetadataKey = "MatNodes.Data";

        private readonly MaterialLibraryWindow libraryWindow;

        private NodeEditor? editor;
        private InputNode geometryNode;
        private BRDFShadingModelNode outputNode;

        private List<TextureFileNode> textureFiles = new();

        private Sphere sphere;

        private ConstantBuffer<Matrix4x4> world;
        private ConstantBuffer<CBCamera> view;

        private StructuredBuffer<LightData> lights;
        private ConstantBuffer<DeferredLightParams> lightParams;

        private PointLight pointLight = new();

        private DepthStencil depthStencil;
        private Texture2D textureTonemap;
        private Texture2D texturePreview;
        private Texture2D iblDFG;

        private IGraphicsDevice device;
        private IGraphicsPipeline? pipeline;
        private ISamplerState sampler;
        private IGraphicsPipeline dfg;
        private IGraphicsPipeline tonemap;
        private IGraphicsPipeline fxaa;

        private Vector3 sc = new(2, 0, 0);
        private const float speed = 1;
        private static bool first = true;

        private CameraTransform camera = new();
        private (string, Type)[] intrinsicFuncs;
        private (string, Type)[] operatorFuncs;
        private readonly ShaderGenerator generator = new();
        private bool autoGenerate = true;
        private volatile bool compiling;
        private readonly SemaphoreSlim semaphore = new(1);

        private string? currentFile;
        private MaterialLibrary? materialLibrary;
        private MaterialData? material;

        public MaterialEditorWindow()
        {
            IsShown = true;
            Flags = ImGuiWindowFlags.MenuBar;
            libraryWindow = new(this);
        }

        public string? CurrentFile => currentFile;

        public MaterialLibrary? MaterialLibrary => materialLibrary;

        public MaterialData? Material
        {
            get => material;
            set
            {
                if (device == null || materialLibrary == null)
                    return;

                if (!materialLibrary.Materials.Contains(value))
                    return;

                if (editor != null)
                {
                    editor.NodePinValueChanged -= NodePinValueChanged;
                    editor.LinkAdded -= LinkAdded;
                    editor.LinkRemoved -= LinkRemoved;
                    editor.Destroy();
                    editor = null;
                }

                material = value;

                if (value == null)
                {
                    return;
                }

                var json = value.Metadata.GetOrAdd<MetadataStringEntry>(MetadataKey).Value;

                try
                {
                    if (string.IsNullOrEmpty(json))
                    {
                        editor = new();
                        geometryNode = new(editor.GetUniqueId(), false, false);
                        outputNode = new(editor.GetUniqueId(), false, false);
                        editor.AddNode(geometryNode);
                        editor.AddNode(outputNode);
                        editor.Initialize();
                    }
                    else
                    {
                        editor = NodeEditor.Deserialize(json);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to deserialize node material data: {value.Name}", ex.Message);
                    Logger.Error($"Failed to deserialize node material data: {value.Name}");
                    Logger.Log(ex);
                    return;
                }

                editor.Minimap = true;
                editor.Location = ImNodesNET.ImNodesMiniMapLocation.TopRight;
                editor.NodePinValueChanged += NodePinValueChanged;
                editor.LinkAdded += LinkAdded;
                editor.LinkRemoved += LinkRemoved;
                geometryNode = editor.GetNode<InputNode>();
                outputNode = editor.GetNode<BRDFShadingModelNode>();

                foreach (var tex in editor.GetNodes<TextureFileNode>())
                {
                    tex.Device = device;
                    tex.Reload();
                    textureFiles.Add(tex);
                }
            }
        }

        protected override string Name { get; } = "Material Editor";

        public void Load(string filename)
        {
            try
            {
                materialLibrary = MaterialLibrary.LoadExternal(filename);
                currentFile = filename;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to load material library: {filename}", ex.Message);
                Logger.Error($"Failed to load material library: {filename}");
                Logger.Log(ex);
            }
        }

        public void Unload()
        {
            Material = null;
            materialLibrary = null;
            currentFile = null;
        }

        public void Save()
        {
            if (currentFile == null)
                return;

            SaveAs(currentFile);
        }

        public void CreateNew()
        {
            materialLibrary = new();
        }

        public void SaveAs(string filename)
        {
            if (materialLibrary == null)
                return;

            if (material != null && editor != null)
            {
                material.Metadata.GetOrAdd<MetadataStringEntry>(MetadataKey).Value = editor.Serialize();
            }

            try
            {
                materialLibrary.Save(filename, Encoding.UTF8);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to save material library: {filename}", ex.Message);
                Logger.Error($"Failed to save material library: {filename}");
                Logger.Log(ex);
            }
        }

        protected override void InitWindow(IGraphicsDevice device)
        {
            this.device = device;
            libraryWindow.Init(device);
            generator.OnPreBuildTable += Generator_OnPreBuildTable;

            if (material != null)
            {
                Material = material;
            }

            sphere = new(device);
            world = new(device, Matrix4x4.Transpose(Matrix4x4.Identity), CpuAccessFlags.None);
            view = new(device, CpuAccessFlags.Write);
            lightParams = new(device, CpuAccessFlags.Write);
            lights = new(device, CpuAccessFlags.Write);
            pointLight.Transform.Position = new(10, 10, -10);
            pointLight.Transform.Recalculate();
            lights.Add(new(pointLight));

            depthStencil = new(device, 1024, 1024, Format.D32Float);
            textureTonemap = new(device, Format.R16G16B16A16Float, 1024, 1024, 1, 1, CpuAccessFlags.None, GpuAccessFlags.RW);
            texturePreview = new(device, Format.R16G16B16A16Float, 1024, 1024, 1, 1, CpuAccessFlags.None, GpuAccessFlags.RW);
            sampler = device.CreateSamplerState(SamplerStateDescription.LinearClamp);
            tonemap = device.CreateGraphicsPipeline(new()
            {
                VertexShader = "quad.hlsl",
                PixelShader = "effects/tonemap/ps.hlsl"
            }, GraphicsPipelineState.DefaultFullscreen);
            fxaa = device.CreateGraphicsPipeline(new()
            {
                VertexShader = "quad.hlsl",
                PixelShader = "effects/fxaa/ps.hlsl"
            }, GraphicsPipelineState.DefaultFullscreen);

            camera.Fov = 90;
            camera.Width = 1;
            camera.Height = 1;

            intrinsicFuncs = Assembly.GetExecutingAssembly().GetTypes().AsParallel().Where(x => x.BaseType == typeof(FuncCallNodeBase)).Select(x => (x.Name.Replace("Node", string.Empty), x)).ToArray();
            intrinsicFuncs = Assembly.GetExecutingAssembly().GetTypes().AsParallel().Where(x => x.BaseType == typeof(FuncCallVoidNodeBase)).Select(x => (x.Name.Replace("Node", string.Empty), x)).ToArray().Union(intrinsicFuncs).OrderBy(x => x.Item1).ToArray();
            operatorFuncs = Assembly.GetExecutingAssembly().GetTypes().AsParallel().Where(x => x.BaseType == typeof(FuncOperatorBaseNode)).Select(x => (x.Name.Replace("Node", string.Empty), x)).ToArray().OrderBy(x => x.Item1).ToArray();

            dfg = device.CreateGraphicsPipeline(new()
            {
                VertexShader = "quad.hlsl",
                PixelShader = "effects/dfg/ps.hlsl"
            }, new ShaderMacro[2] { new("MULTISCATTER", false ? "1" : "0"), new("CLOTH", true ? "1" : "0") });

            iblDFG = new(device, Format.R16G16B16A16Float, 128, 128, 1, 1, CpuAccessFlags.None, GpuAccessFlags.RW);
            var context = device.Context;
            context.SetRenderTarget(iblDFG.RTV, null);
            context.SetViewport(new(128, 128));
            context.SetGraphicsPipeline(dfg);
            context.DrawInstanced(4, 1, 0, 0);
            context.SetGraphicsPipeline(null);
            context.SetRenderTarget(null, null);
        }

        private void LinkRemoved(object? sender, Link e)
        {
            if (autoGenerate)
                Generate();
        }

        private void LinkAdded(object? sender, Link e)
        {
            if (autoGenerate)
                Generate();
        }

        private void NodePinValueChanged(object? sender, Pin e)
        {
            if (autoGenerate)
                Generate();
        }

        private void Generator_OnPreBuildTable(VariableTable table)
        {
            table.SetBaseSrv(2);
            table.SetBaseSampler(2);
        }

        protected override void DisposeCore()
        {
            libraryWindow.Dispose();

            if (editor != null && material != null)
            {
                material.Metadata.GetOrAdd<MetadataStringEntry>(MetadataKey).Value = editor.Serialize();
                editor.Destroy();
                editor = null;
            }

            sphere.Dispose();

            world.Dispose();
            view.Dispose();

            lights.Dispose();
            lightParams.Dispose();

            depthStencil.Dispose();
            textureTonemap.Dispose();
            texturePreview.Dispose();
            iblDFG.Dispose();

            pipeline?.Dispose();
            sampler.Dispose();
            dfg.Dispose();
            tonemap.Dispose();
            fxaa.Dispose();
        }

        public override void DrawContent(IGraphicsContext context)
        {
            if (openFileDialog.Draw())
            {
                if (openFileDialog.Result == OpenFileResult.Ok)
                {
                    Load(openFileDialog.FullPath);
                }
            }

            if (saveFileDialog.Draw())
            {
                if (saveFileDialog.Result == SaveFileResult.Ok)
                {
                    SaveAs(saveFileDialog.FullPath);
                }
            }

            libraryWindow.DrawWindow(context);

            DrawNodesWindow(context);
            DrawPreviewWindow(context);

            if (ImGui.BeginMenuBar())
            {
                if (ImGui.BeginMenu("File"))
                {
                    if (ImGui.MenuItem("New"))
                    {
                        CreateNew();
                    }

                    if (ImGui.MenuItem("Load"))
                    {
                        openFileDialog.Show();
                    }

                    ImGui.Separator();

                    if (ImGui.MenuItem("Save"))
                    {
                        Save();
                    }

                    if (ImGui.MenuItem("Save as"))
                    {
                        Save();
                    }

                    ImGui.Separator();

                    if (ImGui.MenuItem("Close"))
                    {
                        Unload();
                    }

                    ImGui.EndMenu();
                }
                if (ImGui.MenuItem("Generate"))
                {
                    Generate();
                }

                ImGui.Checkbox("Auto", ref autoGenerate);

                ImGui.EndMenuBar();
            }

            editor?.Draw();
        }

        private void DrawNodesWindow(IGraphicsContext context)
        {
            if (editor == null)
            {
                ImGui.Text("No material open");
                return;
            }
            if (!ImGui.Begin("Add node"))
            {
                ImGui.End();
                return;
            }

            if (ImGui.CollapsingHeader("Textures"))
            {
                if (ImGui.MenuItem("Texture File"))
                {
                    TextureFileNode node = new(editor.GetUniqueId(), true, false, context.Device);
                    editor.AddNode(node);
                    textureFiles.Add(node);
                }
            }

            if (ImGui.CollapsingHeader("Methods"))
            {
                if (ImGui.MenuItem("Normal Map"))
                {
                    NormalMapNode node = new(editor.GetUniqueId(), true, false);
                    editor.AddNode(node);
                }
            }

            if (ImGui.CollapsingHeader("Constants"))
            {
                if (ImGui.MenuItem("Converter"))
                {
                    ConvertNode node = new(editor.GetUniqueId(), true, false);
                    editor.AddNode(node);
                }
                if (ImGui.MenuItem("Constant"))
                {
                    ConstantNode node = new(editor.GetUniqueId(), true, false);
                    editor.AddNode(node);
                }
                if (ImGui.MenuItem("Component Mask"))
                {
                    ComponentMaskNode node = new(editor.GetUniqueId(), true, false);
                    editor.AddNode(node);
                }
                if (ImGui.MenuItem("Components to Vector"))
                {
                    PackNode node = new(editor.GetUniqueId(), true, false);
                    editor.AddNode(node);
                }
                if (ImGui.MenuItem("Vector to Components"))
                {
                    SplitNode node = new(editor.GetUniqueId(), true, false);
                    editor.AddNode(node);
                }
                if (ImGui.MenuItem("Cam pos"))
                {
                    CamPosNode node = new(editor.GetUniqueId(), true, false);
                    editor.AddNode(node);
                }
            }

            if (ImGui.CollapsingHeader("Operators"))
            {
                for (int i = 0; i < operatorFuncs.Length; i++)
                {
                    var func = operatorFuncs[i];
                    if (ImGui.MenuItem(func.Item1))
                    {
                        Node node = (Node)Activator.CreateInstance(func.Item2, editor.GetUniqueId(), true, false);
                        editor.AddNode(node);
                    }
                }
            }

            if (ImGui.CollapsingHeader("Intrinsic Functions"))
            {
                for (int i = 0; i < intrinsicFuncs.Length; i++)
                {
                    var func = intrinsicFuncs[i];
                    if (ImGui.MenuItem(func.Item1))
                    {
                        Node node = (Node)Activator.CreateInstance(func.Item2, editor.GetUniqueId(), true, false);
                        editor.AddNode(node);
                    }
                }
            }

            ImGui.End();
        }

        private void DrawPreviewWindow(IGraphicsContext context)
        {
            if (!ImGui.Begin("Preview"))
            {
                ImGui.End();
                return;
            }

            if (pipeline != null && pipeline.IsValid && pipeline.IsInitialized && !compiling)
            {
                view.Update(context, new(camera, new(1024, 1024)));
                lights.Update(context);
                lightParams.Update(context, new(lights.Count));

                context.ClearRenderTargetView(texturePreview.RTV, new(0, 0, 0, 1));
                context.ClearDepthStencilView(depthStencil.DSV, DepthStencilClearFlags.All, 1, 0);
                context.SetRenderTarget(texturePreview.RTV, depthStencil.DSV);
                context.PSSetShaderResource(0, lights.SRV);
                context.PSSetShaderResource(1, iblDFG.SRV);
                context.PSSetSampler(0, sampler);

                for (int i = 0; i < textureFiles.Count; i++)
                {
                    var tex = textureFiles[i];
                    if (generator.TextureMapping.TryGetValue(tex, out uint slot))
                    {
                        context.PSSetShaderResource(slot, tex.Image);
                        context.PSSetSampler(slot, tex.Sampler);
                    }
                }

                context.VSSetConstantBuffer(0, world);
                context.VSSetConstantBuffer(1, view);
                context.PSSetConstantBuffer(0, lightParams);
                context.PSSetConstantBuffer(1, view);
                context.SetViewport(new(1024, 1024));
                context.SetGraphicsPipeline(pipeline);

                sphere.DrawAuto(context);

                context.SetGraphicsPipeline(null);
                context.VSSetConstantBuffer(0, null);
                context.VSSetConstantBuffer(1, null);
                context.PSSetConstantBuffer(0, null);
                context.PSSetConstantBuffer(1, null);

                for (int i = 0; i < textureFiles.Count; i++)
                {
                    var tex = textureFiles[i];
                    if (generator.TextureMapping.TryGetValue(tex, out uint slot))
                    {
                        context.PSSetShaderResource(slot, null);
                        context.PSSetSampler(slot, null);
                    }
                }

                context.PSSetSampler(0, null);
                context.PSSetShaderResource(0, null);
                context.PSSetShaderResource(1, null);
                context.SetRenderTarget(null, null);

                context.ClearRenderTargetView(textureTonemap.RTV, new(0, 0, 0, 1));
                context.SetRenderTarget(textureTonemap.RTV, null);
                context.SetGraphicsPipeline(tonemap);
                context.PSSetShaderResource(0, texturePreview.SRV);
                context.PSSetSampler(0, sampler);
                context.DrawInstanced(4, 1, 0, 0);
                context.PSSetSampler(0, null);
                context.PSSetShaderResource(0, null);
                context.SetGraphicsPipeline(null);
                context.SetRenderTarget(null, null);

                context.SetRenderTarget(texturePreview.RTV, null);
                context.SetGraphicsPipeline(fxaa);
                context.PSSetShaderResource(0, textureTonemap.SRV);
                context.PSSetSampler(0, sampler);
                context.DrawInstanced(4, 1, 0, 0);
                context.PSSetSampler(0, null);
                context.PSSetShaderResource(0, null);
                context.SetGraphicsPipeline(null);
                context.SetRenderTarget(null, null);
            }
            var size = ImGui.GetContentRegionAvail();
            size.Y = size.X;
            ImGui.Image(texturePreview.SRV?.NativePointer ?? 0, size);

            if (ImGui.IsItemHovered() || first)
            {
                Vector2 delta = Vector2.Zero;
                if (Mouse.IsDown(MouseButton.Middle))
                {
                    delta = Mouse.Delta;
                }

                float wheel = 0;
                if (Keyboard.IsDown(Key.LCtrl))
                {
                    wheel = Mouse.DeltaWheel.Y * Time.Delta;
                }

                // Only update the camera's position if the mouse got moved in either direction
                if (delta.X != 0f || delta.Y != 0f || wheel != 0f || first)
                {
                    sc.X += sc.X / 2 * -wheel;

                    // Rotate the camera left and right
                    sc.Y += -delta.X * Time.Delta * speed;

                    // Rotate the camera up and down
                    // Prevent the camera from turning upside down (1.5f = approx. Pi / 2)
                    sc.Z = Math.Clamp(sc.Z + delta.Y * Time.Delta * speed, -MathF.PI / 2, MathF.PI / 2);

                    first = false;

                    // Calculate the cartesian coordinates
                    Vector3 pos = SphereHelper.GetCartesianCoordinates(sc);
                    var orientation = Quaternion.CreateFromYawPitchRoll(-sc.Y, sc.Z, 0);
                    camera.PositionRotation = (pos, orientation);
                    camera.Recalculate();
                }
            }

            ImGui.End();
        }

        private void Generate()
        {
            if (editor == null)
                return;

            semaphore.Wait();
            compiling = true;
            Directory.CreateDirectory("generated/" + "shaders/");

            IOSignature inputSig = new("Pixel",
                new SignatureDef("pos", new(VectorType.Float4)),
                new SignatureDef("uv", new(VectorType.Float3)),
                new SignatureDef("normal", new(VectorType.Float3)),
                new SignatureDef("tangent", new(VectorType.Float3)),
                new SignatureDef("binormal", new(VectorType.Float3)));

            IOSignature outputSig = new("Material",
                new SignatureDef("baseColor", new(VectorType.Float4)),
                new SignatureDef("normal", new(VectorType.Float3)),
                new SignatureDef("roughness", new(ScalarType.Float)),
                new SignatureDef("metallic", new(ScalarType.Float)),
                new SignatureDef("reflectance", new(ScalarType.Float)),
                new SignatureDef("ao", new(ScalarType.Float)),
                new SignatureDef("emissive", new(VectorType.Float4)));

            string result = generator.Generate(outputNode, editor.Nodes, "setupMaterial", false, false, inputSig, outputSig);

            File.WriteAllText("assets/generated/shaders/generated/usercodeMaterial.hlsl", result);
            FileSystem.Refresh();
            pipeline ??= device.CreateGraphicsPipeline(new()
            {
                PixelShader = "generated/main.hlsl",
                VertexShader = "generated/vs.hlsl",
            },
            new GraphicsPipelineState()
            {
                Blend = BlendDescription.Opaque,
                BlendFactor = default,
                DepthStencil = DepthStencilDescription.Default,
                Rasterizer = RasterizerDescription.CullBack,
                SampleMask = 0,
                StencilRef = 0,
                Topology = PrimitiveTopology.TriangleList
            });

            pipeline.Recompile();
            first = true;

            compiling = false;
            semaphore.Release();
        }
    }
}