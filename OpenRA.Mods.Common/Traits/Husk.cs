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
		public readonly int PreviewFacing = 96;

		IEnumerable<object> IActorPreviewInitInfo.ActorPreviewInits(ActorInfo ai, ActorPreviewType type)
		{
			yield return new FacingInit(PreviewFacing);
		}

		public override object Create(ActorInitializer init) { return new Husk(init, this); }

		public int GetInitialFacing() { return 128; }

		public IReadOnlyDictionary<CPos, SubCell> OccupiedCells(ActorInfo info, CPos location, SubCell subCell = SubCell.Any)
		{
			var occupied = new Dictionary<CPos, SubCell>() { { location, SubCell.FullCell } };
			return new ReadOnlyDictionary<CPos, SubCell>(occupied);
		}

		bool IOccupySpaceInfo.SharesCell { get { return false; } }

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

		INotifyVisualPositionChanged[] notifyVisualPositionChanged;

		[Sync]
		public CPos TopLeft { get; private set; }

		[Sync]
		public WPos CenterPosition { get; private set; }

		[Sync]
		public int Facing { get; set; }

		public int TurnSpeed { get { return 0; } }

		public Husk(ActorInitializer init, HuskInfo info)
		{
			this.info = info;
			self = init.Self;

			TopLeft = init.GetValue<LocationInit, CPos>(info);
			CenterPosition = init.GetValue<CenterPositionInit, WPos>(info, init.World.Map.CenterOfCell(TopLeft));
			Facing = init.GetValue<FacingInit, int>(info, 128);

			dragSpeed = init.GetValue<HuskSpeedInit, int>(info, 0);
			finalPosition = init.World.Map.CenterOfCell(TopLeft);

			effectiveOwner = init.GetValue<EffectiveOwnerInit, Player>(info, self.Owner);
		}

		void INotifyCreated.Created(Actor self)
		{
			var distance = (finalPosition - CenterPosition).Length;
			if (dragSpeed > 0 && distance > 0)
				self.QueueActivity(new Drag(self, CenterPosition, finalPosition, distance / dragSpeed));

			notifyVisualPositionChanged = self.TraitsImplementing<INotifyVisualPositionChanged>().ToArray();
		}

		public bool CanExistInCell(CPos cell)
		{
			if (!self.World.Map.Contains(cell))
				return false;

			if (!info.AllowedTerrain.Contains(self.World.Map.GetTerrainInfo(cell).Type))
				return false;

			return true;
		}

		public Pair<CPos, SubCell>[] OccupiedCells() { return new[] { Pair.New(TopLeft, SubCell.FullCell) }; }
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

		public void SetVisualPosition(Actor self, WPos pos)
		{
			CenterPosition = pos;
			self.World.ScreenMap.AddOrUpdate(self);

			// This can be called from the constructor before notifyVisualPositionChanged is assigned.
			if (notifyVisualPositionChanged != null)
				foreach (var n in notifyVisualPositionChanged)
					n.VisualPositionChanged(self, 0, 0);
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
		bool IEffectiveOwner.Disguised { get { return true; } }
		Player IEffectiveOwner.Owner { get { return effectiveOwner; } }
	}

	public class HuskSpeedInit : IActorInit<int>
	{
		[FieldFromYamlKey]
		readonly int value = 0;

		public HuskSpeedInit() { }
		public HuskSpeedInit(int init) { value = init; }
		public int Value { get { return value; } }
	}
}
