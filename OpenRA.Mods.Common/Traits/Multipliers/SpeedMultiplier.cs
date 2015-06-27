#region Copyright & License Information
/*
 * Copyright 2007-2015 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("The speed of this actor is multiplied based on upgrade level if specified.")]
	public class SpeedMultiplierInfo : UpgradeMultiplierTraitInfo, ITraitInfo
	{
		public SpeedMultiplierInfo()
			: base(new int[] { 110, 115, 120, 150 }) { }

		public object Create(ActorInitializer init) { return new SpeedMultiplier(this); }
	}

	public class SpeedMultiplier : UpgradeMultiplierTrait, ISpeedModifier
	{
		public SpeedMultiplier(SpeedMultiplierInfo info)
			: base(info) { }

		public int GetSpeedModifier() { return GetModifier(); }
	}
}
