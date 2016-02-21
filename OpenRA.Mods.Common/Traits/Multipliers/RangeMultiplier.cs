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

using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Range of this actor is multiplied based on upgrade level.")]
	public class RangeMultiplierInfo : UpgradeMultiplierTraitInfo, IRangeModifierInfo
	{
		public override object Create(ActorInitializer init) { return new RangeMultiplier(this, init.Self.Info.Name); }

		public int GetRangeModifierDefault()
		{
			return BaseLevel > 0 || UpgradeTypes.Length == 0 ? 100 : Modifier[0];
		}
	}

	public class RangeMultiplier : UpgradeMultiplierTrait, IRangeModifier
	{
		public RangeMultiplier(RangeMultiplierInfo info, string actorType)
			: base(info, "RangeMultiplier", actorType) { }

		public int GetRangeModifier() { return GetModifier(); }
	}
}
