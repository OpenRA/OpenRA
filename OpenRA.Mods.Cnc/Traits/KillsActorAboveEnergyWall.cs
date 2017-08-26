#region Copyright & License Information
/*
 * Copyright 2007-2018 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Linq;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.Cnc.Traits
{
	[Desc("Used to connect aircraft with `EnergyWall` logic.")]
	public class KillsActorAboveEnergyWallInfo : ConditionalTraitInfo
	{
		public override object Create(ActorInitializer init) { return new KillsActorAboveEnergyWall(init.Self, this); }
	}

	public class KillsActorAboveEnergyWall : ConditionalTrait<KillsActorAboveEnergyWallInfo>, ITick
	{
		public KillsActorAboveEnergyWall(Actor self, KillsActorAboveEnergyWallInfo info)
			: base(info) { }

		void ITick.Tick(Actor self)
		{
			if (IsTraitDisabled)
				return;

			var actors = self.World.ActorMap.GetActorsAt(self.Location).Where(a => a != self);
			foreach (var actor in actors)
			{
				var energyWall = actor.TraitOrDefault<EnergyWall>();
				if (energyWall != null)
					energyWall.Info.WeaponInfo.Impact(Target.FromActor(self), actor, Enumerable.Empty<int>());
			}
		}
	}
}