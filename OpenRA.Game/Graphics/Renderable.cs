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
using System.Drawing;

namespace OpenRA.Graphics
{
	public class RenderableComparer : IComparer<Renderable>
	{
		public int Compare(Renderable x, Renderable y)
		{
			return x.SortOrder.CompareTo(y.SortOrder);
		}
	}

	public struct Renderable
	{
		readonly Sprite Sprite;
		readonly float2 Pos;
		readonly int Z;
		float Scale;

		// TODO: Fix Parachute and WithShadow so these can be made private
		public readonly PaletteReference Palette;
		public readonly int ZOffset;

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

		public Renderable WithPxOffset(float2 offset) { return new Renderable(Sprite, Pos + offset, Palette, Z, ZOffset, Scale); }
		public Renderable WithZOffset(int newOffset) { return new Renderable(Sprite, Pos, Palette, Z, newOffset, Scale); }

		public void Render(WorldRenderer wr)
		{
			Sprite.DrawAt(Pos, Palette.Index, Scale);
		}

		public Size Size
		{
			get
			{
				var size = (Scale*Sprite.size).ToInt2();
				return new Size(size.X, size.Y);
			}
		}

		public int SortOrder { get { return Z + ZOffset; } }
	}
}
