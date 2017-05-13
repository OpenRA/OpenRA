#region Copyright & License Information
/*
 * Copyright 2007-2017 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using OpenRA.Graphics;
using SDL2;

namespace OpenRA.Platforms.Default
{
	sealed class Sdl2GraphicsDevice : ThreadAffine, IGraphicsDevice
	{
		readonly Sdl2Input input;

		IntPtr context, window;
		bool disposed;

		public Size WindowSize { get; private set; }
		public float WindowScale { get; private set; }

		internal Size SurfaceSize { get; private set; }
		public event Action<float, float> OnWindowScaleChanged = (before, after) => { };

		[DllImport("user32.dll")]
		static extern bool SetProcessDPIAware();

		public Sdl2GraphicsDevice(Size windowSize, WindowMode windowMode)
		{
			Console.WriteLine("Using SDL 2 with OpenGL renderer");
			WindowSize = windowSize;

			// Disable legacy scaling on Windows
			if (Platform.CurrentPlatform == PlatformType.Windows && !Game.Settings.Graphics.DisableWindowsDPIScaling)
				SetProcessDPIAware();

			SDL.SDL_Init(SDL.SDL_INIT_NOPARACHUTE | SDL.SDL_INIT_VIDEO);
			SDL.SDL_GL_SetAttribute(SDL.SDL_GLattr.SDL_GL_DOUBLEBUFFER, 1);
			SDL.SDL_GL_SetAttribute(SDL.SDL_GLattr.SDL_GL_RED_SIZE, 8);
			SDL.SDL_GL_SetAttribute(SDL.SDL_GLattr.SDL_GL_GREEN_SIZE, 8);
			SDL.SDL_GL_SetAttribute(SDL.SDL_GLattr.SDL_GL_BLUE_SIZE, 8);
			SDL.SDL_GL_SetAttribute(SDL.SDL_GLattr.SDL_GL_ALPHA_SIZE, 0);

			SDL.SDL_DisplayMode display;
			SDL.SDL_GetCurrentDisplayMode(0, out display);

			Console.WriteLine("Desktop resolution: {0}x{1}", display.w, display.h);
			if (WindowSize.Width == 0 && WindowSize.Height == 0)
			{
				Console.WriteLine("No custom resolution provided, using desktop resolution");
				WindowSize = new Size(display.w, display.h);
			}

			Console.WriteLine("Using resolution: {0}x{1}", WindowSize.Width, WindowSize.Height);

			var windowFlags = SDL.SDL_WindowFlags.SDL_WINDOW_OPENGL | SDL.SDL_WindowFlags.SDL_WINDOW_ALLOW_HIGHDPI;

			// HiDPI doesn't work properly on OSX with (legacy) fullscreen mode
			if (Platform.CurrentPlatform == PlatformType.OSX && windowMode == WindowMode.Fullscreen)
				SDL.SDL_SetHint(SDL.SDL_HINT_VIDEO_HIGHDPI_DISABLED, "1");

			window = SDL.SDL_CreateWindow("OpenRA", SDL.SDL_WINDOWPOS_CENTERED, SDL.SDL_WINDOWPOS_CENTERED,
				WindowSize.Width, WindowSize.Height, windowFlags);

			SurfaceSize = WindowSize;
			WindowScale = 1;

			// Enable high resolution rendering for Retina displays
			if (Platform.CurrentPlatform == PlatformType.OSX)
			{
				// OSX defines the window size in "points", with a device-dependent number of pixels per point.
				// The window scale is simply the ratio of GL pixels / window points.
				int width, height;
				SDL.SDL_GL_GetDrawableSize(window, out width, out height);
				SurfaceSize = new Size(width, height);
				WindowScale = width * 1f / WindowSize.Width;
			}
			else if (Platform.CurrentPlatform == PlatformType.Windows)
			{
				float ddpi, hdpi, vdpi;
				if (!Game.Settings.Graphics.DisableWindowsDPIScaling && SDL.SDL_GetDisplayDPI(0, out ddpi, out hdpi, out vdpi) == 0)
				{
					WindowScale = ddpi / 96;
					WindowSize = new Size((int)(SurfaceSize.Width / WindowScale), (int)(SurfaceSize.Height / WindowScale));
				}
			}
			else
			{
				float scale = 1;
				var scaleVariable = Environment.GetEnvironmentVariable("OPENRA_DISPLAY_SCALE");
				if (scaleVariable != null && float.TryParse(scaleVariable, out scale))
				{
					WindowScale = scale;
					WindowSize = new Size((int)(SurfaceSize.Width / WindowScale), (int)(SurfaceSize.Height / WindowScale));
				}
			}

			Console.WriteLine("Using window scale {0:F2}", WindowScale);

			if (Game.Settings.Game.LockMouseWindow)
				GrabWindowMouseFocus();
			else
				ReleaseWindowMouseFocus();

			if (windowMode == WindowMode.Fullscreen)
			{
				SDL.SDL_SetWindowFullscreen(window, (uint)SDL.SDL_WindowFlags.SDL_WINDOW_FULLSCREEN);

				// Fullscreen mode on OSX will ignore the configured display resolution
				// and instead always picks an arbitrary scaled resolution choice that may
				// not match the window size, leading to graphical and input issues.
				// We work around this by force disabling HiDPI and resetting the window and
				// surface sizes to match the size that is forced by SDL.
				// This is usually not what the player wants, but is the best we can consistently do.
				if (Platform.CurrentPlatform == PlatformType.OSX)
				{
					int width, height;
					SDL.SDL_GetWindowSize(window, out width, out height);
					WindowSize = SurfaceSize = new Size(width, height);
					WindowScale = 1;
				}
			}
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
			if (context == IntPtr.Zero || SDL.SDL_GL_MakeCurrent(window, context) < 0)
				throw new InvalidOperationException("Can not create OpenGL context. (Error: {0})".F(SDL.SDL_GetError()));

			OpenGL.Initialize();

			OpenGL.glEnableVertexAttribArray(Shader.VertexPosAttributeIndex);
			OpenGL.CheckGLError();
			OpenGL.glEnableVertexAttribArray(Shader.TexCoordAttributeIndex);
			OpenGL.CheckGLError();
			OpenGL.glEnableVertexAttribArray(Shader.TexMetadataAttributeIndex);
			OpenGL.CheckGLError();

			SDL.SDL_SetModState(SDL.SDL_Keymod.KMOD_NONE);
			input = new Sdl2Input();
		}

		public IHardwareCursor CreateHardwareCursor(string name, Size size, byte[] data, int2 hotspot)
		{
			VerifyThreadAffinity();
			try
			{
				// Pixel double the cursor on non-OSX if the window scale is large enough
				// OSX does this for us automatically
				if (Platform.CurrentPlatform != PlatformType.OSX && WindowScale > 1.5)
				{
					var scaledData = new byte[4 * data.Length];
					for (var y = 0; y < size.Height * 4; y += 4)
					{
						for (var x = 0; x < size.Width * 4; x += 4)
						{
							var a = 4 * (y * size.Width + x);
							var b = 4 * ((y + 1) * size.Width + x);
							for (var i = 0; i < 4; i++)
							{
								scaledData[2 * a + i] = scaledData[2 * a + 4 + i] = data[a + i];
								scaledData[2 * b + i] = scaledData[2 * b + 4 + i] = data[b + i];
							}
						}
					}

					size = new Size(2 * size.Width, 2 * size.Height);
					data = scaledData;
				}

				return new SDL2HardwareCursor(size, data, hotspot);
			}
			catch (Exception ex)
			{
				throw new InvalidDataException("Failed to create hardware cursor `{0}` - {1}".F(name, ex.Message), ex);
			}
		}

		public void SetHardwareCursor(IHardwareCursor cursor)
		{
			VerifyThreadAffinity();
			var c = cursor as SDL2HardwareCursor;
			if (c == null)
				SDL.SDL_ShowCursor((int)SDL.SDL_bool.SDL_FALSE);
			else
			{
				SDL.SDL_ShowCursor((int)SDL.SDL_bool.SDL_TRUE);
				SDL.SDL_SetCursor(c.Cursor);
			}
		}

		internal void WindowSizeChanged()
		{
			// The ratio between pixels and points can change when moving between displays in OSX
			// We need to recalculate our scale to account for the potential change in the actual rendered area
			if (Platform.CurrentPlatform == PlatformType.OSX)
			{
				int width, height;
				SDL.SDL_GL_GetDrawableSize(window, out width, out height);

				if (width != SurfaceSize.Width || height != SurfaceSize.Height)
				{
					var oldScale = WindowScale;
					SurfaceSize = new Size(width, height);
					WindowScale = width * 1f / WindowSize.Width;

					OnWindowScaleChanged(oldScale, WindowScale);
				}
			}
		}

		sealed class SDL2HardwareCursor : IHardwareCursor
		{
			public IntPtr Cursor { get; private set; }
			IntPtr surface;

			public SDL2HardwareCursor(Size size, byte[] data, int2 hotspot)
			{
				try
				{
					surface = SDL.SDL_CreateRGBSurface(0, size.Width, size.Height, 32, 0x00FF0000, 0x0000FF00, 0x000000FF, 0xFF000000);
					if (surface == IntPtr.Zero)
						throw new InvalidDataException("Failed to create surface: {0}".F(SDL.SDL_GetError()));

					var sur = (SDL.SDL_Surface)Marshal.PtrToStructure(surface, typeof(SDL.SDL_Surface));
					Marshal.Copy(data, 0, sur.pixels, data.Length);

					// This call very occasionally fails on Windows, but often works when retried.
					for (var retries = 0; retries < 3 && Cursor == IntPtr.Zero; retries++)
						Cursor = SDL.SDL_CreateColorCursor(surface, hotspot.X, hotspot.Y);
					if (Cursor == IntPtr.Zero)
						throw new InvalidDataException("Failed to create cursor: {0}".F(SDL.SDL_GetError()));
				}
				catch
				{
					Dispose();
					throw;
				}
			}

			~SDL2HardwareCursor()
			{
				Game.RunAfterTick(() => Dispose(false));
			}

			public void Dispose()
			{
				Game.RunAfterTick(() => Dispose(true));
				GC.SuppressFinalize(this);
			}

			void Dispose(bool disposing)
			{
				if (Cursor != IntPtr.Zero)
				{
					SDL.SDL_FreeCursor(Cursor);
					Cursor = IntPtr.Zero;
				}

				if (surface != IntPtr.Zero)
				{
					SDL.SDL_FreeSurface(surface);
					surface = IntPtr.Zero;
				}
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

		static int ModeFromPrimitiveType(PrimitiveType pt)
		{
			switch (pt)
			{
				case PrimitiveType.PointList: return OpenGL.GL_POINTS;
				case PrimitiveType.LineList: return OpenGL.GL_LINES;
				case PrimitiveType.TriangleList: return OpenGL.GL_TRIANGLES;
			}

			throw new NotImplementedException();
		}

		public void DrawPrimitives(PrimitiveType pt, int firstVertex, int numVertices)
		{
			VerifyThreadAffinity();
			OpenGL.glDrawArrays(ModeFromPrimitiveType(pt), firstVertex, numVertices);
			OpenGL.CheckGLError();
		}

		public void Clear()
		{
			VerifyThreadAffinity();
			OpenGL.glClearColor(0, 0, 0, 1);
			OpenGL.CheckGLError();
			OpenGL.glClear(OpenGL.GL_COLOR_BUFFER_BIT | OpenGL.GL_DEPTH_BUFFER_BIT);
			OpenGL.CheckGLError();
		}

		public void EnableDepthBuffer()
		{
			VerifyThreadAffinity();
			OpenGL.glClear(OpenGL.GL_DEPTH_BUFFER_BIT);
			OpenGL.CheckGLError();
			OpenGL.glEnable(OpenGL.GL_DEPTH_TEST);
			OpenGL.CheckGLError();
			OpenGL.glDepthFunc(OpenGL.GL_LEQUAL);
			OpenGL.CheckGLError();
		}

		public void DisableDepthBuffer()
		{
			VerifyThreadAffinity();
			OpenGL.glDisable(OpenGL.GL_DEPTH_TEST);
			OpenGL.CheckGLError();
		}

		public void ClearDepthBuffer()
		{
			VerifyThreadAffinity();
			OpenGL.glClear(OpenGL.GL_DEPTH_BUFFER_BIT);
			OpenGL.CheckGLError();
		}

		public void SetBlendMode(BlendMode mode)
		{
			VerifyThreadAffinity();
			OpenGL.glBlendEquation(OpenGL.GL_FUNC_ADD);
			OpenGL.CheckGLError();

			switch (mode)
			{
				case BlendMode.None:
					OpenGL.glDisable(OpenGL.GL_BLEND);
					break;
				case BlendMode.Alpha:
					OpenGL.glEnable(OpenGL.GL_BLEND);
					OpenGL.CheckGLError();
					OpenGL.glBlendFunc(OpenGL.GL_ONE, OpenGL.GL_ONE_MINUS_SRC_ALPHA);
					break;
				case BlendMode.Additive:
				case BlendMode.Subtractive:
					OpenGL.glEnable(OpenGL.GL_BLEND);
					OpenGL.CheckGLError();
					OpenGL.glBlendFunc(OpenGL.GL_ONE, OpenGL.GL_ONE);
					if (mode == BlendMode.Subtractive)
					{
						OpenGL.CheckGLError();
						OpenGL.glBlendEquation(OpenGL.GL_FUNC_REVERSE_SUBTRACT);
					}

					break;
				case BlendMode.Multiply:
					OpenGL.glEnable(OpenGL.GL_BLEND);
					OpenGL.CheckGLError();
					OpenGL.glBlendFunc(OpenGL.GL_DST_COLOR, OpenGL.GL_ONE_MINUS_SRC_ALPHA);
					OpenGL.CheckGLError();
					break;
				case BlendMode.Multiplicative:
					OpenGL.glEnable(OpenGL.GL_BLEND);
					OpenGL.CheckGLError();
					OpenGL.glBlendFunc(OpenGL.GL_ZERO, OpenGL.GL_SRC_COLOR);
					break;
				case BlendMode.DoubleMultiplicative:
					OpenGL.glEnable(OpenGL.GL_BLEND);
					OpenGL.CheckGLError();
					OpenGL.glBlendFunc(OpenGL.GL_DST_COLOR, OpenGL.GL_SRC_COLOR);
					break;
			}

			OpenGL.CheckGLError();
		}

		public void GrabWindowMouseFocus()
		{
			VerifyThreadAffinity();
			SDL.SDL_SetWindowGrab(window, SDL.SDL_bool.SDL_TRUE);
		}

		public void ReleaseWindowMouseFocus()
		{
			VerifyThreadAffinity();
			SDL.SDL_SetWindowGrab(window, SDL.SDL_bool.SDL_FALSE);
		}

		public void EnableScissor(int left, int top, int width, int height)
		{
			VerifyThreadAffinity();

			if (width < 0)
				width = 0;

			if (height < 0)
				height = 0;

			var bottom = WindowSize.Height - (top + height);
			if (WindowSize != SurfaceSize)
			{
				left = (int)Math.Round(WindowScale * left);
				bottom = (int)Math.Round(WindowScale * bottom);
				width = (int)Math.Round(WindowScale * width);
				height = (int)Math.Round(WindowScale * height);
			}

			OpenGL.glScissor(left, bottom, width, height);
			OpenGL.CheckGLError();
			OpenGL.glEnable(OpenGL.GL_SCISSOR_TEST);
			OpenGL.CheckGLError();
		}

		public void DisableScissor()
		{
			VerifyThreadAffinity();
			OpenGL.glDisable(OpenGL.GL_SCISSOR_TEST);
			OpenGL.CheckGLError();
		}

		public Bitmap TakeScreenshot()
		{
			var rect = new Rectangle(Point.Empty, SurfaceSize);
			var bitmap = new Bitmap(rect.Width, rect.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
			var data = bitmap.LockBits(rect,
				System.Drawing.Imaging.ImageLockMode.WriteOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

			OpenGL.glPushClientAttrib(OpenGL.GL_CLIENT_PIXEL_STORE_BIT);

			OpenGL.glPixelStoref(OpenGL.GL_PACK_ROW_LENGTH, data.Stride / 4f);
			OpenGL.glPixelStoref(OpenGL.GL_PACK_ALIGNMENT, 1);

			OpenGL.glReadPixels(rect.X, rect.Y, rect.Width, rect.Height, OpenGL.GL_BGRA, OpenGL.GL_UNSIGNED_BYTE, data.Scan0);
			OpenGL.glFinish();

			OpenGL.glPopClientAttrib();

			bitmap.UnlockBits(data);

			// OpenGL standard defines the origin in the bottom left corner which is why this is upside-down by default.
			bitmap.RotateFlip(RotateFlipType.RotateNoneFlipY);

			return bitmap;
		}

		public void Present()
		{
			VerifyThreadAffinity();
			SDL.SDL_GL_SwapWindow(window);
		}

		public void PumpInput(IInputHandler inputHandler)
		{
			VerifyThreadAffinity();
			input.PumpInput(this, inputHandler);
		}

		public string GetClipboardText()
		{
			VerifyThreadAffinity();
			return input.GetClipboardText();
		}

		public bool SetClipboardText(string text)
		{
			VerifyThreadAffinity();
			return input.SetClipboardText(text);
		}

		public IVertexBuffer<Vertex> CreateVertexBuffer(int size)
		{
			VerifyThreadAffinity();
			return new VertexBuffer<Vertex>(size);
		}

		public ITexture CreateTexture()
		{
			VerifyThreadAffinity();
			return new Texture();
		}

		public ITexture CreateTexture(Bitmap bitmap)
		{
			VerifyThreadAffinity();
			return new Texture(bitmap);
		}

		public IFrameBuffer CreateFrameBuffer(Size s)
		{
			VerifyThreadAffinity();
			return new FrameBuffer(s);
		}

		public IShader CreateShader(string name)
		{
			VerifyThreadAffinity();
			return new Shader(name);
		}

		public string GLVersion { get { return OpenGL.Version; } }
	}
}
