using OpenGL;
using System;

namespace TQ._3D_Test
{
    class Buffer : IDisposable
    {
        readonly uint _handle;
        public Buffer(bool _ = true) => _handle = Gl.GenBuffer();
        ~Buffer() => Dispose();
        public void Dispose()
        {
            Gl.DeleteBuffers(_handle);
            GC.SuppressFinalize(this);
        }

        public void Bind(BufferTarget bufferTarget) => Gl.BindBuffer(bufferTarget, _handle);
    }
}
