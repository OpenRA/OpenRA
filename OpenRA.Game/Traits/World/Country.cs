#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

namespace OpenRA.Traits
{
	public class CountryInfo : TraitInfo<Country>
	{
		public readonly string Name = null;
		public readonly string Race = null;
		public readonly bool Selectable = true;
	}

	public class Country { /* we're only interested in the Info */ }
}
