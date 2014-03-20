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
using System.Drawing;
using System.Linq;
using OpenRA.FileFormats;
using OpenRA.Mods.RA.Activities;
using OpenRA.Traits;

namespace OpenRA.Mods.RA.Move
{
	[Desc("Unit is able to move.")]
	public class MobileInfo : ITraitInfo, IOccupySpaceInfo, IFacingInfo, IMoveInfo, UsesInit<FacingInit>, UsesInit<LocationInit>, UsesInit<SubCellInit>
	{
		[FieldLoader.LoadUsing("LoadSpeeds")]
		[Desc("Set Water: 0 for ground units and lower the value on rough terrain.")]
		public readonly Dictionary<string, TerrainInfo> TerrainSpeeds;
		[Desc("e.g. crate, wall, infantry")]
		public readonly string[] Crushes;
		public readonly int WaitAverage = 5;
		public readonly int WaitSpread = 2;
		public readonly int InitialFacing = 128;
		[Desc("Rate of Turning")]
		public readonly int ROT = 255;
		public readonly int Speed = 1;
		public readonly bool OnRails = false;
		[Desc("Allow multiple (infantry) units in one cell.")]
		public readonly bool SharesCell = false;

		public virtual object Create(ActorInitializer init) { return new Mobile(init, this); }

		static object LoadSpeeds(MiniYaml y)
		{
			Dictionary<string, TerrainInfo> ret = new Dictionary<string, TerrainInfo>();
			foreach (var t in y.NodesDict["TerrainSpeeds"].Nodes)
			{
				var speed = FieldLoader.GetValue<decimal>("speed", t.Value.Value);
				var cost = t.Value.NodesDict.ContainsKey("PathingCost")
					? FieldLoader.GetValue<int>("cost", t.Value.NodesDict["PathingCost"].Value)
					: (int)(10000 / speed);
				ret.Add(t.Key, new TerrainInfo { Speed = speed, Cost = cost });
			}

			return ret;
		}

		public class TerrainInfo
		{
			public int Cost = int.MaxValue;
			public decimal Speed = 0;
		}

		public int MovementCostForCell(World world, CPos cell)
		{
			if (!world.Map.IsInMap(cell.X, cell.Y))
				return int.MaxValue;

			var type = world.GetTerrainType(cell);
			if (!TerrainSpeeds.ContainsKey(type))
				return int.MaxValue;

			return TerrainSpeeds[type].Cost;
		}

		public int GetMovementClass(TileSet tileset)
		{
			/* collect our ability to cross *all* terraintypes, in a bitvector */
			var passability = tileset.Terrain.OrderBy(t => t.Key)
				.Select(t => TerrainSpeeds.ContainsKey(t.Key) && TerrainSpeeds[t.Key].Cost < int.MaxValue);

			return passability.ToBits();
		}

		public static readonly Dictionary<SubCell, WVec> SubCellOffsets = new Dictionary<SubCell, WVec>()
		{
			{SubCell.TopLeft, new WVec(-299, -256, 0)},
			{SubCell.TopRight, new WVec(256, -256, 0)},
			{SubCell.Center, new WVec(0, 0, 0)},
			{SubCell.BottomLeft, new WVec(-299, 256, 0)},
			{SubCell.BottomRight, new WVec(256, 256, 0)},
			{SubCell.FullCell, new WVec(0, 0, 0)},
		};

		static bool IsMovingInMyDirection(Actor self, Actor other)
		{
			if (!other.IsMoving()) return false;
			if (self == null) return true;

			var selfMobile = self.TraitOrDefault<Mobile>();
			if (selfMobile == null) return false;

			var otherMobile = other.TraitOrDefault<Mobile>();
			if (otherMobile == null) return false;

			// Sign of dot-product indicates (roughly) if vectors are facing in same or opposite directions:
			var dp = CVec.Dot((selfMobile.toCell - self.Location), (otherMobile.toCell - other.Location));
			if (dp <= 0) return false;

			return true;
		}

		public bool CanEnterCell(World world, CPos cell)
		{
			return CanEnterCell(world, null, cell, null, true, true);
		}

