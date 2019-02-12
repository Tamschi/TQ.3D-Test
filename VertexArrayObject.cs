using OpenGL;
using System;

namespace TQ._3D_Test
{
    readonly struct VertexArrayObject : IDisposable
    {
        readonly uint _handle;
        public VertexArrayObject(bool _ = true) => _handle = Gl.CreateVertexArray();
        public void Dispose() => Gl.DeleteVertexArrays(_handle);

        public void VertexBuffer(uint bindingIndex, Buffer buffer, IntPtr offset, int stride)
            => Gl.VertexArrayVertexBuffer(_handle, bindingIndex, (uint)buffer, offset, stride);

        internal void ElementBuffer(Buffer buffer)
            => Gl.VertexArrayElementBuffer(_handle, (uint)buffer);

        internal void Bind()
            => Gl.BindVertexArray(_handle);

        internal void AttributeFormat(uint attributeIndex, int size, VertexAttribType type, bool normalized, uint relativeOffset)
            => Gl.VertexArrayAttribFormat(_handle, attributeIndex, size, type, normalized, relativeOffset);

        internal void AttributeBinding(uint attributeIndex, uint bindingIndex)
            => Gl.VertexArrayAttribBinding(_handle, attributeIndex, bindingIndex);
    }
}
