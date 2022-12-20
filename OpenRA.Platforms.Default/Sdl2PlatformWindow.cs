#region Copyright & License Information
/*
 * Copyright 2007-2022 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using OpenRA.Primitives;
using SDL2;

namespace OpenRA.Platforms.Default
{
	sealed class Sdl2PlatformWindow : ThreadAffine, IPlatformWindow
	{
		readonly IGraphicsContext context;
		readonly Sdl2Input input;

		public IGraphicsContext Context => context;

		readonly IntPtr window;
		bool disposed;

		readonly object syncObject = new object();
		readonly Size windowSize;
		Size surfaceSize;
		float windowScale = 1f;
		int2? lockedMousePosition;
		float scaleModifier;
		readonly GLProfile profile;
		readonly GLProfile[] supportedProfiles;

		internal IntPtr Window
		{
			get
			{
				lock (syncObject)
					return window;
			}
		}

		public Size NativeWindowSize
		{
			get
			{
				lock (syncObject)
					return windowSize;
			}
		}

		public Size EffectiveWindowSize
		{
			get
			{
				lock (syncObject)
					return new Size((int)(windowSize.Width / scaleModifier), (int)(windowSize.Height / scaleModifier));
			}
		}

		public float NativeWindowScale
		{
			get
			{
				lock (syncObject)
					return windowScale;
			}
		}

		public float EffectiveWindowScale
		{
			get
			{
				lock (syncObject)
					return windowScale * scaleModifier;
			}
		}

		public Size SurfaceSize
		{
			get
			{
				lock (syncObject)
					return surfaceSize;
			}
		}

		public int CurrentDisplay => SDL.SDL_GetWindowDisplayIndex(window);

		public int DisplayCount => SDL.SDL_GetNumVideoDisplays();

		public bool HasInputFocus { get; internal set; }

		public bool IsSuspended { get; internal set; }

		public GLProfile GLProfile
		{
			get
			{
				lock (syncObject)
					return profile;
			}
		}

		public GLProfile[] SupportedGLProfiles
		{
			get
			{
				lock (syncObject)
					return supportedProfiles;
			}
		}

		public event Action<float, float, float, float> OnWindowScaleChanged = (oldNative, oldEffective, newNative, newEffective) => { };

		[DllImport("user32.dll")]
		static extern bool SetProcessDPIAware();

		[DllImport("libX11")]
		static extern IntPtr XInternAtom(IntPtr display, string atom_name, bool only_if_exists);

		[DllImport("libX11", CharSet=CharSet.Ansi)]
		static extern int XChangeProperty(IntPtr display, IntPtr window, IntPtr property, IntPtr type, int format, IntPtr mode, string data, int elements);

		[DllImport("libX11")]
		static extern IntPtr XFlush(IntPtr display);

		public Sdl2PlatformWindow(Size requestEffectiveWindowSize, WindowMode windowMode,
			float scaleModifier, int batchSize, int videoDisplay, GLProfile requestProfile, bool enableLegacyGL)
		{
			// Lock the Window/Surface properties until initialization is complete
			lock (syncObject)
			{
				this.scaleModifier = scaleModifier;

				// Disable legacy scaling on Windows
				if (Platform.CurrentPlatform == PlatformType.Windows)
					SetProcessDPIAware();

				// Decide which OpenGL profile to use.
				// Prefer standard GL over GLES provided by the native driver
				var testProfiles = new List<GLProfile> { GLProfile.ANGLE, GLProfile.Modern, GLProfile.Embedded };
				if (enableLegacyGL)
					testProfiles.Add(GLProfile.Legacy);

				var errorLog = new List<string>();
				supportedProfiles = testProfiles
					.Where(profile => CanCreateGLWindow(profile, errorLog))
					.ToArray();

				if (supportedProfiles.Length == 0)
				{
					foreach (var error in errorLog)
						Log.Write("graphics", error);

					throw new InvalidOperationException("No supported OpenGL profiles were found.");
				}

				profile = supportedProfiles.Contains(requestProfile) ? requestProfile : supportedProfiles.First();

				// Note: This must be called after the CanCreateGLWindow checks above,
				// which needs to create and destroy its own SDL contexts as a workaround for specific buggy drivers
				if (SDL.SDL_Init(SDL.SDL_INIT_VIDEO) != 0)
					Log.Write("graphics", $"SDL initialisation failed: {SDL.SDL_GetError()}");

				SetSDLAttributes(profile);
				Console.WriteLine($"Using SDL 2 with OpenGL ({profile}) renderer");
				if (videoDisplay < 0 || videoDisplay >= DisplayCount)
					videoDisplay = 0;

				SDL.SDL_GetCurrentDisplayMode(videoDisplay, out var display);

				// Windows and Linux define window sizes in native pixel units.
				// Query the display/dpi scale so we can convert our requested effective size to pixels.
				// This is not necessary on macOS, which defines window sizes in effective units ("points").
				if (Platform.CurrentPlatform == PlatformType.Windows)
				{
					// Launch the game with OPENRA_DISPLAY_SCALE to force a specific scaling factor
					// Otherwise fall back to Windows's DPI configuration
					var scaleVariable = Environment.GetEnvironmentVariable("OPENRA_DISPLAY_SCALE");
					if (scaleVariable == null || !float.TryParse(scaleVariable, NumberStyles.Float, NumberFormatInfo.InvariantInfo, out windowScale) || windowScale <= 0)
						if (SDL.SDL_GetDisplayDPI(videoDisplay, out var ddpi, out _, out _) == 0)
							windowScale = ddpi / 96;
				}
				else if (Platform.CurrentPlatform == PlatformType.Linux)
				{
					// Launch the game with OPENRA_DISPLAY_SCALE to force a specific scaling factor
					// Otherwise fall back to GDK_SCALE or parsing the x11 DPI configuration
					var scaleVariable = Environment.GetEnvironmentVariable("OPENRA_DISPLAY_SCALE") ?? Environment.GetEnvironmentVariable("GDK_SCALE");
					if (scaleVariable == null || !float.TryParse(scaleVariable, NumberStyles.Float, NumberFormatInfo.InvariantInfo, out windowScale) || windowScale <= 0)
					{
						// Attempt to automatically detect DPI
						try
						{
							var psi = new ProcessStartInfo("/usr/bin/xrdb", "-query")
							{
								UseShellExecute = false,
								RedirectStandardOutput = true
							};

							var p = Process.Start(psi);
							var lines = p.StandardOutput.ReadToEnd().Split('\n');

							foreach (var line in lines)
								if (line.StartsWith("Xft.dpi") && int.TryParse(line.Substring(8), out var dpi))
									windowScale = dpi / 96f;
						}
						catch { }
					}
				}

				Console.WriteLine($"Desktop resolution: {display.w}x{display.h}");
				if (requestEffectiveWindowSize.Width == 0 && requestEffectiveWindowSize.Height == 0)
				{
					Console.WriteLine("No custom resolution provided, using desktop resolution");
					surfaceSize = windowSize = new Size(display.w, display.h);
				}
				else
					surfaceSize = windowSize = new Size((int)(requestEffectiveWindowSize.Width * windowScale), (int)(requestEffectiveWindowSize.Height * windowScale));

				Console.WriteLine($"Using resolution: {windowSize.Width}x{windowSize.Height}");

				var windowFlags = SDL.SDL_WindowFlags.SDL_WINDOW_OPENGL | SDL.SDL_WindowFlags.SDL_WINDOW_ALLOW_HIGHDPI;

				// HiDPI doesn't work properly on OSX with (legacy) fullscreen mode
				if (Platform.CurrentPlatform == PlatformType.OSX && windowMode == WindowMode.Fullscreen)
					SDL.SDL_SetHint(SDL.SDL_HINT_VIDEO_HIGHDPI_DISABLED, "1");

				window = SDL.SDL_CreateWindow("OpenRA", SDL.SDL_WINDOWPOS_CENTERED_DISPLAY(videoDisplay), SDL.SDL_WINDOWPOS_CENTERED_DISPLAY(videoDisplay),
					windowSize.Width, windowSize.Height, windowFlags);

				if (Platform.CurrentPlatform == PlatformType.Linux)
				{
					// The KDE task switcher limits itself to the 128px icon unless we
					// set an X11 _KDE_NET_WM_DESKTOP_FILE property on the window
					var currentDesktop = Environment.GetEnvironmentVariable("XDG_CURRENT_DESKTOP");
					var desktopFilename = Environment.GetEnvironmentVariable("OPENRA_DESKTOP_FILENAME");
					if (desktopFilename != null && currentDesktop == "KDE")
					{
						try
						{
							var info = default(SDL.SDL_SysWMinfo);
							SDL.SDL_VERSION(out info.version);
							SDL.SDL_GetWindowWMInfo(Window, ref info);

							var d = info.info.x11.display;
							var w = info.info.x11.window;
							var property = XInternAtom(d, "_KDE_NET_WM_DESKTOP_FILE", false);
							var type = XInternAtom(d, "UTF8_STRING", false);

							XChangeProperty(d, w, property, type, 8, IntPtr.Zero, desktopFilename, desktopFilename.Length + 1);
							XFlush(d);
						}
						catch
						{
							Log.Write("debug", "Failed to set _KDE_NET_WM_DESKTOP_FILE");
							Console.WriteLine("Failed to set _KDE_NET_WM_DESKTOP_FILE");
						}
					}
				}

				// Enable high resolution rendering for Retina displays
				if (Platform.CurrentPlatform == PlatformType.OSX)
				{
					// OSX defines the window size in "points", with a device-dependent number of pixels per point.
					// The window scale is simply the ratio of GL pixels / window points.
					SDL.SDL_GL_GetDrawableSize(Window, out var width, out var height);
					surfaceSize = new Size(width, height);
					windowScale = width * 1f / windowSize.Width;
				}
				else
					windowSize = new Size((int)(surfaceSize.Width / windowScale), (int)(surfaceSize.Height / windowScale));

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
						SDL.SDL_GetWindowSize(Window, out var width, out var height);
						windowSize = surfaceSize = new Size(width, height);
						windowScale = 1;
					}
				}
				else if (windowMode == WindowMode.PseudoFullscreen)
				{
					SDL.SDL_SetWindowFullscreen(Window, (uint)SDL.SDL_WindowFlags.SDL_WINDOW_FULLSCREEN_DESKTOP);
					SDL.SDL_SetHint(SDL.SDL_HINT_VIDEO_MINIMIZE_ON_FOCUS_LOSS, "0");

					if (Platform.CurrentPlatform == PlatformType.OSX)
					{
						// Activating SDL_WINDOW_FULLSCREEN_DESKTOP on a display with a notch will automatically
						// reduce the window height and align the top-left of the window to the safe area.
						//
						// SDL (as of version 2.26) does not contain an API to query the safeAreaInsets before
						// the window is created. We work around this by checking the window height after going
						// fullscreen, and recalculating our sizes to match the new window geometry.
						//
						// This workaround will become redundant once window resizing is implemented.
						SDL.SDL_GetWindowSize(Window, out var width, out var height);
						if (height != windowSize.Height)
						{
							windowSize = new Size(width, height);

							SDL.SDL_GL_GetDrawableSize(Window, out width, out height);
							surfaceSize = new Size(width, height);
							windowScale = width * 1f / windowSize.Width;

							Console.WriteLine($"Using new resolution: {windowSize.Width}x{windowSize.Height}");
						}
					}
				}

				Console.WriteLine($"Using window scale {windowScale:F2}");
			}

			// Run graphics rendering on a dedicated thread.
			// The calling thread will then have more time to process other tasks, since rendering happens in parallel.
			// If the calling thread is the main game thread, this means it can run more logic and render ticks.
			// This is disabled when running in windowed mode on Windows because it breaks the ability to minimize/restore the window.
			if (Platform.CurrentPlatform == PlatformType.Windows && windowMode == WindowMode.Windowed)
			{
				var ctx = new Sdl2GraphicsContext(this);
				ctx.InitializeOpenGL();
				context = ctx;
			}
			else
				context = new ThreadedGraphicsContext(new Sdl2GraphicsContext(this), batchSize);

			context.SetVSyncEnabled(Game.Settings.Graphics.VSync);

			SDL.SDL_SetModState(SDL.SDL_Keymod.KMOD_NONE);
			input = new Sdl2Input();
		}

		byte[] DoublePixelData(byte[] data, Size size)
		{
			var scaledData = new byte[4 * data.Length];
			for (var y = 0; y < size.Height; y++)
			{
				for (var x = 0; x < size.Width; x++)
				{
					var a = 4 * (y * size.Width + x);
					var b = 8 * (2 * y * size.Width + x);
					var c = b + 8 * size.Width;
					for (var i = 0; i < 4; i++)
						scaledData[b + i] = scaledData[b + 4 + i] = scaledData[c + i] = scaledData[c + 4 + i] = data[a + i];
				}
			}

			return scaledData;
		}

		public IHardwareCursor CreateHardwareCursor(string name, Size size, byte[] data, int2 hotspot, bool pixelDouble)
		{
			VerifyThreadAffinity();
			try
			{
				// Pixel double the cursor on non-OSX if the window scale is large enough
				// OSX does this for us automatically
				if (Platform.CurrentPlatform != PlatformType.OSX && NativeWindowScale > 1.5f)
				{
					data = DoublePixelData(data, size);
					size = new Size(2 * size.Width, 2 * size.Height);
					hotspot *= 2;
				}

				// Scale all but the "default" cursor if requested by the player
				if (pixelDouble)
				{
					data = DoublePixelData(data, size);
					size = new Size(2 * size.Width, 2 * size.Height);
					hotspot *= 2;
				}

				var cursor = new Sdl2HardwareCursor(size, data, hotspot);
				return cursor.Cursor == IntPtr.Zero ? null : cursor;
			}
			catch (Exception ex)
			{
				Log.Write("debug", $"Failed to create hardware cursor `{name}` - {ex.Message}");
				Console.WriteLine($"Failed to create hardware cursor `{name}` - {ex.Message}");
				return null;
			}
		}

		public void SetHardwareCursor(IHardwareCursor cursor)
		{
			VerifyThreadAffinity();
			if (cursor is Sdl2HardwareCursor c)
			{
				SDL.SDL_ShowCursor((int)SDL.SDL_bool.SDL_TRUE);
				SDL.SDL_SetCursor(c.Cursor);
			}
			else
				SDL.SDL_ShowCursor((int)SDL.SDL_bool.SDL_FALSE);
		}

		public void SetWindowTitle(string title)
		{
			VerifyThreadAffinity();
			SDL.SDL_SetWindowTitle(window, title);
		}

		public void SetRelativeMouseMode(bool mode)
		{
			if (mode)
			{
				SDL.SDL_GetMouseState(out var x, out var y);
				lockedMousePosition = new int2(x, y);
			}
			else
			{
				if (lockedMousePosition.HasValue)
					SDL.SDL_WarpMouseInWindow(window, lockedMousePosition.Value.X, lockedMousePosition.Value.Y);

				lockedMousePosition = null;
			}
		}

		internal void WindowSizeChanged()
		{
			// The ratio between pixels and points can change when moving between displays in OSX
			// We need to recalculate our scale to account for the potential change in the actual rendered area
			if (Platform.CurrentPlatform == PlatformType.OSX)
			{
				SDL.SDL_GL_GetDrawableSize(Window, out var width, out var height);

				if (width != SurfaceSize.Width || height != SurfaceSize.Height)
				{
					float oldScale;
					lock (syncObject)
					{
						oldScale = windowScale;
						surfaceSize = new Size(width, height);
						windowScale = width * 1f / windowSize.Width;
					}

					OnWindowScaleChanged(oldScale, oldScale * scaleModifier, windowScale, windowScale * scaleModifier);
				}
			}
		}

		public void Dispose()
		{
			if (disposed)
				return;

			disposed = true;

			context?.Dispose();

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
			input.PumpInput(this, inputHandler, lockedMousePosition);

			if (lockedMousePosition.HasValue)
				SDL.SDL_WarpMouseInWindow(window, lockedMousePosition.Value.X, lockedMousePosition.Value.Y);
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

		static void SetSDLAttributes(GLProfile profile)
		{
			SDL.SDL_GL_ResetAttributes();
			SDL.SDL_GL_SetAttribute(SDL.SDL_GLattr.SDL_GL_DOUBLEBUFFER, 1);
			SDL.SDL_GL_SetAttribute(SDL.SDL_GLattr.SDL_GL_RED_SIZE, 8);
			SDL.SDL_GL_SetAttribute(SDL.SDL_GLattr.SDL_GL_GREEN_SIZE, 8);
			SDL.SDL_GL_SetAttribute(SDL.SDL_GLattr.SDL_GL_BLUE_SIZE, 8);
			SDL.SDL_GL_SetAttribute(SDL.SDL_GLattr.SDL_GL_ALPHA_SIZE, 0);

			var useAngle = profile == GLProfile.ANGLE ? "1" : "0";
			SDL.SDL_SetHint("SDL_OPENGL_ES_DRIVER", useAngle);

			switch (profile)
			{
				case GLProfile.Modern:
					SDL.SDL_GL_SetAttribute(SDL.SDL_GLattr.SDL_GL_CONTEXT_MAJOR_VERSION, 3);
					SDL.SDL_GL_SetAttribute(SDL.SDL_GLattr.SDL_GL_CONTEXT_MINOR_VERSION, 2);
					SDL.SDL_GL_SetAttribute(SDL.SDL_GLattr.SDL_GL_CONTEXT_PROFILE_MASK, (int)SDL.SDL_GLprofile.SDL_GL_CONTEXT_PROFILE_CORE);
					break;
				case GLProfile.ANGLE:
				case GLProfile.Embedded:
					SDL.SDL_GL_SetAttribute(SDL.SDL_GLattr.SDL_GL_CONTEXT_MAJOR_VERSION, 3);
					SDL.SDL_GL_SetAttribute(SDL.SDL_GLattr.SDL_GL_CONTEXT_MINOR_VERSION, 0);
					SDL.SDL_GL_SetAttribute(SDL.SDL_GLattr.SDL_GL_CONTEXT_PROFILE_MASK, (int)SDL.SDL_GLprofile.SDL_GL_CONTEXT_PROFILE_ES);
					break;
				case GLProfile.Legacy:
					SDL.SDL_GL_SetAttribute(SDL.SDL_GLattr.SDL_GL_CONTEXT_MAJOR_VERSION, 2);
					SDL.SDL_GL_SetAttribute(SDL.SDL_GLattr.SDL_GL_CONTEXT_MINOR_VERSION, 1);
					break;
			}
		}

		static bool CanCreateGLWindow(GLProfile profile, List<string> errorLog)
		{
			// Implementation inspired by TestIndividualGLVersion from Veldrid

			// Need to create and destroy its own SDL contexts as a workaround for specific buggy drivers
			if (SDL.SDL_Init(SDL.SDL_INIT_VIDEO) != 0)
			{
				// Continue to harvest additional SDL errors below
				errorLog.Add($"{profile}: SDL init failed: {SDL.SDL_GetError()}");
				SDL.SDL_ClearError();
			}

			SetSDLAttributes(profile);

			var flags = SDL.SDL_WindowFlags.SDL_WINDOW_HIDDEN | SDL.SDL_WindowFlags.SDL_WINDOW_OPENGL;
			var window = SDL.SDL_CreateWindow("", 0, 0, 1, 1, flags);
			if (window == IntPtr.Zero || !string.IsNullOrEmpty(SDL.SDL_GetError()))
			{
				errorLog.Add($"{profile}: SDL window creation failed: {SDL.SDL_GetError()}");
				SDL.SDL_ClearError();
				SDL.SDL_Quit();
				return false;
			}

			var context = SDL.SDL_GL_CreateContext(window);
			if (context == IntPtr.Zero || SDL.SDL_GL_MakeCurrent(window, context) < 0)
			{
				errorLog.Add($"{profile}: GL context creation failed: {SDL.SDL_GetError()}");
				SDL.SDL_ClearError();
				SDL.SDL_DestroyWindow(window);
				SDL.SDL_Quit();
				return false;
			}

			// Distinguish between ANGLE and native GLES
			var success = true;
			if (profile == GLProfile.ANGLE || profile == GLProfile.Embedded)
			{
				var isAngle = SDL.SDL_GL_ExtensionSupported("GL_ANGLE_texture_usage") == SDL.SDL_bool.SDL_TRUE;
				success = isAngle ^ (profile != GLProfile.ANGLE);
				if (!success)
					errorLog.Add(isAngle ? "GL profile is ANGLE" : "GL profile is Embedded");
			}

			SDL.SDL_GL_DeleteContext(context);
			SDL.SDL_DestroyWindow(window);
			SDL.SDL_Quit();
			return success;
		}

		public void SetScaleModifier(float scale)
		{
			var oldScaleModifier = scaleModifier;
			scaleModifier = scale;
			OnWindowScaleChanged(windowScale, windowScale * oldScaleModifier, windowScale, windowScale * scaleModifier);
		}
	}
}
