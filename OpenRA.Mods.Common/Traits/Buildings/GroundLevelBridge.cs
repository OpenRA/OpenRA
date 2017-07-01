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

using System.Collections.Generic;
using System.Linq;
using OpenRA.GameRules;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Bridge actor that can't be passed underneath.")]
	class GroundLevelBridgeInfo : ITraitInfo, IRulesetLoaded, Requires<BuildingInfo>, Requires<HealthInfo>
	{
		public readonly string TerrainType = "Bridge";

		public readonly string Type = "GroundLevelBridge";

		public readonly CVec[] NeighbourOffsets = { };

		[Desc("The name of the weapon to use when demolishing the bridge")]
		[WeaponReference] public readonly string DemolishWeapon = "Demolish";

		public WeaponInfo DemolishWeaponInfo { get; private set; }

		public void RulesetLoaded(Ruleset rules, ActorInfo ai) { DemolishWeaponInfo = rules.Weapons[DemolishWeapon.ToLowerInvariant()]; }

		public object Create(ActorInitializer init) { return new GroundLevelBridge(init.Self, this); }
	}

	class GroundLevelBridge : IBridgeSegment, INotifyAddedToWorld, INotifyRemovedFromWorld
	{
		public readonly GroundLevelBridgeInfo Info;
		readonly Actor self;
		readonly BridgeLayer bridgeLayer;
		readonly IEnumerable<CPos> cells;
		readonly Health health;

		public GroundLevelBridge(Actor self, GroundLevelBridgeInfo info)
		{
			Info = info;
			this.self = self;
			health = self.Trait<Health>();

			bridgeLayer = self.World.WorldActor.Trait<BridgeLayer>();
			var buildingInfo = self.Info.TraitInfo<BuildingInfo>();
			cells = buildingInfo.PathableTiles(self.Location);
		}

		void UpdateTerrain(Actor self, byte terrainIndex)
		{
			foreach (var cell in cells)
				self.World.Map.CustomTerrain[cell] = terrainIndex;

			var domainIndex = self.World.WorldActor.TraitOrDefault<DomainIndex>();
			if (domainIndex != null)
				domainIndex.UpdateCells(self.World, cells);
		}

		void INotifyAddedToWorld.AddedToWorld(Actor self)
		{
			bridgeLayer.Add(self);

			var tileSet = self.World.Map.Rules.TileSet;
			var terrainIndex = tileSet.GetTerrainIndex(Info.TerrainType);
			UpdateTerrain(self, terrainIndex);
			KillInvalidActorsInFootprint(self);
		}

		void INotifyRemovedFromWorld.RemovedFromWorld(Actor self)
		{
			bridgeLayer.Remove(self);

			UpdateTerrain(self, byte.MaxValue);
			KillInvalidActorsInFootprint(self);
		}

		void KillInvalidActorsInFootprint(Actor self)
		{
			foreach (var c in cells)
				foreach (var a in self.World.ActorMap.GetActorsAt(c))
					if (a.Info.HasTraitInfo<IPositionableInfo>() && !a.Trait<IPositionable>().CanEnterCell(c))
						a.Kill(self);
		}

		void IBridgeSegment.Repair(Actor repairer)
		{
			health.InflictDamage(self, repairer, new Damage(-health.MaxHP), true);
		}

		void IBridgeSegment.Demolish(Actor saboteur)
		{
			self.World.AddFrameEndTask(w =>
			{
				if (self.IsDead)
					return;

				// Use .FromPos since this actor is dead. Cannot use Target.FromActor
				Info.DemolishWeaponInfo.Impact(Target.FromPos(self.CenterPosition), saboteur, Enumerable.Empty<int>());

				self.World.WorldActor.Trait<ScreenShaker>().AddEffect(15, self.CenterPosition, 6);
				self.Kill(saboteur);
			});
		}

		string IBridgeSegment.Type { get { return Info.Type; } }
		DamageState IBridgeSegment.DamageState { get { return self.GetDamageState(); } }
		bool IBridgeSegment.Valid { get { return self.IsInWorld; } }
		CVec[] IBridgeSegment.NeighbourOffsets { get { return Info.NeighbourOffsets; } }
		CPos IBridgeSegment.Location { get { return self.Location; } }
	}
}