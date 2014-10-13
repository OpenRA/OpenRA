#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using OpenRA.Mods.Common;
using OpenRA.Scripting;
using OpenRA.Traits;

namespace OpenRA.Mods.RA.Scripting
{
	[ScriptPropertyGroup("General")]
	public class UpgradeProperties : ScriptActorProperties, Requires<UpgradeManagerInfo>
	{
		UpgradeManager um;
		public UpgradeProperties(ScriptContext context, Actor self)
			: base(context, self)
		{
			um = self.Trait<UpgradeManager>();
		}

		[Desc("Grant an upgrade to this actor.")]
		public void GrantUpgrade(string upgrade)
		{
			um.GrantUpgrade(self, upgrade, this);
		}

		[Desc("Revoke an upgrade that was previously granted using GrantUpgrade.")]
		public void RevokeUpgrade(string upgrade)
		{
			um.RevokeUpgrade(self, upgrade, this);
		}

		[Desc("Grant a limited-time upgrade to this actor.")]
		public void GrantTimedUpgrade(string upgrade, int duration)
		{
			um.GrantTimedUpgrade(self, upgrade, duration);
		}

		[Desc("Check whether this actor accepts a specific upgrade.")]
		public bool AcceptsUpgrade(string upgrade)
		{
			return um.AcceptsUpgrade(self, upgrade);
		}
	}
}