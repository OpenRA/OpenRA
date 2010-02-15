using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

using Tao.OpenGl;
using Tao.Cg;
using Tao.Platform.Windows;
using System.Runtime.InteropServices;
using System.Drawing.Imaging;

namespace OpenRa.GlRenderer
{
    public class GraphicsDevice
    {
        Graphics g;
        internal IntPtr dc;
        internal IntPtr rc;
        internal IntPtr cgContext;
        internal int vertexProfile, fragmentProfile;

        public static void CheckGlError()
        {
            var n = Gl.glGetError();
            if (n != Gl.GL_NO_ERROR)
                throw new InvalidOperationException("GL Error");
        }

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

            cgContext = Cg.cgCreateContext();
            //Cg.cgSetErrorCallback(CgErrorCallback);

            CgGl.cgGLRegisterStates(cgContext);
            CgGl.cgGLSetManageTextureParameters(cgContext, true);
            vertexProfile = CgGl.cgGLGetLatestProfile(CgGl.CG_GL_VERTEX);
            fragmentProfile = CgGl.cgGLGetLatestProfile(CgGl.CG_GL_FRAGMENT);
        }

        void CgErrorCallback()
        {
            var err = Cg.cgGetError();
            var str = Cg.cgGetErrorString(err);
            throw new InvalidOperationException(
                string.Format("CG Error: {0}: {1}", err, str));
        }

        public void EnableScissor(int left, int top, int width, int height)
        {
            Gl.glScissor(left, top, width, height);
            CheckGlError();
            Gl.glEnable(Gl.GL_SCISSOR_TEST);
            CheckGlError();
        }

        public void DisableScissor()
        {
            Gl.glDisable(Gl.GL_SCISSOR_TEST);
            CheckGlError();
        }

        public void Begin() { }
        public void End() { }

        public void Clear(Color c)
        {
            Gl.glClearColor(1, 1, 1, 1);
            CheckGlError();
            Gl.glClear(Gl.GL_COLOR_BUFFER_BIT);
            CheckGlError();
        }

        public void Present()
        {
            Wgl.wglSwapBuffers(dc);
            CheckGlError();
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
        int buffer;

        public VertexBuffer(GraphicsDevice dev, int size, VertexFormat fmt)
        {
            Gl.glGenBuffers(1, out buffer);
            GraphicsDevice.CheckGlError();
        }

        public void SetData(T[] data)
        {
            Bind();
            Gl.glBufferData(Gl.GL_ARRAY_BUFFER,
                new IntPtr(Marshal.SizeOf(typeof(T))), data, Gl.GL_DYNAMIC_DRAW);
            GraphicsDevice.CheckGlError();
        }

        public void Bind()
        {
            Gl.glBindBuffer(Gl.GL_ARRAY_BUFFER, buffer);
            GraphicsDevice.CheckGlError();
            Gl.glVertexPointer(3, Gl.GL_FLOAT, Marshal.SizeOf(typeof(T)), IntPtr.Zero);
            GraphicsDevice.CheckGlError();
            Gl.glTexCoordPointer(4, Gl.GL_FLOAT, Marshal.SizeOf(typeof(T)), new IntPtr(12));
            GraphicsDevice.CheckGlError();
        }
        
        bool disposed;
        public void Dispose()
        {
            if (disposed) return;
            GC.SuppressFinalize(this);
            Gl.glDeleteBuffers(1, ref buffer);
            GraphicsDevice.CheckGlError();
            disposed = true;
        }

        //~VertexBuffer() { Dispose(); }
    }

    public class IndexBuffer : IDisposable
    {
        int buffer;

        public IndexBuffer(GraphicsDevice dev, int size)
        {
            Gl.glGenBuffers(1, out buffer); 
            GraphicsDevice.CheckGlError();
        }

        public void SetData(ushort[] data)
        {
            Bind();
            Gl.glBufferData(Gl.GL_ELEMENT_ARRAY_BUFFER,
                new IntPtr(2 * data.Length), data, Gl.GL_DYNAMIC_DRAW);
            GraphicsDevice.CheckGlError();
        }

        public void Bind()
        {
            Gl.glBindBuffer(Gl.GL_ELEMENT_ARRAY_BUFFER, buffer);
            GraphicsDevice.CheckGlError();
        }

