using OpenGL;
using System;

namespace TQ._3D_Test
{
    readonly struct Buffer : IDisposable
    {
        readonly uint _handle;
        public Buffer(bool _ = true) => _handle = Gl.CreateBuffer();

        public void Dispose() => Gl.DeleteBuffers(_handle);

        public static explicit operator uint(Buffer buffer) => buffer._handle;

        internal unsafe void BufferData<T>(Span<T> data, BufferUsage usage) where T : unmanaged
        { fixed (T* ptr = data) Gl.NamedBufferData(_handle, (uint)(data.Length * sizeof(T)), (IntPtr)ptr, usage); }
    }
}
