#region Copyright & License Information
/*
 * Copyright 2007-2018 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Drawing;
using System.Runtime.InteropServices;
using SDL2;

namespace OpenRA.Platforms.Default
{
	sealed class Sdl2PlatformWindow : ThreadAffine, IPlatformWindow
	{
		readonly IGraphicsContext context;
		readonly Sdl2Input input;

		public IGraphicsContext Context { get { return context; } }

		readonly IntPtr window;
		bool disposed;

		readonly object syncObject = new object();
		Size windowSize;
		Size surfaceSize;
		float windowScale;

		internal IntPtr Window
		{
			get
			{
				lock (syncObject)
					return window;
			}
		}

		public Size WindowSize
		{
			get
			{
				lock (syncObject)
					return windowSize;
			}
		}

		public float WindowScale
		{
			get
			{
				lock (syncObject)
					return windowScale;
			}
		}

		internal Size SurfaceSize
		{
			get
			{
				lock (syncObject)
					return surfaceSize;
			}
		}

		public event Action<float, float> OnWindowScaleChanged = (before, after) => { };

		[DllImport("user32.dll")]
		static extern bool SetProcessDPIAware();

		public Sdl2PlatformWindow(Size requestWindowSize, WindowMode windowMode, int batchSize)
		{
			Console.WriteLine("Using SDL 2 with OpenGL renderer");

			// Lock the Window/Surface properties until initialization is complete
			lock (syncObject)
			{
				windowSize = requestWindowSize;

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
				if (windowSize.Width == 0 && windowSize.Height == 0)
				{
					Console.WriteLine("No custom resolution provided, using desktop resolution");
					windowSize = new Size(display.w, display.h);
				}

				Console.WriteLine("Using resolution: {0}x{1}", windowSize.Width, windowSize.Height);

				var windowFlags = SDL.SDL_WindowFlags.SDL_WINDOW_OPENGL | SDL.SDL_WindowFlags.SDL_WINDOW_ALLOW_HIGHDPI;

				// HiDPI doesn't work properly on OSX with (legacy) fullscreen mode
				if (Platform.CurrentPlatform == PlatformType.OSX && windowMode == WindowMode.Fullscreen)
					SDL.SDL_SetHint(SDL.SDL_HINT_VIDEO_HIGHDPI_DISABLED, "1");

				window = SDL.SDL_CreateWindow("OpenRA", SDL.SDL_WINDOWPOS_CENTERED, SDL.SDL_WINDOWPOS_CENTERED,
					windowSize.Width, windowSize.Height, windowFlags);

				surfaceSize = windowSize;
				windowScale = 1;

				// Enable high resolution rendering for Retina displays
				if (Platform.CurrentPlatform == PlatformType.OSX)
				{
					// OSX defines the window size in "points", with a device-dependent number of pixels per point.
					// The window scale is simply the ratio of GL pixels / window points.
					int width, height;

					SDL.SDL_GL_GetDrawableSize(Window, out width, out height);
					surfaceSize = new Size(width, height);
					windowScale = width * 1f / windowSize.Width;
				}
				else if (Platform.CurrentPlatform == PlatformType.Windows)
				{
					float ddpi, hdpi, vdpi;
					if (!Game.Settings.Graphics.DisableWindowsDPIScaling && SDL.SDL_GetDisplayDPI(0, out ddpi, out hdpi, out vdpi) == 0)
					{
						windowScale = ddpi / 96;
						windowSize = new Size((int)(surfaceSize.Width / windowScale), (int)(surfaceSize.Height / windowScale));
					}
				}
				else
				{
					float scale = 1;
					var scaleVariable = Environment.GetEnvironmentVariable("OPENRA_DISPLAY_SCALE");
					if (scaleVariable != null && float.TryParse(scaleVariable, out scale))
					{
						windowScale = scale;
						windowSize = new Size((int)(surfaceSize.Width / windowScale), (int)(surfaceSize.Height / windowScale));
					}
				}

				Console.WriteLine("Using window scale {0:F2}", windowScale);

				if (Game.Settings.Game.LockMouseWindow)
					GrabWindowMouseFocus();
				else
					ReleaseWindowMouseFocus();

				if (windowMode == WindowMode.Fullscreen)
				{
					SDL.SDL_SetWindowFullscreen(Window, (uint)SDL.SDL_WindowFlags.SDL_WINDOW_FULLSCREEN);

					// Fullscreen mode on OSX will ignore the configured display resolution
					// and instead always picks an arbitrary scaled resolution choice that may
					// not match the window size, leading to graphical and input issues.
					// We work around this by force disabling HiDPI and resetting the window and
					// surface sizes to match the size that is forced by SDL.
					// This is usually not what the player wants, but is the best we can consistently do.
					if (Platform.CurrentPlatform == PlatformType.OSX)
					{
						int width, height;
						SDL.SDL_GetWindowSize(Window, out width, out height);
						windowSize = surfaceSize = new Size(width, height);
						windowScale = 1;
					}
				}
				else if (windowMode == WindowMode.PseudoFullscreen)
				{
					// Work around a visual glitch in OSX: the window is offset
					// partially offscreen if the dock is at the left of the screen
					if (Platform.CurrentPlatform == PlatformType.OSX)
						SDL.SDL_SetWindowPosition(Window, 0, 0);

					SDL.SDL_SetWindowFullscreen(Window, (uint)SDL.SDL_WindowFlags.SDL_WINDOW_FULLSCREEN_DESKTOP);
					SDL.SDL_SetHint(SDL.SDL_HINT_VIDEO_MINIMIZE_ON_FOCUS_LOSS, "0");
				}
			}

			// Run graphics rendering on a dedicated thread.
			// The calling thread will then have more time to process other tasks, since rendering happens in parallel.
			// If the calling thread is the main game thread, this means it can run more logic and render ticks.
			context = new ThreadedGraphicsContext(new Sdl2GraphicsContext(this), batchSize);

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

				return new Sdl2HardwareCursor(size, data, hotspot);
			}
			catch (Exception ex)
			{
				throw new Sdl2HardwareCursorException("Failed to create hardware cursor `{0}` - {1}".F(name, ex.Message), ex);
			}
		}

		public void SetHardwareCursor(IHardwareCursor cursor)
		{
			VerifyThreadAffinity();
			var c = cursor as Sdl2HardwareCursor;
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
				SDL.SDL_GL_GetDrawableSize(Window, out width, out height);

				if (width != SurfaceSize.Width || height != SurfaceSize.Height)
				{
					float oldScale;
					lock (syncObject)
					{
						oldScale = windowScale;
						surfaceSize = new Size(width, height);
						windowScale = width * 1f / windowSize.Width;
					}

					OnWindowScaleChanged(oldScale, windowScale);
				}
			}
		}

		public void Dispose()
		{
			if (disposed)
				return;

			disposed = true;

			if (context != null)
				context.Dispose();

			if (Window != IntPtr.Zero)
				SDL.SDL_DestroyWindow(Window);

			SDL.SDL_Quit();
		}

		public void GrabWindowMouseFocus()
		{
			VerifyThreadAffinity();
			SDL.SDL_SetWindowGrab(Window, SDL.SDL_bool.SDL_TRUE);
		}

		public void ReleaseWindowMouseFocus()
		{
			VerifyThreadAffinity();
			SDL.SDL_SetWindowGrab(Window, SDL.SDL_bool.SDL_FALSE);
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
	}
}
