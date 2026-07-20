#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
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
		sealed class InfluenceNode
		{
			public InfluenceNode Next;
			public SubCell SubCell;
			public Actor Actor;
		}

		sealed class Bin
		{
			public readonly List<Actor> Actors = [];
			public readonly List<TraitPair<DetectCloaked>> Detectors = [];
			public readonly List<ProximityTrigger> ProximityTriggers = [];
		}

		sealed class CellTrigger
		{
			public readonly CPos[] Footprint;
			public bool Dirty;

			readonly Action<Actor> onActorEntered;
			readonly Action<Actor> onActorExited;
			readonly HashSet<Actor> oldActors = [];
			readonly HashSet<Actor> currentActors = [];

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

		sealed class ProximityTrigger : IDisposable
		{
			public WPos TopLeft { get; private set; }
			public WPos BottomRight { get; private set; }

			public bool Dirty;

			readonly Action<Actor> onActorEntered;
			readonly Action<Actor> onActorExited;
			readonly HashSet<Actor> oldActors = [];
			readonly HashSet<Actor> currentActors = [];

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
				foreach (var actor in am.ActorsInBoxAsIterator(position - delta, position + delta))
					if ((actor.CenterPosition - position).HorizontalLengthSquared < range.LengthSquared
						&& (vRange.Length == 0 || actor.World.Map.DistanceAboveTerrain(actor.CenterPosition).LengthSquared <= vRange.LengthSquared))
						currentActors.Add(actor);

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
		readonly Dictionary<int, CellTrigger> cellTriggers = [];
		readonly Dictionary<CPos, List<CellTrigger>> cellTriggerInfluence = [];
		readonly Dictionary<int, ProximityTrigger> proximityTriggers = [];
		int nextTriggerId;

		CellLayer<InfluenceNode>[] influence;

		// Index 0 is kept null as layer 0 is used for the ground layer.
		public ICustomMovementLayer[] CustomMovementLayers = [null];
		public event Action<CPos> CellUpdated;
		readonly Bin[] bins;
		readonly int rows, cols;

		// Position updates are done in one pass
		// to ensure consistency during a tick
		readonly HashSet<Actor> addActorPosition = [];
		readonly HashSet<Actor> removeActorPosition = [];
		readonly Predicate<Actor> actorShouldBeRemoved;
		readonly Predicate<TraitPair<DetectCloaked>> detectorShouldBeRemoved;

		public WDist LargestActorRadius { get; }
		public WDist LargestBlockingActorRadius { get; }
		public WDist LargestDetectionRange { get; }

		public ActorMap(World world, ActorMapInfo info)
		{
			this.info = info;
			map = world.Map;
			influence = [new CellLayer<InfluenceNode>(world.Map)];

			cols = CellCoordToBinIndex(world.Map.MapSize.Width) + 1;
			rows = CellCoordToBinIndex(world.Map.MapSize.Height) + 1;
			bins = new Bin[rows * cols];
			for (var row = 0; row < rows; row++)
				for (var col = 0; col < cols; col++)
					bins[row * cols + col] = new Bin();

			// PERF: Cache this delegate so it does not have to be allocated repeatedly.
			actorShouldBeRemoved = removeActorPosition.Contains;
			detectorShouldBeRemoved = detector => removeActorPosition.Contains(detector.Actor);

			LargestActorRadius = map.Rules.Actors.SelectMany(a => a.Value.TraitInfos<HitShapeInfo>()).Max(h => h.Type.OuterRadius);
			var blockers = map.Rules.Actors.Where(a => a.Value.HasTraitInfo<IBlocksProjectilesInfo>()).ToList();
			LargestBlockingActorRadius = blockers.Count != 0 ? blockers.SelectMany(a => a.Value.TraitInfos<HitShapeInfo>()).Max(h => h.Type.OuterRadius) : WDist.Zero;
			var largestDetectionRange = 0m;
			foreach (var actorInfo in map.Rules.Actors.Values)
			{
				// Conditional multipliers may independently toggle at runtime. Applying
				// every increase gives a conservative upper bound for the spatial query.
				var maximumMultiplier = 1m;
				foreach (var modifier in actorInfo.TraitInfos<DetectCloakedMultiplierInfo>())
					if (modifier.Modifier > 100)
						maximumMultiplier *= modifier.Modifier / 100m;

				foreach (var detector in actorInfo.TraitInfos<DetectCloakedInfo>())
					largestDetectionRange = Math.Max(largestDetectionRange, detector.Range.Length * maximumMultiplier);
			}

			LargestDetectionRange = new WDist((int)Math.Min(int.MaxValue, largestDetectionRange));
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

			public ActorsAtEnumerator(InfluenceNode node)
			{
				this.node = node;
				Current = null;
			}

			public void Reset() { throw new NotSupportedException(); }
			public Actor Current { get; private set; }

			readonly object IEnumerator.Current => Current;
			public readonly void Dispose() { }
			public bool MoveNext()
			{
				while (node != null)
				{
					Current = node.Actor;
					node = node.Next;
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
				return [];

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
				if (i.SubCell == sub || i.SubCell == SubCell.FullCell || always)
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

			var influenceNode = layer[uv];
			if (preferredSubCell != SubCell.Any && !AnyActorsAt(influenceNode, cell, preferredSubCell, checkTransient))
				return preferredSubCell;

			if (influenceNode == null)
				return map.Grid.DefaultSubCell;

			for (var i = (int)SubCell.First; i < map.Grid.SubCellOffsets.Length; i++)
				if (i != (int)preferredSubCell && !AnyActorsAt(influenceNode, cell, (SubCell)i, checkTransient))
					return (SubCell)i;

			return SubCell.Invalid;
		}

		public SubCell FreeSubCell(CPos cell, SubCell preferredSubCell, Func<Actor, bool> checkIfBlocker)
		{
			var uv = cell.ToMPos(map);
			var layer = influence[cell.Layer];
			if (!layer.Contains(uv))
				return preferredSubCell != SubCell.Any ? preferredSubCell : SubCell.First;

			var influenceNode = layer[uv];
			if (preferredSubCell != SubCell.Any && !AnyActorsAt(influenceNode, preferredSubCell, checkIfBlocker))
				return preferredSubCell;

			if (influenceNode == null)
				return map.Grid.DefaultSubCell;

			for (var i = (byte)SubCell.First; i < map.Grid.SubCellOffsets.Length; i++)
				if (i != (byte)preferredSubCell && !AnyActorsAt(influenceNode, (SubCell)i, checkIfBlocker))
					return (SubCell)i;

			return SubCell.Invalid;
		}

		// NOTE: always includes transients with influence
		public bool AnyActorsAt(CPos a)
		{
			var uv = a.ToMPos(map);
			var layer = influence[a.Layer];
			if (!layer.Contains(uv))
				return false;

			return layer[uv] != null;
		}

		// NOTE: pos required to be in map bounds
		static bool AnyActorsAt(InfluenceNode influenceNode, CPos a, SubCell sub, bool checkTransient)
		{
			var always = sub == SubCell.FullCell || sub == SubCell.Any;
			for (var i = influenceNode; i != null; i = i.Next)
			{
				if (always || i.SubCell == sub || i.SubCell == SubCell.FullCell)
				{
					if (checkTransient)
						return true;

					// PERF: Avoid trait lookup
					if (i.Actor.OccupiesSpace is not IPositionable pos || !pos.IsLeavingCell(a, i.SubCell))
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

			return AnyActorsAt(layer[uv], a, sub, checkTransient);
		}

		// NOTE: can not check aircraft
		static bool AnyActorsAt(InfluenceNode influenceNode, SubCell sub, Func<Actor, bool> withCondition)
		{
			var always = sub == SubCell.FullCell || sub == SubCell.Any;

			for (var i = influenceNode; i != null; i = i.Next)
				if ((always || i.SubCell == sub || i.SubCell == SubCell.FullCell) && withCondition(i.Actor))
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

			return AnyActorsAt(layer[uv], sub, withCondition);
		}

		public IEnumerable<Actor> AllActors()
		{
			foreach (var layer in influence)
			{
				if (layer == null)
					continue;
				foreach (var node in layer)
					for (var i = node; i != null; i = i.Next)
						yield return i.Actor;
			}
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
					bin.Detectors.RemoveAll(detectorShouldBeRemoved);
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
				if (a.IsInWorld && !a.Disposed)
					foreach (var detector in a.TraitsImplementingAsIterator<DetectCloaked>())
						bin.Detectors.Add(new TraitPair<DetectCloaked>(a, detector));
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
					cellTriggerInfluence.Add(c, []);

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

		public readonly struct ActorsInBoxIterator
		{
			readonly ActorMap actorMap;
			readonly int left;
			readonly int top;
			readonly int right;
			readonly int bottom;
			readonly Rectangle region;

			internal ActorsInBoxIterator(ActorMap actorMap, WPos a, WPos b)
			{
				this.actorMap = actorMap;
				left = Math.Min(a.X, b.X);
				top = Math.Min(a.Y, b.Y);
				right = Math.Max(a.X, b.X);
				bottom = Math.Max(a.Y, b.Y);
				region = actorMap.BinRectangleCoveringWorldArea(left, top, right, bottom);
			}

			public ActorsInBoxEnumerator GetEnumerator()
			{
				return new ActorsInBoxEnumerator(actorMap, left, top, right, bottom, region);
			}
		}

		public struct ActorsInBoxEnumerator
		{
			readonly ActorMap actorMap;
			readonly int left;
			readonly int top;
			readonly int right;
			readonly int bottom;
			readonly int minCol;
			readonly int maxCol;
			readonly int maxRow;
			int row;
			int col;
			int actorIndex;
			List<Actor> actors;

			internal ActorsInBoxEnumerator(ActorMap actorMap, int left, int top, int right, int bottom, Rectangle region)
			{
				this.actorMap = actorMap;
				this.left = left;
				this.top = top;
				this.right = right;
				this.bottom = bottom;
				minCol = region.Left;
				maxCol = region.Right;
				maxRow = region.Bottom;
				row = region.Top;
				col = minCol - 1;
				actorIndex = 0;
				actors = null;
				Current = null;
			}

			public Actor Current { get; private set; }

			public bool MoveNext()
			{
				while (true)
				{
					if (actors != null)
					{
						while (actorIndex < actors.Count)
						{
							var actor = actors[actorIndex++];
							if (!actor.IsInWorld)
								continue;

							var center = actor.CenterPosition;
							if (left <= center.X && center.X <= right && top <= center.Y && center.Y <= bottom)
							{
								Current = actor;
								return true;
							}
						}
					}

					if (col < maxCol)
						col++;
					else
					{
						col = minCol;
						if (++row > maxRow)
						{
							Current = null;
							return false;
						}
					}

					actors = actorMap.BinAt(row, col).Actors;
					actorIndex = 0;
				}
			}
		}

		public ActorsInBoxIterator ActorsInBoxAsIterator(WPos a, WPos b)
		{
			return new ActorsInBoxIterator(this, a, b);
		}

		public readonly struct DetectorsInBoxIterator
		{
			readonly ActorMap actorMap;
			readonly int left;
			readonly int top;
			readonly int right;
			readonly int bottom;
			readonly Rectangle region;

			internal DetectorsInBoxIterator(ActorMap actorMap, WPos a, WPos b)
			{
				this.actorMap = actorMap;
				left = Math.Min(a.X, b.X);
				top = Math.Min(a.Y, b.Y);
				right = Math.Max(a.X, b.X);
				bottom = Math.Max(a.Y, b.Y);
				region = actorMap.BinRectangleCoveringWorldArea(left, top, right, bottom);
			}

			public DetectorsInBoxEnumerator GetEnumerator()
			{
				return new DetectorsInBoxEnumerator(actorMap, left, top, right, bottom, region);
			}
		}

		public struct DetectorsInBoxEnumerator
		{
			readonly ActorMap actorMap;
			readonly int left;
			readonly int top;
			readonly int right;
			readonly int bottom;
			readonly int minCol;
			readonly int maxCol;
			readonly int maxRow;
			int row;
			int col;
			int detectorIndex;
			List<TraitPair<DetectCloaked>> detectors;

			internal DetectorsInBoxEnumerator(ActorMap actorMap, int left, int top, int right, int bottom, Rectangle region)
			{
				this.actorMap = actorMap;
				this.left = left;
				this.top = top;
				this.right = right;
				this.bottom = bottom;
				minCol = region.Left;
				maxCol = region.Right;
				maxRow = region.Bottom;
				row = region.Top;
				col = minCol - 1;
				detectorIndex = 0;
				detectors = null;
				Current = default;
			}

			public TraitPair<DetectCloaked> Current { get; private set; }

			public bool MoveNext()
			{
				while (true)
				{
					if (detectors != null)
					{
						while (detectorIndex < detectors.Count)
						{
							var detector = detectors[detectorIndex++];
							var actor = detector.Actor;
							if (!actor.IsInWorld)
								continue;

							var center = actor.CenterPosition;
							if (left <= center.X && center.X <= right && top <= center.Y && center.Y <= bottom)
							{
								Current = detector;
								return true;
							}
						}
					}

					if (col < maxCol)
						col++;
					else
					{
						col = minCol;
						if (++row > maxRow)
						{
							Current = default;
							return false;
						}
					}

					detectors = actorMap.BinAt(row, col).Detectors;
					detectorIndex = 0;
				}
			}
		}

		public DetectorsInBoxIterator DetectorsInBox(WPos a, WPos b)
		{
			return new DetectorsInBoxIterator(this, a, b);
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
