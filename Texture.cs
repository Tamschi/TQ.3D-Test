using OpenGL;
using System;

namespace TQ._3D_Test
{
    readonly struct Texture : IDisposable
    {
        readonly uint _handle;
        public Texture(TextureTarget target) => _handle = Gl.CreateTexture(target);

        public void Dispose() => Gl.DeleteTextures(_handle);

        public void BindUnit(uint unit) => Gl.BindTextureUnit(unit, _handle);

        public void Parameteri<T>(TextureParameterName parameterName, T param)
            where T : unmanaged => Gl.TextureParameteri(_handle, parameterName, ref param);

        internal void Storage2D(int levels, InternalFormat internalFormat, int width, int height)
            => Gl.TextureStorage2D(_handle, levels, internalFormat, width, height);

        public unsafe void CompressedSubImage2D(int level, InternalFormat internalFormat, int xOffset, int yOffset, int width, int height, Span<byte> data)
        { fixed (byte* dataPtr = data) Gl.CompressedTextureSubImage2D(_handle, level, xOffset, yOffset, width, height, (PixelFormat)internalFormat, data.Length, (IntPtr)dataPtr); }
    }
}