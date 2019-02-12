using OpenGL;
using System;

namespace TQ._3D_Test
{
    readonly struct ShaderProgram : IDisposable
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

        public void Dispose() => Gl.DeleteProgram(_handle);

        internal bool TryGetAttributeLocation(string inputName, out AttributeIndex index)
        {
            switch (Gl.GetAttribLocation(_handle, inputName))
            {
                case -1: index = default; return false;
                case int i:
                    index = new AttributeIndex((uint)i);
                    return true;
            }
        }

        public static explicit operator uint(ShaderProgram program) => program._handle;
    }
}