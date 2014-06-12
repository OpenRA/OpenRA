#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Move;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common
{
	public class HuskInfo : ITraitInfo, IOccupySpaceInfo, IFacingInfo
	{
		public readonly string[] AllowedTerrain = { };

		public object Create(ActorInitializer init) { return new Husk(init, this); }

		public int GetInitialFacing() { return 128; }
	}

	public class Husk : IPositionable, IFacing, ISync, INotifyAddedToWorld, INotifyRemovedFromWorld, IDisable
	{
		readonly HuskInfo info;
		readonly Actor self;

		[Sync] public CPos TopLeft { get; private set; }
		[Sync] public WPos CenterPosition { get; private set; }
		[Sync] public int Facing { get; set; }

		public int ROT { get { return 0; } }

		public Husk(ActorInitializer init, HuskInfo info)
		{
			this.info = info;
			this.self = init.self;

			TopLeft = init.Get<LocationInit, CPos>();
			CenterPosition = init.Contains<CenterPositionInit>() ? init.Get<CenterPositionInit, WPos>() : TopLeft.CenterPosition;
			Facing = init.Contains<FacingInit>() ? init.Get<FacingInit, int>() : 128;

			var speed = init.Contains<HuskSpeedInit>() ? init.Get<HuskSpeedInit, int>() : 0;
			var distance = (TopLeft.CenterPosition - CenterPosition).Length;
			if (speed > 0 && distance > 0)
				self.QueueActivity(new Drag(CenterPosition, TopLeft.CenterPosition, distance / speed));
		}

		public IEnumerable<Pair<CPos, SubCell>> OccupiedCells() { yield return Pair.New(TopLeft, SubCell.FullCell); }
		public bool CanEnterCell(CPos cell, Actor ignoreActor, bool checkTransientActors)
		{
			if (!self.World.Map.IsInMap(cell.X, cell.Y))
				return false;

			if (!info.AllowedTerrain.Contains(self.World.GetTerrainType(cell)))
				return false;

			if (!checkTransientActors)
				return true;

			return !self.World.ActorMap.GetUnitsAt(cell)
				.Where(x => x != ignoreActor)
				.Any();
		}

		public bool CanEnterCell(CPos cell) { return CanEnterCell(cell, null, true); }

		public void SetPosition(Actor self, CPos cell) { SetPosition(self, cell.CenterPosition); }
		public void SetVisualPosition(Actor self, WPos pos)
		{
			CenterPosition = pos;
			self.World.ScreenMap.Update(self);
		}

		public void SetPosition(Actor self, WPos pos)
		{
			self.World.ActorMap.RemoveInfluence(self, this);
			CenterPosition = pos;
			TopLeft = pos.ToCPos();
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
