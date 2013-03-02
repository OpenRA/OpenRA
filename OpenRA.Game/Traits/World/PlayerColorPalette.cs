#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using OpenRA.FileFormats;
using OpenRA.Graphics;

namespace OpenRA.Traits
{
	public class PlayerColorPaletteInfo : ITraitInfo
	{
		public readonly string BasePalette = null;
		public readonly string BaseName = "player";
		public readonly int[] RemapIndex = {};
		public readonly bool AllowModifiers = true;

		public object Create( ActorInitializer init ) { return new PlayerColorPalette( init.self.Owner, this ); }
	}

	public class PlayerColorPalette : IPalette
	{
		readonly Player owner;
		readonly PlayerColorPaletteInfo info;

		public PlayerColorPalette( Player owner, PlayerColorPaletteInfo info )
		{
			this.owner = owner;
			this.info = info;
		}

		public void InitPalette( WorldRenderer wr )
		{
			var paletteName = "{0}{1}".F( info.BaseName, owner.InternalName );
			var newpal = new Palette(wr.Palette(info.BasePalette).Palette,
							 new PlayerColorRemap(info.RemapIndex, owner.ColorRamp));
			wr.AddPalette(paletteName, newpal, info.AllowModifiers);
		}
	}
}
