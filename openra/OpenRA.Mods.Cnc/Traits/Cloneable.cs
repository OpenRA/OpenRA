#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Cnc.Traits
{
	// Type tag for CloneableTypes
	public class CloneableType { }

	[Desc("Actors with the \"ClonesProducedUnits\" trait will produce a free duplicate of me.")]
	public class CloneableInfo : TraitInfo<Cloneable>
	{
		[FieldLoader.Require]
		[Desc("This unit's cloneable type is:")]
		public readonly BitSet<CloneableType> Types = default;
	}

	public class Cloneable { }
}
