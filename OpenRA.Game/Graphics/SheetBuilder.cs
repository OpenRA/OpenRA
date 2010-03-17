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

namespace OpenRA.Graphics
{
	class SheetBuilder
	{
		public static SheetBuilder SharedInstance;
		public static void Initialize(Renderer r)
		{
			SharedInstance = new SheetBuilder(r, TextureChannel.Red);
		}

		public SheetBuilder(Renderer r, TextureChannel ch)
		{
			renderer = r;
			current = null;
			rowHeight = 0;
			channel = null;
			initialChannel = ch;
		}

		public Sprite Add(byte[] src, Size size)
		{
			Sprite rect = Allocate(size);
			Util.FastCopyIntoChannel(rect, src);
			return rect;
		}

		public Sprite Add(Size size, byte paletteIndex)
		{
			byte[] data = new byte[size.Width * size.Height];
			for (int i = 0; i < data.Length; i++)
				data[i] = paletteIndex;

			return Add(data, size);
		}

		Sheet NewSheet() { return new Sheet( renderer, new Size( Renderer.SheetSize, Renderer.SheetSize ) ); }

		Renderer renderer;
		Sheet current = null;
		int rowHeight = 0;
		Point p;
		TextureChannel? channel = null;
		TextureChannel initialChannel;

		TextureChannel? NextChannel(TextureChannel? t)
		{
			if (t == null)
				return initialChannel;

			switch (t.Value)
			{
				case TextureChannel.Red: return TextureChannel.Green;
				case TextureChannel.Green: return TextureChannel.Blue;
				case TextureChannel.Blue: return TextureChannel.Alpha;
				case TextureChannel.Alpha: return null;

				default: return null;
			}
		}

		public Sprite Allocate(Size imageSize)
		{
			if (current == null)
			{
				current = NewSheet();
				channel = NextChannel(null);
			}

			if (imageSize.Width + p.X > current.Size.Width)
			{
				p = new Point(0, p.Y + rowHeight);
				rowHeight = imageSize.Height;
			}

			if (imageSize.Height > rowHeight)
				rowHeight = imageSize.Height;

			if (p.Y + imageSize.Height > current.Size.Height)
			{

				if (null == (channel = NextChannel(channel)))
				{
					current = NewSheet();
					channel = NextChannel(channel);
				}

				rowHeight = imageSize.Height;
				p = new Point(0,0);
			}

			Sprite rect = new Sprite(current, new Rectangle(p, imageSize), channel.Value);
			current.MakeDirty();
			p.X += imageSize.Width;

			return rect;
		}
	}
}
