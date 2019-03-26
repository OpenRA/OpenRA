#region Copyright & License Information
/*
 * Copyright 2007-2019 The OpenRA Developers (see AUTHORS)
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
using OpenRA.Activities;
using OpenRA.Mods.Common.Activities;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Unit is able to move.")]
	public class TransformToMobileInfo : PausableConditionalTraitInfo, IMoveInfo, IFacingInfo
	{
		[Desc("Which Locomotor does this trait use. Must be defined on the World actor.")]
		[LocomotorReference]
		[FieldLoader.Require]
		public readonly string Locomotor = null;

		public readonly int InitialFacing = 0;

		[Desc("Speed at which the actor turns.")]
		public readonly int TurnSpeed = 255;

		public readonly int Speed = 1;

		public readonly string Cursor = "move";
		public readonly string BlockedCursor = "move-blocked";

		[VoiceReference]
		public readonly string Voice = "Action";

		public override object Create(ActorInitializer init) { return new TransformToMobile(init, this); }

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

		public bool CanMoveIntoShroud() { return LocomotorInfo.MoveIntoShroud; }

		public bool CanEnterCell(World world, Actor self, CPos cell, Actor ignoreActor = null, bool checkTransientActors = true)
		{
			if (LocomotorInfo.MovementCostForCell(world, cell) == int.MaxValue)
				return false;

			var check = checkTransientActors ? CellConditions.All : CellConditions.BlockedByMovers;
			return LocomotorInfo.CanMoveFreelyInto(world, self, cell, ignoreActor, check);
		}

		public bool CanMoveInCell(World world, Actor self, CPos cell, Actor ignoreActor = null, bool checkTransientActors = true)
		{
			return CanEnterCell(world, self, cell, ignoreActor, checkTransientActors);
		}
	}

	public class TransformToMobile : PausableConditionalTrait<TransformToMobileInfo>, IIssueOrder, IResolveOrder, IOrderVoice, IMove,
		IFacing
	{
		readonly Actor self;
		readonly Lazy<IEnumerable<int>> speedModifiers;

		#region IMove CurrentMovementTypes
		MovementType movementTypes;
		public MovementType CurrentMovementTypes
		{
			get
			{
				return movementTypes;
			}

			set
			{
				var oldValue = movementTypes;
				movementTypes = value;
				if (value != oldValue)
					foreach (var n in notifyMoving)
						n.MovementTypeChanged(self, value);
			}
		}
		#endregion

		int facing;
		public SubCell FromSubCell, ToSubCell;
		INotifyCustomLayerChanged[] notifyCustomLayerChanged;
		INotifyVisualPositionChanged[] notifyVisualPositionChanged;
		INotifyMoving[] notifyMoving;
		INotifyFinishedMoving[] notifyFinishedMoving;
		IWrapMove[] moveWrappers;
		IPositionable positionable;

		#region IFacing
		[Sync]
		public int Facing
		{
			get { return facing; }
			set { facing = value; }
		}

		public int TurnSpeed { get { return Info.TurnSpeed; } }
		#endregion

		public TransformToMobile(ActorInitializer init, TransformToMobileInfo info)
			: base(info)
		{
			self = init.Self;

			speedModifiers = Exts.Lazy(() => self.TraitsImplementing<ISpeedModifier>().ToArray().Select(x => x.GetSpeedModifier()));

			ToSubCell = FromSubCell = info.LocomotorInfo.SharesCell ? init.World.Map.Grid.DefaultSubCell : SubCell.FullCell;
			if (init.Contains<SubCellInit>())
				FromSubCell = ToSubCell = init.Get<SubCellInit, SubCell>();

			Facing = init.Contains<FacingInit>() ? init.Get<FacingInit, int>() : info.InitialFacing;
		}

		protected override void Created(Actor self)
		{
			notifyCustomLayerChanged = self.TraitsImplementing<INotifyCustomLayerChanged>().ToArray();
			notifyVisualPositionChanged = self.TraitsImplementing<INotifyVisualPositionChanged>().ToArray();
			notifyMoving = self.TraitsImplementing<INotifyMoving>().ToArray();
			notifyFinishedMoving = self.TraitsImplementing<INotifyFinishedMoving>().ToArray();
			moveWrappers = self.TraitsImplementing<IWrapMove>().ToArray();
			positionable = self.TraitOrDefault<IPositionable>();

			base.Created(self);
		}

		#region IMove

		Activity WrapMove(Activity inner)
		{
			var moveWrapper = moveWrappers.FirstOrDefault(Exts.IsTraitEnabled);
			if (moveWrapper != null)
				return moveWrapper.WrapMove(inner);

			return inner;
		}

		public Activity MoveTo(CPos cell, int nearEnough, bool evaluateNearestMovableCell = false)
		{
			return WrapMove(new Move(self, cell, WDist.FromCells(nearEnough), null, evaluateNearestMovableCell));
		}

		public Activity MoveTo(CPos cell, Actor ignoreActor)
		{
			return WrapMove(new Move(self, cell, WDist.Zero, ignoreActor));
		}

		public Activity MoveWithinRange(Target target, WDist range,
			WPos? initialTargetPosition = null, Color? targetLineColor = null)
		{
			return WrapMove(new MoveWithinRange(self, target, WDist.Zero, range, initialTargetPosition, targetLineColor));
		}

		public Activity MoveWithinRange(Target target, WDist minRange, WDist maxRange,
			WPos? initialTargetPosition = null, Color? targetLineColor = null)
		{
			return WrapMove(new MoveWithinRange(self, target, minRange, maxRange, initialTargetPosition, targetLineColor));
		}

		public Activity MoveFollow(Actor self, Target target, WDist minRange, WDist maxRange,
			WPos? initialTargetPosition = null, Color? targetLineColor = null)
		{
			return WrapMove(new Follow(self, target, minRange, maxRange, initialTargetPosition, targetLineColor));
		}

		public Activity MoveIntoWorld(Actor self, CPos cell, SubCell subCell = SubCell.Any)
		{
			var pos = self.CenterPosition;

			if (subCell == SubCell.Any)
				subCell = Info.LocomotorInfo.SharesCell ? self.World.ActorMap.FreeSubCell(cell, subCell) : SubCell.FullCell;

			// TODO: solve/reduce cell is full problem
			if (subCell == SubCell.Invalid)
				subCell = self.World.Map.Grid.DefaultSubCell;

			// Reserve the exit cell
			positionable.SetPosition(self, cell, subCell);
			positionable.SetVisualPosition(self, pos);

			return WrapMove(VisualMove(self, pos, self.World.Map.CenterOfSubCell(cell, subCell), cell));
		}

		public Activity MoveToTarget(Actor self, Target target,
			WPos? initialTargetPosition = null, Color? targetLineColor = null)
		{
			if (target.Type == TargetType.Invalid)
				return null;

			return WrapMove(new MoveAdjacentTo(self, target, initialTargetPosition, targetLineColor));
		}

		public Activity MoveIntoTarget(Actor self, Target target)
		{
			if (target.Type == TargetType.Invalid)
				return null;

			// Activity cancels if the target moves by more than half a cell
			// to avoid problems with the cell grid
			return WrapMove(new VisualMoveIntoTarget(self, target, new WDist(512)));
		}

		public Activity VisualMove(Actor self, WPos fromPos, WPos toPos)
		{
			return WrapMove(VisualMove(self, fromPos, toPos, self.Location));
		}

		public int EstimatedMoveDuration(Actor self, WPos fromPos, WPos toPos)
		{
			var speed = MovementSpeedForCell(self, self.Location);
			return speed > 0 ? (toPos - fromPos).Length / speed : 0;
		}

		public CPos NearestMoveableCell(CPos target)
		{
			// Limit search to a radius of 10 tiles
			return NearestMoveableCell(target, 1, 10);
		}

		public bool CanEnterTargetNow(Actor self, Target target)
		{
			if (target.Type == TargetType.FrozenActor && !target.FrozenActor.IsValid)
				return false;

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

			if (Info.CanMoveInCell(self.World, self, target))
				return target;

			foreach (var tile in self.World.Map.FindTilesInAnnulus(target, minRange, maxRange))
				if (Info.CanMoveInCell(self.World, self, tile))
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

		public Activity ScriptedMove(CPos cell) { return new Move(self, cell); }
		public Activity MoveTo(Func<List<CPos>> pathFunc) { return new Move(self, pathFunc); }

		Activity VisualMove(Actor self, WPos fromPos, WPos toPos, CPos cell)
		{
			var speed = MovementSpeedForCell(self, cell);
			var length = speed > 0 ? (toPos - fromPos).Length / speed : 0;

			var delta = toPos - fromPos;
			var facing = delta.HorizontalLengthSquared != 0 ? delta.Yaw.Facing : Facing;
			return ActivityUtils.SequenceActivities(self, new Turn(self, facing), new Drag(self, fromPos, toPos, length));
		}

		#endregion

		IEnumerable<IOrderTargeter> IIssueOrder.Orders
		{
			get
			{
				if (!IsTraitDisabled)
					yield return new MoveOrderTargeter(self, this);
			}
		}

		// Note: Returns a valid order even if the unit can't move to the target
		Order IIssueOrder.IssueOrder(Actor self, IOrderTargeter order, Target target, bool queued)
		{
			if (order is MoveOrderTargeter)
				return new Order("Move", self, target, queued);

			return null;
		}

		void IResolveOrder.ResolveOrder(Actor self, Order order)
		{
			if (IsTraitDisabled)
				return;

			if (order.OrderString == "Move")
			{
				var cell = self.World.Map.Clamp(this.self.World.Map.CellContaining(order.Target.CenterPosition));
				if (!Info.LocomotorInfo.MoveIntoShroud && !self.Owner.Shroud.IsExplored(cell))
					return;

				if (!order.Queued)
					self.CancelActivity();

				self.SetTargetLine(Target.FromCell(self.World, cell), Color.Green);
				self.QueueActivity(order.Queued, MoveTo(cell, 8, true));
			}

			if (order.OrderString == "Stop")
				self.CancelActivity();
		}

		string IOrderVoice.VoicePhraseForOrder(Actor self, Order order)
		{
			if (IsTraitDisabled)
				return null;

			switch (order.OrderString)
			{
				case "Move":
					if (!Info.LocomotorInfo.MoveIntoShroud && order.Target.Type != TargetType.Invalid)
					{
						var cell = self.World.Map.CellContaining(order.Target.CenterPosition);
						if (!self.Owner.Shroud.IsExplored(cell))
							return null;
					}

					return Info.Voice;
				case "Stop":
					return Info.Voice;
				default:
					return null;
			}
		}

		class MoveOrderTargeter : IOrderTargeter
		{
			readonly TransformToMobile mobile;
			readonly TransformToMobileInfo info;
			readonly bool rejectMove;
			public bool TargetOverridesSelection(TargetModifiers modifiers)
			{
				return modifiers.HasModifier(TargetModifiers.ForceMove);
			}

			public MoveOrderTargeter(Actor self, TransformToMobile unit)
			{
				mobile = unit;
				info = mobile.Info;
				rejectMove = !self.AcceptsOrder("Move");
			}

			public string OrderID { get { return "Move"; } }
			public int OrderPriority { get { return 4; } }
			public bool IsQueued { get; protected set; }

			public bool CanTarget(Actor self, Target target, List<Actor> othersAtTarget, ref TargetModifiers modifiers, ref string cursor)
			{
				var moveWrapper = mobile.moveWrappers.FirstOrDefault(Exts.IsTraitEnabled);
				if (moveWrapper == null || !moveWrapper.CanWrapMoveOrder(modifiers))
					return false;

				if (rejectMove || target.Type != TargetType.Terrain)
					return false;

				var location = self.World.Map.CellContaining(target.CenterPosition);
				IsQueued = modifiers.HasModifier(TargetModifiers.ForceQueue);

				var explored = self.Owner.Shroud.IsExplored(location);
				cursor = self.World.Map.Contains(location) ?
					(self.World.Map.GetTerrainInfo(location).CustomCursor ?? info.Cursor) : info.BlockedCursor;

				if (mobile.IsTraitPaused
					|| (!explored && !info.CanMoveIntoShroud())
					|| (explored && !info.CanMoveInCell(self.World, self, location, null, false)))
					cursor = info.BlockedCursor;

				return true;
			}
		}
	}
}
