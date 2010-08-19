#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using System.Drawing;
using OpenRA.FileFormats;
using OpenRA.FileFormats.Graphics;

namespace OpenRA.Graphics
{
	public class Sheet
	{
		Bitmap bitmap;
		ITexture texture;
		bool dirty;
		byte[] data;
		public readonly Size Size;

		public Sheet(Size size)
		{
			Size = size;
		}

		internal Sheet(string filename)
		{
			bitmap = (Bitmap)Image.FromStream(FileSystem.Open(filename));
			Size = bitmap.Size;
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
					if (data != null)
					{
						texture.SetData(data, Size.Width, Size.Height);
						dirty = false;
					}
					else if (bitmap != null)
					{
						texture.SetData(Bitmap);
						dirty = false;
					}
				}

				return texture;
			}
		}

		public Bitmap Bitmap { get { if (bitmap == null) bitmap = new Bitmap(Size.Width, Size.Height); return bitmap; } }
		public byte[] Data { get { if (data == null) data = new byte[4 * Size.Width * Size.Height]; return data; } }

		public void MakeDirty() { dirty = true; }
	}
}
