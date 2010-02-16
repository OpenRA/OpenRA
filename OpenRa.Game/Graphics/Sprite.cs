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

namespace OpenRa.Graphics
{
	public class Sprite
	{
		public readonly Rectangle bounds;
		public readonly Sheet sheet;
		public readonly TextureChannel channel;
		public readonly RectangleF uv;
		public readonly float2 size;

		readonly float2[] uvhax;

		internal Sprite(Sheet sheet, Rectangle bounds, TextureChannel channel)
		{
			this.bounds = bounds;
			this.sheet = sheet;
			this.channel = channel;

			uv = new RectangleF(
					(float)(bounds.Left) / sheet.Size.Width,
					(float)(bounds.Top) / sheet.Size.Height,
					(float)(bounds.Width) / sheet.Size.Width,
					(float)(bounds.Height) / sheet.Size.Height);

			uvhax = new float2[]
			{
				new float2( uv.Left, uv.Top ),
				new float2( uv.Right, uv.Top ),
				new float2( uv.Left, uv.Bottom ),
				new float2( uv.Right, uv.Bottom ),
			};

			this.size = new float2(bounds.Size);
		}

		public float2 FastMapTextureCoords( int k )
		{
			return uvhax[ k ];
		}
	}

	public enum TextureChannel
	{
		Red = 0,
		Green = 1,
		Blue = 2,
		Alpha = 3,
	}
}
