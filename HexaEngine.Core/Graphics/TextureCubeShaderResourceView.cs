﻿namespace HexaEngine.Core.Graphics
{
    using System;

    /// <summary>
    /// Represents a shader resource view for a cube texture.
    /// </summary>
    public struct TextureCubeShaderResourceView : IEquatable<TextureCubeShaderResourceView>
    {
        /// <summary>
        /// The most detailed mip level.
        /// </summary>
        public int MostDetailedMip;

        /// <summary>
        /// The number of mip levels.
        /// </summary>
        public int MipLevels;

        public override readonly bool Equals(object? obj)
        {
            return obj is TextureCubeShaderResourceView view && Equals(view);
        }

        public readonly bool Equals(TextureCubeShaderResourceView other)
        {
            return MostDetailedMip == other.MostDetailedMip &&
                   MipLevels == other.MipLevels;
        }

        public override readonly int GetHashCode()
        {
            return HashCode.Combine(MostDetailedMip, MipLevels);
        }

        public static bool operator ==(TextureCubeShaderResourceView left, TextureCubeShaderResourceView right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(TextureCubeShaderResourceView left, TextureCubeShaderResourceView right)
        {
            return !(left == right);
        }
    }
}