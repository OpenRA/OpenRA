#region Copyright & License Information
/*
 * Copyright 2007-2016 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;

namespace OpenRA.Graphics
{
	public sealed class Sheet : IDisposable
	{
		bool dirty;
		bool releaseBufferOnCommit;
		ITexture texture;
		byte[] data;

		public readonly Size Size;
		public readonly SheetType Type;

		public byte[] GetData()
		{
			CreateBuffer();
			return data;
		}

		public bool Buffered { get { return data != null || texture == null; } }

		public Sheet(SheetType type, Size size)
		{
			Type = type;
			Size = size;
		}

		public Sheet(SheetType type, ITexture texture)
		{
			Type = type;
			this.texture = texture;
			Size = texture.Size;
		}

		public Sheet(SheetType type, Stream stream)
		{
			using (var bitmap = (Bitmap)Image.FromStream(stream))
			{
				Size = bitmap.Size;
				data = new byte[4 * Size.Width * Size.Height];

				Util.FastCopyIntoSprite(new Sprite(this, bitmap.Bounds(), TextureChannel.Red), bitmap);
			}

			Type = type;
			ReleaseBuffer();
		}

		public ITexture GetTexture()
		{
			if (texture == null)
			{
				texture = Game.Renderer.Device.CreateTexture();
				dirty = true;
			}

			if (data != null && dirty)
			{
				texture.SetData(data, Size.Width, Size.Height);
				dirty = false;
				if (releaseBufferOnCommit)
					data = null;
			}

			return texture;
		}

		public Bitmap AsBitmap()
		{
			var d = GetData();
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
			var d = GetData();
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

		public void CreateBuffer()
		{
			if (data != null)
				return;
			if (texture == null)
				data = new byte[4 * Size.Width * Size.Height];
			else
				data = texture.GetData();
			releaseBufferOnCommit = false;
		}

		public void CommitBufferedData()
		{
			if (!Buffered)
				throw new InvalidOperationException(
					"This sheet is unbuffered. You cannot call CommitBufferedData on an unbuffered sheet. " +
					"If you need to completely replace the texture data you should set data into the texture directly. " +
					"If you need to make only small changes to the texture data consider creating a buffered sheet instead.");

			dirty = true;
		}

		public void ReleaseBuffer()
		{
			if (!Buffered)
				return;
			dirty = true;
			releaseBufferOnCommit = true;
		}

		public void Dispose()
		{
			if (texture != null)
				texture.Dispose();
		}
	}
}
