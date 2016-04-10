#region Copyright & License Information
/*
 * Copyright 2007-2016 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using OpenRA.Traits;

namespace OpenRA.Mods.RA.Traits
{
	[Desc("Actors with the \"ClonesProducedUnits\" trait will produce a free duplicate of me.")]
	public class CloneableInfo : TraitInfo<Cloneable>
	{
		[FieldLoader.Require]
		[Desc("This unit's cloneable type is:")]
		public readonly HashSet<string> Types = new HashSet<string>();
	}

	public class Cloneable { }
}
