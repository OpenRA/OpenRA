#region Copyright & License Information
/*
 * Copyright 2007-2020 The OpenRA Developers (see AUTHORS)
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
using OpenRA.Mods.Common.Pathfinder;
using OpenRA.Primitives;
using OpenRA.Support;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Unit is able to move.")]
	public class MobileInfo : PausableConditionalTraitInfo, IMoveInfo, IPositionableInfo, IFacingInfo, IActorPreviewInitInfo,
		IEditorActorOptions
	{
		[LocomotorReference]
		[FieldLoader.Require]
		[Desc("Which Locomotor does this trait use. Must be defined on the World actor.")]
		public readonly string Locomotor = null;

		public readonly WAngle InitialFacing = WAngle.Zero;

		[Desc("Speed at which the actor turns.")]
		public readonly WAngle TurnSpeed = new WAngle(512);

		public readonly int Speed = 1;

		[Desc("If set to true, this unit will always turn in place instead of following a curved trajectory (like infantry).")]
		public readonly bool AlwaysTurnInPlace = false;

		[Desc("Cursor to display when a move order can be issued at target location.")]
		public readonly string Cursor = "move";

		[Desc("Cursor to display when a move order cannot be issued at target location.")]
		public readonly string BlockedCursor = "move-blocked";

		[VoiceReference]
		public readonly string Voice = "Action";

		[Desc("Color to use for the target line for regular move orders.")]
		public readonly Color TargetLineColor = Color.Green;

		[Desc("Facing to use for actor previews (map editor, color picker, etc)")]
		public readonly WAngle PreviewFacing = new WAngle(384);

		[Desc("Display order for the facing slider in the map editor")]
		public readonly int EditorFacingDisplayOrder = 3;

		[ConsumedConditionReference]
		[Desc("Boolean expression defining the condition under which the regular (non-force) move cursor is disabled.")]
		public readonly BooleanExpression RequireForceMoveCondition = null;

		[ConsumedConditionReference]
		[Desc("Boolean expression defining the condition under which this actor cannot be nudged by other actors.")]
		public readonly BooleanExpression ImmovableCondition = null;

		IEnumerable<ActorInit> IActorPreviewInitInfo.ActorPreviewInits(ActorInfo ai, ActorPreviewType type)
		{
			yield return new FacingInit(PreviewFacing);
		}

		public Color GetTargetLineColor() { return TargetLineColor; }

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

			// We need to reset the reference to the locomotor between each worlds, otherwise we are reference the previous state.
			locomotor = null;

			base.RulesetLoaded(rules, ai);
		}

		public WAngle GetInitialFacing() { return InitialFacing; }

		// initialized and used by CanEnterCell
		Locomotor locomotor;

		/// <summary>
		/// Note: If the target <paramref name="cell"/> has any free subcell, the value of <paramref name="subCell"/> is ignored.
		/// </summary>
		public bool CanEnterCell(World world, Actor self, CPos cell, SubCell subCell = SubCell.FullCell, Actor ignoreActor = null, BlockedByActor check = BlockedByActor.All)
		{
			// PERF: Avoid repeated trait queries on the hot path
			if (locomotor == null)
				locomotor = world.WorldActor.TraitsImplementing<Locomotor>()
				   .SingleOrDefault(l => l.Info.Name == Locomotor);

			if (locomotor.MovementCostForCell(cell) == short.MaxValue)
				return false;

			return locomotor.CanMoveFreelyInto(self, cell, subCell, check, ignoreActor);
		}

		public bool CanStayInCell(World world, CPos cell)
		{
			// PERF: Avoid repeated trait queries on the hot path
			if (locomotor == null)
				locomotor = world.WorldActor.TraitsImplementing<Locomotor>()
				   .SingleOrDefault(l => l.Info.Name == Locomotor);

			if (cell.Layer == CustomMovementLayerType.Tunnel)
				return false;

			return locomotor.CanStayInCell(cell);
		}

		public IReadOnlyDictionary<CPos, SubCell> OccupiedCells(ActorInfo info, CPos location, SubCell subCell = SubCell.Any)
		{
			return new ReadOnlyDictionary<CPos, SubCell>(new Dictionary<CPos, SubCell>() { { location, subCell } });
		}

		bool IOccupySpaceInfo.SharesCell { get { return LocomotorInfo.SharesCell; } }

		IEnumerable<EditorActorOption> IEditorActorOptions.ActorOptions(ActorInfo ai, World world)
		{
			yield return new EditorActorSlider("Facing", EditorFacingDisplayOrder, 0, 1023, 8,
				actor =>
				{
					var init = actor.GetInitOrDefault<FacingInit>(this);
					return (init != null ? init.Value : InitialFacing).Angle;
				},
				(actor, value) => actor.ReplaceInit(new FacingInit(new WAngle((int)value))));
		}
	}

	public class Mobile : PausableConditionalTrait<MobileInfo>, IIssueOrder, IResolveOrder, IOrderVoice, IPositionable, IMove, ITick, ICreationActivity,
		IFacing, IDeathActorInitModifier, INotifyAddedToWorld, INotifyRemovedFromWorld, INotifyBlockingMove, IActorPreviewInitModifier, INotifyBecomingIdle
	{
		readonly Actor self;
		readonly Lazy<IEnumerable<int>> speedModifiers;

		readonly bool returnToCellOnCreation;
		readonly bool returnToCellOnCreationRecalculateSubCell = true;
		readonly int creationActivityDelay;

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
				{
					self.World.ActorMap.UpdateOccupiedCells(self.OccupiesSpace);
					foreach (var n in notifyMoving)
						n.MovementTypeChanged(self, value);
				}
			}
		}
		#endregion

		WAngle oldFacing;
		WRot orientation;
		WPos oldPos;
		CPos fromCell, toCell;
		public SubCell FromSubCell, ToSubCell;

		INotifyCustomLayerChanged[] notifyCustomLayerChanged;
		INotifyVisualPositionChanged[] notifyVisualPositionChanged;
		INotifyMoving[] notifyMoving;
		INotifyFinishedMoving[] notifyFinishedMoving;
		IWrapMove[] moveWrappers;
		bool requireForceMove;

		public bool IsImmovable { get; private set; }
		public bool TurnToMove;
		public bool IsBlocking { get; private set; }

		public bool IsMovingBetweenCells
		{
			get { return FromCell != ToCell; }
		}

		#region IFacing

		[Sync]
		public WAngle Facing
		{
			get { return orientation.Yaw; }
			set { orientation = orientation.WithYaw(value); }
		}

		public WRot Orientation { get { return orientation; } }

		public WAngle TurnSpeed { get { return Info.TurnSpeed; } }
		#endregion

		[Sync]
		public CPos FromCell { get { return fromCell; } }

		[Sync]
		public CPos ToCell { get { return toCell; } }

		[Sync]
		public int PathHash;	// written by Move.EvalPath, to temporarily debug this crap.

		public Locomotor Locomotor { get; private set; }

		public IPathFinder Pathfinder { get; private set; }

		#region IOccupySpace

		[Sync]
		public WPos CenterPosition { get; private set; }

		public CPos TopLeft { get { return ToCell; } }

		public (CPos, SubCell)[] OccupiedCells()
		{
			if (FromCell == ToCell)
				return new[] { (FromCell, FromSubCell) };

			// HACK: Should be fixed properly, see https://github.com/OpenRA/OpenRA/pull/17292 for an explanation
			if (Info.LocomotorInfo.SharesCell)
				return new[] { (ToCell, ToSubCell) };

			return new[] { (FromCell, FromSubCell), (ToCell, ToSubCell) };
		}
		#endregion

		public Mobile(ActorInitializer init, MobileInfo info)
			: base(info)
		{
			self = init.Self;

			speedModifiers = Exts.Lazy(() => self.TraitsImplementing<ISpeedModifier>().ToArray().Select(x => x.GetSpeedModifier()));

			ToSubCell = FromSubCell = info.LocomotorInfo.SharesCell ? init.World.Map.Grid.DefaultSubCell : SubCell.FullCell;

			var subCellInit = init.GetOrDefault<SubCellInit>();
			if (subCellInit != null)
			{
				FromSubCell = ToSubCell = subCellInit.Value;
				returnToCellOnCreationRecalculateSubCell = false;
			}

			var locationInit = init.GetOrDefault<LocationInit>();
			if (locationInit != null)
			{
				fromCell = toCell = locationInit.Value;
				SetVisualPosition(self, init.World.Map.CenterOfSubCell(FromCell, FromSubCell));
			}

			Facing = oldFacing = init.GetValue<FacingInit, WAngle>(info.InitialFacing);

			// Sets the initial visual position
			// Unit will move into the cell grid (defined by LocationInit) as its initial activity
			var centerPositionInit = init.GetOrDefault<CenterPositionInit>();
			if (centerPositionInit != null)
			{
				oldPos = centerPositionInit.Value;
				SetVisualPosition(self, oldPos);
				returnToCellOnCreation = true;
			}

			creationActivityDelay = init.GetValue<CreationActivityDelayInit, int>(0);
		}

		protected override void Created(Actor self)
		{
			notifyCustomLayerChanged = self.TraitsImplementing<INotifyCustomLayerChanged>().ToArray();
			notifyVisualPositionChanged = self.TraitsImplementing<INotifyVisualPositionChanged>().ToArray();
			notifyMoving = self.TraitsImplementing<INotifyMoving>().ToArray();
			notifyFinishedMoving = self.TraitsImplementing<INotifyFinishedMoving>().ToArray();
			moveWrappers = self.TraitsImplementing<IWrapMove>().ToArray();
			Pathfinder = self.World.WorldActor.Trait<IPathFinder>();
			Locomotor = self.World.WorldActor.TraitsImplementing<Locomotor>()
				.Single(l => l.Info.Name == Info.Locomotor);

			base.Created(self);
		}

		void ITick.Tick(Actor self)
		{
			UpdateMovement(self);
		}

		public void UpdateMovement(Actor self)
		{
			var newMovementTypes = MovementType.None;
			if ((oldPos - CenterPosition).HorizontalLengthSquared != 0)
				newMovementTypes |= MovementType.Horizontal;

			if (oldPos.Z != CenterPosition.Z)
				newMovementTypes |= MovementType.Vertical;

			if (oldFacing != Facing)
				newMovementTypes |= MovementType.Turn;

			CurrentMovementTypes = newMovementTypes;

			oldPos = CenterPosition;
			oldFacing = Facing;
		}

		void INotifyAddedToWorld.AddedToWorld(Actor self)
		{
			self.World.AddToMaps(self, this);
		}

		void INotifyRemovedFromWorld.RemovedFromWorld(Actor self)
		{
			self.World.RemoveFromMaps(self, this);
		}

		protected override void TraitEnabled(Actor self)
		{
			self.World.ActorMap.UpdateOccupiedCells(self.OccupiesSpace);
		}

		protected override void TraitDisabled(Actor self)
		{
			self.World.ActorMap.UpdateOccupiedCells(self.OccupiesSpace);
		}

		protected override void TraitResumed(Actor self)
		{
			self.World.ActorMap.UpdateOccupiedCells(self.OccupiesSpace);
		}

		protected override void TraitPaused(Actor self)
		{
			self.World.ActorMap.UpdateOccupiedCells(self.OccupiesSpace);
		}

		#region Local misc stuff

		public void Nudge(Actor nudger)
		{
			if (IsTraitDisabled || IsTraitPaused || IsImmovable)
				return;

			var cell = GetAdjacentCell(nudger.Location);
			if (cell != null)
				self.QueueActivity(false, MoveTo(cell.Value, 0));
		}

		public CPos? GetAdjacentCell(CPos nextCell, Func<CPos, bool> preferToAvoid = null)
		{
			var availCells = new List<CPos>();
			var notStupidCells = new List<CPos>();
			foreach (CVec direction in CVec.Directions)
			{
				var p = ToCell + direction;
				if (CanEnterCell(p) && CanStayInCell(p) && (preferToAvoid == null || !preferToAvoid(p)))
					availCells.Add(p);
				else if (p != nextCell && p != ToCell)
					notStupidCells.Add(p);
			}

			CPos? newCell = null;
			if (availCells.Count > 0)
				newCell = availCells.Random(self.World.SharedRandom);
			else
			{
				var cellInfo = notStupidCells
					.SelectMany(c => self.World.ActorMap.GetActorsAt(c).Where(IsMovable),
						(c, a) => new { Cell = c, Actor = a })
					.RandomOrDefault(self.World.SharedRandom);
				if (cellInfo != null)
					newCell = cellInfo.Cell;
			}

			return newCell;
		}

		static bool IsMovable(Actor otherActor)
		{
			if (!otherActor.IsIdle)
				return false;

			var mobile = otherActor.TraitOrDefault<Mobile>();
			if (mobile == null || mobile.IsTraitDisabled || mobile.IsTraitPaused || mobile.IsImmovable)
				return false;

			return true;
		}

		public bool IsLeaving()
		{
			if (CurrentMovementTypes.HasMovementType(MovementType.Horizontal))
				return true;

			if (CurrentMovementTypes.HasMovementType(MovementType.Turn))
				return TurnToMove;

			return false;
		}

		public bool CanInteractWithGroundLayer(Actor self)
		{
			// TODO: Think about extending this to support arbitrary layer-layer checks
			// in a way that is compatible with the other IMove types.
			// This would then allow us to e.g. have units attack other units inside tunnels.
			if (ToCell.Layer == 0)
				return true;

			if (self.World.GetCustomMovementLayers().TryGetValue(ToCell.Layer, out var layer))
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

		public SubCell GetAvailableSubCell(CPos a, SubCell preferredSubCell = SubCell.Any, Actor ignoreActor = null, BlockedByActor check = BlockedByActor.All)
		{
			return Locomotor.GetAvailableSubCell(self, a, check, preferredSubCell, ignoreActor);
		}

		public bool CanExistInCell(CPos cell)
		{
			return Locomotor.MovementCostForCell(cell) != short.MaxValue;
		}

		public bool CanEnterCell(CPos cell, Actor ignoreActor = null, BlockedByActor check = BlockedByActor.All)
		{
			return Info.CanEnterCell(self.World, self, cell, ToSubCell, ignoreActor, check);
		}

		public bool CanStayInCell(CPos cell)
		{
			return Info.CanStayInCell(self.World, cell);
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
			IsBlocking = false;

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

			var actors = self.World.ActorMap.GetActorsAt(ToCell, ToSubCell).Where(a => a != self).ToList();
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

		Activity WrapMove(Activity inner)
		{
			var moveWrapper = moveWrappers.FirstOrDefault(Exts.IsTraitEnabled);
			if (moveWrapper != null)
				return moveWrapper.WrapMove(inner);

			return inner;
		}

		public Activity MoveTo(CPos cell, int nearEnough = 0, Actor ignoreActor = null,
			bool evaluateNearestMovableCell = false, Color? targetLineColor = null)
		{
			return WrapMove(new Move(self, cell, WDist.FromCells(nearEnough), ignoreActor, evaluateNearestMovableCell, targetLineColor));
		}

		public Activity MoveWithinRange(in Target target, WDist range,
			WPos? initialTargetPosition = null, Color? targetLineColor = null)
		{
			return WrapMove(new MoveWithinRange(self, target, WDist.Zero, range, initialTargetPosition, targetLineColor));
		}

		public Activity MoveWithinRange(in Target target, WDist minRange, WDist maxRange,
			WPos? initialTargetPosition = null, Color? targetLineColor = null)
		{
			return WrapMove(new MoveWithinRange(self, target, minRange, maxRange, initialTargetPosition, targetLineColor));
		}

		public Activity MoveFollow(Actor self, in Target target, WDist minRange, WDist maxRange,
			WPos? initialTargetPosition = null, Color? targetLineColor = null)
		{
			return WrapMove(new Follow(self, target, minRange, maxRange, initialTargetPosition, targetLineColor));
		}

		public Activity ReturnToCell(Actor self)
		{
			return new ReturnToCellActivity(self);
		}

		public class ReturnToCellActivity : Activity
		{
			readonly Mobile mobile;
			readonly bool recalculateSubCell;

			CPos cell;
			SubCell subCell;
			WPos pos;
			int delay;

			public ReturnToCellActivity(Actor self, int delay = 0, bool recalculateSubCell = false)
			{
				mobile = self.Trait<Mobile>();
				IsInterruptible = false;
				this.delay = delay;
				this.recalculateSubCell = recalculateSubCell;
			}

			protected override void OnFirstRun(Actor self)
			{
				pos = self.CenterPosition;
				if (self.World.Map.DistanceAboveTerrain(pos) > WDist.Zero && self.TraitOrDefault<Parachutable>() != null)
					QueueChild(new Parachute(self));
			}

			public override bool Tick(Actor self)
			{
				pos = self.CenterPosition;
				cell = mobile.ToCell;
				subCell = mobile.ToSubCell;

				if (recalculateSubCell)
					subCell = mobile.Info.LocomotorInfo.SharesCell ? self.World.ActorMap.FreeSubCell(cell, subCell, a => a != self) : SubCell.FullCell;

				// TODO: solve/reduce cell is full problem
				if (subCell == SubCell.Invalid)
					subCell = self.World.Map.Grid.DefaultSubCell;

				// Reserve the exit cell
				mobile.SetPosition(self, cell, subCell);
				mobile.SetVisualPosition(self, pos);

				if (delay > 0)
					QueueChild(new Wait(delay));

				QueueChild(mobile.VisualMove(self, pos, self.World.Map.CenterOfSubCell(cell, subCell)));
				return true;
			}
		}

		public Activity MoveToTarget(Actor self, in Target target,
			WPos? initialTargetPosition = null, Color? targetLineColor = null)
		{
			if (target.Type == TargetType.Invalid)
				return null;

			return WrapMove(new MoveAdjacentTo(self, target, initialTargetPosition, targetLineColor));
		}

		public Activity MoveIntoTarget(Actor self, in Target target)
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

		public bool CanEnterTargetNow(Actor self, in Target target)
		{
			if (target.Type == TargetType.FrozenActor && !target.FrozenActor.IsValid)
				return false;

			return self.Location == self.World.Map.CellContaining(target.CenterPosition) || Util.AdjacentCells(self.World, target).Any(c => c == self.Location);
		}

		#endregion

		#region Local IMove-related

		public int MovementSpeedForCell(Actor self, CPos cell)
		{
			var terrainSpeed = Locomotor.MovementSpeedForCell(cell);
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

			if (target == self.Location && CanStayInCell(target))
				return target;

			if (CanEnterCell(target, check: BlockedByActor.Immovable) && CanStayInCell(target))
				return target;

			foreach (var tile in self.World.Map.FindTilesInAnnulus(target, minRange, maxRange))
				if (CanEnterCell(tile, check: BlockedByActor.Immovable) && CanStayInCell(tile))
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
		public Activity MoveTo(Func<BlockedByActor, List<CPos>> pathFunc) { return new Move(self, pathFunc); }

		Activity VisualMove(Actor self, WPos fromPos, WPos toPos, CPos cell)
		{
			var speed = MovementSpeedForCell(self, cell);
			var length = speed > 0 ? (toPos - fromPos).Length / speed : 0;

			var delta = toPos - fromPos;
			var facing = delta.HorizontalLengthSquared != 0 ? delta.Yaw : Facing;

			return new Drag(self, fromPos, toPos, length, facing);
		}

		CPos? ClosestGroundCell()
		{
			var above = new CPos(TopLeft.X, TopLeft.Y);
			if (CanEnterCell(above))
				return above;

			var pathFinder = self.World.WorldActor.Trait<IPathFinder>();
			List<CPos> path;
			using (var search = PathSearch.Search(self.World, Locomotor, self, BlockedByActor.All,
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
				inits.Add(new DynamicFacingInit(() => Facing));
		}

		void IDeathActorInitModifier.ModifyDeathActorInit(Actor self, TypeDictionary init)
		{
			init.Add(new FacingInit(Facing));

			// Allows the husk to drag to its final position
			if (CanEnterCell(self.Location, self, BlockedByActor.Stationary))
				init.Add(new HuskSpeedInit(MovementSpeedForCell(self, self.Location)));
		}

		void INotifyBecomingIdle.OnBecomingIdle(Actor self)
		{
			if (self.Location.Layer == 0)
			{
				// Make sure that units aren't left idling in a transit-only cell
				// HACK: activities should be making sure that this can't happen in the first place!
				if (!Locomotor.CanStayInCell(self.Location))
					self.QueueActivity(MoveTo(self.Location, evaluateNearestMovableCell: true));
				return;
			}

			var cml = self.World.WorldActor.TraitsImplementing<ICustomMovementLayer>()
				.First(l => l.Index == self.Location.Layer);

			if (!cml.ReturnToGroundLayerOnIdle)
				return;

			var moveTo = ClosestGroundCell();
			if (moveTo != null)
				self.QueueActivity(MoveTo(moveTo.Value, 0));
		}

		void INotifyBlockingMove.OnNotifyBlockingMove(Actor self, Actor blocking)
		{
			if (!self.AppearsFriendlyTo(blocking))
				return;

			if (self.IsIdle)
			{
				Nudge(blocking);
				return;
			}

			IsBlocking = true;
		}

		public override IEnumerable<VariableObserver> GetVariableObservers()
		{
			foreach (var observer in base.GetVariableObservers())
				yield return observer;

			if (Info.RequireForceMoveCondition != null)
				yield return new VariableObserver(RequireForceMoveConditionChanged, Info.RequireForceMoveCondition.Variables);

			if (Info.ImmovableCondition != null)
				yield return new VariableObserver(ImmovableConditionChanged, Info.ImmovableCondition.Variables);
		}

		void RequireForceMoveConditionChanged(Actor self, IReadOnlyDictionary<string, int> conditions)
		{
			requireForceMove = Info.RequireForceMoveCondition.Evaluate(conditions);
		}

		void ImmovableConditionChanged(Actor self, IReadOnlyDictionary<string, int> conditions)
		{
			var wasImmovable = IsImmovable;
			IsImmovable = Info.ImmovableCondition.Evaluate(conditions);
			if (wasImmovable != IsImmovable)
				self.World.ActorMap.UpdateOccupiedCells(self.OccupiesSpace);
		}

		IEnumerable<IOrderTargeter> IIssueOrder.Orders
		{
			get
			{
				if (!IsTraitDisabled)
					yield return new MoveOrderTargeter(self, this);
			}
		}

		// Note: Returns a valid order even if the unit can't move to the target
		Order IIssueOrder.IssueOrder(Actor self, IOrderTargeter order, in Target target, bool queued)
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

				self.QueueActivity(order.Queued, WrapMove(new Move(self, cell, WDist.FromCells(8), null, true, Info.TargetLineColor)));
				self.ShowTargetLines();
			}

			// TODO: This should only cancel activities queued by this trait
			else if (order.OrderString == "Stop")
				self.CancelActivity();
			else if (order.OrderString == "Scatter")
				Nudge(self);
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
				case "Scatter":
				case "Stop":
					return Info.Voice;
				default:
					return null;
			}
		}

		Activity ICreationActivity.GetCreationActivity()
		{
			return returnToCellOnCreation ? new ReturnToCellActivity(self, creationActivityDelay, returnToCellOnCreationRecalculateSubCell) : null;
		}

		class MoveOrderTargeter : IOrderTargeter
		{
			readonly Mobile mobile;
			readonly LocomotorInfo locomotorInfo;
			readonly bool rejectMove;
			public bool TargetOverridesSelection(Actor self, in Target target, List<Actor> actorsAt, CPos xy, TargetModifiers modifiers)
			{
				// Always prioritise orders over selecting other peoples actors or own actors that are already selected
				if (target.Type == TargetType.Actor && (target.Actor.Owner != self.Owner || self.World.Selection.Contains(target.Actor)))
					return true;

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

			public bool CanTarget(Actor self, in Target target, List<Actor> othersAtTarget, ref TargetModifiers modifiers, ref string cursor)
			{
				if (rejectMove || target.Type != TargetType.Terrain || (mobile.requireForceMove && !modifiers.HasModifier(TargetModifiers.ForceMove)))
					return false;

				var location = self.World.Map.CellContaining(target.CenterPosition);
				IsQueued = modifiers.HasModifier(TargetModifiers.ForceQueue);

				var explored = self.Owner.Shroud.IsExplored(location);
				cursor = self.World.Map.Contains(location) ?
					(self.World.Map.GetTerrainInfo(location).CustomCursor ?? mobile.Info.Cursor) : mobile.Info.BlockedCursor;

				if (mobile.IsTraitPaused
					|| (!explored && !locomotorInfo.MoveIntoShroud)
					|| (explored && mobile.Locomotor.MovementCostForCell(location) == short.MaxValue))
					cursor = mobile.Info.BlockedCursor;

				return true;
			}
		}
	}
}
