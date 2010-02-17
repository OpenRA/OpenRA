#region Copyright & License Information
/*
 * Copyright 2007,2009,2010 Chris Forbes, Robert Pepperell, Matthew Bowra-Dean, Paul Chote, Alli Witheford.
 * This file is part of OpenRA.
 * 
 *  OpenRA is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 * 
 *  OpenRA is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 * 
 *  You should have received a copy of the GNU General Public License
 *  along with OpenRA.  If not, see <http://www.gnu.org/licenses/>.
 */
#endregion

using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Tao.Cg;
using Tao.OpenGl;
using Tao.Platform.Windows;
using OpenRa.FileFormats.Graphics;
using Tao.Glfw;

[assembly: Renderer( typeof( OpenRa.GlRenderer.GraphicsDevice ))]

namespace OpenRa.GlRenderer
{
    public class GraphicsDevice : IGraphicsDevice
    {
		Size windowSize;
        internal IntPtr cgContext;
        internal int vertexProfile, fragmentProfile;

		readonly Glfw.GLFWmousebuttonfun mouseButtonCallback;
		readonly Glfw.GLFWmouseposfun mousePositionCallback;
		readonly Glfw.GLFWwindowclosefun windowCloseCallback;
		int mouseX, mouseY;

        internal static void CheckGlError()
        {
			var n = Gl.glGetError();
			if (n != Gl.GL_NO_ERROR)
			    throw new InvalidOperationException("GL Error");
        }

		public GraphicsDevice( int width, int height, bool fullscreen, bool vsync )
		{
			Glfw.glfwInit();
			Glfw.glfwOpenWindow(width, height, 0, 0, 0, 0, 0, 0, /*fullscreen ? Glfw.GLFW_FULLSCREEN :*/ Glfw.GLFW_WINDOW);
			Glfw.glfwSetWindowTitle("OpenRA (OpenGL version)");
			bool initDone = false;
			Glfw.glfwSetMouseButtonCallback( mouseButtonCallback = ( button, action ) =>
				{
					var b = button == Glfw.GLFW_MOUSE_BUTTON_1 ? MouseButtons.Left
						: button == Glfw.GLFW_MOUSE_BUTTON_2 ? MouseButtons.Right
						: button == Glfw.GLFW_MOUSE_BUTTON_3 ? MouseButtons.Middle
						: 0;
					Game.DispatchMouseInput( action == Glfw.GLFW_PRESS ? MouseInputEvent.Down : MouseInputEvent.Up,
						new MouseEventArgs( b, action == Glfw.GLFW_PRESS ? 1 : 0, mouseX, mouseY, 0 ), 0 );
				} );
			Glfw.glfwSetMousePosCallback(mousePositionCallback = (x, y) =>
				{
					mouseX = x;
					mouseY = y;
					if (initDone)
						Game.DispatchMouseInput(MouseInputEvent.Move, new MouseEventArgs(0, 0, x, y, 0), 0);
				});
			Glfw.glfwSetWindowCloseCallback( windowCloseCallback = () =>
				{
					Game.Exit();
					Glfw.glfwIconifyWindow();
					return Gl.GL_TRUE;
				} );
			CheckGlError();

			windowSize = new Size( width, height );

			cgContext = Cg.cgCreateContext();
			Cg.cgSetErrorCallback( CgErrorCallback );

			CgGl.cgGLRegisterStates( cgContext );
			CgGl.cgGLSetManageTextureParameters( cgContext, true );
			vertexProfile = CgGl.cgGLGetLatestProfile( CgGl.CG_GL_VERTEX );
			fragmentProfile = CgGl.cgGLGetLatestProfile( CgGl.CG_GL_FRAGMENT );

			Gl.glEnableClientState( Gl.GL_VERTEX_ARRAY );
			CheckGlError();
			Gl.glEnableClientState( Gl.GL_TEXTURE_COORD_ARRAY );
			CheckGlError();

			initDone = true;
		}

		static Cg.CGerrorCallbackFuncDelegate CgErrorCallback = () =>
		{
			var err = Cg.cgGetError();
			var str = Cg.cgGetErrorString( err );
			throw new InvalidOperationException(
				string.Format( "CG Error: {0}: {1}", err, str ) );
		};

        public void EnableScissor(int left, int top, int width, int height)
        {
			if( width < 0 ) width = 0;
			if( height < 0 ) height = 0;
			Gl.glScissor( left, windowSize.Height - ( top + height ), width, height );
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
            Gl.glClearColor(0, 0, 0, 0);
            CheckGlError();
            Gl.glClear(Gl.GL_COLOR_BUFFER_BIT);
            CheckGlError();
        }

        public void Present()
        {
			Glfw.glfwSwapBuffers();
            CheckGlError();
        }

		public void DrawIndexedPrimitives( PrimitiveType pt, Range<int> vertices, Range<int> indices )
		{
			Gl.glDrawElements( ModeFromPrimitiveType( pt ), indices.End - indices.Start, Gl.GL_UNSIGNED_SHORT, new IntPtr( indices.Start * 2 ) );
			CheckGlError();
		}

