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

using OpenRA.Mods.Common.Traits;

namespace OpenRA.Mods.Cnc.Traits
{
	public class TSChemicalMissilePowerInfo : NukePowerInfo
	{
		public override object Create(ActorInitializer init) { return new TSChemicalMissilePower(init.Self, this); }
	}

	sealed class TSChemicalMissilePower : NukePower
	{
		public TSChemicalMissilePower(Actor self, NukePowerInfo info)
			: base(self, info)
		{
		}

		// There is an event that informs you when a support power activates, but
		// unfortunately it doesn't tell you which power was used.
		protected override void AfterActivate(Actor self)
		{
			var playerResources = self.Owner.PlayerActor.Trait<TSPlayerResources>();
			playerResources.Veins = 0;
		}
	}
}
