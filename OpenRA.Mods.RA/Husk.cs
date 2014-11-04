#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Linq;
using OpenRA.Mods.RA.Move;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	[Desc("Spawns remains of a husk actor with the correct facing.")]
	class HuskInfo : ITraitInfo, IOccupySpaceInfo, IFacingInfo
	{
		public readonly string[] AllowedTerrain = { };

		public object Create(ActorInitializer init) { return new Husk(init, this); }

		public int GetInitialFacing() { return 128; }
	}

	class Husk : IPositionable, IFacing, ISync, INotifyCreated, INotifyAddedToWorld, INotifyRemovedFromWorld, IDisable
	{
		readonly HuskInfo info;
		readonly Actor self;

		readonly int dragSpeed;
		readonly WPos finalPosition;

		[Sync] public CPos TopLeft { get; private set; }
		[Sync] public WPos CenterPosition { get; private set; }
		[Sync] public int Facing { get; set; }

		public int ROT { get { return 0; } }

		public Husk(ActorInitializer init, HuskInfo info)
		{
			this.info = info;
			this.self = init.self;

			TopLeft = init.Get<LocationInit, CPos>();
			CenterPosition = init.Contains<CenterPositionInit>() ? init.Get<CenterPositionInit, WPos>() : init.world.Map.CenterOfCell(TopLeft);
			Facing = init.Contains<FacingInit>() ? init.Get<FacingInit, int>() : 128;

			dragSpeed = init.Contains<HuskSpeedInit>() ? init.Get<HuskSpeedInit, int>() : 0;
			finalPosition = init.world.Map.CenterOfCell(TopLeft);
		}

		public void Created(Actor self)
		{
			var distance = (finalPosition - CenterPosition).Length;
			if (dragSpeed > 0 && distance > 0)
				self.QueueActivity(new Drag(self, CenterPosition, finalPosition, distance / dragSpeed));
		}

		public IEnumerable<Pair<CPos, SubCell>> OccupiedCells() { yield return Pair.New(TopLeft, SubCell.FullCell); }
		public bool IsLeavingCell(CPos location, SubCell subCell = SubCell.Any) { return false; }
		public SubCell GetValidSubCell(SubCell preferred = SubCell.Any) { return SubCell.FullCell; }
		public SubCell GetAvailableSubCell(CPos cell, SubCell preferredSubCell = SubCell.Any, Actor ignoreActor = null, bool checkTransientActors = true)
		{
			if (!self.World.Map.Contains(cell))
				return SubCell.Invalid;

			if (!info.AllowedTerrain.Contains(self.World.Map.GetTerrainInfo(cell).Type))
				return SubCell.Invalid;

			if (!checkTransientActors)
				return SubCell.FullCell;

			return !self.World.ActorMap.GetUnitsAt(cell)
				.Where(x => x != ignoreActor)
				.Any() ? SubCell.FullCell : SubCell.Invalid;
		}

		public bool CanEnterCell(CPos a, Actor ignoreActor = null, bool checkTransientActors = true)
		{
			return GetAvailableSubCell(a, SubCell.Any, ignoreActor, checkTransientActors) != SubCell.Invalid;
		}

		public void SetPosition(Actor self, CPos cell, SubCell subCell = SubCell.Any) { SetPosition(self, self.World.Map.CenterOfCell(cell)); }

		public void SetVisualPosition(Actor self, WPos pos)
		{
			CenterPosition = pos;
			self.World.ScreenMap.Update(self);
		}

		public void SetPosition(Actor self, WPos pos)
		{
			self.World.ActorMap.RemoveInfluence(self, this);
			CenterPosition = pos;
			TopLeft = self.World.Map.CellContaining(pos);
			self.World.ActorMap.AddInfluence(self, this);
			self.World.ActorMap.UpdatePosition(self, this);
			self.World.ScreenMap.Update(self);
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

		public bool Disabled
		{
			get { return true; }
		}
	}

	public class HuskSpeedInit : IActorInit<int>
	{
		[FieldFromYamlKey] readonly int value = 0;
		public HuskSpeedInit() { }
		public HuskSpeedInit(int init) { value = init; }
		public int Value(World world) { return value; }
	}
}
