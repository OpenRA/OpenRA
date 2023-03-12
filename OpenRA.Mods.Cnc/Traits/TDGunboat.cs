#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Linq;
using OpenRA.Activities;
using OpenRA.Mods.Common;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Cnc.Traits
{
	public class TDGunboatInfo : TraitInfo, IPositionableInfo, IFacingInfo, IMoveInfo, IActorPreviewInitInfo
	{
		public readonly int Speed = 28;

		[Desc("Facing to use when actor spawns. Only 256 and 768 supported.")]
		public readonly WAngle InitialFacing = new(256);

		[Desc("Facing to use for actor previews (map editor, color picker, etc). Only 256 and 768 supported.")]
		public readonly WAngle PreviewFacing = new(256);

		public override object Create(ActorInitializer init) { return new TDGunboat(init, this); }

		public WAngle GetInitialFacing() { return InitialFacing; }
		public Color GetTargetLineColor() { return Color.Green; }

		IEnumerable<ActorInit> IActorPreviewInitInfo.ActorPreviewInits(ActorInfo ai, ActorPreviewType type)
		{
			yield return new FacingInit(PreviewFacing);
		}

		public IReadOnlyDictionary<CPos, SubCell> OccupiedCells(ActorInfo info, CPos location, SubCell subCell = SubCell.Any)
		{
			return new Dictionary<CPos, SubCell>() { { location, SubCell.FullCell } };
		}

		bool IOccupySpaceInfo.SharesCell => false;

		// Used to determine if actor can spawn
		public bool CanEnterCell(World world, Actor self, CPos cell, SubCell subCell = SubCell.FullCell, Actor ignoreActor = null, BlockedByActor check = BlockedByActor.All)
		{
			return world.Map.Contains(cell);
		}
	}

	public class TDGunboat : ITick, ISync, IFacing, IPositionable, IMove, IDeathActorInitModifier,
		INotifyCreated, INotifyAddedToWorld, INotifyRemovedFromWorld, IActorPreviewInitModifier
	{
		public readonly TDGunboatInfo Info;
		readonly Actor self;
		static readonly WAngle Left = new(256);
		static readonly WAngle Right = new(768);

		IEnumerable<int> speedModifiers;
		INotifyCenterPositionChanged[] notifyCenterPositionChanged;

		[Sync]
		public WAngle Facing
		{
			get => Orientation.Yaw;
			set => Orientation = Orientation.WithYaw(value);
		}

		public WRot Orientation { get; private set; }

		[Sync]
		public WPos CenterPosition { get; private set; }

		public CPos TopLeft => self.World.Map.CellContaining(CenterPosition);

		// Isn't used anyway
		public WAngle TurnSpeed => WAngle.Zero;

		CPos cachedLocation;

		public TDGunboat(ActorInitializer init, TDGunboatInfo info)
		{
			Info = info;
			self = init.Self;

			var locationInit = init.GetOrDefault<LocationInit>();
			if (locationInit != null)
				SetPosition(self, locationInit.Value);

			var centerPositionInit = init.GetOrDefault<CenterPositionInit>();
			if (centerPositionInit != null)
				SetPosition(self, centerPositionInit.Value);

			Facing = init.GetValue<FacingInit, WAngle>(Info.GetInitialFacing());

			// Prevent mappers from setting bogus facings
			if (Facing != Left && Facing != Right)
				Facing = Facing.Angle > 511 ? Right : Left;
		}

		void INotifyCreated.Created(Actor self)
		{
			speedModifiers = self.TraitsImplementing<ISpeedModifier>().ToArray().Select(sm => sm.GetSpeedModifier());
			cachedLocation = self.Location;
			notifyCenterPositionChanged = self.TraitsImplementing<INotifyCenterPositionChanged>().ToArray();
		}

		void INotifyAddedToWorld.AddedToWorld(Actor self)
		{
			self.World.AddToMaps(self, this);
		}

		void INotifyRemovedFromWorld.RemovedFromWorld(Actor self)
		{
			self.World.RemoveFromMaps(self, this);
		}

		void ITick.Tick(Actor self)
		{
			if (cachedLocation != self.Location)
			{
				// If the actor just left the map, switch facing
				if (!self.World.Map.Contains(self.Location))
					Turn();
			}

			cachedLocation = self.Location;

			SetCenterPosition(self, self.CenterPosition + MoveStep(Facing));
		}

		void Turn()
		{
			Facing = Facing == Left ? Right : Left;
		}

		int MovementSpeed => Common.Util.ApplyPercentageModifiers(Info.Speed, speedModifiers);

		public (CPos, SubCell)[] OccupiedCells() { return new[] { (TopLeft, SubCell.FullCell) }; }

		WVec MoveStep(WAngle facing)
		{
			return MoveStep(MovementSpeed, facing);
		}

		static WVec MoveStep(int speed, WAngle facing)
		{
			var dir = new WVec(0, -1024, 0).Rotate(WRot.FromYaw(facing));
			return speed * dir / 1024;
		}

		void IDeathActorInitModifier.ModifyDeathActorInit(Actor self, TypeDictionary init)
		{
			init.Add(new FacingInit(Facing));
		}

		public bool CanExistInCell(CPos cell) { return true; }
		public bool IsLeavingCell(CPos location, SubCell subCell = SubCell.Any) { return false; }
		public bool CanEnterCell(CPos cell, Actor ignoreActor = null, BlockedByActor check = BlockedByActor.All) { return true; }
		public SubCell GetValidSubCell(SubCell preferred) { return SubCell.Invalid; }
		public SubCell GetAvailableSubCell(CPos a, SubCell preferredSubCell = SubCell.Any, Actor ignoreActor = null, BlockedByActor check = BlockedByActor.All)
		{
			// Does not use any subcell
			return SubCell.Invalid;
		}

		public void SetCenterPosition(Actor self, WPos pos) { SetPosition(self, pos); }

		public void SetPosition(Actor self, CPos cell, SubCell subCell = SubCell.Any)
		{
			SetPosition(self, self.World.Map.CenterOfCell(cell));
		}

		public void SetPosition(Actor self, WPos pos)
		{
			if (self.IsInWorld)
				self.World.ActorMap.RemoveInfluence(self, this);

			CenterPosition = pos;

			if (!self.IsInWorld)
				return;

			self.World.UpdateMaps(self, this);
			self.World.ActorMap.AddInfluence(self, this);

			// This can be called from the constructor before notifyCenterPositionChanged is assigned.
			if (notifyCenterPositionChanged != null)
				foreach (var n in notifyCenterPositionChanged)
					n.CenterPositionChanged(self, 0, 0);
		}

		public Activity MoveTo(CPos cell, int nearEnough = 0, Actor ignoreActor = null,
			bool evaluateNearestMovableCell = false, Color? targetLineColor = null) { return null; }
		public Activity MoveWithinRange(in Target target, WDist range,
			WPos? initialTargetPosition = null, Color? targetLineColor = null) { return null; }
		public Activity MoveWithinRange(in Target target, WDist minRange, WDist maxRange,
			WPos? initialTargetPosition = null, Color? targetLineColor = null) { return null; }
		public Activity MoveFollow(Actor self, in Target target, WDist minRange, WDist maxRange,
			WPos? initialTargetPosition = null, Color? targetLineColor = null) { return null; }
		public Activity ReturnToCell(Actor self) { return null; }
		public Activity MoveToTarget(Actor self, in Target target,
			WPos? initialTargetPosition = null, Color? targetLineColor = null) { return null; }
		public Activity MoveOntoTarget(Actor self, in Target target, in WVec offset,
			WAngle? facing, Color? targetLineColor = null) { return null; }
		public Activity MoveIntoTarget(Actor self, in Target target) { return null; }
		public Activity LocalMove(Actor self, WPos fromPos, WPos toPos) { return null; }

		public int EstimatedMoveDuration(Actor self, WPos fromPos, WPos toPos)
		{
			return (toPos - fromPos).Length / Info.Speed;
		}

		public CPos NearestMoveableCell(CPos cell) { return cell; }

		// Actors with TDGunboat always move
		public MovementType CurrentMovementTypes { get => MovementType.Horizontal; set { } }

		public bool CanEnterTargetNow(Actor self, in Target target)
		{
			return false;
		}

		void IActorPreviewInitModifier.ModifyActorPreviewInit(Actor self, TypeDictionary inits)
		{
			if (!inits.Contains<DynamicFacingInit>() && !inits.Contains<FacingInit>())
				inits.Add(new DynamicFacingInit(() => Facing));
		}
	}
}
