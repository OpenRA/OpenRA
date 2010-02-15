using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Drawing;
using System.IO;

namespace OpenRa.Gl
{
    public class GraphicsDevice
    {
        public GraphicsDevice(Control host, int width, int height, bool fullscreen, bool vsync) { }
        public void EnableScissor(int left, int top, int width, int height) { }
        public void DisableScissor() { }
        public void Begin() { }
        public void End() { }
        public void Clear(Color c) { }
        public void Present() { }
        public void DrawIndexedPrimitives(PrimitiveType pt, Range<int> vertices, Range<int> indices) { }
        public void DrawIndexedPrimitives(PrimitiveType pt, int numVerts, int numPrimitives) { }
    }

    public struct Range<T>
    {
        public Range(T start, T end) { }
    }

    public class VertexBuffer<T> where T : struct
    {
        public VertexBuffer(GraphicsDevice dev, int size, VertexFormat fmt) { }
        public void SetData(T[] data) { }
        public void Bind() { }
    }

    public class IndexBuffer
    {
        public IndexBuffer(GraphicsDevice dev, int size) { }
        public void SetData(ushort[] data) { }
        public void Bind() { }
    }

    public class Shader
    {
        public Shader(GraphicsDevice dev, Stream s) { }
        public ShaderQuality Quality { get; set; }
        public void Render(Action a) { }
        public void SetValue(string param, Texture texture) { }
        public void SetValue<T>(string param, T t) where T : struct { }
        public void Commit() { }

    }

    public class Texture
    {
        public Texture(GraphicsDevice dev, Bitmap bitmap) { }
        public void SetData(Bitmap bitmap) { }
    }

    [Flags]
    public enum VertexFormat { Position, Texture2 }

    public enum ShaderQuality { Low, Medium, High }
    public enum PrimitiveType { PointList, LineList, TriangleList }
}
