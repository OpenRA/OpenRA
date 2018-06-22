﻿#region Copyright & License Information
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
using System.IO;
using System.Runtime.InteropServices;
using SDL2;

namespace OpenRA.Platforms.Default
{
	class Sdl2HardwareCursorException : Exception
	{
		public Sdl2HardwareCursorException(string message) : base(message) { }
		public Sdl2HardwareCursorException(string message, Exception innerException) : base(message, innerException) { }
	}

	sealed class Sdl2HardwareCursor : IHardwareCursor
	{
		public IntPtr Cursor { get; private set; }
		IntPtr surface;

		public Sdl2HardwareCursor(Size size, byte[] data, int2 hotspot)
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
					throw new Sdl2HardwareCursorException("Failed to create cursor: {0}".F(SDL.SDL_GetError()));
			}
			catch
			{
				Dispose();
				throw;
			}
		}

		public void Dispose()
		{
			Dispose(true);
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
}
