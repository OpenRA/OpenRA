#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using OpenRA.FileSystem;

namespace OpenRA.Graphics
{
	public sealed class Sheet : IDisposable
	{
		readonly object textureLock = new object();
		bool dirty;
		bool releaseBufferOnCommit;
		ITexture texture;
		byte[] data;

		public readonly Size Size;
		public byte[] Data { get { return data ?? texture.GetData(); } }
		public bool Buffered { get { return data != null; } }

		public Sheet(Size size, bool buffered)
		{
			Size = size;
			if (buffered)
				data = new byte[4 * Size.Width * Size.Height];
		}

		public Sheet(ITexture texture)
		{
			this.texture = texture;
			Size = texture.Size;
		}

		public Sheet(string filename)
		{
			using (var stream = GlobalFileSystem.Open(filename))
			using (var bitmap = (Bitmap)Image.FromStream(stream))
			{
				Size = bitmap.Size;

				var dataStride = 4 * Size.Width;
				data = new byte[dataStride * Size.Height];

				var bd = bitmap.LockBits(bitmap.Bounds(),
					ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
				for (var y = 0; y < Size.Height; y++)
					Marshal.Copy(IntPtr.Add(bd.Scan0, y * bd.Stride), data, y * dataStride, dataStride);
				bitmap.UnlockBits(bd);
			}

			ReleaseBuffer();
		}

		public ITexture Texture
		{
			// This is only called from the main thread but 'dirty'
			// is set from other threads too via CommitData().
			get
			{
				GenerateTexture();
				return texture;
			}
		}

		void GenerateTexture()
		{
			if (texture == null)
			{
				texture = Game.Renderer.Device.CreateTexture();
				dirty = true;
			}

			if (Buffered)
			{
				lock (textureLock)
				{
					if (dirty)
					{
						texture.SetData(data, Size.Width, Size.Height);
						dirty = false;
						if (releaseBufferOnCommit)
							data = null;
					}
				}
			}
		}

		public Bitmap AsBitmap()
		{
			var d = Data;
			var dataStride = 4 * Size.Width;
			var bitmap = new Bitmap(Size.Width, Size.Height);

			var bd = bitmap.LockBits(bitmap.Bounds(),
				ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
			for (var y = 0; y < Size.Height; y++)
				Marshal.Copy(d, y * dataStride, IntPtr.Add(bd.Scan0, y * bd.Stride), dataStride);
			bitmap.UnlockBits(bd);

			return bitmap;
		}

		public Bitmap AsBitmap(TextureChannel channel, IPalette pal)
		{
			var d = Data;
			var dataStride = 4 * Size.Width;
			var bitmap = new Bitmap(Size.Width, Size.Height);
			var channelOffset = (int)channel;

			var bd = bitmap.LockBits(bitmap.Bounds(),
				ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
			unsafe
			{
				var colors = (uint*)bd.Scan0;
				for (var y = 0; y < Size.Height; y++)
				{
					var dataRowIndex = y * dataStride + channelOffset;
					var bdRowIndex = y * bd.Stride / 4;
					for (var x = 0; x < Size.Width; x++)
					{
						var paletteIndex = d[dataRowIndex + 4 * x];
						colors[bdRowIndex + x] = pal[paletteIndex];
					}
				}
			}

			bitmap.UnlockBits(bd);

			return bitmap;
		}

		public void CommitData()
		{
			if (!Buffered)
				throw new InvalidOperationException(
					"This sheet is unbuffered. You cannot call CommitData on an unbuffered sheet. " +
					"If you need to completely replace the texture data you should set data into the texture directly. " +
					"If you need to make only small changes to the texture data consider creating a buffered sheet instead.");

			lock (textureLock)
				dirty = true;
		}

		public void ReleaseBuffer()
		{
			lock (textureLock)
				releaseBufferOnCommit = true;
		}

		public void Dispose()
		{
			if (texture != null)
				texture.Dispose();
		}
	}
}
