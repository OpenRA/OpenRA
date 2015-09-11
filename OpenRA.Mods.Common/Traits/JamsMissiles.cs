#region Copyright & License Information
/*
 * Copyright 2007-2015 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Collections.Generic;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Jams missle tracking and provides \"missiles\" variant of \"jams\" range type.")]
	public class JamsMissilesInfo : ITraitInfo, IRanged, IProvidesRangesInfo
	{
		[FieldLoader.Ignore] public readonly IEnumerable<IRanged> AsRanges;

		public readonly int Range = 0;
		public readonly bool AlliedMissiles = true;
		public readonly int Chance = 100;

		public JamsMissilesInfo() { AsRanges = new IRanged[] { this }; }
		public object Create(ActorInitializer init) { return new JamsMissiles(this); }
		public WDist GetMaximumRange(ActorInfo ai, World w) { return WDist.FromCells(Range); }
		public WDist GetMinimumRange(ActorInfo ai, World w) { return WDist.Zero; }
		public IEnumerable<IRanged> GetRanges(string type, string variant, ActorInfo ai, World w) { return AsRanges; }
		public bool ProvidesRanges(string type, string variant, ActorInfo ai, World w)
		{
			return type == "jams" && (string.IsNullOrEmpty(variant) || variant == "missiles");
		}
	}

	public class JamsMissiles : IProvidesRanges
	{
		readonly JamsMissilesInfo info;

		// Convert cells to world units
		public int Range { get { return 1024 * info.Range; } }
		public bool AlliedMissiles { get { return info.AlliedMissiles; } }
		public int Chance { get { return info.Chance; } }

		public JamsMissiles(JamsMissilesInfo info) { this.info = info; }
		public bool ProvidesRanges(string type, string variant) { return info.ProvidesRanges(type, variant, null, null); }
		public IEnumerable<IRanged> GetRanges(string type, string variant) { return info.AsRanges; }
	}
}
