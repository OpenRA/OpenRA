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

	public enum FootprintCellType
	{
		Empty = '_',
		OccupiedPassable = '=',
		Blocking = 'x'
	}

	public class BuildingInfo : ITraitInfo, IOccupySpaceInfo, IPlaceBuildingDecorationInfo, UsesInit<LocationInit>
	{
		[Desc("Where you are allowed to place the building (Water, Clear, ...)")]
		public readonly HashSet<string> TerrainTypes = new HashSet<string>();

		[Desc("The range to the next building it can be constructed. Set it higher for walls.")]
		public readonly int Adjacent = 2;

		[Desc("x means cell is blocked, = means part of the footprint but passable, ",
			"_ means completely empty.")]
		[FieldLoader.LoadUsing("LoadFootprint")]
		public readonly Dictionary<CVec, FootprintCellType> Footprint;

		public readonly CVec Dimensions = new CVec(1, 1);

		[Desc("Shift center of the actor by this offset.")]
		public readonly WVec LocalCenterOffset = WVec.Zero;

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

		protected static object LoadFootprint(MiniYaml yaml)
		{
			var footprintYaml = yaml.Nodes.FirstOrDefault(n => n.Key == "Footprint");
			var footprintChars = footprintYaml != null ? footprintYaml.Value.Value.Where(x => !char.IsWhiteSpace(x)).ToArray() : new[] { 'x' };

			var dimensionsYaml = yaml.Nodes.FirstOrDefault(n => n.Key == "Dimensions");
			var dim = dimensionsYaml != null ? FieldLoader.GetValue<CVec>("Dimensions", dimensionsYaml.Value.Value) : new CVec(1, 1);

			if (footprintChars.Length != dim.X * dim.Y)
			{
				var fp = footprintYaml.Value.Value.ToString();
				var dims = dim.X + "x" + dim.Y;
				throw new YamlException("Invalid footprint: {0} does not match dimensions {1}".F(fp, dims));
			}

			var index = 0;
			var ret = new Dictionary<CVec, FootprintCellType>();
			for (var y = 0; y < dim.Y; y++)
			{
				for (var x = 0; x < dim.X; x++)
				{
					var c = footprintChars[index++];
					if (!Enum.IsDefined(typeof(FootprintCellType), (FootprintCellType)c))
						throw new YamlException("Invalid footprint cell type '{0}'".F(c));

					ret[new CVec(x, y)] = (FootprintCellType)c;
				}
			}

			return ret;
		}

		public IEnumerable<CPos> FootprintTiles(CPos location, FootprintCellType type)
		{
			return Footprint.Where(kv => kv.Value == type).Select(kv => location + kv.Key);
		}

		public IEnumerable<CPos> Tiles(CPos location)
		{
			foreach (var t in FootprintTiles(location, FootprintCellType.OccupiedPassable))
				yield return t;

			foreach (var t in FootprintTiles(location, FootprintCellType.Blocking))
				yield return t;
		}

		public IEnumerable<CPos> FrozenUnderFogTiles(CPos location)
		{
			foreach (var t in FootprintTiles(location, FootprintCellType.Empty))
				yield return t;

			foreach (var t in Tiles(location))
				yield return t;
		}

		public IEnumerable<CPos> UnpathableTiles(CPos location)
		{
			foreach (var t in FootprintTiles(location, FootprintCellType.Blocking))
				yield return t;
		}

		public IEnumerable<CPos> PathableTiles(CPos location)
		{
			foreach (var t in FootprintTiles(location, FootprintCellType.Empty))
				yield return t;

			foreach (var t in FootprintTiles(location, FootprintCellType.OccupiedPassable))
				yield return t;
		}

		public CVec LocationOffset()
		{
			return new CVec(Dimensions.X / 2, Dimensions.Y > 1 ? (Dimensions.Y + 1) / 2 : 0);
		}

		public WVec CenterOffset(World w)
		{
			var off = (w.Map.CenterOfCell(new CPos(Dimensions.X, Dimensions.Y)) - w.Map.CenterOfCell(new CPos(1, 1))) / 2;
			return (off - new WVec(0, 0, off.Z)) + LocalCenterOffset;
		}

		public Actor FindBaseProvider(World world, Player p, CPos topLeft)
		{
			var center = world.Map.CenterOfCell(topLeft) + CenterOffset(world);
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

		public virtual bool IsCloseEnoughToBase(World world, Player p, string buildingName, CPos topLeft)
		{
			if (p.PlayerActor.Trait<DeveloperMode>().BuildAnywhere)
				return true;

			if (RequiresBaseProvider && FindBaseProvider(world, p, topLeft) == null)
				return false;

			var buildingMaxBounds = Dimensions;

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

			var buildingTiles = Tiles(topLeft).ToList();
			return nearnessCandidates
				.Any(a => buildingTiles
					.Any(b => Math.Abs(a.X - b.X) <= Adjacent
						&& Math.Abs(a.Y - b.Y) <= Adjacent));
		}

		public IReadOnlyDictionary<CPos, SubCell> OccupiedCells(ActorInfo info, CPos topLeft, SubCell subCell = SubCell.Any)
		{
			var occupied = UnpathableTiles(topLeft)
				.ToDictionary(c => c, c => SubCell.FullCell);

			return new ReadOnlyDictionary<CPos, SubCell>(occupied);
		}

		bool IOccupySpaceInfo.SharesCell { get { return false; } }

		public IEnumerable<IRenderable> Render(WorldRenderer wr, World w, ActorInfo ai, WPos centerPosition)
		{
			if (!RequiresBaseProvider)
				return SpriteRenderable.None;

			return w.ActorsWithTrait<BaseProvider>().SelectMany(a => a.Trait.RangeCircleRenderables(wr));
		}
	}

	public class Building : IOccupySpace, ITargetableCells, INotifySold, INotifyTransform, ISync, INotifyCreated, INotifyAddedToWorld, INotifyRemovedFromWorld
	{
		public readonly BuildingInfo Info;
		public bool BuildComplete { get; private set; }
		[Sync] readonly CPos topLeft;
		readonly Actor self;
		public readonly bool SkipMakeAnimation;

		Pair<CPos, SubCell>[] occupiedCells;
		Pair<CPos, SubCell>[] targetableCells;

		// Shared activity lock: undeploy, sell, capture, etc.
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

			occupiedCells = Info.UnpathableTiles(TopLeft)
				.Select(c => Pair.New(c, SubCell.FullCell)).ToArray();

			targetableCells = Info.FootprintTiles(TopLeft, FootprintCellType.Blocking)
				.Select(c => Pair.New(c, SubCell.FullCell)).ToArray();

			CenterPosition = init.World.Map.CenterOfCell(topLeft) + Info.CenterOffset(init.World);
			SkipMakeAnimation = init.Contains<SkipMakeAnimsInit>();
		}

		public IEnumerable<Pair<CPos, SubCell>> OccupiedCells() { return occupiedCells; }

		public IEnumerable<Pair<CPos, SubCell>> TargetableCells() { return targetableCells; }

		void INotifyCreated.Created(Actor self)
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

		void INotifyRemovedFromWorld.RemovedFromWorld(Actor self)
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

		void INotifySold.Selling(Actor self)
		{
			if (Info.RemoveSmudgesOnSell)
				RemoveSmudges();

			BuildComplete = false;
		}

		void INotifySold.Sold(Actor self) { }

		void INotifyTransform.BeforeTransform(Actor self)
		{
			if (Info.RemoveSmudgesOnTransform)
				RemoveSmudges();

			foreach (var s in Info.UndeploySounds)
				Game.Sound.PlayToPlayer(SoundType.World, self.Owner, s, self.CenterPosition);
		}

		void INotifyTransform.OnTransform(Actor self) { }
		void INotifyTransform.AfterTransform(Actor self) { }

		public void RemoveSmudges()
		{
			var smudgeLayers = self.World.WorldActor.TraitsImplementing<SmudgeLayer>();

			foreach (var smudgeLayer in smudgeLayers)
				foreach (var footprintTile in Info.Tiles(self.Location))
					smudgeLayer.RemoveSmudge(footprintTile);
		}
	}
}