		public void DrawIndexedPrimitives( PrimitiveType pt, int numVerts, int numPrimitives )
		{
			Gl.glDrawElements( ModeFromPrimitiveType( pt ), numPrimitives * IndicesPerPrimitive( pt ), Gl.GL_UNSIGNED_SHORT, IntPtr.Zero );
			CheckGlError();
		}

		static int ModeFromPrimitiveType( PrimitiveType pt )
		{
			switch( pt )
			{
			case PrimitiveType.PointList: return Gl.GL_POINTS;
			case PrimitiveType.LineList: return Gl.GL_LINES;
			case PrimitiveType.TriangleList: return Gl.GL_TRIANGLES;
			}
			throw new NotImplementedException();
		}

		static int IndicesPerPrimitive( PrimitiveType pt )
		{
			switch( pt )
			{
			case PrimitiveType.PointList: return 1;
			case PrimitiveType.LineList: return 2;
			case PrimitiveType.TriangleList: return 3;
			}
			throw new NotImplementedException();
		}

		#region IGraphicsDevice Members

		public IVertexBuffer<T> CreateVertexBuffer<T>( int size )
			where T : struct
		{
			return new VertexBuffer<T>( this, size );
		}

		public IIndexBuffer CreateIndexBuffer( int size )
		{
			return new IndexBuffer( this, size );
		}

		public ITexture CreateTexture( Bitmap bitmap )
		{
			return new Texture( this, bitmap );
		}

		public IShader CreateShader( Stream stream )
		{
			return new Shader( this, stream );
		}

		#endregion
	}

    public class VertexBuffer<T> : IVertexBuffer<T>, IDisposable
		where T : struct
    {
        int buffer;

        public VertexBuffer(GraphicsDevice dev, int size)
        {
            Gl.glGenBuffers(1, out buffer);
            GraphicsDevice.CheckGlError();
        }

        public void SetData(T[] data)
        {
            Bind();
            Gl.glBufferData(Gl.GL_ARRAY_BUFFER,
                new IntPtr(Marshal.SizeOf(typeof(T))*data.Length), data, Gl.GL_DYNAMIC_DRAW);
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

    public class IndexBuffer : IIndexBuffer, IDisposable
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

    public class Shader : IShader
    {
        IntPtr effect;
        IntPtr technique;
        GraphicsDevice dev;

        public Shader(GraphicsDevice dev, Stream s)
        {
            this.dev = dev;
			string code;
			using (var file = new StreamReader(s))
				code = file.ReadToEnd();
            effect = Cg.cgCreateEffect(dev.cgContext, code, null);

            if (effect == IntPtr.Zero)
            {
                var err = Cg.cgGetErrorString(Cg.cgGetError());
                var results = Cg.cgGetLastListing(dev.cgContext);
                throw new InvalidOperationException(
                    string.Format("Cg compile failed ({0}):\n{1}", err, results));
            }

			technique = Cg.cgGetFirstTechnique( effect );
			if( technique == IntPtr.Zero )
                throw new InvalidOperationException("No techniques");
			while( Cg.cgValidateTechnique( technique ) == 0 )
			{
				technique = Cg.cgGetNextTechnique( technique );
				if( technique == IntPtr.Zero )
	                throw new InvalidOperationException("No valid techniques");
			}
		}

        public void Render(Action a)
        {
            CgGl.cgGLEnableProfile(dev.vertexProfile);
            CgGl.cgGLEnableProfile(dev.fragmentProfile);

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

        public void SetValue(string name, ITexture t)
        {
			var texture = (Texture)t;
			var param = Cg.cgGetNamedEffectParameter( effect, name );
			if( param != IntPtr.Zero && texture != null )
				CgGl.cgGLSetupSampler( param, texture.texture );
        }

        public void SetValue(string name, float x, float y)
        {
            var param = Cg.cgGetNamedEffectParameter(effect, name);
			if( param != IntPtr.Zero )
				CgGl.cgGLSetParameter2f(param, x, y);
        }

        public void Commit() { }
    }

    public class Texture : ITexture
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
			Gl.glBindTexture( Gl.GL_TEXTURE_2D, texture );
			GraphicsDevice.CheckGlError();

            var bits = bitmap.LockBits(
                new Rectangle(0, 0, bitmap.Width, bitmap.Height),
                ImageLockMode.ReadOnly,
                PixelFormat.Format32bppArgb);

            Gl.glTexParameteri(Gl.GL_TEXTURE_2D, Gl.GL_TEXTURE_BASE_LEVEL, 0);
            GraphicsDevice.CheckGlError();
            Gl.glTexParameteri(Gl.GL_TEXTURE_2D, Gl.GL_TEXTURE_MAX_LEVEL, 0);
            GraphicsDevice.CheckGlError();
            Gl.glTexImage2D(Gl.GL_TEXTURE_2D, 0, Gl.GL_RGBA8, bits.Width, bits.Height, 
                0, Gl.GL_BGRA, Gl.GL_UNSIGNED_BYTE, bits.Scan0);        // todo: weird strides
            GraphicsDevice.CheckGlError();

            bitmap.UnlockBits(bits);
        }
    }
}
