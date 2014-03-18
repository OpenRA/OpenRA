#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.FileFormats;
using OpenRA.Traits;

namespace OpenRA.Mods.RA.Buildings
{
	[Desc("Remove this trait to limit base-walking by cheap or defensive buildings.")]
	public class GivesBuildableAreaInfo : TraitInfo<GivesBuildableArea> {}
	public class GivesBuildableArea {}

	public class BuildingInfo : ITraitInfo, IOccupySpaceInfo, UsesInit<LocationInit>
	{
		[Desc("If negative, it will drain power, if positive, it will provide power.")]
		public readonly int Power = 0;
		[Desc("Where you are allowed to place the building (Water, Clear, ...)")]
		public readonly string[] TerrainTypes = {};
		[Desc("The range to the next building it can be constructed. Set it higher for walls.")]
		public readonly int Adjacent = 2;
		[Desc("x means space it blocks, _ is a part that is passable by actors.")]
		public readonly string Footprint = "x";
		public readonly int2 Dimensions = new int2(1, 1);
		public readonly bool RequiresBaseProvider = false;
		public readonly bool AllowInvalidPlacement = false;

		public readonly string[] BuildSounds = {"placbldg.aud", "build5.aud"};
		public readonly string[] SellSounds = {"cashturn.aud"};

		public object Create(ActorInitializer init) { return new Building(init, this); }

		public Actor FindBaseProvider(World world, Player p, CPos topLeft)
		{
			var center = topLeft.CenterPosition + FootprintUtils.CenterOffset(this);
			foreach (var bp in world.ActorsWithTrait<BaseProvider>())
			{
				var validOwner = bp.Actor.Owner == p || (world.LobbyInfo.GlobalSettings.AllyBuildRadius && bp.Actor.Owner.Stances[p] == Stance.Ally);
				if (!validOwner || !bp.Trait.Ready())
					continue;

				// Range is counted from the center of the actor, not from each cell.
				var target = Target.FromPos(bp.Actor.CenterPosition);
				if (target.IsInRange(center, WRange.FromCells(bp.Trait.Info.Range)))
					return bp.Actor;
			}
			return null;
		}

		public bool IsCloseEnoughToBase(World world, Player p, string buildingName, CPos topLeft)
		{
			if (p.PlayerActor.Trait<DeveloperMode>().BuildAnywhere)
				return true;

			if (RequiresBaseProvider && FindBaseProvider(world, p, topLeft) == null)
				return false;

			var buildingMaxBounds = (CVec)Dimensions;
			if (Rules.Info[buildingName].Traits.Contains<BibInfo>())
				buildingMaxBounds += new CVec(0, 1);

			var scanStart = world.ClampToWorld(topLeft - new CVec(Adjacent, Adjacent));
			var scanEnd = world.ClampToWorld(topLeft + buildingMaxBounds + new CVec(Adjacent, Adjacent));

			var nearnessCandidates = new List<CPos>();

			var bi = world.WorldActor.Trait<BuildingInfluence>();
			var allyBuildRadius = world.LobbyInfo.GlobalSettings.AllyBuildRadius;

			for (var y = scanStart.Y; y < scanEnd.Y; y++)
			{
				for (var x = scanStart.X; x < scanEnd.X; x++)
				{
				    var pos = new CPos(x, y);
					var at = bi.GetBuildingAt(pos);
					if (at == null || !at.IsInWorld || !at.HasTrait<GivesBuildableArea>())
						continue;

					if (at.Owner == p || (allyBuildRadius && at.Owner.Stances[p] == Stance.Ally))
						nearnessCandidates.Add(pos);
				}
			}

			var buildingTiles = FootprintUtils.Tiles(buildingName, this, topLeft).ToList();
			return nearnessCandidates
				.Any(a => buildingTiles
					.Any(b => Math.Abs(a.X - b.X) <= Adjacent
						&& Math.Abs(a.Y - b.Y) <= Adjacent));
		}
	}

	public class Building : INotifyDamage, IOccupySpace, INotifyCapture, ISync, ITechTreePrerequisite, INotifyAddedToWorld, INotifyRemovedFromWorld
	{
		readonly Actor self;
		public readonly BuildingInfo Info;
		[Sync] readonly CPos topLeft;

		PowerManager PlayerPower;

		[Sync] public bool Locked;	/* shared activity lock: undeploy, sell, capture, etc */

		public bool Lock()
		{
			if (Locked) return false;
			Locked = true;
			return true;
		}

		public void Unlock() { Locked = false; }

		public CPos TopLeft { get { return topLeft; } }
		public WPos CenterPosition { get; private set; }

		public IEnumerable<string> ProvidesPrerequisites { get { yield return self.Info.Name; } }

		public Building(ActorInitializer init, BuildingInfo info)
		{
			this.self = init.self;
			this.topLeft = init.Get<LocationInit, CPos>();
			this.Info = info;
			this.PlayerPower = init.self.Owner.PlayerActor.Trait<PowerManager>();

			occupiedCells = FootprintUtils.UnpathableTiles( self.Info.Name, Info, TopLeft )
				.Select(c => Pair.New(c, SubCell.FullCell)).ToArray();

			CenterPosition = topLeft.CenterPosition + FootprintUtils.CenterOffset(Info);
		}

		public int GetPowerUsage()
		{
			if (Info.Power <= 0)
				return Info.Power;

			var health = self.TraitOrDefault<Health>();
			return health != null ? (Info.Power * health.HP / health.MaxHP) : Info.Power;
		}

		public void Damaged(Actor self, AttackInfo e)
		{
			// Power plants lose power with damage
			if (Info.Power > 0)
				PlayerPower.UpdateActor(self, GetPowerUsage());
		}

		Pair<CPos, SubCell>[] occupiedCells;
		public IEnumerable<Pair<CPos, SubCell>> OccupiedCells() { return occupiedCells; }

		public void OnCapture(Actor self, Actor captor, Player oldOwner, Player newOwner)
		{
			PlayerPower = newOwner.PlayerActor.Trait<PowerManager>();
		}

		public void AddedToWorld(Actor self)
		{
			self.World.ActorMap.AddInfluence(self, this);
			self.World.ActorMap.AddPosition(self, this);
			self.World.ScreenMap.Add(self);
		}

		public void RemovedFromWorld(Actor self)
		{
			self.World.ActorMap.RemoveInfluence(self, this);
			self.World.ActorMap.RemovePosition(self, this);
			self.World.ScreenMap.Remove(self);
		}
	}
}
