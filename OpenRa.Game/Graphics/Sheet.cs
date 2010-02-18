#region Copyright & License Information
/*
 * Copyright 2007,2009,2010 Chris Forbes, Robert Pepperell, Matthew Bowra-Dean, Paul Chote, Alli Witheford.
 * This file is part of OpenRA.
 * 
 *  OpenRA is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 * 
 *  OpenRA is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 * 
 *  You should have received a copy of the GNU General Public License
 *  along with OpenRA.  If not, see <http://www.gnu.org/licenses/>.
 */
#endregion

using System.Drawing;
using OpenRa.FileFormats;
using OpenRa.FileFormats.Graphics;

namespace OpenRa.Graphics
{
	public class Sheet
	{
		readonly Renderer renderer;
		protected readonly Bitmap bitmap;

		ITexture texture;

		internal Sheet(Renderer renderer, Size size)
		{
			this.renderer = renderer;
			this.bitmap = new Bitmap(size.Width, size.Height);
		}

		internal Sheet(Renderer renderer, string filename)
		{
			this.renderer = renderer;
			this.bitmap = (Bitmap)Image.FromStream(FileSystem.Open(filename));
		}

		void Resolve()
		{
            texture = renderer.Device.CreateTexture(bitmap);
		}

		public ITexture Texture
		{
			get
			{
				if (texture == null)
					Resolve();

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
	}
}
