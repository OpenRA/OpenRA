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
	public enum TargetType { Invalid, Actor, Terrain, FrozenActor }
	public struct Target
	{
		public static readonly Target[] None = {};
		public static readonly Target Invalid = new Target { type = TargetType.Invalid };

		TargetType type;
		Actor actor;
		ITargetable targetable;
		FrozenActor frozen;
		WPos pos;
		int generation;

		public static Target FromPos(WPos p) { return new Target { pos = p, type = TargetType.Terrain }; }
		public static Target FromCell(World w, CPos c) { return new Target { pos = w.Map.CenterOfCell(c), type = TargetType.Terrain }; }
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
				targetable = a.TraitOrDefault<ITargetable>(),
				type = TargetType.Actor,
				generation = a.Generation,
			};
		}

		public static Target FromFrozenActor(FrozenActor a)  { return new Target { frozen = a, type = TargetType.FrozenActor }; }

		public Actor Actor { get { return actor; } }
		public FrozenActor FrozenActor { get { return frozen; } }

		public TargetType Type
		{
			get
			{
				if (type == TargetType.Actor)
				{
					// Actor is no longer in the world
					if (!actor.IsInWorld || actor.IsDead())
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

			if (targetable != null && !targetable.TargetableBy(actor, targeter))
				return false;

			return true;
		}

		public bool RequiresForceFire
		{
			get { return targetable != null && targetable.RequiresForceFire; }
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
		static readonly WPos[] NoPositions = {};
		public IEnumerable<WPos> Positions
		{
			get
			{
				switch (Type)
				{
				case TargetType.Actor:
					var targetable = actor.TraitOrDefault<ITargetable>();
					if (targetable == null)
						return new [] { actor.CenterPosition };

					var positions = targetable.TargetablePositions(actor);
					return positions.Any() ? positions : new [] { actor.CenterPosition };
				case TargetType.FrozenActor:
					return new [] { frozen.CenterPosition };
				case TargetType.Terrain:
					return new [] { pos };
				default:
				case TargetType.Invalid:
					return NoPositions;
				}
			}
		}

		public bool IsInRange(WPos origin, WRange range)
		{
			if (Type == TargetType.Invalid)
				return false;

			// Target ranges are calculated in 2D, so ignore height differences
			var rangeSquared = range.Range*range.Range;
			return Positions.Any(t => (t - origin).HorizontalLengthSquared <= rangeSquared);
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
