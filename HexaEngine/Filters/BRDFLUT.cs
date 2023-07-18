﻿#nullable disable

namespace HexaEngine.Filters
{
    using HexaEngine.Core.Graphics;
    using HexaEngine.Core.Graphics.Primitives;

    public class BRDFLUT
    {
        private IGraphicsPipeline pipeline;

        public IRenderTargetView Target;
        private bool disposedValue;

        public BRDFLUT(IGraphicsDevice device, bool multiscatter, bool cloth)
        {
            pipeline = device.CreateGraphicsPipeline(new()
            {
                VertexShader = "effects/dfg/vs.hlsl",
                PixelShader = "effects/dfg/ps.hlsl"
            }, GraphicsPipelineState.DefaultFullscreen, new ShaderMacro[2] { new("MULTISCATTER", multiscatter ? "1" : "0"), new("CLOTH", cloth ? "1" : "0") });
        }

        public void Draw(IGraphicsContext context)
        {
            if (Target == null)
            {
                return;
            }

            int width = (int)Target.Viewport.Width;
            int height = (int)Target.Viewport.Height;
            int xTileSize = width / 16;
            int yTileSize = height / 16;

            context.ClearRenderTargetView(Target, default);

            context.SetRenderTarget(Target, null);
            context.SetViewport(Target.Viewport);

            context.SetGraphicsPipeline(pipeline);

            context.DrawInstanced(4, 1, 0, 0);

            context.SetGraphicsPipeline(null);
            context.SetRenderTarget(null, null);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                pipeline.Dispose();
                Target = null;
                pipeline = null;
                disposedValue = true;
            }
        }

        ~BRDFLUT()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: false);
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}