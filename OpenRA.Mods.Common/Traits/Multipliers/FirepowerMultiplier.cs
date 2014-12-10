#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using OpenRA.Traits;

namespace OpenRA.Mods.Common
{
	[Desc("The firepower of this actor is multiplied based on upgrade level.")]
	public class FirepowerMultiplierInfo : UpgradeMultiplierTraitInfo, ITraitInfo
	{
		public FirepowerMultiplierInfo()
			: base(new string[] { "firepower" }, new int[] { 110, 115, 120, 130 }) { }

		public object Create(ActorInitializer init) { return new FirepowerMultiplier(this); }
	}

	public class FirepowerMultiplier : UpgradeMultiplierTrait, IFirepowerModifier
	{
		public FirepowerMultiplier(FirepowerMultiplierInfo info)
			: base(info) { }

		public int GetFirepowerModifier() { return GetModifier(); }
	}
}