		public bool CanEnterCell(World world, Actor self, CPos cell, Actor ignoreActor, bool checkTransientActors, bool blockedByMovers)
		{
			if (MovementCostForCell(world, cell) == int.MaxValue)
				return false;

			if (SharesCell && world.ActorMap.HasFreeSubCell(cell))
				return true;

			var blockingActors = world.ActorMap.GetUnitsAt(cell)
				.Where(x => x != ignoreActor)
				// Neutral/enemy units are blockers. Allied units that are moving are not blockers.
				.Where(x => blockedByMovers || (self == null || self.Owner.Stances[x.Owner] != Stance.Ally || !IsMovingInMyDirection(self, x)))
				.ToList();

			if (checkTransientActors && blockingActors.Count > 0)
			{
				// Non-sharable unit can enter a cell with shareable units only if it can crush all of them
				if (self == null || Crushes == null)
					return false;

				if (blockingActors.Any(a => !(a.HasTrait<ICrushable>() &&
											 a.TraitsImplementing<ICrushable>().Any(b => b.CrushableBy(Crushes, self.Owner)))))
					return false;
			}

			return true;
		}

		public int GetInitialFacing() { return InitialFacing; }
	}

	public class Mobile : IIssueOrder, IResolveOrder, IOrderVoice, IPositionable, IMove, IFacing, ISync, INotifyAddedToWorld, INotifyRemovedFromWorld, INotifyBlockingMove
	{
		public readonly Actor self;
		public readonly MobileInfo Info;
		public bool IsMoving { get; set; }

		int __facing;
		CPos __fromCell, __toCell;
		public SubCell fromSubCell, toSubCell;

		//int __altitude;

		[Sync] public int Facing
		{
			get { return __facing; }
			set { __facing = value; }
		}

		public int ROT { get { return Info.ROT; } }

		[Sync] public WPos CenterPosition { get; private set; }
		[Sync] public CPos fromCell { get { return __fromCell; } }
		[Sync] public CPos toCell { get { return __toCell; } }

		[Sync] public int PathHash;	// written by Move.EvalPath, to temporarily debug this crap.

		public void SetLocation(CPos from, SubCell fromSub, CPos to, SubCell toSub)
		{
			if (fromCell == from && toCell == to && fromSubCell == fromSub && toSubCell == toSub)
				return;

			RemoveInfluence();
			__fromCell = from;
			__toCell = to;
			fromSubCell = fromSub;
			toSubCell = toSub;
			AddInfluence();
		}

		const int avgTicksBeforePathing = 5;
		const int spreadTicksBeforePathing = 5;
		internal int ticksBeforePathing = 0;

		public Mobile(ActorInitializer init, MobileInfo info)
		{
			this.self = init.self;
			this.Info = info;

			toSubCell = fromSubCell = info.SharesCell ? SubCell.Center : SubCell.FullCell;
			if (init.Contains<SubCellInit>())
			{
				this.fromSubCell = this.toSubCell = init.Get<SubCellInit, SubCell>();
			}

			if (init.Contains<LocationInit>())
			{
				this.__fromCell = this.__toCell = init.Get<LocationInit, CPos>();
				SetVisualPosition(self, fromCell.CenterPosition + MobileInfo.SubCellOffsets[fromSubCell]);
			}

			this.Facing = init.Contains<FacingInit>() ? init.Get<FacingInit, int>() : info.InitialFacing;

			// Sets the visual position to WPos accuracy
			// Use LocationInit if you want to insert the actor into the ActorMap!
			if (init.Contains<CenterPositionInit>())
				SetVisualPosition(self, init.Get<CenterPositionInit, WPos>());
		}

		public void SetPosition(Actor self, CPos cell)
		{
			SetLocation(cell,fromSubCell, cell,fromSubCell);
			SetVisualPosition(self, fromCell.CenterPosition + MobileInfo.SubCellOffsets[fromSubCell]);
			FinishedMoving(self);
		}

		public void SetPosition(Actor self, WPos pos)
		{
			var cell = pos.ToCPos();
			SetLocation(cell,fromSubCell, cell,fromSubCell);
			SetVisualPosition(self, pos);
			FinishedMoving(self);
		}

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

