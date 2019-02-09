using OpenGL;
using System;

namespace TQ._3D_Test
{
    static class SGl
    {
        public static unsafe void BufferData(BufferTarget target, Span<byte> data, BufferUsage usage)
        { fixed (byte* ptr = data) Gl.BufferData(target, (uint)data.Length, (IntPtr)ptr, usage); }
    }
}
