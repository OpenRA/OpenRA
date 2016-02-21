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
using System.Collections.Generic;
using System.Linq;

namespace OpenRA.Traits
{
	public enum TargetType { Invalid, Actor, Terrain, FrozenActor }
	public struct Target
	{
		public static readonly Target[] None = { };
		public static readonly Target Invalid = new Target { type = TargetType.Invalid };

		TargetType type;
		Actor actor;
		FrozenActor frozen;
		WPos pos;
		int generation;

		public static Target FromPos(WPos p) { return new Target { pos = p, type = TargetType.Terrain }; }
		public static Target FromCell(World w, CPos c, SubCell subCell = SubCell.FullCell)
		{
			return new Target { pos = w.Map.CenterOfSubCell(c, subCell), type = TargetType.Terrain };
		}

		public static Target FromOrder(World w, Order o)
		{
			return o.TargetActor != null
				? FromActor(o.TargetActor)
				: FromCell(w, o.TargetLocation);
		}

		public static Target FromActor(Actor a)
		{
			if (a == null)
				return Invalid;

			return new Target
			{
				actor = a,
				type = TargetType.Actor,
				generation = a.Generation,
			};
		}

		public static Target FromFrozenActor(FrozenActor a) { return new Target { frozen = a, type = TargetType.FrozenActor }; }

		public Actor Actor { get { return actor; } }
		public FrozenActor FrozenActor { get { return frozen; } }

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
			if (targeter == null || Type == TargetType.Invalid)
				return false;

			if (actor != null && !actor.IsTargetableBy(targeter))
				return false;

			return true;
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
						return pos;
					default:
					case TargetType.Invalid:
						throw new InvalidOperationException("Attempting to query the position of an invalid Target");
				}
			}
		}

		// Positions available to target for range checks
		static readonly WPos[] NoPositions = { };
		public IEnumerable<WPos> Positions
		{
			get
			{
				switch (Type)
				{
					case TargetType.Actor:
						if (!actor.Targetables.Any(Exts.IsTraitEnabled))
							return new[] { actor.CenterPosition };

						var targetablePositions = actor.TraitOrDefault<ITargetablePositions>();
						if (targetablePositions != null)
						{
							var positions = targetablePositions.TargetablePositions(actor);
							if (positions.Any())
								return positions;
						}

						return new[] { actor.CenterPosition };
					case TargetType.FrozenActor:
						return new[] { frozen.CenterPosition };
					case TargetType.Terrain:
						return new[] { pos };
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
					return pos.ToString();

				default:
				case TargetType.Invalid:
					return "Invalid";
			}
		}
	}
}
