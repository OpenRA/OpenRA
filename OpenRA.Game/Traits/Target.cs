#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
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
	public struct Target
	{
		public static readonly Target[] NoTargets = {};
		public static readonly Target None = new Target();

		Actor actor;
		WPos pos;
		bool valid;
		int generation;

		public static Target FromPos(WPos p) { return new Target { pos = p, valid = true }; }
		public static Target FromPos(PPos p) { return new Target { pos = p.ToWPos(0), valid = true }; }
		public static Target FromCell(CPos c) { return new Target { pos = c.CenterPosition, valid = true }; }
		public static Target FromOrder(Order o)
		{
			return o.TargetActor != null
				? Target.FromActor(o.TargetActor)
				: Target.FromCell(o.TargetLocation);
		}

		public static Target FromActor(Actor a)
		{
			return new Target
			{
				actor = a,
				valid = (a != null),
				generation = a.Generation,
			};
		}

		public bool IsValid { get { return valid && (actor == null || (actor.IsInWorld && !actor.IsDead() && actor.Generation == generation)); } }
		public PPos PxPosition { get { return IsActor ? actor.Trait<IHasLocation>().PxPosition : PPos.FromWPos(pos); } }
		public PPos CenterLocation { get { return PxPosition; } }
		public Actor Actor { get { return IsActor ? actor : null; } }

		// TODO: This should return true even if the actor is destroyed
		public bool IsActor { get { return actor != null && !actor.Destroyed; } }

		// Representative position - see Positions for the full set of targetable positions.
		public WPos CenterPosition
		{
			get
			{
				if (!IsValid)
					throw new InvalidOperationException("Attempting to query the position of an invalid Target");

				return actor != null ? actor.CenterPosition : pos;
			}
		}

		// Positions available to target for range checks
		static readonly WPos[] NoPositions = {};
		public IEnumerable<WPos> Positions
		{
			get
			{
				if (!IsValid)
					return NoPositions;

				if (actor == null)
					return new []{pos};

				var targetable = actor.TraitOrDefault<ITargetable>();
				if (targetable == null)
					return new []{actor.CenterPosition};

				return targetable.TargetableCells(actor).Select(c => c.CenterPosition);
			}
		}

		public bool IsInRange(WPos origin, WRange range)
		{
			if (!IsValid)
				return false;

			// Target ranges are calculated in 2D, so ignore height differences
			return Positions.Any(t => (t.X - origin.X)*(t.X - origin.X) +
				(t.Y - origin.Y)*(t.Y - origin.Y) <= range.Range*range.Range);
		}

	}
}
