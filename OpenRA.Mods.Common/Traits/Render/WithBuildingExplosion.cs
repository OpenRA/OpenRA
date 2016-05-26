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
using OpenRA.Effects;
using OpenRA.Mods.Common.Effects;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits.Render
{
	[Desc("Display explosions over the building footprint when it is destroyed.")]
	class WithBuildingExplosionInfo : ITraitInfo, Requires<BuildingInfo>
	{
		[Desc("'Image' where Sequences are looked up.")]
		public readonly string Image = "explosion";

		[Desc("Explosion sequence names to use.")]
		[SequenceReference("Image")] public readonly string[] Sequences = { "building" };

		[Desc("Delay the explosions by this many ticks.")]
		public readonly int Delay = 0;

		[Desc("Custom palette name.")]
		[PaletteReference] public readonly string Palette = "effect";

		public object Create(ActorInitializer init) { return new WithBuildingExplosion(init.Self, this); }
	}

	class WithBuildingExplosion : INotifyKilled
	{
		readonly WithBuildingExplosionInfo info;
		readonly BuildingInfo buildingInfo;

		public WithBuildingExplosion(Actor self, WithBuildingExplosionInfo info)
		{
			this.info = info;
			buildingInfo = self.Info.TraitInfo<BuildingInfo>();
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
				world.AddFrameEndTask(w => w.Add(new SpriteEffect(w.Map.CenterOfCell(c), w, info.Image, info.Sequences.Random(w.SharedRandom), info.Palette)));
		}
	}
}
