#region Copyright & License Information
/*
* Copyright 2007-2015 The OpenRA Developers (see AUTHORS)
* This file is part of OpenRA, which is free software. It is made
* available to you under the terms of the GNU General Public License
* as published by the Free Software Foundation. For more information,
* see COPYING.
*/
#endregion

using System;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using OpenRA;
using OpenRA.Graphics;
using OpenTK.Graphics.OpenGL;
using SDL2;

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

	public sealed class Sdl2GraphicsDevice : IGraphicsDevice
	{
		Size size;
		Sdl2Input input;
		IntPtr context, window;
		bool disposed;

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

			if (Game.Settings.Game.LockMouseWindow)
				GrabWindowMouseFocus();
			else
				ReleaseWindowMouseFocus();

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

			context = SDL.SDL_GL_CreateContext(window);
			SDL.SDL_GL_MakeCurrent(window, context);
			GL.LoadAll();
			ErrorHandler.CheckGlVersion();
			ErrorHandler.CheckGlError();

			if (SDL.SDL_GL_ExtensionSupported("GL_EXT_framebuffer_object") == SDL.SDL_bool.SDL_FALSE)
			{
				ErrorHandler.WriteGraphicsLog("OpenRA requires the OpenGL extension GL_EXT_framebuffer_object.\n"
					+ "Please try updating your GPU driver to the latest version provided by the manufacturer.");
				throw new InvalidProgramException("Missing OpenGL extension GL_EXT_framebuffer_object. See graphics.log for details.");
			}

			GL.EnableClientState(ArrayCap.VertexArray);
			ErrorHandler.CheckGlError();
			GL.EnableClientState(ArrayCap.TextureCoordArray);
			ErrorHandler.CheckGlError();

			SDL.SDL_SetModState(0);
			input = new Sdl2Input();
		}

		public IHardwareCursor CreateHardwareCursor(string name, Size size, byte[] data, int2 hotspot)
		{
			var c = new SDL2HardwareCursor(size, data, hotspot);
			if (c.Cursor == IntPtr.Zero)
				throw new InvalidDataException("Failed to create hardware cursor `{0}`: {1}".F(name, SDL.SDL_GetError()));

			return c;
		}

		public void SetHardwareCursor(IHardwareCursor cursor)
		{
			var c = cursor as SDL2HardwareCursor;
			if (c == null)
				SDL.SDL_ShowCursor(0);
			else
			{
				SDL.SDL_ShowCursor(1);
				SDL.SDL_SetCursor(c.Cursor);
			}
		}

		class SDL2HardwareCursor : IHardwareCursor
		{
			public readonly IntPtr Cursor;
			readonly IntPtr surface;

			public SDL2HardwareCursor(Size size, byte[] data, int2 hotspot)
			{
				surface = SDL.SDL_CreateRGBSurface(0, size.Width, size.Height, 32, 0x00FF0000, 0x0000FF00, 0x000000FF, 0xFF000000);

				var sur = (SDL2.SDL.SDL_Surface)Marshal.PtrToStructure(surface, typeof(SDL2.SDL.SDL_Surface));
				Marshal.Copy(data, 0, sur.pixels, data.Length);
				Cursor = SDL.SDL_CreateColorCursor(surface, hotspot.X, hotspot.Y);
			}

			public void Dispose()
			{
				SDL.SDL_FreeCursor(Cursor);
				SDL.SDL_FreeSurface(surface);
			}
		}

		public void Dispose()
		{
			if (disposed)
				return;

			disposed = true;
			if (context != IntPtr.Zero)
			{
				SDL.SDL_GL_DeleteContext(context);
				context = IntPtr.Zero;
			}

			if (window != IntPtr.Zero)
			{
				SDL.SDL_DestroyWindow(window);
				window = IntPtr.Zero;
			}

			SDL.SDL_Quit();
		}

		static BeginMode ModeFromPrimitiveType(PrimitiveType pt)
		{
			switch (pt)
			{
				case PrimitiveType.PointList: return BeginMode.Points;
				case PrimitiveType.LineList: return BeginMode.Lines;
				case PrimitiveType.TriangleList: return BeginMode.Triangles;
				case PrimitiveType.QuadList: return BeginMode.Quads;
			}

			throw new NotImplementedException();
		}

		public void DrawPrimitives(PrimitiveType pt, int firstVertex, int numVertices)
		{
			GL.DrawArrays(ModeFromPrimitiveType(pt), firstVertex, numVertices);
			ErrorHandler.CheckGlError();
		}

		public void Clear()
		{
			GL.ClearColor(0, 0, 0, 0);
			ErrorHandler.CheckGlError();
			GL.Clear(ClearBufferMask.ColorBufferBit);
			ErrorHandler.CheckGlError();
		}

		public void EnableDepthBuffer()
		{
			GL.Clear(ClearBufferMask.DepthBufferBit);
			ErrorHandler.CheckGlError();
			GL.Enable(EnableCap.DepthTest);
			ErrorHandler.CheckGlError();
		}

		public void DisableDepthBuffer()
		{
			GL.Disable(EnableCap.DepthTest);
			ErrorHandler.CheckGlError();
		}

		public void SetBlendMode(BlendMode mode)
		{
			GL.BlendEquation(BlendEquationMode.FuncAdd);
			ErrorHandler.CheckGlError();

			switch (mode)
			{
				case BlendMode.None:
					GL.Disable(EnableCap.Blend);
					break;
				case BlendMode.Alpha:
					GL.Enable(EnableCap.Blend);
					ErrorHandler.CheckGlError();
					GL.BlendFunc(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha);
					break;
				case BlendMode.Additive:
					GL.Enable(EnableCap.Blend);
					ErrorHandler.CheckGlError();
					GL.BlendFunc(BlendingFactorSrc.One, BlendingFactorDest.One);
					break;
				case BlendMode.Subtractive:
					GL.Enable(EnableCap.Blend);
					ErrorHandler.CheckGlError();
					GL.BlendFunc(BlendingFactorSrc.One, BlendingFactorDest.One);
					ErrorHandler.CheckGlError();
					GL.BlendEquation(BlendEquationMode.FuncReverseSubtract);
					break;
				case BlendMode.Multiply:
					GL.Enable(EnableCap.Blend);
					ErrorHandler.CheckGlError();
					GL.BlendFuncSeparate(BlendingFactorSrc.DstColor, BlendingFactorDest.Zero, BlendingFactorSrc.One, BlendingFactorDest.OneMinusSrcAlpha);
					ErrorHandler.CheckGlError();
					break;
				case BlendMode.SoftAdditive:
					GL.Enable(EnableCap.Blend);
					ErrorHandler.CheckGlError();
					GL.BlendFunc(BlendingFactorSrc.OneMinusDstColor, BlendingFactorDest.One);
					break;
				case BlendMode.Translucency25:
					GL.Enable(EnableCap.Blend);
					ErrorHandler.CheckGlError();
					GL.BlendFunc(BlendingFactorSrc.OneMinusConstantAlpha, BlendingFactorDest.One);
					ErrorHandler.CheckGlError();
					GL.BlendColor(1f, 1f, 1f, 0.25f);
					break;
				case BlendMode.Translucency50:
					GL.Enable(EnableCap.Blend);
					ErrorHandler.CheckGlError();
					GL.BlendFunc(BlendingFactorSrc.OneMinusConstantAlpha, BlendingFactorDest.One);
					ErrorHandler.CheckGlError();
					GL.BlendColor(1f, 1f, 1f, 0.5f);
					break;
				case BlendMode.Translucency75:
					GL.Enable(EnableCap.Blend);
					ErrorHandler.CheckGlError();
					GL.BlendFunc(BlendingFactorSrc.OneMinusConstantAlpha, BlendingFactorDest.One);
					ErrorHandler.CheckGlError();
					GL.BlendColor(1f, 1f, 1f, 0.75f);
					break;
				case BlendMode.Multiplicative:
					GL.Enable(EnableCap.Blend);
					ErrorHandler.CheckGlError();
					GL.BlendFunc(BlendingFactorSrc.Zero, BlendingFactorDest.SrcColor);
					break;
				case BlendMode.DoubleMultiplicative:
					GL.Enable(EnableCap.Blend);
					ErrorHandler.CheckGlError();
					GL.BlendFunc(BlendingFactorSrc.DstColor, BlendingFactorDest.SrcColor);
					break;
			}

			ErrorHandler.CheckGlError();
		}

		public void GrabWindowMouseFocus()
		{
			SDL.SDL_SetWindowGrab(window, SDL.SDL_bool.SDL_TRUE);
		}

		public void ReleaseWindowMouseFocus()
		{
			SDL.SDL_SetWindowGrab(window, SDL.SDL_bool.SDL_FALSE);
		}

		public void EnableScissor(int left, int top, int width, int height)
		{
			if (width < 0)
				width = 0;

			if (height < 0)
				height = 0;

			GL.Scissor(left, size.Height - (top + height), width, height);
			ErrorHandler.CheckGlError();
			GL.Enable(EnableCap.ScissorTest);
			ErrorHandler.CheckGlError();
		}

		public void DisableScissor()
		{
			GL.Disable(EnableCap.ScissorTest);
			ErrorHandler.CheckGlError();
		}

		public void SetLineWidth(float width)
		{
			GL.LineWidth(width);
			ErrorHandler.CheckGlError();
		}

		public void Present() { SDL.SDL_GL_SwapWindow(window); }
		public void PumpInput(IInputHandler inputHandler) { input.PumpInput(inputHandler); }
		public string GetClipboardText() { return input.GetClipboardText(); }
		public IVertexBuffer<Vertex> CreateVertexBuffer(int size) { return new VertexBuffer<Vertex>(size); }
		public ITexture CreateTexture() { return new Texture(); }
		public ITexture CreateTexture(Bitmap bitmap) { return new Texture(bitmap); }
		public IFrameBuffer CreateFrameBuffer(Size s) { return new FrameBuffer(s); }
		public IShader CreateShader(string name) { return new Shader(name); }
	}
}
