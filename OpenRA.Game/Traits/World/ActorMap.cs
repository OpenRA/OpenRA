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
using OpenRA.FileFormats;
using OpenRA.Traits;

namespace OpenRA.Traits
{
	public enum SubCell { FullCell, TopLeft, TopRight, Center, BottomLeft, BottomRight }

	public class ActorMapInfo : ITraitInfo
	{
		[Desc("Size of partition bins (cells)")]
		public readonly int BinSize = 10;

		public object Create(ActorInitializer init) { return new ActorMap(init.world, this); }
	}

	public class ActorMap
	{
		class InfluenceNode
		{
			public InfluenceNode next;
			public SubCell subCell;
			public Actor actor;
		}

		readonly ActorMapInfo info;
		readonly Map map;
		InfluenceNode[,] influence;

		List<Actor>[] actors;
		int rows, cols;

		public ActorMap(World world, ActorMapInfo info)
		{
			this.info = info;
			map = world.Map;
			influence = new InfluenceNode[world.Map.MapSize.X, world.Map.MapSize.Y];

			cols = world.Map.MapSize.X / info.BinSize + 1;
			rows = world.Map.MapSize.Y / info.BinSize + 1;
			actors = new List<Actor>[rows * cols];
			for (var j = 0; j < rows; j++)
				for (var i = 0; i < cols; i++)
					actors[j * cols + i] = new List<Actor>();
		}

		public IEnumerable<Actor> GetUnitsAt(CPos a)
		{
			if (!map.IsInMap(a))
				yield break;

			for (var i = influence[a.X, a.Y]; i != null; i = i.next)
				if (!i.actor.Destroyed)
					yield return i.actor;
		}

		public IEnumerable<Actor> GetUnitsAt(CPos a, SubCell sub)
		{
			if (!map.IsInMap(a))
				yield break;

			for (var i = influence[a.X, a.Y]; i != null; i = i.next)
				if (!i.actor.Destroyed && (i.subCell == sub || i.subCell == SubCell.FullCell))
					yield return i.actor;
		}

		public bool HasFreeSubCell(CPos a)
		{
			if (!AnyUnitsAt(a))
				return true;

			return new[] { SubCell.TopLeft, SubCell.TopRight, SubCell.Center,
				SubCell.BottomLeft, SubCell.BottomRight }.Any(b => !AnyUnitsAt(a,b));
		}

		public SubCell? FreeSubCell(CPos a)
		{
			if (!HasFreeSubCell(a))
				return null;

			return new[] { SubCell.TopLeft, SubCell.TopRight, SubCell.Center,
				SubCell.BottomLeft, SubCell.BottomRight }.First(b => !AnyUnitsAt(a,b));
		}


		public bool AnyUnitsAt(CPos a)
		{
			return influence[a.X, a.Y] != null;
		}

		public bool AnyUnitsAt(CPos a, SubCell sub)
		{
			for (var i = influence[a.X, a.Y]; i != null; i = i.next)
				if (i.subCell == sub || i.subCell == SubCell.FullCell)
					return true;

			return false;
		}

		public void AddInfluence(Actor self, IOccupySpace ios)
		{
			foreach (var c in ios.OccupiedCells())
				influence[c.First.X, c.First.Y] = new InfluenceNode { next = influence[c.First.X, c.First.Y], subCell = c.Second, actor = self };
		}

		public void RemoveInfluence(Actor self, IOccupySpace ios)
		{
			foreach (var c in ios.OccupiedCells())
				RemoveInfluenceInner(ref influence[c.First.X, c.First.Y], self);
		}

		void RemoveInfluenceInner(ref InfluenceNode influenceNode, Actor toRemove)
		{
			if (influenceNode == null)
				return;
			else if (influenceNode.actor == toRemove)
				influenceNode = influenceNode.next;

			if (influenceNode != null)
				RemoveInfluenceInner(ref influenceNode.next, toRemove);
		}

		public void AddPosition(Actor a, IOccupySpace ios)
		{
			var pos = ios.CenterPosition;
			var i = (pos.X / info.BinSize).Clamp(0, cols - 1);
			var j = (pos.Y / info.BinSize).Clamp(0, rows - 1);
			actors[j*cols + i].Add(a);
		}

		public void RemovePosition(Actor a, IOccupySpace ios)
		{
			foreach (var bin in actors)
				bin.Remove(a);
		}

		public void UpdatePosition(Actor a, IOccupySpace ios)
		{
			RemovePosition(a, ios);
			AddPosition(a, ios);
		}
	}
}
