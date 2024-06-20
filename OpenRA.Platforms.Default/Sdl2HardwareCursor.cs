#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.IO;
using System.Runtime.InteropServices;
using OpenRA.Primitives;
using Silk.NET.SDL;

namespace OpenRA.Platforms.Default
{
	sealed class Sdl2HardwareCursor : IHardwareCursor
	{
		readonly Sdl sdl;

		public unsafe Cursor* Cursor { get; private set; }
		unsafe Surface* surface;

		public unsafe Sdl2HardwareCursor(Sdl sdl, Size size, byte[] data, int2 hotspot)
		{
			this.sdl = sdl;

			try
			{
				surface = sdl.CreateRGBSurface(0, size.Width, size.Height, 32, 0x00FF0000, 0x0000FF00, 0x000000FF, 0xFF000000);
				if (surface == null)
					throw new InvalidDataException($"Failed to create surface: {sdl.GetErrorS()}");

				var sur = (Surface)Marshal.PtrToStructure((IntPtr)surface, typeof(Surface));
				Marshal.Copy(data, 0, (IntPtr)sur.Pixels, data.Length);

				// This call very occasionally fails on Windows, but often works when retried.
				for (var retries = 0; retries < 3 && Cursor == null; retries++)
					Cursor = sdl.CreateColorCursor(surface, hotspot.X, hotspot.Y);
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

		unsafe void Dispose(bool _)
		{
			if (Cursor != null)
			{
				sdl.FreeCursor(Cursor);
				Cursor = null;
			}

			if (surface != null)
			{
				sdl.FreeSurface(surface);
				surface = null;
			}
		}

		~Sdl2HardwareCursor()
		{
			Dispose(false);
		}
	}
}