        bool disposed;
        public void Dispose()
        {
            if (disposed) return;
            GC.SuppressFinalize(this);
            Gl.glDeleteBuffers(1, ref buffer);
            GraphicsDevice.CheckGlError();
            disposed = true;
        }

        //~IndexBuffer() { Dispose(); }
    }

    public class Shader
    {
        IntPtr effect;
        IntPtr highTechnique;
        IntPtr lowTechnique;
        GraphicsDevice dev;

        public Shader(GraphicsDevice dev, Stream s)
        {
            this.dev = dev;
            var code = new StreamReader(s).ReadToEnd();
            effect = Cg.cgCreateEffect(dev.cgContext, code, null);

            if (effect == IntPtr.Zero)
            {
                var err = Cg.cgGetErrorString(Cg.cgGetError());
                var results = Cg.cgGetLastListing(dev.cgContext);
                throw new InvalidOperationException(
                    string.Format("Cg compile failed ({0}):\n{1}", err, results));
            }

            lowTechnique = Cg.cgGetNamedTechnique(effect, "low_quality");
            highTechnique = Cg.cgGetNamedTechnique(effect, "high_quality");

            if (lowTechnique != IntPtr.Zero && 0 == Cg.cgValidateTechnique(lowTechnique))
                lowTechnique = IntPtr.Zero;
            if (highTechnique != IntPtr.Zero && 0 == Cg.cgValidateTechnique(highTechnique))
                highTechnique = IntPtr.Zero;

            if (highTechnique == IntPtr.Zero && lowTechnique == IntPtr.Zero)
                throw new InvalidOperationException("No valid techniques");
        }

        public ShaderQuality Quality { get; set; }

        public void Render(Action a)
        {
            CgGl.cgGLEnableProfile(dev.vertexProfile);
            CgGl.cgGLEnableProfile(dev.fragmentProfile);

            var technique = Quality == ShaderQuality.High ? highTechnique : lowTechnique;
            var pass = Cg.cgGetFirstPass(technique);
            while (pass != IntPtr.Zero)
            {
                Cg.cgSetPassState(pass);
                a();
                Cg.cgResetPassState(pass);
                pass = Cg.cgGetNextPass(pass);
            }

            CgGl.cgGLDisableProfile(dev.fragmentProfile);
            CgGl.cgGLDisableProfile(dev.vertexProfile);
        }

        public void SetValue(string param, Texture texture) { }
        public void SetValue<T>(string param, T t) where T : struct { }
        public void Commit() { }
    }

    public class Texture
    {
        internal int texture;

        public Texture(GraphicsDevice dev, Bitmap bitmap)
        {
            Gl.glGenTextures(1, out texture);
            GraphicsDevice.CheckGlError();
            SetData(bitmap);
        }

        public void SetData(Bitmap bitmap)
        {
            var bits = bitmap.LockBits(
                new Rectangle(0, 0, bitmap.Width, bitmap.Height),
                ImageLockMode.ReadOnly,
                PixelFormat.Format32bppArgb);

            Gl.glTexParameteri(Gl.GL_TEXTURE_2D, Gl.GL_TEXTURE_BASE_LEVEL, 0);
            GraphicsDevice.CheckGlError();
            Gl.glTexParameteri(Gl.GL_TEXTURE_2D, Gl.GL_TEXTURE_MAX_LEVEL, 0);
            GraphicsDevice.CheckGlError();
            Gl.glTexImage2D(Gl.GL_TEXTURE_2D, 0, Gl.GL_RGBA8, bits.Width, bits.Height, 
                0, Gl.GL_RGBA, Gl.GL_UNSIGNED_BYTE, bits.Scan0);        // todo: weird strides
            GraphicsDevice.CheckGlError();

            bitmap.UnlockBits(bits);
        }
    }

    [Flags]
    public enum VertexFormat { Position, Texture2 }

    public enum ShaderQuality { Low, High }
    public enum PrimitiveType
    {
        PointList = Gl.GL_POINTS, 
        LineList = Gl.GL_LINES, 
        TriangleList = Gl.GL_TRIANGLES
    }
}
