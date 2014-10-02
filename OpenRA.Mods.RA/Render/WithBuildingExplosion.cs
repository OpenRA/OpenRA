#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using OpenRA.Mods.RA.Buildings;
using OpenRA.Mods.Common.Effects;
using OpenRA.Traits;

namespace OpenRA.Mods.RA.Render
{
	[Desc("Display explosions over the building footprint when it is destroyed.")]
	class WithBuildingExplosionInfo : ITraitInfo, Requires<BuildingInfo>
	{
		[Desc("Explosion sequence name to use")]
		public readonly string Sequence = "building";

		[Desc("Custom palette name")]
		public readonly string Palette = "effect";
		
		public object Create(ActorInitializer init) { return new WithBuildingExplosion(this); }
	}

	class WithBuildingExplosion : INotifyKilled
	{
		WithBuildingExplosionInfo info;

		public WithBuildingExplosion(WithBuildingExplosionInfo info)
		{
			this.info = info;
		}

		public void Killed(Actor self, AttackInfo e)
		{
			var buildingInfo = self.Info.Traits.Get<BuildingInfo>();
			FootprintUtils.UnpathableTiles(self.Info.Name, buildingInfo, self.Location).Do(
				t => self.World.AddFrameEndTask(w => w.Add(new Explosion(w, w.Map.CenterOfCell(t), info.Sequence, info.Palette))));
		}
	}
}
