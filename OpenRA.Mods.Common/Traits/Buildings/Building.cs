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

using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Traits.Render;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Remove this trait to limit base-walking by cheap or defensive buildings.")]
	public class GivesBuildableAreaInfo : TraitInfo<GivesBuildableArea> { }
	public class GivesBuildableArea { }

	public class BuildingInfo : ITraitInfo, IOccupySpaceInfo, IPlaceBuildingDecorationInfo, UsesInit<LocationInit>
	{
		[Desc("Where you are allowed to place the building (Water, Clear, ...)")]
		public readonly HashSet<string> TerrainTypes = new HashSet<string>();
		[Desc("The range to the next building it can be constructed. Set it higher for walls.")]
		public readonly int Adjacent = 2;
		[Desc("x means space it blocks, _ is a part that is passable by actors.")]
		public readonly string Footprint = "x";
		public readonly CVec Dimensions = new CVec(1, 1);
		public readonly bool RequiresBaseProvider = false;
		public readonly bool AllowInvalidPlacement = false;
		[Desc("Clear smudges from underneath the building footprint.")]
		public readonly bool RemoveSmudgesOnBuild = true;
		[Desc("Clear smudges from underneath the building footprint on sell.")]
		public readonly bool RemoveSmudgesOnSell = true;
		[Desc("Clear smudges from underneath the building footprint on transform.")]
		public readonly bool RemoveSmudgesOnTransform = true;

		public readonly string[] BuildSounds = { "placbldg.aud", "build5.aud" };
		public readonly string[] UndeploySounds = { "cashturn.aud" };

		public virtual object Create(ActorInitializer init) { return new Building(init, this); }

		public Actor FindBaseProvider(World world, Player p, CPos topLeft)
		{
			var center = world.Map.CenterOfCell(topLeft) + FootprintUtils.CenterOffset(world, this);
			var allyBuildEnabled = world.WorldActor.Trait<MapBuildRadius>().AllyBuildRadiusEnabled;

			foreach (var bp in world.ActorsWithTrait<BaseProvider>())
			{
				var validOwner = bp.Actor.Owner == p || (allyBuildEnabled && bp.Actor.Owner.Stances[p] == Stance.Ally);
				if (!validOwner || !bp.Trait.Ready())
					continue;

				// Range is counted from the center of the actor, not from each cell.
				var target = Target.FromPos(bp.Actor.CenterPosition);
				if (target.IsInRange(center, bp.Trait.Info.Range))
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
			var bibInfo = world.Map.Rules.Actors[buildingName].TraitInfoOrDefault<BibInfo>();
			if (bibInfo != null && !bibInfo.HasMinibib)
				buildingMaxBounds += new CVec(0, 1);

			var scanStart = world.Map.Clamp(topLeft - new CVec(Adjacent, Adjacent));
			var scanEnd = world.Map.Clamp(topLeft + buildingMaxBounds + new CVec(Adjacent, Adjacent));

			var nearnessCandidates = new List<CPos>();
			var bi = world.WorldActor.Trait<BuildingInfluence>();
			var allyBuildEnabled = world.WorldActor.Trait<MapBuildRadius>().AllyBuildRadiusEnabled;

			for (var y = scanStart.Y; y < scanEnd.Y; y++)
			{
				for (var x = scanStart.X; x < scanEnd.X; x++)
				{
					var pos = new CPos(x, y);

					var buildingAtPos = bi.GetBuildingAt(pos);

					if (buildingAtPos == null)
					{
						var unitsAtPos = world.ActorMap.GetActorsAt(pos).Where(a => a.IsInWorld
							&& (a.Owner == p || (allyBuildEnabled && a.Owner.Stances[p] == Stance.Ally))
							&& a.Info.HasTraitInfo<GivesBuildableAreaInfo>());

						if (unitsAtPos.Any())
							nearnessCandidates.Add(pos);
					}
					else if (buildingAtPos.IsInWorld && buildingAtPos.Info.HasTraitInfo<GivesBuildableAreaInfo>()
						&& (buildingAtPos.Owner == p || (allyBuildEnabled && buildingAtPos.Owner.Stances[p] == Stance.Ally)))
						nearnessCandidates.Add(pos);
				}
			}

			var buildingTiles = FootprintUtils.Tiles(world.Map.Rules, buildingName, this, topLeft).ToList();
			return nearnessCandidates
				.Any(a => buildingTiles
					.Any(b => Math.Abs(a.X - b.X) <= Adjacent
						&& Math.Abs(a.Y - b.Y) <= Adjacent));
		}

		public IReadOnlyDictionary<CPos, SubCell> OccupiedCells(ActorInfo info, CPos topLeft, SubCell subCell = SubCell.Any)
		{
			var occupied = FootprintUtils.UnpathableTiles(info.Name, this, topLeft)
				.ToDictionary(c => c, c => SubCell.FullCell);

			return new ReadOnlyDictionary<CPos, SubCell>(occupied);
		}

		bool IOccupySpaceInfo.SharesCell { get { return false; } }

		public IEnumerable<IRenderable> Render(WorldRenderer wr, World w, ActorInfo ai, WPos centerPosition)
		{
			if (!RequiresBaseProvider)
				yield break;

			foreach (var a in w.ActorsWithTrait<BaseProvider>())
				foreach (var r in a.Trait.RenderAfterWorld(wr))
					yield return r;
		}
	}

	public class Building : IOccupySpace, INotifySold, INotifyTransform, ISync, INotifyCreated, INotifyAddedToWorld, INotifyRemovedFromWorld, ITargetablePositions
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

		public Building(ActorInitializer init, BuildingInfo info)
		{
			self = init.Self;
			topLeft = init.Get<LocationInit, CPos>();
			Info = info;

			occupiedCells = FootprintUtils.UnpathableTiles(self.Info.Name, Info, TopLeft)
				.Select(c => Pair.New(c, SubCell.FullCell)).ToArray();

			CenterPosition = init.World.Map.CenterOfCell(topLeft) + FootprintUtils.CenterOffset(init.World, Info);
			SkipMakeAnimation = init.Contains<SkipMakeAnimsInit>();
		}

		Pair<CPos, SubCell>[] occupiedCells;
		public IEnumerable<Pair<CPos, SubCell>> OccupiedCells() { return occupiedCells; }

		public IEnumerable<WPos> TargetablePositions(Actor self)
		{
			return OccupiedCells().Select(c => self.World.Map.CenterOfCell(c.First));
		}

		public void Created(Actor self)
		{
			if (SkipMakeAnimation || !self.Info.HasTraitInfo<WithMakeAnimationInfo>())
				NotifyBuildingComplete(self);
		}

		public virtual void AddedToWorld(Actor self)
		{
			if (Info.RemoveSmudgesOnBuild)
				RemoveSmudges();

			self.World.ActorMap.AddInfluence(self, this);
			self.World.ActorMap.AddPosition(self, this);

			if (!self.Bounds.Size.IsEmpty)
				self.World.ScreenMap.Add(self);
		}

		public void RemovedFromWorld(Actor self)
		{
			self.World.ActorMap.RemoveInfluence(self, this);
			self.World.ActorMap.RemovePosition(self, this);

			if (!self.Bounds.Size.IsEmpty)
				self.World.ScreenMap.Remove(self);
		}

		public void NotifyBuildingComplete(Actor self)
		{
			if (BuildComplete)
				return;

			BuildComplete = true;
			Unlock();

			foreach (var notify in self.TraitsImplementing<INotifyBuildComplete>())
				notify.BuildingComplete(self);
		}

		public void Selling(Actor self)
		{
			if (Info.RemoveSmudgesOnSell)
				RemoveSmudges();

			BuildComplete = false;
		}

		public void Sold(Actor self) { }

		public void BeforeTransform(Actor self)
		{
			if (Info.RemoveSmudgesOnTransform)
				RemoveSmudges();

			foreach (var s in Info.UndeploySounds)
				Game.Sound.PlayToPlayer(self.Owner, s, self.CenterPosition);
		}

		public void OnTransform(Actor self) { }
		public void AfterTransform(Actor self) { }

		public void RemoveSmudges()
		{
			var smudgeLayers = self.World.WorldActor.TraitsImplementing<SmudgeLayer>();

			foreach (var smudgeLayer in smudgeLayers)
				foreach (var footprintTile in FootprintUtils.Tiles(self))
					smudgeLayer.RemoveSmudge(footprintTile);
		}
	}
}
