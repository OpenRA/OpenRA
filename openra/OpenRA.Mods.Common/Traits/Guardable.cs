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

using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("This unit can be guarded (followed and protected) by a Guard unit.")]
	public class GuardableInfo : TraitInfo<Guardable>
	{
		[Desc("Maximum range that guarding actors will maintain.")]
		public readonly WDist Range = WDist.FromCells(2);
	}

	public class Guardable { }
}
