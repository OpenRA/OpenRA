#region Copyright & License Information
/*
* Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
* This file is part of OpenRA, which is free software. It is made
* available to you under the terms of the GNU General Public License
* as published by the Free Software Foundation. For more information,
* see COPYING.
*/
#endregion

using System;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using OpenRA.Graphics;
using Tao.OpenGl;
using Tao.Sdl;

namespace OpenRA.Renderer.SdlCommon
{
	public abstract class SdlGraphics : IGraphicsDevice
	{
		Size windowSize;
		SdlInput input;

		public Size WindowSize { get { return windowSize; } }

		public SdlGraphics(Size size, WindowMode window, string[] extensions)
		{
			windowSize = size;
			InitializeSdlGl(ref windowSize, window, extensions);

			Gl.glEnableClientState(Gl.GL_VERTEX_ARRAY);
			ErrorHandler.CheckGlError();
			Gl.glEnableClientState(Gl.GL_TEXTURE_COORD_ARRAY);
			ErrorHandler.CheckGlError();

			Sdl.SDL_SetModState(0);

			input = new SdlInput();
		}

		IntPtr InitializeSdlGl(ref Size size, WindowMode window, string[] requiredExtensions)
		{
			Sdl.SDL_Init(Sdl.SDL_INIT_NOPARACHUTE | Sdl.SDL_INIT_VIDEO);
			Sdl.SDL_GL_SetAttribute(Sdl.SDL_GL_DOUBLEBUFFER, 1);
			Sdl.SDL_GL_SetAttribute(Sdl.SDL_GL_RED_SIZE, 8);
			Sdl.SDL_GL_SetAttribute(Sdl.SDL_GL_GREEN_SIZE, 8);
			Sdl.SDL_GL_SetAttribute(Sdl.SDL_GL_BLUE_SIZE, 8);
			Sdl.SDL_GL_SetAttribute(Sdl.SDL_GL_ALPHA_SIZE, 0);

			int windowFlags = 0;
			switch (window)
			{
			case WindowMode.Fullscreen:
				windowFlags |= Sdl.SDL_FULLSCREEN;
				break;
			case WindowMode.PseudoFullscreen:
				windowFlags |= Sdl.SDL_NOFRAME;
				Environment.SetEnvironmentVariable("SDL_VIDEO_WINDOW_POS", "0,0");
				break;
			case WindowMode.Windowed:
				Environment.SetEnvironmentVariable("SDL_VIDEO_CENTERED", "1");
				break;
			default:
				break;
			}

			var info = (Sdl.SDL_VideoInfo)Marshal.PtrToStructure(
				Sdl.SDL_GetVideoInfo(), typeof(Sdl.SDL_VideoInfo));
			Console.WriteLine("Desktop resolution: {0}x{1}",
				info.current_w, info.current_h);

			if (size.Width == 0 && size.Height == 0)
			{
				Console.WriteLine("No custom resolution provided, using desktop resolution");
				size = new Size(info.current_w, info.current_h);
			}

			Console.WriteLine("Using resolution: {0}x{1}", size.Width, size.Height);

			var surf = Sdl.SDL_SetVideoMode(size.Width, size.Height, 0, Sdl.SDL_OPENGL | windowFlags);
			if (surf == IntPtr.Zero)
				Console.WriteLine("Failed to set video mode.");

			Sdl.SDL_WM_SetCaption("OpenRA", "OpenRA");
			Sdl.SDL_ShowCursor(0);
			Sdl.SDL_EnableUNICODE(1);
			Sdl.SDL_EnableKeyRepeat(Sdl.SDL_DEFAULT_REPEAT_DELAY, Sdl.SDL_DEFAULT_REPEAT_INTERVAL);

			ErrorHandler.CheckGlError();

			var extensions = Gl.glGetString(Gl.GL_EXTENSIONS);
			if (extensions == null)
				Console.WriteLine("Failed to fetch GL_EXTENSIONS, this is bad.");

			var missingExtensions = requiredExtensions.Where(r => !extensions.Contains(r)).ToArray();

			if (missingExtensions.Any())
			{
				ErrorHandler.WriteGraphicsLog("Unsupported GPU: Missing extensions: {0}"
					.F(missingExtensions.JoinWith(",")));
				throw new InvalidProgramException("Unsupported GPU. See graphics.log for details.");
			}

			return surf;
		}

