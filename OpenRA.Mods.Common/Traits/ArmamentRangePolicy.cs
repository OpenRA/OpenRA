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
using System.Linq;
using OpenRA.Activities;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	/// <summary>Implement on the info of an optional, mod-defined range policy.</summary>
	public interface IArmamentRangePolicyInfo : ITraitInfoInterface { }

	/// <summary>Mod-owned weapon reachability. No policy preserves legacy range rules.</summary>
	public interface IArmamentRangePolicy
	{
		WDist GetMaximumRange(Armament armament, WDist nominalRange);
		bool IsInRange(Armament armament, WPos origin, in Target target, WDist nominalMinRange, WDist nominalMaxRange);
		int RangeRevision { get; }
	}

	public interface IMoveWithinTargetRange
	{
		Activity MoveWithinRange(in Target target, TargetRange range,
			WPos? initialTargetPosition = null, Color? targetLineColor = null);
	}

	public interface IMoveWithinTargetRangeInfo : ITraitInfoInterface { }

	/// <summary>Search bounds and the exact predicate shared by movement and firing.</summary>
	public sealed class TargetRange
	{
		readonly Armament[] armaments;
		readonly bool requireAll;
		readonly Map map;

		public TargetRange(Map map, Armament[] armaments, bool requireAll)
		{
			if (armaments.Length == 0)
				throw new ArgumentException("At least one armament is required.", nameof(armaments));

			this.map = map;
			this.armaments = armaments.ToArray();
			this.requireAll = requireAll;
		}

		Armament[] Active => armaments.Where(a => !a.IsTraitPaused && !a.IsTraitDisabled).ToArray();

		public WDist Maximum
		{
			get
			{
				var active = Active;
				return active.Length == 0 ? armaments.Max(a => a.MaxRange())
					: requireAll ? active.Min(a => a.MaxRange()) : active.Max(a => a.MaxRange());
			}
		}

		public int Revision
		{
			get
			{
				var revision = map.TerrainGeometryRevision;
				foreach (var a in armaments)
				{
					revision = unchecked(revision * 31 + a.RangeRevision);
					revision = unchecked(revision * 31 + a.MaxRange().Length);
					revision = unchecked(revision * 31 + (a.IsTraitPaused || a.IsTraitDisabled ? 1 : 0));
				}

				return revision;
			}
		}

		public bool Contains(WPos origin, in Target target)
		{
			var active = Active;
			var all = requireAll && active.Length != 0;
			if (active.Length == 0)
				active = armaments;

			// Avoid capturing an in parameter in LINQ predicates.
			var any = false;
			foreach (var a in active)
			{
				var within = a.IsInRange(origin, target);
				if (all && !within)
					return false;

				any |= within;
			}

			return any;
		}
	}
}
