#region Copyright & License Information
/*
 * Copyright 2007-2022 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

namespace OpenRA.Graphics
{
	public sealed class PaletteReference
	{
		readonly float index;
		readonly HardwarePalette hardwarePalette;

		public readonly string Name;
		public IPalette Palette { get; internal set; }
		public float TextureIndex => index / hardwarePalette.Height;
		public float TextureMidIndex => (index + 0.5f) / hardwarePalette.Height;

		public PaletteReference(string name, int index, IPalette palette, HardwarePalette hardwarePalette)
		{
			Name = name;
			Palette = palette;
			this.index = index;
			this.hardwarePalette = hardwarePalette;
		}

		public bool HasColorShift => hardwarePalette.HasColorShift(Name);
	}
}
