#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Linq;

namespace OpenRA.Traits
{
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
			public int SubCell;
			public Actor Actor;
		}

		readonly ActorMapInfo info;
		readonly Map map;
		readonly CellLayer<InfluenceNode> influence;

		readonly List<Actor>[] actors;
		readonly int rows, cols;

		// Position updates are done in one pass
		// to ensure consistency during a tick
		readonly HashSet<Actor> addActorPosition = new HashSet<Actor>();
		readonly HashSet<Actor> removeActorPosition = new HashSet<Actor>();
		readonly Predicate<Actor> actorShouldBeRemoved;

		public ActorMap(World world, ActorMapInfo info)
		{
			this.info = info;
			map = world.Map;
			influence = new CellLayer<InfluenceNode>(world.Map);

			cols = world.Map.MapSize.X / info.BinSize + 1;
			rows = world.Map.MapSize.Y / info.BinSize + 1;
			actors = new List<Actor>[rows * cols];
			for (var j = 0; j < rows; j++)
				for (var i = 0; i < cols; i++)
					actors[j * cols + i] = new List<Actor>();

			// Cache this delegate so it does not have to be allocated repeatedly.
			actorShouldBeRemoved = removeActorPosition.Contains;
		}

		public IEnumerable<Actor> GetUnitsAt(CPos a)
		{
			if (!map.Contains(a))
				yield break;

			for (var i = influence[a]; i != null; i = i.Next)
				if (!i.Actor.Destroyed)
					yield return i.Actor;
		}

		public IEnumerable<Actor> GetUnitsAt(CPos a, int sub)
		{
			if (!map.Contains(a))
				yield break;

			for (var i = influence[a]; i != null; i = i.Next)
				if (!i.Actor.Destroyed && (i.SubCell == sub || i.SubCell == 0))
					yield return i.Actor;
		}

		public bool HasFreeSubCell(CPos a, bool checkTransient = true)
		{
			return FreeSubCell(a, -1, checkTransient) >= 0;
		}

		public int FreeSubCell(CPos a, int preferredSubCell = -1, bool checkTransient = true)
		{
			if (preferredSubCell >= 0 && !AnyUnitsAt(a, preferredSubCell, checkTransient))
				return preferredSubCell;

			if (!AnyUnitsAt(a))
				return map.SubCellDefaultIndex;

			for (var i = 1; i < map.SubCellOffsets.Length; i++)
				if (i != preferredSubCell && !AnyUnitsAt(a, i, checkTransient))
					return i;
			return -1;
		}

		public int FreeSubCell(CPos a, int preferredSubCell, Func<Actor, bool> checkIfBlocker)
		{
			if (preferredSubCell >= 0 && !AnyUnitsAt(a, preferredSubCell, checkIfBlocker))
				return preferredSubCell;

			if (!AnyUnitsAt(a))
				return map.SubCellDefaultIndex;

			for (var i = 1; i < map.SubCellOffsets.Length; i++)
				if (i != preferredSubCell && !AnyUnitsAt(a, i, checkIfBlocker))
					return i;
			return -1;
		}

		// NOTE: does not check transients, but checks aircraft
		public bool AnyUnitsAt(CPos a)
		{
			return influence[a] != null;
		}

		// NOTE: can not check aircraft
		public bool AnyUnitsAt(CPos a, int sub, bool checkTransient = true)
		{
			for (var i = influence[a]; i != null; i = i.Next)
				if (sub <= 0 || i.SubCell == sub || i.SubCell == 0)
				{
					if (checkTransient)
						return true;
					var pos = i.Actor.TraitOrDefault<IPositionable>();
					if (pos == null || !pos.IsLeaving(a, i.SubCell))
						return true;
				}

			return false;
		}

		// NOTE: can not check aircraft
		public bool AnyUnitsAt(CPos a, int sub, Func<Actor, bool> withCondition)
		{
			for (var i = influence[a]; i != null; i = i.Next)
				if (sub <= 0 || i.SubCell == sub || i.SubCell == 0)
					if (withCondition(i.Actor))
						return true;

			return false;
		}

		public void AddInfluence(Actor self, IOccupySpace ios)
		{
			foreach (var c in ios.OccupiedCells())
				influence[c.First] = new InfluenceNode { Next = influence[c.First], SubCell = c.Second, Actor = self };
		}

		public void RemoveInfluence(Actor self, IOccupySpace ios)
		{
			foreach (var c in ios.OccupiedCells())
			{
				var temp = influence[c.First];
				RemoveInfluenceInner(ref temp, self);
				influence[c.First] = temp;
			}
		}

		void RemoveInfluenceInner(ref InfluenceNode influenceNode, Actor toRemove)
		{
			if (influenceNode == null)
				return;

			if (influenceNode.Actor == toRemove)
				influenceNode = influenceNode.Next;

			if (influenceNode != null)
				RemoveInfluenceInner(ref influenceNode.Next, toRemove);
		}

		public void Tick(Actor self)
		{
			// Position updates are done in one pass
			// to ensure consistency during a tick
			foreach (var bin in actors)
				bin.RemoveAll(actorShouldBeRemoved);

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
			UpdatePosition(a, ios);
		}

		public void RemovePosition(Actor a, IOccupySpace ios)
		{
			removeActorPosition.Add(a);
		}

		public void UpdatePosition(Actor a, IOccupySpace ios)
		{
			RemovePosition(a, ios);
			addActorPosition.Add(a);
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

			for (var j = j1; j <= j2; j++)
			{
				for (var i = i1; i <= i2; i++)
				{
					foreach (var actor in actors[j * cols + i])
					{
						if (actor.IsInWorld)
						{
							var c = actor.CenterPosition;
							if (left <= c.X && c.X <= right && top <= c.Y && c.Y <= bottom)
								yield return actor;
						}
					}
				}
			}
		}

		public IEnumerable<Actor> ActorsInWorld()
		{
			return actors.SelectMany(bin => bin.Where(actor => actor.IsInWorld));
		}
	}
}
