﻿using System.Runtime.CompilerServices;

namespace HexaEngine.Core.Graphics
{
    [InlineArray(BlendDescription.SimultaneousRenderTargetCount)]
    public struct RenderTargetArray
    {
        public IRenderTargetView View;
    }
}