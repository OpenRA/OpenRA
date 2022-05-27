#region Copyright & License Information
/*
 * Copyright 2007-2022 The OpenRA Developers (see AUTHORS)
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
using System.Linq;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[TraitLocation(SystemActors.World | SystemActors.EditorWorld)]
	public class ActorMapInfo : TraitInfo
	{
		[Desc("Size of partition bins (cells)")]
		public readonly int BinSize = 10;

		public override object Create(ActorInitializer init) { return new ActorMap(init.World, this); }
	}

	public class ActorMap : IActorMap, ITick, INotifyCreated
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
			readonly HashSet<Actor> oldActors = new HashSet<Actor>();
			readonly HashSet<Actor> currentActors = new HashSet<Actor>();

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

				// PERF: Reuse collection to avoid allocations.
				oldActors.Clear();
				oldActors.UnionWith(currentActors);

				currentActors.Clear();
				currentActors.UnionWith(Footprint.SelectMany(actorMap.GetActorsAt));

				if (onActorEntered != null)
					foreach (var a in currentActors)
						if (!oldActors.Contains(a))
							onActorEntered(a);

				if (onActorExited != null)
					foreach (var a in oldActors)
						if (!currentActors.Contains(a))
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
			readonly HashSet<Actor> oldActors = new HashSet<Actor>();
			readonly HashSet<Actor> currentActors = new HashSet<Actor>();

			WPos position;
			WDist range;
			WDist vRange;

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

				// PERF: Reuse collection to avoid allocations.
				oldActors.Clear();
				oldActors.UnionWith(currentActors);

				var delta = new WVec(range, range, WDist.Zero);
				currentActors.Clear();
				currentActors.UnionWith(
					am.ActorsInBox(position - delta, position + delta)
					.Where(a => (a.CenterPosition - position).HorizontalLengthSquared < range.LengthSquared
						&& (vRange.Length == 0 || (a.World.Map.DistanceAboveTerrain(a.CenterPosition).LengthSquared <= vRange.LengthSquared))));

				if (onActorEntered != null)
					foreach (var a in currentActors)
						if (!oldActors.Contains(a))
							onActorEntered(a);

				if (onActorExited != null)
					foreach (var a in oldActors)
						if (!currentActors.Contains(a))
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

		CellLayer<InfluenceNode>[] influence;

		// Index 0 is kept null as layer 0 is used for the ground layer.
		public ICustomMovementLayer[] CustomMovementLayers = new ICustomMovementLayer[] { null };
		public event Action<CPos> CellUpdated;
		readonly Bin[] bins;
		readonly int rows, cols;

		// Position updates are done in one pass
		// to ensure consistency during a tick
		readonly HashSet<Actor> addActorPosition = new HashSet<Actor>();
		readonly HashSet<Actor> removeActorPosition = new HashSet<Actor>();
		readonly Predicate<Actor> actorShouldBeRemoved;

		public WDist LargestActorRadius { get; }
		public WDist LargestBlockingActorRadius { get; }

		public ActorMap(World world, ActorMapInfo info)
		{
			this.info = info;
			map = world.Map;
			influence = new[] { new CellLayer<InfluenceNode>(world.Map) };

			cols = CellCoordToBinIndex(world.Map.MapSize.X) + 1;
			rows = CellCoordToBinIndex(world.Map.MapSize.Y) + 1;
			bins = new Bin[rows * cols];
			for (var row = 0; row < rows; row++)
				for (var col = 0; col < cols; col++)
					bins[row * cols + col] = new Bin();

			// PERF: Cache this delegate so it does not have to be allocated repeatedly.
			actorShouldBeRemoved = removeActorPosition.Contains;

			LargestActorRadius = map.Rules.Actors.SelectMany(a => a.Value.TraitInfos<HitShapeInfo>()).Max(h => h.Type.OuterRadius);
			var blockers = map.Rules.Actors.Where(a => a.Value.HasTraitInfo<IBlocksProjectilesInfo>());
			LargestBlockingActorRadius = blockers.Any() ? blockers.SelectMany(a => a.Value.TraitInfos<HitShapeInfo>()).Max(h => h.Type.OuterRadius) : WDist.Zero;
		}

		void INotifyCreated.Created(Actor self)
		{
			var customMovementLayers = self.TraitsImplementing<ICustomMovementLayer>().ToList();
			if (customMovementLayers.Count == 0)
				return;

			var length = customMovementLayers.Max(cml => cml.Index) + 1;
			Array.Resize(ref CustomMovementLayers, length);
			Array.Resize(ref influence, length);

			foreach (var cml in customMovementLayers)
			{
				CustomMovementLayers[cml.Index] = cml;
				influence[cml.Index] = new CellLayer<InfluenceNode>(self.World.Map);
			}
		}

		struct ActorsAtEnumerator : IEnumerator<Actor>
		{
			InfluenceNode node;
			Actor current;

			public ActorsAtEnumerator(InfluenceNode node)
			{
				this.node = node;
				current = null;
			}

			public void Reset() { throw new NotSupportedException(); }
			public Actor Current => current;

			object IEnumerator.Current => current;
			public void Dispose() { }
			public bool MoveNext()
			{
				while (node != null)
				{
					current = node.Actor;
					node = node.Next;
					if (!current.Disposed)
						return true;
				}

				return false;
			}
		}

		readonly struct ActorsAtEnumerable : IEnumerable<Actor>
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
			var layer = influence[a.Layer];
			if (!layer.Contains(uv))
				return Enumerable.Empty<Actor>();

			return new ActorsAtEnumerable(layer[uv]);
		}

		public IEnumerable<Actor> GetActorsAt(CPos a, SubCell sub)
		{
			var uv = a.ToMPos(map);
			var layer = influence[a.Layer];
			if (!layer.Contains(uv))
				yield break;

			var always = sub == SubCell.FullCell || sub == SubCell.Any;
			for (var i = layer[uv]; i != null; i = i.Next)
				if (!i.Actor.Disposed && (i.SubCell == sub || i.SubCell == SubCell.FullCell || always))
					yield return i.Actor;
		}

		public bool HasFreeSubCell(CPos cell, bool checkTransient = true)
		{
			return FreeSubCell(cell, SubCell.Any, checkTransient) != SubCell.Invalid;
		}

		public SubCell FreeSubCell(CPos cell, SubCell preferredSubCell = SubCell.Any, bool checkTransient = true)
		{
			var uv = cell.ToMPos(map);
			var layer = influence[cell.Layer];
			if (!layer.Contains(uv))
				return preferredSubCell != SubCell.Any ? preferredSubCell : SubCell.First;

			if (preferredSubCell != SubCell.Any && !AnyActorsAt(uv, cell, layer, preferredSubCell, checkTransient))
				return preferredSubCell;

			if (!AnyActorsAt(uv, layer))
				return map.Grid.DefaultSubCell;

			for (var i = (int)SubCell.First; i < map.Grid.SubCellOffsets.Length; i++)
				if (i != (int)preferredSubCell && !AnyActorsAt(uv, cell, layer, (SubCell)i, checkTransient))
					return (SubCell)i;

			return SubCell.Invalid;
		}

		public SubCell FreeSubCell(CPos cell, SubCell preferredSubCell, Func<Actor, bool> checkIfBlocker)
		{
			var uv = cell.ToMPos(map);
			var layer = influence[cell.Layer];
			if (!layer.Contains(uv))
				return preferredSubCell != SubCell.Any ? preferredSubCell : SubCell.First;

			if (preferredSubCell != SubCell.Any && !AnyActorsAt(uv, layer, preferredSubCell, checkIfBlocker))
				return preferredSubCell;

			if (!AnyActorsAt(uv, layer))
				return map.Grid.DefaultSubCell;

			for (var i = (byte)SubCell.First; i < map.Grid.SubCellOffsets.Length; i++)
				if (i != (byte)preferredSubCell && !AnyActorsAt(uv, layer, (SubCell)i, checkIfBlocker))
					return (SubCell)i;

			return SubCell.Invalid;
		}

		// NOTE: pos required to be in map bounds
		bool AnyActorsAt(MPos uv, CellLayer<InfluenceNode> layer)
		{
			return layer[uv] != null;
		}

		// NOTE: always includes transients with influence
		public bool AnyActorsAt(CPos a)
		{
			var uv = a.ToMPos(map);
			var layer = influence[a.Layer];
			if (!layer.Contains(uv))
				return false;

			return AnyActorsAt(uv, layer);
		}

		// NOTE: pos required to be in map bounds
		bool AnyActorsAt(MPos uv, CPos a, CellLayer<InfluenceNode> layer, SubCell sub, bool checkTransient)
		{
			var always = sub == SubCell.FullCell || sub == SubCell.Any;
			for (var i = layer[uv]; i != null; i = i.Next)
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
		public bool AnyActorsAt(CPos a, SubCell sub, bool checkTransient = true)
		{
			var uv = a.ToMPos(map);
			var layer = influence[a.Layer];
			if (!layer.Contains(uv))
				return false;

			return AnyActorsAt(uv, a, layer, sub, checkTransient);
		}

		// NOTE: can not check aircraft
		bool AnyActorsAt(MPos uv, CellLayer<InfluenceNode> layer, SubCell sub, Func<Actor, bool> withCondition)
		{
			var always = sub == SubCell.FullCell || sub == SubCell.Any;
			for (var i = layer[uv]; i != null; i = i.Next)
				if ((always || i.SubCell == sub || i.SubCell == SubCell.FullCell) && !i.Actor.Disposed && withCondition(i.Actor))
					return true;

			return false;
		}

		// NOTE: can not check aircraft
		public bool AnyActorsAt(CPos a, SubCell sub, Func<Actor, bool> withCondition)
		{
			var uv = a.ToMPos(map);
			var layer = influence[a.Layer];
			if (!layer.Contains(uv))
				return false;

			return AnyActorsAt(uv, layer, sub, withCondition);
		}

		public void AddInfluence(Actor self, IOccupySpace ios)
		{
			foreach (var c in ios.OccupiedCells())
			{
				var uv = c.Cell.ToMPos(map);
				var layer = influence[c.Cell.Layer];
				if (!layer.Contains(uv))
					continue;

				layer[uv] = new InfluenceNode { Next = layer[uv], SubCell = c.SubCell, Actor = self };

				if (cellTriggerInfluence.TryGetValue(c.Cell, out var triggers))
					foreach (var t in triggers)
						t.Dirty = true;

				CellUpdated?.Invoke(c.Cell);
			}
		}

		public void RemoveInfluence(Actor self, IOccupySpace ios)
		{
			foreach (var c in ios.OccupiedCells())
			{
				var uv = c.Cell.ToMPos(map);
				var layer = influence[c.Cell.Layer];
				if (!layer.Contains(uv))
					continue;

				var temp = layer[uv];
				RemoveInfluenceInner(ref temp, self);
				layer[uv] = temp;

				if (cellTriggerInfluence.TryGetValue(c.Cell, out var triggers))
					foreach (var t in triggers)
						t.Dirty = true;

				CellUpdated?.Invoke(c.Cell);
			}
		}

		static void RemoveInfluenceInner(ref InfluenceNode influenceNode, Actor toRemove)
		{
			if (influenceNode == null)
				return;

			RemoveInfluenceInner(ref influenceNode.Next, toRemove);

			if (influenceNode.Actor == toRemove)
				influenceNode = influenceNode.Next;
		}

		public void UpdateOccupiedCells(IOccupySpace ios)
		{
			if (CellUpdated == null)
				return;

			foreach (var c in ios.OccupiedCells())
				CellUpdated(c.Cell);
		}

		void ITick.Tick(Actor self)
		{
			// Position updates are done in one pass
			// to ensure consistency during a tick
			if (removeActorPosition.Count > 0)
			{
				foreach (var bin in bins)
				{
					var removed = bin.Actors.RemoveAll(actorShouldBeRemoved);
					if (removed > 0)
						foreach (var t in bin.ProximityTriggers)
							t.Dirty = true;
				}

				removeActorPosition.Clear();
			}

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

			var layer = influence[0];
			foreach (var c in cells)
			{
				if (!layer.Contains(c))
					continue;

				if (!cellTriggerInfluence.ContainsKey(c))
					cellTriggerInfluence.Add(c, new List<CellTrigger>());

				cellTriggerInfluence[c].Add(t);
			}

			return id;
		}

		public IEnumerable<CPos> TriggerPositions()
		{
			return cellTriggerInfluence.Keys;
		}

		public void RemoveCellTrigger(int id)
		{
			if (!cellTriggers.TryGetValue(id, out var trigger))
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
			if (!proximityTriggers.TryGetValue(id, out var t))
				return;

			foreach (var bin in BinsInBox(t.TopLeft, t.BottomRight))
				bin.ProximityTriggers.Remove(t);

			t.Dispose();
		}

		public void UpdateProximityTrigger(int id, WPos newPos, WDist newRange, WDist newVRange)
		{
			if (!proximityTriggers.TryGetValue(id, out var t))
				return;

			foreach (var bin in BinsInBox(t.TopLeft, t.BottomRight))
				bin.ProximityTriggers.Remove(t);

			t.Update(newPos, newRange, newVRange);

			foreach (var bin in BinsInBox(t.TopLeft, t.BottomRight))
				bin.ProximityTriggers.Add(t);
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

	public static class ActorMapWorldExts
	{
		/// <summary>
		/// Returns an array of custom movement layers.
		/// The <see cref="ICustomMovementLayer.Index"/> of a layer is used to index into this array.
		/// This array may contain null entries for layers which are not present in the world.
		/// This array is guaranteed to have a length of at least one. Index 0 is always null.
		/// Index 0 is kept null as layer 0 is used for the ground layer, consumers can combine
		/// the ground layer and custom layers into a single array for easy indexing.
		/// </summary>
		public static ICustomMovementLayer[] GetCustomMovementLayers(this World world)
		{
			return ((ActorMap)world.ActorMap).CustomMovementLayers;
		}
	}
}
