#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using System.Collections.Generic;
using OpenRA.FileFormats;
using OpenRA.Traits;
using OpenRA.Widgets.Delegates;
using OpenRA.Graphics;

namespace OpenRA.Mods.RA
{
	class ColorPickerPaletteModifierInfo : TraitInfo<ColorPickerPaletteModifier> {}
	
	class ColorPickerPaletteModifier : IPalette, IPaletteModifier
	{	
		PaletteFormat format;
		public void InitPalette( WorldRenderer wr )
		{
			var info = Rules.Info["player"].Traits.Get<PlayerColorPaletteInfo>();
			format = info.PaletteFormat;
			wr.AddPalette("colorpicker", wr.GetPalette(info.BasePalette));
		}

		public void AdjustPalette(Dictionary<string, Palette> palettes)
		{
			palettes["colorpicker"] = new Palette(palettes["colorpicker"],
			  new PlayerColorRemap(LobbyDelegate.CurrentColorPreview1,
				  LobbyDelegate.CurrentColorPreview2, format));
		}
	}
}
