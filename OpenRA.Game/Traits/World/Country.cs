#region Copyright & License Information
/*
 * Copyright 2007-2015 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

namespace OpenRA.Traits
{
	[Desc("Attach this to the `World` actor.")]
	public class CountryInfo : TraitInfo<Country>
	{
		[Desc("This is the name exposed to the players.")]
		public readonly string Name = null;

		[Desc("This is the internal name for owner checks.")]
		public readonly string Race = null;

		[Desc("Pick a random race as the player's race out of this list.")]
		public readonly string[] RandomRaceMembers = { };

		[Desc("The side that the country belongs to. For example, England belongs to the 'Allies' side.")]
		public readonly string Side = null;

		[Translate]
		public readonly string Description = null;

		public readonly bool Selectable = true;
	}

	public class Country { /* we're only interested in the Info */ }
}
