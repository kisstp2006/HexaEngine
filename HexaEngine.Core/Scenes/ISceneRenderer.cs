﻿namespace HexaEngine.Core.Scenes
{
    using HexaEngine.Core.Graphics;
    using HexaEngine.Core.Lights;
    using HexaEngine.Core.PostFx;
    using HexaEngine.Core.Windows;
    using HexaEngine.Mathematics;
    using System;

    public interface ISceneRenderer : IDisposable
    {
        CPUProfiler Profiler { get; }

        PostProcessingManager PostProcessing { get; }

        ViewportShading Shading { get; set; }

        Task Initialize(IGraphicsDevice device, ISwapChain swapChain, IRenderWindow window);

        void Render(IGraphicsContext context, IRenderWindow window, Viewport viewport, Scene scene, Camera camera);

        void DrawSettings();
    }
}