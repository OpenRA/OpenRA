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
using System.Drawing.Imaging;
using OpenRA.FileFormats;
using OpenRA.FileSystem;

namespace OpenRA.Graphics
{
	public class Sheet
	{
		ITexture texture;
		bool dirty;
		byte[] data;

		public readonly Size Size;
		public byte[] Data { get { return data ?? texture.GetData(); } }

		public Sheet(Size size)
		{
			Size = size;
			data = new byte[4*Size.Width*Size.Height];
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

				data = new byte[4 * Size.Width * Size.Height];
				var b = bitmap.LockBits(bitmap.Bounds(),
					ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);

				unsafe
				{
					int* c = (int*)b.Scan0;

					for (var x = 0; x < Size.Width; x++)
						for (var y = 0; y < Size.Height; y++)
						{
							var i = 4 * Size.Width * y + 4 * x;

							// Convert argb to bgra
							var argb = *(c + (y * b.Stride >> 2) + x);
							data[i++] = (byte)(argb >> 0);
							data[i++] = (byte)(argb >> 8);
							data[i++] = (byte)(argb >> 16);
							data[i++] = (byte)(argb >> 24);
						}
				}
				bitmap.UnlockBits(b);
			}
		}

		public ITexture Texture
		{
			get
			{
				if (texture == null)
				{
					texture = Game.Renderer.Device.CreateTexture();
					dirty = true;
				}

				if (dirty)
				{
					texture.SetData(data, Size.Width, Size.Height);
					dirty = false;
				}

				return texture;
			}
		}

		public Bitmap AsBitmap()
		{
			var d = Data;
			var b = new Bitmap(Size.Width, Size.Height);
			var output = b.LockBits(new Rectangle(0, 0, Size.Width, Size.Height),
				ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);

			unsafe
			{
				int* c = (int*)output.Scan0;

				for (var x = 0; x < Size.Width; x++)
					for (var y = 0; y < Size.Height; y++)
					{
						var i = 4*Size.Width*y + 4*x;

						// Convert bgra to argb
						var argb = (d[i+3] << 24) | (d[i+2] << 16) | (d[i+1] << 8) | d[i];
						*(c + (y * output.Stride >> 2) + x) = argb;
					}
			}
			b.UnlockBits(output);

			return b;
		}

		public Bitmap AsBitmap(TextureChannel channel, Palette pal)
		{
			var d = Data;
			var b = new Bitmap(Size.Width, Size.Height);
			var output = b.LockBits(new Rectangle(0, 0, Size.Width, Size.Height),
				ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);

			unsafe
			{
				int* c = (int*)output.Scan0;

				for (var x = 0; x < Size.Width; x++)
					for (var y = 0; y < Size.Height; y++)
				{
					var index = d[4*Size.Width*y + 4*x + (int)channel];
					*(c + (y * output.Stride >> 2) + x) = pal.GetColor(index).ToArgb();
				}
			}
			b.UnlockBits(output);

			return b;
		}

		public void CommitData()
		{
			if (data == null)
				throw new InvalidOperationException("Texture-wrappers are read-only");

			dirty = true;
		}
	}
}
