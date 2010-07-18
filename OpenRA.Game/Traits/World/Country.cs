#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

namespace OpenRA.Traits
{
	public class CountryInfo : TraitInfo<Country>
	{
		public readonly string Name = null;
		public readonly string Race = null;

		/* todo: icon,... */
	}

	public class Country { /* we're only interested in the Info */ }
}
