﻿#nullable disable

namespace HexaEngine.Pipelines.Deferred
{
    using HexaEngine.Core.Graphics;
    using HexaEngine.Mathematics;

    public class Grid : IGraphicsPipeline
    {
        private IGraphicsPipeline pipeline;

        public Grid(IGraphicsDevice device)
        {
            pipeline = device.CreateGraphicsPipeline(new()
            {
                VertexShader = "deferred/grid/vs.hlsl",
                PixelShader = "deferred/grid/ps.hlsl"
            },
            new GraphicsPipelineState()
            {
                DepthStencil = DepthStencilDescription.Default,
                Rasterizer = RasterizerDescription.CullBack,
                Blend = BlendDescription.Opaque,
                Topology = PrimitiveTopology.LineList,
            },
            new ShaderMacro[]
            {
                new("INSTANCED", 1)
            });
        }

        public GraphicsPipelineDesc Description => pipeline.Description;

        public string DebugName => pipeline.DebugName;

        public GraphicsPipelineState State { get => pipeline.State; set => pipeline.State = value; }

        public void BeginDraw(IGraphicsContext context, Viewport viewport)
        {
            pipeline.BeginDraw(context, viewport);
        }

        public void Dispose()
        {
            pipeline.Dispose();
        }

        public void DrawIndexedInstanced(IGraphicsContext context, Viewport viewport, uint indexCount, uint instanceCount, uint indexOffset, int vertexOffset, uint instanceOffset)
        {
            pipeline.DrawIndexedInstanced(context, viewport, indexCount, instanceCount, indexOffset, vertexOffset, instanceOffset);
        }

        public void DrawIndexedInstancedIndirect(IGraphicsContext context, Viewport viewport, IBuffer args, uint stride)
        {
            pipeline.DrawIndexedInstancedIndirect(context, viewport, args, stride);
        }

        public void DrawInstanced(IGraphicsContext context, Viewport viewport, IBuffer args, uint stride)
        {
            pipeline.DrawInstanced(context, viewport, args, stride);
        }

        public void DrawInstanced(IGraphicsContext context, Viewport viewport, uint vertexCount, uint instanceCount, uint vertexOffset, uint instanceOffset)
        {
            pipeline.DrawInstanced(context, viewport, vertexCount, instanceCount, vertexOffset, instanceOffset);
        }

        public void EndDraw(IGraphicsContext context)
        {
            pipeline.EndDraw(context);
        }

        public void Recompile()
        {
            pipeline.Recompile();
        }
    }
}