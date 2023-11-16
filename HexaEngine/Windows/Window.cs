﻿namespace HexaEngine.Windows
{
    using HexaEngine.Core;
    using HexaEngine.Core.Audio;
    using HexaEngine.Core.Debugging;
    using HexaEngine.Core.Graphics;
    using HexaEngine.Core.UI;
    using HexaEngine.Core.Windows;
    using HexaEngine.Core.Windows.Events;
    using HexaEngine.Editor;
    using HexaEngine.Mathematics;
    using HexaEngine.Rendering.Renderers;
    using HexaEngine.Resources;
    using HexaEngine.Resources.Factories;
    using HexaEngine.Scenes;
    using HexaEngine.Scenes.Managers;
    using Microsoft.Extensions.DependencyInjection;
    using System;
    using System.Numerics;

    /// <summary>
    /// Represents the application window that implements the <see cref="IRenderWindow"/> interface.
    /// </summary>
    public class Window : SdlWindow, IRenderWindow
    {
        private ThreadDispatcher renderDispatcher;
        private bool running;
        private bool resetTime;
        private IAudioDevice audioDevice;
        private IGraphicsDevice graphicsDevice;
        private IGraphicsContext graphicsContext;
        private ISwapChain swapChain;
        private Frameviewer frameviewer;
        private bool imGuiWidgets;
        private SceneRenderer sceneRenderer;
        private Task initTask;
        private bool rendererInitialized;
        private bool resize = false;

        private Thread updateThread;

        private readonly Barrier syncBarrier = new(2);

        private ImGuiManager? imGuiRenderer;

        /// <summary>
        /// Gets or sets the rendering flags for the window.
        /// </summary>
        public RendererFlags Flags;

        /// <summary>
        /// Gets the thread dispatcher associated with the rendering.
        /// </summary>
        public ThreadDispatcher Dispatcher => renderDispatcher;

        /// <summary>
        /// Gets the graphics device used by the window.
        /// </summary>
        public IGraphicsDevice Device => graphicsDevice;

        /// <summary>
        /// Gets the graphics context associated with the window.
        /// </summary>
        public IGraphicsContext Context => graphicsContext;

        /// <summary>
        /// Gets the audio device used by the window.
        /// </summary>
        public IAudioDevice AudioDevice => audioDevice;

        /// <summary>
        /// Gets the swap chain associated with the window.
        /// </summary>
        public ISwapChain SwapChain => swapChain;

        /// <summary>
        /// Gets or sets the startup scene to be loaded.
        /// </summary>
        public string? StartupScene;

        private Viewport windowViewport;
        private Viewport renderViewport;

        /// <summary>
        /// Gets the viewport of the window.
        /// </summary>
        public Viewport WindowViewport => windowViewport;

        /// <summary>
        /// Gets the scene renderer associated with the window.
        /// </summary>
        public ISceneRenderer Renderer => sceneRenderer;

        /// <summary>
        /// Gets the viewport used for rendering.
        /// </summary>
        public Viewport RenderViewport => renderViewport;

#nullable disable

        /// <summary>
        /// Initializes a new instance of the <see cref="Window"/> class.
        /// </summary>
        public Window()
        {
        }

#nullable restore

        /// <summary>
        /// Initializes the window, including setting up rendering resources, audio, and other necessary components.
        /// </summary>
        /// <param name="audioDevice">The audio device to use for audio processing.</param>
        /// <param name="graphicsDevice">The graphics device to use for rendering.</param>
        public virtual void Initialize(IAudioDevice audioDevice, IGraphicsDevice graphicsDevice)
        {
            running = true;
            this.audioDevice = audioDevice;
            this.graphicsDevice = graphicsDevice;

            graphicsContext = graphicsDevice.Context;
            swapChain = graphicsDevice.CreateSwapChain(this) ?? throw new PlatformNotSupportedException();
            swapChain.Active = true;
            renderDispatcher = new(Thread.CurrentThread);

            if (Application.MainWindow == this)
            {
                // Initialize AudioManager if this is the main window
                AudioManager.Initialize(audioDevice);

                // Setup shared resource manager and its descriptors
                ServiceCollection descriptors = new();
                descriptors.AddSingleton<IResourceFactory, GraphicsPipelineResourceFactory>();
                descriptors.AddSingleton<IResourceFactory, MaterialResourceFactory>();
                descriptors.AddSingleton<IResourceFactory, MaterialShaderResourceFactory>();
                descriptors.AddSingleton<IResourceFactory, MaterialTextureResourceFactory>();
                descriptors.AddSingleton<IResourceFactory, MeshResourceFactory>();
                descriptors.AddSingleton<IResourceFactory, TerrainMaterialResourceFactory>();

                ResourceManager.Shared = new("Shared", graphicsDevice, audioDevice, descriptors);
                PipelineManager.Initialize(graphicsDevice);
            }

            // Initialize Designer if in editor mode
            if (Application.InEditorMode)
            {
                Designer.Init(graphicsDevice);
            }

            frameviewer = new(graphicsDevice);

            imGuiWidgets = (Flags & RendererFlags.ImGuiWidgets) != 0;

            // Initialize ImGui renderer if ImGui flag is set
            if ((Flags & RendererFlags.ImGui) != 0)
            {
                imGuiRenderer = new(this, graphicsDevice, graphicsContext);
            }

            // Initialize WindowManager if ImGuiWidgets flag is set
            if ((Flags & RendererFlags.ImGuiWidgets) != 0)
            {
                WindowManager.Init(graphicsDevice);
            }

            // Subscribe to the SceneChanged event
            SceneManager.SceneChanged += SceneChanged;

            // Invoke virtual method for additional renderer-specific initialization
            OnRendererInitialize(graphicsDevice);

            // Create and initialize the scene renderer
            sceneRenderer = new(Flags);
            initTask = sceneRenderer.Initialize(graphicsDevice, swapChain, this);
            initTask.ContinueWith(x =>
            {
                if (x.IsCompletedSuccessfully)
                {
                    Logger.Info("Renderer: Initialized");
                }
                if (x.IsFaulted)
                {
                    Logger.Error("Renderer: Failed Initialize");
                    Logger.Log(x.Exception);
                }

                // Set the render viewport based on the initialized scene renderer
                renderViewport = new(sceneRenderer.Width, sceneRenderer.Height);

                rendererInitialized = true;
            });

            // Load the specified startup scene if provided
            if (StartupScene != null)
            {
#nullable disable // Scene cant be null here, unless something bad happend like corrupted files, but I dont care.
                SceneManager.Load(StartupScene);
                SceneManager.Current.IsSimulating = true;
#nullable restore
            }

            // Create and start the scene update worker thread
            updateThread = new(UpdateScene);
            updateThread.Name = "Scene Update Worker";
            updateThread.Start();
        }

        /// <summary>
        /// Continuously updates the current scene while the application is running.
        /// </summary>
        private void UpdateScene()
        {
            while (running)
            {
#if PROFILE
                // Signal and wait for synchronization if profiling is enabled
                syncBarrier.SignalAndWait();
#endif
                // Update the current scene
                SceneManager.Current?.Update();

                // Signal and wait for synchronization with the main thread.
                syncBarrier.SignalAndWait();
            }
        }

        /// <summary>
        /// Event handler called when the active scene in the <see cref="SceneManager"/> changes.
        /// </summary>
        /// <param name="sender">The sender of the event.</param>
        /// <param name="e">The event arguments containing information about the scene change.</param>
        private void SceneChanged(object? sender, SceneChangedEventArgs e)
        {
            resetTime = true;
        }

        /// <summary>
        /// Renders the content of the window using the specified graphics context.
        /// </summary>
        /// <param name="context">The graphics context.</param>
        public void Render(IGraphicsContext context)
        {
#if PROFILE
            // Begin profiling frame and total time if profiling is enabled.
            Device.Profiler.BeginFrame();
            Device.Profiler.Begin(Context, "Total");
            sceneRenderer.Profiler.BeginFrame();
            sceneRenderer.Profiler.Begin("Total");
#endif
#if PROFILE
            // Signal and wait for synchronization if profiling is enabled.
            syncBarrier.SignalAndWait();
#endif
            // Resize the swap chain if necessary.
            if (resize)
            {
                swapChain.Resize(Width, Height);
                resize = false;
            }

            // Initialize time if requested.
            if (resetTime)
            {
                Time.Initialize();
                resetTime = false;
            }

            // Clear depth-stencil and render target views.
            context.ClearDepthStencilView(swapChain.BackbufferDSV, DepthStencilClearFlags.Depth | DepthStencilClearFlags.Stencil, 1, 0);
            context.ClearRenderTargetView(swapChain.BackbufferRTV, Vector4.Zero);

            // Execute rendering commands from the render dispatcher.
            renderDispatcher.ExecuteQueue();

            // Start ImGui frame rendering.
            imGuiRenderer?.NewFrame();

            // Invoke virtual method for pre-render operations.
            OnRenderBegin(context);

            // Determine if rendering should occur based on initialization status.
            var drawing = rendererInitialized;

            // Check if ImGui widgets should be rendered and the application is in editor mode.
            if (imGuiWidgets && Application.InEditorMode)
            {
                // Update and draw the frame viewer.
                frameviewer.SourceViewport = Viewport;
                frameviewer.Update();
                frameviewer.Draw();
                drawing &= frameviewer.IsVisible;
                windowViewport = Application.InEditorMode ? frameviewer.RenderViewport : Viewport;

                // Set the camera for DebugDraw based on the current camera's view projection matrix.

#nullable disable // cant be null because CameraManager.Current would be the editor camera because of Application.InEditorMode.
                DebugDraw.SetCamera(CameraManager.Current.Transform.ViewProjection);
#nullable restore
            }

            // Wait for swap chain presentation.
            swapChain.WaitForPresent();

            // Check if rendering should occur based on the active scene.
            drawing &= SceneManager.Current is not null;

            // Render the scene if drawing is enabled.
            if (drawing)
            {
#nullable disable // a few lines above there is a null check.
                SceneManager.Current.GraphicsUpdate(context);
#nullable restore
                sceneRenderer.Render(context, this, windowViewport, SceneManager.Current, CameraManager.Current);
            }

            // Draw additional elements like Designer, WindowManager, ImGuiConsole, MessageBoxes, etc.
            Designer.Draw();
            WindowManager.Draw(context);
            ImGuiConsole.Draw();
            MessageBoxes.Draw();

            // Invoke virtual method for post-render operations.
            OnRender(context);

            // Set the render target to swap chain backbuffer.
            context.SetRenderTarget(swapChain.BackbufferRTV, null);

#if PROFILE
            // Begin profiling ImGui if profiling is enabled.
            Device.Profiler.Begin(Context, "ImGui");
            sceneRenderer.Profiler.Begin("ImGui");
#endif
            // End the ImGui frame rendering.
            imGuiRenderer?.EndFrame();
#if PROFILE
            // End profiling ImGui if profiling is enabled.
            sceneRenderer.Profiler.End("ImGui");
            Device.Profiler.End(Context, "ImGui");
#endif
            // Invoke virtual method for post-render operations.
            OnRenderEnd(context);

            // Present and swap buffers.
            swapChain.Present();

            // Wait for swap chain presentation to complete.
            swapChain.Wait();

            // Signal and wait for synchronization with the update thread.
            syncBarrier.SignalAndWait();

#if PROFILE
            // End profiling frame and total time if profiling is enabled.
            sceneRenderer.Profiler.End("Total");
            Device.Profiler.End(Context, "Total");
            Device.Profiler.EndFrame(context);
#endif
        }

        /// <summary>
        /// Uninitializes the window, releasing resources and stopping rendering.
        /// </summary>
        public virtual void Uninitialize()
        {
            // Set the running flag to false.
            running = false;

            // Invoke virtual method for disposing renderer-specific resources.
            OnRendererDispose();

            // Remove the participant from the synchronization barrier and join the update thread.
            syncBarrier.RemoveParticipant();
            updateThread.Join();

            // Dispose the profiler associated with the graphics device.
            Device.Profiler.Dispose();

            // Dispose of resources related to ImGui widgets if enabled.
            if (Flags.HasFlag(RendererFlags.ImGuiWidgets))
            {
                WindowManager.Dispose();
            }

            // Dispose of the ImGui renderer if it exists.
            if (imGuiRenderer is not null)
            {
                imGuiRenderer?.Dispose();
            }

            // Unload the scene manager and wait for initialization task completion if not already completed.
            SceneManager.Unload();
            if (!initTask.IsCompleted)
            {
                initTask.Wait();
            }

            // Dispose of the scene renderer, render dispatcher, shared resource manager, audio manager, swap chain,
            // graphics context, and graphics device.
            sceneRenderer.Dispose();
            renderDispatcher.Dispose();
            ResourceManager.Shared.Dispose();
            AudioManager.Release();
            swapChain.Dispose();
            graphicsContext.Dispose();
            graphicsDevice.Dispose();
        }

        /// <summary>
        /// Called when [renderer initialize].
        /// </summary>
        /// <param name="device">The device.</param>
        protected virtual void OnRendererInitialize(IGraphicsDevice device)
        {
        }

        /// <summary>
        /// Called when [render begin].
        /// </summary>
        /// <param name="context">The context.</param>
        protected virtual void OnRenderBegin(IGraphicsContext context)
        {
        }

        /// <summary>
        /// Called when [render].
        /// </summary>
        /// <param name="context">The context.</param>
        protected virtual void OnRender(IGraphicsContext context)
        {
        }

        /// <summary>
        /// Called when [render end].
        /// </summary>
        /// <param name="context">The context.</param>
        protected virtual void OnRenderEnd(IGraphicsContext context)
        {
        }

        /// <summary>
        /// Called when [renderer dispose].
        /// </summary>
        protected virtual void OnRendererDispose()
        {
        }

        /// <summary>
        /// Raises the <see cref="E:HexaEngine.Core.Windows.SdlWindow.Resized" /> event.
        /// </summary>
        /// <param name="args">The event arguments.</param>
        protected override void OnResized(ResizedEventArgs args)
        {
            resize = true;
            base.OnResized(args);
        }
    }
}