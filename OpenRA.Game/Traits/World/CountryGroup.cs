#region Copyright & License Information
/*
 * Copyright 2007-2015 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion
using System;

namespace OpenRA.Traits
{
	[Desc("Used for random country selection.")]
	public class CountryGroupInfo : TraitInfo<CountryGroup>
	{
		[Desc("String displayed in the lobby dropdown.")]
		public readonly string DisplayName = null;

		[Desc("Used for UI flags and other race-specific things.",
		"Must not be shared with any other Countrys or CountryGroups.")]
		public readonly string Race = null;

		[Desc("Races that are a member of this group.")]
		public readonly string[] RaceMembers = { };
	}

	public class CountryGroup { }
}
