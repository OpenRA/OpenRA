#region Copyright & License Information
/*
 * Copyright 2007-2013 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Collections.Generic;

namespace OpenRA.Graphics
{
	public class RenderableComparer : IComparer<Renderable>
	{
		public int Compare(Renderable x, Renderable y)
		{
			return (x.Z + x.ZOffset).CompareTo(y.Z + y.ZOffset);
		}
	}

	public struct Renderable
	{
		public readonly Sprite Sprite;
		public readonly float2 Pos;
		public readonly PaletteReference Palette;
		public readonly int Z;
		public readonly int ZOffset;
		public float Scale;

		public Renderable(Sprite sprite, float2 pos, PaletteReference palette, int z, int zOffset, float scale)
		{
			Sprite = sprite;
			Pos = pos;
			Palette = palette;
			Z = z;
			ZOffset = zOffset;
			Scale = scale;
		}

		public Renderable(Sprite sprite, float2 pos, PaletteReference palette, int z)
			: this(sprite, pos, palette, z, 0, 1f) { }

		public Renderable(Sprite sprite, float2 pos, PaletteReference palette, int z, float scale)
			: this(sprite, pos, palette, z, 0, scale) { }

		public Renderable WithScale(float newScale) { return new Renderable(Sprite, Pos, Palette, Z, ZOffset, newScale); }
		public Renderable WithPalette(PaletteReference newPalette) { return new Renderable(Sprite, Pos, newPalette, Z, ZOffset, Scale); }
		public Renderable WithZOffset(int newOffset) { return new Renderable(Sprite, Pos, Palette, Z, newOffset, Scale); }
		public Renderable WithPos(float2 newPos) { return new Renderable(Sprite, newPos, Palette, Z, ZOffset, Scale); }
	}
}
