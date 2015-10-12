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
	[Desc("Disable the actor when this trait is enabled by an upgrade.")]
	public class DisableOnUpgradeInfo : UpgradableTraitInfo
	{
		public override object Create(ActorInitializer init) { return new DisableOnUpgrade(this); }
	}

	public class DisableOnUpgrade : UpgradableTrait<DisableOnUpgradeInfo>, IDisable, IDisableMove
	{
		public DisableOnUpgrade(DisableOnUpgradeInfo info)
			: base(info) { }

		public bool Disabled { get { return !IsTraitDisabled; } }
		public bool MoveDisabled(Actor self) { return !IsTraitDisabled; }
	}
}
