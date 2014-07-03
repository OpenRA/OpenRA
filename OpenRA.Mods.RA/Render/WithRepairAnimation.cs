#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Linq;
using OpenRA.Traits;

namespace OpenRA.Mods.RA.Render
{
	[Desc("Replaces the building animation when it repairs a unit.")]
	public class WithRepairAnimationInfo : ITraitInfo, Requires<RenderBuildingInfo>
	{
		[Desc("Sequence name to use")]
		public readonly string Sequence = "active";

		public readonly bool PauseOnLowPower = false;

		public object Create(ActorInitializer init) { return new WithRepairAnimation(init.self, this); }
	}

	public class WithRepairAnimation : INotifyRepair
	{
		IEnumerable<IDisable> disabled;
		WithRepairAnimationInfo info;

		public WithRepairAnimation(Actor self, WithRepairAnimationInfo info)
		{
			disabled = self.TraitsImplementing<IDisable>();
			this.info = info;
		}

		public void Repairing(Actor self, Actor host)
		{
			var building = host.TraitOrDefault<RenderBuilding>();
			if (building != null && !(info.PauseOnLowPower && disabled.Any(d => d.Disabled)))
				building.PlayCustomAnim(host, info.Sequence);
		}
	}
}