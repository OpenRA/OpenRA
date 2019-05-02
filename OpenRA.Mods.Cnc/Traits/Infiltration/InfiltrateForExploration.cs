#region Copyright & License Information
/*
 * Copyright 2007-2019 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Linq;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Cnc.Traits
{
	[Desc("Steal and reset the owner's exploration.")]
	class InfiltrateForExplorationInfo : ITraitInfo
	{
		public readonly BitSet<TargetableType> Types = default(BitSet<TargetableType>);

		public object Create(ActorInitializer init) { return new InfiltrateForExploration(init.Self, this); }
	}

	class InfiltrateForExploration : INotifyInfiltrated
	{
		readonly InfiltrateForExplorationInfo info;

		public InfiltrateForExploration(Actor self, InfiltrateForExplorationInfo info)
		{
			this.info = info;
		}

		void INotifyInfiltrated.Infiltrated(Actor self, Actor infiltrator, BitSet<TargetableType> types)
		{
			if (!info.Types.Overlaps(types))
				return;

			infiltrator.Owner.Shroud.Explore(self.Owner.Shroud);
			var preventReset = self.Owner.PlayerActor.TraitsImplementing<IPreventsShroudReset>()
				.Any(p => p.PreventShroudReset(self));
			if (!preventReset)
				self.Owner.Shroud.ResetExploration();
		}
	}
}
