#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using System.Drawing;
using OpenRA.Traits;
using System.Collections.Generic;
using OpenRA.FileFormats;
using OpenRA.Widgets.Delegates;

namespace OpenRA.Mods.RA
{
	class ColorPickerPaletteModifierInfo : TraitInfo<ColorPickerPaletteModifier> {}
	
	class ColorPickerPaletteModifier : IPaletteModifier, ILoadWorldHook
	{	
		bool SplitPlayerPalette;
		public void WorldLoaded(World w)
		{
			// Copy the base palette for the colorpicker
			var info = Rules.Info["world"].Traits.Get<PlayerColorPaletteInfo>();
			SplitPlayerPalette = info.SplitRamp;
			w.WorldRenderer.AddPalette("colorpicker", w.WorldRenderer.GetPalette(info.BasePalette));
		}
		
		public void AdjustPalette(Dictionary<string,Palette> palettes)
		{
			palettes["colorpicker"] = new Palette(palettes["colorpicker"],
			                              new PlayerColorRemap(LobbyDelegate.CurrentColorPreview1, LobbyDelegate.CurrentColorPreview2, SplitPlayerPalette));
		}
	}
}
