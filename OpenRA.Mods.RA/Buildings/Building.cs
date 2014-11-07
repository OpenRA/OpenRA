#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Primitives;
using OpenRA.Traits;
using OpenRA.Mods.Common;
using OpenRA.Mods.RA.Render;

namespace OpenRA.Mods.RA.Buildings
{
	[Desc("Remove this trait to limit base-walking by cheap or defensive buildings.")]
	public class GivesBuildableAreaInfo : TraitInfo<GivesBuildableArea> {}
	public class GivesBuildableArea {}

	public class BuildingInfo : ITraitInfo, IOccupySpaceInfo, UsesInit<LocationInit>
	{
		[Desc("Where you are allowed to place the building (Water, Clear, ...)")]
		public readonly string[] TerrainTypes = {};
		[Desc("The range to the next building it can be constructed. Set it higher for walls.")]
		public readonly int Adjacent = 2;
		[Desc("x means space it blocks, _ is a part that is passable by actors.")]
		public readonly string Footprint = "x";
		public readonly CVec Dimensions = new CVec(1, 1);
		public readonly bool RequiresBaseProvider = false;
		public readonly bool AllowInvalidPlacement = false;

		public readonly string[] BuildSounds = { "placbldg.aud", "build5.aud" };
		public readonly string[] UndeploySounds = { "cashturn.aud" };

		public object Create(ActorInitializer init) { return new Building(init, this); }

		public Actor FindBaseProvider(World world, Player p, CPos topLeft)
		{
			var center = world.Map.CenterOfCell(topLeft) + FootprintUtils.CenterOffset(world, this);
			foreach (var bp in world.ActorsWithTrait<BaseProvider>())
			{
				var validOwner = bp.Actor.Owner == p || (world.LobbyInfo.GlobalSettings.AllyBuildRadius && bp.Actor.Owner.Stances[p].Allied());
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

			var buildingMaxBounds = Dimensions;
			var buildingTraits = world.Map.Rules.Actors[buildingName].Traits;
			if (buildingTraits.Contains<BibInfo>() && !(buildingTraits.Get<BibInfo>().HasMinibib))
				buildingMaxBounds += new CVec(0, 1);

			var scanStart = world.Map.Clamp(topLeft - new CVec(Adjacent, Adjacent));
			var scanEnd = world.Map.Clamp(topLeft + buildingMaxBounds + new CVec(Adjacent, Adjacent));

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

					if (at.Owner == p || (allyBuildRadius && at.Owner.Stances[p].Allied()))
						nearnessCandidates.Add(pos);
				}
			}

			var buildingTiles = FootprintUtils.Tiles(world.Map.Rules, buildingName, this, topLeft).ToList();
			return nearnessCandidates
				.Any(a => buildingTiles
					.Any(b => Math.Abs(a.X - b.X) <= Adjacent
						&& Math.Abs(a.Y - b.Y) <= Adjacent));
		}
	}

	public class Building : IOccupySpace, INotifySold, INotifyTransform, ISync, ITechTreePrerequisite, INotifyCreated, INotifyAddedToWorld, INotifyRemovedFromWorld
	{
		public readonly BuildingInfo Info;
		public bool BuildComplete { get; private set; }
		[Sync] readonly CPos topLeft;
		readonly Actor self;
		public readonly bool SkipMakeAnimation;

		/* shared activity lock: undeploy, sell, capture, etc */
		[Sync] public bool Locked = true;

		public bool Lock()
		{
			if (Locked)
				return false;

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

			occupiedCells = FootprintUtils.UnpathableTiles( self.Info.Name, Info, TopLeft )
				.Select(c => Pair.New(c, SubCell.FullCell)).ToArray();

			CenterPosition = init.world.Map.CenterOfCell(topLeft) + FootprintUtils.CenterOffset(init.world, Info);
			SkipMakeAnimation = init.Contains<SkipMakeAnimsInit>();
		}

		Pair<CPos, SubCell>[] occupiedCells;
		public IEnumerable<Pair<CPos, SubCell>> OccupiedCells() { return occupiedCells; }

		public void Created(Actor self)
		{
			if (SkipMakeAnimation || !self.HasTrait<WithMakeAnimation>())
				NotifyBuildingComplete(self);
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

		public void NotifyBuildingComplete(Actor self)
		{
			if (BuildComplete)
				return;

			BuildComplete = true;
			Locked = false;

			foreach (var notify in self.TraitsImplementing<INotifyBuildComplete>())
				notify.BuildingComplete(self);
		}

		public void Selling(Actor self)
		{
			BuildComplete = false;
		}

		public void Sold(Actor self) { }

		public void BeforeTransform(Actor self)
		{
			foreach (var s in Info.UndeploySounds)
				Sound.PlayToPlayer(self.Owner, s, self.CenterPosition);
		}
		public void OnTransform(Actor self) { }
		public void AfterTransform(Actor self) { }
	}
}
