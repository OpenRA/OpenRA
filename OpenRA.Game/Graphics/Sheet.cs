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
		protected readonly Bitmap bitmap;
		ITexture texture;
		bool dirty;

		internal Sheet(Size size)
		{
			this.bitmap = new Bitmap(size.Width, size.Height);
		}

		internal Sheet(string filename)
		{
			this.bitmap = (Bitmap)Image.FromStream(FileSystem.Open(filename));
		}

		public ITexture Texture
		{
			get
			{
				if (texture == null)
					texture = Game.Renderer.Device.CreateTexture(bitmap);

				if (dirty)
				{
					texture.SetData(bitmap);
					dirty = false;
				}

				return texture;
			}
		}

		public Size Size { get { return bitmap.Size; } }

		protected Color this[Point p]
		{
			get { return bitmap.GetPixel(p.X, p.Y); }
			set { bitmap.SetPixel(p.X, p.Y, value); }
		}

		public Bitmap Bitmap { get { return bitmap; } }	// for perf

		public void MakeDirty() { dirty = true; }
	}
}