				return new Order("Move", self, queued) { TargetLocation = target.CenterPosition.ToCPos() };
			}
			return null;
		}

		public CPos NearestMoveableCell(CPos target)
		{
			return NearestMoveableCell(target, 1, 10);
		}

		public CPos NearestMoveableCell(CPos target, int minRange, int maxRange)
		{
			if (CanEnterCell(target))
				return target;

			var searched = new List<CPos>();
			// Limit search to a radius of 10 tiles
			for (int r = minRange; r < maxRange; r++)
				foreach (var tile in self.World.FindTilesInCircle(target, r).Except(searched))
				{
					if (CanEnterCell(tile))
						return tile;

					searched.Add(tile);
				}

			// Couldn't find a cell
			return target;
		}

		public CPos NearestCell(CPos target, Func<CPos, bool> check, int minRange, int maxRange)
		{
			if (check(target))
				return target;

			var searched = new List<CPos>();
			for (int r = minRange; r < maxRange; r++)
				foreach (var tile in self.World.FindTilesInCircle(target, r).Except(searched))
				{
					if (check(tile))
						return tile;

					searched.Add(tile);
				}

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

			ticksBeforePathing = avgTicksBeforePathing + self.World.SharedRandom.Next(-spreadTicksBeforePathing, spreadTicksBeforePathing);

			self.QueueActivity(new Move(currentLocation, 8));

			self.SetTargetLine(Target.FromCell(currentLocation), Color.Green);
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
				PerformMove(self, self.World.ClampToWorld(order.TargetLocation),
					order.Queued && !self.IsIdle);

			if (order.OrderString == "Stop")
				self.CancelActivity();

			if (order.OrderString == "Scatter")
				Nudge(self, self, true);
		}

		public string VoicePhraseForOrder(Actor self, Order order)
		{
			switch( order.OrderString )
			{
			case "Move":
			case "Scatter":
			case "Stop":
				return "Move";
			default:
				return null;
			}
		}

		public CPos TopLeft { get { return toCell; } }

		public IEnumerable<Pair<CPos, SubCell>> OccupiedCells()
		{
			if (fromCell == toCell)
				yield return Pair.New(fromCell, fromSubCell);
			else if (CanEnterCell(toCell))
				yield return Pair.New(toCell, toSubCell);
			else
			{
				yield return Pair.New(fromCell, fromSubCell);
				yield return Pair.New(toCell, toSubCell);
			}
		}

		public SubCell GetDesiredSubcell(CPos a, Actor ignoreActor)
		{
			if (!Info.SharesCell)
				return SubCell.FullCell;

			// Prioritise the current subcell
			return new[]{ fromSubCell, SubCell.TopLeft, SubCell.TopRight, SubCell.Center,
				SubCell.BottomLeft, SubCell.BottomRight}.First(b =>
			{
				var blockingActors = self.World.ActorMap.GetUnitsAt(a,b).Where(c => c != ignoreActor);
				if (blockingActors.Any())
				{
					// Non-sharable unit can enter a cell with shareable units only if it can crush all of them
					if (Info.Crushes == null)
						return false;

					if (blockingActors.Any(c => !(c.HasTrait<ICrushable>() &&
												  c.TraitsImplementing<ICrushable>().Any(d => d.CrushableBy(Info.Crushes, self.Owner)))))
						return false;
				}
				return true;
			});
		}

		public bool CanEnterCell(CPos p)
		{
			return CanEnterCell(p, null, true);
		}

		public bool CanEnterCell(CPos cell, Actor ignoreActor, bool checkTransientActors)
		{
			return Info.CanEnterCell(self.World, self, cell, ignoreActor, checkTransientActors, true);
		}

		public void EnteringCell(Actor self)
		{
			var crushable = self.World.ActorMap.GetUnitsAt(toCell).Where(a => a != self && a.HasTrait<ICrushable>());
			foreach (var a in crushable)
			{
				var crushActions = a.TraitsImplementing<ICrushable>().Where(b => b.CrushableBy(Info.Crushes, self.Owner));
				foreach (var b in crushActions)
					b.WarnCrush(self);
			}
		}

		public void FinishedMoving(Actor self)
		{
			var crushable = self.World.ActorMap.GetUnitsAt(toCell).Where(a => a != self && a.HasTrait<ICrushable>());
			foreach (var a in crushable)
			{
				var crushActions = a.TraitsImplementing<ICrushable>().Where(b => b.CrushableBy(Info.Crushes, self.Owner));
				foreach (var b in crushActions)
					b.OnCrush(self);
			}
		}

		public int MovementSpeedForCell(Actor self, CPos cell)
		{
			var type = self.World.GetTerrainType(cell);

			if (!Info.TerrainSpeeds.ContainsKey(type))
				return 0;

			decimal speed = Info.Speed * Info.TerrainSpeeds[type].Speed;
			foreach (var t in self.TraitsImplementing<ISpeedModifier>())
				speed *= t.GetSpeedModifier();
			return (int)(speed / 100);
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
					var p = toCell + new CVec(i, j);
					if (CanEnterCell(p))
						availCells.Add(p);
					else
						if (p != nudger.Location && p != toCell)
							notStupidCells.Add(p);
				}

			var moveTo = availCells.Any() ? availCells.Random(self.World.SharedRandom) :
				notStupidCells.Any() ? notStupidCells.Random(self.World.SharedRandom) : (CPos?)null;

			if (moveTo.HasValue)
			{
				self.CancelActivity();
				self.SetTargetLine(Target.FromCell(moveTo.Value), Color.Green, false);
				self.QueueActivity(new Move(moveTo.Value, 0));

				Log.Write("debug", "OnNudge #{0} from {1} to {2}",
					self.ActorID, self.Location, moveTo.Value);
			}
			else
				Log.Write("debug", "OnNudge #{0} refuses at {1}",
					self.ActorID, self.Location);
		}

		class MoveOrderTargeter : IOrderTargeter
		{
			readonly MobileInfo unitType;
			readonly bool rejectMove;

			public MoveOrderTargeter(Actor self, MobileInfo unitType)
			{
				this.unitType = unitType;
				this.rejectMove = !self.AcceptsOrder("Move");
			}

			public string OrderID { get { return "Move"; } }
			public int OrderPriority { get { return 4; } }
			public bool IsQueued { get; protected set; }

			public bool CanTarget(Actor self, Target target, List<Actor> othersAtTarget, TargetModifiers modifiers, ref string cursor)
			{
				if (rejectMove || !target.IsValidFor(self))
					return false;

				var location = target.CenterPosition.ToCPos();
				IsQueued = modifiers.HasModifier(TargetModifiers.ForceQueue);
				cursor = "move";

				if (self.Owner.Shroud.IsExplored(location))
					cursor = self.World.GetTerrainInfo(location).CustomCursor ?? cursor;

				if (!self.World.Map.IsInMap(location) || (self.Owner.Shroud.IsExplored(location) &&
						unitType.MovementCostForCell(self.World, location) == int.MaxValue))
					cursor = "move-blocked";

				return true;
			}
		}

		public Activity ScriptedMove(CPos cell) { return new Move(cell); }
		public Activity MoveTo(CPos cell, int nearEnough) { return new Move(cell, nearEnough); }
		public Activity MoveTo(CPos cell, Actor ignoredActor) { return new Move(cell, ignoredActor); }
		public Activity MoveWithinRange(Target target, WRange range) { return new Move(target, range); }
		public Activity MoveFollow(Actor self, Target target, WRange range) { return new Follow(self, target, range); }
		public Activity MoveTo(Func<List<CPos>> pathFunc) { return new Move(pathFunc); }

		public void OnNotifyBlockingMove(Actor self, Actor blocking)
		{
			if (self.IsIdle && self.AppearsFriendlyTo(blocking))
				Nudge(self, blocking, true);
		}

		public Activity MoveIntoWorld(Actor self, CPos cell)
		{
			var pos = self.CenterPosition;

			// Reserve the exit cell
			SetPosition(self, cell);
			SetVisualPosition(self, pos);

			// Animate transition
			var to = cell.CenterPosition;
			var speed = MovementSpeedForCell(self, cell);
			var length = speed > 0 ? (to - pos).Length / speed : 0;

			var facing = Util.GetFacing(to - pos, Facing);
			return Util.SequenceActivities(new Turn(facing), new Drag(pos, to, length));
		}

		public Activity VisualMove(Actor self, WPos fromPos, WPos toPos)
		{
			var speed = MovementSpeedForCell(self, self.Location);
			var length = speed > 0 ? (toPos - fromPos).Length / speed : 0;

			var facing = Util.GetFacing(toPos - fromPos, Facing);
			return Util.SequenceActivities(new Turn(facing), new Drag(fromPos, toPos, length));
		}
    }
}
