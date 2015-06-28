#region Copyright & License Information
/*
 * Copyright 2007-2015 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using OpenRA.GameRules;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	public class DisableUpgradeInfo : UpgradableTraitInfo
	{
		public override object Create(ActorInitializer init) { return new DisableUpgrade(this); }
	}

	public class DisableUpgrade : UpgradableTrait<DisableUpgradeInfo>, IDisable, IDisableMove
	{
		public DisableUpgrade(DisableUpgradeInfo info)
			: base(info) { }

		// Disable the actor when this trait is enabled.
		public bool Disabled { get { return !IsTraitDisabled; } }
		public bool MoveDisabled(Actor self) { return !IsTraitDisabled; }
	}
}
