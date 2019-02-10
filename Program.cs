using DDS;
using OpenGL;
using OpenGL.CoreUI;
using System;
using System.Collections.Generic;
using System.IO;
using TQ.Mesh.Parts;
using static TQ.Mesh.Parts.VertexBuffer;
using TQTexture = global::TQ.Texture.Texture;

namespace TQ._3D_Test
{
    class Program : IDisposable
    {
        static void Main(string[] args)
        {
            using (var program = new Program())
            { program.Run(); }
        }
        void Run()
        {
            using (var nativeWindow = NativeWindow.Create())
            {
                nativeWindow.ContextCreated += ContextCreated;
                nativeWindow.Render += Render;
                nativeWindow.Create(0, 0, 512, 512, NativeWindowStyle.Overlapped);
                nativeWindow.DepthBits = 32;
                nativeWindow.Show();
                nativeWindow.Run();
            }
        }

        private void ContextCreated(object sender, NativeWindowEventArgs e)
        {
            AttributeId[] attributes = null;
            int vertexStride = -1;

            {
                var texture = new TQTexture(File.ReadAllBytes("texture.tex"));
                _texture = new Texture();
                _texture.Bind(TextureTarget.Texture2d);
                _texture.Parameteri(TextureParameterName.TextureWrapS, TextureWrapMode.Repeat);
                _texture.Parameteri(TextureParameterName.TextureWrapT, TextureWrapMode.Repeat);
                _texture.Parameteri(TextureParameterName.TextureMinFilter, TextureMinFilter.Linear);
                _texture.Parameteri(TextureParameterName.TextureMagFilter, TextureMagFilter.Linear);
                foreach (var frame in texture)
                {
                    var dds = new DDS.DDS(frame.Data);
                    InternalFormat internalFormat;
                    if (dds.Header.Capabilities.HasFlag(Capabilities.Complex))
                    {
                        switch (dds.Header.PixelFormat.FourCC)
                        {
                            case "DXT5": internalFormat = InternalFormat.CompressedSrgbAlphaS3tcDxt5Ext; break;
                            default: throw new NotImplementedException();
                        }
                    }
                    else throw new NotImplementedException();
                    foreach (var layer in dds)
                    {
                        int level = 0;
                        Console.WriteLine($"Error: {Gl.GetError()}");
                        foreach (var mip in layer)
                        {
                            unsafe
                            {
                                fixed (byte* ptr = mip.Data)
                                {
                                    Gl.CompressedTexImage2D(
                                        TextureTarget.Texture2d,
                                        level++,
                                        internalFormat,
                                        width: (int)Math.Max(1, (dds.Header.Width * 2) >> level),
                                        height: (int)Math.Max(1, (dds.Header.Height * 2) >> level),
                                        border: 0,
                                        mip.Data.Length,
                                        (IntPtr)ptr);
                                    Console.WriteLine($"Error: {Gl.GetError()}");
                                }
                            }
                        }
                        break; //TODO: Further layers.
                    }
                    break; //TODO: Further frames.
                }
            }

            var mesh = new Mesh.Mesh(File.ReadAllBytes("mesh.msh"));
            foreach (var part in mesh)
            {
                if (part.Is(out VertexBuffer vertexBuffer))
                {
                    Console.Write("Loading VBO...");
                    _vbo = new Buffer();
                    _vbo.Bind(BufferTarget.ArrayBuffer);
                    _vbo.BufferData(vertexBuffer.Buffer, BufferUsage.StaticDraw);
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
                            var positionAttribute = _program.GetAttributeLocation("position");
                            var uvAttribute = _program.GetAttributeLocation("uv");
                            var offset = 0;
                            foreach (var attribute in attributes)
                            {
                                switch (attribute)
                                {
                                    case AttributeId.Position:
                                        Console.Write(" position...");
                                        Gl.VertexAttribPointer((uint)positionAttribute, size: 3, VertexAttribType.Float, normalized: false, vertexStride, (IntPtr)offset);
                                        Gl.EnableVertexAttribArray((uint)positionAttribute);
                                        break;
                                    case AttributeId.Normal:
                                    case AttributeId.Tangent:
                                    case AttributeId.Bitangent:
                                        break;
                                    case AttributeId.UV:
                                        Console.Write(" uv...");
                                        Gl.VertexAttribPointer((uint)uvAttribute, size: 2, VertexAttribType.Float, normalized: false, vertexStride, (IntPtr)offset);
                                        Gl.EnableVertexAttribArray((uint)uvAttribute);
                                        break;
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
                        {
                            Console.Write($"Setting up uniforms...");
                            _uniformTransformation = Gl.GetUniformLocation((uint)_program, "transformation");
                            unsafe
                            {
                                var matrix = stackalloc float[16] { .5f, 0, 0, 0, 0, .5f, 0, 0, 0, 0, .5f, 0, 0, -.7f, 0, 1 };
                                Gl.ProgramUniformMatrix4f((uint)_program, _uniformTransformation, 1, transpose: false, ref matrix[0]);
                            }
                            Console.WriteLine(" OK!");
                        }
                    }
                }
                else if (part.Is(out IndexBuffer indexBuffer))
                {
                    Console.Write("Loading IBO...");
                    _ibo = new Buffer();
                    _ibo.Bind(BufferTarget.ElementArrayBuffer);
                    _ibo.BufferData(indexBuffer.TriangleIndices, BufferUsage.StaticDraw);
                    Console.WriteLine(" OK!");

                    var drawRanges = new List<(int, int)>();
                    foreach (var drawCall in indexBuffer)
                    { drawRanges.Add((drawCall.Common.StartFaceIndex, drawCall.Common.FaceCount)); }
                    _drawRanges = drawRanges.ToArray();
                }
            }
        }

        Texture _texture;

        Buffer _vbo;
        Buffer _ibo;
        Shader _vertexShader;
        Shader _fragmentShader;
        ShaderProgram _program;
        int _vertexCount;
        int _uniformTransformation;
        (int, int)[] _drawRanges;

        void Render(object sender, NativeWindowEventArgs e)
        {
            Gl.Enable(EnableCap.CullFace);
            Gl.CullFace(CullFaceMode.Front);
            Gl.FrontFace(FrontFaceDirection.Ccw);
            Gl.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            foreach (var (first, count) in _drawRanges)
            { Gl.DrawElements(PrimitiveType.Triangles, count * 3, DrawElementsType.UnsignedShort, (IntPtr)(first * sizeof(ushort))); }
        }

        public void Dispose()
        {
            _vbo.Dispose();
            _ibo.Dispose();
            _vertexShader.Dispose();
            _fragmentShader.Dispose();
            _program.Dispose();
        }
    }
}
