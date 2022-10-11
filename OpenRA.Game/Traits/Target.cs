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
using System.Collections.Generic;
using System.Linq;

namespace OpenRA.Traits
{
	public enum TargetType : byte { Invalid, Actor, Terrain, FrozenActor }
	public readonly struct Target
	{
		public static readonly Target[] None = Array.Empty<Target>();
		public static readonly Target Invalid = default;

		readonly TargetType type;
		readonly Actor actor;
		readonly FrozenActor frozen;
		readonly WPos terrainCenterPosition;
		readonly WPos[] terrainPositions;
		readonly CPos? cell;
		readonly SubCell? subCell;
		readonly int generation;

		Target(WPos terrainCenterPosition, WPos[] terrainPositions = null)
		{
			type = TargetType.Terrain;
			this.terrainCenterPosition = terrainCenterPosition;
			this.terrainPositions = terrainPositions ?? new[] { terrainCenterPosition };

			actor = null;
			frozen = null;
			cell = null;
			subCell = null;
			generation = 0;
		}

		Target(World w, CPos c, SubCell subCell)
		{
			type = TargetType.Terrain;
			terrainCenterPosition = w.Map.CenterOfSubCell(c, subCell);
			terrainPositions = new[] { terrainCenterPosition };
			cell = c;
			this.subCell = subCell;

			actor = null;
			frozen = null;
			generation = 0;
		}

		Target(Actor a, int generation)
		{
			type = TargetType.Actor;
			actor = a;
			this.generation = generation;

			terrainCenterPosition = WPos.Zero;
			terrainPositions = null;
			frozen = null;
			cell = null;
			subCell = null;
		}

		Target(FrozenActor fa)
		{
			type = TargetType.FrozenActor;
			frozen = fa;

			terrainCenterPosition = WPos.Zero;
			terrainPositions = null;
			actor = null;
			cell = null;
			subCell = null;
			generation = 0;
		}

		public static Target FromPos(WPos p) { return new Target(p); }
		public static Target FromTargetPositions(in Target t) { return new Target(t.CenterPosition, t.Positions.ToArray()); }
		public static Target FromCell(World w, CPos c, SubCell subCell = SubCell.FullCell) { return new Target(w, c, subCell); }
		public static Target FromActor(Actor a) { return a != null ? new Target(a, a.Generation) : Invalid; }
		public static Target FromFrozenActor(FrozenActor fa) { return new Target(fa); }

		public Actor Actor => actor;
		public FrozenActor FrozenActor => frozen;

		public TargetType Type
		{
			get
			{
				if (type == TargetType.Actor)
				{
					// Actor is no longer in the world
					if (!actor.IsInWorld || actor.IsDead)
						return TargetType.Invalid;

					// Actor generation has changed (teleported or captured)
					if (actor.Generation != generation)
						return TargetType.Invalid;
				}

				return type;
			}
		}

		public bool IsValidFor(Actor targeter)
		{
			if (targeter == null)
				return false;

			switch (Type)
			{
				case TargetType.Actor:
					return actor.IsTargetableBy(targeter);
				case TargetType.FrozenActor:
					return frozen.IsValid && frozen.Visible && !frozen.Hidden;
				case TargetType.Invalid:
					return false;
				default:
				case TargetType.Terrain:
					return true;
			}
		}

		// Currently all or nothing.
		// TODO: either replace based on target type or put in singleton trait
		public bool RequiresForceFire
		{
			get
			{
				if (actor == null)
					return false;

				// PERF: Avoid LINQ.
				var isTargetable = false;
				foreach (var targetable in actor.Targetables)
				{
					if (!targetable.IsTraitEnabled())
						continue;

					isTargetable = true;
					if (!targetable.RequiresForceFire)
						return false;
				}

				return isTargetable;
			}
		}

		// Representative position - see Positions for the full set of targetable positions.
		public WPos CenterPosition
		{
			get
			{
				switch (Type)
				{
					case TargetType.Actor:
						return actor.CenterPosition;
					case TargetType.FrozenActor:
						return frozen.CenterPosition;
					case TargetType.Terrain:
						return terrainCenterPosition;
					default:
					case TargetType.Invalid:
						throw new InvalidOperationException("Attempting to query the position of an invalid Target");
				}
			}
		}

		// Positions available to target for range checks
		static readonly WPos[] NoPositions = Array.Empty<WPos>();
		public IEnumerable<WPos> Positions
		{
			get
			{
				switch (Type)
				{
					case TargetType.Actor:
						return actor.GetTargetablePositions();
					case TargetType.FrozenActor:
						// TargetablePositions may be null if it is Invalid
						return frozen.TargetablePositions ?? NoPositions;
					case TargetType.Terrain:
						return terrainPositions;
					default:
					case TargetType.Invalid:
						return NoPositions;
				}
			}
		}

		public bool IsInRange(WPos origin, WDist range)
		{
			if (Type == TargetType.Invalid)
				return false;

			// Target ranges are calculated in 2D, so ignore height differences
			return Positions.Any(t => (t - origin).HorizontalLengthSquared <= range.LengthSquared);
		}

		public override string ToString()
		{
			switch (Type)
			{
				case TargetType.Actor:
					return actor.ToString();

				case TargetType.FrozenActor:
					return frozen.ToString();

				case TargetType.Terrain:
					return terrainCenterPosition.ToString();

				default:
				case TargetType.Invalid:
					return "Invalid";
			}
		}

		public static bool operator ==(in Target me, in Target other)
		{
			if (me.type != other.type)
				return false;

			switch (me.type)
			{
				case TargetType.Terrain:
					return me.terrainCenterPosition == other.terrainCenterPosition
						&& me.terrainPositions == other.terrainPositions
						&& me.cell == other.cell && me.subCell == other.subCell;

				case TargetType.Actor:
					return me.Actor == other.Actor && me.generation == other.generation;

				case TargetType.FrozenActor:
					return me.FrozenActor == other.FrozenActor;

				default:
				case TargetType.Invalid:
					return false;
			}
		}

		public static bool operator !=(in Target me, in Target other)
		{
			return !(me == other);
		}

		public override int GetHashCode()
		{
			switch (type)
			{
				case TargetType.Terrain:
					var hash = terrainCenterPosition.GetHashCode() ^ terrainPositions.GetHashCode();
					if (cell != null)
						hash ^= cell.GetHashCode();

					if (subCell != null)
						hash ^= subCell.GetHashCode();

					return hash;

				case TargetType.Actor:
					return Actor.GetHashCode() ^ generation.GetHashCode();

				case TargetType.FrozenActor:
					return FrozenActor.GetHashCode();

				default:
				case TargetType.Invalid:
					return 0;
			}
		}

		public bool Equals(Target other)
		{
			return other == this;
		}

		public override bool Equals(object other)
		{
			return other is Target t && t == this;
		}

		// Expose internal state for serialization by the orders code *only*
		internal static Target FromSerializedActor(Actor a, int generation) { return a != null ? new Target(a, generation) : Invalid; }
		internal TargetType SerializableType => type;
		internal Actor SerializableActor => actor;
		internal int SerializableGeneration => generation;
		internal CPos? SerializableCell => cell;
		internal SubCell? SerializableSubCell => subCell;
		internal WPos SerializablePos => terrainCenterPosition;
	}
}
