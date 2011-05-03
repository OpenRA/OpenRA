#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Linq;
using OpenRA.FileFormats;
using OpenRA.Graphics;
using OpenRA.Mods.RA.Widgets.Delegates;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	class ColorPickerPaletteModifierInfo : ITraitInfo
	{
		public string PlayerPalette = "player";
		public object Create( ActorInitializer init ) { return new ColorPickerPaletteModifier( this ); }
	}
	
	class ColorPickerPaletteModifier : IPalette, IPaletteModifier
	{	
		ColorPickerPaletteModifierInfo Info;
		PaletteFormat format;
		
		public ColorPickerPaletteModifier(ColorPickerPaletteModifierInfo info) { Info = info; }
		
		public void InitPalette( WorldRenderer wr )
		{
			var info = Rules.Info["player"].Traits.WithInterface<PlayerColorPaletteInfo>()
				.Where(p => p.BaseName == Info.PlayerPalette)
				.First();
			format = info.PaletteFormat;
			wr.AddPalette("colorpicker", wr.GetPalette(info.BasePalette));
		}

		public void AdjustPalette(Dictionary<string, Palette> palettes)
		{
			palettes["colorpicker"] = new Palette(palettes["colorpicker"],
			  new PlayerColorRemap(LobbyDelegate.CurrentColorPreview, format));
		}
	}
}
