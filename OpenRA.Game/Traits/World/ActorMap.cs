#region Copyright & License Information
/*
 * Copyright 2007-2016 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace OpenRA.Traits
{
	public enum SubCell { Invalid = int.MinValue, Any = int.MinValue / 2, FullCell = 0, First = 1 }

	public class ActorMapInfo : ITraitInfo
	{
		[Desc("Size of partition bins (cells)")]
		public readonly int BinSize = 10;

		public object Create(ActorInitializer init) { return new ActorMap(init.World, this); }
	}

	public class ActorMap : ITick
	{
		class InfluenceNode
		{
			public InfluenceNode Next;
			public SubCell SubCell;
			public Actor Actor;
		}

		class Bin
		{
			public readonly List<Actor> Actors = new List<Actor>();
			public readonly List<ProximityTrigger> ProximityTriggers = new List<ProximityTrigger>();
		}

		class CellTrigger
		{
			public readonly CPos[] Footprint;
			public bool Dirty;

			readonly Action<Actor> onActorEntered;
			readonly Action<Actor> onActorExited;

			IEnumerable<Actor> currentActors = Enumerable.Empty<Actor>();

			public CellTrigger(CPos[] footprint, Action<Actor> onActorEntered, Action<Actor> onActorExited)
			{
				Footprint = footprint;

				this.onActorEntered = onActorEntered;
				this.onActorExited = onActorExited;

				// Notify any actors that are initially inside the trigger zone
				Dirty = true;
			}

			public void Tick(ActorMap actorMap)
			{
				if (!Dirty)
					return;

				var oldActors = currentActors;
				currentActors = Footprint.SelectMany(actorMap.GetActorsAt).ToList();

				var entered = currentActors.Except(oldActors);
				var exited = oldActors.Except(currentActors);

				if (onActorEntered != null)
					foreach (var a in entered)
						onActorEntered(a);

				if (onActorExited != null)
					foreach (var a in exited)
						onActorExited(a);

				Dirty = false;
			}
		}

		class ProximityTrigger : IDisposable
		{
			public WPos TopLeft { get; private set; }
			public WPos BottomRight { get; private set; }

			public bool Dirty;

			readonly Action<Actor> onActorEntered;
			readonly Action<Actor> onActorExited;

			WPos position;
			WDist range;
			WDist vRange;

			IEnumerable<Actor> currentActors = Enumerable.Empty<Actor>();

			public ProximityTrigger(WPos pos, WDist range, WDist vRange, Action<Actor> onActorEntered, Action<Actor> onActorExited)
			{
				this.onActorEntered = onActorEntered;
				this.onActorExited = onActorExited;

				Update(pos, range, vRange);
			}

			public void Update(WPos newPos, WDist newRange, WDist newVRange)
			{
				position = newPos;
				range = newRange;
				vRange = newVRange;

				var offset = new WVec(newRange, newRange, newVRange);

				TopLeft = newPos - offset;
				BottomRight = newPos + offset;

				Dirty = true;
			}

			public void Tick(ActorMap am)
			{
				if (!Dirty)
					return;

				var oldActors = currentActors;
				var delta = new WVec(range, range, WDist.Zero);
				currentActors = am.ActorsInBox(position - delta, position + delta)
					.Where(a => (a.CenterPosition - position).HorizontalLengthSquared < range.LengthSquared
						&& (vRange.Length == 0 || (a.World.Map.DistanceAboveTerrain(a.CenterPosition).LengthSquared <= vRange.LengthSquared)))
					.ToList();

				var entered = currentActors.Except(oldActors);
				var exited = oldActors.Except(currentActors);

				if (onActorEntered != null)
					foreach (var a in entered)
						onActorEntered(a);

				if (onActorExited != null)
					foreach (var a in exited)
						onActorExited(a);

				Dirty = false;
			}

			public void Dispose()
			{
				if (onActorExited != null)
					foreach (var a in currentActors)
						onActorExited(a);
			}
		}

		readonly ActorMapInfo info;
		readonly Map map;
		readonly Dictionary<int, CellTrigger> cellTriggers = new Dictionary<int, CellTrigger>();
		readonly Dictionary<CPos, List<CellTrigger>> cellTriggerInfluence = new Dictionary<CPos, List<CellTrigger>>();
		readonly Dictionary<int, ProximityTrigger> proximityTriggers = new Dictionary<int, ProximityTrigger>();
		int nextTriggerId;

		readonly CellLayer<InfluenceNode> influence;

		readonly Bin[] bins;
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

			cols = CellCoordToBinIndex(world.Map.MapSize.X) + 1;
			rows = CellCoordToBinIndex(world.Map.MapSize.Y) + 1;
			bins = new Bin[rows * cols];
			for (var row = 0; row < rows; row++)
				for (var col = 0; col < cols; col++)
					bins[row * cols + col] = new Bin();

			// PERF: Cache this delegate so it does not have to be allocated repeatedly.
			actorShouldBeRemoved = removeActorPosition.Contains;
		}

		sealed class ActorsAtEnumerator : IEnumerator<Actor>
		{
			InfluenceNode node;
			public ActorsAtEnumerator(InfluenceNode node) { this.node = node; }
			public void Reset() { throw new NotSupportedException(); }
			public Actor Current { get; private set; }
			object IEnumerator.Current { get { return Current; } }
			public void Dispose() { }
			public bool MoveNext()
			{
				while (node != null)
				{
					Current = node.Actor;
					node = node.Next;
					if (!Current.Disposed)
						return true;
				}

				return false;
			}
		}

		sealed class ActorsAtEnumerable : IEnumerable<Actor>
		{
			readonly InfluenceNode node;
			public ActorsAtEnumerable(InfluenceNode node) { this.node = node; }
			public IEnumerator<Actor> GetEnumerator() { return new ActorsAtEnumerator(node); }
			IEnumerator IEnumerable.GetEnumerator() { return GetEnumerator(); }
		}

		public IEnumerable<Actor> GetActorsAt(CPos a)
		{
			// PERF: Custom enumerator for efficiency - using `yield` is slower.
			var uv = a.ToMPos(map);
			if (!influence.Contains(uv))
				return Enumerable.Empty<Actor>();
			return new ActorsAtEnumerable(influence[uv]);
		}

		public IEnumerable<Actor> GetActorsAt(CPos a, SubCell sub)
		{
			var uv = a.ToMPos(map);
			if (!influence.Contains(uv))
				yield break;

			for (var i = influence[uv]; i != null; i = i.Next)
				if (!i.Actor.Disposed && (i.SubCell == sub || i.SubCell == SubCell.FullCell))
					yield return i.Actor;
		}

		public bool HasFreeSubCell(CPos cell, bool checkTransient = true)
		{
			return FreeSubCell(cell, SubCell.Any, checkTransient) != SubCell.Invalid;
		}

		public SubCell FreeSubCell(CPos cell, SubCell preferredSubCell = SubCell.Any, bool checkTransient = true)
		{
			if (preferredSubCell > SubCell.Any && !AnyActorsAt(cell, preferredSubCell, checkTransient))
				return preferredSubCell;

			if (!AnyActorsAt(cell))
				return map.Grid.DefaultSubCell;

			for (var i = (int)SubCell.First; i < map.Grid.SubCellOffsets.Length; i++)
				if (i != (int)preferredSubCell && !AnyActorsAt(cell, (SubCell)i, checkTransient))
					return (SubCell)i;

			return SubCell.Invalid;
		}

		public SubCell FreeSubCell(CPos cell, SubCell preferredSubCell, Func<Actor, bool> checkIfBlocker)
		{
			if (preferredSubCell > SubCell.Any && !AnyActorsAt(cell, preferredSubCell, checkIfBlocker))
				return preferredSubCell;

			if (!AnyActorsAt(cell))
				return map.Grid.DefaultSubCell;

			for (var i = (int)SubCell.First; i < map.Grid.SubCellOffsets.Length; i++)
				if (i != (int)preferredSubCell && !AnyActorsAt(cell, (SubCell)i, checkIfBlocker))
					return (SubCell)i;
			return SubCell.Invalid;
		}

		// NOTE: always includes transients with influence
		public bool AnyActorsAt(CPos a)
		{
			var uv = a.ToMPos(map);
			if (!influence.Contains(uv))
				return false;

			return influence[uv] != null;
		}

		// NOTE: can not check aircraft
		public bool AnyActorsAt(CPos a, SubCell sub, bool checkTransient = true)
		{
			var uv = a.ToMPos(map);
			if (!influence.Contains(uv))
				return false;

			var always = sub == SubCell.FullCell || sub == SubCell.Any;
			for (var i = influence[uv]; i != null; i = i.Next)
			{
				if (always || i.SubCell == sub || i.SubCell == SubCell.FullCell)
				{
					if (checkTransient)
						return true;

					var pos = i.Actor.TraitOrDefault<IPositionable>();
					if (pos == null || !pos.IsLeavingCell(a, i.SubCell))
						return true;
				}
			}

			return false;
		}

		// NOTE: can not check aircraft
		public bool AnyActorsAt(CPos a, SubCell sub, Func<Actor, bool> withCondition)
		{
			var uv = a.ToMPos(map);
			if (!influence.Contains(uv))
				return false;

			var always = sub == SubCell.FullCell || sub == SubCell.Any;
			for (var i = influence[uv]; i != null; i = i.Next)
				if ((always || i.SubCell == sub || i.SubCell == SubCell.FullCell) && !i.Actor.Disposed && withCondition(i.Actor))
					return true;

			return false;
		}

		public void AddInfluence(Actor self, IOccupySpace ios)
		{
			foreach (var c in ios.OccupiedCells())
			{
				var uv = c.First.ToMPos(map);
				if (!influence.Contains(uv))
					continue;

				influence[uv] = new InfluenceNode { Next = influence[uv], SubCell = c.Second, Actor = self };

				List<CellTrigger> triggers;
				if (cellTriggerInfluence.TryGetValue(c.First, out triggers))
					foreach (var t in triggers)
						t.Dirty = true;
			}
		}

		public void RemoveInfluence(Actor self, IOccupySpace ios)
		{
			foreach (var c in ios.OccupiedCells())
			{
				var uv = c.First.ToMPos(map);
				if (!influence.Contains(uv))
					continue;

				var temp = influence[uv];
				RemoveInfluenceInner(ref temp, self);
				influence[uv] = temp;

				List<CellTrigger> triggers;
				if (cellTriggerInfluence.TryGetValue(c.First, out triggers))
					foreach (var t in triggers)
						t.Dirty = true;
			}
		}

		static void RemoveInfluenceInner(ref InfluenceNode influenceNode, Actor toRemove)
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
			foreach (var bin in bins)
			{
				var removed = bin.Actors.RemoveAll(actorShouldBeRemoved);
				if (removed > 0)
					foreach (var t in bin.ProximityTriggers)
						t.Dirty = true;
			}

			removeActorPosition.Clear();

			foreach (var a in addActorPosition)
			{
				var pos = a.CenterPosition;
				var col = WorldCoordToBinIndex(pos.X).Clamp(0, cols - 1);
				var row = WorldCoordToBinIndex(pos.Y).Clamp(0, rows - 1);
				var bin = BinAt(row, col);

				bin.Actors.Add(a);
				foreach (var t in bin.ProximityTriggers)
					t.Dirty = true;
			}

			addActorPosition.Clear();

			foreach (var t in cellTriggers)
				t.Value.Tick(this);

			foreach (var t in proximityTriggers)
				t.Value.Tick(this);
		}

		public int AddCellTrigger(CPos[] cells, Action<Actor> onEntry, Action<Actor> onExit)
		{
			var id = nextTriggerId++;
			var t = new CellTrigger(cells, onEntry, onExit);
			cellTriggers.Add(id, t);

			foreach (var c in cells)
			{
				if (!influence.Contains(c))
					continue;

				if (!cellTriggerInfluence.ContainsKey(c))
					cellTriggerInfluence.Add(c, new List<CellTrigger>());

				cellTriggerInfluence[c].Add(t);
			}

			return id;
		}

		public void RemoveCellTrigger(int id)
		{
			CellTrigger trigger;
			if (!cellTriggers.TryGetValue(id, out trigger))
				return;

			foreach (var c in trigger.Footprint)
			{
				if (!cellTriggerInfluence.ContainsKey(c))
					continue;

				cellTriggerInfluence[c].RemoveAll(t => t == trigger);
			}
		}

		public int AddProximityTrigger(WPos pos, WDist range, WDist vRange, Action<Actor> onEntry, Action<Actor> onExit)
		{
			var id = nextTriggerId++;
			var t = new ProximityTrigger(pos, range, vRange, onEntry, onExit);
			proximityTriggers.Add(id, t);

			foreach (var bin in BinsInBox(t.TopLeft, t.BottomRight))
				bin.ProximityTriggers.Add(t);

			return id;
		}

		public void RemoveProximityTrigger(int id)
		{
			ProximityTrigger t;
			if (!proximityTriggers.TryGetValue(id, out t))
				return;

			foreach (var bin in BinsInBox(t.TopLeft, t.BottomRight))
				bin.ProximityTriggers.Remove(t);

			t.Dispose();
		}

		public void UpdateProximityTrigger(int id, WPos newPos, WDist newRange, WDist newVRange)
		{
			ProximityTrigger t;
			if (!proximityTriggers.TryGetValue(id, out t))
				return;

			foreach (var bin in BinsInBox(t.TopLeft, t.BottomRight))
				bin.ProximityTriggers.Remove(t);

			t.Update(newPos, newRange, newVRange);

			foreach (var bin in BinsInBox(t.TopLeft, t.BottomRight))
				bin.ProximityTriggers.Add(t);
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

		int CellCoordToBinIndex(int cell)
		{
			return cell / info.BinSize;
		}

		int WorldCoordToBinIndex(int world)
		{
			return CellCoordToBinIndex(world / 1024);
		}

		Rectangle BinRectangleCoveringWorldArea(int worldLeft, int worldTop, int worldRight, int worldBottom)
		{
			var minCol = WorldCoordToBinIndex(worldLeft).Clamp(0, cols - 1);
			var minRow = WorldCoordToBinIndex(worldTop).Clamp(0, rows - 1);
			var maxCol = WorldCoordToBinIndex(worldRight).Clamp(0, cols - 1);
			var maxRow = WorldCoordToBinIndex(worldBottom).Clamp(0, rows - 1);
			return Rectangle.FromLTRB(minCol, minRow, maxCol, maxRow);
		}

		Bin BinAt(int binRow, int binCol)
		{
			return bins[binRow * cols + binCol];
		}

		IEnumerable<Bin> BinsInBox(WPos a, WPos b)
		{
			var left = Math.Min(a.X, b.X);
			var top = Math.Min(a.Y, b.Y);
			var right = Math.Max(a.X, b.X);
			var bottom = Math.Max(a.Y, b.Y);
			var region = BinRectangleCoveringWorldArea(left, top, right, bottom);
			var minCol = region.Left;
			var minRow = region.Top;
			var maxCol = region.Right;
			var maxRow = region.Bottom;
			for (var row = minRow; row <= maxRow; row++)
				for (var col = minCol; col <= maxCol; col++)
					yield return BinAt(row, col);
		}

		public IEnumerable<Actor> ActorsInBox(WPos a, WPos b)
		{
			// PERF: Inline BinsInBox here to avoid allocations as this method is called often.
			var left = Math.Min(a.X, b.X);
			var top = Math.Min(a.Y, b.Y);
			var right = Math.Max(a.X, b.X);
			var bottom = Math.Max(a.Y, b.Y);
			var region = BinRectangleCoveringWorldArea(left, top, right, bottom);
			var minCol = region.Left;
			var minRow = region.Top;
			var maxCol = region.Right;
			var maxRow = region.Bottom;
			for (var row = minRow; row <= maxRow; row++)
			{
				for (var col = minCol; col <= maxCol; col++)
				{
					foreach (var actor in BinAt(row, col).Actors)
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
	}
}
