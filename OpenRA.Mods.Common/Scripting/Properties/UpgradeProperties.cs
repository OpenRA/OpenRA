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
using System.IO;
using System.Linq;
using OpenRA.Mods.Common.Traits;
using OpenRA.Scripting;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Scripting
{
	[ScriptPropertyGroup("General")]
	public class UpgradeProperties : ScriptActorProperties, Requires<UpgradeManagerInfo>
	{
		readonly UpgradeManager um;

		public UpgradeProperties(ScriptContext context, Actor self)
			: base(context, self)
		{
			um = self.Trait<UpgradeManager>();
		}

		[Desc("Grant an upgrade to this actor.")]
		public void GrantUpgrade(string upgrade)
		{
			if (um.Info.Scriptable.Contains(upgrade))
				um.GrantUpgrade(Self, upgrade, this);
			else
				throw new InvalidDataException("The UpgradeManager does not allow scripts to grant/revoke upgrade `{0}`".F(upgrade));
		}

		[Desc("Revoke an upgrade that was previously granted using GrantUpgrade.")]
		public void RevokeUpgrade(string upgrade)
		{
			if (um.Info.Scriptable.Contains(upgrade))
				um.RevokeUpgrade(Self, upgrade, this);
			else
				throw new InvalidDataException("The UpgradeManager does not allow scripts to grant/revoke upgrade `{0}`".F(upgrade));
		}

		[Desc("Grant a limited-time upgrade to this actor.")]
		public void GrantTimedUpgrade(string upgrade, int duration)
		{
			if (um.Info.Scriptable.Contains(upgrade))
				um.GrantTimedUpgrade(Self, upgrade, duration);
			else
				throw new InvalidDataException("The UpgradeManager does not allow scripts to grant/revoke upgrade `{0}`".F(upgrade));
		}

		[Desc("Check whether this actor accepts a specific upgrade.")]
		public bool AcceptsUpgrade(string upgrade)
		{
			return um.Info.Scriptable.Contains(upgrade) && um.AcceptsUpgrade(Self, upgrade);
		}
	}
}