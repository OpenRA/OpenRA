using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

using Tao.OpenGl;
using Tao.Cg;
using Tao.Platform.Windows;
using System.Runtime.InteropServices;

namespace OpenRa.GlRenderer
{
    public class GraphicsDevice
    {
        Graphics g;
        IntPtr dc;
        IntPtr rc;

        public GraphicsDevice(Control control, int width, int height, bool fullscreen, bool vsync)
        {
            g = control.CreateGraphics();
            dc = g.GetHdc();
            
            var pfd = new Gdi.PIXELFORMATDESCRIPTOR
            {
                nSize = (short)Marshal.SizeOf(typeof(Gdi.PIXELFORMATDESCRIPTOR)),
                nVersion = 1,
                dwFlags = Gdi.PFD_SUPPORT_OPENGL | Gdi.PFD_DRAW_TO_BITMAP | Gdi.PFD_DOUBLEBUFFER,
                iPixelType = Gdi.PFD_TYPE_RGBA,
                cColorBits = 24,
                iLayerType = Gdi.PFD_MAIN_PLANE
            };

            var iFormat = Gdi.ChoosePixelFormat(dc, ref pfd);
            Gdi.SetPixelFormat(dc, iFormat, ref pfd);

            rc = Wgl.wglCreateContext(dc);
            if (rc == IntPtr.Zero)
                throw new InvalidOperationException("can't create wglcontext");
            Wgl.wglMakeCurrent(dc, rc);
        }

        public void EnableScissor(int left, int top, int width, int height)
        {
            Gl.glScissor(left, top, width, height);
            Gl.glEnable(Gl.GL_SCISSOR_TEST);
        }

        public void DisableScissor()
        {
            Gl.glDisable(Gl.GL_SCISSOR_TEST);
        }

        public void Begin() { }
        public void End() { }

        public void Clear(Color c)
        {
            Gl.glClearColor(1, 1, 1, 1); 
            Gl.glClear(Gl.GL_COLOR_BUFFER_BIT);
        }

        public void Present()
        {
            Wgl.wglSwapBuffers(dc);
        }

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
