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
using OpenRA.Mods.Common.Orders;
using OpenRA.Primitives;
using OpenRA.Support;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	public enum IdleBehaviorType
	{
		None,
		Land,
		ReturnToBase,
		LeaveMap,
		LeaveMapAtClosestEdge
	}

	public class AircraftInfo : PausableConditionalTraitInfo, IPositionableInfo, IFacingInfo, IMoveInfo, ICruiseAltitudeInfo,
		IActorPreviewInitInfo, IEditorActorOptions
	{
		[Desc("Behavior when aircraft becomes idle. Options are Land, ReturnToBase, LeaveMap, and None.",
			"'Land' will behave like 'None' (hover or circle) if a suitable landing site is not available.")]
		public readonly IdleBehaviorType IdleBehavior = IdleBehaviorType.None;

		public readonly WDist CruiseAltitude = new WDist(1280);

		[Desc("Whether the aircraft can be repulsed.")]
		public readonly bool Repulsable = true;

		[Desc("The distance it tries to maintain from other aircraft if repulsable.")]
		public readonly WDist IdealSeparation = new WDist(1706);

		[Desc("The speed at which the aircraft is repulsed from other aircraft. Specify -1 for normal movement speed.")]
		public readonly int RepulsionSpeed = -1;

		public readonly WAngle InitialFacing = WAngle.Zero;

		[Desc("Speed at which the actor turns.")]
		public readonly WAngle TurnSpeed = new WAngle(512);

		[Desc("Turn speed to apply when aircraft flies in circles while idle. Defaults to TurnSpeed if undefined.")]
		public readonly WAngle? IdleTurnSpeed = null;

		[Desc("Maximum flight speed when cruising.")]
		public readonly int Speed = 1;

		[Desc("If non-negative, force the aircraft to move in circles at this speed when idle (a speed of 0 means don't move), ignoring CanHover.")]
		public readonly int IdleSpeed = -1;

		[Desc("Body pitch when flying forwards. Only relevant for voxel aircraft.")]
		public readonly WAngle Pitch = WAngle.Zero;

		[Desc("Pitch steps to apply each tick when starting/stopping.")]
		public readonly WAngle PitchSpeed = WAngle.Zero;

		[Desc("Body roll when turning. Only relevant for voxel aircraft.")]
		public readonly WAngle Roll = WAngle.Zero;

		[Desc("Body roll to apply when aircraft flies in circles while idle. Defaults to Roll if undefined. Only relevant for voxel aircraft.")]
		public readonly WAngle? IdleRoll = null;

		[Desc("Roll steps to apply each tick when turning.")]
		public readonly WAngle RollSpeed = WAngle.Zero;

		[Desc("Minimum altitude where this aircraft is considered airborne.")]
		public readonly int MinAirborneAltitude = 1;

		public readonly HashSet<string> LandableTerrainTypes = new HashSet<string>();

		[Desc("Can the actor be ordered to move in to shroud?")]
		public readonly bool MoveIntoShroud = true;

		[Desc("e.g. crate, wall, infantry")]
		public readonly BitSet<CrushClass> Crushes = default(BitSet<CrushClass>);

		[Desc("Types of damage that are caused while crushing. Leave empty for no damage types.")]
		public readonly BitSet<DamageType> CrushDamageTypes = default(BitSet<DamageType>);

		[VoiceReference]
		public readonly string Voice = "Action";

		[Desc("Color to use for the target line for regular move orders.")]
		public readonly Color TargetLineColor = Color.Green;

		[GrantedConditionReference]
		[Desc("The condition to grant to self while airborne.")]
		public readonly string AirborneCondition = null;

		[GrantedConditionReference]
		[Desc("The condition to grant to self while at cruise altitude.")]
		public readonly string CruisingCondition = null;

		[Desc("Can the actor hover in place mid-air? If not, then the actor will have to remain in motion (circle around).")]
		public readonly bool CanHover = false;

		[Desc("Can the actor immediately change direction without turning first (doesn't need to fly in a curve)?")]
		public readonly bool CanSlide = false;

		[Desc("Does the actor land and take off vertically?")]
		public readonly bool VTOL = false;

		[Desc("Does this VTOL actor need to turn before landing (on terrain)?")]
		public readonly bool TurnToLand = false;

		[Desc("Does this actor automatically take off after resupplying?")]
		public readonly bool TakeOffOnResupply = false;

		[Desc("Does this actor automatically take off after creation?")]
		public readonly bool TakeOffOnCreation = true;

		[Desc("Can this actor be given an explicit land order using the force-move modifier?")]
		public readonly bool CanForceLand = true;

		[Desc("Altitude at which the aircraft considers itself landed.")]
		public readonly WDist LandAltitude = WDist.Zero;

		[Desc("Range to search for an alternative landing location if the ordered cell is blocked.")]
		public readonly WDist LandRange = WDist.FromCells(5);

		[Desc("How fast this actor ascends or descends during horizontal movement.")]
		public readonly WAngle MaximumPitch = WAngle.FromDegrees(10);

		[Desc("How fast this actor ascends or descends when moving vertically only (vertical take off/landing or hovering towards CruiseAltitude).")]
		public readonly WDist AltitudeVelocity = new WDist(43);

		[Desc("Sounds to play when the actor is taking off.")]
		public readonly string[] TakeoffSounds = { };

		[Desc("Sounds to play when the actor is landing.")]
		public readonly string[] LandingSounds = { };

		[Desc("The distance of the resupply base that the aircraft will wait for its turn.")]
		public readonly WDist WaitDistanceFromResupplyBase = new WDist(3072);

		[Desc("The number of ticks that a airplane will wait to make a new search for an available airport.")]
		public readonly int NumberOfTicksToVerifyAvailableAirport = 150;

		[Desc("Facing to use for actor previews (map editor, color picker, etc)")]
		public readonly WAngle PreviewFacing = new WAngle(384);

		[Desc("Display order for the facing slider in the map editor")]
		public readonly int EditorFacingDisplayOrder = 3;

		[ConsumedConditionReference]
		[Desc("Boolean expression defining the condition under which the regular (non-force) move cursor is disabled.")]
		public readonly BooleanExpression RequireForceMoveCondition = null;

		[Desc("Cursor to display when able to land at target building.")]
		public readonly string EnterCursor = "enter";

		[Desc("Cursor to display when unable to land at target building.")]
		public readonly string EnterBlockedCursor = "enter-blocked";

		public WAngle GetInitialFacing() { return InitialFacing; }
		public WDist GetCruiseAltitude() { return CruiseAltitude; }
		public Color GetTargetLineColor() { return TargetLineColor; }

		public override object Create(ActorInitializer init) { return new Aircraft(init, this); }

		IEnumerable<ActorInit> IActorPreviewInitInfo.ActorPreviewInits(ActorInfo ai, ActorPreviewType type)
		{
			yield return new FacingInit(PreviewFacing);
		}

		public IReadOnlyDictionary<CPos, SubCell> OccupiedCells(ActorInfo info, CPos location, SubCell subCell = SubCell.Any) { return new ReadOnlyDictionary<CPos, SubCell>(); }

		bool IOccupySpaceInfo.SharesCell { get { return false; } }

		// Used to determine if an aircraft can spawn landed
		public bool CanEnterCell(World world, Actor self, CPos cell, SubCell subCell = SubCell.FullCell, Actor ignoreActor = null, BlockedByActor check = BlockedByActor.All)
		{
			if (!world.Map.Contains(cell))
				return false;

			var type = world.Map.GetTerrainInfo(cell).Type;
			if (!LandableTerrainTypes.Contains(type))
				return false;

			if (check == BlockedByActor.None)
				return true;

			// Since aircraft don't share cells, we don't pass the subCell parameter
			return !world.ActorMap.GetActorsAt(cell).Any(x => x != ignoreActor);
		}

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

	public class Aircraft : PausableConditionalTrait<AircraftInfo>, ITick, ISync, IFacing, IPositionable, IMove,
		INotifyAddedToWorld, INotifyRemovedFromWorld, INotifyActorDisposing, INotifyBecomingIdle, ICreationActivity,
		IActorPreviewInitModifier, IDeathActorInitModifier, IIssueDeployOrder, IIssueOrder, IResolveOrder, IOrderVoice
	{
		static readonly (CPos, SubCell)[] NoCells = { };

		readonly Actor self;

		Repairable repairable;
		Rearmable rearmable;
		IAircraftCenterPositionOffset[] positionOffsets;
		IDisposable reservation;
		IEnumerable<int> speedModifiers;
		INotifyMoving[] notifyMoving;
		INotifyVisualPositionChanged[] notifyVisualPositionChanged;
		IOverrideAircraftLanding overrideAircraftLanding;

		WRot orientation;

		[Sync]
		public WAngle Facing
		{
			get { return orientation.Yaw; }
			set { orientation = orientation.WithYaw(value); }
		}

		public WAngle Pitch
		{
			get { return orientation.Pitch; }
			set { orientation = orientation.WithPitch(value); }
		}

		public WAngle Roll
		{
			get { return orientation.Roll; }
			set { orientation = orientation.WithRoll(value); }
		}

		public WRot Orientation { get { return orientation; } }

		[Sync]
		public WPos CenterPosition { get; private set; }

		public CPos TopLeft { get { return self.World.Map.CellContaining(CenterPosition); } }
		public WAngle TurnSpeed { get { return !IsTraitDisabled && !IsTraitPaused ? Info.TurnSpeed : WAngle.Zero; } }
		public WAngle? IdleTurnSpeed { get { return Info.IdleTurnSpeed; } }

		public Actor ReservedActor { get; private set; }
		public bool MayYieldReservation { get; private set; }
		public bool ForceLanding { get; private set; }

		IEnumerable<CPos> landingCells = Enumerable.Empty<CPos>();
		bool requireForceMove;
		int creationActivityDelay;

		bool notify = true;

		public static WPos GroundPosition(Actor self)
		{
			return self.CenterPosition - new WVec(WDist.Zero, WDist.Zero, self.World.Map.DistanceAboveTerrain(self.CenterPosition));
		}

		public bool AtLandAltitude { get { return self.World.Map.DistanceAboveTerrain(GetPosition()) == LandAltitude; } }

		bool airborne;
		bool cruising;
		int airborneToken = Actor.InvalidConditionToken;
		int cruisingToken = Actor.InvalidConditionToken;

		MovementType movementTypes;
		WPos cachedPosition;
		WAngle cachedFacing;

		public Aircraft(ActorInitializer init, AircraftInfo info)
			: base(info)
		{
			self = init.Self;

			var locationInit = init.GetOrDefault<LocationInit>();
			if (locationInit != null)
				SetPosition(self, locationInit.Value);

			var centerPositionInit = init.GetOrDefault<CenterPositionInit>();
			if (centerPositionInit != null)
				SetPosition(self, centerPositionInit.Value);

			Facing = init.GetValue<FacingInit, WAngle>(Info.InitialFacing);
			creationActivityDelay = init.GetValue<CreationActivityDelayInit, int>(0);
		}

		public WDist LandAltitude
		{
			get
			{
				var alt = Info.LandAltitude;
				foreach (var offset in positionOffsets)
					alt -= new WDist(offset.PositionOffset.Z);

				return alt;
			}
		}

		public WPos GetPosition()
		{
			var pos = self.CenterPosition;
			foreach (var offset in positionOffsets)
				pos += offset.PositionOffset;

			return pos;
		}

		public override IEnumerable<VariableObserver> GetVariableObservers()
		{
			foreach (var observer in base.GetVariableObservers())
				yield return observer;

			if (Info.RequireForceMoveCondition != null)
				yield return new VariableObserver(RequireForceMoveConditionChanged, Info.RequireForceMoveCondition.Variables);
		}

		void RequireForceMoveConditionChanged(Actor self, IReadOnlyDictionary<string, int> conditions)
		{
			requireForceMove = Info.RequireForceMoveCondition.Evaluate(conditions);
		}

		protected override void Created(Actor self)
		{
			repairable = self.TraitOrDefault<Repairable>();
			rearmable = self.TraitOrDefault<Rearmable>();
			speedModifiers = self.TraitsImplementing<ISpeedModifier>().ToArray().Select(sm => sm.GetSpeedModifier());
			cachedPosition = self.CenterPosition;
			notifyMoving = self.TraitsImplementing<INotifyMoving>().ToArray();
			positionOffsets = self.TraitsImplementing<IAircraftCenterPositionOffset>().ToArray();
			overrideAircraftLanding = self.TraitOrDefault<IOverrideAircraftLanding>();
			notifyVisualPositionChanged = self.TraitsImplementing<INotifyVisualPositionChanged>().ToArray();
			base.Created(self);
		}

		void INotifyAddedToWorld.AddedToWorld(Actor self)
		{
			AddedToWorld(self);
		}

		protected virtual void AddedToWorld(Actor self)
		{
			self.World.AddToMaps(self, this);

			var altitude = self.World.Map.DistanceAboveTerrain(CenterPosition);
			if (altitude.Length >= Info.MinAirborneAltitude)
				OnAirborneAltitudeReached();
			if (altitude == Info.CruiseAltitude)
				OnCruisingAltitudeReached();
		}

		void INotifyRemovedFromWorld.RemovedFromWorld(Actor self)
		{
			RemovedFromWorld(self);
		}

		protected virtual void RemovedFromWorld(Actor self)
		{
			UnReserve();
			self.World.RemoveFromMaps(self, this);

			OnCruisingAltitudeLeft();
			OnAirborneAltitudeLeft();
		}

		void ITick.Tick(Actor self)
		{
			Tick(self);
		}

		protected virtual void Tick(Actor self)
		{
			// Add land activity if Aircraft trait is paused and the actor can land at the current location.
			if (!ForceLanding && IsTraitPaused && airborne && CanLand(self.Location)
				&& !((self.CurrentActivity is Land) || self.CurrentActivity is Turn))
			{
				self.QueueActivity(false, new Land(self));
				ForceLanding = true;
			}

			// Add takeoff activity if Aircraft trait is not paused and the actor should not land when idle.
			if (ForceLanding && !IsTraitPaused && !cruising && !(self.CurrentActivity is TakeOff))
			{
				ForceLanding = false;

				if (Info.IdleBehavior != IdleBehaviorType.Land)
					self.QueueActivity(false, new TakeOff(self));
			}

			var oldCachedFacing = cachedFacing;
			cachedFacing = Facing;

			var oldCachedPosition = cachedPosition;
			cachedPosition = self.CenterPosition;

			var newMovementTypes = MovementType.None;
			if (oldCachedFacing != Facing)
				newMovementTypes |= MovementType.Turn;

			if ((oldCachedPosition - cachedPosition).HorizontalLengthSquared != 0)
				newMovementTypes |= MovementType.Horizontal;

			if ((oldCachedPosition - cachedPosition).VerticalLengthSquared != 0)
				newMovementTypes |= MovementType.Vertical;

			CurrentMovementTypes = newMovementTypes;

			if (!CurrentMovementTypes.HasMovementType(MovementType.Horizontal))
			{
				if (Info.Roll != WAngle.Zero && Roll != WAngle.Zero)
					Roll = Util.TickFacing(Roll, WAngle.Zero, Info.RollSpeed);

				if (Info.Pitch != WAngle.Zero && Pitch != WAngle.Zero)
					Pitch = Util.TickFacing(Pitch, WAngle.Zero, Info.PitchSpeed);
			}

			Repulse();
		}

		public void Repulse()
		{
			var repulsionForce = GetRepulsionForce();
			if (repulsionForce == WVec.Zero)
				return;

			var speed = Info.RepulsionSpeed != -1 ? Info.RepulsionSpeed : MovementSpeed;

			// HACK: Prevent updating visibility twice per tick. We really shouldn't be
			// moving twice in a tick in the first place.
			notify = false;
			SetPosition(self, CenterPosition + FlyStep(speed, repulsionForce.Yaw));
			notify = true;
		}

		public virtual WVec GetRepulsionForce()
		{
			if (!Info.Repulsable)
				return WVec.Zero;

			if (reservation != null)
			{
				var distanceFromReservationActor = (ReservedActor.CenterPosition - self.CenterPosition).HorizontalLength;
				if (distanceFromReservationActor < Info.WaitDistanceFromResupplyBase.Length)
					return WVec.Zero;
			}

			// Repulsion only applies when we're flying at CruiseAltitude!
			if (!cruising)
				return WVec.Zero;

			// PERF: Avoid LINQ.
			var repulsionForce = WVec.Zero;
			foreach (var actor in self.World.FindActorsInCircle(self.CenterPosition, Info.IdealSeparation))
			{
				if (actor.IsDead)
					continue;

				var ai = actor.Info.TraitInfoOrDefault<AircraftInfo>();
				if (ai == null || !ai.Repulsable || ai.CruiseAltitude != Info.CruiseAltitude)
					continue;

				repulsionForce += GetRepulsionForce(actor);
			}

			// Actors outside the map bounds receive an extra nudge towards the center of the map
			if (!self.World.Map.Contains(self.Location))
			{
				// The map bounds are in projected coordinates, which is technically wrong for this,
				// but we avoid the issues in practice by guessing the middle of the map instead of the edge
				var center = WPos.Lerp(self.World.Map.ProjectedTopLeft, self.World.Map.ProjectedBottomRight, 1, 2);
				repulsionForce += new WVec(0, 1024, 0).Rotate(WRot.FromYaw((self.CenterPosition - center).Yaw));
			}

			if (Info.CanSlide)
				return repulsionForce;

			// Non-hovering actors mush always keep moving forward, so they need extra calculations.
			var currentDir = FlyStep(Facing);
			var length = currentDir.HorizontalLength * repulsionForce.HorizontalLength;
			if (length == 0)
				return WVec.Zero;

			var dot = WVec.Dot(currentDir, repulsionForce) / length;

			// avoid stalling the plane
			return dot >= 0 ? repulsionForce : WVec.Zero;
		}

		public WVec GetRepulsionForce(Actor other)
		{
			if (self == other || other.CenterPosition.Z < self.CenterPosition.Z)
				return WVec.Zero;

			var d = self.CenterPosition - other.CenterPosition;
			var distSq = d.HorizontalLengthSquared;
			if (distSq > Info.IdealSeparation.LengthSquared)
				return WVec.Zero;

			if (distSq < 1)
			{
				var yaw = self.World.SharedRandom.Next(0, 1023);
				var rot = new WRot(WAngle.Zero, WAngle.Zero, new WAngle(yaw));
				return new WVec(1024, 0, 0).Rotate(rot);
			}

			return (d * 1024 * 8) / (int)distSq;
		}

		public Actor GetActorBelow()
		{
			// Map.DistanceAboveTerrain(WPos pos) is called directly because Aircraft is an IPositionable trait
			// and all calls occur in Tick methods.
			if (self.World.Map.DistanceAboveTerrain(CenterPosition) != LandAltitude)
				return null; // Not on the resupplier.

			return self.World.ActorMap.GetActorsAt(self.Location)
				.FirstOrDefault(a => a.Info.HasTraitInfo<ReservableInfo>());
		}

		public void MakeReservation(Actor target)
		{
			UnReserve();
			var reservable = target.TraitOrDefault<Reservable>();
			if (reservable != null)
			{
				reservation = reservable.Reserve(target, self, this);
				ReservedActor = target;
			}
		}

		public void AllowYieldingReservation()
		{
			if (reservation == null)
				return;

			MayYieldReservation = true;
		}

		public void UnReserve()
		{
			if (reservation == null)
				return;

			reservation.Dispose();
			reservation = null;
			ReservedActor = null;
			MayYieldReservation = false;
		}

		bool AircraftCanEnter(Actor a, TargetModifiers modifiers)
		{
			if (requireForceMove && !modifiers.HasModifier(TargetModifiers.ForceMove))
				return false;

			return AircraftCanEnter(a);
		}

		bool AircraftCanEnter(Actor a)
		{
			if (self.AppearsHostileTo(a))
				return false;

			var canRearmAtActor = rearmable != null && rearmable.Info.RearmActors.Contains(a.Info.Name);
			var canRepairAtActor = repairable != null && repairable.Info.RepairActors.Contains(a.Info.Name);

			return canRearmAtActor || canRepairAtActor;
		}

		bool AircraftCanResupplyAt(Actor a, bool allowedToForceEnter = false)
		{
			if (self.AppearsHostileTo(a))
				return false;

			var canRearmAtActor = rearmable != null && rearmable.Info.RearmActors.Contains(a.Info.Name);
			var canRepairAtActor = repairable != null && repairable.Info.RepairActors.Contains(a.Info.Name);

			var allowedToEnterRearmer = canRearmAtActor && (allowedToForceEnter || rearmable.RearmableAmmoPools.Any(p => !p.HasFullAmmo));
			var allowedToEnterRepairer = canRepairAtActor && (allowedToForceEnter || self.GetDamageState() != DamageState.Undamaged);

			return allowedToEnterRearmer || allowedToEnterRepairer;
		}

		public int MovementSpeed
		{
			get { return !IsTraitDisabled && !IsTraitPaused ? Util.ApplyPercentageModifiers(Info.Speed, speedModifiers) : 0; }
		}

		public (CPos Cell, SubCell SubCell)[] OccupiedCells()
		{
			if (!self.IsAtGroundLevel())
				return landingCells.Select(c => (c, SubCell.FullCell)).ToArray();

			return new[] { (TopLeft, SubCell.FullCell) };
		}

		public WVec FlyStep(WAngle facing)
		{
			return FlyStep(MovementSpeed, facing);
		}

		public WVec FlyStep(int speed, WAngle facing)
		{
			var dir = new WVec(0, -1024, 0).Rotate(WRot.FromYaw(facing));
			return speed * dir / 1024;
		}

		public CPos? FindLandingLocation(CPos targetCell, WDist maxSearchDistance)
		{
			// The easy case
			if (CanLand(targetCell, blockedByMobile: false))
				return targetCell;

			var cellRange = (maxSearchDistance.Length + 1023) / 1024;
			var centerPosition = self.World.Map.CenterOfCell(targetCell);
			foreach (var c in self.World.Map.FindTilesInCircle(targetCell, cellRange))
			{
				if (!CanLand(c, blockedByMobile: false))
					continue;

				var delta = self.World.Map.CenterOfCell(c) - centerPosition;
				if (delta.LengthSquared < maxSearchDistance.LengthSquared)
					return c;
			}

			return null;
		}

		public bool CanLand(IEnumerable<CPos> cells, Actor dockingActor = null, bool blockedByMobile = true)
		{
			foreach (var c in cells)
				if (!CanLand(c, dockingActor, blockedByMobile))
					return false;

			return true;
		}

		public bool CanLand(CPos cell, Actor dockingActor = null, bool blockedByMobile = true)
		{
			if (!self.World.Map.Contains(cell))
				return false;

			foreach (var otherActor in self.World.ActorMap.GetActorsAt(cell))
				if (IsBlockedBy(self, otherActor, dockingActor, blockedByMobile))
					return false;

			// Terrain type is ignored when docking with an actor
			if (dockingActor != null)
				return true;

			var landableTerrain = overrideAircraftLanding != null ? overrideAircraftLanding.LandableTerrainTypes : Info.LandableTerrainTypes;
			return landableTerrain.Contains(self.World.Map.GetTerrainInfo(cell).Type);
		}

		bool IsBlockedBy(Actor self, Actor otherActor, Actor ignoreActor, bool blockedByMobile = true)
		{
			// We are not blocked by the actor we are ignoring.
			if (otherActor == self || otherActor == ignoreActor)
				return false;

			// We are not blocked by actors we can nudge out of the way
			// TODO: Generalize blocker checks and handling here and in Locomotor
			if (!blockedByMobile && self.Owner.RelationshipWith(otherActor.Owner) == PlayerRelationship.Ally &&
				otherActor.TraitOrDefault<Mobile>() != null && otherActor.CurrentActivity == null)
				return false;

			// PERF: Only perform ITemporaryBlocker trait look-up if mod/map rules contain any actors that are temporary blockers
			if (self.World.RulesContainTemporaryBlocker)
			{
				// If there is a temporary blocker in our path, but we can remove it, we are not blocked.
				var temporaryBlocker = otherActor.TraitOrDefault<ITemporaryBlocker>();
				if (temporaryBlocker != null && temporaryBlocker.CanRemoveBlockage(otherActor, self))
					return false;
			}

			// If we cannot crush the other actor in our way, we are blocked.
			if (Info.Crushes.IsEmpty)
				return true;

			// If the other actor in our way cannot be crushed, we are blocked.
			// PERF: Avoid LINQ.
			var crushables = otherActor.TraitsImplementing<ICrushable>();
			foreach (var crushable in crushables)
				if (crushable.CrushableBy(otherActor, self, Info.Crushes))
					return false;

			return true;
		}

		public bool CanRearmAt(Actor host)
		{
			return rearmable != null && rearmable.Info.RearmActors.Contains(host.Info.Name) && rearmable.RearmableAmmoPools.Any(p => !p.HasFullAmmo);
		}

		public bool CanRepairAt(Actor host)
		{
			return repairable != null && repairable.Info.RepairActors.Contains(host.Info.Name) && self.GetDamageState() != DamageState.Undamaged;
		}

		public void ModifyDeathActorInit(Actor self, TypeDictionary init)
		{
			init.Add(new FacingInit(Facing));
		}

		void INotifyBecomingIdle.OnBecomingIdle(Actor self)
		{
			OnBecomingIdle(self);
		}

		protected virtual void OnBecomingIdle(Actor self)
		{
			if (Info.IdleBehavior == IdleBehaviorType.LeaveMap)
			{
				self.QueueActivity(new FlyOffMap(self));
				self.QueueActivity(new RemoveSelf());
			}
			else if (Info.IdleBehavior == IdleBehaviorType.LeaveMapAtClosestEdge)
			{
				var edgeCell = self.World.Map.ChooseClosestEdgeCell(self.Location);
				self.QueueActivity(new FlyOffMap(self, Target.FromCell(self.World, edgeCell)));
				self.QueueActivity(new RemoveSelf());
			}
			else if (Info.IdleBehavior == IdleBehaviorType.ReturnToBase && GetActorBelow() == null)
				self.QueueActivity(new ReturnToBase(self, null, !Info.TakeOffOnResupply));
			else
			{
				var dat = self.World.Map.DistanceAboveTerrain(CenterPosition);
				if (dat == LandAltitude)
				{
					if (!CanLand(self.Location) && ReservedActor == null)
						self.QueueActivity(new TakeOff(self));

					// All remaining idle behaviors rely on not being at LandAltitude, so unconditionally return
					return;
				}

				if (Info.IdleBehavior != IdleBehaviorType.Land && dat != Info.CruiseAltitude)
					self.QueueActivity(new TakeOff(self));
				else if (Info.IdleBehavior == IdleBehaviorType.Land && Info.LandableTerrainTypes.Count > 0)
					self.QueueActivity(new Land(self));
				else
					self.QueueActivity(new FlyIdle(self));
			}
		}

		#region Implement IPositionable

		public bool CanExistInCell(CPos cell) { return true; }
		public bool IsLeavingCell(CPos location, SubCell subCell = SubCell.Any) { return false; } // TODO: Handle landing
		public bool CanEnterCell(CPos cell, Actor ignoreActor = null, BlockedByActor check = BlockedByActor.All) { return true; }
		public SubCell GetValidSubCell(SubCell preferred) { return SubCell.Invalid; }
		public SubCell GetAvailableSubCell(CPos a, SubCell preferredSubCell = SubCell.Any, Actor ignoreActor = null, BlockedByActor check = BlockedByActor.All)
		{
			// Does not use any subcell
			return SubCell.Invalid;
		}

		public void SetVisualPosition(Actor self, WPos pos) { SetPosition(self, pos); }

		// Changes position, but not altitude
		public void SetPosition(Actor self, CPos cell, SubCell subCell = SubCell.Any)
		{
			SetPosition(self, self.World.Map.CenterOfCell(cell) + new WVec(0, 0, CenterPosition.Z));
		}

		public void SetPosition(Actor self, WPos pos)
		{
			CenterPosition = pos;

			if (!self.IsInWorld)
				return;

			self.World.UpdateMaps(self, this);

			var altitude = self.World.Map.DistanceAboveTerrain(CenterPosition);

			var isAirborne = altitude.Length >= Info.MinAirborneAltitude;
			if (isAirborne && !airborne)
				OnAirborneAltitudeReached();
			else if (!isAirborne && airborne)
				OnAirborneAltitudeLeft();

			var isCruising = altitude == Info.CruiseAltitude;
			if (isCruising && !cruising)
				OnCruisingAltitudeReached();
			else if (!isCruising && cruising)
				OnCruisingAltitudeLeft();

			// NB: This can be called from the constructor before notifyVisualPositionChanged is assigned.
			if (notify && notifyVisualPositionChanged != null)
				foreach (var n in notifyVisualPositionChanged)
					n.VisualPositionChanged(self, 0, 0);

			FinishedMoving(self);
		}

		public void FinishedMoving(Actor self)
		{
			// Only make actor crush if it is on the ground
			if (!self.IsAtGroundLevel())
				return;

			var actors = self.World.ActorMap.GetActorsAt(TopLeft).Where(a => a != self).ToList();
			if (!AnyCrushables(actors))
				return;

			var notifiers = actors.SelectMany(a => a.TraitsImplementing<INotifyCrushed>().Select(t => new TraitPair<INotifyCrushed>(a, t)));
			foreach (var notifyCrushed in notifiers)
				notifyCrushed.Trait.OnCrush(notifyCrushed.Actor, self, Info.Crushes);
		}

		bool AnyCrushables(List<Actor> actors)
		{
			var crushables = actors.SelectMany(a => a.TraitsImplementing<ICrushable>().Select(t => new TraitPair<ICrushable>(a, t))).ToList();
			if (crushables.Count == 0)
				return false;

			foreach (var crushes in crushables)
				if (crushes.Trait.CrushableBy(crushes.Actor, self, Info.Crushes))
					return true;

			return false;
		}

		public void EnteringCell(Actor self)
		{
			var actors = self.World.ActorMap.GetActorsAt(TopLeft).Where(a => a != self).ToList();
			if (!AnyCrushables(actors))
				return;

			var notifiers = actors.SelectMany(a => a.TraitsImplementing<INotifyCrushed>().Select(t => new TraitPair<INotifyCrushed>(a, t)));
			foreach (var notifyCrushed in notifiers)
				notifyCrushed.Trait.WarnCrush(notifyCrushed.Actor, self, Info.Crushes);
		}

		public void AddInfluence(IEnumerable<CPos> landingCells)
		{
			this.landingCells = landingCells;
			if (self.IsInWorld)
				self.World.ActorMap.AddInfluence(self, this);
		}

		public void AddInfluence(CPos landingCell)
		{
			landingCells = new List<CPos> { landingCell };
			if (self.IsInWorld)
				self.World.ActorMap.AddInfluence(self, this);
		}

		public void RemoveInfluence()
		{
			if (self.IsInWorld)
				self.World.ActorMap.RemoveInfluence(self, this);

			landingCells = Enumerable.Empty<CPos>();
		}

		#endregion

		#region Implement IMove

		public Activity MoveTo(CPos cell, int nearEnough = 0, Actor ignoreActor = null,
			bool evaluateNearestMovableCell = false, Color? targetLineColor = null)
		{
			return new Fly(self, Target.FromCell(self.World, cell), WDist.FromCells(nearEnough), targetLineColor: targetLineColor);
		}

		public Activity MoveWithinRange(in Target target, WDist range,
			WPos? initialTargetPosition = null, Color? targetLineColor = null)
		{
			return new Fly(self, target, WDist.Zero, range, initialTargetPosition, targetLineColor);
		}

		public Activity MoveWithinRange(in Target target, WDist minRange, WDist maxRange,
			WPos? initialTargetPosition = null, Color? targetLineColor = null)
		{
			return new Fly(self, target, minRange, maxRange,
				initialTargetPosition, targetLineColor);
		}

		public Activity MoveFollow(Actor self, in Target target, WDist minRange, WDist maxRange,
			WPos? initialTargetPosition = null, Color? targetLineColor = null)
		{
			return new FlyFollow(self, target, minRange, maxRange,
				initialTargetPosition, targetLineColor);
		}

		public Activity ReturnToCell(Actor self) { return null; }

		class AssociateWithAirfieldActivity : Activity
		{
			readonly Actor self;
			readonly Aircraft aircraft;
			readonly int delay;

			public AssociateWithAirfieldActivity(Actor self, int delay = 0)
			{
				this.self = self;
				aircraft = self.Trait<Aircraft>();
				IsInterruptible = false;
				this.delay = delay;
			}

			protected override void OnFirstRun(Actor self)
			{
				var host = aircraft.GetActorBelow();
				if (host != null)
					aircraft.MakeReservation(host);

				if (delay > 0)
					QueueChild(new Wait(delay));
			}

			public override bool Tick(Actor self)
			{
				if (!aircraft.Info.TakeOffOnCreation)
					return true;

				if (self.World.Map.DistanceAboveTerrain(aircraft.CenterPosition).Length <= aircraft.LandAltitude.Length)
					QueueChild(new TakeOff(self));

				aircraft.UnReserve();
				return true;
			}
		}

		public Activity MoveToTarget(Actor self, in Target target,
			WPos? initialTargetPosition = null, Color? targetLineColor = null)
		{
			return new Fly(self, target, initialTargetPosition, targetLineColor);
		}

		public Activity MoveIntoTarget(Actor self, in Target target)
		{
			return new Land(self, target);
		}

		public Activity VisualMove(Actor self, WPos fromPos, WPos toPos)
		{
			// TODO: Ignore repulsion when moving
			var activities = new CallFunc(() => SetVisualPosition(self, fromPos));
			activities.Queue(new Fly(self, Target.FromPos(toPos)));
			return activities;
		}

		public int EstimatedMoveDuration(Actor self, WPos fromPos, WPos toPos)
		{
			var speed = MovementSpeed;
			return speed > 0 ? (toPos - fromPos).Length / speed : 0;
		}

		public CPos NearestMoveableCell(CPos cell) { return cell; }

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

		public bool CanEnterTargetNow(Actor self, in Target target)
		{
			// Lambdas can't use 'in' variables, so capture a copy for later
			var targetActor = target;
			if (target.Positions.Any(p => self.World.ActorMap.GetActorsAt(self.World.Map.CellContaining(p)).Any(a => a != self && a != targetActor.Actor)))
				return false;

			MakeReservation(target.Actor);
			return true;
		}

		#endregion

		#region Implement order interfaces

		public IEnumerable<IOrderTargeter> Orders
		{
			get
			{
				yield return new EnterAlliedActorTargeter<BuildingInfo>(
					"ForceEnter",
					6,
					Info.EnterCursor,
					Info.EnterBlockedCursor,
					(target, modifiers) => Info.CanForceLand && modifiers.HasModifier(TargetModifiers.ForceMove) && AircraftCanEnter(target),
					target => Reservable.IsAvailableFor(target, self) && AircraftCanResupplyAt(target, true));

				yield return new EnterAlliedActorTargeter<BuildingInfo>(
					"Enter",
					5,
					Info.EnterCursor,
					Info.EnterBlockedCursor,
					AircraftCanEnter,
					target => Reservable.IsAvailableFor(target, self) && AircraftCanResupplyAt(target, !Info.TakeOffOnResupply));

				yield return new AircraftMoveOrderTargeter(this);
			}
		}

		public Order IssueOrder(Actor self, IOrderTargeter order, in Target target, bool queued)
		{
			if (!IsTraitDisabled &&
				(order.OrderID == "Enter" || order.OrderID == "Move" || order.OrderID == "Land" || order.OrderID == "ForceEnter"))
				return new Order(order.OrderID, self, target, queued);

			return null;
		}

		Order IIssueDeployOrder.IssueDeployOrder(Actor self, bool queued)
		{
			if (IsTraitDisabled || rearmable == null || !rearmable.Info.RearmActors.Any())
				return null;

			return new Order("ReturnToBase", self, queued);
		}

		bool IIssueDeployOrder.CanIssueDeployOrder(Actor self, bool queued) { return rearmable != null && rearmable.Info.RearmActors.Any(); }

		public string VoicePhraseForOrder(Actor self, Order order)
		{
			if (IsTraitDisabled)
				return null;

			switch (order.OrderString)
			{
				case "Land":
				case "Move":
					if (!Info.MoveIntoShroud && order.Target.Type != TargetType.Invalid)
					{
						var cell = self.World.Map.CellContaining(order.Target.CenterPosition);
						if (!self.Owner.Shroud.IsExplored(cell))
							return null;
					}

					return Info.Voice;
				case "Enter":
				case "ForceEnter":
				case "Stop":
				case "Scatter":
					return Info.Voice;
				case "ReturnToBase":
					return rearmable != null && rearmable.Info.RearmActors.Any() ? Info.Voice : null;
				default: return null;
			}
		}

		public void ResolveOrder(Actor self, Order order)
		{
			if (IsTraitDisabled)
				return;

			var orderString = order.OrderString;
			if (orderString == "Move")
			{
				var cell = self.World.Map.Clamp(self.World.Map.CellContaining(order.Target.CenterPosition));
				if (!Info.MoveIntoShroud && !self.Owner.Shroud.IsExplored(cell))
					return;

				if (!order.Queued)
					UnReserve();

				var target = Target.FromCell(self.World, cell);

				// TODO: this should scale with unit selection group size.
				self.QueueActivity(order.Queued, new Fly(self, target, WDist.FromCells(8), targetLineColor: Info.TargetLineColor));
				self.ShowTargetLines();
			}
			else if (orderString == "Land")
			{
				var cell = self.World.Map.Clamp(self.World.Map.CellContaining(order.Target.CenterPosition));
				if (!Info.MoveIntoShroud && !self.Owner.Shroud.IsExplored(cell))
					return;

				if (!order.Queued)
					UnReserve();

				var target = Target.FromCell(self.World, cell);

				self.QueueActivity(order.Queued, new Land(self, target, targetLineColor: Info.TargetLineColor));
				self.ShowTargetLines();
			}
			else if (orderString == "Enter" || orderString == "ForceEnter" || orderString == "Repair")
			{
				// Enter, ForceEnter and Repair orders are only valid for own/allied actors,
				// which are guaranteed to never be frozen.
				if (order.Target.Type != TargetType.Actor)
					return;

				var targetActor = order.Target.Actor;
				var isForceEnter = orderString == "ForceEnter";
				var canResupplyAt = AircraftCanResupplyAt(targetActor, isForceEnter || !Info.TakeOffOnResupply);

				// This is what the order targeter checks to display the correct cursor, so we need to make sure
				// the behavior matches the cursor if the player clicks despite a "blocked" cursor.
				if (!canResupplyAt || !Reservable.IsAvailableFor(targetActor, self))
					return;

				if (!order.Queued)
					UnReserve();

				// Aircraft with TakeOffOnResupply would immediately take off again, so there's no point in automatically forcing
				// them to land on a resupplier. For aircraft without it, it makes more sense to land than to idle above a
				// free resupplier.
				var forceLand = isForceEnter || !Info.TakeOffOnResupply;
				self.QueueActivity(order.Queued, new ReturnToBase(self, targetActor, forceLand));
				self.ShowTargetLines();
			}
			else if (orderString == "Stop")
			{
				// We don't want the Stop order to cancel a running Resupply activity.
				// Resupply is always either the main activity or a child of ReturnToBase.
				if (self.CurrentActivity is Resupply ||
					(self.CurrentActivity is ReturnToBase && GetActorBelow() != null))
					return;

				self.CancelActivity();
				UnReserve();
			}
			else if (orderString == "ReturnToBase")
			{
				// Do nothing if not rearmable and don't restart activity every time deploy hotkey is triggered
				if (rearmable == null || !rearmable.Info.RearmActors.Any() || self.CurrentActivity is ReturnToBase || GetActorBelow() != null)
					return;

				if (!order.Queued)
					UnReserve();

				// Aircraft with TakeOffOnResupply would immediately take off again, so there's no point in forcing them to land
				// on a resupplier. For aircraft without it, it makes more sense to land than to idle above a free resupplier.
				self.QueueActivity(order.Queued, new ReturnToBase(self, null, !Info.TakeOffOnResupply));
				self.ShowTargetLines();
			}
			else if (orderString == "Scatter")
				Nudge(self);
		}

		#endregion

		void Nudge(Actor self)
		{
			if (IsTraitDisabled || IsTraitPaused || requireForceMove)
				return;

			// Disable nudging if the aircraft is outside the map
			if (!self.World.Map.Contains(self.Location))
				return;

			var offset = new WVec(0, -self.World.SharedRandom.Next(512, 2048), 0)
				.Rotate(WRot.FromFacing(self.World.SharedRandom.Next(256)));
			var target = Target.FromPos(self.CenterPosition + offset);

			self.QueueActivity(false, new Fly(self, target));
			self.ShowTargetLines();
			UnReserve();
		}

		#region Airborne conditions

		void OnAirborneAltitudeReached()
		{
			if (airborne)
				return;

			airborne = true;
			if (airborneToken == Actor.InvalidConditionToken)
				airborneToken = self.GrantCondition(Info.AirborneCondition);
		}

		void OnAirborneAltitudeLeft()
		{
			if (!airborne)
				return;

			airborne = false;
			if (airborneToken != Actor.InvalidConditionToken)
				airborneToken = self.RevokeCondition(airborneToken);
		}

		#endregion

		#region Cruising conditions

		void OnCruisingAltitudeReached()
		{
			if (cruising)
				return;

			cruising = true;
			if (cruisingToken == Actor.InvalidConditionToken)
				cruisingToken = self.GrantCondition(Info.CruisingCondition);
		}

		void OnCruisingAltitudeLeft()
		{
			if (!cruising)
				return;

			cruising = false;
			if (cruisingToken != Actor.InvalidConditionToken)
				cruisingToken = self.RevokeCondition(cruisingToken);
		}

		#endregion

		void INotifyActorDisposing.Disposing(Actor self)
		{
			UnReserve();
		}

		void IActorPreviewInitModifier.ModifyActorPreviewInit(Actor self, TypeDictionary inits)
		{
			if (!inits.Contains<DynamicFacingInit>() && !inits.Contains<FacingInit>())
				inits.Add(new DynamicFacingInit(() => Facing));
		}

		Activity ICreationActivity.GetCreationActivity()
		{
			return new AssociateWithAirfieldActivity(self, creationActivityDelay);
		}

		public class AircraftMoveOrderTargeter : IOrderTargeter
		{
			readonly Aircraft aircraft;

			public string OrderID { get; protected set; }
			public int OrderPriority { get { return 4; } }
			public bool IsQueued { get; protected set; }

			public AircraftMoveOrderTargeter(Aircraft aircraft)
			{
				this.aircraft = aircraft;
				OrderID = "Move";
			}

			public bool TargetOverridesSelection(Actor self, in Target target, List<Actor> actorsAt, CPos xy, TargetModifiers modifiers)
			{
				// Always prioritise orders over selecting other peoples actors or own actors that are already selected
				if (target.Type == TargetType.Actor && (target.Actor.Owner != self.Owner || self.World.Selection.Contains(target.Actor)))
					return true;

				return modifiers.HasModifier(TargetModifiers.ForceMove);
			}

			public virtual bool CanTarget(Actor self, in Target target, List<Actor> othersAtTarget, ref TargetModifiers modifiers, ref string cursor)
			{
				if (target.Type != TargetType.Terrain || (aircraft.requireForceMove && !modifiers.HasModifier(TargetModifiers.ForceMove)))
					return false;

				var location = self.World.Map.CellContaining(target.CenterPosition);

				// Aircraft can be force-landed by issuing a force-move order on a clear terrain cell
				// Cells that contain a blocking building are treated as regular force move orders, overriding
				// selection for left-mouse orders
				if (modifiers.HasModifier(TargetModifiers.ForceMove) && aircraft.Info.CanForceLand)
				{
					var buildingAtLocation = self.World.ActorMap.GetActorsAt(location)
						.Any(a => a.TraitOrDefault<Building>() != null && a.TraitOrDefault<Selectable>() != null);

					if (!buildingAtLocation || aircraft.CanLand(location, blockedByMobile: false))
						OrderID = "Land";
				}

				IsQueued = modifiers.HasModifier(TargetModifiers.ForceQueue);

				var explored = self.Owner.Shroud.IsExplored(location);
				cursor = !aircraft.IsTraitPaused && (explored || aircraft.Info.MoveIntoShroud) && self.World.Map.Contains(location) ?
					(self.World.Map.GetTerrainInfo(location).CustomCursor ?? "move") :
					"move-blocked";

				return true;
			}
		}
	}
}
