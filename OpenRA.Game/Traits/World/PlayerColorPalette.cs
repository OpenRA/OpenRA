#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
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
		public readonly PaletteFormat PaletteFormat = PaletteFormat.ra;

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
			var paletteName = "{0}{1}".F( info.BaseName, owner.Index );
			var newpal = new Palette(wr.GetPalette(info.BasePalette),
			                 new PlayerColorRemap(owner.Color, owner.Color2, info.PaletteFormat));
			wr.AddPalette(paletteName, newpal);
		}
	}
}
