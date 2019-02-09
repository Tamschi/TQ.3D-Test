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

        internal unsafe void BufferData<T>(Span<T> data, BufferUsage usage) where T : unmanaged
        { fixed (T* ptr = data) Gl.NamedBufferData(_handle, (uint)(data.Length * sizeof(T)), (IntPtr)ptr, usage); }
    }
}
