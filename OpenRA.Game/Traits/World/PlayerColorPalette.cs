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
namespace OpenRA.Traits
{	
	public class PlayerColorPaletteInfo : TraitInfo<PlayerColorPalette>
	{
		public readonly string BasePalette = null;
		public readonly PaletteFormat PaletteFormat = PaletteFormat.ra;
	}

	public class PlayerColorPalette {}
}
