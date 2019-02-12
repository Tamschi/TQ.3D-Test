using OpenGL;
using System;
using System.Text;

namespace TQ._3D_Test
{
    readonly struct Shader : IDisposable
    {
        readonly uint _handle;
        public Shader(ShaderType shaderType, string source)
        {
            _handle = Gl.CreateShader(shaderType);
            try
            {
                Gl.ShaderSource(_handle, new[] { source });
                Gl.CompileShader(_handle);
                Gl.GetShader(_handle, ShaderParameterName.CompileStatus, out var compileStatus);
                switch (compileStatus)
                {
                    case Gl.TRUE: break;
                    case int abnormal:
                        var logBuilder = new StringBuilder(1024);
                        Gl.GetShaderInfoLog(_handle, 1024, out var length, logBuilder);
                        logBuilder.Length = length;
                        throw new ShaderNotCompiledProperlyException(abnormal, logBuilder.ToString());
                }
            }
            catch { Dispose(); throw; }
        }
        
        public void Dispose() => Gl.DeleteShader(_handle);

        public static explicit operator uint(Shader shader) => shader._handle;
    }
}