using OpenGL;
using System;
using System.Numerics;

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

        public void Dispose() => Gl.DeleteProgram(_handle);

        public static explicit operator uint(ShaderProgram program) => program._handle;

        public void BindFragDataLocation(uint bufferIndex, string varyingOut)
            => Gl.BindFragDataLocation(_handle, bufferIndex, varyingOut);

        public void Link() => Gl.LinkProgram(_handle);
        public void Use() => Gl.UseProgram(_handle);

        public bool TryGetAttributeLocation(string inputName, out AttributeLocation location)
        {
            switch (Gl.GetAttribLocation(_handle, inputName))
            {
                case -1: location = default; return false;
                case int l:
                    location = new AttributeLocation((uint)l);
                    return true;
            }
        }

        public bool TryGetUniformLocation(string uniformName, out UniformLocation location)
        {
            switch (Gl.GetUniformLocation(_handle, uniformName))
            {
                case -1: location = default; return false;
                case int l:
                    location = new UniformLocation((uint)l);
                    return true;
            }
        }

        internal unsafe void UniformMatrix4f(UniformLocation uniformLocation, bool transpose, in Matrix4x4 matrix)
        { fixed (Matrix4x4* matrixPtr = &matrix) Gl.ProgramUniformMatrix4(_handle, (int)uniformLocation, count: 1, transpose, (float*)matrixPtr); }
    }
}