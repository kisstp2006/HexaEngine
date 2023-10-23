﻿namespace HexaEngine.UI
{
    using HexaEngine.Core.Graphics;
    using HexaEngine.Core.Scenes;
    using HexaEngine.Culling;
    using HexaEngine.Lights;
    using HexaEngine.Mathematics;
    using HexaEngine.Rendering;

    public abstract class UIComponent : IRendererComponent
    {
        public uint QueueIndex { get; } = (uint)RenderQueueIndex.Overlay;

        public BoundingBox BoundingBox { get; }

        public abstract string DebugName { get; }

        public GameObject GameObject { get; set; }

        public RendererFlags Flags { get; } = RendererFlags.Forward | RendererFlags.Draw | RendererFlags.Update | RendererFlags.NoDepthTest;

        public abstract void Awake();

        public abstract void Destroy();

        public abstract void Draw(IGraphicsContext context, RenderPath path);

        public abstract void Load(IGraphicsDevice device);

        public abstract void Unload();

        public abstract void Update(IGraphicsContext context);

        public void DrawDepth(IGraphicsContext context)
        {
            throw new NotImplementedException();
        }

        public void DrawShadowMap(IGraphicsContext context, IBuffer light, ShadowType type)
        {
            throw new NotImplementedException();
        }

        public void Bake(IGraphicsContext context)
        {
            throw new NotImplementedException();
        }

        public void VisibilityTest(CullingContext context)
        {
            throw new NotImplementedException();
        }
    }
}