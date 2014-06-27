#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	public class BuildableInfo : TraitInfo<Buildable>
	{
		public readonly string[] Prerequisites = { };
		public readonly string[] Owner = { };

		public readonly string[] Queue = { };
		public readonly int BuildLimit = 0;

		// TODO: UI fluff; doesn't belong here
		public readonly int BuildPaletteOrder = 9999;
		public readonly Hotkey Hotkey = Hotkey.Invalid;
	}

	public class Buildable { }
}
