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

namespace OpenRA.Mods.Common
{
	class InfiltrateForExplorationInfo : TraitInfo<InfiltrateForExploration>, Requires<InfiltratableInfo> { }

	class InfiltrateForExploration : IAcceptInfiltrator
	{
		public void OnInfiltrate(Actor self, Actor infiltrator)
		{
			// Steal and reset the owners exploration
			infiltrator.Owner.Shroud.Explore(self.Owner.Shroud);
			if (!self.Owner.Shroud.HasFogVisibility())
				self.Owner.Shroud.ResetExploration();
		}
	}
}
