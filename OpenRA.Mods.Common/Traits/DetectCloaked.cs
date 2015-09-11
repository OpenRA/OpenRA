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
	[Desc("Actor can reveal Cloak actors in a specified range.",
		"Provides \"detect\" range type while enabled with CloakTypes as accepted variants")]
	public class DetectCloakedInfo : UpgradableTraitInfo, IRanged, IProvidesRangesInfo
	{
		[Desc("Specific cloak classifications I can reveal.")]
		public readonly HashSet<string> CloakTypes = new HashSet<string> { "Cloak" };

		[Desc("Measured in cells.")]
		public readonly int Range = 5;

		[FieldLoader.Ignore] public readonly IEnumerable<IRanged> AsRanges;

		public DetectCloakedInfo() { AsRanges = new IRanged[] { this }; }

		public override object Create(ActorInitializer init) { return new DetectCloaked(this); }
		public WDist GetMaximumRange(ActorInfo ai, World w) { return WDist.FromCells(Range); }
		public WDist GetMinimumRange(ActorInfo ai, World w) { return WDist.Zero; }
		public bool ProvidesRanges(string type, string variant, ActorInfo ai, World w)
		{
			return type == "detection" && (string.IsNullOrEmpty(variant) || CloakTypes.Contains(variant));
		}

		public IEnumerable<IRanged> GetRanges(string type, string variant, ActorInfo ai, World w)
		{
			return UpgradeMinEnabledLevel == 0 ? AsRanges : Traits.ProvidesRanges.NoRanges;
		}
	}

	public class DetectCloaked : UpgradableTrait<DetectCloakedInfo>, IProvidesRanges
	{
		public DetectCloaked(DetectCloakedInfo info) : base(info) { }

		public bool ProvidesRanges(string type, string variant) { return Info.ProvidesRanges(type, variant, null, null); }
		public IEnumerable<IRanged> GetRanges(string type, string variant)
		{
			return IsTraitDisabled ? Traits.ProvidesRanges.NoRanges : Info.AsRanges;
		}
	}
}
