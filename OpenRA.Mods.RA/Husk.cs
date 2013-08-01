﻿#region Copyright & License Information
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
using OpenRA.FileFormats;
using OpenRA.Mods.RA.Move;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	class HuskInfo : ITraitInfo, IOccupySpaceInfo, IFacingInfo
	{
		public readonly string[] AllowedTerrain = { };

		public object Create(ActorInitializer init) { return new Husk(init, this); }

		public int GetInitialFacing() { return 128; }
	}

	class Husk : IPositionable, IFacing, ISync
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
			CenterPosition = init.Contains<CenterLocationInit>() ? init.Get<CenterLocationInit, PPos>().ToWPos(0) : TopLeft.CenterPosition;
			Facing = init.Contains<FacingInit>() ? init.Get<FacingInit, int>() : 128;

			var speed = init.Contains<HuskSpeedInit>() ? init.Get<HuskSpeedInit, int>() : 0;
			var distance = (TopLeft.CenterPosition - CenterPosition).Length;
			if (speed > 0 && distance > 0)
				self.QueueActivity(new Drag(CenterPosition, TopLeft.CenterPosition, distance / speed));
		}

		public IEnumerable<Pair<CPos, SubCell>> OccupiedCells() { yield return Pair.New(TopLeft, SubCell.FullCell); }
		public bool CanEnterCell(CPos cell)
		{
			if (!self.World.Map.IsInMap(cell.X, cell.Y))
				return false;

			if (!info.AllowedTerrain.Contains(self.World.GetTerrainType(cell)))
				return false;

			return !self.World.ActorMap.AnyUnitsAt(cell);
		}

		public void SetPosition(Actor self, CPos cell) { SetPosition(self, cell.CenterPosition); }
		public void SetVisualPosition(Actor self, WPos pos) { CenterPosition = pos; }

		public void SetPosition(Actor self, WPos pos)
		{
			self.World.ActorMap.Remove(self, this);
			CenterPosition = pos;
			TopLeft = pos.ToCPos();
			self.World.ActorMap.Add(self, this);
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
