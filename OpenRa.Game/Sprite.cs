using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;

namespace OpenRa.Game
{
	class Sprite
	{
		public readonly Point origin;
		public readonly Size size;
		public readonly Sheet sheet;
		public readonly TextureChannel channel;

		internal Sprite(Sheet sheet, Point origin, Size size, TextureChannel channel)
		{
			this.origin = origin;
			this.size = size;
			this.sheet = sheet;
			this.channel = channel;
		}
	}

	public enum TextureChannel
	{
		Red,
		Green,
		Blue,
		Alpha,
	}
}
