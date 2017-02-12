#region Copyright & License Information
/*
 * Copyright 2007-2017 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.Cnc.Traits
{
	[Desc("Steal and reset the owner's exploration.")]
	class InfiltrateForExplorationInfo : TraitInfo<InfiltrateForExploration> { }

	class InfiltrateForExploration : INotifyInfiltrated
	{
		void INotifyInfiltrated.Infiltrated(Actor self, Actor infiltrator)
		{
			infiltrator.Owner.Shroud.Explore(self.Owner.Shroud);
			if (!self.Owner.HasFogVisibility)
				self.Owner.Shroud.ResetExploration();
		}
	}
}
