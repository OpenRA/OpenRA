#region Copyright & License Information
/*
 * Copyright 2007-2015 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenRA.Mods.Common.Traits;

namespace OpenRA.Test
{
	public class FakeActor : IActor
	{
		IWorld world;

		public ActorInfo Info
		{
			get { throw new NotImplementedException("No need to implement this yet"); }
		}

		public IWorld World
		{
			get { return world; }
		}

		public uint ActorID
		{
			get { return 1; }
		}

		public Player Owner
		{
			get { return null; }
			set { }
		}

		public T TraitOrDefault<T>()
		{
			return default(T);
		}

		public T Trait<T>()
		{
			return default(T);
		}

		public IEnumerable<T> TraitsImplementing<T>()
		{
			throw new NotImplementedException("No need to implement this yet");
		}

		public T TraitInfo<T>()
		{
			return default(T);
		}

		public IEnumerable<Graphics.IRenderable> Render(Graphics.WorldRenderer wr)
		{
			throw new NotImplementedException("No need to implement this yet");
		}

		public FakeActor(IWorld world)
		{
			// TODO: Complete member initialization
			this.world = world;
		}

		public FakeActor()
		{
			// TODO: Complete member initialization
		}
	}

	public class FakeWorld : IWorld
	{
		FakeActor worldactor;
		IMap map;

		public IActor WorldActor
		{
			get { return worldactor; }
		}

		public int WorldTick
		{
			get { return 50; }
		}

		public IMap Map
		{
			get { return map; }
		}

		public TileSet TileSet
		{
			get { throw new NotImplementedException("No need to implement this yet"); }
		}

		public FakeWorld(IMap map)
		{
			// TODO: Complete member initialization
			this.map = map;
		}

		public FakeWorld(IMap map, FakeActor worldactor)
		{
			// TODO: Complete member initialization
			this.map = map;
			this.worldactor = worldactor;
		}
	}

	public class FakeMobileInfo : IMobileInfo
	{
		Func<CPos, bool> conditions;

		public int MovementCostForCell(World world, CPos cell)
		{
			if (conditions(cell))
				return 125;
			return int.MaxValue;
		}

		public bool CanEnterCell(World world, Actor self, CPos cell, out int movementCost, Actor ignoreActor = null, CellConditions check = CellConditions.All)
		{
			movementCost = MovementCostForCell(world, cell);
			return conditions(cell);
		}

		public int GetMovementClass(TileSet tileset)
		{
			throw new NotImplementedException("No need to implement this yet");
		}

		public FakeMobileInfo(Func<CPos, bool> conditions)
		{
			this.conditions = conditions;
		}

		public bool CanEnterCell(World world, Actor self, CPos cell, Actor ignoreActor = null, CellConditions check = CellConditions.All)
		{
			return conditions(cell);
		}

		public object Create(ActorInitializer init)
		{
			throw new NotImplementedException();
		}
	}

	public class FakeMap : IMap
	{
		int width;
		int height;

		public FakeMap(int width, int height)
		{
			// TODO: Complete member initialization
			this.width = width;
			this.height = height;
		}

		public TileShape TileShape
		{
			get { return TileShape.Rectangle; }
		}

		public int2 MapSize
		{
			get { return new int2(width, height); }
			set { throw new NotImplementedException("No need to implement this yet"); }
		}

		public bool Contains(CPos cell)
		{
			return cell.X >= 0 && cell.X < width && cell.Y >= 0 && cell.Y < height;
		}

		public CPos CellContaining(WPos pos)
		{
			throw new NotImplementedException("No need to implement this yet");
		}

		public WVec OffsetOfSubCell(Traits.SubCell subCell)
		{
			throw new NotImplementedException("No need to implement this yet");
		}

		public IEnumerable<CPos> FindTilesInCircle(CPos center, int maxRange)
		{
			throw new NotImplementedException("No need to implement this yet");
		}

		public WPos CenterOfCell(CPos cell)
		{
			throw new NotImplementedException("No need to implement this yet");
		}
	}
}
