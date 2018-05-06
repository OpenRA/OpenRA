#region Copyright & License Information
/*
 * Copyright 2007-2018 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using OpenRA.Activities;
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Effects;
using OpenRA.Mods.Common.Pathfinder;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Unit is able to move.")]
	public class MobileInfo : ConditionalTraitInfo, IMoveInfo, IPositionableInfo, IFacingInfo,
		UsesInit<FacingInit>, UsesInit<LocationInit>, UsesInit<SubCellInit>, IActorPreviewInitInfo
	{
		[Desc("Which Locomotor does this trait use. Must be defined on the World actor.")]
		[LocomotorReference, FieldLoader.Require]
		public readonly string Locomotor = null;

		public readonly int InitialFacing = 0;

		[Desc("Speed at which the actor turns.")]
		public readonly int TurnSpeed = 255;

		public readonly int Speed = 1;

		public readonly string Cursor = "move";
		public readonly string BlockedCursor = "move-blocked";

		[VoiceReference] public readonly string Voice = "Action";

		[Desc("Facing to use for actor previews (map editor, color picker, etc)")]
		public readonly int PreviewFacing = 92;

		IEnumerable<object> IActorPreviewInitInfo.ActorPreviewInits(ActorInfo ai, ActorPreviewType type)
		{
			yield return new FacingInit(PreviewFacing);
		}

		public override object Create(ActorInitializer init) { return new Mobile(init, this); }

		public LocomotorInfo LocomotorInfo { get; private set; }

		public override void RulesetLoaded(Ruleset rules, ActorInfo ai)
		{
			var locomotorInfos = rules.Actors["world"].TraitInfos<LocomotorInfo>();
			LocomotorInfo = locomotorInfos.FirstOrDefault(li => li.Name == Locomotor);
			if (LocomotorInfo == null)
				throw new YamlException("A locomotor named '{0}' doesn't exist.".F(Locomotor));
			else if (locomotorInfos.Count(li => li.Name == Locomotor) > 1)
				throw new YamlException("There is more than one locomotor named '{0}'.".F(Locomotor));

			base.RulesetLoaded(rules, ai);
		}

		public int GetInitialFacing() { return InitialFacing; }

		public bool CanEnterCell(World world, Actor self, CPos cell, Actor ignoreActor = null, bool checkTransientActors = true)
		{
			if (LocomotorInfo.MovementCostForCell(world, cell) == int.MaxValue)
				return false;

			var check = checkTransientActors ? CellConditions.All : CellConditions.BlockedByMovers;
			return LocomotorInfo.CanMoveFreelyInto(world, self, cell, ignoreActor, check);
		}

		public IReadOnlyDictionary<CPos, SubCell> OccupiedCells(ActorInfo info, CPos location, SubCell subCell = SubCell.Any)
		{
			return new ReadOnlyDictionary<CPos, SubCell>(new Dictionary<CPos, SubCell>() { { location, subCell } });
		}

		bool IOccupySpaceInfo.SharesCell { get { return LocomotorInfo.SharesCell; } }
	}

	public class Mobile : ConditionalTrait<MobileInfo>, INotifyCreated, IIssueOrder, IResolveOrder, IOrderVoice, IPositionable, IMove,
		IFacing, IDeathActorInitModifier, INotifyAddedToWorld, INotifyRemovedFromWorld, INotifyBlockingMove, IActorPreviewInitModifier, INotifyBecomingIdle
	{
		const int AverageTicksBeforePathing = 5;
		const int SpreadTicksBeforePathing = 5;
		internal int TicksBeforePathing = 0;

		readonly Actor self;
		readonly Lazy<IEnumerable<int>> speedModifiers;

		#region IMove IsMoving checks
		public bool IsMoving { get; set; }
		public bool IsMovingVertically { get { return false; } set { } }
		#endregion

		int facing;
		CPos fromCell, toCell;
		public SubCell FromSubCell, ToSubCell;
		INotifyCustomLayerChanged[] notifyCustomLayerChanged;
		INotifyVisualPositionChanged[] notifyVisualPositionChanged;
		INotifyFinishedMoving[] notifyFinishedMoving;

		#region IFacing
		[Sync] public int Facing
		{
			get { return facing; }
			set { facing = value; }
		}

		public int TurnSpeed { get { return Info.TurnSpeed; } }
		#endregion

		[Sync] public CPos FromCell { get { return fromCell; } }
		[Sync] public CPos ToCell { get { return toCell; } }

		[Sync] public int PathHash;	// written by Move.EvalPath, to temporarily debug this crap.

		#region IOccupySpace
		[Sync] public WPos CenterPosition { get; private set; }
		public CPos TopLeft { get { return ToCell; } }

		public Pair<CPos, SubCell>[] OccupiedCells()
		{
			if (FromCell == ToCell)
				return new[] { Pair.New(FromCell, FromSubCell) };
			if (CanEnterCell(ToCell))
				return new[] { Pair.New(ToCell, ToSubCell) };

			return new[] { Pair.New(FromCell, FromSubCell), Pair.New(ToCell, ToSubCell) };
		}
		#endregion

		public Mobile(ActorInitializer init, MobileInfo info)
			: base(info)
		{
			self = init.Self;

			speedModifiers = Exts.Lazy(() => self.TraitsImplementing<ISpeedModifier>().ToArray().Select(x => x.GetSpeedModifier()));

			ToSubCell = FromSubCell = info.LocomotorInfo.SharesCell ? init.World.Map.Grid.DefaultSubCell : SubCell.FullCell;
			if (init.Contains<SubCellInit>())
				FromSubCell = ToSubCell = init.Get<SubCellInit, SubCell>();

			if (init.Contains<LocationInit>())
			{
				fromCell = toCell = init.Get<LocationInit, CPos>();
				SetVisualPosition(self, init.World.Map.CenterOfSubCell(FromCell, FromSubCell));
			}

			Facing = init.Contains<FacingInit>() ? init.Get<FacingInit, int>() : info.InitialFacing;

			// Sets the visual position to WPos accuracy
			// Use LocationInit if you want to insert the actor into the ActorMap!
			if (init.Contains<CenterPositionInit>())
				SetVisualPosition(self, init.Get<CenterPositionInit, WPos>());
		}

		protected override void Created(Actor self)
		{
			notifyCustomLayerChanged = self.TraitsImplementing<INotifyCustomLayerChanged>().ToArray();
			notifyVisualPositionChanged = self.TraitsImplementing<INotifyVisualPositionChanged>().ToArray();
			notifyFinishedMoving = self.TraitsImplementing<INotifyFinishedMoving>().ToArray();

			base.Created(self);
		}

		void INotifyAddedToWorld.AddedToWorld(Actor self)
		{
			self.World.AddToMaps(self, this);
		}

		void INotifyRemovedFromWorld.RemovedFromWorld(Actor self)
		{
			self.World.RemoveFromMaps(self, this);
		}

		#region Local misc stuff

		public void Nudge(Actor self, Actor nudger, bool force)
		{
			if (IsTraitDisabled)
				return;

			// Initial fairly braindead implementation.
			// don't allow ourselves to be pushed around by the enemy!
			if (!force && self.Owner.Stances[nudger.Owner] != Stance.Ally)
				return;

			// Don't nudge if we're busy doing something!
			if (!force && !self.IsIdle)
				return;

			// Pick an adjacent available cell.
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
				self.QueueActivity(new Move(self, moveTo.Value, WDist.Zero));

				Log.Write("debug", "OnNudge #{0} from {1} to {2}",
					self.ActorID, self.Location, moveTo.Value);
			}
			else
			{
				var cellInfo = notStupidCells
					.SelectMany(c => self.World.ActorMap.GetActorsAt(c)
						.Where(a => a.IsIdle && a.Info.HasTraitInfo<MobileInfo>()),
						(c, a) => new { Cell = c, Actor = a })
					.RandomOrDefault(self.World.SharedRandom);

				if (cellInfo != null)
				{
					self.CancelActivity();
					var notifyBlocking = new CallFunc(() => self.NotifyBlocker(cellInfo.Cell));
					var waitFor = new WaitFor(() => CanEnterCell(cellInfo.Cell));
					var move = new Move(self, cellInfo.Cell);
					self.QueueActivity(ActivityUtils.SequenceActivities(notifyBlocking, waitFor, move));

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

		public bool CanInteractWithGroundLayer(Actor self)
		{
			// TODO: Think about extending this to support arbitrary layer-layer checks
			// in a way that is compatible with the other IMove types.
			// This would then allow us to e.g. have units attack other units inside tunnels.
			if (ToCell.Layer == 0)
				return true;

			ICustomMovementLayer layer;
			if (self.World.GetCustomMovementLayers().TryGetValue(ToCell.Layer, out layer))
				return layer.InteractsWithDefaultLayer;

			return true;
		}

		#endregion

		#region IPositionable

		// Returns a valid sub-cell
		public SubCell GetValidSubCell(SubCell preferred = SubCell.Any)
		{
			// Try same sub-cell
			if (preferred == SubCell.Any)
				preferred = FromSubCell;

			// Fix sub-cell assignment
			if (Info.LocomotorInfo.SharesCell)
			{
				if (preferred <= SubCell.FullCell)
					return self.World.Map.Grid.DefaultSubCell;
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

			var position = cell.Layer == 0 ? self.World.Map.CenterOfCell(cell) :
				self.World.GetCustomMovementLayers()[cell.Layer].CenterOfCell(cell);

			var subcellOffset = self.World.Map.Grid.OffsetOfSubCell(subCell);
			SetVisualPosition(self, position + subcellOffset);
			FinishedMoving(self);
		}

		// Sets the location (fromCell, toCell, FromSubCell, ToSubCell) and visual position (CenterPosition)
		public void SetPosition(Actor self, WPos pos)
		{
			var cell = self.World.Map.CellContaining(pos);
			SetLocation(cell, FromSubCell, cell, FromSubCell);
			SetVisualPosition(self, self.World.Map.CenterOfSubCell(cell, FromSubCell) + new WVec(0, 0, self.World.Map.DistanceAboveTerrain(pos).Length));
			FinishedMoving(self);
		}

		// Sets only the visual position (CenterPosition)
		public void SetVisualPosition(Actor self, WPos pos)
		{
			CenterPosition = pos;
			self.World.UpdateMaps(self, this);

			// The first time SetVisualPosition is called is in the constructor before creation, so we need a null check here as well
			if (notifyVisualPositionChanged == null)
				return;

			foreach (var n in notifyVisualPositionChanged)
				n.VisualPositionChanged(self, fromCell.Layer, toCell.Layer);
		}

		public bool IsLeavingCell(CPos location, SubCell subCell = SubCell.Any)
		{
			return ToCell != location && fromCell == location
				&& (subCell == SubCell.Any || FromSubCell == subCell || subCell == SubCell.FullCell || FromSubCell == SubCell.FullCell);
		}

		public SubCell GetAvailableSubCell(CPos a, SubCell preferredSubCell = SubCell.Any, Actor ignoreActor = null, bool checkTransientActors = true)
		{
			var cellConditions = checkTransientActors ? CellConditions.All : CellConditions.None;
			return Info.LocomotorInfo.GetAvailableSubCell(self.World, self, a, preferredSubCell, ignoreActor, cellConditions);
		}

		public bool CanExistInCell(CPos cell)
		{
			return Info.LocomotorInfo.MovementCostForCell(self.World, cell) != int.MaxValue;
		}

		public bool CanEnterCell(CPos cell, Actor ignoreActor = null, bool checkTransientActors = true)
		{
			return Info.CanEnterCell(self.World, self, cell, ignoreActor, checkTransientActors);
		}

		#endregion

		#region Local IPositionable-related

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

			// Most custom layer conditions are added/removed when starting the transition between layers.
			if (toCell.Layer != fromCell.Layer)
				foreach (var n in notifyCustomLayerChanged)
					n.CustomLayerChanged(self, fromCell.Layer, toCell.Layer);
		}

		public void FinishedMoving(Actor self)
		{
			// Need to check both fromCell and toCell because FinishedMoving is called multiple times during the move
			if (fromCell.Layer == toCell.Layer)
				foreach (var n in notifyFinishedMoving)
					n.FinishedMoving(self, fromCell.Layer, toCell.Layer);

			// Only make actor crush if it is on the ground
			if (!self.IsAtGroundLevel())
				return;

			var actors = self.World.ActorMap.GetActorsAt(ToCell).Where(a => a != self).ToList();
			if (!AnyCrushables(actors))
				return;

			var notifiers = actors.SelectMany(a => a.TraitsImplementing<INotifyCrushed>().Select(t => new TraitPair<INotifyCrushed>(a, t)));
			foreach (var notifyCrushed in notifiers)
				notifyCrushed.Trait.OnCrush(notifyCrushed.Actor, self, Info.LocomotorInfo.Crushes);
		}

		bool AnyCrushables(List<Actor> actors)
		{
			var crushables = actors.SelectMany(a => a.TraitsImplementing<ICrushable>().Select(t => new TraitPair<ICrushable>(a, t))).ToList();
			if (crushables.Count == 0)
				return false;

			foreach (var crushes in crushables)
				if (crushes.Trait.CrushableBy(crushes.Actor, self, Info.LocomotorInfo.Crushes))
					return true;

			return false;
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

		#endregion

		#region IMove

		public Activity MoveTo(CPos cell, int nearEnough) { return new Move(self, cell, WDist.FromCells(nearEnough)); }
		public Activity MoveTo(CPos cell, Actor ignoreActor) { return new Move(self, cell, WDist.Zero, ignoreActor); }
		public Activity MoveWithinRange(Target target, WDist range) { return new MoveWithinRange(self, target, WDist.Zero, range); }
		public Activity MoveWithinRange(Target target, WDist minRange, WDist maxRange) { return new MoveWithinRange(self, target, minRange, maxRange); }
		public Activity MoveFollow(Actor self, Target target, WDist minRange, WDist maxRange) { return new Follow(self, target, minRange, maxRange); }

		public Activity MoveIntoWorld(Actor self, CPos cell, SubCell subCell = SubCell.Any)
		{
			var pos = self.CenterPosition;

			if (subCell == SubCell.Any)
				subCell = Info.LocomotorInfo.SharesCell ? self.World.ActorMap.FreeSubCell(cell, subCell) : SubCell.FullCell;

			// TODO: solve/reduce cell is full problem
			if (subCell == SubCell.Invalid)
				subCell = self.World.Map.Grid.DefaultSubCell;

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

			return VisualMove(self, self.CenterPosition, target.Positions.PositionClosestTo(self.CenterPosition));
		}

		public Activity VisualMove(Actor self, WPos fromPos, WPos toPos)
		{
			return VisualMove(self, fromPos, toPos, self.Location);
		}

		public CPos NearestMoveableCell(CPos target)
		{
			// Limit search to a radius of 10 tiles
			return NearestMoveableCell(target, 1, 10);
		}

		public bool CanEnterTargetNow(Actor self, Target target)
		{
			return self.Location == self.World.Map.CellContaining(target.CenterPosition) || Util.AdjacentCells(self.World, target).Any(c => c == self.Location);
		}

		#endregion

		#region Local IMove-related

		public int MovementSpeedForCell(Actor self, CPos cell)
		{
			var index = cell.Layer == 0 ? self.World.Map.GetTerrainIndex(cell) :
				self.World.GetCustomMovementLayers()[cell.Layer].GetTerrainIndex(cell);

			if (index == byte.MaxValue)
				return 0;

			var terrainSpeed = Info.LocomotorInfo.TilesetTerrainInfo[self.World.Map.Rules.TileSet][index].Speed;
			if (terrainSpeed == 0)
				return 0;

			var modifiers = speedModifiers.Value.Append(terrainSpeed);

			return Util.ApplyPercentageModifiers(Info.Speed, modifiers);
		}

		public CPos NearestMoveableCell(CPos target, int minRange, int maxRange)
		{
			// HACK: This entire method is a hack, and needs to be replaced with
			// a proper path search that can account for movement layer transitions.
			// HACK: Work around code that blindly tries to move to cells in invalid movement layers.
			// This will need to change (by removing this method completely as above) before we can
			// properly support user-issued orders on to elevated bridges or other interactable custom layers
			if (target.Layer != 0)
				target = new CPos(target.X, target.Y);

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

		public bool CanMoveFreelyInto(CPos cell, Actor ignoreActor = null, bool checkTransientActors = true)
		{
			return Info.LocomotorInfo.CanMoveFreelyInto(self.World, self, cell, ignoreActor, checkTransientActors ? CellConditions.All : CellConditions.BlockedByMovers);
		}

		public void EnteringCell(Actor self)
		{
			// Only make actor crush if it is on the ground
			if (!self.IsAtGroundLevel())
				return;

			var actors = self.World.ActorMap.GetActorsAt(ToCell).Where(a => a != self).ToList();
			if (!AnyCrushables(actors))
				return;

			var notifiers = actors.SelectMany(a => a.TraitsImplementing<INotifyCrushed>().Select(t => new TraitPair<INotifyCrushed>(a, t)));
			foreach (var notifyCrushed in notifiers)
				notifyCrushed.Trait.WarnCrush(notifyCrushed.Actor, self, Info.LocomotorInfo.Crushes);
		}

		public Activity ScriptedMove(CPos cell) { return new Move(self, cell); }
		public Activity MoveTo(Func<List<CPos>> pathFunc) { return new Move(self, pathFunc); }

		Activity VisualMove(Actor self, WPos fromPos, WPos toPos, CPos cell)
		{
			var speed = MovementSpeedForCell(self, cell);
			var length = speed > 0 ? (toPos - fromPos).Length / speed : 0;

			var delta = toPos - fromPos;
			var facing = delta.HorizontalLengthSquared != 0 ? delta.Yaw.Facing : Facing;
			return ActivityUtils.SequenceActivities(new Turn(self, facing), new Drag(self, fromPos, toPos, length));
		}

		CPos? ClosestGroundCell()
		{
			var above = new CPos(TopLeft.X, TopLeft.Y);
			if (CanEnterCell(above))
				return above;

			var pathFinder = self.World.WorldActor.Trait<IPathFinder>();
			List<CPos> path;
			using (var search = PathSearch.Search(self.World, Info.LocomotorInfo, self, true,
					loc => loc.Layer == 0 && CanEnterCell(loc))
				.FromPoint(self.Location))
				path = pathFinder.FindPath(search);

			if (path.Count > 0)
				return path[0];

			return null;
		}

		#endregion

		void IActorPreviewInitModifier.ModifyActorPreviewInit(Actor self, TypeDictionary inits)
		{
			if (!inits.Contains<DynamicFacingInit>() && !inits.Contains<FacingInit>())
				inits.Add(new DynamicFacingInit(() => facing));
		}

		void IDeathActorInitModifier.ModifyDeathActorInit(Actor self, TypeDictionary init)
		{
			init.Add(new FacingInit(facing));

			// Allows the husk to drag to its final position
			if (CanEnterCell(self.Location, self, false))
				init.Add(new HuskSpeedInit(MovementSpeedForCell(self, self.Location)));
		}

		void INotifyBecomingIdle.OnBecomingIdle(Actor self)
		{
			if (TopLeft.Layer == 0)
				return;

			var moveTo = ClosestGroundCell();
			if (moveTo != null)
				self.QueueActivity(MoveTo(moveTo.Value, 0));
		}

		void INotifyBlockingMove.OnNotifyBlockingMove(Actor self, Actor blocking)
		{
			if (self.IsIdle && self.AppearsFriendlyTo(blocking))
				Nudge(self, blocking, true);
		}

		IEnumerable<IOrderTargeter> IIssueOrder.Orders { get { yield return new MoveOrderTargeter(self, this); } }

		// Note: Returns a valid order even if the unit can't move to the target
		Order IIssueOrder.IssueOrder(Actor self, IOrderTargeter order, Target target, bool queued)
		{
			if (order is MoveOrderTargeter)
				return new Order("Move", self, target, queued);

			return null;
		}

		void IResolveOrder.ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString == "Move")
			{
				var loc = self.World.Map.Clamp(order.TargetLocation);

				if (!Info.LocomotorInfo.MoveIntoShroud && !self.Owner.Shroud.IsExplored(loc))
					return;

				if (!order.Queued)
					self.CancelActivity();

				TicksBeforePathing = AverageTicksBeforePathing + self.World.SharedRandom.Next(-SpreadTicksBeforePathing, SpreadTicksBeforePathing);

				self.SetTargetLine(Target.FromCell(self.World, loc), Color.Green);
				self.QueueActivity(order.Queued, new Move(self, loc, WDist.FromCells(8), null, true));
			}

			if (order.OrderString == "Stop")
				self.CancelActivity();

			if (order.OrderString == "Scatter")
				Nudge(self, self, true);
		}

		string IOrderVoice.VoicePhraseForOrder(Actor self, Order order)
		{
			if (!Info.LocomotorInfo.MoveIntoShroud && !self.Owner.Shroud.IsExplored(order.TargetLocation))
				return null;

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

		class MoveOrderTargeter : IOrderTargeter
		{
			readonly Mobile mobile;
			readonly LocomotorInfo locomotorInfo;
			readonly bool rejectMove;
			public bool TargetOverridesSelection(TargetModifiers modifiers)
			{
				return modifiers.HasModifier(TargetModifiers.ForceMove);
			}

			public MoveOrderTargeter(Actor self, Mobile unit)
			{
				mobile = unit;
				locomotorInfo = mobile.Info.LocomotorInfo;
				rejectMove = !self.AcceptsOrder("Move");
			}

			public string OrderID { get { return "Move"; } }
			public int OrderPriority { get { return 4; } }
			public bool IsQueued { get; protected set; }

			public bool CanTarget(Actor self, Target target, List<Actor> othersAtTarget, ref TargetModifiers modifiers, ref string cursor)
			{
				if (rejectMove || target.Type != TargetType.Terrain)
					return false;

				var location = self.World.Map.CellContaining(target.CenterPosition);
				IsQueued = modifiers.HasModifier(TargetModifiers.ForceQueue);

				var explored = self.Owner.Shroud.IsExplored(location);
				cursor = self.World.Map.Contains(location) ?
					(self.World.Map.GetTerrainInfo(location).CustomCursor ?? mobile.Info.Cursor) : mobile.Info.BlockedCursor;

				if (mobile.IsTraitDisabled
					|| (!explored && !locomotorInfo.MoveIntoShroud)
					|| (explored && locomotorInfo.MovementCostForCell(self.World, location) == int.MaxValue))
					cursor = mobile.Info.BlockedCursor;

				return true;
			}
		}
	}
}
