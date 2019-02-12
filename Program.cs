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
        static void Main()
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
        void SetUpAttribute(VertexArrayObject vao, ShaderProgram program, string name, int size, VertexAttribType type, bool normalized, uint relativeOffset, uint bindingIndex)
        {
            if (program.TryGetAttributeLocation(name, out var location))
            {
                vao.AttributeFormat(location, size, type, normalized, relativeOffset);
                vao.AttributeBinding(location, bindingIndex);
                vao.EnableAttribute(location);
            }
            else Console.Write($" Missing attribute in shader: {name}...");
        }

        private void ContextCreated(object sender, NativeWindowEventArgs e)
        {
            AttributeId[] attributes = null;

            _vao = new VertexArrayObject(true);
            {
                var texture = new TQTexture(File.ReadAllBytes("texture.tex"));
                _texture = new Texture(TextureTarget.Texture2d);
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

                    _texture.Storage2D((int)dds.Header.MipmapCount, internalFormat, (int)dds.Header.Width, (int)dds.Header.Height);
                    foreach (var layer in dds)
                    {
                        int level = 0;
                        foreach (var mip in layer)
                        {
                            unsafe
                            {
                                fixed (byte* ptr = mip.Data)
                                {
                                    _texture.CompressedSubImage2D(
                                        level++,
                                        internalFormat,
                                        xOffset: 0,
                                        yOffset: 0,
                                        width: (int)Math.Max(1, (dds.Header.Width * 2) >> level),
                                        height: (int)Math.Max(1, (dds.Header.Height * 2) >> level),
                                        data: mip.Data);
                                    Gl.CheckErrors();
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
                    var vbo = new Buffer(true);
                    _vao.VertexBuffer(0, vbo, IntPtr.Zero, vertexBuffer.Header.Stride);
                    vbo.BufferData(vertexBuffer.Buffer, BufferUsage.StaticDraw);
                    Console.Write(" OK!");
                    attributes = vertexBuffer.Attributes.ToArray();
                    Console.Write($" (also got {attributes.Length} attributes)");
                    _vertexCount = vertexBuffer.Header.VertexCount;
                    Console.WriteLine($" (also got vertex count {_vertexCount})");
                }
                else if (part.Is(out Shaders shaders))
                {
                    foreach (var shader in shaders)
                    {
                        {
                            Console.Write($"Sort of loading {shader.FileName}...");
                            using (var vertexShader = new Shader(ShaderType.VertexShader, File.ReadAllText($"{shader.FileName}.vertex.glsl")))
                            using (var fragmentShader = new Shader(ShaderType.FragmentShader, File.ReadAllText($"{shader.FileName}.fragment.glsl")))
                            { _program = new ShaderProgram(vertexShader, fragmentShader); }
                            _program.Link();
                            Console.WriteLine(" OK!");
                        }
                        {
                            Console.Write($"Setting up attibutes...");
                            var offset = 0u;
                            foreach (var attribute in attributes)
                            {
                                switch (attribute)
                                {
                                    case AttributeId.Position: SetUpAttribute(_vao, _program, "position", 3, VertexAttribType.Float, normalized: false, offset, bindingIndex: 0); break;
                                    case AttributeId.Normal: SetUpAttribute(_vao, _program, "normal", 3, VertexAttribType.Float, normalized: true, offset, bindingIndex: 0); break;
                                    case AttributeId.Tangent: SetUpAttribute(_vao, _program, "tangent", 3, VertexAttribType.Float, normalized: true, offset, bindingIndex: 0); break;
                                    case AttributeId.Bitangent: SetUpAttribute(_vao, _program, "bitangent", 3, VertexAttribType.Float, normalized: true, offset, bindingIndex: 0); break;
                                    case AttributeId.UV: SetUpAttribute(_vao, _program, "uv", 2, VertexAttribType.Float, normalized: false, offset, bindingIndex: 0); break;
                                    case AttributeId.Weights: SetUpAttribute(_vao, _program, "weights", 4, VertexAttribType.Float, normalized: false, offset, bindingIndex: 0); break;
                                    case AttributeId.Bones: SetUpAttribute(_vao, _program, "bones", 4, VertexAttribType.Byte, normalized: false, offset, bindingIndex: 0); break;
                                    case AttributeId.Bytes: SetUpAttribute(_vao, _program, "bytes", 4, VertexAttribType.UnsignedByte, normalized: false, offset, bindingIndex: 0); break;
                                }
                                offset += GetAttributeSize(attribute);
                            }
                            Console.WriteLine(" OK!");
                        }
                        {
                            Console.Write($"Setting up uniforms...");
                            if (_program.TryGetUniformLocation("transformation", out var transformationUniform))
                            {
                                var matrix = new Matrix4x4(
                                    .5f, 0, 0, 0,
                                    0, .5f, 0, 0,
                                    0, 0, .5f, 0,
                                    0, -.7f, 0, 1
                                );
                                _program.UniformMatrix4f(transformationUniform, transpose: false, in matrix);
                            }
                            else throw new NotImplementedException();
                            Console.WriteLine(" OK!");
                        }
                    }
                }
                else if (part.Is(out IndexBuffer indexBuffer))
                {
                    Console.Write("Loading IBO...");
                    var ibo = new Buffer(true);
                    _vao.ElementBuffer(ibo);
                    ibo.BufferData(indexBuffer.TriangleIndices, BufferUsage.StaticDraw);
                    Console.WriteLine(" OK!");

                    var drawRanges = new List<(int, int)>();
                    foreach (var drawCall in indexBuffer)
                    { drawRanges.Add((drawCall.Common.StartFaceIndex, drawCall.Common.FaceCount)); }
                    _drawRanges = drawRanges.ToArray();
                }
                else if (part.Is(out Bones bones))
                {
                    _boneVao = new VertexArrayObject(true);
                    Console.Write("Loading Bones...");
                    _boneMatrices = new Matrix4x4[bones.Count];
                    _bonePositions = new Vector3[bones.Count];
                    for (int i = 0; i < _boneMatrices.Length; i++)
                    { _boneMatrices[i] = Matrix4x4.Identity; }

                    foreach (var bone in bones)
                    {
                        var i = bone.Index;
                        var position = new Vector4(bone.Position, 1);
                        var bonePosition = Vector4.Transform(position, _boneMatrices[i]);
                        _bonePositions[i] = new Vector3(bonePosition.X, bonePosition.Y, bonePosition.Z);
                        var boneMatrix = new Matrix4x4(
                            bone.Axes[0], bone.Axes[1], bone.Axes[2], 0,
                            bone.Axes[3], bone.Axes[4], bone.Axes[5], 0,
                            bone.Axes[6], bone.Axes[7], bone.Axes[8], 0,
                            bone.Position.X, bone.Position.Y, bone.Position.Z, 1
                        );
                        _boneMatrices[i] = boneMatrix * _boneMatrices[i];
                        foreach (var childBone in bone)
                        { _boneMatrices[childBone.Index] = _boneMatrices[i]; }
                    }

                    var boneVbo = new Buffer(true);
                    _boneVao.VertexBuffer(0, boneVbo, IntPtr.Zero, 3 * sizeof(float));
                    Gl.CheckErrors();
                    Span<Vector3> boneVboData = (from p in _bonePositions select new Vector3(p.X / 2, p.Y / 2 - .7f, p.Z / 2)).ToArray().AsSpan();
                    boneVbo.BufferData(boneVboData, BufferUsage.StaticDraw);
                    Gl.CheckErrors();

                    //TODO: LINQ it!
                    var boneIbo = new Buffer(true);
                    _boneVao.ElementBuffer(boneIbo);
                    Gl.CheckErrors();
                    var boneIboEntries = new List<(int, int)>();
                    foreach (var parent in bones) foreach (var child in parent) boneIboEntries.Add((parent.Index, child.Index));
                    boneIbo.BufferData(boneIboEntries.SelectMany(x => new[] { (ushort)x.Item1, (ushort)x.Item2 }).ToArray().AsSpan(), BufferUsage.StaticDraw);
                    Gl.CheckErrors();
                    _boneLinkCount = boneIboEntries.Count;

                    using (var vertexShader = new Shader(ShaderType.VertexShader, File.ReadAllText("bones.vertex.glsl")))
                    using (var fragmentShader = new Shader(ShaderType.FragmentShader, File.ReadAllText("bones.fragment.glsl")))
                    { _boneProgram = new ShaderProgram(vertexShader, fragmentShader); }
                    _boneProgram.Link();
                    Console.WriteLine(" OK!");

                    SetUpAttribute(_boneVao, _boneProgram, "position", 3, VertexAttribType.Float, normalized: false, relativeOffset: 0, bindingIndex: 0);

                    Gl.CheckErrors();
                }
            }
        }

        Matrix4x4[] _boneMatrices;
        Vector3[] _bonePositions;
        ShaderProgram _boneProgram;

        VertexArrayObject _boneVao;
        int _boneLinkCount;

        Texture _texture;

        VertexArrayObject _vao;
        ShaderProgram _program;
        int _vertexCount;
        (int, int)[] _drawRanges;

        void Render(object sender, NativeWindowEventArgs e)
        {
            Gl.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            _vao.Bind();
            _program.Use();
            Gl.Enable(EnableCap.CullFace);
            Gl.CullFace(CullFaceMode.Front);
            Gl.FrontFace(FrontFaceDirection.Ccw);
            _texture.BindUnit(0);
            foreach (var (first, count) in _drawRanges)
            { Gl.DrawElements(PrimitiveType.Triangles, count * 3, DrawElementsType.UnsignedShort, (IntPtr)(first * sizeof(ushort))); }

            _boneVao.Bind();
            _boneProgram.Use();
            Gl.Disable(EnableCap.CullFace);
            Gl.DrawElements(PrimitiveType.Lines, _boneLinkCount * 2, DrawElementsType.UnsignedShort, IntPtr.Zero);
            Gl.CheckErrors();
        }

        public void Dispose()
        {
            _program.Dispose();
            _boneProgram.Dispose();
        }
    }
}