using OpenGL;
using OpenGL.CoreUI;
using System;
using System.IO;
using TQ.Mesh.Parts;
using static TQ.Mesh.Parts.VertexBuffer;

namespace TQ._3D_Test
{
    class Program : IDisposable
    {
        static void Main(string[] args)
        {
            using (var program = new Program())
            {
                program.Run();
            }
        }
        void Run()
        {
            using (var nativeWindow = NativeWindow.Create())
            {
                nativeWindow.ContextCreated += ContextCreated;
                nativeWindow.Render += Render;
                nativeWindow.Create(0, 0, 256, 256, NativeWindowStyle.Overlapped);
                nativeWindow.Show();
                nativeWindow.Run();
            }
        }

        private void ContextCreated(object sender, NativeWindowEventArgs e)
        {
            AttributeId[] attributes = null;
            int vertexStride = -1;

            var mesh = new Mesh.Mesh(File.ReadAllBytes("mesh.msh"));
            foreach (var part in mesh)
            {
                if (part.Is(out VertexBuffer vertexBuffer))
                {
                    Console.Write("Loading VBO...");
                    _vbo = new Buffer();
                    _vbo.Bind(BufferTarget.ArrayBuffer);
                    SGl.BufferData(BufferTarget.ArrayBuffer, vertexBuffer.Buffer, BufferUsage.StaticDraw);
                    Console.Write(" OK!");
                    attributes = vertexBuffer.Attributes.ToArray();
                    Console.Write($" (also got {attributes.Length} attributes)");
                    vertexStride = vertexBuffer.Header.Stride;
                    Console.WriteLine($" (also got stride {vertexStride})");
                    _vertexCount = vertexBuffer.Header.VertexCount;
                    Console.WriteLine($" (also got vertex count {_vertexCount})");
                }
                else if (part.Is(out Shaders shaders))
                {
                    foreach (var shader in shaders)
                    {
                        {
                            Console.Write($"Not really loading {shader.FileName}...");
                            _vertexShader = new Shader(ShaderType.VertexShader, File.ReadAllText("vertex.glsl"));
                            _fragmentShader = new Shader(ShaderType.FragmentShader, File.ReadAllText("fragment.glsl"));
                            _program = new ShaderProgram(_vertexShader, _fragmentShader);
                            _program.Link();
                            _program.Use();
                            Console.WriteLine(" OK!");
                        }
                        {
                            Console.Write($"Setting up attibutes...");
                            int positionAttribute = _program.GetAttributeLocation("position");
                            int offset = 0;
                            foreach (var attribute in attributes)
                            {
                                switch (attribute)
                                {
                                    case AttributeId.Position:
                                        Console.Write($" position...");
                                        Gl.VertexAttribPointer((uint)positionAttribute, 3, VertexAttribType.Float, normalized: false, vertexStride, (IntPtr)offset);
                                        Gl.EnableVertexAttribArray((uint)positionAttribute);
                                        break;
                                    case AttributeId.Normal:
                                    case AttributeId.Tangent:
                                    case AttributeId.Bitangent:
                                    case AttributeId.UV:
                                    case AttributeId.Weights:
                                    case AttributeId.Bones:
                                    case AttributeId.Bytes:
                                    default:
                                        break;
                                }
                                offset += GetAttributeSize(attribute);
                            }
                            Console.WriteLine(" OK!");
                        }
                    }
                }
            }
        }

        Buffer _vbo;
        Shader _vertexShader;
        Shader _fragmentShader;
        ShaderProgram _program;
        int _vertexCount;

        void Render(object sender, NativeWindowEventArgs e)
        {
            Gl.DrawArrays(PrimitiveType.Points, 0, _vertexCount);
        }

        public void Dispose()
        {
            _vbo.Dispose();
        }
    }
}
