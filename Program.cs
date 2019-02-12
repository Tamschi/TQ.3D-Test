using DDS;
using OpenGL;
using OpenGL.CoreUI;
using System;
using System.Collections.Generic;
using System.IO;
using TQ.Mesh.Parts;
using static TQ.Mesh.Parts.VertexBuffer;
using TQTexture = global::TQ.Texture.Texture;
using System.Linq;
using System.Numerics;

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
                            case "DXT5": internalFormat = InternalFormat.CompressedRgbaS3tcDxt5Ext; break;
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
                    _vertexStride = vertexBuffer.Header.Stride;
                    Console.WriteLine($" (also got stride {_vertexStride})");
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
                            _positionAttribute = (uint)_program.GetAttributeLocation("position");
                            _uvAttribute = (uint)_program.GetAttributeLocation("uv");
                            var offset = 0;
                            foreach (var attribute in attributes)
                            {
                                switch (attribute)
                                {
                                    case AttributeId.Position:
                                        Console.Write(" position...");
                                        _positionOffset = (IntPtr)offset;
                                        break;
                                    case AttributeId.Normal:
                                    case AttributeId.Tangent:
                                    case AttributeId.Bitangent:
                                        break;
                                    case AttributeId.UV:
                                        Console.Write(" uv...");
                                        _uvOffset = (IntPtr)offset;
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
                else if (part.Is(out Bones bones))
                {
                    Console.Write("Loading Bones...");
                    _boneMatrices = new Matrix4x4[bones.Count];
                    _bonePositions = new Vector3[bones.Count];
                    for (int i = 0; i < _boneMatrices.Length; i++)
                    { _boneMatrices[i] = Matrix4x4.Identity; }

                    foreach (var bone in bones)
                    {
                        var i = bone.Index;
                        var position = new Vector4(bone.Position[0], bone.Position[1], bone.Position[2], 1);
                        var bonePosition = Vector4.Transform(position, _boneMatrices[i]);
                        _bonePositions[i] = new Vector3(bonePosition.X, bonePosition.Y, bonePosition.Z);
                        var boneMatrix = new Matrix4x4(
                            bone.Axes[0], bone.Axes[1], bone.Axes[2], 0,
                            bone.Axes[3], bone.Axes[4], bone.Axes[5], 0,
                            bone.Axes[6], bone.Axes[7], bone.Axes[8], 0,
                            bone.Position[0], bone.Position[1], bone.Position[2], 1
                        );
                        _boneMatrices[i] = boneMatrix * _boneMatrices[i];
                        foreach (var childBone in bone)
                        { _boneMatrices[childBone.Index] = _boneMatrices[i]; }
                    }

                    _boneVbo = new Buffer();
                    Gl.CheckErrors();
                    Span<Vector3> boneVboData = (from p in _bonePositions select new Vector3(p.X / 2, p.Y / 2 - .7f, p.Z / 2)).ToArray().AsSpan();
                    _boneVbo.BufferData(boneVboData, BufferUsage.StaticDraw);
                    Gl.CheckErrors();

                    //TODO: LINQ it!
                    _boneIbo = new Buffer();
                    Gl.CheckErrors();
                    var boneIboEntries = new List<(int, int)>();
                    foreach (var parent in bones) foreach (var child in parent) boneIboEntries.Add((parent.Index, child.Index));
                    _boneIbo.BufferData(boneIboEntries.SelectMany(x => new[] { (ushort)x.Item1, (ushort)x.Item2 }).ToArray().AsSpan(), BufferUsage.StaticDraw);
                    Gl.CheckErrors();
                    _boneLinkCount = boneIboEntries.Count;

                    var vertexShader = new Shader(ShaderType.VertexShader, File.ReadAllText("bones.vertex.glsl"));
                    Gl.CheckErrors();
                    var fragmentShader = new Shader(ShaderType.FragmentShader, File.ReadAllText("bones.fragment.glsl"));
                    Gl.CheckErrors();
                    _boneProgram = new ShaderProgram(vertexShader, fragmentShader);
                    Gl.CheckErrors();
                    _boneProgram.Link();
                    Gl.CheckErrors();
                    _boneProgram.Use();
                    Gl.CheckErrors();
                    _bonePositionAttribute = (uint)_boneProgram.GetAttributeLocation("position");
                    Console.WriteLine(" OK!");

                    Gl.CheckErrors();
                }
            }
        }

        Matrix4x4[] _boneMatrices;
        Vector3[] _bonePositions;
        ShaderProgram _boneProgram;
        uint _bonePositionAttribute;

        Buffer _boneVbo;
        Buffer _boneIbo;
        int _boneLinkCount;

        Texture _texture;

        Buffer _vbo;
        Buffer _ibo;
        Shader _vertexShader;
        Shader _fragmentShader;
        ShaderProgram _program;
        int _vertexCount;
        int _uniformTransformation;
        (int, int)[] _drawRanges;
        uint _positionAttribute;
        IntPtr _positionOffset;
        uint _uvAttribute;
        IntPtr _uvOffset;
        int _vertexStride;

        void Render(object sender, NativeWindowEventArgs e)
        {
            Gl.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            _program.Use();
            Gl.Enable(EnableCap.CullFace);
            Gl.CullFace(CullFaceMode.Front);
            Gl.FrontFace(FrontFaceDirection.Ccw);
            _vbo.Bind(BufferTarget.ArrayBuffer);
            _ibo.Bind(BufferTarget.ElementArrayBuffer);
            Gl.VertexAttribPointer(_positionAttribute, size: 3, VertexAttribType.Float, normalized: false, _vertexStride, _positionOffset);
            Gl.EnableVertexAttribArray(_positionAttribute);
            Gl.VertexAttribPointer(_uvAttribute, size: 2, VertexAttribType.Float, normalized: false, _vertexStride, _uvOffset);
            Gl.EnableVertexAttribArray(_uvAttribute);
            foreach (var (first, count) in _drawRanges)
            { Gl.DrawElements(PrimitiveType.Triangles, count * 3, DrawElementsType.UnsignedShort, (IntPtr)(first * sizeof(ushort))); }
            Gl.DisableVertexAttribArray(_uvAttribute);
            Gl.DisableVertexAttribArray(_positionAttribute);

            _boneProgram.Use();
            Gl.Disable(EnableCap.CullFace);
            _boneVbo.Bind(BufferTarget.ArrayBuffer);
            _boneIbo.Bind(BufferTarget.ElementArrayBuffer);
            Gl.VertexAttribPointer(_bonePositionAttribute, size: 3, VertexAttribType.Float, normalized: false, 3 * sizeof(float), IntPtr.Zero);
            Gl.EnableVertexAttribArray(_bonePositionAttribute);
            Gl.DrawElements(PrimitiveType.Lines, _boneLinkCount * 2, DrawElementsType.UnsignedShort, IntPtr.Zero);
            Gl.DisableVertexAttribArray(_bonePositionAttribute);
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
