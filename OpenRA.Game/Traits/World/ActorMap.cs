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
	public enum SubCell { Invalid = int.MinValue, Any = int.MinValue / 2, FullCell = 0, First = 1 }

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

		class Bin
		{
			public readonly List<Actor> Actors = new List<Actor>();
			public readonly List<ProximityTrigger> ProximityTriggers = new List<ProximityTrigger>();
		}

		class CellTrigger
		{
			public readonly int Id;
			public readonly CPos[] Footprint;
			public bool Dirty;

			Action<Actor> onActorEntered;
			Action<Actor> onActorExited;

			IEnumerable<Actor> currentActors = Enumerable.Empty<Actor>();

			public CellTrigger(int id, CPos[] footprint, Action<Actor> onActorEntered, Action<Actor> onActorExited)
			{
				Id = id;
				Footprint = footprint;

				this.onActorEntered = onActorEntered;
				this.onActorExited = onActorExited;

				// Notify any actors that are initially inside the trigger zone
				Dirty = true;
			}

			public void Tick(ActorMap am)
			{
				if (!Dirty)
					return;

				var oldActors = currentActors;
				currentActors = Footprint.SelectMany(c => am.GetUnitsAt(c)).ToList();

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
			public readonly int Id;
			public WPos Position { get; private set; }
			public WRange Range { get; private set; }

			public WPos TopLeft { get; private set; }
			public WPos BottomRight { get; private set; }

			public bool Dirty;

			Action<Actor> onActorEntered;
			Action<Actor> onActorExited;

			IEnumerable<Actor> currentActors = Enumerable.Empty<Actor>();

			public ProximityTrigger(int id, WPos pos, WRange range, Action<Actor> onActorEntered, Action<Actor> onActorExited)
			{
				Id = id;

				this.onActorEntered = onActorEntered;
				this.onActorExited = onActorExited;

				Update(pos, range);
			}

			public void Update(WPos newPos, WRange newRange)
			{
				Position = newPos;
				Range = newRange;

				var offset = new WVec(newRange, newRange, WRange.Zero);
				TopLeft = newPos - offset;
				BottomRight = newPos + offset;

				Dirty = true;
			}

			public void Tick(ActorMap am)
			{
				if (!Dirty)
					return;

				var oldActors = currentActors;
				var delta = new WVec(Range, Range, WRange.Zero);
				currentActors = am.ActorsInBox(Position - delta, Position + delta)
					.Where(a => (a.CenterPosition - Position).HorizontalLengthSquared < Range.Range * Range.Range)
					.ToList();

				var entered = currentActors.Except(oldActors);
				var exited = oldActors.Except(currentActors);

				foreach (var a in entered)
					onActorEntered(a);

				foreach (var a in exited)
					onActorExited(a);

				Dirty = false;
			}

			public void Dispose()
			{
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

			cols = world.Map.MapSize.X / info.BinSize + 1;
			rows = world.Map.MapSize.Y / info.BinSize + 1;
			bins = new Bin[rows * cols];
			for (var j = 0; j < rows; j++)
				for (var i = 0; i < cols; i++)
					bins[j * cols + i] = new Bin();

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

		public IEnumerable<Actor> GetUnitsAt(CPos a, SubCell sub)
		{
			if (!map.Contains(a))
				yield break;

			for (var i = influence[a]; i != null; i = i.Next)
				if (!i.Actor.Destroyed && (i.SubCell == sub || i.SubCell == SubCell.FullCell))
					yield return i.Actor;
		}

		public bool HasFreeSubCell(CPos a, bool checkTransient = true)
		{
			return FreeSubCell(a, SubCell.Any, checkTransient) != SubCell.Invalid;
		}

		public SubCell FreeSubCell(CPos a, SubCell preferredSubCell = SubCell.Any, bool checkTransient = true)
		{
			if (preferredSubCell > SubCell.Any && !AnyUnitsAt(a, preferredSubCell, checkTransient))
				return preferredSubCell;

			if (!AnyUnitsAt(a))
				return map.DefaultSubCell;

			for (var i = (int)SubCell.First; i < map.SubCellOffsets.Length; i++)
				if (i != (int)preferredSubCell && !AnyUnitsAt(a, (SubCell)i, checkTransient))
					return (SubCell)i;
			return SubCell.Invalid;
		}

		public SubCell FreeSubCell(CPos a, SubCell preferredSubCell, Func<Actor, bool> checkIfBlocker)
		{
			if (preferredSubCell > SubCell.Any && !AnyUnitsAt(a, preferredSubCell, checkIfBlocker))
				return preferredSubCell;

			if (!AnyUnitsAt(a))
				return map.DefaultSubCell;

			for (var i = (int)SubCell.First; i < map.SubCellOffsets.Length; i++)
				if (i != (int)preferredSubCell && !AnyUnitsAt(a, (SubCell)i, checkIfBlocker))
					return (SubCell)i;
			return SubCell.Invalid;
		}

		// NOTE: always includes transients with influence
		public bool AnyUnitsAt(CPos a)
		{
			if (!map.Contains(a))
				return false;

			return influence[a] != null;
		}

		// NOTE: can not check aircraft
		public bool AnyUnitsAt(CPos a, SubCell sub, bool checkTransient = true)
		{
			if (!map.Contains(a))
				return false;

			var always = sub == SubCell.FullCell || sub == SubCell.Any;
			for (var i = influence[a]; i != null; i = i.Next)
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
		public bool AnyUnitsAt(CPos a, SubCell sub, Func<Actor, bool> withCondition)
		{
			if (!map.Contains(a))
				return false;

			var always = sub == SubCell.FullCell || sub == SubCell.Any;
			for (var i = influence[a]; i != null; i = i.Next)
				if ((always || i.SubCell == sub || i.SubCell == SubCell.FullCell) && !i.Actor.Destroyed && withCondition(i.Actor))
					return true;

			return false;
		}

		public void AddInfluence(Actor self, IOccupySpace ios)
		{
			foreach (var c in ios.OccupiedCells())
			{
				if (!map.Contains(c.First))
					continue;

				influence[c.First] = new InfluenceNode { Next = influence[c.First], SubCell = c.Second, Actor = self };

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
				if (!map.Contains(c.First))
					continue;

				var temp = influence[c.First];
				RemoveInfluenceInner(ref temp, self);
				influence[c.First] = temp;

				List<CellTrigger> triggers;
				if (cellTriggerInfluence.TryGetValue(c.First, out triggers))
					foreach (var t in triggers)
						t.Dirty = true;
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
				var pos = a.OccupiesSpace.CenterPosition;
				var i = (pos.X / info.BinSize).Clamp(0, cols - 1);
				var j = (pos.Y / info.BinSize).Clamp(0, rows - 1);
				var bin = bins[j * cols + i];

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
			var t = new CellTrigger(id, cells, onEntry, onExit);
			cellTriggers.Add(id, t);

			foreach (var c in cells)
			{
				if (!map.Contains(c))
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

		public int AddProximityTrigger(WPos pos, WRange range, Action<Actor> onEntry, Action<Actor> onExit)
		{
			var id = nextTriggerId++;
			var t = new ProximityTrigger(id, pos, range, onEntry, onExit);
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

		public void UpdateProximityTrigger(int id, WPos newPos, WRange newRange)
		{
			ProximityTrigger t;
			if (!proximityTriggers.TryGetValue(id, out t))
				return;

			foreach (var bin in BinsInBox(t.TopLeft, t.BottomRight))
				bin.ProximityTriggers.Remove(t);

			t.Update(newPos, newRange);

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

		IEnumerable<Bin> BinsInBox(WPos a, WPos b)
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
				for (var i = i1; i <= i2; i++)
					yield return bins[j * cols + i];
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
					foreach (var actor in bins[j * cols + i].Actors)
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
			return bins.SelectMany(bin => bin.Actors.Where(actor => actor.IsInWorld));
		}
	}
}
