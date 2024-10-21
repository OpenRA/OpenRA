#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using OpenRA.Primitives;

namespace OpenRA.Widgets
{
	public struct WidgetBounds
	{
		public int X, Y, Width, Height;
		public readonly int Left => X;
		public readonly int Right => X + Width;
		public readonly int Top => Y;
		public readonly int Bottom => Y + Height;

		public WidgetBounds(int x, int y, int width, int height)
		{
			X = x;
			Y = y;
			Width = width;
			Height = height;
		}

		public readonly Rectangle ToRectangle()
		{
			return new Rectangle(X, Y, Width, Height);
		}
	}
}
