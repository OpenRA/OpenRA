#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
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
		[Desc("The prerequisite names that must be available before this can be built.",
			"This can be prefixed with ! to invert the prerequisite (disabling production if the prerequisite is available)",
			"and/or ~ to hide the actor from the production palette if the prerequisite is not available.",
			"Prerequisites are granted by actors with the Building trait (with a prerequisite string given by the lower case actor name)",
			"and by the ProvidesCustomPrerequisite trait.")]
		public readonly string[] Prerequisites = { };

		[Desc("Restrict production to a specific race(s). **Deprecated**: Use race-specific prerequisites instead.")]
		public readonly string[] Owner = { };

		[Desc("Production queue(s) that can produce this.")]
		public readonly string[] Queue = { };

		[Desc("Override the production structure type (from the Production Produces list) that this unit should be built at.")]
		public readonly string BuildAtProductionType = null;

		[Desc("Disable production when there are more than this many of this actor on the battlefield. Set to 0 to disable.")]
		public readonly int BuildLimit = 0;

		// TODO: UI fluff; doesn't belong here
		public readonly int BuildPaletteOrder = 9999;
		public readonly Hotkey Hotkey = Hotkey.Invalid;
	}

	public class Buildable { }
}
