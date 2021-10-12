#region Copyright & License Information
/*
 * Copyright 2007-2021 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Linq;
using OpenRA.Mods.Common.Activities;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Spawns remains of a husk actor with the correct facing.")]
	public class HuskInfo : TraitInfo, IPositionableInfo, IFacingInfo, IActorPreviewInitInfo
	{
		public readonly HashSet<string> AllowedTerrain = new HashSet<string>();

		[Desc("Facing to use for actor previews (map editor, color picker, etc)")]
		public readonly WAngle PreviewFacing = new WAngle(384);

		IEnumerable<ActorInit> IActorPreviewInitInfo.ActorPreviewInits(ActorInfo ai, ActorPreviewType type)
		{
			yield return new FacingInit(PreviewFacing);
		}

		public override object Create(ActorInitializer init) { return new Husk(init, this); }

		public WAngle GetInitialFacing() { return new WAngle(512); }

		public IReadOnlyDictionary<CPos, SubCell> OccupiedCells(ActorInfo info, CPos location, SubCell subCell = SubCell.Any)
		{
			return new Dictionary<CPos, SubCell>() { { location, SubCell.FullCell } };
		}

		bool IOccupySpaceInfo.SharesCell => false;

		public bool CanEnterCell(World world, Actor self, CPos cell, SubCell subCell = SubCell.FullCell, Actor ignoreActor = null, BlockedByActor check = BlockedByActor.All)
		{
			// IPositionable*Info*.CanEnterCell is only ever used for things like exiting production facilities,
			// all places relevant for husks check IPositionable.CanEnterCell instead, so we can safely set this to true.
			return true;
		}
	}

	public class Husk : IPositionable, IFacing, ISync, INotifyCreated, INotifyAddedToWorld, INotifyRemovedFromWorld,
		IDeathActorInitModifier, IEffectiveOwner
	{
		readonly Actor self;
		readonly HuskInfo info;
		readonly Player effectiveOwner;

		readonly int dragSpeed;
		readonly WPos finalPosition;

		INotifyCenterPositionChanged[] notifyCenterPositionChanged;

		[Sync]
		public CPos TopLeft { get; private set; }

		[Sync]
		public WPos CenterPosition { get; private set; }

		WRot orientation;

		[Sync]
		public WAngle Facing
		{
			get => orientation.Yaw;
			set => orientation = orientation.WithYaw(value);
		}

		public WRot Orientation => orientation;

		public WAngle TurnSpeed => WAngle.Zero;

		public Husk(ActorInitializer init, HuskInfo info)
		{
			this.info = info;
			self = init.Self;

			TopLeft = init.GetValue<LocationInit, CPos>();
			CenterPosition = init.GetValue<CenterPositionInit, WPos>(init.World.Map.CenterOfCell(TopLeft));
			Facing = init.GetValue<FacingInit, WAngle>(info.GetInitialFacing());

			dragSpeed = init.GetValue<HuskSpeedInit, int>(0);
			finalPosition = init.World.Map.CenterOfCell(TopLeft);

			effectiveOwner = init.GetValue<EffectiveOwnerInit, Player>(info, self.Owner);
		}

		void INotifyCreated.Created(Actor self)
		{
			var distance = (finalPosition - CenterPosition).Length;
			if (dragSpeed > 0 && distance > 0)
				self.QueueActivity(new Drag(self, CenterPosition, finalPosition, distance / dragSpeed));

			notifyCenterPositionChanged = self.TraitsImplementing<INotifyCenterPositionChanged>().ToArray();
		}

		public bool CanExistInCell(CPos cell)
		{
			if (!self.World.Map.Contains(cell))
				return false;

			if (!info.AllowedTerrain.Contains(self.World.Map.GetTerrainInfo(cell).Type))
				return false;

			return true;
		}

		public (CPos, SubCell)[] OccupiedCells() { return new[] { (TopLeft, SubCell.FullCell) }; }
		public bool IsLeavingCell(CPos location, SubCell subCell = SubCell.Any) { return false; }
		public SubCell GetValidSubCell(SubCell preferred = SubCell.Any) { return SubCell.FullCell; }
		public SubCell GetAvailableSubCell(CPos cell, SubCell preferredSubCell = SubCell.Any, Actor ignoreActor = null, BlockedByActor check = BlockedByActor.All)
		{
			if (!CanExistInCell(cell))
				return SubCell.Invalid;

			if (check == BlockedByActor.None)
				return SubCell.FullCell;

			return self.World.ActorMap.GetActorsAt(cell)
				.All(x => x == ignoreActor) ? SubCell.FullCell : SubCell.Invalid;
		}

		public bool CanEnterCell(CPos a, Actor ignoreActor = null, BlockedByActor check = BlockedByActor.All)
		{
			return GetAvailableSubCell(a, SubCell.Any, ignoreActor, check) != SubCell.Invalid;
		}

		public void SetPosition(Actor self, CPos cell, SubCell subCell = SubCell.Any) { SetPosition(self, self.World.Map.CenterOfCell(cell)); }

		public void SetCenterPosition(Actor self, WPos pos)
		{
			CenterPosition = pos;
			self.World.ScreenMap.AddOrUpdate(self);

			// This can be called from the constructor before notifyCenterPositionChanged is assigned.
			if (notifyCenterPositionChanged != null)
				foreach (var n in notifyCenterPositionChanged)
					n.CenterPositionChanged(self, 0, 0);
		}

		public void SetPosition(Actor self, WPos pos)
		{
			self.World.ActorMap.RemoveInfluence(self, this);
			CenterPosition = pos;
			TopLeft = self.World.Map.CellContaining(pos);
			self.World.ActorMap.AddInfluence(self, this);

			self.World.UpdateMaps(self, this);
		}

		void INotifyAddedToWorld.AddedToWorld(Actor self)
		{
			self.World.AddToMaps(self, this);
		}

		void INotifyRemovedFromWorld.RemovedFromWorld(Actor self)
		{
			self.World.RemoveFromMaps(self, this);
		}

		void IDeathActorInitModifier.ModifyDeathActorInit(Actor self, TypeDictionary init)
		{
			init.Add(new FacingInit(Facing));
		}

		// We return self.Owner if there's no effective owner
		bool IEffectiveOwner.Disguised => true;
		Player IEffectiveOwner.Owner => effectiveOwner;
	}

	public class HuskSpeedInit : ValueActorInit<int>, ISingleInstanceInit
	{
		public HuskSpeedInit(int value)
			: base(value) { }
	}
}
