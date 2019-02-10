using OpenGL;
using System;

namespace TQ._3D_Test
{
    class Texture : IDisposable
    {
        readonly uint _handle;
        public Texture(bool _ = true) => _handle = Gl.GenTexture();
        ~Texture() => Dispose();
        public void Dispose()
        {
            Gl.DeleteTextures(_handle);
            GC.SuppressFinalize(this);
        }

        public void Bind(TextureTarget target) => Gl.BindTexture(target, _handle);

        public void Parameteri<T>(TextureParameterName parameterName, T param)
            where T : unmanaged => Gl.TextureParameteri(_handle, parameterName, ref param);
    }
}
