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
using OpenRA.FileFormats.Graphics;
using Tao.Cg;
using Tao.OpenGl;
using Tao.Sdl;

[assembly: Renderer( typeof( OpenRA.GlRenderer.GraphicsDevice ))]

namespace OpenRA.GlRenderer
{
	public class GraphicsDevice : IGraphicsDevice
	{
		Size windowSize;
		internal IntPtr cgContext;
		internal int vertexProfile, fragmentProfile;

		IntPtr surf;

		public Size WindowSize { get { return windowSize; } }

		internal static void CheckGlError()
		{
			var n = Gl.glGetError();
			if (n != Gl.GL_NO_ERROR)
				throw new InvalidOperationException("GL Error");
		}

		public GraphicsDevice(int width, int height, bool windowed, bool vsync)
		{
			Sdl.SDL_Init(Sdl.SDL_INIT_NOPARACHUTE | Sdl.SDL_INIT_VIDEO);
			Sdl.SDL_GL_SetAttribute(Sdl.SDL_GL_DOUBLEBUFFER, 1);
			Sdl.SDL_GL_SetAttribute(Sdl.SDL_GL_RED_SIZE, 8);
			Sdl.SDL_GL_SetAttribute(Sdl.SDL_GL_GREEN_SIZE, 8);
			Sdl.SDL_GL_SetAttribute(Sdl.SDL_GL_BLUE_SIZE, 8);
			Sdl.SDL_GL_SetAttribute(Sdl.SDL_GL_ALPHA_SIZE, 0);

			if (Environment.OSVersion.Platform == PlatformID.Win32NT)
			{
				// pseudo-fullscreen, for sane debugging.
				Environment.SetEnvironmentVariable("SDL_VIDEO_WINDOW_POS", "0,0");
				surf = Sdl.SDL_SetVideoMode(width, height, 0, Sdl.SDL_NOFRAME | Sdl.SDL_OPENGL | (windowed ? 0 : Sdl.SDL_FULLSCREEN));
			}
			else
			{
				// OSX doesn't like this, due to quirks of their WM.
				surf = Sdl.SDL_SetVideoMode(width, height, 0, Sdl.SDL_OPENGL | (windowed ? 0 : Sdl.SDL_FULLSCREEN));
			}

			Sdl.SDL_WM_SetCaption("OpenRA", "OpenRA");
			Sdl.SDL_ShowCursor(0);
			Sdl.SDL_EnableUNICODE(1);
			Sdl.SDL_EnableKeyRepeat(Sdl.SDL_DEFAULT_REPEAT_INTERVAL, Sdl.SDL_DEFAULT_REPEAT_DELAY);

			CheckGlError();

			windowSize = new Size(width, height);

			cgContext = Cg.cgCreateContext();

			Cg.cgSetErrorCallback(CgErrorCallback);

			CgGl.cgGLRegisterStates(cgContext);
			CgGl.cgGLSetManageTextureParameters(cgContext, true);
			vertexProfile = CgGl.cgGLGetLatestProfile(CgGl.CG_GL_VERTEX);
			fragmentProfile = CgGl.cgGLGetLatestProfile(CgGl.CG_GL_FRAGMENT);

			Console.WriteLine("VP Profile: " + vertexProfile);
			Console.WriteLine("FP Profile: " + fragmentProfile);

			Gl.glEnableClientState(Gl.GL_VERTEX_ARRAY);
			CheckGlError();
			Gl.glEnableClientState(Gl.GL_TEXTURE_COORD_ARRAY);
			CheckGlError();
		}

		static Cg.CGerrorCallbackFuncDelegate CgErrorCallback = () =>
		{
			var err = Cg.cgGetError();
			var str = Cg.cgGetErrorString(err);
			throw new InvalidOperationException(
				string.Format("CG Error: {0}: {1}", err, str));
		};

