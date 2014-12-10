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
	[Desc("The reloading time of this actor is multiplied based on upgrade level.")]
	public class ReloadTimeMultiplierInfo : UpgradeMultiplierTraitInfo, ITraitInfo
	{
		public ReloadTimeMultiplierInfo()
			: base(new string[] { "reload" }, new int[] { 95, 90, 85, 75 }) { }

		public object Create(ActorInitializer init) { return new ReloadTimeMultiplier(this); }
	}

	public class ReloadTimeMultiplier : UpgradeMultiplierTrait, IReloadModifier
	{
		public ReloadTimeMultiplier(ReloadTimeMultiplierInfo info)
			: base(info) { }

		public int GetReloadModifier() { return GetModifier(); }
	}
}
