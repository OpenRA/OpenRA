#region Copyright & License Information
/*
 * Copyright 2007-2015 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using OpenRA.Activities;
using OpenRA.Mods.Common;
using OpenRA.Mods.Common.Activities;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Flags]
	public enum CellConditions
	{
		None = 0,
		TransientActors,
		BlockedByMovers,
		All = TransientActors | BlockedByMovers
	}

	public static class CellConditionsExts
	{
		public static bool HasCellCondition(this CellConditions c, CellConditions cellCondition)
		{
			return (c & cellCondition) == cellCondition;
		}
	}

	[Desc("Unit is able to move.")]
	public class MobileInfo : IMoveInfo, IPositionableInfo, IOccupySpaceInfo, IFacingInfo,
		UsesInit<FacingInit>, UsesInit<LocationInit>, UsesInit<SubCellInit>
	{
		[FieldLoader.LoadUsing("LoadSpeeds", true)]
		[Desc("Set Water: 0 for ground units and lower the value on rough terrain.")]
		public readonly Dictionary<string, TerrainInfo> TerrainSpeeds;

		[Desc("e.g. crate, wall, infantry")]
		public readonly HashSet<string> Crushes = new HashSet<string>();

		public readonly int WaitAverage = 5;

		public readonly int WaitSpread = 2;

		public readonly int InitialFacing = 0;

		[Desc("Rate of Turning")]
		public readonly int ROT = 255;

		public readonly int Speed = 1;

		public readonly bool OnRails = false;

		[Desc("Allow multiple (infantry) units in one cell.")]
		public readonly bool SharesCell = false;

		[Desc("Can the actor be ordered to move in to shroud?")]
		public readonly bool MoveIntoShroud = true;

		public readonly string Cursor = "move";
		public readonly string BlockedCursor = "move-blocked";

		[VoiceReference] public readonly string Voice = "Action";

		public virtual object Create(ActorInitializer init) { return new Mobile(init, this); }

		static object LoadSpeeds(MiniYaml y)
		{
			var ret = new Dictionary<string, TerrainInfo>();
			foreach (var t in y.ToDictionary()["TerrainSpeeds"].Nodes)
			{
				var speed = FieldLoader.GetValue<int>("speed", t.Value.Value);
				var nodesDict = t.Value.ToDictionary();
				var cost = nodesDict.ContainsKey("PathingCost")
					? FieldLoader.GetValue<int>("cost", nodesDict["PathingCost"].Value)
					: (int)(10000 / speed);
				ret.Add(t.Key, new TerrainInfo(speed, cost));
			}

			return ret;
		}

		TerrainInfo[] LoadTilesetSpeeds(TileSet tileSet)
		{
			var info = new TerrainInfo[tileSet.TerrainInfo.Length];
			for (var i = 0; i < info.Length; i++)
				info[i] = TerrainInfo.Impassable;

			foreach (var kvp in TerrainSpeeds)
			{
				byte index;
				if (tileSet.TryGetTerrainIndex(kvp.Key, out index))
					info[index] = kvp.Value;
			}

			return info;
		}

		public class TerrainInfo
		{
			public static readonly TerrainInfo Impassable = new TerrainInfo();

			public readonly int Cost;
			public readonly int Speed;

			public TerrainInfo()
			{
				Cost = int.MaxValue;
				Speed = 0;
			}

			public TerrainInfo(int speed, int cost)
			{
				Speed = speed;
				Cost = cost;
			}
		}

		public struct WorldMovementInfo
		{
			internal readonly World World;
			internal readonly TerrainInfo[] TerrainInfos;
			internal WorldMovementInfo(World world, MobileInfo info)
			{
				World = world;
				TerrainInfos = info.TilesetTerrainInfo[world.TileSet];
			}
		}

		public readonly Cache<TileSet, TerrainInfo[]> TilesetTerrainInfo;
		public readonly Cache<TileSet, int> TilesetMovementClass;

		public MobileInfo()
		{
			TilesetTerrainInfo = new Cache<TileSet, TerrainInfo[]>(LoadTilesetSpeeds);
			TilesetMovementClass = new Cache<TileSet, int>(CalculateTilesetMovementClass);
		}

		public int MovementCostForCell(World world, CPos cell)
		{
			return MovementCostForCell(world.Map, TilesetTerrainInfo[world.TileSet], cell);
		}

		int MovementCostForCell(Map map, TerrainInfo[] terrainInfos, CPos cell)
		{
			if (!map.Contains(cell))
				return int.MaxValue;

			var index = map.GetTerrainIndex(cell);
			if (index == byte.MaxValue)
				return int.MaxValue;

			return terrainInfos[index].Cost;
		}

		public int CalculateTilesetMovementClass(TileSet tileset)
		{
			/* collect our ability to cross *all* terraintypes, in a bitvector */
			return TilesetTerrainInfo[tileset].Select(ti => ti.Cost < int.MaxValue).ToBits();
		}

		public int GetMovementClass(TileSet tileset)
		{
			return TilesetMovementClass[tileset];
		}

		static bool IsMovingInMyDirection(Actor self, Actor other)
		{
			if (!other.IsMoving()) return false;
			if (self == null) return true;

			var selfMobile = self.TraitOrDefault<Mobile>();
			if (selfMobile == null) return false;

			var otherMobile = other.TraitOrDefault<Mobile>();
			if (otherMobile == null) return false;

			// Sign of dot-product indicates (roughly) if vectors are facing in same or opposite directions:
			var dp = CVec.Dot(selfMobile.ToCell - self.Location, otherMobile.ToCell - other.Location);

			return dp > 0;
		}

		public int TileSetMovementHash(TileSet tileSet)
		{
			var terrainInfos = TilesetTerrainInfo[tileSet];

			// Compute and return the hash using aggregate
			return terrainInfos.Aggregate(terrainInfos.Length,
				(current, terrainInfo) => unchecked(current * 31 + terrainInfo.Cost));
		}

		public bool CanEnterCell(World world, Actor self, CPos cell, Actor ignoreActor = null, CellConditions check = CellConditions.All)
		{
			if (MovementCostForCell(world, cell) == int.MaxValue)
				return false;

			return CanMoveFreelyInto(world, self, cell, ignoreActor, check);
		}

		// Determines whether the actor is blocked by other Actors
		public bool CanMoveFreelyInto(World world, Actor self, CPos cell, Actor ignoreActor, CellConditions check)
		{
			if (!check.HasCellCondition(CellConditions.TransientActors))
				return true;

			if (SharesCell && world.ActorMap.HasFreeSubCell(cell))
				return true;

			foreach (var otherActor in world.ActorMap.GetUnitsAt(cell))
				if (IsBlockedBy(self, otherActor, ignoreActor, check))
					return false;

			return true;
		}

		bool IsBlockedBy(Actor self, Actor otherActor, Actor ignoreActor, CellConditions check)
		{
			// We are not blocked by the actor we are ignoring.
			if (otherActor == ignoreActor)
				return false;

			// If the check allows: we are not blocked by allied units moving in our direction.
			if (!check.HasCellCondition(CellConditions.BlockedByMovers) &&
				self != null &&
				self.Owner.Stances[otherActor.Owner] == Stance.Ally &&
				IsMovingInMyDirection(self, otherActor))
				return false;

			// If we cannot crush the other actor in our way, we are blocked.
			if (self == null || Crushes == null || Crushes.Count == 0)
				return true;

			// If the other actor in our way cannot be crushed, we are blocked.
			var crushables = otherActor.TraitsImplementing<ICrushable>();
			var lacksCrushability = true;
			foreach (var crushable in crushables)
			{
				lacksCrushability = false;
				if (!crushable.CrushableBy(Crushes, self.Owner))
					return true;
			}

			// If there are no crushable traits at all, this means the other actor cannot be crushed - we are blocked.
			if (lacksCrushability)
				return true;

			// We are not blocked by the other actor.
			return false;
		}

		public WorldMovementInfo GetWorldMovementInfo(World world)
		{
			return new WorldMovementInfo(world, this);
		}

		public int MovementCostToEnterCell(WorldMovementInfo worldMovementInfo, Actor self, CPos cell, Actor ignoreActor = null, CellConditions check = CellConditions.All)
		{
			var cost = MovementCostForCell(worldMovementInfo.World.Map, worldMovementInfo.TerrainInfos, cell);
			if (cost == int.MaxValue || !CanMoveFreelyInto(worldMovementInfo.World, self, cell, ignoreActor, check))
				return int.MaxValue;
			return cost;
		}

		public SubCell GetAvailableSubCell(
			World world, Actor self, CPos cell, SubCell preferredSubCell = SubCell.Any, Actor ignoreActor = null, CellConditions check = CellConditions.All)
		{
			if (MovementCostForCell(world, cell) == int.MaxValue)
				return SubCell.Invalid;

			if (check.HasCellCondition(CellConditions.TransientActors))
			{
				Func<Actor, bool> checkTransient = otherActor => IsBlockedBy(self, otherActor, ignoreActor, check);

				if (!SharesCell)
					return world.ActorMap.AnyUnitsAt(cell, SubCell.FullCell, checkTransient) ? SubCell.Invalid : SubCell.FullCell;

				return world.ActorMap.FreeSubCell(cell, preferredSubCell, checkTransient);
			}

			if (!SharesCell)
				return world.ActorMap.AnyUnitsAt(cell, SubCell.FullCell) ? SubCell.Invalid : SubCell.FullCell;

			return world.ActorMap.FreeSubCell(cell, preferredSubCell);
		}

		public int GetInitialFacing() { return InitialFacing; }

		public IReadOnlyDictionary<CPos, SubCell> OccupiedCells(ActorInfo info, CPos location, SubCell subCell = SubCell.Any)
		{
			return new ReadOnlyDictionary<CPos, SubCell>(new Dictionary<CPos, SubCell>() { { location, subCell } });
		}

		bool IOccupySpaceInfo.SharesCell { get { return SharesCell; } }
	}

	public class Mobile : IIssueOrder, IResolveOrder, IOrderVoice, IPositionable, IMove, IFacing, ISync, INotifyAddedToWorld, INotifyRemovedFromWorld, INotifyBlockingMove
	{
		const int AverageTicksBeforePathing = 5;
		const int SpreadTicksBeforePathing = 5;
		internal int TicksBeforePathing = 0;

		readonly Actor self;
		readonly Lazy<IEnumerable<int>> speedModifiers;
		public readonly MobileInfo Info;
		public bool IsMoving { get; set; }

		int facing;
		CPos fromCell, toCell;
		public SubCell FromSubCell, ToSubCell;

		[Sync] public int Facing
		{
			get { return facing; }
			set { facing = value; }
		}

		public int ROT { get { return Info.ROT; } }

		[Sync] public WPos CenterPosition { get; private set; }
		[Sync] public CPos FromCell { get { return fromCell; } }
		[Sync] public CPos ToCell { get { return toCell; } }

		[Sync] public int PathHash;	// written by Move.EvalPath, to temporarily debug this crap.

		// Sets only the location (fromCell, toCell, FromSubCell, ToSubCell)
		public void SetLocation(CPos from, SubCell fromSub, CPos to, SubCell toSub)
		{
			if (FromCell == from && ToCell == to && FromSubCell == fromSub && ToSubCell == toSub)
				return;

			RemoveInfluence();
			fromCell = from;
			toCell = to;
			FromSubCell = fromSub;
			ToSubCell = toSub;
			AddInfluence();
		}

		public Mobile(ActorInitializer init, MobileInfo info)
		{
			self = init.Self;
			Info = info;

			speedModifiers = Exts.Lazy(() => self.TraitsImplementing<ISpeedModifier>().ToArray().Select(x => x.GetSpeedModifier()));

			ToSubCell = FromSubCell = info.SharesCell ? init.World.Map.DefaultSubCell : SubCell.FullCell;
			if (init.Contains<SubCellInit>())
				FromSubCell = ToSubCell = init.Get<SubCellInit, SubCell>();

			if (init.Contains<LocationInit>())
			{
				fromCell = toCell = init.Get<LocationInit, CPos>();
				SetVisualPosition(self, init.World.Map.CenterOfSubCell(FromCell, FromSubCell));
			}

			this.Facing = init.Contains<FacingInit>() ? init.Get<FacingInit, int>() : info.InitialFacing;

			// Sets the visual position to WPos accuracy
			// Use LocationInit if you want to insert the actor into the ActorMap!
			if (init.Contains<CenterPositionInit>())
				SetVisualPosition(self, init.Get<CenterPositionInit, WPos>());
		}

		// Returns a valid sub-cell
		public SubCell GetValidSubCell(SubCell preferred = SubCell.Any)
		{
			// Try same sub-cell
			if (preferred == SubCell.Any)
				preferred = FromSubCell;

			// Fix sub-cell assignment
			if (Info.SharesCell)
			{
				if (preferred <= SubCell.FullCell)
					return self.World.Map.DefaultSubCell;
			}
			else
			{
				if (preferred != SubCell.FullCell)
					return SubCell.FullCell;
			}

			return preferred;
		}

		// Sets the location (fromCell, toCell, FromSubCell, ToSubCell) and visual position (CenterPosition)
		public void SetPosition(Actor self, CPos cell, SubCell subCell = SubCell.Any)
		{
			subCell = GetValidSubCell(subCell);
			SetLocation(cell, subCell, cell, subCell);
			SetVisualPosition(self, self.World.Map.CenterOfSubCell(cell, subCell));
			FinishedMoving(self);
		}

		// Sets the location (fromCell, toCell, FromSubCell, ToSubCell) and visual position (CenterPosition)
		public void SetPosition(Actor self, WPos pos)
		{
			var cell = self.World.Map.CellContaining(pos);
			SetLocation(cell, FromSubCell, cell, FromSubCell);
			SetVisualPosition(self, self.World.Map.CenterOfSubCell(cell, FromSubCell) + new WVec(0, 0, pos.Z));
			FinishedMoving(self);
		}

		// Sets only the visual position (CenterPosition)
		public void SetVisualPosition(Actor self, WPos pos)
		{
			CenterPosition = pos;
			if (self.IsInWorld)
			{
				self.World.ScreenMap.Update(self);
				self.World.ActorMap.UpdatePosition(self, this);
			}
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

		public IEnumerable<IOrderTargeter> Orders { get { yield return new MoveOrderTargeter(self, Info); } }

		// Note: Returns a valid order even if the unit can't move to the target
		public Order IssueOrder(Actor self, IOrderTargeter order, Target target, bool queued)
		{
			if (order is MoveOrderTargeter)
			{
				if (Info.OnRails)
					return null;

				return new Order("Move", self, queued) { TargetLocation = self.World.Map.CellContaining(target.CenterPosition) };
			}

			return null;
		}

		public CPos NearestMoveableCell(CPos target)
		{
			// Limit search to a radius of 10 tiles
			return NearestMoveableCell(target, 1, 10);
		}

		public CPos NearestMoveableCell(CPos target, int minRange, int maxRange)
		{
			if (CanEnterCell(target))
				return target;

			foreach (var tile in self.World.Map.FindTilesInAnnulus(target, minRange, maxRange))
				if (CanEnterCell(tile))
					return tile;

			// Couldn't find a cell
			return target;
		}

		public CPos NearestCell(CPos target, Func<CPos, bool> check, int minRange, int maxRange)
		{
			if (check(target))
				return target;

			foreach (var tile in self.World.Map.FindTilesInAnnulus(target, minRange, maxRange))
				if (check(tile))
					return tile;

			// Couldn't find a cell
			return target;
		}

		void PerformMoveInner(Actor self, CPos targetLocation, bool queued)
		{
			var currentLocation = NearestMoveableCell(targetLocation);

			if (!CanEnterCell(currentLocation))
			{
				if (queued) self.CancelActivity();
				return;
			}

			if (!queued) self.CancelActivity();

			TicksBeforePathing = AverageTicksBeforePathing + self.World.SharedRandom.Next(-SpreadTicksBeforePathing, SpreadTicksBeforePathing);

			self.QueueActivity(new Move(self, currentLocation, 8));

			self.SetTargetLine(Target.FromCell(self.World, currentLocation), Color.Green);
		}

		protected void PerformMove(Actor self, CPos targetLocation, bool queued)
		{
			if (queued)
				self.QueueActivity(new CallFunc(() => PerformMoveInner(self, targetLocation, true)));
			else
				PerformMoveInner(self, targetLocation, false);
		}

		public void ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString == "Move")
			{
				if (!Info.MoveIntoShroud && !self.Owner.Shroud.IsExplored(order.TargetLocation))
					return;

				PerformMove(self, self.World.Map.Clamp(order.TargetLocation),
					order.Queued && !self.IsIdle);
			}

			if (order.OrderString == "Stop")
				self.CancelActivity();

			if (order.OrderString == "Scatter")
				Nudge(self, self, true);
		}

		public string VoicePhraseForOrder(Actor self, Order order)
		{
			switch (order.OrderString)
			{
				case "Move":
				case "Scatter":
				case "Stop":
					return Info.Voice;
				default:
					return null;
			}
		}

		public CPos TopLeft { get { return ToCell; } }

		public IEnumerable<Pair<CPos, SubCell>> OccupiedCells()
		{
			if (FromCell == ToCell)
				return new[] { Pair.New(FromCell, FromSubCell) };
			if (CanEnterCell(ToCell))
				return new[] { Pair.New(ToCell, ToSubCell) };
			return new[] { Pair.New(FromCell, FromSubCell), Pair.New(ToCell, ToSubCell) };
		}

		public bool IsLeavingCell(CPos location, SubCell subCell = SubCell.Any)
		{
			return ToCell != location && fromCell == location
				&& (subCell == SubCell.Any || FromSubCell == subCell || subCell == SubCell.FullCell || FromSubCell == SubCell.FullCell);
		}

		public SubCell GetAvailableSubCell(CPos a, SubCell preferredSubCell = SubCell.Any, Actor ignoreActor = null, bool checkTransientActors = true)
		{
			return Info.GetAvailableSubCell(self.World, self, a, preferredSubCell, ignoreActor, checkTransientActors ? CellConditions.All : CellConditions.None);
		}

		public bool CanEnterCell(CPos cell, Actor ignoreActor = null, bool checkTransientActors = true)
		{
			return Info.CanEnterCell(self.World, self, cell, ignoreActor, checkTransientActors ? CellConditions.All : CellConditions.BlockedByMovers);
		}

		public bool CanMoveFreelyInto(CPos cell, Actor ignoreActor = null, bool checkTransientActors = true)
		{
			return Info.CanMoveFreelyInto(self.World, self, cell, ignoreActor, checkTransientActors ? CellConditions.All : CellConditions.BlockedByMovers);
		}

		public void EnteringCell(Actor self)
		{
			// Only make actor crush if it is on the ground
			if (self.CenterPosition.Z != 0)
				return;

			var crushables = self.World.ActorMap.GetUnitsAt(ToCell).Where(a => a != self)
				.SelectMany(a => a.TraitsImplementing<ICrushable>().Where(b => b.CrushableBy(Info.Crushes, self.Owner)));
			foreach (var crushable in crushables)
				crushable.WarnCrush(self);
		}

		public void FinishedMoving(Actor self)
		{
			// Only make actor crush if it is on the ground
			if (!self.IsAtGroundLevel())
				return;

			var crushables = self.World.ActorMap.GetUnitsAt(ToCell).Where(a => a != self)
				.SelectMany(a => a.TraitsImplementing<ICrushable>().Where(c => c.CrushableBy(Info.Crushes, self.Owner)));
			foreach (var crushable in crushables)
				crushable.OnCrush(self);
		}

		public int MovementSpeedForCell(Actor self, CPos cell)
		{
			var index = self.World.Map.GetTerrainIndex(cell);
			if (index == byte.MaxValue)
				return 0;

			var terrainSpeed = Info.TilesetTerrainInfo[self.World.TileSet][index].Speed;
			if (terrainSpeed == 0)
				return 0;

			var modifiers = speedModifiers.Value.Append(terrainSpeed);

			return Util.ApplyPercentageModifiers(Info.Speed, modifiers);
		}

		public void AddInfluence()
		{
			if (self.IsInWorld)
				self.World.ActorMap.AddInfluence(self, this);
		}

		public void RemoveInfluence()
		{
			if (self.IsInWorld)
				self.World.ActorMap.RemoveInfluence(self, this);
		}

		public void Nudge(Actor self, Actor nudger, bool force)
		{
			/* initial fairly braindead implementation. */
			if (!force && self.Owner.Stances[nudger.Owner] != Stance.Ally)
				return;		/* don't allow ourselves to be pushed around
							 * by the enemy! */

			if (!force && !self.IsIdle)
				return;		/* don't nudge if we're busy doing something! */

			// pick an adjacent available cell.
			var availCells = new List<CPos>();
			var notStupidCells = new List<CPos>();

			for (var i = -1; i < 2; i++)
				for (var j = -1; j < 2; j++)
				{
					var p = ToCell + new CVec(i, j);
					if (CanEnterCell(p))
						availCells.Add(p);
					else if (p != nudger.Location && p != ToCell)
						notStupidCells.Add(p);
				}

			var moveTo = availCells.Any() ? availCells.Random(self.World.SharedRandom) : (CPos?)null;

			if (moveTo.HasValue)
			{
				self.CancelActivity();
				self.SetTargetLine(Target.FromCell(self.World, moveTo.Value), Color.Green, false);
				self.QueueActivity(new Move(self, moveTo.Value, 0));

				Log.Write("debug", "OnNudge #{0} from {1} to {2}",
					self.ActorID, self.Location, moveTo.Value);
			}
			else
			{
				var cellInfo = notStupidCells
					.SelectMany(c => self.World.ActorMap.GetUnitsAt(c)
						.Where(a => a.IsIdle && a.Info.HasTraitInfo<MobileInfo>()),
						(c, a) => new { Cell = c, Actor = a })
					.RandomOrDefault(self.World.SharedRandom);

				if (cellInfo != null)
				{
					self.CancelActivity();
					var notifyBlocking = new CallFunc(() => self.NotifyBlocker(cellInfo.Cell));
					var waitFor = new WaitFor(() => CanEnterCell(cellInfo.Cell));
					var move = new Move(self, cellInfo.Cell);
					self.QueueActivity(Util.SequenceActivities(notifyBlocking, waitFor, move));

					Log.Write("debug", "OnNudge (notify next blocking actor, wait and move) #{0} from {1} to {2}",
						self.ActorID, self.Location, cellInfo.Cell);
				}
				else
				{
					Log.Write("debug", "OnNudge #{0} refuses at {1}",
						self.ActorID, self.Location);
				}
			}
		}

		class MoveOrderTargeter : IOrderTargeter
		{
			readonly MobileInfo unitType;
			readonly bool rejectMove;
			readonly IDisableMove[] moveDisablers;
			public bool OverrideSelection { get { return false; } }

			public MoveOrderTargeter(Actor self, MobileInfo unitType)
			{
				this.unitType = unitType;
				rejectMove = !self.AcceptsOrder("Move");
				moveDisablers = self.TraitsImplementing<IDisableMove>().ToArray();
			}

			public string OrderID { get { return "Move"; } }
			public int OrderPriority { get { return 4; } }
			public bool IsQueued { get; protected set; }

			public bool CanTarget(Actor self, Target target, List<Actor> othersAtTarget, TargetModifiers modifiers, ref string cursor)
			{
				if (rejectMove || !target.IsValidFor(self))
					return false;

				var location = self.World.Map.CellContaining(target.CenterPosition);
				IsQueued = modifiers.HasModifier(TargetModifiers.ForceQueue);

				var explored = self.Owner.Shroud.IsExplored(location);
				cursor = self.World.Map.Contains(location) ?
					(self.World.Map.GetTerrainInfo(location).CustomCursor ?? unitType.Cursor) : unitType.BlockedCursor;

				if ((!explored && !unitType.MoveIntoShroud)
					|| (explored && unitType.MovementCostForCell(self.World, location) == int.MaxValue)
					|| moveDisablers.Any(d => d.MoveDisabled(self)))
					cursor = unitType.BlockedCursor;

				return true;
			}
		}

		public Activity ScriptedMove(CPos cell) { return new Move(self, cell); }
		public Activity MoveTo(CPos cell, int nearEnough) { return new Move(self, cell, nearEnough); }
		public Activity MoveTo(CPos cell, Actor ignoredActor) { return new Move(self, cell, ignoredActor); }
		public Activity MoveWithinRange(Target target, WDist range) { return new MoveWithinRange(self, target, WDist.Zero, range); }
		public Activity MoveWithinRange(Target target, WDist minRange, WDist maxRange) { return new MoveWithinRange(self, target, minRange, maxRange); }
		public Activity MoveFollow(Actor self, Target target, WDist minRange, WDist maxRange) { return new Follow(self, target, minRange, maxRange); }
		public Activity MoveTo(Func<List<CPos>> pathFunc) { return new Move(self, pathFunc); }

		public void OnNotifyBlockingMove(Actor self, Actor blocking)
		{
			if (self.IsIdle && self.AppearsFriendlyTo(blocking))
				Nudge(self, blocking, true);
		}

		public Activity MoveIntoWorld(Actor self, CPos cell, SubCell subCell = SubCell.Any)
		{
			var pos = self.CenterPosition;

			if (subCell == SubCell.Any)
				subCell = self.World.ActorMap.FreeSubCell(cell, subCell);

			// TODO: solve/reduce cell is full problem
			if (subCell == SubCell.Invalid)
				subCell = self.World.Map.DefaultSubCell;

			// Reserve the exit cell
			SetPosition(self, cell, subCell);
			SetVisualPosition(self, pos);

			return VisualMove(self, pos, self.World.Map.CenterOfSubCell(cell, subCell), cell);
		}

		public Activity MoveToTarget(Actor self, Target target)
		{
			if (target.Type == TargetType.Invalid)
				return null;

			return new MoveAdjacentTo(self, target);
		}

		public Activity MoveIntoTarget(Actor self, Target target)
		{
			if (target.Type == TargetType.Invalid)
				return null;

			return VisualMove(self, self.CenterPosition, target.CenterPosition);
		}

		public bool CanEnterTargetNow(Actor self, Target target)
		{
			return self.Location == self.World.Map.CellContaining(target.CenterPosition) || Util.AdjacentCells(self.World, target).Any(c => c == self.Location);
		}

		public Activity VisualMove(Actor self, WPos fromPos, WPos toPos)
		{
			return VisualMove(self, fromPos, toPos, self.Location);
		}

		public Activity VisualMove(Actor self, WPos fromPos, WPos toPos, CPos cell)
		{
			var speed = MovementSpeedForCell(self, cell);
			var length = speed > 0 ? (toPos - fromPos).Length / speed : 0;

			var facing = Util.GetFacing(toPos - fromPos, Facing);
			return Util.SequenceActivities(new Turn(self, facing), new Drag(self, fromPos, toPos, length));
		}
	}
}
