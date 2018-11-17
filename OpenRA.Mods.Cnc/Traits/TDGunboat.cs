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

using System.Collections.Generic;
using System.Linq;
using OpenRA.Activities;
using OpenRA.Mods.Common;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Cnc.Traits
{
	public class TDGunboatInfo : ITraitInfo, IPositionableInfo, IFacingInfo, IMoveInfo,
		UsesInit<LocationInit>, UsesInit<FacingInit>, IActorPreviewInitInfo
	{
		public readonly int Speed = 28;

		[Desc("Facing to use when actor spawns. Only 64 and 192 supported.")]
		public readonly int InitialFacing = 64;

		[Desc("Facing to use for actor previews (map editor, color picker, etc). Only 64 and 192 supported.")]
		public readonly int PreviewFacing = 64;

		public virtual object Create(ActorInitializer init) { return new TDGunboat(init, this); }

		public int GetInitialFacing() { return InitialFacing; }

		IEnumerable<object> IActorPreviewInitInfo.ActorPreviewInits(ActorInfo ai, ActorPreviewType type)
		{
			yield return new FacingInit(PreviewFacing);
		}

		public IReadOnlyDictionary<CPos, SubCell> OccupiedCells(ActorInfo info, CPos location, SubCell subCell = SubCell.Any)
		{
			var occupied = new Dictionary<CPos, SubCell>() { { location, SubCell.FullCell } };
			return new ReadOnlyDictionary<CPos, SubCell>(occupied);
		}

		bool IOccupySpaceInfo.SharesCell { get { return false; } }

		// Used to determine if actor can spawn
		public bool CanEnterCell(World world, Actor self, CPos cell, Actor ignoreActor = null, bool checkTransientActors = false)
		{
			if (!world.Map.Contains(cell))
				return false;

			return true;
		}
	}

	public class TDGunboat : ITick, ISync, IFacing, IPositionable, IMove, IDeathActorInitModifier,
		INotifyCreated, INotifyAddedToWorld, INotifyRemovedFromWorld, IActorPreviewInitModifier
	{
		public readonly TDGunboatInfo Info;
		readonly Actor self;

		IEnumerable<int> speedModifiers;

		[Sync] public int Facing { get; set; }
		[Sync] public WPos CenterPosition { get; private set; }
		public CPos TopLeft { get { return self.World.Map.CellContaining(CenterPosition); } }

		// Isn't used anyway
		public int TurnSpeed { get { return 255; } }

		CPos cachedLocation;

		public TDGunboat(ActorInitializer init, TDGunboatInfo info)
		{
			Info = info;
			self = init.Self;

			if (init.Contains<LocationInit>())
				SetPosition(self, init.Get<LocationInit, CPos>());

			if (init.Contains<CenterPositionInit>())
				SetPosition(self, init.Get<CenterPositionInit, WPos>());

			Facing = init.Contains<FacingInit>() ? init.Get<FacingInit, int>() : Info.GetInitialFacing();

			// Prevent mappers from setting bogus facings
			if (Facing != 64 && Facing != 192)
				Facing = Facing > 127 ? 192 : 64;
		}

		void INotifyCreated.Created(Actor self)
		{
			speedModifiers = self.TraitsImplementing<ISpeedModifier>().ToArray().Select(sm => sm.GetSpeedModifier());
			cachedLocation = self.Location;
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

			SetVisualPosition(self, self.CenterPosition + MoveStep(Facing));
		}

		void Turn()
		{
			if (Facing == 64)
				Facing = 192;
			else
				Facing = 64;
		}

		int MovementSpeed
		{
			get { return Util.ApplyPercentageModifiers(Info.Speed, speedModifiers); }
		}

		public Pair<CPos, SubCell>[] OccupiedCells() { return new[] { Pair.New(TopLeft, SubCell.FullCell) }; }

		WVec MoveStep(int facing)
		{
			return MoveStep(MovementSpeed, facing);
		}

		WVec MoveStep(int speed, int facing)
		{
			var dir = new WVec(0, -1024, 0).Rotate(WRot.FromFacing(facing));
			return speed * dir / 1024;
		}

		void IDeathActorInitModifier.ModifyDeathActorInit(Actor self, TypeDictionary init)
		{
			init.Add(new FacingInit(Facing));
		}

		public bool CanExistInCell(CPos cell) { return true; }
		public bool IsLeavingCell(CPos location, SubCell subCell = SubCell.Any) { return false; }
		public bool CanEnterCell(CPos cell, Actor ignoreActor = null, bool checkTransientActors = false) { return true; }
		public SubCell GetValidSubCell(SubCell preferred) { return SubCell.Invalid; }
		public SubCell GetAvailableSubCell(CPos a, SubCell preferredSubCell = SubCell.Any, Actor ignoreActor = null, bool checkTransientActors = true)
		{
			// Does not use any subcell
			return SubCell.Invalid;
		}

		public void SetVisualPosition(Actor self, WPos pos) { SetPosition(self, pos); }

		public void SetPosition(Actor self, CPos cell, SubCell subCell = SubCell.Any)
		{
			SetPosition(self, self.World.Map.CenterOfCell(cell));
		}

		public void SetPosition(Actor self, WPos pos)
		{
			CenterPosition = pos;

			if (!self.IsInWorld)
				return;

			self.World.UpdateMaps(self, this);
		}

		public Activity MoveTo(CPos cell, int nearEnough) { return null; }
		public Activity MoveTo(CPos cell, Actor ignoreActor) { return null; }
		public Activity MoveWithinRange(Target target, WDist range) { return null; }
		public Activity MoveWithinRange(Target target, WDist minRange, WDist maxRange) { return null; }
		public Activity MoveFollow(Actor self, Target target, WDist minRange, WDist maxRange) { return null; }
		public Activity MoveIntoWorld(Actor self, CPos cell, SubCell subCell = SubCell.Any) { return null; }
		public Activity MoveToTarget(Actor self, Target target) { return null; }
		public Activity MoveIntoTarget(Actor self, Target target) { return null; }
		public Activity VisualMove(Actor self, WPos fromPos, WPos toPos) { return null; }

		public int EstimatedMoveDuration(Actor self, WPos fromPos, WPos toPos)
		{
			return (toPos - fromPos).Length / Info.Speed;
		}

		public CPos NearestMoveableCell(CPos cell) { return cell; }

		// Actors with TDGunboat always move
		public bool IsMoving { get { return true; } set { } }

		public bool IsMovingVertically { get { return false; } set { } }

		public bool CanEnterTargetNow(Actor self, Target target)
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
