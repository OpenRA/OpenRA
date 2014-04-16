#region Copyright & License Information
/*
* Copyright 2007-2013 The OpenRA Developers (see AUTHORS)
* This file is part of OpenRA, which is free software. It is made
* available to you under the terms of the GNU General Public License
* as published by the Free Software Foundation. For more information,
* see COPYING.
*/
#endregion

using System;
using System.Drawing;
using System.Linq;
using OpenRA;
using OpenRA.Graphics;
using OpenRA.Renderer.Glsl;
using OpenRA.Renderer.SdlCommon;
using SDL2;
using Tao.OpenGl;

[assembly: Renderer(typeof(OpenRA.Renderer.Sdl2.DeviceFactory))]

namespace OpenRA.Renderer.Sdl2
{
	public class DeviceFactory : IDeviceFactory
	{
		public IGraphicsDevice Create(Size size, WindowMode windowMode)
		{
			Console.WriteLine("Using SDL 2 with OpenGL renderer");
			return new Sdl2GraphicsDevice(size, windowMode);
		}
	}

	public class Sdl2GraphicsDevice : IGraphicsDevice
	{
		static string[] requiredExtensions =
		{
			"GL_ARB_vertex_shader",
			"GL_ARB_fragment_shader",
			"GL_ARB_vertex_buffer_object",
			"GL_EXT_framebuffer_object"
		};

		Size size;
		Sdl2Input input;
		IntPtr window;

		public Size WindowSize { get { return size; } }

		public Sdl2GraphicsDevice(Size windowSize, WindowMode windowMode)
		{
			size = windowSize;

			SDL.SDL_Init(SDL.SDL_INIT_NOPARACHUTE | SDL.SDL_INIT_VIDEO);
			SDL.SDL_GL_SetAttribute(SDL.SDL_GLattr.SDL_GL_DOUBLEBUFFER, 1);
			SDL.SDL_GL_SetAttribute(SDL.SDL_GLattr.SDL_GL_RED_SIZE, 8);
			SDL.SDL_GL_SetAttribute(SDL.SDL_GLattr.SDL_GL_GREEN_SIZE, 8);
			SDL.SDL_GL_SetAttribute(SDL.SDL_GLattr.SDL_GL_BLUE_SIZE, 8);
			SDL.SDL_GL_SetAttribute(SDL.SDL_GLattr.SDL_GL_ALPHA_SIZE, 0);

			SDL.SDL_DisplayMode display;
			SDL.SDL_GetCurrentDisplayMode(0, out display);

			Console.WriteLine("Desktop resolution: {0}x{1}", display.w, display.h);
			if (size.Width == 0 && size.Height == 0)
			{
				Console.WriteLine("No custom resolution provided, using desktop resolution");
				size = new Size(display.w, display.h);
			}

			Console.WriteLine("Using resolution: {0}x{1}", size.Width, size.Height);

			window = SDL.SDL_CreateWindow("OpenRA", SDL.SDL_WINDOWPOS_CENTERED, SDL.SDL_WINDOWPOS_CENTERED, size.Width, size.Height, SDL.SDL_WindowFlags.SDL_WINDOW_OPENGL);

			if (windowMode == WindowMode.Fullscreen)
				SDL.SDL_SetWindowFullscreen(window, (uint)SDL.SDL_WindowFlags.SDL_WINDOW_FULLSCREEN);
			else if (windowMode == WindowMode.PseudoFullscreen)
			{
				// Work around a visual glitch in OSX: the window is offset
				// partially offscreen if the dock is at the left of the screen
				if (Platform.CurrentPlatform == PlatformType.OSX)
					SDL.SDL_SetWindowPosition(window, 0, 0);

				SDL.SDL_SetWindowFullscreen(window, (uint)SDL.SDL_WindowFlags.SDL_WINDOW_FULLSCREEN_DESKTOP);
				SDL.SDL_SetHint(SDL.SDL_HINT_VIDEO_MINIMIZE_ON_FOCUS_LOSS, "0");
			}

			SDL.SDL_ShowCursor(0);
			SDL.SDL_GL_CreateContext(window);
			ErrorHandler.CheckGlError();

			var extensions = Gl.glGetString(Gl.GL_EXTENSIONS);
			if (extensions == null)
				Console.WriteLine("Failed to fetch GL_EXTENSIONS, this is bad.");

			var missingExtensions = requiredExtensions.Where(r => !extensions.Contains(r)).ToArray();
			if (missingExtensions.Any())
			{
				ErrorHandler.WriteGraphicsLog("Unsupported GPU: Missing extensions: {0}".F(missingExtensions.JoinWith(",")));
				throw new InvalidProgramException("Unsupported GPU. See graphics.log for details.");
			}

			Gl.glEnableClientState(Gl.GL_VERTEX_ARRAY);
			ErrorHandler.CheckGlError();
			Gl.glEnableClientState(Gl.GL_TEXTURE_COORD_ARRAY);
			ErrorHandler.CheckGlError();

			SDL.SDL_SetModState(0);
			input = new Sdl2Input();
		}

		public virtual void Quit()
		{
			SDL.SDL_Quit();
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
			if (width < 0)
				width = 0;

			if (height < 0)
				height = 0;

			Gl.glScissor(left, size.Height - (top + height), width, height);
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

		public void Present() { SDL.SDL_GL_SwapWindow(window); }
		public void PumpInput(IInputHandler inputHandler) { input.PumpInput(inputHandler); }
		public IVertexBuffer<Vertex> CreateVertexBuffer(int size) { return new VertexBuffer<Vertex>(size); }
		public ITexture CreateTexture() { return new Texture(); }
		public ITexture CreateTexture(Bitmap bitmap) { return new Texture(bitmap); }
		public IFrameBuffer CreateFrameBuffer(Size s) { return new FrameBuffer(s); }
		public IShader CreateShader(string name) { return new Shader(name); }
	}
}
