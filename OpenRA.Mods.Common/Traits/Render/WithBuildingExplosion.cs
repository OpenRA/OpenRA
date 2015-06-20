#region Copyright & License Information
/*
 * Copyright 2007-2015 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Collections.Generic;
using OpenRA.Effects;
using OpenRA.Mods.Common.Effects;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Display explosions over the building footprint when it is destroyed.")]
	class WithBuildingExplosionInfo : ITraitInfo, Requires<BuildingInfo>
	{
		[Desc("Group where Sequence is looked up.")]
		public readonly string SequenceCollection = "explosion";

		[Desc("Explosion sequence name to use")]
		[SequenceReference("SequenceCollection")] public readonly string Sequence = "building";

		[Desc("Delay the explosions by this many ticks.")]
		public readonly int Delay = 0;

		[Desc("Custom palette name")]
		public readonly string Palette = "effect";

		public object Create(ActorInitializer init) { return new WithBuildingExplosion(init.Self, this); }
	}

	class WithBuildingExplosion : INotifyKilled
	{
		WithBuildingExplosionInfo info;
		BuildingInfo buildingInfo;

		public WithBuildingExplosion(Actor self, WithBuildingExplosionInfo info)
		{
			this.info = info;
			buildingInfo = self.Info.Traits.Get<BuildingInfo>();
		}

		public void Killed(Actor self, AttackInfo e)
		{
			var cells = FootprintUtils.UnpathableTiles(self.Info.Name, buildingInfo, self.Location);

			if (info.Delay > 0)
				self.World.AddFrameEndTask(w => w.Add(new DelayedAction(info.Delay, () => SpawnExplosions(self.World, cells))));
			else
				SpawnExplosions(self.World, cells);
		}

		void SpawnExplosions(World world, IEnumerable<CPos> cells)
		{
			foreach (var c in cells)
				world.AddFrameEndTask(w => w.Add(new Explosion(w, w.Map.CenterOfCell(c), info.Sequence, info.Palette)));
		}
	}
}