		public void EnableScissor(int left, int top, int width, int height)
		{
			if (width < 0) width = 0;
			if (height < 0) height = 0;
			Gl.glScissor(left, windowSize.Height - (top + height), width, height);
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

		MouseButtons lastButtonBits = (MouseButtons)0;

		static MouseButtons MakeButton(byte b)
		{
			return b == Sdl.SDL_BUTTON_LEFT ? MouseButtons.Left
							: b == Sdl.SDL_BUTTON_RIGHT ? MouseButtons.Right
							: b == Sdl.SDL_BUTTON_MIDDLE ? MouseButtons.Middle
							: 0;
		}

		static Modifiers MakeModifiers(int raw)
		{
			return ((raw & Sdl.KMOD_ALT) != 0 ? Modifiers.Alt : 0)
				| ((raw & Sdl.KMOD_CTRL) != 0 ? Modifiers.Ctrl : 0)
				| ((raw & Sdl.KMOD_SHIFT) != 0 ? Modifiers.Shift : 0);
		}

		public void Present()
		{
			Sdl.SDL_GL_SwapBuffers();

			var mods = MakeModifiers(Sdl.SDL_GetModState());
			Game.HandleModifierKeys(mods);

			Sdl.SDL_Event e;
			while (Sdl.SDL_PollEvent(out e) != 0)
			{
				switch (e.type)
				{
					case Sdl.SDL_QUIT:
						OpenRA.Game.Exit();
						break;

					case Sdl.SDL_MOUSEBUTTONDOWN:
						{
							var button = MakeButton(e.button.button);
							lastButtonBits |= button;

							Game.DispatchMouseInput(MouseInputEvent.Down,
								new MouseEventArgs(button, 1, e.button.x, e.button.y, 0),
								mods);
						} break;

					case Sdl.SDL_MOUSEBUTTONUP:
						{
							var button = MakeButton(e.button.button);
							lastButtonBits &= ~button;

							Game.DispatchMouseInput(MouseInputEvent.Up,
								new MouseEventArgs(button, 1, e.button.x, e.button.y, 0),
								mods);
						} break;

					case Sdl.SDL_MOUSEMOTION:
						{
							Game.DispatchMouseInput(MouseInputEvent.Move,
								new MouseEventArgs(lastButtonBits, 0, e.motion.x, e.motion.y, 0),
								mods);
						} break;

					case Sdl.SDL_KEYDOWN:
						{
							switch (e.key.keysym.sym)
							{
								case Sdl.SDLK_UP: Game.HandleArrowKeyScroll("up", true); break;
								case Sdl.SDLK_LEFT: Game.HandleArrowKeyScroll("left", true); break;
								case Sdl.SDLK_DOWN: Game.HandleArrowKeyScroll("down", true); break;
								case Sdl.SDLK_RIGHT: Game.HandleArrowKeyScroll("right", true); break;
							}

							if (e.key.keysym.unicode != 0)
								Game.HandleKeyPress(new KeyPressEventArgs((char)e.key.keysym.unicode), mods);

							else if (mods != 0)
							{
								var keyName = Sdl.SDL_GetKeyName(e.key.keysym.sym);
								if (keyName.Length == 1)
									Game.HandleKeyPress(new KeyPressEventArgs(keyName[0]), mods);
								else if (keyName == "f4" && ((mods & Modifiers.Alt) != 0))
									OpenRA.Game.Exit();
							}
						} break;

					case Sdl.SDL_KEYUP:
						{
							switch (e.key.keysym.sym)
							{
								case Sdl.SDLK_UP: Game.HandleArrowKeyScroll("up", false); break;
								case Sdl.SDLK_LEFT: Game.HandleArrowKeyScroll("left", false); break;
								case Sdl.SDLK_DOWN: Game.HandleArrowKeyScroll("down", false); break;
								case Sdl.SDLK_RIGHT: Game.HandleArrowKeyScroll("right", false); break;
							}
						} break;
				}
			}

			CheckGlError();
		}

		public void DrawIndexedPrimitives(PrimitiveType pt, Range<int> vertices, Range<int> indices)
		{
			Gl.glDrawElements(ModeFromPrimitiveType(pt), indices.End - indices.Start, Gl.GL_UNSIGNED_SHORT, new IntPtr(indices.Start * 2));
			CheckGlError();
		}

		public void DrawIndexedPrimitives(PrimitiveType pt, int numVerts, int numPrimitives)
		{
			Gl.glDrawElements(ModeFromPrimitiveType(pt), numPrimitives * IndicesPerPrimitive(pt), Gl.GL_UNSIGNED_SHORT, IntPtr.Zero);
			CheckGlError();
		}

		static int ModeFromPrimitiveType(PrimitiveType pt)
		{
			switch (pt)
			{
				case PrimitiveType.PointList: return Gl.GL_POINTS;
				case PrimitiveType.LineList: return Gl.GL_LINES;
				case PrimitiveType.TriangleList: return Gl.GL_TRIANGLES;
			}
			throw new NotImplementedException();
		}

		static int IndicesPerPrimitive(PrimitiveType pt)
		{
			switch (pt)
			{
				case PrimitiveType.PointList: return 1;
				case PrimitiveType.LineList: return 2;
				case PrimitiveType.TriangleList: return 3;
			}
			throw new NotImplementedException();
		}

		#region IGraphicsDevice Members

		public IVertexBuffer<Vertex> CreateVertexBuffer(int size)
		{
			return new VertexBuffer<Vertex>(this, size);
		}

		public IIndexBuffer CreateIndexBuffer(int size)
		{
			return new IndexBuffer(this, size);
		}

		public ITexture CreateTexture(Bitmap bitmap)
		{
			return new Texture(this, bitmap);
		}

		public IShader CreateShader(Stream stream)
		{
			return new Shader(this, stream);
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
				new IntPtr(Marshal.SizeOf(typeof(T)) * data.Length), data, Gl.GL_DYNAMIC_DRAW);
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

			technique = Cg.cgGetFirstTechnique(effect);
			if (technique == IntPtr.Zero)
				throw new InvalidOperationException("No techniques");
			while (Cg.cgValidateTechnique(technique) == 0)
			{
				technique = Cg.cgGetNextTechnique(technique);
				if (technique == IntPtr.Zero)
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
			var param = Cg.cgGetNamedEffectParameter(effect, name);
			if (param != IntPtr.Zero && texture != null)
				CgGl.cgGLSetupSampler(param, texture.texture);
		}

		public void SetValue(string name, float x, float y)
		{
			var param = Cg.cgGetNamedEffectParameter(effect, name);
			if (param != IntPtr.Zero)
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
			if (!IsPowerOf2(bitmap.Width) || !IsPowerOf2(bitmap.Height))
			{
				//throw new InvalidOperationException( "non-power-of-2-texture" );
				bitmap = new Bitmap(bitmap, new Size(NextPowerOf2(bitmap.Width), NextPowerOf2(bitmap.Height)));
			}

			Gl.glBindTexture(Gl.GL_TEXTURE_2D, texture);
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

		bool IsPowerOf2(int v)
		{
			return (v & (v - 1)) == 0;
		}

		int NextPowerOf2(int v)
		{
			--v;
			v |= v >> 1;
			v |= v >> 2;
			v |= v >> 4;
			v |= v >> 8;
			++v;
			return v;
		}
	}
}
