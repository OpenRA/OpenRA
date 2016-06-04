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

using System.Linq;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	public class UpgradeOnTerrainInfo : ITraitInfo, Requires<UpgradeManagerInfo>
	{
		[UpgradeGrantedReference]
		public readonly string[] Upgrades = { "terrain" };

		[Desc("Terrain names to trigger the upgrade.")]
		public readonly string[] TerrainTypes = { };

		public object Create(ActorInitializer init) { return new UpgradeOnTerrain(init, this); }
	}

	public class UpgradeOnTerrain : ITick
	{
		readonly Actor self;
		readonly UpgradeOnTerrainInfo info;
		readonly UpgradeManager manager;

		bool granted;
		string previousTerrain;

		public UpgradeOnTerrain(ActorInitializer init, UpgradeOnTerrainInfo info)
		{
			self = init.Self;
			this.info = info;
			manager = self.Trait<UpgradeManager>();
		}

		public void Tick(Actor self)
		{
			var currentTerrain = self.World.Map.GetTerrainInfo(self.Location).Type;
			var wantsGranted = info.TerrainTypes.Contains(currentTerrain);
			if (currentTerrain != previousTerrain)
			{
				if (wantsGranted && !granted)
				{
					foreach (var up in info.Upgrades)
						manager.GrantUpgrade(self, up, this);

					granted = true;
				}
				else if (!wantsGranted && granted)
				{
					foreach (var up in info.Upgrades)
						manager.RevokeUpgrade(self, up, this);

					granted = false;
				}
			}

			previousTerrain = currentTerrain;
		}
	}
}