		public virtual void Quit()
		{
			Sdl.SDL_Quit();
		}

		int ModeFromPrimitiveType(PrimitiveType pt)
		{
			switch (pt)
			{
			case PrimitiveType.PointList: return Gl.GL_POINTS;
			case PrimitiveType.LineList: return Gl.GL_LINES;
			case PrimitiveType.TriangleList: return Gl.GL_TRIANGLES;
			case PrimitiveType.QuadList: return Gl.GL_QUADS;
			}

			throw new NotImplementedException();
		}

		public void DrawPrimitives(PrimitiveType pt, int firstVertex, int numVertices)
		{
			Gl.glDrawArrays(ModeFromPrimitiveType(pt), firstVertex, numVertices);
			ErrorHandler.CheckGlError();
		}

		public void Clear()
		{
			Gl.glClearColor(0, 0, 0, 0);
			ErrorHandler.CheckGlError();
			Gl.glClear(Gl.GL_COLOR_BUFFER_BIT);
			ErrorHandler.CheckGlError();
		}

		public void EnableDepthBuffer()
		{
			Gl.glClear(Gl.GL_DEPTH_BUFFER_BIT);
			ErrorHandler.CheckGlError();
			Gl.glEnable(Gl.GL_DEPTH_TEST);
			ErrorHandler.CheckGlError();
		}

		public void DisableDepthBuffer()
		{
			Gl.glDisable(Gl.GL_DEPTH_TEST);
			ErrorHandler.CheckGlError();
		}

		public void SetBlendMode(BlendMode mode)
		{
			Gl.glBlendEquation(Gl.GL_FUNC_ADD);
			ErrorHandler.CheckGlError();

			switch (mode)
			{
				case BlendMode.None:
					Gl.glDisable(Gl.GL_BLEND);
					break;
				case BlendMode.Alpha:
					Gl.glEnable(Gl.GL_BLEND);
					ErrorHandler.CheckGlError();
					Gl.glBlendFunc(Gl.GL_SRC_ALPHA, Gl.GL_ONE_MINUS_SRC_ALPHA);
					break;
				case BlendMode.Additive:
					Gl.glEnable(Gl.GL_BLEND);
					ErrorHandler.CheckGlError();
					Gl.glBlendFunc(Gl.GL_ONE, Gl.GL_ONE);
					break;
				case BlendMode.Subtractive:
					Gl.glEnable(Gl.GL_BLEND);
					ErrorHandler.CheckGlError();
					Gl.glBlendFunc(Gl.GL_ONE, Gl.GL_ONE);
					ErrorHandler.CheckGlError();
					Gl.glBlendEquation(Gl.GL_FUNC_REVERSE_SUBTRACT);
					break;
				case BlendMode.Multiply:
					Gl.glEnable(Gl.GL_BLEND);
					ErrorHandler.CheckGlError();
					Gl.glBlendFuncSeparate(Gl.GL_DST_COLOR, Gl.GL_ZERO, Gl.GL_ONE, Gl.GL_ONE_MINUS_SRC_ALPHA);
					ErrorHandler.CheckGlError();
					break;
			}
			ErrorHandler.CheckGlError();
		}

		public void EnableScissor(int left, int top, int width, int height)
		{
			if (width < 0) width = 0;
			if (height < 0) height = 0;

			Gl.glScissor(left, windowSize.Height - (top + height), width, height);
			ErrorHandler.CheckGlError();
			Gl.glEnable(Gl.GL_SCISSOR_TEST);
			ErrorHandler.CheckGlError();
		}

		public void DisableScissor()
		{
			Gl.glDisable(Gl.GL_SCISSOR_TEST);
			ErrorHandler.CheckGlError();
		}

		public void SetLineWidth(float width)
		{
			Gl.glLineWidth(width);
			ErrorHandler.CheckGlError();
		}

		public void Present() { Sdl.SDL_GL_SwapBuffers(); }
		public void PumpInput(IInputHandler inputHandler) { input.PumpInput(inputHandler); }
		public IVertexBuffer<Vertex> CreateVertexBuffer(int size) { return new VertexBuffer<Vertex>(size); }
		public ITexture CreateTexture() { return new Texture(); }
		public ITexture CreateTexture(Bitmap bitmap) { return new Texture(bitmap); }
		public IFrameBuffer CreateFrameBuffer(Size s) { return new FrameBuffer(s); }
		public abstract IShader CreateShader(string name);
	}
}