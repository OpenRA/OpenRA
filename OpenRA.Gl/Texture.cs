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
using System.Drawing.Imaging;
using OpenRA.FileFormats.Graphics;
using Tao.OpenGl;

namespace OpenRA.GlRenderer
{
	public class Texture : ITexture
	{
		internal int texture;

		public Texture(GraphicsDevice dev, Bitmap bitmap)
		{
			Gl.glGenTextures(1, out texture);
			GraphicsDevice.CheckGlError();
			SetData(bitmap);
		}

		public void SetData(Bitmap bitmap)
		{
			if (!IsPowerOf2(bitmap.Width) || !IsPowerOf2(bitmap.Height))
			{
				//throw new InvalidOperationException( "non-power-of-2-texture" );
				bitmap = new Bitmap(bitmap, new Size(NextPowerOf2(bitmap.Width), NextPowerOf2(bitmap.Height)));
			}

			Gl.glBindTexture(Gl.GL_TEXTURE_2D, texture);
			GraphicsDevice.CheckGlError();

			var bits = bitmap.LockBits(
				new Rectangle(0, 0, bitmap.Width, bitmap.Height),
				ImageLockMode.ReadOnly,
				PixelFormat.Format32bppArgb);

			Gl.glTexParameteri(Gl.GL_TEXTURE_2D, Gl.GL_TEXTURE_BASE_LEVEL, 0);
			GraphicsDevice.CheckGlError();
			Gl.glTexParameteri(Gl.GL_TEXTURE_2D, Gl.GL_TEXTURE_MAX_LEVEL, 0);
			GraphicsDevice.CheckGlError();
			Gl.glTexImage2D(Gl.GL_TEXTURE_2D, 0, Gl.GL_RGBA8, bits.Width, bits.Height,
				0, Gl.GL_BGRA, Gl.GL_UNSIGNED_BYTE, bits.Scan0);        // todo: weird strides
			GraphicsDevice.CheckGlError();

			bitmap.UnlockBits(bits);
		}

		bool IsPowerOf2(int v)
		{
			return (v & (v - 1)) == 0;
		}

		int NextPowerOf2(int v)
		{
			--v;
			v |= v >> 1;
			v |= v >> 2;
			v |= v >> 4;
			v |= v >> 8;
			++v;
			return v;
		}
	}
}
