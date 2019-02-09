using OpenGL;
using System;

namespace TQ._3D_Test
{
    internal class ShaderProgram : IDisposable
    {
        readonly uint _handle;

        public ShaderProgram(params Shader[] shaders)
        {
            _handle = Gl.CreateProgram();
            foreach (var shader in shaders)
            { Gl.AttachShader(_handle, (uint)shader); }
        }

        public void BindFragDataLocation(uint bufferIndex, string varyingOut)
            => Gl.BindFragDataLocation(_handle, bufferIndex, varyingOut);

        public void Link() => Gl.LinkProgram(_handle);
        public void Use() => Gl.UseProgram(_handle);

        ~ShaderProgram() => Dispose();
        public void Dispose()
        {
            Gl.DeleteProgram(_handle);
            GC.SuppressFinalize(this);
        }

        internal int GetAttributeLocation(string inputName)
            => Gl.GetAttribLocation(_handle, inputName);
    }
}