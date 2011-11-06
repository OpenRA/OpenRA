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
	public class MobileInfo : ITraitInfo, IFacingInfo, UsesInit<FacingInit>, UsesInit<LocationInit>, UsesInit<SubCellInit>
	{
		[FieldLoader.LoadUsing("LoadSpeeds")]
		public readonly Dictionary<string, TerrainInfo> TerrainSpeeds;
		public readonly string[] Crushes;
		public readonly int WaitAverage = 60;
		public readonly int WaitSpread = 20;
		public readonly int InitialFacing = 128;
		public readonly int ROT = 255;
		public readonly int Speed = 1;
		public readonly bool OnRails = false;
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

		public int MovementCostForCell(World world, int2 cell)
		{
			if (!world.Map.IsInMap(cell.X, cell.Y))
				return int.MaxValue;

			var type = world.GetTerrainType(cell);
			if (!TerrainSpeeds.ContainsKey(type))
				return int.MaxValue;

			return TerrainSpeeds[type].Cost;
		}

		public readonly Dictionary<SubCell, int2> SubCellOffsets = new Dictionary<SubCell, int2>()
		{
			{SubCell.TopLeft, new int2(-7,-6)},
			{SubCell.TopRight, new int2(6,-6)},
			{SubCell.Center, new int2(0,0)},
			{SubCell.BottomLeft, new int2(-7,6)},
			{SubCell.BottomRight, new int2(6,6)},
			{SubCell.FullCell, new int2(0,0)},
		};

		public bool CanEnterCell(World world, Player owner, int2 cell, Actor ignoreActor, bool checkTransientActors)
		{
			if (MovementCostForCell(world, cell) == int.MaxValue)
				return false;

			if (SharesCell && world.ActorMap.HasFreeSubCell(cell))
				return true;

			var blockingActors = world.ActorMap.GetUnitsAt(cell).Where(x => x != ignoreActor).ToList();
			if (checkTransientActors && blockingActors.Count > 0)
			{
				// Non-sharable unit can enter a cell with shareable units only if it can crush all of them
				if (Crushes == null)
					return false;

				if (blockingActors.Any(a => !(a.HasTrait<ICrushable>() &&
									         a.TraitsImplementing<ICrushable>().Any(b => b.CrushableBy(Crushes, owner)))))
					return false;
			}

			return true;
		}
	}

	public class Mobile : IIssueOrder, IResolveOrder, IOrderVoice, IOccupySpace, IMove, IFacing, INudge, ISync
	{
		public readonly Actor self;
		public readonly MobileInfo Info;
		public bool IsMoving { get; internal set; }

		int __facing;
		int2 __fromCell, __toCell;
		public SubCell fromSubCell, toSubCell;

		int __altitude;

		[Sync]
		public int Facing
		{
			get { return __facing; }
			set { __facing = value; }
		}

		[Sync]
		public int Altitude
		{
			get { return __altitude; }
			set { __altitude = value; }
		}

		public int ROT { get { return Info.ROT; } }
		public int InitialFacing { get { return Info.InitialFacing; } }

		[Sync]
		public int2 PxPosition { get; set; }
		[Sync]
		public int2 fromCell { get { return __fromCell; } }
		[Sync]
		public int2 toCell { get { return __toCell; } }

		[Sync]
		public int PathHash;	// written by Move.EvalPath, to temporarily debug this crap.

		public void SetLocation(int2 from, SubCell fromSub, int2 to, SubCell toSub)
		{
			if (fromCell == from && toCell == to) return;
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
				this.__fromCell = this.__toCell = init.Get<LocationInit, int2>();
				this.PxPosition = Util.CenterOfCell(fromCell) + info.SubCellOffsets[fromSubCell];
			}

			this.Facing = init.Contains<FacingInit>() ? init.Get<FacingInit, int>() : info.InitialFacing;
			this.Altitude = init.Contains<AltitudeInit>() ? init.Get<AltitudeInit, int>() : 0;
		}

		public void SetPosition(Actor self, int2 cell)
		{
			SetLocation(cell,fromSubCell, cell,fromSubCell);
			PxPosition = Util.CenterOfCell(fromCell) + Info.SubCellOffsets[fromSubCell];
			FinishedMoving(self);
		}

		public void SetPxPosition(Actor self, int2 px)
		{
			var cell = Util.CellContaining(px);
			SetLocation(cell,fromSubCell, cell,fromSubCell);
			PxPosition = px;
			FinishedMoving(self);
		}

		public void AdjustPxPosition(Actor self, int2 px)	/* visual hack only */
		{
			PxPosition = px;
		}

		public IEnumerable<IOrderTargeter> Orders { get { yield return new MoveOrderTargeter(Info); } }

		// Note: Returns a valid order even if the unit can't move to the target
		public Order IssueOrder(Actor self, IOrderTargeter order, Target target, bool queued)
		{
			if (order is MoveOrderTargeter)
			{
				if (Info.OnRails) return null;
				return new Order("Move", self, queued) { TargetLocation = Util.CellContaining(target.CenterLocation) };
			}
			return null;
		}

		public int2 NearestMoveableCell(int2 target)
		{
			if (CanEnterCell(target))
				return target;

			var searched = new List<int2>() { };
			// Limit search to a radius of 10 tiles
			for (int r = 1; r < 10; r++)
				foreach (var tile in self.World.FindTilesInCircle(target, r).Except(searched))
				{
					if (CanEnterCell(tile))
						return tile;

					searched.Add(tile);
				}

			// Couldn't find a cell
			return target;
		}

		void PerformMoveInner(Actor self, int2 targetLocation, bool queued)
		{
			int2 currentLocation = NearestMoveableCell(targetLocation);

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

		protected void PerformMove(Actor self, int2 targetLocation, bool queued)
		{
			if (queued)
				self.QueueActivity(new CallFunc(() => PerformMoveInner(self, targetLocation, queued)));
			else
				PerformMoveInner(self, targetLocation, queued);
		}

		public void ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString == "Move")
				PerformMove(self, self.World.ClampToWorld(order.TargetLocation),
					order.Queued && !self.IsIdle);

			if (order.OrderString == "Stop")
				self.CancelActivity();

			if (order.OrderString == "Scatter")
				OnNudge(self, self, true);
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

		public int2 TopLeft { get { return toCell; } }

		public IEnumerable<Pair<int2, SubCell>> OccupiedCells()
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

		public SubCell GetDesiredSubcell(int2 a, Actor ignoreActor)
		{
			if (!Info.SharesCell)
				return SubCell.FullCell;

			// Prioritise the current subcell
			return new[]{ fromSubCell, SubCell.TopLeft, SubCell.TopRight, SubCell.Center,
				SubCell.BottomLeft, SubCell.BottomRight}.First(b =>
			{
				var blockingActors = self.World.ActorMap.GetUnitsAt(a,b).Where(c => c != ignoreActor);
				if (blockingActors.Count() > 0)
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

		public bool CanEnterCell(int2 p)
		{
			return CanEnterCell(p, null, true);
		}

		public bool CanEnterCell(int2 cell, Actor ignoreActor, bool checkTransientActors)
		{
			return Info.CanEnterCell(self.World, self.Owner, cell, ignoreActor, checkTransientActors);
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

		public int MovementSpeedForCell(Actor self, int2 cell)
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
				self.World.ActorMap.Add(self, this);
		}

		public void RemoveInfluence()
		{
			if (self.IsInWorld)
				self.World.ActorMap.Remove(self, this);
		}

		public void OnNudge(Actor self, Actor nudger, bool force)
		{
			/* initial fairly braindead implementation. */
			if (!force && self.Owner.Stances[nudger.Owner] != Stance.Ally)
				return;		/* don't allow ourselves to be pushed around
							 * by the enemy! */

			if (!force && !self.IsIdle)
				return;		/* don't nudge if we're busy doing something! */

			// pick an adjacent available cell.
			var availCells = new List<int2>();
			var notStupidCells = new List<int2>();

			for (var i = -1; i < 2; i++)
				for (var j = -1; j < 2; j++)
				{
					var p = toCell + new int2(i, j);
					if (CanEnterCell(p))
						availCells.Add(p);
					else
						if (p != nudger.Location && p != toCell)
							notStupidCells.Add(p);
				}

			var moveTo = availCells.Any() ? availCells.Random(self.World.SharedRandom) :
				notStupidCells.Any() ? notStupidCells.Random(self.World.SharedRandom) : (int2?)null;

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

			public MoveOrderTargeter(MobileInfo unitType)
			{
				this.unitType = unitType;
			}

			public string OrderID { get { return "Move"; } }
			public int OrderPriority { get { return 4; } }
			public bool IsQueued { get; protected set; }

			public bool CanTargetActor(Actor self, Actor target, bool forceAttack, bool forceQueued, ref string cursor)
			{
				return false;
			}

			public bool CanTargetLocation(Actor self, int2 location, List<Actor> actorsAtLocation, bool forceAttack, bool forceQueued, ref string cursor)
			{
				IsQueued = forceQueued;
				cursor = "move";
				if (!self.World.Map.IsInMap(location) || (self.World.LocalPlayer.Shroud.IsExplored(location) &&
						unitType.MovementCostForCell(self.World, location) == int.MaxValue))
					cursor = "move-blocked";

				return true;
			}
		}

		public Activity ScriptedMove(int2 cell) { return new Move(cell); }
		public Activity MoveTo(int2 cell, int nearEnough) { return new Move(cell, nearEnough); }
		public Activity MoveTo(int2 cell, Actor ignoredActor) { return new Move(cell, ignoredActor); }
		public Activity MoveWithinRange(Target target, int range) { return new Move(target, range); }
		public Activity MoveTo(Func<List<int2>> pathFunc) { return new Move(pathFunc); }
	}
}
