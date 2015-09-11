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
using System.Linq;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("This actor enables the radar minimap.")]
	public class ProvidesRadarInfo : TraitInfo<ProvidesRadar> { }

	public class ProvidesRadar : ITick
	{
		public bool IsActive { get; private set; }

		public void Tick(Actor self) { IsActive = UpdateActive(self); }

		static bool UpdateActive(Actor self)
		{
			// Check if powered
			if (self.IsDisabled()) return false;

			var isJammed = self.World.ActorsWithTrait<JamsRadar>().Any(a => a.Actor.Owner.Stances[self.Owner] != Stance.Ally
				&& (self.Location - a.Actor.Location).Length <= a.Actor.Info.Traits.Get<JamsRadarInfo>().Range);

			return !isJammed;
		}
	}

	[Desc("When an actor with this trait is in range of an actor with ProvidesRadar, it will temporarily disable the radar minimap for the enemy player.",
		"Provides \"radar\" variant of \"jams\" range type.")]
	public class JamsRadarInfo : ITraitInfo, IRanged, IProvidesRangesInfo
	{
		[FieldLoader.Ignore] public readonly IEnumerable<IRanged> AsRanges;

		[Desc("Range for jamming.")]
		public readonly int Range = 0;

		public JamsRadarInfo() { AsRanges = new IRanged[] { this }; }
		public object Create(ActorInitializer init) { return new JamsRadar(this); }
		public WDist GetMaximumRange(ActorInfo ai, World w) { return WDist.FromCells(Range); }
		public WDist GetMinimumRange(ActorInfo ai, World w) { return WDist.Zero; }
		public IEnumerable<IRanged> GetRanges(string type, string variant, ActorInfo ai, World w) { return AsRanges; }
		public bool ProvidesRanges(string type, string variant, ActorInfo ai, World w)
		{
			return type == "jams" && (string.IsNullOrEmpty(variant) || variant == "radar");
		}
	}

	public class JamsRadar : IProvidesRanges
	{
		readonly JamsRadarInfo info;

		public JamsRadar(JamsRadarInfo info) { this.info = info; }
		public bool ProvidesRanges(string type, string variant) { return info.ProvidesRanges(type, variant, null, null); }
		public IEnumerable<IRanged> GetRanges(string type, string variant) { return info.AsRanges; }
	}
}
