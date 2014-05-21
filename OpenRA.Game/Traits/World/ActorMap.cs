#region Copyright & License Information
/*
 * Copyright 2007-2013 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.FileFormats;

namespace OpenRA.Traits
{
	public enum SubCell { FullCell, TopLeft, TopRight, Center, BottomLeft, BottomRight }

	public class ActorMapInfo : ITraitInfo
	{
		[Desc("Size of partition bins (cells)")]
		public readonly int BinSize = 10;

		public object Create(ActorInitializer init) { return new ActorMap(init.world, this); }
	}

	public class ActorMap : ITick
	{
		class InfluenceNode
		{
			public InfluenceNode Next;
			public SubCell SubCell;
			public Actor Actor;
		}

		static readonly SubCell[] SubCells =
		{
			SubCell.TopLeft, SubCell.TopRight, SubCell.Center,
			SubCell.BottomLeft, SubCell.BottomRight
		};

		readonly ActorMapInfo info;
		readonly Map map;
		InfluenceNode[,] influence;

		List<Actor>[] actors;
		int rows, cols;

		// Position updates are done in one pass
		// to ensure consistency during a tick
		readonly List<Actor> addActorPosition = new List<Actor>();
		readonly HashSet<Actor> removeActorPosition = new HashSet<Actor>();

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

			for (var i = influence[a.X, a.Y]; i != null; i = i.Next)
				if (!i.Actor.Destroyed)
					yield return i.Actor;
		}

		public IEnumerable<Actor> GetUnitsAt(CPos a, SubCell sub)
		{
			if (!map.IsInMap(a))
				yield break;

			for (var i = influence[a.X, a.Y]; i != null; i = i.Next)
				if (!i.Actor.Destroyed && (i.SubCell == sub || i.SubCell == SubCell.FullCell))
					yield return i.Actor;
		}

		public bool HasFreeSubCell(CPos a)
		{
			if (!AnyUnitsAt(a))
				return true;

			return SubCells.Any(b => !AnyUnitsAt(a, b));
		}

		public SubCell? FreeSubCell(CPos a)
		{
			if (!HasFreeSubCell(a))
				return null;

			return SubCells.First(b => !AnyUnitsAt(a, b));
		}

		public bool AnyUnitsAt(CPos a)
		{
			return influence[a.X, a.Y] != null;
		}

		public bool AnyUnitsAt(CPos a, SubCell sub)
		{
			for (var i = influence[a.X, a.Y]; i != null; i = i.Next)
				if (i.SubCell == sub || i.SubCell == SubCell.FullCell)
					return true;

			return false;
		}

		public void AddInfluence(Actor self, IOccupySpace ios)
		{
			foreach (var c in ios.OccupiedCells())
				influence[c.First.X, c.First.Y] = new InfluenceNode { Next = influence[c.First.X, c.First.Y], SubCell = c.Second, Actor = self };
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
			else if (influenceNode.Actor == toRemove)
				influenceNode = influenceNode.Next;

			if (influenceNode != null)
				RemoveInfluenceInner(ref influenceNode.Next, toRemove);
		}

		public void Tick(Actor self)
		{
			// Position updates are done in one pass
			// to ensure consistency during a tick
			foreach (var bin in actors)
				bin.RemoveAll(removeActorPosition.Contains);

			removeActorPosition.Clear();

			foreach (var a in addActorPosition)
			{
				var pos = a.OccupiesSpace.CenterPosition;
				var i = (pos.X / info.BinSize).Clamp(0, cols - 1);
				var j = (pos.Y / info.BinSize).Clamp(0, rows - 1);
				actors[j * cols + i].Add(a);
			}

			addActorPosition.Clear();
		}

		public void AddPosition(Actor a, IOccupySpace ios)
		{
			addActorPosition.Add(a);
		}

		public void RemovePosition(Actor a, IOccupySpace ios)
		{
			removeActorPosition.Add(a);
		}

		public void UpdatePosition(Actor a, IOccupySpace ios)
		{
			RemovePosition(a, ios);
			AddPosition(a, ios);
		}

		public IEnumerable<Actor> ActorsInBox(WPos a, WPos b)
		{
			var left = Math.Min(a.X, b.X);
			var top = Math.Min(a.Y, b.Y);
			var right = Math.Max(a.X, b.X);
			var bottom = Math.Max(a.Y, b.Y);
			var i1 = (left / info.BinSize).Clamp(0, cols - 1);
			var i2 = (right / info.BinSize).Clamp(0, cols - 1);
			var j1 = (top / info.BinSize).Clamp(0, rows - 1);
			var j2 = (bottom / info.BinSize).Clamp(0, rows - 1);

			var actorsInBox = new HashSet<Actor>();
			for (var j = j1; j <= j2; j++)
			{
				for (var i = i1; i <= i2; i++)
				{
					foreach (var actor in actors[j * cols + i])
					{
						var c = actor.CenterPosition;
						if (actor.IsInWorld && left <= c.X && c.X <= right && top <= c.Y && c.Y <= bottom)
							if (actorsInBox.Add(actor))
								yield return actor;
					}
				}
			}
		}
	}
}
