#region Copyright & License Information
/*
 * Copyright 2007-2013 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Drawing;
using System.Linq;
using OpenRA.Graphics;

namespace OpenRA.Traits
{
	[Desc("Add this to the Player actor definition.")]
	public class PlayerHighlightPaletteInfo : ITraitInfo
	{
		[Desc("The prefix for the resulting player palettes")]
		public readonly string BaseName = "highlight";
		
		public object Create(ActorInitializer init) { return new PlayerHighlightPalette(init.self.Owner, this); }
	}
	
	public class PlayerHighlightPalette : ILoadsPalettes
	{
		readonly Player owner;
		readonly PlayerHighlightPaletteInfo info;
		
		public PlayerHighlightPalette(Player owner, PlayerHighlightPaletteInfo info)
		{
			this.owner = owner;
			this.info = info;
		}
		
		public void LoadPalettes(WorldRenderer wr)
		{
			var argb = (uint)Color.FromArgb(128, owner.Color.RGB).ToArgb();
			wr.AddPalette(info.BaseName + owner.InternalName, new ImmutablePalette(Enumerable.Range(0, Palette.Size).Select(i => i == 0 ? 0 : argb)));
		}
	}
}
